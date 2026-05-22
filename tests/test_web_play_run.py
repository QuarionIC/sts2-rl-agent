"""Tests for the local full-run web UI state surface."""

import sts2_env.events  # noqa: F401

from sts2_env.core.enums import RoomType
from sts2_env.run.run_manager import RunManager
from sts2_env.web.play_run import RunSession, serialize_run


def test_web_session_starts_at_neow_and_advances_to_map() -> None:
    session = RunSession()

    state = session.start(character="Ironclad", seed=123)
    assert state["phase"] == RunManager.PHASE_EVENT
    assert state["screen"]["type"] == "event"
    assert state["screen"]["title"] == "Neow"
    assert state["actions"][0]["label"] == "Booming Conch - Gain a positive relic"

    reward = session.take_action(0)
    assert reward["phase"] == RunManager.PHASE_CARD_REWARD
    assert reward["screen"]["type"] == "reward"
    assert reward["screen"]["items"] == ["Booming Conch"]

    map_state = session.take_action(0)
    assert map_state["phase"] == RunManager.PHASE_MAP_CHOICE
    assert map_state["screen"]["type"] == "map"
    assert map_state["screen"]["paths"]


def test_web_state_serializes_combat_for_browser_display() -> None:
    mgr = RunManager(seed=456, character_id="Ironclad")
    mgr._enter_combat(RoomType.MONSTER)
    actions = mgr.get_available_actions()

    state = serialize_run(
        mgr,
        actions,
        seed=456,
        character="Ironclad",
        ascension=0,
        last_description="",
    )

    assert state["screen"]["type"] == "combat"
    assert state["screen"]["enemies"]
    assert "intent" in state["screen"]["enemies"][0]
    assert any(action["kind"] == "play_card" for action in state["actions"])
