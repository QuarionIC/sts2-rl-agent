"""Tests for RunStateAdapter: bridge JSON <-> full-run unified action space.

Covers observation encoding parity against run_env.py's own semantics,
per-phase action-mask correctness, and action-decode dispatch for each
phase (map, card reward variants, boss relic, shop, rest, event, treasure,
and the reused combat slice).
"""

from __future__ import annotations

import numpy as np

from sts2_env.bridge.protocol import BridgeStateType
from sts2_env.bridge.run_state_adapter import RunStateAdapter
from sts2_env.gym_env.run_env import (
    RUN_OBS_SIZE,
    TOTAL_ACTIONS,
    _BOSS_RELIC_START,
    _CARD_RWD_EXTRA_START,
    _CARD_RWD_START,
    _EVENT_START,
    _MAP_START,
    _PLAYER_SELECT_START,
    _REST_START,
    _SHOP_START,
    _TREASURE_START,
)
from sts2_env.gym_env.observation import OBS_SIZE as COMBAT_OBS_SIZE


def _combat_state(**overrides) -> dict:
    state = {
        "type": BridgeStateType.COMBAT_ACTION,
        "player": {"hp": 60, "max_hp": 80, "block": 0, "energy": 3, "max_energy": 3, "powers": []},
        "hand": [],
        "enemies": [],
        "potions": [],
        "draw_pile_count": 0,
        "discard_pile_count": 0,
        "exhaust_pile_count": 0,
        "act": 1,
        "floor": 3,
    }
    state.update(overrides)
    return state


# ---------------------------------------------------------------------------
# Observation encoding parity
# ---------------------------------------------------------------------------


def test_encode_observation_has_correct_shape():
    adapter = RunStateAdapter()
    obs = adapter.encode_observation(_combat_state())
    assert obs.shape == (RUN_OBS_SIZE,)
    assert obs.dtype == np.float32


def test_encode_observation_reuses_combat_vector_for_first_131_dims():
    adapter = RunStateAdapter()
    state = _combat_state()
    obs = adapter.encode_observation(state)

    # hp/max_hp is dim 0 of the combat vector (see gym_env/observation.py).
    assert obs[0] == 60 / 80

    from sts2_env.bridge.state_adapter import StateAdapter
    combat_only = StateAdapter().encode_observation(state)
    assert np.allclose(obs[:COMBAT_OBS_SIZE], combat_only)


def test_encode_observation_act_and_floor_dims():
    adapter = RunStateAdapter()
    state = _combat_state(act=2, floor=10)
    obs = adapter.encode_observation(state)

    idx = COMBAT_OBS_SIZE
    # act is 1-indexed on the wire; current_act_index is 0-indexed / 3.0.
    assert obs[idx + 0] == 1 / 3.0
    assert obs[idx + 1] == 10 / 50.0


def test_encode_observation_phase_one_hot_map_select():
    adapter = RunStateAdapter()
    state = {"type": BridgeStateType.MAP_SELECT, "nodes": [], "floor": 1, "act": 1}
    obs = adapter.encode_observation(state)

    idx = COMBAT_OBS_SIZE + 9  # phase one-hot starts here
    phase_slice = obs[idx: idx + 8]
    assert phase_slice.sum() == 1.0
    assert phase_slice[0] == 1.0  # MAP_CHOICE is index 0


def test_encode_observation_phase_one_hot_combat():
    adapter = RunStateAdapter()
    state = _combat_state()
    obs = adapter.encode_observation(state)

    idx = COMBAT_OBS_SIZE + 9
    phase_slice = obs[idx: idx + 8]
    assert phase_slice[1] == 1.0  # COMBAT is index 1


def test_encode_observation_hp_ratio_defaults_to_full_outside_combat():
    adapter = RunStateAdapter()
    state = {"type": BridgeStateType.SHOP, "options": [], "floor": 1, "act": 1}
    obs = adapter.encode_observation(state)

    idx = COMBAT_OBS_SIZE + 3
    assert obs[idx] == 1.0


def test_encode_observation_is_clipped():
    adapter = RunStateAdapter()
    state = _combat_state(floor=100000)
    obs = adapter.encode_observation(state)
    assert obs.max() <= 10.0
    assert obs.min() >= -1.0


# ---------------------------------------------------------------------------
# Action mask: per-phase slice correctness
# ---------------------------------------------------------------------------


def _assert_only_slice_set(mask: np.ndarray, start: int, size: int) -> None:
    assert mask.shape == (TOTAL_ACTIONS,)
    outside = np.concatenate([mask[:start], mask[start + size:]])
    assert outside.sum() == 0, "mask has bits set outside the expected slice"


def test_mask_map_select_unmasks_only_map_slice():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.MAP_SELECT,
        "nodes": [{"index": 0, "type": "Monster"}, {"index": 1, "type": "Shop"}],
    }
    mask = adapter.compute_action_mask(state)

    _assert_only_slice_set(mask, _MAP_START, 5)
    assert mask[_MAP_START] == 1
    assert mask[_MAP_START + 1] == 1
    assert mask[_MAP_START + 2] == 0


def test_mask_card_reward_plain_screen():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": 0, "id": "STRIKE"}, {"index": 1, "id": "DEFEND"}],
        "can_skip": True,
    }
    mask = adapter.compute_action_mask(state)

    assert mask[_CARD_RWD_START] == 1
    assert mask[_CARD_RWD_START + 1] == 1
    assert mask[_CARD_RWD_START + 2] == 0
    assert mask[_CARD_RWD_START + 3] == 1  # skip
    assert mask[_CARD_RWD_EXTRA_START: _CARD_RWD_EXTRA_START + 3].sum() == 0


def test_mask_card_reward_plain_screen_no_skip():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": 0, "id": "STRIKE"}],
        "can_skip": False,
    }
    mask = adapter.compute_action_mask(state)
    assert mask[_CARD_RWD_START + 3] == 0


def test_mask_card_reward_extra_cards():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": i} for i in range(5)],
        "can_skip": True,
    }
    mask = adapter.compute_action_mask(state)
    assert mask[_CARD_RWD_START: _CARD_RWD_START + 3].sum() == 3
    assert mask[_CARD_RWD_EXTRA_START] == 1
    assert mask[_CARD_RWD_EXTRA_START + 1] == 1
    assert mask[_CARD_RWD_EXTRA_START + 2] == 0


def test_mask_boss_relic():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.BOSS_RELIC,
        "options": [
            {"index": 0, "action": "pick_relic", "enabled": True},
            {"index": 1, "action": "pick_relic", "enabled": True},
            {"index": 2, "action": "pick_relic", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _BOSS_RELIC_START, 3)
    assert mask[_BOSS_RELIC_START: _BOSS_RELIC_START + 3].sum() == 3


def test_mask_shop_leave_always_unmasked():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.SHOP,
        "options": [
            {"index": 0, "action": "leave_shop", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _SHOP_START, 10)
    assert mask[_SHOP_START] == 1
    assert mask[_SHOP_START + 1] == 0


def test_mask_shop_buyable_items():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.SHOP,
        "options": [
            {"index": 0, "action": "leave_shop", "enabled": True},
            {"index": 1, "action": "buy_card", "enabled": True},
            {"index": 2, "action": "buy_relic", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    assert mask[_SHOP_START] == 1
    assert mask[_SHOP_START + 1] == 1
    assert mask[_SHOP_START + 2] == 1
    assert mask[_SHOP_START + 3] == 0


def test_mask_rest_site():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.REST_SITE,
        "options": [
            {"index": 0, "id": "HEAL", "enabled": True},
            {"index": 1, "id": "SMITH", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _REST_START, 5)
    assert mask[_REST_START: _REST_START + 2].sum() == 2


def test_mask_event():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.EVENT,
        "options": [
            {"index": 0, "action": "event_choice", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _EVENT_START, 4)
    assert mask[_EVENT_START] == 1


def test_mask_crystal_sphere_uses_single_safe_slot():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CRYSTAL_SPHERE,
        "options": [
            {"index": 0, "action": "divine_cell", "enabled": True},
            {"index": 1, "action": "divine_cell", "enabled": True},
        ],
    }
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _EVENT_START, 4)
    assert mask[_EVENT_START] == 1
    assert mask[_EVENT_START + 1] == 0


def test_mask_treasure_always_single_slot():
    adapter = RunStateAdapter()
    state = {"type": BridgeStateType.TREASURE, "options": [{"index": 0, "action": "collect"}]}
    mask = adapter.compute_action_mask(state)
    _assert_only_slice_set(mask, _TREASURE_START, 1)
    assert mask[_TREASURE_START] == 1


def test_mask_combat_delegates_to_state_adapter_and_leaves_player_select_empty():
    adapter = RunStateAdapter()
    state = _combat_state()
    mask = adapter.compute_action_mask(state)

    assert mask[0] == 1  # END_TURN always valid
    assert mask[_PLAYER_SELECT_START: _PLAYER_SELECT_START + 7].sum() == 0


def test_mask_never_all_zero():
    adapter = RunStateAdapter()
    state = {"type": "some_unknown_type"}
    mask = adapter.compute_action_mask(state)
    assert mask.sum() >= 1


# ---------------------------------------------------------------------------
# Action decode round-tripping
# ---------------------------------------------------------------------------


def test_decode_map_select_chooses_bridge_index():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.MAP_SELECT,
        "nodes": [{"index": 0, "type": "Monster"}, {"index": 1, "type": "Shop"}],
    }
    decoded = adapter.decode_action(_MAP_START + 1, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_card_reward_plain_pick():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": 0}, {"index": 1}],
        "can_skip": True,
    }
    decoded = adapter.decode_action(_CARD_RWD_START + 1, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_card_reward_plain_skip():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": 0}, {"index": 1}],
        "can_skip": True,
    }
    decoded = adapter.decode_action(_CARD_RWD_START + 3, state)
    assert decoded == {"phase": "noncombat", "method": "skip", "args": []}


def test_decode_card_reward_extra_card():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.CARD_REWARD,
        "cards": [{"index": i} for i in range(5)],
        "can_skip": True,
    }
    decoded = adapter.decode_action(_CARD_RWD_EXTRA_START, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [3]}


def test_decode_card_reward_reward_screen_pick_and_proceed():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.REWARD_SCREEN,
        "options": [
            {"index": 0, "action": "pick_reward", "enabled": True},
            {"index": 1, "action": "proceed", "enabled": True},
        ],
    }
    pick = adapter.decode_action(_CARD_RWD_START, state)
    assert pick == {"phase": "noncombat", "method": "choose", "args": [0]}

    proceed = adapter.decode_action(_CARD_RWD_START + 3, state)
    assert proceed == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_boss_relic():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.BOSS_RELIC,
        "options": [
            {"index": 0, "action": "pick_relic"},
            {"index": 1, "action": "pick_relic"},
        ],
    }
    decoded = adapter.decode_action(_BOSS_RELIC_START + 1, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_shop_leave_and_buy():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.SHOP,
        "options": [
            {"index": 0, "action": "leave_shop"},
            {"index": 1, "action": "buy_card"},
        ],
    }
    leave = adapter.decode_action(_SHOP_START, state)
    assert leave == {"phase": "noncombat", "method": "choose", "args": [0]}

    buy = adapter.decode_action(_SHOP_START + 1, state)
    assert buy == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_rest_site():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.REST_SITE,
        "options": [{"index": 0, "id": "HEAL"}, {"index": 1, "id": "SMITH"}],
    }
    decoded = adapter.decode_action(_REST_START + 1, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [1]}


def test_decode_event():
    adapter = RunStateAdapter()
    state = {
        "type": BridgeStateType.EVENT,
        "options": [{"index": 0, "action": "event_choice"}],
    }
    decoded = adapter.decode_action(_EVENT_START, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [0]}


def test_decode_treasure_always_collects_first():
    adapter = RunStateAdapter()
    state = {"type": BridgeStateType.TREASURE, "options": [{"index": 0, "action": "collect"}]}
    decoded = adapter.decode_action(_TREASURE_START, state)
    assert decoded == {"phase": "noncombat", "method": "choose", "args": [0]}


def test_decode_combat_delegates_to_state_adapter():
    adapter = RunStateAdapter()
    state = _combat_state()
    decoded = adapter.decode_action(0, state)
    assert decoded["phase"] == "combat"
    assert decoded["action"] == {"type": "END_TURN"}


def test_decode_player_select_falls_back_to_neutral_combat_action():
    adapter = RunStateAdapter()
    state = _combat_state()
    decoded = adapter.decode_action(_PLAYER_SELECT_START, state)
    assert decoded["phase"] == "combat"
