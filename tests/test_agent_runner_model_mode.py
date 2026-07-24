"""Tests for agent_runner's full-run vs combat-only model auto-detection."""

from __future__ import annotations

from types import SimpleNamespace

import pytest

from sts2_env.bridge.agent_runner import (
    COMBAT_ONLY_ACTION_SPACE_SIZE,
    COMBAT_ONLY_OBS_SIZE,
    FULL_RUN_ACTION_SPACE_SIZE,
    FULL_RUN_OBS_SIZE,
    MODEL_MODE_COMBAT_ONLY,
    MODEL_MODE_FULL_RUN,
    _send_combat_action,
    _send_noncombat_action,
    detect_model_mode,
)
from sts2_env.bridge.protocol import ActionType


def _fake_model(action_n: int, obs_n: int) -> SimpleNamespace:
    return SimpleNamespace(
        action_space=SimpleNamespace(n=action_n),
        observation_space=SimpleNamespace(shape=(obs_n,)),
    )


def test_detect_model_mode_full_run():
    model = _fake_model(FULL_RUN_ACTION_SPACE_SIZE, FULL_RUN_OBS_SIZE)
    assert detect_model_mode(model) == MODEL_MODE_FULL_RUN


def test_detect_model_mode_combat_only():
    model = _fake_model(COMBAT_ONLY_ACTION_SPACE_SIZE, COMBAT_ONLY_OBS_SIZE)
    assert detect_model_mode(model) == MODEL_MODE_COMBAT_ONLY


def test_detect_model_mode_unrecognized_raises():
    model = _fake_model(42, 17)
    with pytest.raises(ValueError):
        detect_model_mode(model)


def test_full_run_and_combat_only_sizes_match_known_constants():
    # Guards against silent drift between agent_runner's detection
    # thresholds and the actual gym env sizes.
    assert FULL_RUN_ACTION_SPACE_SIZE == 157
    assert FULL_RUN_OBS_SIZE == 151
    assert COMBAT_ONLY_ACTION_SPACE_SIZE == 115
    assert COMBAT_ONLY_OBS_SIZE == 131


# ---------------------------------------------------------------------------
# Shared dispatch helpers
# ---------------------------------------------------------------------------


class _FakeClient:
    def __init__(self) -> None:
        self.calls: list[tuple[str, tuple]] = []

    def end_turn(self) -> None:
        self.calls.append(("end_turn", ()))

    def play_card(self, card_index: int, target_index: int = -1) -> None:
        self.calls.append(("play_card", (card_index, target_index)))

    def use_potion(self, slot: int, target_index: int = -1) -> None:
        self.calls.append(("use_potion", (slot, target_index)))

    def choose(self, index: int) -> None:
        self.calls.append(("choose", (index,)))

    def skip(self) -> None:
        self.calls.append(("skip", ()))

    def choose_many(self, indexes: list[int]) -> None:
        self.calls.append(("choose_many", (indexes,)))


def test_send_combat_action_end_turn():
    client = _FakeClient()
    _send_combat_action(client, {"type": ActionType.END_TURN}, combat_delay=0.0)
    assert client.calls == [("end_turn", ())]


def test_send_combat_action_play_card():
    client = _FakeClient()
    _send_combat_action(
        client, {"type": "PLAY", "card_index": 2, "target_index": 1}, combat_delay=0.0
    )
    assert client.calls == [("play_card", (2, 1))]


def test_send_combat_action_potion():
    client = _FakeClient()
    _send_combat_action(
        client,
        {"type": "PLAY", "out_of_hand": True, "potion_slot": 3, "target_index": -1},
        combat_delay=0.0,
    )
    assert client.calls == [("use_potion", (3, -1))]


def test_send_noncombat_action_choose():
    client = _FakeClient()
    _send_noncombat_action(client, {"phase": "noncombat", "method": "choose", "args": [2]})
    assert client.calls == [("choose", (2,))]


def test_send_noncombat_action_skip():
    client = _FakeClient()
    _send_noncombat_action(client, {"phase": "noncombat", "method": "skip", "args": []})
    assert client.calls == [("skip", ())]


def test_send_noncombat_action_choose_many():
    client = _FakeClient()
    _send_noncombat_action(
        client, {"phase": "noncombat", "method": "choose_many", "args": [[0, 2]]}
    )
    assert client.calls == [("choose_many", ([0, 2],))]
