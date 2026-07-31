"""A repeated live choice must be varied before the mod kills the run.

run_env has had an anti-dither guard since forensics found deterministic
policies toggling one option forever. The LIVE bridge path had none, so the
same behaviour ran unchecked until RlNonCombatRoomHandlers hit its
MaxRepeatedChoices = 3 and RlAutoSlayer reported "Run finished: terminated".

Measured 2026-07-31 (planner_div6.log)::

    RUN-RL (event): action 145 -> choose([0])
    RUN-RL (event): action 145 -> choose([0])
    RUN-RL (event): action 145 -> choose([0])
    Received: type=run_complete
    Run finished: terminated (run 3 this session)

The guard has to act BELOW the mod's limit of 3, or the run is already dead
by the time it would fire.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import (
    _LAST_NONCOMBAT_CHOICE,
    _break_repeated_choice,
    _screen_fingerprint,
)


def _event(floor=7, options=("Accept", "Refuse")):
    return {
        "type": "event",
        "floor": floor,
        "options": [
            {"index": i, "label": label, "enabled": True}
            for i, label in enumerate(options)
        ],
    }


CHOOSE_0 = {"phase": "run", "method": "choose", "args": [0]}


@pytest.fixture(autouse=True)
def _clear_guard():
    saved = list(_LAST_NONCOMBAT_CHOICE)
    _LAST_NONCOMBAT_CHOICE[:] = [None, 0]
    yield
    _LAST_NONCOMBAT_CHOICE[:] = saved


def test_the_guard_fires_below_the_mods_abort_threshold():
    assert agent_runner.NONCOMBAT_REPEAT_LIMIT < 3, (
        "the mod aborts the room after 3 identical choices, so a guard at or "
        "above 3 can never run"
    )


def test_a_repeated_choice_is_eventually_varied():
    state = _event()
    seen = [_break_repeated_choice(state, CHOOSE_0)["args"][0]
            for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 1)]
    assert seen[-1] != 0, f"never varied: {seen}"
    assert seen[:-1] == [0] * agent_runner.NONCOMBAT_REPEAT_LIMIT


def test_the_first_choices_are_left_alone():
    # The policy gets to make its decision; the guard is a backstop, not an
    # override.
    state = _event()
    assert _break_repeated_choice(state, CHOOSE_0) == CHOOSE_0


def test_a_different_screen_resets_the_counter():
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT):
        _break_repeated_choice(_event(floor=7), CHOOSE_0)
    # A genuinely different screen must not inherit the previous streak.
    assert _break_repeated_choice(_event(floor=8), CHOOSE_0) == CHOOSE_0


def test_a_screen_with_one_option_is_not_rotated():
    state = _event(options=("Only choice",))
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 2):
        decoded = _break_repeated_choice(state, CHOOSE_0)
    assert decoded == CHOOSE_0, "there is nothing else to pick"


def test_non_choose_actions_pass_through():
    skip = {"phase": "run", "method": "skip", "args": []}
    state = _event()
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 2):
        assert _break_repeated_choice(state, skip) == skip


def test_the_fingerprint_distinguishes_screens_by_their_options():
    assert _screen_fingerprint(_event()) == _screen_fingerprint(_event())
    assert _screen_fingerprint(_event()) != _screen_fingerprint(
        _event(options=("Fight", "Flee")))
    assert _screen_fingerprint(_event(floor=7)) != _screen_fingerprint(
        _event(floor=8))
