"""Weak-vs-regular enemy encounter selection is COMBAT-COUNT based.

The first ``ActConfig.num_weak_encounters`` REGULAR monster combats of an act
draw from the weak encounter pool; every regular monster combat after that
draws from the normal pool. This is gated on the number of regular monster
combats entered so far this act -- NOT on the floor number (``act_floor``,
which counts every room: events, rests, shops, elites, treasure...).

Ground truth: decompiled ActModel.GenerateRooms builds an ordered encounter
queue (first NumberOfWeakEncounters slots weak, remainder regular) consumed in
order by monster rooms. Elite/boss combats use their own pools and never
consume the weak counter. A "?"/Unknown map node that resolves into a monster
fight flows through the same RunManager._enter_combat(RoomType.MONSTER) path
and therefore also counts.
"""

from __future__ import annotations

import sts2_env.run.run_manager as rm
from sts2_env.core.enums import MapPointType, RoomType
from sts2_env.map.acts import ACT_0, ACT_1, ACT_2
from sts2_env.run.run_manager import RunManager

SEED = 12345


def _recording_pools(recorder: list[str]):
    """Return a drop-in for run_manager._get_encounter_pools whose setup
    functions record which pool ("weak"/"normal"/"elite"/"boss") was chosen
    while still delegating to the real encounter setup so combat is valid."""

    # Capture the real implementation before monkeypatch swaps it in (avoids
    # the factory recursing into itself).
    original = rm._get_encounter_pools

    def _factory(pool_key: str) -> dict[str, list]:
        real = original(pool_key)

        def tag(name: str) -> list:
            base = real[name]
            real_fn = base[0] if base else None

            def _setup(combat, encounter_rng) -> None:
                recorder.append(name)
                if real_fn is not None:
                    real_fn(combat, encounter_rng)

            return [_setup]

        return {"weak": tag("weak"), "normal": tag("normal"), "elite": tag("elite"), "boss": tag("boss")}

    return _factory


def _pin_act(mgr: RunManager, act_config) -> None:
    """Pin the run to a known vanilla ActConfig so pool_key/num_weak_encounters
    are deterministic regardless of the per-slot act randomization. Only the
    ActConfig matters for pool selection (we drive _enter_combat directly, so
    the generated map is irrelevant)."""
    idx = act_config.act_index
    mgr.run_state.current_act_index = idx
    mgr.run_state.acts[idx] = act_config.to_mutable()
    mgr.run_state.regular_monster_combats_this_act = 0


# ── (a) Act 1: first 3 regular monster combats weak, 4th+ regular ──────────


def test_act1_first_three_regular_monster_combats_are_weak(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)
    assert mgr.run_state.current_act.num_weak_encounters == 3

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    for _ in range(5):
        mgr._enter_combat(RoomType.MONSTER)

    assert recorder == ["weak", "weak", "weak", "normal", "normal"]
    assert mgr.run_state.regular_monster_combats_this_act == 5


# ── (c) Floor number is irrelevant: non-combat rooms don't shift weakness ──


def test_weak_selection_is_combat_count_not_floor_number(monkeypatch):
    """Bumping act_floor far past num_weak_encounters (as rests/events/shops
    would) must NOT make an early regular monster combat "regular". Under the
    old floor-based bug, act_floor=100 would have selected the normal pool."""
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    for combat_index in range(4):
        # Simulate lots of intervening non-combat rooms before every combat.
        mgr.run_state.act_floor = 100 + combat_index
        mgr._enter_combat(RoomType.MONSTER)

    # Combat count, not floor, decides: first 3 weak despite act_floor >> 3.
    assert recorder == ["weak", "weak", "weak", "normal"]


def test_real_rest_site_between_combats_does_not_change_weakness(monkeypatch):
    """A real rest site inserted between monster combats leaves the weak
    counter untouched, so the combat that follows is still weak if fewer than
    num_weak_encounters regular monster combats have happened."""
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    mgr._enter_combat(RoomType.MONSTER)  # weak (1st)
    mgr._enter_combat(RoomType.MONSTER)  # weak (2nd)

    # Insert a real rest site room (advances the floor but not the counter).
    mgr.run_state.act_floor = 50
    mgr._enter_rest_site()
    assert mgr.run_state.regular_monster_combats_this_act == 2

    mgr._enter_combat(RoomType.MONSTER)  # still weak (3rd) -- counter, not floor
    mgr._enter_combat(RoomType.MONSTER)  # normal (4th)

    assert recorder == ["weak", "weak", "weak", "normal"]


# ── (b) Act 2 / Act 3: first 2 regular monster combats weak ────────────────


def test_act2_first_two_regular_monster_combats_are_weak(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_1)
    assert mgr.run_state.current_act.num_weak_encounters == 2

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    for _ in range(4):
        mgr._enter_combat(RoomType.MONSTER)

    assert recorder == ["weak", "weak", "normal", "normal"]


def test_act3_first_two_regular_monster_combats_are_weak(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_2)
    assert mgr.run_state.current_act.num_weak_encounters == 2

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    for _ in range(4):
        mgr._enter_combat(RoomType.MONSTER)

    assert recorder == ["weak", "weak", "normal", "normal"]


# ── enter_act resets the per-act counter ───────────────────────────────────


def test_enter_act_resets_regular_monster_combat_counter(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    for _ in range(4):
        mgr._enter_combat(RoomType.MONSTER)
    assert mgr.run_state.regular_monster_combats_this_act == 4

    # Advancing to the next act resets the weak counter (fresh weak encounters).
    mgr.run_state.enter_act(1)
    assert mgr.run_state.regular_monster_combats_this_act == 0

    mgr._enter_combat(RoomType.MONSTER)
    assert recorder[-1] == "weak"
    assert mgr.run_state.regular_monster_combats_this_act == 1


# ── (d) Elite/boss combats never consume the weak counter ──────────────────


def test_elite_combat_does_not_consume_weak_counter(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    # An elite before any regular combats must not advance the weak counter...
    mgr._enter_combat(RoomType.ELITE)
    assert mgr.run_state.regular_monster_combats_this_act == 0

    # ...and elites interleaved with monsters don't steal weak slots either.
    mgr._enter_combat(RoomType.MONSTER)  # weak (1st)
    mgr._enter_combat(RoomType.ELITE)  # no counter change
    mgr._enter_combat(RoomType.MONSTER)  # weak (2nd)
    mgr._enter_combat(RoomType.MONSTER)  # weak (3rd)
    mgr._enter_combat(RoomType.MONSTER)  # normal (4th)

    assert mgr.run_state.regular_monster_combats_this_act == 4
    assert recorder == ["elite", "weak", "elite", "weak", "weak", "normal"]


def test_boss_combat_does_not_consume_weak_counter(monkeypatch):
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    before = mgr.run_state.regular_monster_combats_this_act
    mgr._enter_combat(RoomType.BOSS)
    assert mgr.run_state.regular_monster_combats_this_act == before


# ── (e) A "?"/Unknown node that resolves to a monster fight also counts ─────


def test_unknown_node_resolving_to_monster_counts_toward_weak_counter(monkeypatch):
    """The map-move path resolves a "?" (Unknown) node's room type via
    UnknownMapPointOdds; when it comes up MONSTER it must funnel through the
    same _enter_combat(RoomType.MONSTER) as a plain Monster node and advance
    the weak counter."""
    mgr = RunManager(seed=SEED, character_id="Ironclad")
    _pin_act(mgr, ACT_0)

    recorder: list[str] = []
    monkeypatch.setattr(rm, "_get_encounter_pools", _recording_pools(recorder))

    # Force the "?" roll to come up MONSTER (a real possible outcome of
    # UnknownMapPointOdds.roll) and record what point type was resolved.
    seen_point_types: list[MapPointType] = []

    def fake_resolve(point_type, blacklist=None):
        seen_point_types.append(point_type)
        return RoomType.MONSTER

    monkeypatch.setattr(mgr.run_state, "resolve_room_type", fake_resolve)

    # Take a reachable node and mark it as an Unknown "?" node.
    coord = mgr._available_coords[0]
    point = mgr.run_state.map.get_point(coord)
    point.point_type = MapPointType.UNKNOWN

    before = mgr.run_state.regular_monster_combats_this_act
    mgr._do_map_move({"coord": (coord.col, coord.row)})

    # The "?" node was resolved (as UNKNOWN) and produced a counted weak combat.
    assert MapPointType.UNKNOWN in seen_point_types
    assert mgr.phase == RunManager.PHASE_COMBAT
    assert mgr.run_state.regular_monster_combats_this_act == before + 1
    assert recorder == ["weak"]
