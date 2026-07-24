"""RunStateAdapter: bridges the C# game's JSON state to the unified
full-run action/observation spaces defined by ``sts2_env.gym_env.run_env``.

``STS2RunEnv`` (``run_env.py``) is the authoritative ground truth for any
"full-run" model trained via ``scripts/train_full_run.py``: a
``Discrete(157)`` action space (combat slice 0-114 reused verbatim from the
combat-only env, plus non-combat slices for map/card-reward/boss-relic/
shop/rest/event/treasure/player-select) and a 151-dim observation (131-dim
combat vector + 20 run-level dims). This adapter always adapts *to*
``run_env.py``'s semantics -- never the other way around -- since existing
trained models depend on that layout staying fixed.

The bridge only ever sees the game's JSON state (no live ``RunManager``), so
this module translates between run_env.py's "local index into
``RunManager.get_available_actions()``" semantics and whatever ordered list
of options the bridge JSON exposes for the same phase (``state["nodes"]``,
``state["options"]``, ``state["cards"]``, ...).

Known, deliberate gaps versus a live ``RunManager`` (see the agent_runner
task report for full detail):

  * The combat bridge protocol (``RlCombatHandler.cs``) only ever exposes a
    single controllable creature and never surfaces mid-combat
    ``pending_choice`` prompts (e.g. Exhume/Warcry-style card selection), so
    the ``player_select`` slice (150-156) is never unmasked and combat
    choice-resolution local indices are only ever meaningful when the
    combat StateAdapter itself reports them (it currently does not).
  * Card bundles (``card_bundle``), multi-card selection prompts
    (``card_select``), and the Crystal Sphere minigame (``crystal_sphere``)
    have no representation at all in run_env.py's action space (its own
    mask/step logic never looks for ``pick_card_bundle`` or per-cell
    "divine_cell" actions). These are therefore handled with a fixed,
    deterministic fallback (never influenced by the model's chosen action)
    rather than invented policy-driven behaviour.
  * The aggregate post-combat "reward screen" (``reward_screen``, offering
    card/potion/relic/gold rewards together with a proceed button) does not
    correspond 1:1 to run_env.py's single-reward-at-a-time CARD_REWARD
    semantics. It is mapped heuristically (first pickable reward vs.
    proceed) -- see the report for details.
  * Most run-level observation dims (act_floor, gold, deck_size,
    relic_count, num_potions, max_potion_slots, ascension_level, is_elite,
    is_boss) are not present anywhere in the current bridge JSON wire
    format and are populated with neutral defaults (0). This is a real
    train/inference distribution-shift risk, documented in the report.
"""

from __future__ import annotations

from typing import Any

import numpy as np

from sts2_env.bridge.protocol import MSG_TYPE_GAME_STATE, BridgeStateType, Phase
from sts2_env.bridge.state_adapter import StateAdapter
from sts2_env.gym_env.observation import OBS_SIZE as COMBAT_OBS_SIZE
from sts2_env.gym_env.run_env import (
    OBS_ACT_FLOOR_SCALE,
    OBS_ASCENSION_SCALE,
    OBS_CURRENT_ACT_SCALE,
    OBS_DECK_SIZE_SCALE,
    OBS_GOLD_SCALE,
    OBS_MAX_POTION_SLOTS_SCALE,
    OBS_RELIC_COUNT_SCALE,
    OBS_TOTAL_FLOOR_SCALE,
    OBS_VALUE_HIGH,
    OBS_VALUE_LOW,
    RUN_OBS_SIZE,
    TOTAL_ACTIONS,
    _LAYOUT,
    _PHASE_INDEX,
)
from sts2_env.run.run_manager import RunManager

DEFAULT_CHOICE_INDEX = 0

# Action-name strings used by the bridge's "reward_screen" / "card_reward"
# payloads (mirrors NonCombatBridgeProtocol constants in
# bridge_mod/RlNonCombatRoomHandlers.cs).
_REWARD_PICK_ACTION = "pick_reward"
_REWARD_PROCEED_ACTION = "proceed"

# Bridge msg_type ("state['type']") -> RunManager phase constant, used for
# the observation's phase one-hot. Mirrors agent_runner._phase_for_state's
# msg_type -> Phase mapping, but resolved to RunManager's own phase names.
_BRIDGE_PHASE_TO_RUN_PHASE: dict[str, str] = {
    BridgeStateType.MAP_SELECT: RunManager.PHASE_MAP_CHOICE,
    BridgeStateType.COMBAT_ACTION: RunManager.PHASE_COMBAT,
    BridgeStateType.REWARD_SCREEN: RunManager.PHASE_CARD_REWARD,
    BridgeStateType.CARD_BUNDLE: RunManager.PHASE_CARD_REWARD,
    BridgeStateType.CARD_REWARD: RunManager.PHASE_CARD_REWARD,
    BridgeStateType.CARD_SELECT: RunManager.PHASE_CARD_REWARD,
    BridgeStateType.REST_SITE: RunManager.PHASE_REST_SITE,
    BridgeStateType.SHOP: RunManager.PHASE_SHOP,
    BridgeStateType.CRYSTAL_SPHERE: RunManager.PHASE_EVENT,
    BridgeStateType.EVENT: RunManager.PHASE_EVENT,
    BridgeStateType.TREASURE: RunManager.PHASE_TREASURE,
    BridgeStateType.BOSS_RELIC: RunManager.PHASE_BOSS_RELIC,
}


class RunStateAdapter:
    """Converts full-run bridge JSON state to/from run_env.py's unified space.

    Usage::

        adapter = RunStateAdapter()
        state = client.receive_state()
        obs = adapter.encode_observation(state)
        mask = adapter.compute_action_mask(state)
        decoded = adapter.decode_action(action_int, state)
    """

    def __init__(self) -> None:
        self._combat_adapter = StateAdapter()

    # ------------------------------------------------------------------
    # Observation
    # ------------------------------------------------------------------

    def encode_observation(self, state: dict[str, Any]) -> np.ndarray:
        """Encode bridge JSON as a flat float32 vector of shape (RUN_OBS_SIZE,)."""
        obs = np.zeros(RUN_OBS_SIZE, dtype=np.float32)

        combat_obs = self._combat_adapter.encode_observation(state)
        n = min(len(combat_obs), COMBAT_OBS_SIZE)
        obs[:n] = combat_obs[:n]

        idx = COMBAT_OBS_SIZE
        run_state = state.get("run_state")
        run_state = run_state if isinstance(run_state, dict) else {}

        act_index = _first_int(state, run_state, key="act", default=1) - 1
        obs[idx + 0] = max(0, act_index) / OBS_CURRENT_ACT_SCALE
        obs[idx + 1] = _first_int(state, run_state, key="floor", default=0) / OBS_TOTAL_FLOOR_SCALE
        # act_floor is not present anywhere in the current bridge wire
        # format; left at 0 (unknown) rather than guessed.
        obs[idx + 2] = _first_int(state, run_state, key="act_floor", default=0) / OBS_ACT_FLOOR_SCALE

        obs[idx + 3] = _hp_ratio(state)
        obs[idx + 4] = _first_int(state, run_state, key="gold", default=0) / OBS_GOLD_SCALE

        obs[idx + 5] = _deck_size(state, run_state) / OBS_DECK_SIZE_SCALE
        obs[idx + 6] = _relic_count(state, run_state) / OBS_RELIC_COUNT_SCALE

        num_potions, max_potion_slots = _potion_counts(state, run_state)
        obs[idx + 7] = num_potions / max(max_potion_slots, 1)
        obs[idx + 8] = max_potion_slots / OBS_MAX_POTION_SLOTS_SCALE

        run_phase = _BRIDGE_PHASE_TO_RUN_PHASE.get(state.get("type", ""))
        phase_idx = _PHASE_INDEX.get(run_phase, 0) if run_phase is not None else 0
        obs[idx + 9 + phase_idx] = 1.0

        obs[idx + 17] = _first_int(state, run_state, key="ascension_level", default=0) / OBS_ASCENSION_SCALE

        room_type = _canonical(state.get("room_type") or run_state.get("room_type"))
        obs[idx + 18] = 1.0 if room_type == "elite" else 0.0
        obs[idx + 19] = 1.0 if room_type == "boss" else 0.0

        np.clip(obs, OBS_VALUE_LOW, OBS_VALUE_HIGH, out=obs)
        return obs

    # ------------------------------------------------------------------
    # Action mask
    # ------------------------------------------------------------------

    def compute_action_mask(self, state: dict[str, Any]) -> np.ndarray:
        """Compute the (TOTAL_ACTIONS,) mask for the current bridge state."""
        mask = np.zeros(TOTAL_ACTIONS, dtype=np.int8)
        layout = _LAYOUT
        msg_type = state.get("type", "")
        phase = _bridge_phase(state)

        if phase in Phase.COMBAT_PHASES:
            combat_mask = self._combat_adapter.compute_action_mask(state)
            n = min(len(combat_mask), layout.combat_size)
            mask[layout.combat_start: layout.combat_start + n] = combat_mask[:n]
            # player_select (150-156): the bridge combat protocol never
            # reports more than one controllable creature, so this slice is
            # intentionally left fully masked out. See module docstring.

        elif phase == Phase.MAP_SELECT:
            nodes = state.get("nodes", []) or []
            n = min(len(nodes), layout.map_size)
            mask[layout.map_start: layout.map_start + n] = 1

        elif phase == Phase.CARD_REWARD:
            self._mask_card_reward(state, msg_type, mask)

        elif phase == Phase.BOSS_RELIC:
            options = _enabled_options(state)
            n = min(len(options), layout.boss_relic_size)
            mask[layout.boss_relic_start: layout.boss_relic_start + n] = 1

        elif phase == Phase.SHOP:
            options = _enabled_options(state)
            mask[layout.shop_start] = 1
            n = min(max(0, len(options) - 1), layout.shop_size - 1)
            mask[layout.shop_start + 1: layout.shop_start + 1 + n] = 1

        elif phase == Phase.REST:
            options = _enabled_options(state)
            n = min(len(options), layout.rest_size)
            mask[layout.rest_start: layout.rest_start + n] = 1

        elif phase == Phase.EVENT:
            if msg_type == BridgeStateType.CRYSTAL_SPHERE:
                # Not representable in run_env.py's action space (no
                # per-cell action exists); always offer a single safe slot.
                mask[layout.event_start] = 1
            else:
                options = _enabled_options(state)
                n = min(len(options), layout.event_size)
                mask[layout.event_start: layout.event_start + n] = 1

        elif phase == Phase.TREASURE:
            mask[layout.treasure_start] = 1

        if mask.sum() == 0:
            mask[0] = 1
        return mask

    def _mask_card_reward(self, state: dict[str, Any], msg_type: str, mask: np.ndarray) -> None:
        layout = _LAYOUT
        if msg_type == BridgeStateType.CARD_REWARD:
            cards = state.get("cards", []) or []
            can_skip = bool(state.get("can_skip", True))
            n = min(len(cards), 3)
            mask[layout.card_reward_start: layout.card_reward_start + n] = 1
            if can_skip:
                mask[layout.card_reward_start + 3] = 1
            extra = max(0, min(len(cards), 6) - 3)
            mask[layout.card_reward_extra_start: layout.card_reward_extra_start + extra] = 1
        elif msg_type == BridgeStateType.REWARD_SCREEN:
            options = _enabled_options(state)
            has_pick = any(_action_of(o) == _REWARD_PICK_ACTION for o in options)
            has_proceed = any(_action_of(o) == _REWARD_PROCEED_ACTION for o in options)
            if has_pick:
                mask[layout.card_reward_start] = 1
            if has_proceed:
                mask[layout.card_reward_start + 3] = 1
            if not has_pick and not has_proceed and options:
                mask[layout.card_reward_start] = 1
        elif msg_type in (BridgeStateType.CARD_BUNDLE, BridgeStateType.CARD_SELECT):
            # Not representable in run_env.py's action space; always take
            # the single, fixed, model-independent fallback action.
            mask[layout.card_reward_start] = 1
        else:
            mask[layout.card_reward_start + 3] = 1

    # ------------------------------------------------------------------
    # Action decode
    # ------------------------------------------------------------------

    def decode_action(self, action: int, state: dict[str, Any]) -> dict[str, Any]:
        """Convert a unified action index to a dispatch command.

        Returns either ``{"phase": "combat", "action": <StateAdapter decode
        dict>}`` (for the reused combat slice) or ``{"phase": "noncombat",
        "method": <STS2GameClient method name>, "args": [...]}``.
        """
        layout = _LAYOUT
        phase = _bridge_phase(state)
        msg_type = state.get("type", "")

        if layout.player_select_start <= action < layout.player_select_start + layout.player_select_size:
            # Not exposed by the bridge combat protocol (single
            # controllable creature only): fall back to whatever the
            # combat adapter resolves for a neutral action.
            return {"phase": "combat", "action": self._combat_adapter.decode_action(0, state)}

        if phase in Phase.COMBAT_PHASES:
            local = _clamp(action - layout.combat_start, layout.combat_size)
            return {"phase": "combat", "action": self._combat_adapter.decode_action(local, state)}

        if phase == Phase.MAP_SELECT:
            nodes = state.get("nodes", []) or []
            if not nodes:
                return _choose(DEFAULT_CHOICE_INDEX)
            local = _clamp(action - layout.map_start, len(nodes))
            return _choose(_read_index(nodes[local], local))

        if phase == Phase.CARD_REWARD:
            return self._decode_card_reward(action, state, msg_type)

        if phase == Phase.BOSS_RELIC:
            options = _enabled_options(state)
            if not options:
                return _choose(DEFAULT_CHOICE_INDEX)
            local = _clamp(action - layout.boss_relic_start, len(options))
            return _choose(_read_index(options[local], local))

        if phase == Phase.SHOP:
            options = _enabled_options(state)
            local = action - layout.shop_start
            if local <= 0 or local >= layout.shop_size or local > len(options) - 1:
                return _choose(0)  # leave shop (bridge option index 0)
            return _choose(_read_index(options[local], local))

        if phase == Phase.REST:
            options = _enabled_options(state)
            if not options:
                return _choose(DEFAULT_CHOICE_INDEX)
            local = _clamp(action - layout.rest_start, len(options))
            return _choose(_read_index(options[local], local))

        if phase == Phase.EVENT:
            if msg_type == BridgeStateType.CRYSTAL_SPHERE:
                return self._decode_crystal_sphere(state)
            options = _enabled_options(state)
            if not options:
                return _choose(DEFAULT_CHOICE_INDEX)
            local = _clamp(action - layout.event_start, len(options))
            return _choose(_read_index(options[local], local))

        if phase == Phase.TREASURE:
            # run_env.py's treasure phase is always a single undifferentiated
            # "collect" action -- no index is ever encoded.
            return _choose(0)

        return _choose(DEFAULT_CHOICE_INDEX)

    def _decode_card_reward(self, action: int, state: dict[str, Any], msg_type: str) -> dict[str, Any]:
        layout = _LAYOUT
        if msg_type == BridgeStateType.CARD_REWARD:
            cards = state.get("cards", []) or []
            can_skip = bool(state.get("can_skip", True))
            if layout.card_reward_extra_start <= action < layout.card_reward_extra_start + layout.card_reward_extra_size:
                idx = 3 + (action - layout.card_reward_extra_start)
                if idx < len(cards):
                    return _choose(_read_index(cards[idx], idx))
                return _skip() if can_skip else _choose(0)
            local = action - layout.card_reward_start
            if 0 <= local < 3 and local < len(cards):
                return _choose(_read_index(cards[local], local))
            return _skip() if can_skip else _choose(0)

        if msg_type == BridgeStateType.REWARD_SCREEN:
            options = _enabled_options(state)
            local = action - layout.card_reward_start
            pick = _first_matching_option(options, actions=(_REWARD_PICK_ACTION,))
            proceed = _first_matching_option(options, actions=(_REWARD_PROCEED_ACTION,))
            if local == 3 or pick is None:
                if proceed is not None:
                    return _choose(_read_index(proceed, 0))
                return _skip()
            return _choose(_read_index(pick, 0))

        if msg_type == BridgeStateType.CARD_BUNDLE:
            bundles = [b for b in state.get("bundles", []) or [] if bool(b.get("enabled", True))]
            if bundles:
                return _choose(_read_index(bundles[0], 0))
            return _skip()

        if msg_type == BridgeStateType.CARD_SELECT:
            indexes = _pick_card_select_indexes(state)
            if not indexes:
                return _skip()
            if len(indexes) == 1:
                return _choose(indexes[0])
            return {"phase": "noncombat", "method": "choose_many", "args": [indexes]}

        return _skip()

    def _decode_crystal_sphere(self, state: dict[str, Any]) -> dict[str, Any]:
        options = _enabled_options(state)
        cell = _first_matching_option(options, actions=("divine_cell",))
        if cell is not None:
            return _choose(_read_index(cell, 0))
        proceed = _first_matching_option(options, actions=(_REWARD_PROCEED_ACTION,))
        if proceed is not None:
            return _choose(_read_index(proceed, 0))
        return _choose(DEFAULT_CHOICE_INDEX)


# ----------------------------------------------------------------
# Bridge phase helper (mirrors agent_runner._phase_for_state)
# ----------------------------------------------------------------


def _bridge_phase(state: dict[str, Any]) -> str:
    msg_type = state.get("type", "")
    return {
        BridgeStateType.COMBAT_ACTION: Phase.COMBAT_PLAY,
        MSG_TYPE_GAME_STATE: state.get("phase", Phase.UNKNOWN),
        BridgeStateType.MAP_SELECT: Phase.MAP_SELECT,
        BridgeStateType.REWARD_SCREEN: Phase.CARD_REWARD,
        BridgeStateType.CARD_BUNDLE: Phase.CARD_REWARD,
        BridgeStateType.CARD_REWARD: Phase.CARD_REWARD,
        BridgeStateType.CARD_SELECT: Phase.CARD_REWARD,
        BridgeStateType.REST_SITE: Phase.REST,
        BridgeStateType.SHOP: Phase.SHOP,
        BridgeStateType.CRYSTAL_SPHERE: Phase.EVENT,
        BridgeStateType.EVENT: Phase.EVENT,
        BridgeStateType.TREASURE: Phase.TREASURE,
        BridgeStateType.BOSS_RELIC: Phase.BOSS_RELIC,
    }.get(msg_type, state.get("phase", Phase.UNKNOWN))


# ----------------------------------------------------------------
# Small, self-contained option/field helpers (deliberately not shared with
# agent_runner.py's heuristic functions, to keep this module importable
# without a circular dependency and to avoid touching the heuristic path).
# ----------------------------------------------------------------


def _choose(index: int) -> dict[str, Any]:
    return {"phase": "noncombat", "method": "choose", "args": [index]}


def _skip() -> dict[str, Any]:
    return {"phase": "noncombat", "method": "skip", "args": []}


def _clamp(value: int, length: int) -> int:
    if length <= 0:
        return 0
    return max(0, min(value, length - 1))


def _enabled_options(state: dict[str, Any]) -> list[dict[str, Any]]:
    return [
        option
        for option in state.get("options", []) or []
        if bool(option.get("enabled", True))
    ]


def _action_of(option: dict[str, Any]) -> str:
    return _canonical(option.get("action"))


def _first_matching_option(
    options: list[dict[str, Any]],
    *,
    actions: tuple[str, ...] = (),
) -> dict[str, Any] | None:
    action_set = {_canonical(value) for value in actions}
    for option in options:
        if action_set and _canonical(option.get("action")) in action_set:
            return option
    return None


def _read_index(option: dict[str, Any], fallback: int) -> int:
    value = option.get("index")
    try:
        return int(value)
    except (TypeError, ValueError):
        return fallback


def _canonical(value: Any) -> str:
    return str(value or "").replace("_", "").replace(" ", "").casefold()


def _pick_card_select_indexes(state: dict[str, Any]) -> list[int]:
    """Pick the minimum required number of cards, front to back.

    ``card_select`` prompts (deck upgrade/transform/enchant/hand-selection)
    have no representation in run_env.py's action space at all, so this is
    a fixed, deterministic fallback rather than a model-driven decision.
    """
    cards = list(state.get("cards", []) or [])
    min_select = max(int(state.get("min_select", 1) or 0), 0)
    max_select = max(int(state.get("max_select", min_select) or 0), 0)
    if not cards or max_select == 0 or min_select == 0:
        return []
    count = min(min_select, max_select, len(cards))
    return [_read_index(card, i) for i, card in enumerate(cards[:count])]


def _first_int(state: dict[str, Any], run_state: dict[str, Any], *, key: str, default: int) -> int:
    for container in (state, run_state):
        if key in container:
            try:
                return int(container[key])
            except (TypeError, ValueError):
                continue
    return default


def _hp_ratio(state: dict[str, Any]) -> float:
    combat = state.get("combat_state") or state
    player = combat.get("player") if isinstance(combat, dict) else None
    if isinstance(player, dict):
        max_hp = player.get("max_hp")
        hp = player.get("hp")
        try:
            if max_hp and int(max_hp) > 0:
                return float(hp) / float(max_hp)
        except (TypeError, ValueError):
            pass
    # Non-combat bridge payloads carry top-level hp/max_hp ints (they must
    # NOT use a "player" dict -- StateAdapter treats any state containing
    # "player" as a combat observation).
    max_hp = state.get("max_hp")
    hp = state.get("hp")
    try:
        if max_hp and int(max_hp) > 0:
            return float(hp) / float(max_hp)
    except (TypeError, ValueError):
        pass
    # Fall back to full health rather than signalling (incorrect) death.
    return 1.0


def _deck_size(state: dict[str, Any], run_state: dict[str, Any]) -> int:
    deck = run_state.get("deck")
    if isinstance(deck, list):
        return len(deck)
    try:
        return int(state.get("deck_size", 0) or 0)
    except (TypeError, ValueError):
        return 0


def _relic_count(state: dict[str, Any], run_state: dict[str, Any]) -> int:
    relics = run_state.get("relics")
    if isinstance(relics, list):
        return len(relics)
    try:
        return int(state.get("relic_count", 0) or 0)
    except (TypeError, ValueError):
        return 0


def _potion_counts(state: dict[str, Any], run_state: dict[str, Any]) -> tuple[int, int]:
    combat = state.get("combat_state") or state
    potions = combat.get("potions") if isinstance(combat, dict) else None
    if not isinstance(potions, list):
        potions = run_state.get("potions")
    num_potions = len([p for p in potions if p]) if isinstance(potions, list) else 0
    max_slots = run_state.get("max_potion_slots") or state.get("max_potion_slots") or 0
    try:
        max_slots = int(max_slots)
    except (TypeError, ValueError):
        max_slots = 0
    return num_potions, max_slots
