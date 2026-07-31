"""After answering an event, the agent must be able to leave it.

An event has two kinds of page: a QUESTION, whose options are real choices,
and a RESULT, whose only affordance is Proceed. The wire did not distinguish
them -- RlNonCombatRoomHandlers sent every option as a generic
EventChoiceAction even though it branches on Option.IsProceed a few lines
later -- so the agent answered a result page as though it were still a
question.

The page then did not change, three identical clicks tripped the mod's
MaxRepeatedChoices, the event was abandoned, and room-level recovery ended
the run. Reported 2026-07-31 as "the agent restarted a run instead of
clicking proceed on an event after it chose an option".
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import (
    _LAST_NONCOMBAT_CHOICE,
    _break_repeated_choice,
    _pick_event_option_heuristic,
    _proceed_option,
)


def _question_page():
    return {
        "type": "event", "floor": 7,
        "options": [
            {"index": 0, "action": "event_choice", "is_proceed": False,
             "label": "Take the gold", "enabled": True},
            {"index": 1, "action": "event_choice", "is_proceed": False,
             "label": "Leave", "enabled": True},
        ],
    }


def _result_page():
    return {
        "type": "event", "floor": 7,
        "options": [
            {"index": 0, "action": "proceed", "is_proceed": True,
             "label": "Proceed", "enabled": True},
        ],
    }


def _mixed_page():
    return {
        "type": "event", "floor": 7,
        "options": [
            {"index": 0, "action": "event_choice", "is_proceed": False,
             "label": "Pray", "enabled": True},
            {"index": 1, "action": "proceed", "is_proceed": True,
             "label": "Proceed", "enabled": True},
        ],
    }


@pytest.fixture(autouse=True)
def _clear_guard():
    saved = list(_LAST_NONCOMBAT_CHOICE)
    _LAST_NONCOMBAT_CHOICE[:] = [None, 0]
    yield
    _LAST_NONCOMBAT_CHOICE[:] = saved


def test_a_result_page_is_recognised():
    assert _proceed_option(_result_page()) is not None
    assert _proceed_option(_question_page()) is None


def test_a_result_page_proceeds():
    assert _pick_event_option_heuristic(_result_page()) == 0


def test_a_question_page_is_still_answered_normally():
    # The fix must not turn every event into an immediate exit.
    assert _pick_event_option_heuristic(_question_page()) == 0


def test_proceed_is_not_taken_reflexively_when_real_choices_remain():
    # Some events offer "leave" alongside a real choice on the FIRST page.
    # Declining is a decision, not a reflex, so the policy keeps it.
    assert _pick_event_option_heuristic(_mixed_page()) == 0


def test_a_stuck_choice_falls_back_to_proceed_rather_than_a_guess():
    state = _mixed_page()
    stuck = {"phase": "run", "method": "choose", "args": [0]}
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT):
        assert _break_repeated_choice(state, stuck) == stuck
    broken = _break_repeated_choice(state, stuck)
    assert broken["args"] == [1], (
        "a page that will not advance is a result page; Proceed is the answer, "
        "not an arbitrary other option"
    )


def test_a_payload_without_the_flag_still_works():
    # A mod build predating is_proceed must not crash or misbehave.
    legacy = {
        "type": "event", "floor": 7,
        "options": [{"index": 0, "action": "event_choice", "enabled": True}],
    }
    assert _proceed_option(legacy) is None
    assert _pick_event_option_heuristic(legacy) == 0
