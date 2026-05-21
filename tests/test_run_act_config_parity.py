"""Run act configuration parity tests."""

from sts2_env.events.act2 import LuminousChoir, RanwidTheElder
from sts2_env.run.run_state import RunState


def test_run_state_uses_mutable_act_copies_like_csharp_runstate():
    run_seed = 42
    act_two_index = 1
    first_run = RunState(seed=run_seed)
    second_run = RunState(seed=run_seed)

    first_run.acts[act_two_index].event_ids = [RanwidTheElder.event_id]

    assert second_run.acts[act_two_index].event_ids != [RanwidTheElder.event_id]
    assert LuminousChoir.event_id in second_run.acts[act_two_index].event_ids
