"""Declining a card reward must end the room, not spin it.

The livelock, measured live 2026-07-31 (planner_div5.log, 10+ round trips per
second until the run timed out)::

    RUN-RL (reward_screen): action 120 -> choose([0])   # opens the reward
    RUN-RL (card_reward):   action 123 -> skip([])      # skips it
    RUN-RL (reward_screen): action 120 -> choose([0])   # opens it again...

The cause is NOT that the agent forgot it had skipped. Skip inside the card
selection screen is a *cancel*::

    CardRewardAlternative.Generate:
        new CardRewardAlternative("Skip",
            PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward)

    PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward:
        "end card selection, but don't complete it - the player may re-enter
         card selection. Used for the skip button."

``CardReward.OnSelect`` returns ``rewardComplete = false`` for it, so the
reward stays in the set. What actually consumes it is leaving the rewards
screen: ``RewardsSetSynchronizer.SkipRewardsSet`` calls ``OnSkipped()`` on
every reward that is not ``SuccessfullySelected``.

So an agent with perfect memory of "I skipped this" would STILL loop. The skip
has to be turned into a Proceed.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import (
    _CARD_REWARD_LATCH,
    _apply_card_reward_latch,
    _pick_reward_screen_option,
    card_reward_is_declined,
    card_reward_should_force_take,
    note_card_reward_declined,
)
from sts2_env.bridge.protocol import BridgeStateType


def _card_reward(floor=5, act=1):
    return {
        "type": BridgeStateType.CARD_REWARD,
        "floor": floor,
        "act": act,
        "can_skip": True,
        "cards": [{"index": 0, "id": "STRIKE_NECROBINDER", "type": "Attack"}],
    }


def _reward_screen(floor=5, act=1):
    return {
        "type": BridgeStateType.REWARD_SCREEN,
        "floor": floor,
        "act": act,
        "options": [
            {"index": 0, "action": "pick_reward", "enabled": True},
            {"index": 1, "action": "proceed", "enabled": True},
        ],
    }


@pytest.fixture(autouse=True)
def _clear_latch():
    saved = list(_CARD_REWARD_LATCH)
    _CARD_REWARD_LATCH[:] = [None, False, 0]
    yield
    _CARD_REWARD_LATCH[:] = saved


SKIP = {"phase": "run", "method": "skip", "args": []}
OPEN_REWARD = {"phase": "run", "method": "choose", "args": [0]}


def test_declining_then_facing_the_rewards_screen_proceeds():
    _apply_card_reward_latch(_card_reward(), SKIP)
    decoded = _apply_card_reward_latch(_reward_screen(), OPEN_REWARD)
    # index 1 is proceed; index 0 would re-open the reward and loop.
    assert decoded == {"phase": "run", "method": "choose", "args": [1]}


def test_the_heuristic_reward_screen_also_proceeds_once_declined():
    note_card_reward_declined(_card_reward())
    assert _pick_reward_screen_option(_reward_screen()) == 1


def test_without_a_decline_the_rewards_screen_still_opens_the_reward():
    # The fix must not make the agent refuse every card it is offered.
    assert _pick_reward_screen_option(_reward_screen()) == 0
    decoded = _apply_card_reward_latch(_reward_screen(), OPEN_REWARD)
    assert decoded == OPEN_REWARD


def test_the_latch_does_not_survive_into_the_next_room():
    note_card_reward_declined(_card_reward(floor=5))
    assert card_reward_is_declined(_card_reward(floor=5))
    # New floor: this room's card reward has not been declined.
    assert not card_reward_is_declined(_card_reward(floor=6))
    assert _pick_reward_screen_option(_reward_screen(floor=6)) == 0


def test_repeated_reoffers_eventually_force_a_take():
    state = _card_reward()
    for _ in range(agent_runner.CARD_REWARD_MAX_REOFFERS):
        decoded = _apply_card_reward_latch(state, SKIP)
        assert decoded == SKIP, "should still be allowed to skip"
    assert card_reward_should_force_take(state)
    forced = _apply_card_reward_latch(state, SKIP)
    assert forced["method"] == "choose", (
        "after repeated re-offers the agent must take a card rather than "
        "livelock the run"
    )


def test_forcing_a_take_is_bounded_to_one_room():
    state = _card_reward(floor=5)
    for _ in range(agent_runner.CARD_REWARD_MAX_REOFFERS + 2):
        _apply_card_reward_latch(state, SKIP)
    assert card_reward_should_force_take(state)
    # The next room starts clean, so a legitimate skip is still possible.
    assert not card_reward_should_force_take(_card_reward(floor=6))


def test_a_rewards_screen_with_no_proceed_option_is_left_alone():
    # Nothing to proceed to -- overriding to a nonexistent option would be
    # worse than opening the reward.
    note_card_reward_declined(_card_reward())
    state = _reward_screen()
    state["options"] = [{"index": 0, "action": "pick_reward", "enabled": True}]
    assert _apply_card_reward_latch(state, OPEN_REWARD) == OPEN_REWARD
    assert _pick_reward_screen_option(state) == 0
