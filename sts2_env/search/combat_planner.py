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
    #: ``h_player_hp`` now scores SIMULATED post-enemy-turn HP, not current HP
    #: -- see ``use_next_turn_hp`` and ``_post_enemy_turn_hp``.
    h_player_hp: float = 3.0
    #: Rank non-lethal nodes by player/Osty HP AFTER simulating END TURN
    #: instead of by HP before the enemies act.
    #:
    #: The three block terms below were three hand-tuned proxies for one
    #: quantity: how much HP survives the enemy turn. Simulating it measures
    #: that quantity directly and subsumes all of them -- block that absorbs
    #: an attack raises the score, block beyond incoming damage does not (so
    #: over-blocking earns nothing), and block against a buffing enemy earns
    #: nothing. It additionally prices things no intent-reading proxy could
    #: see: killing an enemy removes its attack, and poison/burn/end-of-turn
    #: HP loss, Osty redirect and thorns all land in the simulated HP.
    #:
    #: Cost: one extra clone+apply per non-lethal node evaluated, so roughly
    #: 2x per-node cost. Set False to A/B against the old block proxies.
    use_next_turn_hp: bool = True
    #: Extra penalty when the simulated enemy turn KILLS the player, on top of
    #: the 0 HP that already scores.
    #:
    #: DEFAULT 0.0, and a large value is actively harmful -- measured. A
    #: mid-turn probe asks "what if I stop playing right now", so a node one
    #: card short of lethal reports death; at 100_000 that node was removed
    #: from the beam outright, which is exactly the kill-line pruning
    #: ``_select_beam`` exists to prevent. Leaving it at 0.0 lets such a node
    #: keep its ``h_enemy_hp`` credit and stay in contention.
    death_penalty: float = 0.0
    #: DISABLED (0.0): superseded by ``use_next_turn_hp``. Kept as knobs so
    #: the old behaviour can be restored without a code change -- they are
    #: only evaluated when at least one is non-zero.
    h_block: float = 0.0            # was 0.6:  block up to incoming damage
    h_wasted_block: float = 0.0     # was 0.35: penalty for over-blocking
    h_enemy_hp: float = -2.0
    h_osty_hp: float = 0.9
    #: A turn that ends with every enemy dead takes zero further damage and
    #: ends the fight; nothing else in the heuristic is comparable.
    lethal_bonus: float = 400.0
    #: DISABLED (0.0): "block >= incoming" is exactly the case simulated HP
    #: already reports as zero HP lost, so the threshold bonus is redundant.
    safe_turn_bonus: float = 0.0    # was 120.0
    #: Stop expanding once this many wins exist overall AND the best of them
    #: is within ``damage_tolerance`` of a damage-free clear. A win found
    #: early is usually a fast-kill line, not the cleanest one, so the search
    #: keeps refining until a low-damage win exists or the beam collapses.
    enough_wins: int = 8
    #: Damage (HP lost) considered "clean enough" to stop searching/escalating.
    damage_tolerance: float = 0.0
    #: Once a win exists, beam nodes whose current HP cannot beat the best
    #: win's final HP (plus this slack) are pruned -- they can only produce
    #: strictly worse wins.
    #:
    #: 0.0 asserts that in-combat healing is IMPOSSIBLE, which makes the bound
    #: ``final_hp <= current_hp`` exact and the prune fully admissible -- the
    #: strongest sound setting. Any line that CAN heal more than this breaks
    #: admissibility: the prune may discard a node that would have produced a
    #: strictly better win, and in ``plan_combat`` it may additionally end the
    #: search early (the frontier filter can empty the frontier). Raise it to
    #: the maximum recoverable HP if healing is ever reintroduced.
    prune_margin: float = 0.0
    #: Wall-clock soft cap per plan_combat call. Search returns the best
    #: result found so far when it expires; 0 disables. Keeps the min-damage
    #: refinement from spending minutes polishing a single fight.
    time_budget_s: float = 8.0

    #: EXHAUSTIVE WITHIN A TURN, BEAM BETWEEN TURNS.
    #:
    #: Default False preserves the existing behaviour exactly (beam at every
    #: ply). With it on, every mid-turn node survives to the next ply and the
    #: beam is applied ONLY across turn boundaries.
    #:
    #: This is the mode the search actually wants. plan_combat expands one
    #: ACTION per level, so at beam_width=12 a single turn's card plays
    #: already overflow the beam and lethal lines are pruned *within* the turn
    #: -- several plies before the kill lands. Intra-turn branching is small
    #: enough to enumerate: energy caps the number of plays (typically ~3),
    #: and _signature collapses the many orderings that transpose.
    #:
    #: Bounded three ways so it cannot run away: time_budget_s, max_expansions
    #: and exhaustive_max_frontier below.
    exhaustive_within_turn: bool = False
    #: Safety valve for the mode above. If the mid-turn frontier exceeds this,
    #: fall back to beam selection for that ply rather than growing without
    #: limit -- an unusual deck (0-cost draw loops, many card generators) can
    #: still blow up an "exhaustive" turn.
    exhaustive_max_frontier: int = 20_000

    # ---- TEMPORARY: revert by deleting this __post_init__ -----------------
    # Neutralises max_expansions and enough_wins instead of removing the
    # fields. The fields must stay declared because TRAIN_LADDER/EVAL_LADDER
    # pass max_expansions= as a keyword at module scope, so deleting them
    # breaks import. Setting them here overrides every construction path,
    # including those explicit kwargs.
    #
    # With both neutralised the ONLY remaining bounds are beam_width,
    # max_depth and time_budget_s. Note TRAIN_LADDER[0] sets
    # time_budget_s=0.0, which disables the deadline entirely -- that rung is
    # now bounded only by max_depth x beam_width x legal_actions.
    def __post_init__(self) -> None:
        self.max_expansions = 1 << 62   # was: 8_000 / 40_000 / 120_000 / 900_000
        self.enough_wins = 1 << 62      # was: 8 -- early stop now unreachable
    # ---- end TEMPORARY ---------------------------------------------------


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


def _powers_key(creature) -> tuple:
    """Power ids WITH amounts. Strength 2 and Strength 6 are not the same
    state; the old signature kept amounts for the player but reduced enemies
    to ``len(powers)``, so enemy scaling was invisible to dedup."""
    return tuple(sorted(
        (str(pid), int(getattr(inst, "amount", 0) or 0))
        for pid, inst in (getattr(creature, "powers", {}) or {}).items()
    ))


def _pile_key(cards) -> tuple:
    """Pile CONTENTS IN ORDER.

    The old signature stored only len(draw)/len(discard)/len(exhaust). Draw
    order fully determines every future hand, and the post-reshuffle draw
    order is a function of the discard pile's order -- which is the card-play
    order. So the exact case the module docstring calls a safe transposition
    ("playing Strike A before Strike B") produced identical signatures with
    divergent futures, and one of the two was silently discarded.
    """
    return tuple((str(c.card_id), bool(getattr(c, "upgraded", False)))
                 for c in cards)


def _enemy_move_key(combat, enemy) -> tuple | None:
    """The enemy's currently-rolled move and the intents it implies.

    Two states whose enemies intend different things are not interchangeable:
    the player sees different intents, blocks differently, and the observation
    differs. None of that was in the old signature.
    """
    ais = getattr(combat, "enemy_ais", None) or {}
    ai = ais.get(getattr(enemy, "combat_id", None))
    move = getattr(ai, "current_move", None)
    if move is None:
        return None
    move_id = (getattr(move, "move_id", None) or getattr(move, "id", None)
               or type(move).__name__)
    intents = tuple(
        (str(getattr(i, "intent_type", "")), int(getattr(i, "damage", 0) or 0),
         int(getattr(i, "hits", 1) or 1))
        for i in (getattr(move, "intents", None) or [])
    )
    return (str(move_id), intents)


def _signature(combat) -> tuple:
    """Transposition key. Two states with equal signatures are treated as
    interchangeable, so anything the search can act on MUST appear here --
    an omission silently deletes one of two genuinely different states.

    Six omissions were confirmed by audit and are fixed here:

    * **Pending choices.** Toggling an option in a multi-select prompt mutates
      only ``pending_choice.selected_indices``. Nothing else moves, so every
      toggle child hashed identically to its parent, was found in ``seen``,
      and was dropped -- leaving ``children`` empty and breaking the beam
      loop. The planner could not resolve a forced discard/exhaust at all and
      returned a bare ``[END TURN]``, which is not even legal while a choice
      is open. Triggers on DREDGE and on any enemy-forced discard.
    * **Enemy identity.** Keyed by ``monster_id`` and filtered to living
      enemies, so in any same-species pair (two Nibbits, three Inklets)
      "kill the front one" and "kill the back one" collapsed together --
      despite different intents, different survivors and different action
      indices. Now keyed by ``combat_id`` with dead enemies retained so slot
      alignment survives.
    * **Pile order**, **enemy power amounts**, **enemy AI move**, and
      **Osty** -- see the helpers above and the fields below.
    """
    p = combat.primary_player
    state = combat.combat_player_states[0]

    enemies = tuple(
        (str(getattr(e, "combat_id", "")), str(e.monster_id), bool(e.is_alive),
         int(e.current_hp), int(e.block), _powers_key(e),
         _enemy_move_key(combat, e))
        for e in combat.enemies
    )

    # Osty is a real combatant: the damage pipeline redirects to it and the
    # heuristic scores its HP, yet it was absent from the key entirely.
    osty = combat.get_osty(p)
    osty_key = None if osty is None else (
        bool(osty.is_alive), int(osty.current_hp),
        int(getattr(osty, "max_hp", 0) or 0), _powers_key(osty),
    )

    pc = getattr(combat, "pending_choice", None)
    choice_key = None if pc is None else (
        str(getattr(pc, "prompt", "")),
        int(getattr(pc, "num_options", 0) or 0),
        bool(getattr(pc, "is_multi", False)),
        tuple(sorted(getattr(pc, "selected_indices", ()) or ())),
    )

    # Potions are scored by _terminal_score (potion_bonus), so a state that
    # drank one must not collapse with one that did not.
    potions = tuple(
        None if x is None else str(getattr(x, "potion_id", x))
        for x in (getattr(state, "potions", ()) or ())
    )

    return (
        int(p.current_hp), int(p.block), int(state.energy),
        int(combat.turn_count),
        _pile_key(state.hand), _pile_key(state.draw),
        _pile_key(state.discard), _pile_key(state.exhaust),
        enemies, _powers_key(p), osty_key, choice_key, potions,
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
            # Intent.is_attack covers only ATTACK and MULTI_ATTACK
            # (sts2_env/monsters/intents.py). DEATH_BLOW is a damaging intent
            # it excludes, so every DEATH_BLOW move read as ZERO incoming
            # damage -- including an Act-4 boss whose intent damage field is
            # literally 0 and whose real damage is computed at resolution.
            kind = getattr(getattr(intent, "intent_type", None), "name", "")
            if getattr(intent, "is_attack", False) or kind == "DEATH_BLOW":
                total += int(getattr(intent, "total_damage", 0) or 0)
    return total


def _live_hp(combat) -> tuple[float, float]:
    """(player_hp, osty_hp) as they stand right now, Osty 0 when dead."""
    p = combat.primary_player
    osty = combat.get_osty(p)
    return (
        float(max(0, p.current_hp)) if p is not None else 0.0,
        float(max(0, osty.current_hp))
        if (osty is not None and osty.is_alive) else 0.0,
    )


def _post_enemy_turn_hp(combat) -> tuple[float, float, bool]:
    """Simulate END TURN from *combat*; report HP AFTER the enemies act.

    This is the quantity the three block terms were proxying for, measured
    instead of approximated. It prices, with no special-casing:

    * block that absorbs an attack        -> HP retained, scored
    * block beyond the incoming attack    -> no HP gain, so over-blocking
                                             earns nothing
    * block against a buffing enemy       -> no HP gain, so it is not played
    * killing an enemy                    -> that attack never lands
    * poison, burn, end-of-turn HP loss, Osty redirect, thorns -> all
      included, and NONE of them were visible to the intent-reading proxy

    Returns ``(player_hp, osty_hp, died)``.

    Two caveats, both deliberate:

    1. ``combat`` is itself already a search clone, so the probe clone never
       touches live state and its RNG advance is discarded with it.
    2. At a MID-TURN node this answers "what if I stopped playing and ended
       the turn right now", which is a lower bound on the turn's outcome. It
       therefore slightly favours a node that has already played its defence
       over a sibling still holding it with energy to spare. Siblings at the
       same depth have played the same number of cards, so the bias is close
       to uniform within a beam level; a conservative lower bound is also the
       right direction of error for a min-HP-loss objective.
    """
    cur_hp, cur_osty = _live_hp(combat)
    before_turn = int(getattr(combat, "turn_count", 0))

    probe = clone_combat(combat)
    try:
        apply_combat_action(probe, ACTION_END_TURN)
    except Exception:
        # END TURN not applicable here (forced card-select prompt, etc.).
        return cur_hp, cur_osty, False
    if int(getattr(probe, "turn_count", 0)) <= before_turn and not probe.is_over:
        # Applying it did not actually advance the turn, so the simulation
        # says nothing; fall back to the pre-turn values.
        return cur_hp, cur_osty, False

    pp = probe.primary_player
    if pp is None or not pp.is_alive:
        return 0.0, 0.0, True
    hp, osty_hp = _live_hp(probe)
    return hp, osty_hp, False


def _heuristic(combat, cfg: PlannerConfig, ended_turn: bool = False) -> float:
    """Rank non-terminal states for beam retention.

    Every node is scored in the SAME units -- "HP once the enemies have had
    their next go" -- charged exactly once. Which is why ``ended_turn``
    matters, and why it is not optional in practice:

    * ``enemy_hp <= 0`` (lethal): no enemy turn will ever follow, so current
      HP *is* the final HP. Score it directly, plus ``lethal_bonus``.
    * ``ended_turn=True``: this node was reached BY an END TURN, and
      ``apply_combat_action`` resolves the enemy turn as part of it -- so
      ``current_hp`` already includes the attack. Probing again would charge a
      SECOND enemy turn, making boundary nodes look worse than their mid-turn
      siblings and biasing the beam against ever ending a turn. Measured: with
      that double charge the search stopped reaching terminals at all (4 of 6
      seeds returned the truncated ``best_open`` fallback).
    * otherwise (mid-turn): ``current_hp`` has not paid for the incoming
      attack yet, so simulate END TURN to price it.

    The point of all this is that current HP is not survivability: a node at
    40 HP facing 30 unblocked damage is worse than a node at 30 HP facing
    nothing, and a current-HP term ranks them the wrong way round. The three
    block proxies existed to patch that; simulating measures it instead.
    """
    p = combat.primary_player
    enemy_hp = sum(e.current_hp for e in combat.enemies if e.is_alive)

    if enemy_hp <= 0:
        # LETHAL: current HP is the final HP; no enemy turn will follow.
        hp, osty_hp = _live_hp(combat)
        return (cfg.h_player_hp * hp
                + cfg.h_osty_hp * osty_hp
                + cfg.lethal_bonus)

    if cfg.use_next_turn_hp and not ended_turn:
        hp, osty_hp, died = _post_enemy_turn_hp(combat)
        if died:
            hp = osty_hp = 0.0
            # NOT a hard prune -- see PlannerConfig.death_penalty. The node
            # keeps its h_enemy_hp credit so a near-lethal line survives.
    else:
        hp, osty_hp = _live_hp(combat)
        died = False

    score = (cfg.h_player_hp * hp
             + cfg.h_enemy_hp * enemy_hp
             + cfg.h_osty_hp * osty_hp)
    if died:
        score -= cfg.death_penalty

    # Legacy block proxies, default 0.0 and skipped entirely unless one is
    # re-enabled -- so the common path pays no incoming_damage() call.
    if cfg.h_block or cfg.h_wasted_block or cfg.safe_turn_bonus:
        incoming = incoming_damage(combat)
        score += cfg.h_block * min(p.block, incoming)
        score -= cfg.h_wasted_block * max(0, p.block - incoming)
        if incoming > 0 and p.block >= incoming:
            score += cfg.safe_turn_bonus
    return score


def _terminal_score(combat, cfg: PlannerConfig) -> tuple[bool, float]:
    """(won, objective score) for a finished combat.

    Losses are RANKED, not lumped. Every loss used to return exactly
    ``-win_bonus``, so ``best_loss`` could never be improved on and the
    planner kept the FIRST death it happened to enumerate -- the "fights the
    search cannot win still need a least-bad action sequence" promise in the
    module docstring was not implemented at all.

    A least-bad line is one that got furthest: enemy HP removed is the only
    progress measure available at a losing terminal, and it is what a
    follow-up turn (or a differently-seeded retry) would build on.
    """
    p = combat.primary_player
    won = bool(p is not None and p.is_alive)
    if not won:
        enemy_hp = sum(max(0, e.current_hp) for e in combat.enemies if e.is_alive)
        # Strictly below any win, ordered among themselves by damage done:
        # LESS enemy HP left scores higher. Written as an explicit subtraction
        # rather than via h_enemy_hp, whose sign convention is negative and
        # would silently invert this.
        return False, -cfg.win_bonus - float(enemy_hp)
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
            improved = best_win is None or last.final_hp > best_win.final_hp
            if improved:
                best_win = last
            damage = last.entry_hp - last.final_hp
            if damage <= cfg.damage_tolerance:
                return best_win
            # Bloody win: keep climbing, but ONLY WHILE THE EXTRA BUDGET IS
            # BUYING SOMETHING.
            #
            # This used to run exactly one extra rung and then return, so on a
            # bloody rung-0 win the two deepest EVAL rungs (beam 64, and the
            # beam-160 desperation rung) were unreachable -- the budgets the
            # ladder's own comments document as converting fights.
            #
            # Climbing unconditionally is just as wrong in the other
            # direction: with damage_tolerance = 0.0 every win that costs even
            # 1 HP is "bloody", so EVERY fight would walk the whole ladder --
            # 6+15+30+120s of EVAL budget on fights that were already won. The
            # improvement test keeps the deep rungs available for fights where
            # they help while stopping the moment a rung adds nothing.
            if i > 0 and not improved:
                break
            continue
    return best_win if best_win is not None else last


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

    # Zero-damage turn tracking: record the best line that provably costs no
    # HP over the turn it ends, so it can be chosen outright when the search
    # finds no win.
    #
    # The old test was `cp.block >= root_incoming`, evaluated on the child of
    # an END TURN. Two things made it near-useless. Block is CLEARED when the
    # turn resolves, so cp.block is ~0 by the time it is read -- the test
    # therefore reduced to `0 >= root_incoming`, i.e. "the ROOT turn had no
    # attack intents", which is a fact about the root and not about this line
    # at all. And root_incoming is captured once, so on any turn past the
    # first it was stale anyway.
    #
    # The correct test needs no intent reading: END TURN resolves the enemy
    # turn, so the child's HP already includes the attack. A turn cost nothing
    # exactly when the child's HP is not below the PARENT's HP.
    best_safe: tuple[tuple[float, float], list[int]] | None = None

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
                    # PREFER SHORTER LINES ON A TIE.
                    #
                    # Strictly a tie-break: it sits BELOW score, so brevity can
                    # never buy a worse outcome -- no HP is traded for it. Among
                    # equally good plans a shorter action sequence is better on
                    # two counts: fewer actions replayed against the live game
                    # means less exposure to sim/client divergence (measured at
                    # ~62% of turn boundaries), and a shorter line reaches the
                    # same result in fewer decisions.
                    if won:
                        if (best_win is None or score > best_win[0]
                                or (score == best_win[0]
                                    and len(new_path) < len(best_win[1]))):
                            best_win = (score, new_path, hp)
                        wins_found += 1
                    else:
                        if (best_loss is None or score > best_loss[0]
                                or (score == best_loss[0]
                                    and len(new_path) < len(best_loss[1]))):
                            best_loss = (score, new_path, hp)
                    continue
                # A zero-damage turn, measured rather than predicted: END TURN
                # has already resolved the enemy turn on `child`, so the turn
                # cost nothing exactly when the child's HP did not drop below
                # the PARENT's. No intent reading, no block inspection, and
                # correct on every turn -- not just the root's.
                if int(action) == ACTION_END_TURN and cp is not None:
                    pp = state.primary_player
                    hp_before = float(max(0, pp.current_hp)) if pp is not None else 0.0
                    if float(max(0, cp.current_hp)) >= hp_before:
                        alive_hp = sum(max(0, e.current_hp)
                                       for e in child.enemies if e.is_alive)
                        # Rank damage-free turns by HP kept, then by PROGRESS.
                        # Ranking on HP alone let the shallowest damage-free
                        # line win -- typically an immediate END TURN that
                        # achieves nothing, which is how plan_combat came to
                        # return a bare [END TURN] on roughly one in eight A10
                        # turn-openings.
                        cand = ((float(cp.current_hp), -float(alive_hp)), new_path)
                        if best_safe is None or cand[0] > best_safe[0]:
                            best_safe = cand

                sig = _signature(child)
                if sig in seen:
                    continue
                seen.add(sig)
                ended_turn = int(action) == ACTION_END_TURN
                h = _heuristic(child, cfg, ended_turn=ended_turn)
                children.append((h, child, new_path, ended_turn))
            if expansions >= cfg.max_expansions:
                break

        if not children:
            break
        by_score = sorted(children, key=lambda t: -t[0])
        if best_open is None or by_score[0][0] > best_open[0]:
            best_open = (by_score[0][0], by_score[0][2])

        if cfg.exhaustive_within_turn:
            # Keep EVERY mid-turn node; beam only across turn boundaries.
            mid = [c for c in children if not c[3]]
            boundary = [c for c in children if c[3]]
            if len(mid) <= cfg.exhaustive_max_frontier:
                kept_boundary = sorted(boundary, key=lambda t: -t[0])
                kept_boundary = kept_boundary[: cfg.beam_width]
                frontier = [(c, p) for _h, c, p, _e in (mid + kept_boundary)]
                if expansions >= cfg.max_expansions:
                    break
                continue
            logger.debug(
                "exhaustive within-turn frontier hit the cap (%d > %d) -- "
                "falling back to beam selection for this ply",
                len(mid), cfg.exhaustive_max_frontier)

        # SPLIT BEAM. Ranking by the single scalar heuristic is precisely the
        # failure _select_beam was written to prevent: a line that spends a
        # whole turn setting up a kill looks bad to a heuristic that rewards
        # HP, so it is pruned several plies before the kill lands. plan_combat
        # is the ONLY search the eval path reaches, and it never used
        # _select_beam -- so the production path still contained the bug the
        # fix was written for.
        #
        # Split here rather than delegating: _select_beam recomputes
        # _heuristic, and with use_next_turn_hp each recomputation costs a
        # clone+apply. These scores are already in hand.
        half = max(1, cfg.beam_width // 2)
        by_lethal = sorted(children, key=lambda t: _living_enemy_hp(t[1]))
        picked: list = []
        seen_paths: set[tuple] = set()
        for cand in list(by_lethal[:half]) + list(by_score):
            key = tuple(cand[2])
            if key in seen_paths:
                continue
            seen_paths.add(key)
            picked.append(cand)
            if len(picked) >= cfg.beam_width:
                break
        frontier = [(c, p) for _h, c, p, _e in picked]
        if expansions >= cfg.max_expansions:
            break

    if best_win is not None:
        return PlanResult(best_win[1], True, best_win[2], expansions,
                          len(best_win[1]), entry_hp=entry_hp)
    # No terminal win found this search, but a provably damage-free turn was:
    # take it rather than a heuristic line that concedes HP.
    if best_safe is not None:
        return PlanResult(best_safe[1], False, best_safe[0][0], expansions,
                          len(best_safe[1]), entry_hp=entry_hp)
    # ALIVE BEATS DEAD. best_loss is a line that provably ends in death;
    # best_open is one that is merely unfinished at the horizon. Returning the
    # death line first meant the planner would walk into a loss in preference
    # to a surviving line whenever no win was found -- and since all deaths
    # used to score identically, it was not even the least-bad death.
    if best_open is not None:
        return PlanResult(best_open[1], False, 0.0, expansions,
                          len(best_open[1]), entry_hp=entry_hp, truncated=True)
    if best_loss is not None:
        return PlanResult(best_loss[1], False, best_loss[2], expansions,
                          len(best_loss[1]), entry_hp=entry_hp)
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
                 per_turn: bool = False):
        self._env = env
        #: Per-turn planning is now OPT-IN. A turn-local objective cannot
        #: see the four things that decide fights: killing an enemy removes
        #: its future attacks, Vulnerable applied early pays off over later
        #: turns, correct blocking depends on what is coming after this
        #: turn, and lethal often needs a setup turn first. Whole-combat
        #: min-HP-loss captures all four without special-casing any of them
        #: -- a dead enemy simply stops dealing damage in the search.
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
    #: Number of actions in the line. Enters key() as -length, i.e. LAST and
    #: negated, so a shorter sequence wins between otherwise-equal lines.
    length: int = 0

    def key(self) -> tuple:
        """Exact HP, no bucketing.

        Bucketing existed to stop a PER-TURN planner from turtling: with a
        turn-local objective, conceding 1 HP to deal 20 looked strictly
        worse than blocking everything for 0. That tradeoff is an artifact
        of optimising one turn in isolation. Optimising the WHOLE combat
        removes it -- spending HP now to end the fight a turn earlier shows
        up as less total HP lost, so exact comparison is correct and the
        bucket only blurred real differences.

        ``-length`` is the LAST term, so between two otherwise-equal lines --
        most importantly between two LETHAL lines -- the shorter action
        sequence wins. It sits below hp_preserved so brevity can never buy a
        worse outcome, and fewer replayed actions means less exposure to
        sim/client divergence.
        """
        return (self.lethal, self.hp_preserved, self.setup, self.damage,
                -self.length)


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
        # Derive the turn-boundary flag from the last action on the path so
        # boundary nodes are not charged twice for the enemy turn.
        ended = bool(_path) and int(_path[-1]) == ACTION_END_TURN
        return (-_heuristic(child, cfg, ended_turn=ended), -setup)

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
                # Enforce the deadline mid-level too -- see plan_combat_min_hp.
                if (deadline is not None and expansions % 64 == 0
                        and time.monotonic() > deadline):
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
                        length=len(new_path),
                    )
                    # A line that kills us is never acceptable while any
                    # alternative survives; rank it below everything.
                    if not cp.is_alive:
                        obj = TurnObjective(-1, -1e9, 0.0, 0.0, len(new_path))
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


# ---------------------------------------------------------------------------
# Whole-combat search for the minimum-HP-loss line
# ---------------------------------------------------------------------------
#
# plan_turn optimises ONE turn at a time. That is the right default against the
# live client, because a per-turn plan resynchronises with the real game every
# turn. But a turn-greedy line can be globally worse: spending block now to
# take zero damage can cost the tempo that would have ended the fight a turn
# earlier, and every extra turn is another enemy attack.
#
# plan_combat_min_hp searches the WHOLE combat for the line that finishes it
# with the most HP intact. It is only sound when the enemy AI state is known --
# otherwise turns past the first are planned against a freshly-rolled move
# rather than the real one -- so it refuses to run unless told the state was
# restored.
#
# "Exhaustive" is bounded by construction. Combat branching is large enough
# that true exhaustion is not reachable, so this is a wide beam with
# transposition dedup and an admissible-ish prune: any line already down more
# HP than the best completed win is abandoned, since HP never comes back
# except through healing, which the prune margin covers.


@dataclass
class CombatPlan:
    """A whole-combat line."""

    actions: list[int]
    won: bool
    entry_hp: float
    final_hp: float
    turns: int
    expansions: int
    exhausted: bool = False   # True when the search space was fully covered

    @property
    def hp_lost(self) -> float:
        return max(0.0, self.entry_hp - self.final_hp)


def plan_combat_min_hp(
    root_combat,
    config: PlannerConfig | None = None,
    ai_state_known: bool = True,
    max_turns: int = 30,
) -> CombatPlan | None:
    """Search the whole combat for the line that ends it with the most HP.

    Returns None when ``ai_state_known`` is False: planning multiple turns
    ahead without the enemy's real move state produces a confident plan for
    a fight that is not the one being played, which is worse than declining.
    """
    if not ai_state_known:
        logger.warning("whole-combat planning declined: enemy AI state unknown, "
                       "so turns past the first would be planned against a "
                       "freshly-rolled move rather than the real one")
        return None

    cfg = config or PlannerConfig(beam_width=48, max_expansions=400_000,
                                  time_budget_s=30.0)
    root = clone_combat(root_combat)
    entry_hp = float(max(0, root.primary_player.current_hp))
    deadline = (time.monotonic() + cfg.time_budget_s) if cfg.time_budget_s > 0 else None

    frontier: list[tuple[Any, list[int]]] = [(root, [])]
    seen: set[tuple] = {_signature(root)}
    best: CombatPlan | None = None
    expansions = 0
    truncated_by_budget = False

    for _depth in range(cfg.max_depth * max(1, max_turns // 4)):
        if not frontier:
            break
        if deadline is not None and time.monotonic() > deadline:
            truncated_by_budget = True
            break

        children: list[tuple[float, Any, list[int]]] = []
        for state, path in frontier:
            mask = get_action_mask(state).astype(bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                continue
            for action in legal:
                if expansions >= cfg.max_expansions:
                    truncated_by_budget = True
                    break
                # The deadline MUST be enforced mid-level, not only at the top
                # of the depth loop. The bridge plans at beam_width=512, so one
                # level is ~10k clone+applies -- long enough on its own to blow
                # a 90s budget, and the mod's AutoSlay watchdog does not slow a
                # late run, it ENDS it at 120s. Checking only between levels
                # made time_budget_s unenforceable exactly where it matters.
                if (deadline is not None and expansions % 64 == 0
                        and time.monotonic() > deadline):
                    truncated_by_budget = True
                    break
                expansions += 1
                child = clone_combat(state)
                try:
                    apply_combat_action(child, int(action))
                except Exception:
                    continue
                new_path = path + [int(action)]
                cp = child.primary_player
                if cp is None:
                    continue

                if child.is_over or not cp.is_alive:
                    if cp.is_alive:
                        cand = CombatPlan(
                            actions=new_path, won=True, entry_hp=entry_hp,
                            final_hp=float(max(0, cp.current_hp)),
                            turns=int(getattr(child, "turn_count", 0)),
                            expansions=expansions,
                        )
                        # Tie-break on line length -- see plan_combat. Below
                        # final_hp, so brevity never costs HP.
                        if (best is None or cand.final_hp > best.final_hp
                                or (cand.final_hp == best.final_hp
                                    and len(cand.actions) < len(best.actions))):
                            best = cand
                    continue

                if int(getattr(child, "turn_count", 0)) > max_turns:
                    continue
                # Prune lines that cannot beat the best completed win. HP is
                # only recoverable through in-combat healing, which prune_margin
                # allows for.
                if best is not None and cp.current_hp + cfg.prune_margin <= best.final_hp:
                    continue

                sig = _signature(child)
                if sig in seen:
                    continue
                seen.add(sig)
                children.append((_heuristic(child, cfg,
                                            ended_turn=int(action) == ACTION_END_TURN),
                                 child, new_path))
            if expansions >= cfg.max_expansions:
                break

        if not children:
            break
        children.sort(key=lambda t: -t[0])
        frontier = [(c, p) for _, c, p in children[: cfg.beam_width]]

    if best is not None:
        best.exhausted = not truncated_by_budget and not frontier
        best.expansions = expansions
    return best


def plan_turn_lookahead(
    root_combat,
    config: PlannerConfig | None = None,
    horizon_turns: int = 3,
    ai_state_known: bool = True,
) -> TurnPlan:
    """Search ``horizon_turns`` ahead, minimising HP lost; return THIS turn.

    Receding horizon, and it is the right shape for this problem.

    A one-turn objective cannot see the four things that decide fights:
    killing an enemy removes its future attacks, Vulnerable applied now pays
    off on later turns, correct blocking depends on what is coming after
    this turn, and lethal often needs a setup turn first. All four need
    lookahead.

    But planning the ENTIRE combat once is worse in practice, measured: the
    beam that fits in budget is too shallow to solve a whole fight, and a
    single stale plan loses to per-turn replanning that re-searches from the
    true state each turn (4/6 wins and 141 HP lost vs 132 for per-turn over
    six seeded combats).

    Looking a few turns ahead and replanning each turn keeps the multi-turn
    signal while retaining fresh information. HP is compared EXACTLY -- no
    bucketing -- because over a horizon, conceding HP to end the fight
    sooner shows up as less total HP lost rather than as a turn-local
    tradeoff the bucket had to paper over.
    """
    cfg = config or PlannerConfig(beam_width=32, max_expansions=150_000,
                                  time_budget_s=10.0)
    if not ai_state_known:
        return plan_turn(root_combat, cfg)

    root = clone_combat(root_combat)
    entry_hp = float(max(0, root.primary_player.current_hp))
    start_turn = int(getattr(root, "turn_count", 0))
    deadline = (time.monotonic() + cfg.time_budget_s) if cfg.time_budget_s > 0 else None

    # (state, full path, actions belonging to THIS turn, powers played)
    frontier: list[tuple[Any, list[int], list[int], float]] = [(root, [], [], 0.0)]
    seen: set[tuple] = set()
    best: tuple[tuple, list[int]] | None = None   # (score key, this-turn actions)
    expansions = 0

    def score(state, setup: float) -> tuple:
        p = state.primary_player
        alive = _living_enemy_hp(state)
        won = alive <= 0 and p is not None and p.is_alive
        dead = p is None or not p.is_alive
        if dead:
            return (-1, -1e9, 0.0, 0.0)
        hp_lost = entry_hp - float(max(0, p.current_hp))
        # Won > HP preserved (exact) > setup > enemy HP removed.
        return (1 if won else 0, -hp_lost, setup, -float(alive))

    for _ in range(cfg.max_depth):
        if not frontier:
            break
        if deadline is not None and time.monotonic() > deadline:
            break
        children: list[tuple[float, Any, list[int], list[int], float]] = []

        for state, path, this_turn, setup in frontier:
            mask = get_action_mask(state).astype(bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                continue
            for action in legal:
                if expansions >= cfg.max_expansions:
                    break
                # Enforce the deadline mid-level too -- see plan_combat_min_hp.
                if (deadline is not None and expansions % 64 == 0
                        and time.monotonic() > deadline):
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
                # Only actions before the FIRST end-of-turn belong to this turn.
                still_this_turn = int(getattr(state, "turn_count", 0)) == start_turn
                new_this = this_turn + [action] if still_this_turn else this_turn
                new_setup = setup + (1.0 if is_power else 0.0)
                cp = child.primary_player
                if cp is None:
                    continue

                turns_done = int(getattr(child, "turn_count", 0)) - start_turn
                terminal = child.is_over or not cp.is_alive
                if terminal or turns_done >= horizon_turns:
                    k = score(child, new_setup)
                    if best is None or k > best[0]:
                        best = (k, new_this or [ACTION_END_TURN])
                    continue

                sig = (_signature(child), turns_done)
                if sig in seen:
                    continue
                seen.add(sig)
                children.append((_heuristic(child, cfg,
                                            ended_turn=action == ACTION_END_TURN),
                                 child, new_path, new_this, new_setup))
            if expansions >= cfg.max_expansions:
                break

        if not children:
            break
        # Same split-beam rationale as plan_turn: ranking only by a safety
        # heuristic prunes kill lines before the kill lands.
        trimmed = _select_beam([(c, p, s) for _, c, p, _t, s in children], cfg)
        keep = {tuple(p) for _c, p, _s in trimmed}
        frontier = [(c, p, t, s) for _h, c, p, t, s in children if tuple(p) in keep]
        frontier = frontier[: cfg.beam_width]

    if best is None:
        return TurnPlan([ACTION_END_TURN], TurnObjective(), expansions)
    k, actions = best
    return TurnPlan(actions,
                    TurnObjective(lethal=max(0, k[0]), hp_preserved=k[1],
                                  setup=k[2], damage=-k[3],
                                  length=len(actions)),
                    expansions, lethal=k[0] == 1)
