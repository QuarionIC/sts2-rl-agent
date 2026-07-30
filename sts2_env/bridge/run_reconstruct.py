"""Rebuild run-level state from a bridge payload and encode it for the RL run agent.

Why this module exists
----------------------
Every trained hierarchical run agent consumes the 4778-dim RICH observation
produced by :meth:`RichObservationEncoder.encode_run`, which reads a live
``RunManager``. The bridge's :class:`RunStateAdapter` emits a different,
151-dim vector, so ``detect_model_mode`` rejects every current checkpoint
outright -- the run agents were unusable in the live game.

This module closes that gap the same way :mod:`combat_reconstruct` closed it
for the combat agent: rebuild REAL simulator objects from the wire payload and
hand those to the same encoder the model trained against. Reconstruction is
load-bearing; a hand-ported vector would drift from the encoder silently.

What "real objects" buys us
---------------------------
A duck-typed shim would satisfy the encoder's attribute reads while quietly
differing in the details that matter -- ``CardInstance.tags`` is declared
``frozenset[str]`` but is actually ``frozenset[CardTag]`` after
``__post_init__`` runs, and the archetype scalars are computed from those tags.
Constructing genuine :class:`RunState` / :class:`PlayerState` / :class:`ActMap`
objects means the encoder cannot tell the difference between a reconstructed
run and a simulated one.

What the wire cannot supply
---------------------------
Two things are absent from the payload and are therefore approximated. Both
are logged once so they never become invisible:

``rs.acts`` (act-slot selection)
    The game exposes no per-act slot object at all -- there is no ``ActSlots``
    property, no ``is_legacy`` flag, anywhere in the decompiled source. The
    reconstructed RunState keeps its own RNG-selected acts, which are valid
    and in-distribution but need not match the live run. Affects the act
    candidate one-hot and, indirectly, the boss id.

``mgr._offered_potion`` / ``_offered_relic`` / ``rs.pending_choice``
    Three reward-subscreen flag dims of sim-side RunManager state with no
    wire equivalent. Left at 0 / None.

Everything else -- deck contents, relics, potions, HP, gold, floors,
ascension, the map graph and the visited path -- comes from the wire.

Measured, not assumed: ``tests/test_run_reconstruct.py`` round-trips real
simulated runs through this module and asserts that the ONLY dims which may
differ are the ones listed above (the boss embedding index, the act-candidate
one-hot and the three flags) -- 3 to 4 dims of 4778 in practice. It compares
by dim index rather than by segment name so a regression in the other
seventy-five dims of the run segment still fails.
"""
from __future__ import annotations

import logging
from typing import Any

import numpy as np

logger = logging.getLogger(__name__)

# Emit the "this field is approximated" warnings once per process rather than
# once per decision. At ~25 run decisions per run and 8 runs per session they
# would otherwise drown the log that a human actually reads.
_WARNED: set[str] = set()


def _warn_once(key: str, message: str, *args: Any) -> None:
    if key not in _WARNED:
        _WARNED.add(key)
        logger.warning(message, *args)


class RunView:
    """The ``mgr``-shaped object :meth:`encode_run` reads.

    encode_run touches exactly three attributes on its manager argument --
    ``run_state``, ``get_combat_state()`` and ``phase`` -- plus four that are
    ``getattr``-guarded. The guarded four are supplied explicitly rather than
    omitted: they are private names (``_offered_potion``, ``_offered_relic``,
    ``_current_room_type``) and a missing one yields silent zeros instead of
    an error, so being explicit is the difference between a documented
    approximation and an invisible one.
    """

    def __init__(self, run_state: Any, phase: str,
                 room_type: Any = None) -> None:
        self.run_state = run_state
        self.phase = phase
        self._current_room_type = room_type
        self._offered_potion = None
        self._offered_relic = None

    def get_combat_state(self) -> None:
        """Always None -- the run agent only ever acts out of combat.

        This mirrors HierarchicalRunEnv, which resolves every combat through
        its combat controller before returning control to the run policy. The
        encoder's in-combat branch was therefore never exercised during
        training of these checkpoints, and must not be exercised now.
        """
        return None


def _to_map_point_type(raw: Any) -> Any:
    """Wire ``PointType.ToString()`` -> simulator ``MapPointType``."""
    from sts2_env.core.enums import MapPointType

    name = str(raw or "").strip().upper().replace(" ", "_")
    try:
        return MapPointType[name]
    except KeyError:
        pass
    # The game writes PascalCase ("RestSite"); the simulator uses SCREAMING
    # _SNAKE ("REST_SITE"). Compare on alphanumerics only.
    flat = "".join(ch for ch in name if ch.isalnum())
    for member in MapPointType:
        if "".join(ch for ch in member.name if ch.isalnum()) == flat:
            return member
    return MapPointType.UNASSIGNED


def _to_room_type(raw: Any) -> Any:
    from sts2_env.core.enums import RoomType

    name = str(raw or "").strip().upper().replace(" ", "_")
    try:
        return RoomType[name]
    except KeyError:
        pass
    flat = "".join(ch for ch in name if ch.isalnum())
    for member in RoomType:
        if "".join(ch for ch in member.name if ch.isalnum()) == flat:
            return member
    return None


def _build_deck(raw_deck: list[dict[str, Any]]) -> tuple[list[Any], list[str]]:
    """Construct real CardInstances from the wire's ``run_deck``.

    Returns ``(cards, unresolved_ids)``. Unresolved ids are returned rather
    than swallowed because the deck bag and the seven archetype scalars are
    computed from these cards: a dropped card is not a small error, it is the
    model being told the deck contains something other than what it contains.
    """
    from sts2_env.bridge.combat_reconstruct import _to_card_id
    from sts2_env.cards.factory import create_card

    cards: list[Any] = []
    unresolved: list[str] = []
    for entry in raw_deck or []:
        if isinstance(entry, dict):
            raw_id = entry.get("id")
            upgraded = bool(entry.get("upgraded", False))
        else:
            raw_id, upgraded = entry, False
        card_id = _to_card_id(str(raw_id))
        if card_id is None:
            unresolved.append(str(raw_id))
            continue
        try:
            card = create_card(card_id, upgraded)
        except Exception as exc:  # pragma: no cover - factory guard
            unresolved.append(f"{raw_id} ({type(exc).__name__})")
            continue
        # Instance-applied keywords. Without these the ethereal and zero-cost
        # archetype scalars are computed from the card's factory defaults --
        # measured against real runs, a DEFEND_NECROBINDER that the run had
        # made Ethereal reconstructed with no keywords, moving the ethereal
        # scalar from 0.1 to 0.0.
        keywords = entry.get("keywords") if isinstance(entry, dict) else None
        if keywords:
            card.keywords = set(card.keywords) | {
                str(k).strip().lower() for k in keywords if k}
        cards.append(card)
    return cards, unresolved


def _build_map(raw_points: list[dict[str, Any]],
               raw_visited: list[dict[str, Any]]) -> tuple[Any, list[Any]]:
    """Rebuild the act map graph and the visited path.

    The lookahead segment walks ``children`` three rows deep from the last
    visited point, so both the edges AND the visited path have to be right --
    an empty visited list silently reroots the lookahead at the act start,
    which reads as "floor 1" no matter where the run actually is.
    """
    from sts2_env.map.generator import ActMap
    from sts2_env.map.map_point import MapCoord

    points = raw_points or []
    max_row = max((int(p.get("row", 0)) for p in points), default=0)
    act_map = ActMap(num_rooms=max(max_row, 1))

    for entry in points:
        point = act_map.get_or_create(int(entry.get("col", 0)),
                                      int(entry.get("row", 0)))
        point.point_type = _to_map_point_type(entry.get("type"))

    # Edges in a second pass: a child can appear before its parent in the
    # payload, and get_or_create must not invent a type-less duplicate.
    for entry in points:
        parent = act_map.get_or_create(int(entry.get("col", 0)),
                                       int(entry.get("row", 0)))
        for child in entry.get("children") or []:
            parent.add_child(act_map.get_or_create(int(child.get("col", 0)),
                                                   int(child.get("row", 0))))

    # start_point / boss_point are meta-nodes the game keeps outside its grid;
    # identify them by row so room_points() stays correct.
    for point in act_map.all_points():
        if point.row == 0:
            act_map.start_point = point
    boss = [p for p in act_map.all_points() if p.row == max_row]
    if boss:
        act_map.boss_point = boss[0]

    visited = [MapCoord(int(c.get("col", 0)), int(c.get("row", 0)))
               for c in (raw_visited or [])]
    return act_map, visited


def reconstruct_run(state: dict[str, Any],
                    character_id: str = "Necrobinder") -> RunView | None:
    """Rebuild a RunView from a bridge payload, or None if not possible.

    Returns None (rather than a half-populated view) when the payload lacks
    the deck, because a run agent choosing card rewards from a zeroed deck bag
    is worse than falling back to the heuristics: it would look like it was
    working.
    """
    from sts2_env.bridge.run_state_adapter import _BRIDGE_PHASE_TO_RUN_PHASE
    from sts2_env.run.run_state import RunState

    msg_type = state.get("type", "")
    phase = _BRIDGE_PHASE_TO_RUN_PHASE.get(msg_type)
    if phase is None:
        logger.debug("run reconstruction: no run phase for wire type %r", msg_type)
        return None

    raw_deck = state.get("run_deck")
    if not raw_deck:
        _warn_once(
            "no_run_deck",
            "run reconstruction unavailable: payload carries no 'run_deck'. "
            "The deck bag and archetype scalars are 594 of the 4778 "
            "observation dims, so the RL run agent cannot be fed from this "
            "payload; falling back to the heuristics. Rebuild and redeploy "
            "the bridge mod to add the field.",
        )
        return None

    run_state = RunState(
        ascension_level=int(state.get("ascension_level", 0) or 0),
        character_id=character_id,
    )

    player = run_state.player
    player.max_hp = int(state.get("max_hp", player.max_hp) or player.max_hp)
    player.current_hp = int(state.get("hp", player.current_hp) or player.current_hp)
    player.gold = int(state.get("gold", 0) or 0)
    player.relics = [str(r) for r in (state.get("relics") or [])]
    player.max_potion_slots = int(
        state.get("max_potion_slots", player.max_potion_slots)
        or player.max_potion_slots)

    cards, unresolved = _build_deck(raw_deck)
    if unresolved:
        # Loud, and states the consequence -- the lesson from the COUNTDOWN
        # bug, where an id mismatch logged at DEBUG cost the planner a whole
        # card and was invisible for the entire session.
        logger.error(
            "run agent: %d unresolvable deck card id(s) (%s). The deck bag "
            "and archetype scalars would describe a different deck than the "
            "game holds, so card-reward and shop choices would be made on "
            "false information. Falling back to the heuristics.",
            len(unresolved), ", ".join(unresolved[:6]),
        )
        return None
    player.deck = cards

    # Potions: same construction path as combat, so ids resolve identically.
    from sts2_env.bridge.combat_reconstruct import _make_potion

    potions: list[Any] = [None] * max(player.max_potion_slots, 0)
    for entry in state.get("potions") or []:
        slot = int(entry.get("slot", 0) or 0)
        potion = _make_potion(entry.get("id"), slot)
        if potion is not None and 0 <= slot < len(potions):
            potions[slot] = potion
    player.potions = potions

    run_state.current_act_index = max(0, int(state.get("act", 1) or 1) - 1)
    run_state.act_floor = int(state.get("act_floor", 0) or 0)
    run_state.total_floor = int(state.get("floor", 0) or 0)

    raw_points = state.get("act_map")
    if raw_points:
        act_map, visited = _build_map(raw_points, state.get("visited_coords"))
        run_state.map = act_map
        run_state.visited_map_coords = visited
    else:
        _warn_once(
            "no_act_map",
            "payload carries no 'act_map': the 27-dim map lookahead will be "
            "zero, which the model reads as 'nothing reachable ahead'. Map "
            "choices will be made without lookahead.",
        )

    _warn_once(
        "acts_approximated",
        "run reconstruction: the game exposes no act-slot selection, so the "
        "reconstructed run keeps its own RNG-chosen acts. The act candidate "
        "one-hot and the boss id may not match the live run.",
    )

    return RunView(run_state, phase, _to_room_type(state.get("room_type")))


def encode_run_observation(state: dict[str, Any],
                           character_id: str = "Necrobinder") -> np.ndarray | None:
    """Bridge payload -> the 4778-dim RICH run observation, or None."""
    from sts2_env.gym_env.rich_observation import RichObservationEncoder

    view = reconstruct_run(state, character_id=character_id)
    if view is None:
        return None
    try:
        return RichObservationEncoder().encode_run(view)
    except Exception as exc:
        logger.error(
            "run observation encoding failed (%s: %s) -- falling back to the "
            "heuristics for this decision.", type(exc).__name__, exc,
        )
        return None
