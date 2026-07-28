"""Deterministic combat planning: search for a trajectory, then replay it.

Why a planner instead of a combat policy
----------------------------------------
STS2 combat is deterministic given the combat's starting seed: the same
action sequence from the same starting state always produces the same
outcome. That turns combat from a control problem into an OPTIMIZATION
problem -- find an action sequence that wins while losing as little HP as
possible -- and an optimizer needs no training, no generalization, and no
value network. It plays each fight with the exact deck the run agent built,
however unusual that deck is, which also dissolves the combat-agent
distribution-shift problem entirely.

This replaces the learned combat agent in the hierarchy. The RL problem
shrinks to out-of-combat decisions only (map routing, card picks, shops,
rests, events), which is precisely the slice the hierarchical env already
isolates.

Search shape
------------
Beam search over cloned ``CombatState``s, expanded one decision at a time:

* Nodes are deduplicated by a cheap state signature (HP/block/energy/enemy
  HP/hand/pile sizes/turn), because different action orders frequently
  transpose into identical states (playing Strike A before Strike B).
* Each expansion clones the parent state (``clone_combat``) and applies one
  legal action (``apply_combat_action``) -- the same primitives MCTS used,
  so no live state is ever touched.
* Terminal WINs are scored by HP retained (plus a small bonus per unused
  potion -- burning every potion on an easy fight is a real cost to the rest
  of the run). The best win found anywhere in the search is kept.
* If the node budget runs out with no win found, the best-heuristic
  non-terminal line is returned instead: fights the search cannot win still
  need a least-bad action sequence.

Perfect information caveat
--------------------------
The planner searches the seeded state, so it implicitly knows the hidden
draw order. That is exactly what "treat combat as deterministic
optimization" means, and it is sound in-sim and for a bridge whose simulator
matches the client bit-for-bit (the plan replays identically in the real
game). Any sim/client divergence breaks replay, so a bridge integration must
replan on divergence rather than blindly continuing the queue.
"""

from __future__ import annotations

import logging
import time
from dataclasses import dataclass, field

import numpy as np

from sts2_env.gym_env.action_space import get_action_mask
from sts2_env.search.combat_mcts import apply_combat_action, clone_combat

logger = logging.getLogger(__name__)

#: END TURN is combat action 0 in the unified layout.
ACTION_END_TURN = 0


@dataclass
class PlannerConfig:
    #: Beam width: non-terminal nodes kept per depth level.
    beam_width: int = 12
    #: Hard cap on clone+apply expansions per combat (the cost driver).
    max_expansions: int = 8_000
    #: Hard cap on plan length in decisions.
    max_depth: int = 240
    #: Objective weights. Win dominates everything; among wins, HP retained
    #: dominates potions; potions break ties so the planner does not burn
    #: consumables it did not need.
    win_bonus: float = 1_000_000.0
    hp_weight: float = 1_000.0
    potion_bonus: float = 50.0
    #: Heuristic weights for non-terminal ranking (search guidance only).
    h_player_hp: float = 3.0
    #: Block is scored only up to the damage actually INCOMING this turn.
    #: It used to be rewarded unconditionally, which made the planner stack
    #: block against enemies that were buffing or defending -- block that
    #: expires unused at end of turn. Observed live: defensive cards played
    #: when nothing was attacking.
    h_block: float = 0.6
    #: Explicit penalty for block beyond incoming damage, so an over-block
    #: line never ties with an equivalent line that attacked instead.
    h_wasted_block: float = 0.35
    h_enemy_hp: float = -2.0
    h_osty_hp: float = 0.9
    #: A turn that ends with every enemy dead takes zero further damage and
    #: ends the fight; nothing else in the heuristic is comparable.
    lethal_bonus: float = 400.0
    #: A turn where block >= incoming damage takes ZERO HP loss. Ranked just
    #: below lethal, because surviving intact is the planner's whole job.
    safe_turn_bonus: float = 120.0
    #: Stop expanding once this many wins exist overall AND the best of them
    #: is within ``damage_tolerance`` of a damage-free clear. A win found
    #: early is usually a fast-kill line, not the cleanest one, so the search
    #: keeps refining until a low-damage win exists or the beam collapses.
    enough_wins: int = 8
    #: Damage (HP lost) considered "clean enough" to stop searching/escalating.
    damage_tolerance: float = 3.0
    #: Once a win exists, beam nodes whose current HP cannot beat the best
    #: win's final HP (plus this healing slack) are pruned -- they can only
    #: produce strictly worse wins. The slack covers in-combat healing.
    prune_margin: float = 10.0
    #: Wall-clock soft cap per plan_combat call. Search returns the best
    #: result found so far when it expires; 0 disables. Keeps the min-damage
    #: refinement from spending minutes polishing a single fight.
    time_budget_s: float = 8.0


@dataclass
class PlanResult:
    actions: list[int]
    won: bool
    final_hp: float
    expansions: int
    depth: int
    #: Player HP when planning started; damage taken = entry_hp - final_hp.
    entry_hp: float = 0.0
    #: True when the budget ran out before any terminal node was reached and
    #: the returned line is the best-heuristic prefix (not a full playout).
    truncated: bool = False


def _signature(combat) -> tuple:
    """Cheap transposition key. Two states with equal signatures are treated
    as interchangeable for search purposes (the full state is far bigger, but
    HP/energy/zones/turn capture everything the objective can see)."""
    p = combat.primary_player
    state = combat.combat_player_states[0]
    enemies = tuple(
        (str(e.monster_id), e.current_hp, e.block, len(e.powers))
        for e in combat.enemies if e.is_alive
    )
    hand = tuple(sorted((str(c.card_id), c.upgraded) for c in state.hand))
    powers = tuple(sorted(
        (str(pid), inst.amount if hasattr(inst, "amount") else 0)
        for pid, inst in p.powers.items()
    ))
    return (
        p.current_hp, p.block, state.energy, combat.turn_count,
        len(state.draw), len(state.discard), len(state.exhaust),
        hand, enemies, powers,
    )


def incoming_damage(combat) -> int:
    """Damage the living enemies INTEND to deal this turn.

    Read from each monster's currently-rolled move, which is exactly the
    intent the player sees. Non-attack intents (defend/buff/debuff) count
    zero, which is the whole point: block is only worth anything against an
    attack.
    """
    total = 0
    for enemy in combat.enemies:
        if not enemy.is_alive:
            continue
        ai = combat.enemy_ais.get(enemy.combat_id)
        move = getattr(ai, "current_move", None)
        for intent in (getattr(move, "intents", None) or []):
            if getattr(intent, "is_attack", False):
                total += int(getattr(intent, "total_damage", 0) or 0)
    return total


def _heuristic(combat, cfg: PlannerConfig) -> float:
    """Rank non-terminal states for beam retention.

    Two properties dominate, in this order: killing everything this turn,
    and taking zero damage this turn. Both are checked explicitly rather
    than left to emerge from the linear terms, because a linear combination
    of HP and enemy HP does not distinguish "survives at 1 HP" from "takes
    nothing", and the planner's objective is to minimise HP loss.
    """
    p = combat.primary_player
    enemy_hp = sum(e.current_hp for e in combat.enemies if e.is_alive)
    osty = combat.get_osty(p)
    osty_hp = osty.current_hp if (osty is not None and osty.is_alive) else 0

    incoming = incoming_damage(combat)
    useful_block = min(p.block, incoming)
    wasted_block = max(0, p.block - incoming)

    score = (
        cfg.h_player_hp * p.current_hp
        + cfg.h_block * useful_block
        - cfg.h_wasted_block * wasted_block
        + cfg.h_enemy_hp * enemy_hp
        + cfg.h_osty_hp * osty_hp
    )
    if enemy_hp <= 0:
        score += cfg.lethal_bonus
    elif incoming > 0 and p.block >= incoming:
        score += cfg.safe_turn_bonus
    return score


def _terminal_score(combat, cfg: PlannerConfig) -> tuple[bool, float]:
    """(won, objective score) for a finished combat."""
    p = combat.primary_player
    won = bool(p is not None and p.is_alive)
    if not won:
        return False, -cfg.win_bonus
    potions_left = sum(
        1 for x in combat.combat_player_states[0].potions if x is not None
    )
    return True, (
        cfg.win_bonus
        + cfg.hp_weight * max(0, p.current_hp)
        + cfg.potion_bonus * potions_left
    )


def plan_combat_escalating(
    root_combat,
    ladder: tuple[PlannerConfig, ...] | None = None,
) -> PlanResult:
    """Plan with escalating budgets: cheap first, wider only on a loss.

    Most fights are won at the default budget in ~1s; the expensive
    configurations only run for fights the cheap search loses, which is
    where they demonstrably matter (a 46-HP fight vs 127 enemy HP flipped
    from loss to WIN between beam 32 and beam 64). The first winning plan is
    returned immediately; if every rung loses, the best loss from the
    LARGEST budget is returned (deepest search = least-bad line).
    """
    if ladder is None:
        ladder = (
            PlannerConfig(),
            PlannerConfig(beam_width=32, max_expansions=40_000),
            PlannerConfig(beam_width=64, max_expansions=120_000),
        )
    last = None
    best_win = None
    for i, cfg in enumerate(ladder):
        last = plan_combat(root_combat, cfg)
        if last.won:
            if best_win is None or last.final_hp > best_win.final_hp:
                best_win = last
            damage = last.entry_hp - last.final_hp
            if damage <= cfg.damage_tolerance:
                return best_win
            # Bloody win: give the NEXT rung one chance to find a cleaner
            # line, then take the best win seen. One extra rung bounds the
            # cost to fights where minimizing damage actually has room.
            if i + 1 < len(ladder):
                nxt = plan_combat(root_combat, ladder[i + 1])
                if nxt.won and nxt.final_hp > best_win.final_hp:
                    best_win = nxt
            return best_win
    return last


def plan_combat(root_combat, config: PlannerConfig | None = None) -> PlanResult:
    """Search a cloned copy of ``root_combat`` for the best action sequence.

    The input state is never mutated. Returns the best trajectory found
    under the budget; ``won`` reports whether that trajectory ends in a win.
    """
    cfg = config or PlannerConfig()

    root = clone_combat(root_combat)
    # (state, path, heuristic score) triples per beam level.
    frontier: list[tuple[object, list[int]]] = [(root, [])]
    seen: set[tuple] = {_signature(root)}

    best_win: tuple[float, list[int], float] | None = None  # (score, path, hp)
    best_loss: tuple[float, list[int], float] | None = None
    best_open: tuple[float, list[int]] | None = None  # heuristic fallback
    expansions = 0
    wins_found = 0

    entry_hp = float(max(0, root.primary_player.current_hp))
    deadline = (time.monotonic() + cfg.time_budget_s) if cfg.time_budget_s > 0 else None

    # Zero-damage turn tracking. The requirement is explicit: for every turn,
    # prefer a line that takes NO HP loss, either by killing everything
    # before ending the turn or by blocking the whole incoming attack. The
    # beam heuristic biases toward those, but a bias can still be outvoted;
    # this records the best genuinely damage-free line so it can be chosen
    # outright when one exists.
    root_turn = int(getattr(root, "turn_count", 0))
    root_incoming = incoming_damage(root)
    best_safe: tuple[float, list[int]] | None = None

    for depth in range(cfg.max_depth):
        if not frontier:
            break
        if deadline is not None and time.monotonic() > deadline:
            break
        best_win_hp = best_win[2] if best_win is not None else None
        if (wins_found >= cfg.enough_wins and best_win_hp is not None
                and entry_hp - best_win_hp <= cfg.damage_tolerance):
            break
        if best_win_hp is not None:
            # A win retaining best_win_hp exists; lines already below it
            # (minus healing slack) cannot yield a strictly better win.
            frontier = [
                (st, pa) for st, pa in frontier
                if st.primary_player.current_hp + cfg.prune_margin > best_win_hp
            ]
            if not frontier:
                break
        children: list[tuple[float, object, list[int]]] = []
        for state, path in frontier:
            mask = get_action_mask(state).astype(bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                continue
            for action in legal:
                if expansions >= cfg.max_expansions:
                    break
                if (deadline is not None and expansions % 64 == 0
                        and time.monotonic() > deadline):
                    break
                expansions += 1
                child = clone_combat(state)
                try:
                    apply_combat_action(child, int(action))
                except Exception:
                    # A sim exception in a hypothetical line: prune it.
                    continue
                new_path = path + [int(action)]
                cp = child.primary_player
                dead = cp is None or not cp.is_alive
                if child.is_over or dead:
                    won, score = _terminal_score(child, cfg)
                    hp = float(max(0, cp.current_hp)) if cp is not None else 0.0
                    if won:
                        if best_win is None or score > best_win[0]:
                            best_win = (score, new_path, hp)
                        wins_found += 1
                    else:
                        if best_loss is None or score > best_loss[0]:
                            best_loss = (score, new_path, hp)
                    continue
                # A zero-damage turn: the action ended the turn (or the fight)
                # and the player is provably unharmed this turn -- every enemy
                # is dead, or block covers the full incoming attack.
                if int(action) == ACTION_END_TURN and cp is not None:
                    alive_hp = sum(e.current_hp for e in child.enemies if e.is_alive)
                    covered = (alive_hp <= 0) or (cp.block >= root_incoming)
                    if covered and cp.current_hp >= entry_hp:
                        cand = (float(cp.current_hp), new_path)
                        if best_safe is None or cand[0] > best_safe[0]:
                            best_safe = cand

                sig = _signature(child)
                if sig in seen:
                    continue
                seen.add(sig)
                h = _heuristic(child, cfg)
                children.append((h, child, new_path))
            if expansions >= cfg.max_expansions:
                break

        if not children:
            break
        children.sort(key=lambda t: -t[0])
        if best_open is None or children[0][0] > best_open[0]:
            best_open = (children[0][0], children[0][2])
        frontier = [(c, p) for _, c, p in children[: cfg.beam_width]]
        if expansions >= cfg.max_expansions:
            break

    if best_win is not None:
        return PlanResult(best_win[1], True, best_win[2], expansions,
                          len(best_win[1]), entry_hp=entry_hp)
    # No terminal win found this search, but a provably damage-free turn was:
    # take it rather than a heuristic line that concedes HP.
    if best_safe is not None:
        return PlanResult(best_safe[1], False, best_safe[0], expansions,
                          len(best_safe[1]), entry_hp=entry_hp)
    if best_loss is not None:
        return PlanResult(best_loss[1], False, best_loss[2], expansions,
                          len(best_loss[1]), entry_hp=entry_hp)
    if best_open is not None:
        return PlanResult(best_open[1], False, 0.0, expansions,
                          len(best_open[1]), entry_hp=entry_hp, truncated=True)
    return PlanResult([ACTION_END_TURN], False, 0.0, expansions, 1,
                      entry_hp=entry_hp, truncated=True)


#: Fast profile for TRAINING throughput: single cheap rung, one escalation
#: on loss, tight time budgets. ~1-3s per combat.
TRAIN_LADDER = (
    PlannerConfig(beam_width=4, max_expansions=800, time_budget_s=0.0),
    PlannerConfig(beam_width=16, max_expansions=6_000, time_budget_s=4.0),
)

#: Thorough profile for EVALUATION / real play: full escalation with the
#: min-damage refinement given room to work.
EVAL_LADDER = (
    PlannerConfig(time_budget_s=6.0),
    PlannerConfig(beam_width=32, max_expansions=40_000, time_budget_s=15.0),
    PlannerConfig(beam_width=64, max_expansions=120_000, time_budget_s=30.0),
    # Desperation rung: only reached when every cheaper rung planned a LOSS,
    # so it never slows ordinary fights. Justified by measurement: an A10
    # 49-HP-vs-68 fight lost at beam 64/96 was WON (2 HP left) at beam 160 --
    # deep beams convert real fights, and at the margin those conversions are
    # exactly the ones that decide runs.
    PlannerConfig(beam_width=160, max_expansions=900_000, time_budget_s=120.0),
)


class PlannedCombatController:
    """CombatController that plans each combat once, then replays the queue.

    Determinism makes this sound: the plan was computed on an exact clone,
    so replaying it on the live combat visits the same states. Divergence
    (a queued action illegal in the live state) means the determinism
    assumption broke -- it is counted, logged, and answered by replanning
    from the current live state rather than pressing on blindly.
    """

    def __init__(self, env, config: PlannerConfig | None = None,
                 ladder: tuple[PlannerConfig, ...] | None = (),
                 per_turn: bool = True):
        self._env = env
        #: Plan one turn at a time with the lexicographic objective
        #: (lethal > HP preserved > setup > damage) instead of searching the
        #: whole combat to a terminal. Whole-combat search missed lethal
        #: lines -- a kill several plies deep lost the beam to shallower,
        #: better-looking lines -- and let damage dealt outweigh HP lost.
        self.per_turn = per_turn
        self.turn_plans = 0
        self.turn_lethals = 0
        #: Anti-stall: turn number at the last replan, and how many replans
        #: have happened without the turn advancing.
        self._last_turn: int | None = None
        self._stuck_replans = 0
        self.forced_end_turns = 0
        self.cfg = config or PlannerConfig()
        #: () (default) = use the standard escalation ladder; None = single
        #: budget from ``config``; an explicit tuple = custom ladder.
        self.ladder = (None if ladder is None
                       else (ladder if ladder else None))
        if ladder == ():
            self.ladder = (
                PlannerConfig(),
                PlannerConfig(beam_width=32, max_expansions=40_000),
                PlannerConfig(beam_width=64, max_expansions=120_000),
            )
        self._queue: list[int] = []
        self._combat_key: int | None = None
        self.plans = 0
        self.replans = 0
        self.planned_wins = 0
        self.plan_expansions = 0

    def act(self, obs: np.ndarray, mask: np.ndarray) -> int:
        mgr = self._env._mgr
        combat = mgr.get_combat_state() if mgr is not None else None
        if combat is None:
            legal = np.flatnonzero(np.asarray(mask, dtype=bool))
            return int(legal[0]) if legal.size else 0

        # Anti-stall guard. A truncated plan (budget exhausted before any
        # terminal) can return a short line that fails to advance the fight;
        # the queue then empties, we replan from a nearly identical state,
        # and the cycle repeats. Each iteration costs a full search, so a
        # stalled fight burned ~120 searches (minutes) inside ONE env step
        # and was the dominant cost in planner-based training -- measured
        # throughput was 1.0 fps.
        turn = int(getattr(combat, "turn_count", -1))
        if turn != self._last_turn:
            self._last_turn = turn
            self._stuck_replans = 0
        if self._stuck_replans >= 2:
            # Two replans without the turn advancing: force END TURN so the
            # fight always makes progress. Ending a turn is legal whenever
            # the player is acting, and it is what a stuck line needs.
            m0 = np.asarray(mask, dtype=bool)
            if m0.size > ACTION_END_TURN and m0[ACTION_END_TURN]:
                self._stuck_replans = 0
                self._queue = []
                self.forced_end_turns += 1
                return ACTION_END_TURN

        key = id(combat)
        if key != self._combat_key or not self._queue:
            if self.per_turn:
                # Replan every time the queue empties, which is once per
                # turn: the plan always ends the turn, so exhausting it
                # means a new turn has begun.
                if key != self._combat_key:
                    self._combat_key = key
                    self.plans += 1
                self._last_turn = turn
                self._stuck_replans = 0
                cfg = (self.ladder[0] if self.ladder else self.cfg)
                tp = plan_turn(combat, cfg)
                self.turn_plans += 1
                self.turn_lethals += int(tp.lethal)
                self._queue = list(tp.actions)
                self.plan_expansions += tp.expansions
                action = self._queue.pop(0) if self._queue else ACTION_END_TURN
                m = np.asarray(mask, dtype=bool)
                if not (0 <= action < m.size) or not m[action]:
                    legal = np.flatnonzero(m)
                    action = int(legal[0]) if legal.size else 0
                    self._queue = []
                return int(action)

            if key != self._combat_key:
                self._combat_key = key
                self._last_turn = turn
                self._stuck_replans = 0
                self.plans += 1
                # Fresh combat: full ladder (escalates on loss / bloody win).
                result = (plan_combat_escalating(combat, self.ladder)
                          if self.ladder is not None
                          else plan_combat(combat, self.cfg))
            else:
                # Queue exhausted mid-combat: the previous plan hit its
                # horizon (time/expansion budget) before reaching a terminal.
                # This is receding-horizon continuation from a DEEPER state,
                # not a fresh fight -- the cheap rung suffices, and running
                # the full ladder here was the main throughput sink.
                self.replans += 1
                self._stuck_replans += 1
                cheap = (self.ladder[0] if self.ladder is not None else self.cfg)
                result = plan_combat(combat, cheap)
            self._queue = list(result.actions)
            self.plan_expansions += result.expansions
            if result.won:
                self.planned_wins += 1

        action = self._queue.pop(0) if self._queue else ACTION_END_TURN
        m = np.asarray(mask, dtype=bool)
        if not (0 <= action < m.size) or not m[action]:
            # Divergence from the planned line: replan from the live state.
            self.replans += 1
            result = (plan_combat_escalating(combat, self.ladder)
                      if self.ladder is not None else plan_combat(combat, self.cfg))
            self._queue = list(result.actions)
            action = self._queue.pop(0) if self._queue else ACTION_END_TURN
            if not (0 <= action < m.size) or not m[action]:
                legal = np.flatnonzero(m)
                action = int(legal[0]) if legal.size else 0
                self._queue = []
        return int(action)


# ---------------------------------------------------------------------------
# Per-turn planning with a lexicographic objective
# ---------------------------------------------------------------------------
#
# Whole-combat planning searched to a terminal state and returned one long
# action queue. Two failures showed up in live play: lethal lines were missed
# (a kill several plies deep lost the beam to shallower, higher-heuristic
# lines) and blocking was insufficient (the scalar heuristic let a small HP
# loss be outweighed by damage dealt).
#
# Planning ONE TURN at a time with a strictly ordered objective fixes both.
# Within a turn the branching factor is small enough to search hard, so a
# lethal line is found rather than pruned; and because the objective is
# lexicographic, no amount of damage dealt can ever outrank taking less HP
# loss. Replanning every turn is also what the live bridge needs, since it
# resynchronises with the real game each turn instead of trusting a long
# precomputed queue.


@dataclass
class TurnObjective:
    """Lexicographic score for a finished turn. Higher is better, compared
    strictly in priority order:

    1. ``lethal``       -- every enemy dead, ending the combat.
    2. ``hp_preserved`` -- negative HP lost, measured AFTER the enemies act.
    3. ``setup``        -- scaling cards (powers) committed this turn.
    4. ``damage``       -- enemy HP removed this turn.

    Ordering matters more than weighting here: a tuple comparison makes it
    impossible for (4) to buy its way past (2), which is exactly the failure
    a single weighted sum produced.
    """

    lethal: int = 0
    hp_preserved: float = 0.0
    setup: float = 0.0
    damage: float = 0.0

    def key(self) -> tuple:
        return (self.lethal, self.hp_preserved, self.setup, self.damage)


@dataclass
class TurnPlan:
    actions: list[int]
    objective: TurnObjective
    expansions: int
    lethal: bool = False


def _is_power_card(combat, action: int) -> bool:
    """True when *action* plays a POWER card from hand.

    Powers are the archetypal 'set up to scale' play: they cost tempo now
    for compounding value later, so a turn that lands one is preferred over
    an equal-damage turn that does not.
    """
    from sts2_env.core.enums import CardType
    from sts2_env.gym_env.action_space import action_to_card_and_target, is_potion_action

    if action == ACTION_END_TURN or is_potion_action(action):
        return False
    try:
        idx, _ = action_to_card_and_target(action)
    except Exception:
        return False
    if idx is None:
        return False
    hand = combat.combat_player_states[0].hand
    if idx >= len(hand):
        return False
    return getattr(hand[idx], "card_type", None) == CardType.POWER


def _living_enemy_hp(combat) -> int:
    return sum(e.current_hp for e in combat.enemies if e.is_alive)


def _select_beam(children: list, cfg: PlannerConfig) -> list:
    """Choose which mid-turn nodes survive to the next ply.

    Ranking mid-turn nodes by a single scalar is what made the planner miss
    lethal lines: a line that spends the whole turn setting up a kill looks
    bad to a heuristic that rewards block and current HP, so it was pruned
    several plies before the kill landed.

    Mid-turn the terminal objective cannot be evaluated -- the enemies have
    not acted, so HP loss is unknown. Rather than collapse that uncertainty
    into one number, the beam is SPLIT along the two top priorities so
    neither can starve the other:

    * half ranked by proximity to LETHAL (least enemy HP remaining), which
      preserves kill lines however ugly they look right now;
    * half ranked by the safety heuristic, which preserves the block lines.

    Ties are broken by setup then damage, mirroring priorities 3 and 4.
    """
    half = max(1, cfg.beam_width // 2)

    def lethal_key(t):
        child, _path, setup = t
        return (_living_enemy_hp(child), -setup)

    def safe_key(t):
        child, _path, setup = t
        return (-_heuristic(child, cfg), -setup)

    by_lethal = sorted(children, key=lethal_key)[:half]
    by_safety = sorted(children, key=safe_key)[:half]

    seen_paths: set[tuple] = set()
    beam = []
    for child, path, setup in by_lethal + by_safety:
        key = tuple(path)
        if key in seen_paths:
            continue
        seen_paths.add(key)
        beam.append((child, path, setup))
        if len(beam) >= cfg.beam_width:
            break
    return beam


def plan_turn(root_combat, config: PlannerConfig | None = None) -> TurnPlan:
    """Search this turn only; return the best action sequence for it.

    The returned sequence always ends the turn (or the combat). Scoring
    happens AFTER ``END TURN`` resolves, so ``hp_preserved`` reflects what
    the enemies actually did rather than what they intended.
    """
    cfg = config or PlannerConfig()
    root = clone_combat(root_combat)
    entry_hp = float(max(0, root.primary_player.current_hp))
    entry_enemy_hp = float(_living_enemy_hp(root))
    deadline = (time.monotonic() + cfg.time_budget_s) if cfg.time_budget_s > 0 else None

    # (state, path, setup_score)
    frontier: list[tuple[Any, list[int], float]] = [(root, [], 0.0)]
    seen: set[tuple] = set()
    best: TurnPlan | None = None
    expansions = 0

    for _ in range(cfg.max_depth):
        if not frontier:
            break
        if deadline is not None and time.monotonic() > deadline:
            break
        children: list[tuple[float, Any, list[int], float]] = []

        for state, path, setup in frontier:
            mask = get_action_mask(state).astype(bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                continue
            for action in legal:
                if expansions >= cfg.max_expansions:
                    break
                expansions += 1
                action = int(action)
                is_power = _is_power_card(state, action)
                child = clone_combat(state)
                try:
                    apply_combat_action(child, action)
                except Exception:
                    continue
                new_path = path + [action]
                new_setup = setup + (1.0 if is_power else 0.0)
                cp = child.primary_player
                if cp is None:
                    continue

                ended = action == ACTION_END_TURN or child.is_over or not cp.is_alive
                if ended:
                    alive_hp = _living_enemy_hp(child)
                    obj = TurnObjective(
                        lethal=1 if (alive_hp <= 0 and cp.is_alive) else 0,
                        hp_preserved=-(entry_hp - float(max(0, cp.current_hp))),
                        setup=new_setup,
                        damage=entry_enemy_hp - float(alive_hp),
                    )
                    # A line that kills us is never acceptable while any
                    # alternative survives; rank it below everything.
                    if not cp.is_alive:
                        obj = TurnObjective(-1, -1e9, 0.0, 0.0)
                    if best is None or obj.key() > best.objective.key():
                        best = TurnPlan(new_path, obj, expansions,
                                        lethal=bool(obj.lethal))
                    continue

                sig = _signature(child)
                if sig in seen:
                    continue
                seen.add(sig)
                # Mid-turn ranking only decides what stays in the beam; the
                # real decision is the terminal tuple above.
                children.append((child, new_path, new_setup))
            if expansions >= cfg.max_expansions:
                break

        if best is not None and best.lethal:
            break  # nothing outranks ending the fight
        if not children:
            break
        frontier = _select_beam(children, cfg)
        if expansions >= cfg.max_expansions:
            break

    if best is None:
        best = TurnPlan([ACTION_END_TURN], TurnObjective(), expansions)
    return best
