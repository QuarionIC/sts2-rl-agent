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
    h_block: float = 0.6
    h_enemy_hp: float = -2.0
    h_osty_hp: float = 0.9
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


def _heuristic(combat, cfg: PlannerConfig) -> float:
    """Rank non-terminal states for beam retention."""
    p = combat.primary_player
    enemy_hp = sum(e.current_hp for e in combat.enemies if e.is_alive)
    osty = combat.get_osty(p)
    osty_hp = osty.current_hp if (osty is not None and osty.is_alive) else 0
    return (
        cfg.h_player_hp * p.current_hp
        + cfg.h_block * p.block
        + cfg.h_enemy_hp * enemy_hp
        + cfg.h_osty_hp * osty_hp
    )


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
                 ladder: tuple[PlannerConfig, ...] | None = ()):
        self._env = env
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
