"""Tests for the interactive full-run CLI support surface."""

import sts2_env.events  # noqa: F401

from sts2_env.core.enums import RoomType
from sts2_env.potions.base import create_potion
from sts2_env.cli.play_run import describe_card, display_name, display_text
from sts2_env.run.run_manager import RunManager


NEOW_TEST_SEED = 123
COMBAT_TEST_SEED = 456


def test_run_manager_can_start_at_neow_and_force_relic_pick() -> None:
    mgr = RunManager(seed=NEOW_TEST_SEED, character_id="Ironclad", start_with_neow=True)

    assert mgr.phase == RunManager.PHASE_EVENT
    assert [action["action"] for action in mgr.get_available_actions()] == [
        "event_choice",
        "event_choice",
        "event_choice",
    ]

    result = mgr.take_action(mgr.get_available_actions()[0])
    assert result["phase"] == RunManager.PHASE_CARD_REWARD

    actions = mgr.get_available_actions()
    assert [action["action"] for action in actions] == ["pick_relic_reward"]
    assert mgr.take_action({"action": "skip_relic"})["success"] is False

    relic = actions[0]["relic_id"]
    result = mgr.take_action(actions[0])

    assert result["phase"] == RunManager.PHASE_MAP_CHOICE
    assert relic in mgr.run_state.player.relics


def test_run_manager_combat_actions_include_potions() -> None:
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Ironclad")
    mgr._enter_combat(RoomType.MONSTER)
    combat = mgr.get_combat_state()
    assert combat is not None

    combat.potions = [create_potion("FirePotion")]
    actions = mgr.get_available_actions()

    potion_actions = [action for action in actions if action["action"] == "use_potion"]
    assert potion_actions
    assert potion_actions[0]["potion_id"] == "FirePotion"


def test_interactive_cli_uses_player_readable_names() -> None:
    mgr = RunManager(seed=COMBAT_TEST_SEED, character_id="Ironclad")
    card = mgr.run_state.player.deck[0]

    assert display_name("BOOMING_CONCH") == "Booming Conch"
    assert display_text("Obtained relic BOOMING_CONCH.") == "Obtained relic Booming Conch."
    assert display_text("BASH(2E 8dmg)") == "Bash(2E 8dmg)"
    assert "STRIKE_IRONCLAD" not in describe_card(card)
