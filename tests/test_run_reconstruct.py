"""Round-trip parity for the run-level bridge reconstruction.

The RL run agent is fed by rebuilding simulator objects from a wire payload
and encoding those with the same encoder the model trained against. That is
only sound if the round trip is lossless, and "lossless" has to be measured
rather than assumed -- a segment silently left at zero produces an
off-distribution observation that still looks like a working integration.

These tests encode a genuine simulated run directly, serialise that same run
the way RunStateBridgeFields.Apply does, reconstruct it, and compare the two
4778-dim vectors segment by segment.
"""
from __future__ import annotations

import numpy as np
import pytest

from sts2_env.bridge.run_reconstruct import (
    encode_run_observation,
    reconstruct_run,
)
from sts2_env.gym_env.rich_observation import RichObservationEncoder, segment_table

def _approximated_dims() -> set[int]:
    """Exact observation indices the wire cannot currently supply.

    Deliberately enumerated as INDICES, not segment names. The run segment is
    78 dims covering floors, HP, gold, deck aggregates, the map lookahead and
    more; allowing the whole segment to differ because three of its dims are
    approximated would stop this test from catching a regression in the other
    seventy-five.

    Two independent causes:

    1. Act selection (``ids_boss`` + the act-candidate one-hot). The game
       exposes no per-act slot object anywhere in the decompiled source, so
       the reconstructed run keeps its own RNG-chosen acts.
    2. Reward-subscreen flags (``_offered_potion``, ``_offered_relic``,
       ``pending_choice``). Sim-side RunManager state with no wire field.

    Both are candidates for a future wire extension; until then they are
    known, bounded, and asserted rather than silent.
    """
    import sts2_env.gym_env.rich_observation as R

    dims = {R.IDS_BOSS_OFF}
    cand_start = R.RUN_OFF + R.RUN_ACT_CAND_OFF
    dims.update(range(
        cand_start, cand_start + R.NUM_ACT_SLOTS * (R.MAX_ACT_CANDIDATES + 1)))
    flag_start = R.RUN_OFF + R.RUN_PHASE_OFF + R.NUM_RUN_PHASES
    dims.update(range(flag_start, flag_start + 3))
    return dims

WIRE_FOR_PHASE = {
    "MAP_CHOICE": "map_select",
    "CARD_REWARD": "card_reward",
    "SHOP": "shop",
    "REST_SITE": "rest_site",
    "EVENT": "event",
    "TREASURE": "treasure",
    "BOSS_RELIC": "boss_relic",
}


def _payload_from_mgr(mgr, wire_type: str) -> dict:
    """Mirror RunStateBridgeFields.Apply, including run_deck and act_map."""
    rs = mgr.run_state
    player = rs.player
    payload = {
        "type": wire_type,
        "act": rs.current_act_index + 1,
        "act_index": rs.current_act_index,
        "floor": rs.total_floor,
        "act_floor": rs.act_floor,
        "ascension_level": rs.ascension_level,
        "hp": player.current_hp,
        "max_hp": player.max_hp,
        "gold": player.gold,
        "deck_size": len(player.deck),
        "relic_count": len(player.relics),
        "relics": list(player.relics),
        "max_potion_slots": player.max_potion_slots,
        "run_deck": [{"id": c.card_id.name, "upgraded": bool(c.upgraded),
                      "keywords": sorted(c.keywords)}
                     for c in player.deck],
        # potion_id is an enum for some potions and a plain str for others,
        # so normalise the way the C# side does (it always sends a string).
        "potions": [{"slot": i, "id": getattr(p.potion_id, "name", p.potion_id)}
                    for i, p in enumerate(player.potions) if p is not None],
    }
    room = getattr(mgr, "_current_room_type", None)
    if room is not None:
        payload["room_type"] = room.name
    if rs.map is not None:
        payload["act_map"] = [
            {"row": pt.row, "col": pt.col, "type": pt.point_type.name,
             "children": [{"row": c.row, "col": c.col} for c in pt.children]}
            for pt in rs.map.all_points()
        ]
        payload["visited_coords"] = [{"row": c.row, "col": c.col}
                                     for c in rs.visited_map_coords]
    return payload


def _walk_run(seed: int, max_steps: int = 30):
    """Yield (mgr, wire_type) at each out-of-combat decision point."""
    from scripts.train_hierarchical import make_run_env

    env = make_run_env(None, ascension=0, max_act_count=1, seed=seed)
    env.set_shaping_scale(0.0)
    env.reset(seed=seed)

    for _ in range(max_steps):
        mgr = env._mgr
        if mgr is None:
            return
        wire_type = WIRE_FOR_PHASE.get(mgr.phase)
        if wire_type is not None and player_deck_nonempty(mgr):
            yield mgr, wire_type
        mask = np.asarray(env.action_masks(), dtype=bool)
        legal = np.flatnonzero(mask)
        if not legal.size:
            return
        _, _, done, truncated, _ = env.step(int(legal[0]))
        if done or truncated:
            return


def player_deck_nonempty(mgr) -> bool:
    return len(mgr.run_state.player.deck) > 0


# Seeds matter here. The first version of this test used three seeds and
# passed; widening to ten surfaced a real divergence (a runtime-applied
# 'ethereal' keyword that create_card could not reproduce) that those three
# never hit. Seed 2 is the one that caught it -- keep it.
@pytest.mark.parametrize("seed", [1, 2, 3, 7, 11, 42, 99, 999, 4242, 12345])
def test_round_trip_matches_direct_encoding(seed):
    """A reconstructed run encodes identically to the run it came from."""
    encoder = RichObservationEncoder()
    allowed = _approximated_dims()
    compared = 0
    unexpected: dict[int, str] = {}

    for mgr, wire_type in _walk_run(seed):
        direct = encoder.encode_run(mgr)
        reconstructed = encode_run_observation(_payload_from_mgr(mgr, wire_type))
        assert reconstructed is not None, (
            f"reconstruction returned None at phase {mgr.phase}")
        compared += 1

        diff = np.abs(direct - reconstructed)
        for index in np.flatnonzero(diff > 1e-6):
            index = int(index)
            if index in allowed:
                continue
            segment = next(
                (n for n, s, z in segment_table() if s <= index < s + z), "?")
            unexpected[index] = segment

    assert compared > 0, "walked the run without reaching a decision point"
    assert not unexpected, (
        f"dims diverged that should be exact (index -> segment): {unexpected}")


def test_deck_bag_is_actually_populated():
    """Guard against 'parity' that holds because both sides are zero.

    The deck bag and archetype scalars are 594 dims and the whole reason the
    wire had to grow a run_deck field. If a refactor stopped populating them,
    the round-trip test above would still pass -- zero equals zero.
    """
    for mgr, wire_type in _walk_run(12345):
        obs = encode_run_observation(_payload_from_mgr(mgr, wire_type))
        assert obs is not None
        deck_bag = dict((n, (s, z)) for n, s, z in segment_table())["deck_bag"]
        start, size = deck_bag
        assert obs[start:start + size].sum() > 0, "deck bag is empty"
        arch = dict((n, (s, z)) for n, s, z in segment_table())["archetype_scalars"]
        a_start, a_size = arch
        assert obs[a_start:a_start + a_size].sum() > 0, "archetype scalars empty"
        return
    pytest.fail("no decision point reached")


def test_missing_run_deck_refuses_rather_than_zeroing():
    """No run_deck must mean 'fall back', never 'encode a zeroed deck'."""
    payload = {
        "type": "card_reward",
        "act": 1, "floor": 3, "act_floor": 3, "ascension_level": 0,
        "hp": 50, "max_hp": 66, "gold": 120,
    }
    assert reconstruct_run(payload) is None
    assert encode_run_observation(payload) is None


def test_unresolvable_card_id_refuses(caplog):
    """An unresolvable deck id must refuse loudly, not drop the card.

    Dropping is what the COUNTDOWN bug did in combat: the simulator held a
    different deck than the game and nothing said so.
    """
    payload = {
        "type": "card_reward",
        "act": 1, "floor": 3, "act_floor": 3, "ascension_level": 0,
        "hp": 50, "max_hp": 66, "gold": 120,
        "run_deck": [{"id": "STRIKE_NECROBINDER", "upgraded": False},
                     {"id": "NOT_A_REAL_CARD_XYZ", "upgraded": False}],
    }
    with caplog.at_level("ERROR"):
        assert reconstruct_run(payload) is None
    assert any("unresolvable deck card id" in record.getMessage()
               for record in caplog.records), "refusal was not logged at ERROR"
