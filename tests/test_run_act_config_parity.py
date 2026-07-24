"""Run act configuration parity tests."""

from sts2_env.events.act2 import LuminousChoir, RanwidTheElder
from sts2_env.map.acts import ALL_ACTS
from sts2_env.potions.base import create_potion
from sts2_env.relics.base import RelicId
from sts2_env.run.events import pick_event
from sts2_env.run.run_state import RunState

RUN_SEED = 42
ACT_TWO_INDEX = 1
ENTRY_BLOCKED_GOLD = LuminousChoir.ENTRY_GOLD_COST - 1
FIRE_POTION_ID = "FirePotion"


def test_initialize_run_generates_shuffled_act_event_rooms_like_csharp_runmanager():
    run_state = RunState(seed=RUN_SEED)
    # Capture each SELECTED act's candidate event list + legacy flag before
    # _generate_rooms rewrites them (the acts chosen per slot vary by seed).
    pre = [(act.is_legacy, list(act.event_ids)) for act in run_state.acts]

    run_state.initialize_run()

    up_front_expected = 0
    for (is_legacy, before), act in zip(pre, run_state.acts):
        if is_legacy:
            # "Acts from the Past" legacy act: the event pool is filtered
            # (event_allowed_in_act + IActRestricted) and shrine-interleaved
            # via build_legacy_event_pool -- a subset/reorder of the
            # candidate list that does NOT touch the shared up_front stream.
            assert set(act.event_ids).issubset(set(before))
            assert act.event_ids  # always at least some allowed events
        else:
            # Vanilla act: Fisher-Yates shuffle preserves the multiset and
            # consumes len-1 up_front draws (0 for N<=1, e.g. Act4Heart's
            # eventless Act 4).
            assert sorted(act.event_ids) == sorted(before)
            up_front_expected += max(0, len(before) - 1)
    assert run_state.rng.up_front.counter == up_front_expected


def test_initialize_run_does_not_regenerate_event_rooms_after_first_initialization():
    run_state = RunState(seed=RUN_SEED)
    run_state.initialize_run()
    event_ids_by_act = [list(act.event_ids) for act in run_state.acts]
    up_front_counter = run_state.rng.up_front.counter

    run_state.initialize_run()

    assert [act.event_ids for act in run_state.acts] == event_ids_by_act
    assert run_state.rng.up_front.counter == up_front_counter


def test_run_state_uses_mutable_act_copies_like_csharp_runstate():
    first_run = RunState(seed=RUN_SEED)
    second_run = RunState(seed=RUN_SEED)

    first_run.acts[ACT_TWO_INDEX].event_ids = [RanwidTheElder.event_id]

    assert second_run.acts[ACT_TWO_INDEX].event_ids != [RanwidTheElder.event_id]
    assert LuminousChoir.event_id in second_run.acts[ACT_TWO_INDEX].event_ids


def test_pick_event_advances_through_current_act_event_order_like_csharp_roomset():
    run_state = RunState(seed=RUN_SEED)
    run_state.current_act_index = ACT_TWO_INDEX
    run_state.current_act.event_ids = [
        LuminousChoir.event_id,
        RanwidTheElder.event_id,
    ]
    run_state.player.gold = ENTRY_BLOCKED_GOLD
    run_state.player.add_potion(create_potion(FIRE_POTION_ID))
    run_state.player.obtain_relic(RelicId.ANCHOR.name)

    event = pick_event(run_state)

    assert isinstance(event, RanwidTheElder)
    assert run_state.current_act.events_visited == 2
    assert run_state.rng.up_front.counter == 0


def test_pick_event_with_explicit_pool_does_not_mutate_act_event_cursor():
    run_state = RunState(seed=RUN_SEED)
    run_state.current_act_index = ACT_TWO_INDEX
    run_state.current_act.events_visited = 1
    run_state.player.gold = RanwidTheElder.ENTRY_GOLD_COST
    run_state.player.add_potion(create_potion(FIRE_POTION_ID))
    run_state.player.obtain_relic(RelicId.ANCHOR.name)

    event = pick_event(run_state, pool=[RanwidTheElder.event_id])

    assert isinstance(event, RanwidTheElder)
    assert run_state.current_act.events_visited == 1


def test_pick_event_repeats_current_act_event_after_unique_events_are_exhausted_like_csharp_roomset():
    run_state = RunState(seed=RUN_SEED)
    run_state.current_act_index = ACT_TWO_INDEX
    run_state.current_act.event_ids = [RanwidTheElder.event_id]
    run_state.current_act.events_visited = 1
    run_state.visited_event_ids.add(RanwidTheElder.event_id)
    run_state.player.gold = RanwidTheElder.ENTRY_GOLD_COST
    run_state.player.add_potion(create_potion(FIRE_POTION_ID))
    run_state.player.obtain_relic(RelicId.ANCHOR.name)

    event = pick_event(run_state)

    assert isinstance(event, RanwidTheElder)
    assert run_state.current_act.events_visited == 3
