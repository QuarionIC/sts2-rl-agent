"""Run act configuration parity tests."""

from sts2_env.events.act2 import LuminousChoir, RanwidTheElder
from sts2_env.potions.base import create_potion
from sts2_env.relics.base import RelicId
from sts2_env.run.events import pick_event
from sts2_env.run.run_state import RunState

RUN_SEED = 42
ACT_TWO_INDEX = 1
ENTRY_BLOCKED_GOLD = LuminousChoir.ENTRY_GOLD_COST - 1
FIRE_POTION_ID = "FirePotion"


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
