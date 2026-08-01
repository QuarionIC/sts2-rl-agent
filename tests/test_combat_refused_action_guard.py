"""A combat action the game refuses must not be replayed forever.

Measured overnight 2026-08-01 in a live session: the planner played FLATTEN
from hand slot 0 against EYE_WITH_TEETH **49 times in three minutes**. The live
hand never lost the card -- the game simply would not accept it -- and because
the planner is DETERMINISTIC, every replan of that unchanged state produced
FLATTEN again. The fight only ended when the mod's watchdog killed the run.

The non-combat path has had a repeat breaker since the event-dither incident.
Combat had none, so a single unplayable card could burn an entire run.

Two attempts is the right threshold precisely BECAUSE the planner is
deterministic: nothing is learned between identical replans of an unchanged
state, so a third try cannot succeed where the first two failed.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import (
    _LAST_COMBAT_SEND,
    _combat_action_is_being_refused,
    _combat_state_fingerprint,
)


def _state(hand=("FLATTEN", "DEFEND_NECROBINDER", "DAZED"), energy=2):
    return {
        "type": "combat_action",
        "hand": [{"id": c} for c in hand],
        "player": {"energy": energy},
    }


def _play(card_index=0, target=0):
    return {"type": "PLAY", "card_index": card_index, "target_index": target}


@pytest.fixture(autouse=True)
def _clear():
    saved = list(_LAST_COMBAT_SEND)
    _LAST_COMBAT_SEND[:] = [None, 0]
    yield
    _LAST_COMBAT_SEND[:] = saved


def test_the_first_attempts_are_allowed():
    # The guard is a backstop, not a veto: a card that simply takes a moment
    # to register must still get played.
    state = _state()
    for _ in range(agent_runner.COMBAT_REJECT_LIMIT):
        assert not _combat_action_is_being_refused(state, _play())


def test_a_repeatedly_refused_action_is_eventually_caught():
    state = _state()
    for _ in range(agent_runner.COMBAT_REJECT_LIMIT):
        _combat_action_is_being_refused(state, _play())
    assert _combat_action_is_being_refused(state, _play())


def test_the_threshold_is_low_because_the_planner_is_deterministic():
    # A high threshold would burn dozens of round trips learning nothing --
    # 49 of them, in the incident this exists to prevent.
    assert agent_runner.COMBAT_REJECT_LIMIT <= 3


def test_a_changed_hand_resets_the_counter():
    # The action LANDED (or the turn moved on); this is not a refusal.
    state = _state()
    for _ in range(agent_runner.COMBAT_REJECT_LIMIT + 2):
        _combat_action_is_being_refused(state, _play())
    moved_on = _state(hand=("DEFEND_NECROBINDER", "DAZED"), energy=1)
    assert not _combat_action_is_being_refused(moved_on, _play())


def test_a_different_action_from_the_same_hand_resets_the_counter():
    state = _state()
    for _ in range(agent_runner.COMBAT_REJECT_LIMIT + 2):
        _combat_action_is_being_refused(state, _play(card_index=0))
    assert not _combat_action_is_being_refused(state, _play(card_index=1))


def test_energy_is_part_of_the_fingerprint():
    # Same hand, different energy, is a different situation -- a card refused
    # for cost may be affordable now.
    assert _combat_state_fingerprint(_state(energy=2)) != \
        _combat_state_fingerprint(_state(energy=3))


def test_the_target_is_part_of_the_action_key():
    state = _state()
    for _ in range(agent_runner.COMBAT_REJECT_LIMIT + 2):
        _combat_action_is_being_refused(state, _play(target=0))
    # Retargeting is a genuinely different attempt and deserves its own budget.
    assert not _combat_action_is_being_refused(state, _play(target=1))


def test_the_incident_is_bounded():
    """The 49-attempt case must now stop in single digits."""
    state = _state()
    attempts = 0
    for _ in range(60):
        attempts += 1
        if _combat_action_is_being_refused(state, _play()):
            break
    assert attempts <= 5, f"took {attempts} attempts to give up"
