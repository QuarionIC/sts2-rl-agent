"""Agent runner: connects a trained RL model to the real STS2 game.

Main loop:
  1. Connect to the game via TCP (bridge mod must be installed & running)
  2. Load a trained MaskablePPO model
  3. Receive game state -> encode observation -> model.predict -> send action
  4. Handle all game phases (combat, map, rewards, shop, rest, events)

Usage:
  python -m sts2_env.bridge.agent_runner --model-path models/combat_ppo.zip
  python -m sts2_env.bridge.agent_runner --model-path models/full_run_ppo.zip --port 9002

Two kinds of models are supported, auto-detected from the loaded model's
``action_space``/``observation_space`` shape (see :func:`detect_model_mode`):

  * **Combat-only** models (``Discrete(115)`` / obs size 131, trained via
    ``CombatEnv``): the trained policy drives combat only; every non-combat
    phase (map navigation, card rewards, shop, rest, events, treasure, boss
    relics) is handled by simple hardcoded heuristics.
  * **Full-run** models (``Discrete(157)`` / obs size 151, trained via
    :class:`sts2_env.gym_env.run_env.STS2RunEnv` with
    ``scripts/train_full_run.py``): the trained policy drives *every*
    decision, combat and non-combat alike, via
    :class:`sts2_env.bridge.run_state_adapter.RunStateAdapter`. The
    heuristic functions are not used at all in this mode. See that module's
    docstring for known bridge-protocol gaps (multi-creature player
    selection, card bundles, the Crystal Sphere minigame, and several
    run-level observation fields are not fully representable with the
    current bridge JSON).
"""

from __future__ import annotations

import argparse
import logging
import sys
import time
from typing import Any

from sts2_env.bridge.client import STS2GameClient
from sts2_env.bridge.protocol import (
    ActionType,
    BridgeStateType,
    MSG_TYPE_ERROR,
    MSG_TYPE_GAME_STATE,
    MSG_TYPE_PONG,
    Phase,
)
from sts2_env.bridge.run_state_adapter import RunStateAdapter
from sts2_env.bridge.state_adapter import StateAdapter
from sts2_env.core.constants import ACTION_SPACE_SIZE as COMBAT_ONLY_ACTION_SPACE_SIZE
from sts2_env.gym_env.observation import OBS_SIZE as COMBAT_ONLY_OBS_SIZE
from sts2_env.gym_env.run_env import RUN_OBS_SIZE as FULL_RUN_OBS_SIZE
from sts2_env.gym_env.run_env import TOTAL_ACTIONS as FULL_RUN_ACTION_SPACE_SIZE
from sts2_env.parity.bridge_replay import BridgeReplayRecorder

logger = logging.getLogger(__name__)

DEFAULT_CHOICE_INDEX = 0
CARD_REWARD_LARGE_DECK_SIZE = 30
REST_HP_RATIO_THRESHOLD = 0.5
TERMINAL_PHASES = frozenset({
    BridgeStateType.GAME_OVER,
    BridgeStateType.RUN_COMPLETE,
})

MODEL_MODE_COMBAT_ONLY = "combat_only"
MODEL_MODE_FULL_RUN = "full_run"

ROOM_PRIORITY_HEALTHY = (
    "boss",
    "elite",
    "monster",
    "event",
    "unknown",
    "treasure",
    "shop",
    "restsite",
)
ROOM_PRIORITY_LOW_HP = (
    "restsite",
    "shop",
    "treasure",
    "monster",
    "event",
    "unknown",
    "elite",
    "boss",
)
CARD_REWARD_TYPE_PRIORITY = ("power", "attack", "skill")
SHOP_PURCHASE_ACTION_PRIORITY = (
    "buy_relic",
    "buy_card",
    "buy_potion",
    "remove_card",
    "buy_item",
)
SHOP_LEAVE_ACTION = "leave_shop"
REWARD_PROCEED_ACTION = "proceed"
REWARD_PICK_ACTION = "pick_reward"
CARD_BUNDLE_PICK_ACTION = "pick_card_bundle"
CRYSTAL_SPHERE_CELL_ACTION = "divine_cell"
REST_HEAL_OPTION_ID = "heal"
REST_SMITH_OPTION_ID = "smith"
TREASURE_COLLECT_ACTION = "collect"
BOSS_RELIC_PICK_ACTION = "pick_relic"


def load_model(model_path: str) -> Any:
    """Load a trained MaskablePPO model.

    Args:
        model_path: Path to the saved model (.zip file).

    Returns:
        Loaded MaskablePPO model instance.
    """
    try:
        from sb3_contrib import MaskablePPO
    except ImportError:
        logger.error(
            "sb3-contrib is required. Install with: pip install sb3-contrib"
        )
        raise

    logger.info("Loading model from %s", model_path)
    model = MaskablePPO.load(model_path)
    logger.info("Model loaded successfully.")
    return model


def detect_model_mode(model: Any) -> str:
    """Detect whether *model* is a full-run or combat-only trained model.

    The two model types were trained against different Gymnasium action /
    observation spaces and cannot be told apart any other way:

      * Combat-only (``CombatEnv``): ``action_space.n`` ==
        :data:`COMBAT_ONLY_ACTION_SPACE_SIZE` (115), observation size ==
        :data:`COMBAT_ONLY_OBS_SIZE` (131).
      * Full-run (:class:`sts2_env.gym_env.run_env.STS2RunEnv`):
        ``action_space.n`` == :data:`FULL_RUN_ACTION_SPACE_SIZE` (157),
        observation size == :data:`FULL_RUN_OBS_SIZE` (151).

    Returns:
        ``MODEL_MODE_FULL_RUN`` or ``MODEL_MODE_COMBAT_ONLY``.

    Raises:
        ValueError: If the model's spaces match neither known layout.
    """
    action_n = int(model.action_space.n)
    obs_n = int(model.observation_space.shape[0])

    if action_n == FULL_RUN_ACTION_SPACE_SIZE and obs_n == FULL_RUN_OBS_SIZE:
        return MODEL_MODE_FULL_RUN
    if action_n == COMBAT_ONLY_ACTION_SPACE_SIZE and obs_n == COMBAT_ONLY_OBS_SIZE:
        return MODEL_MODE_COMBAT_ONLY

    raise ValueError(
        f"Unrecognized model action/observation space: action_space.n={action_n}, "
        f"observation_space.shape[0]={obs_n}. Expected a combat-only model "
        f"({COMBAT_ONLY_ACTION_SPACE_SIZE} actions / {COMBAT_ONLY_OBS_SIZE} obs dims) "
        f"or a full-run model ({FULL_RUN_ACTION_SPACE_SIZE} actions / "
        f"{FULL_RUN_OBS_SIZE} obs dims)."
    )


def run_agent(
    model_path: str | None = None,
    host: str = "127.0.0.1",
    port: int = 9002,
    deterministic: bool = True,
    verbose: bool = False,
    record_replay_path: str | None = None,
    replay_factory: str | None = None,
    action_delay: float = 0.0,
    combat_delay: float = 0.0,
    llm_model: str | None = None,
    llm_gpu_layers: int = 34,
    llm_ctx: int = 4096,
    llm_max_tokens: int = 48,
    llm_temperature: float = 0.2,
    combat_policy: str = "planner",
    rl_combat_model: str | None = None,
) -> None:
    """Main agent loop.

    Connects to the game, loads the model, and plays indefinitely
    until disconnected or interrupted.

    Args:
        model_path: Path to saved MaskablePPO model.
        host: Bridge server host.
        port: Bridge server port.
        deterministic: Whether to use deterministic action selection.
        verbose: Whether to log every action taken.
        action_delay: Seconds to pause before each non-combat decision.
        combat_delay: Seconds to pause before each combat action (end turn is instant).
    """
    _COMBAT_POLICY[0] = combat_policy
    _COMBAT_POLICY[1] = None
    logger.info("Combat policy: %s | out-of-combat: %s",
                combat_policy, "LLM" if llm_model else "model/heuristic")

    if combat_policy == "rl":
        if not rl_combat_model:
            raise SystemExit("--combat-policy rl requires --rl-combat-model")
        from sb3_contrib import MaskablePPO

        logger.info("Loading RL combat agent: %s", rl_combat_model)
        _RL_COMBAT[0] = MaskablePPO.load(rl_combat_model, device="cpu")
        logger.info("RL combat agent loaded (actions=%d, obs=%d)",
                    _RL_COMBAT[0].policy.action_space.n,
                    _RL_COMBAT[0].observation_space.shape[0])

    llm_policy = None
    if llm_model:
        # LLM drives every decision; no MaskablePPO model is loaded. The
        # heuristic _pick_* helpers stay in place as the fallback for any
        # reply that fails to parse.
        from sts2_env.bridge.llm_policy import BridgeLLMPolicy
        from sts2_env.llm.runner import LLMConfig, LocalLLM

        logger.info("Loading local LLM: %s", llm_model)
        _llm = LocalLLM(LLMConfig(
            model_path=llm_model, n_ctx=llm_ctx, n_gpu_layers=llm_gpu_layers,
            max_tokens=llm_max_tokens, temperature=llm_temperature,
        ))
        llm_policy = BridgeLLMPolicy(_llm)
        logger.info("LLM loaded in %.0fs -- driving all decisions", _llm.load_s)

    model = None
    model_mode = None
    if model_path:
        model = load_model(model_path)
        model_mode = detect_model_mode(model)
    elif llm_policy is None and combat_policy not in ("planner", "rl"):
        raise SystemExit(
            "Provide --model-path, --llm-model, or --combat-policy planner")

    adapter = StateAdapter()
    run_state_adapter: RunStateAdapter | None = None
    if model_mode == MODEL_MODE_FULL_RUN:
        run_state_adapter = RunStateAdapter()
        logger.info(
            "Loaded full-run model (action_space=%d, obs=%d) -- "
            "driving all phases via the trained policy.",
            FULL_RUN_ACTION_SPACE_SIZE, FULL_RUN_OBS_SIZE,
        )
    elif model is None:
        logger.info(
            "No MaskablePPO model loaded -- combat: %s, non-combat: %s.",
            combat_policy, "LLM" if llm_model else "heuristics",
        )
    else:
        logger.info(
            "Loaded combat-only model (action_space=%d, obs=%d) -- "
            "using heuristics for non-combat phases.",
            COMBAT_ONLY_ACTION_SPACE_SIZE, COMBAT_ONLY_OBS_SIZE,
        )

    logger.info("Connecting to STS2 at %s:%d...", host, port)

    with STS2GameClient(host=host, port=port) as raw_client:
        client: STS2GameClient | BridgeReplayRecorder
        if record_replay_path is not None:
            metadata = {
                "model_path": model_path,
                "host": host,
                "port": port,
            }
            if replay_factory is not None:
                metadata["scenario_factory"] = replay_factory
            client = BridgeReplayRecorder(raw_client, metadata=metadata)
            logger.info("Recording supported bridge states to %s", record_replay_path)
        else:
            client = raw_client
        logger.info("Connected. Starting agent loop.")

        step_count = 0
        combat_count = 0

        try:
            while True:
                try:
                    logger.info("Waiting for game state...")
                    state = client.receive_state()
                    logger.info("Received: type=%s", state.get("type", "?"))
                except TimeoutError:
                    logger.warning("Timeout waiting for state. Sending ping...")
                    if client.ping():
                        continue
                    else:
                        logger.error("Lost connection. Attempting reconnect...")
                        _reconnect_with_retry(client)
                        continue
                except ConnectionError:
                    logger.error("Connection lost. Attempting reconnect...")
                    _reconnect_with_retry(client)
                    continue

                msg_type = state.get("type", "")
                phase = _phase_for_state(state)
                step_count += 1

                if verbose and step_count % 10 == 1:
                    logger.info("Step %d: type=%s phase=%s", step_count, msg_type, phase)

                if verbose and msg_type:
                    logger.debug("Received: type=%s keys=%s", msg_type, list(state.keys()))

                if phase == MSG_TYPE_PONG:
                    continue
                if phase in TERMINAL_PHASES:
                    logger.info("Run finished: %s", state.get("result", state.get("message", "unknown")))
                    break
                if phase == MSG_TYPE_ERROR:
                    logger.warning("Game error: %s", state.get("message", ""))
                    continue

                # Pause before non-combat decisions so a human can follow along.
                if (
                    action_delay > 0
                    and phase != Phase.COMBAT_WAITING
                    and phase not in Phase.COMBAT_PHASES
                ):
                    time.sleep(action_delay)

                if llm_policy is not None:
                    _LLM_POLICY[0] = llm_policy

                # ---- Combat: deterministic per-turn planner ----
                # Must come BEFORE the model branches. In LLM mode there is
                # no MaskablePPO model at all, so the old combat branch below
                # would dereference None; and previously nothing routed
                # combat to the planner, which is why live play showed no
                # planner behaviour despite --combat-policy planner.
                if phase in Phase.COMBAT_PHASES and _COMBAT_POLICY[0] == "rl":
                    act_idx = _rl_combat_action(state)
                    if act_idx is not None:
                        decoded = adapter.decode_action(act_idx, state)
                        if verbose:
                            _log_combat_action(state, act_idx, decoded)
                        _send_combat_action(client, decoded, combat_delay)
                        continue
                    act_idx = _heuristic_combat_action(state, adapter)
                    if act_idx is not None:
                        decoded = adapter.decode_action(act_idx, state)
                        _send_combat_action(client, decoded, combat_delay)
                    else:
                        client.end_turn()
                    continue

                if phase in Phase.COMBAT_PHASES and _COMBAT_POLICY[0] == "planner":
                    planned = _combat_planner_action(state)
                    if planned is not None:
                        decoded = adapter.decode_action(planned, state)
                        if verbose:
                            _log_combat_action(state, planned, decoded)
                        _send_combat_action(client, decoded, combat_delay)
                        continue
                    # Planner unavailable (payload lacks pile data): fall
                    # through to the LLM/model paths below.
                    if llm_policy is not None:
                        act_idx = _llm_combat_action(state, adapter)
                        if act_idx is not None:
                            decoded = adapter.decode_action(act_idx, state)
                            _send_combat_action(client, decoded, combat_delay)
                            continue
                        client.end_turn()
                        continue
                    if model is None:
                        # Planner declined (e.g. an unrecognised modded
                        # monster). Play a heuristic action rather than
                        # ending the turn -- passing every turn lost a live
                        # run outright.
                        act_idx = _heuristic_combat_action(state, adapter)
                        if act_idx is not None:
                            decoded = adapter.decode_action(act_idx, state)
                            if verbose:
                                _log_combat_action(state, act_idx, decoded)
                            _send_combat_action(client, decoded, combat_delay)
                        else:
                            client.end_turn()
                        continue

                if phase in Phase.COMBAT_PHASES and llm_policy is not None:
                    act_idx = _llm_combat_action(state, adapter)
                    if act_idx is not None:
                        decoded = adapter.decode_action(act_idx, state)
                        _send_combat_action(client, decoded, combat_delay)
                    else:
                        client.end_turn()
                    continue

                if run_state_adapter is not None:
                    # ---- Full-run model: trained policy drives every phase ----
                    if phase == Phase.COMBAT_WAITING:
                        # Game is processing enemy turn / animations — just wait
                        pass
                    elif phase in Phase.ACTIONABLE:
                        obs = run_state_adapter.encode_observation(state)
                        mask = run_state_adapter.compute_action_mask(state)

                        action, _states = model.predict(
                            obs,
                            action_masks=mask,
                            deterministic=deterministic,
                        )
                        action_int = int(action)

                        decoded = run_state_adapter.decode_action(action_int, state)

                        if decoded["phase"] == "combat":
                            combat_decoded = decoded["action"]
                            if verbose:
                                _log_combat_action(state, action_int, combat_decoded)
                            _send_combat_action(client, combat_decoded, combat_delay)
                        else:
                            if verbose:
                                logger.info(
                                    "%s (%s): model action %d -> %s(%s)",
                                    phase, msg_type, action_int,
                                    decoded["method"], decoded.get("args"),
                                )
                            _send_noncombat_action(client, decoded)
                    else:
                        logger.debug("Unknown phase '%s', waiting...", phase)

                elif phase in Phase.COMBAT_PHASES:
                    # ---- Combat-only model: use trained model for combat ----
                    obs = adapter.encode_observation(state)
                    mask = adapter.compute_action_mask(state)

                    # Ensure at least one action is valid
                    if mask.sum() == 0:
                        logger.warning("No valid actions! Defaulting to END_TURN.")
                        client.end_turn()
                        continue

                    action, _states = model.predict(
                        obs,
                        action_masks=mask,
                        deterministic=deterministic,
                    )
                    action_int = int(action)

                    decoded = adapter.decode_action(action_int, state)

                    if verbose:
                        _log_combat_action(state, action_int, decoded)

                    _send_combat_action(client, decoded, combat_delay)

                elif phase == Phase.MAP_SELECT:
                    choice = _pick_map_node(state)
                    if verbose:
                        logger.info("MAP: choosing node %d", choice)
                    client.choose(choice)

                elif phase == Phase.CARD_REWARD:
                    if msg_type == BridgeStateType.CARD_BUNDLE:
                        choice = _pick_card_bundle_index(state)
                        if verbose:
                            logger.info("CARD_BUNDLE: choosing bundle %s", choice)
                        client.choose(choice)
                    elif msg_type == BridgeStateType.CARD_SELECT:
                        indexes = _pick_card_select_indexes(state)
                        if verbose:
                            logger.info("CARD_SELECT: choosing indexes %s", indexes)
                        if not indexes:
                            client.skip()
                        elif len(indexes) == 1:
                            client.choose(indexes[0])
                        else:
                            client.choose_many(indexes)
                    else:
                        choice = (
                            _pick_reward_screen_option(state)
                            if msg_type == BridgeStateType.REWARD_SCREEN
                            else _pick_card_reward_index(state)
                        )
                        if verbose:
                            logger.info("CARD_REWARD: choosing option %s", choice)
                        _send_choice_or_skip(client, choice)

                elif phase == Phase.REST:
                    choice = _pick_rest_option(state)
                    if verbose:
                        logger.info("REST: choosing option %d", choice)
                    client.choose(choice)

                elif phase == Phase.SHOP:
                    choice = _pick_shop_option(state)
                    if verbose:
                        logger.info("SHOP: choosing option %d", choice)
                    client.choose(choice)

                elif phase == Phase.EVENT:
                    choice = (
                        _pick_crystal_sphere_option(state)
                        if msg_type == BridgeStateType.CRYSTAL_SPHERE
                        else _pick_event_option(state)
                    )
                    if verbose:
                        logger.info("EVENT: choosing option %d", choice)
                    client.choose(choice)

                elif phase == Phase.TREASURE:
                    choice = _pick_treasure_option(state)
                    if verbose:
                        logger.info("TREASURE: choosing option %d", choice)
                    client.choose(choice)

                elif phase == Phase.BOSS_RELIC:
                    choice = _pick_boss_relic_option(state)
                    if verbose:
                        logger.info("BOSS_RELIC: choosing option %d", choice)
                    client.choose(choice)

                elif phase == Phase.COMBAT_WAITING:
                    # Game is processing enemy turn / animations — just wait
                    pass

                else:
                    logger.debug("Unknown phase '%s', waiting...", phase)

                # Log progress periodically
                if step_count % 100 == 0:
                    logger.info("Step %d, combats seen: %d", step_count, combat_count)
        finally:
            if isinstance(client, BridgeReplayRecorder):
                saved_path = client.save(record_replay_path)
                logger.info("Saved bridge replay trace to %s", saved_path)


# ----------------------------------------------------------------
# Shared dispatch helpers (used by both the combat-only and full-run paths)
# ----------------------------------------------------------------


def _send_combat_action(client: Any, decoded: dict[str, Any], combat_delay: float) -> None:
    """Send a StateAdapter/RunStateAdapter-decoded combat action to the client.

    *decoded* has the shape produced by :meth:`StateAdapter.decode_action`
    (``{"type": ActionType.END_TURN}``, or a ``PLAY`` dict with either
    ``card_index``/``target_index`` or ``out_of_hand``/``slot``/
    ``target_index`` for potions).
    """
    # Small pause before combat actions; ending the turn is instant.
    if combat_delay > 0 and decoded["type"] != ActionType.END_TURN:
        time.sleep(combat_delay)

    if decoded["type"] == ActionType.END_TURN:
        client.end_turn()
    elif decoded.get("out_of_hand"):
        client.use_potion(
            decoded.get("slot", decoded.get("potion_slot", -1)),
            decoded.get("target_index", -1),
        )
    else:
        client.play_card(
            decoded["card_index"],
            decoded.get("target_index", -1),
        )


def _send_noncombat_action(client: Any, decoded: dict[str, Any]) -> None:
    """Send a RunStateAdapter non-combat-decoded action to the client.

    *decoded* has the shape ``{"phase": "noncombat", "method": <client
    method name>, "args": [...]}`` as produced by
    :meth:`RunStateAdapter.decode_action`.
    """
    method = getattr(client, decoded["method"])
    method(*decoded.get("args", []))


# ----------------------------------------------------------------
# Heuristic decision functions for non-combat phases
# ----------------------------------------------------------------


def _phase_for_state(state: dict[str, Any]) -> str:
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
        BridgeStateType.GAME_OVER: BridgeStateType.GAME_OVER,
        BridgeStateType.RUN_COMPLETE: BridgeStateType.RUN_COMPLETE,
        MSG_TYPE_PONG: MSG_TYPE_PONG,
        MSG_TYPE_ERROR: MSG_TYPE_ERROR,
    }.get(msg_type, state.get("phase", Phase.UNKNOWN))


def _pick_map_node_heuristic(state: dict[str, Any]) -> int:
    """Choose a reachable map node from the bridge state's node list."""
    nodes = list(state.get("nodes", []))
    if not nodes:
        return DEFAULT_CHOICE_INDEX
    hp_ratio = _read_hp_ratio(state)
    priority = (
        ROOM_PRIORITY_LOW_HP
        if hp_ratio is not None and hp_ratio < REST_HP_RATIO_THRESHOLD
        else ROOM_PRIORITY_HEALTHY
    )
    for room_type in priority:
        for fallback_index, node in enumerate(nodes):
            if _canonical_text(node.get("type")) == room_type:
                return _read_index(node, fallback_index)
    return _read_index(nodes[0], DEFAULT_CHOICE_INDEX)



def _pick_map_node(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_map_node_heuristic(state)
    return _llm_pick(state, list(state.get("nodes", [])), 'Which room do you move to next?', _fb, 'MAP')

def _pick_card_select_indexes(state: dict[str, Any]) -> list[int]:
    """Choose required card indexes for upgrade/transform/select screens."""
    cards = list(state.get("cards", []))
    min_select = max(int(state.get("min_select", 1)), 0)
    max_select = max(int(state.get("max_select", min_select)), 0)
    if not cards or max_select == 0 or min_select == 0:
        return []
    count = min(min_select, max_select, len(cards))
    return [_read_index(card, fallback) for fallback, card in enumerate(cards[:count])]


def _pick_card_reward_index_heuristic(state: dict[str, Any]) -> int | None:
    """Choose a card reward, or return None when skipping is the best action."""
    cards = list(state.get("cards", []))
    can_skip = bool(state.get("can_skip", False))
    if not cards:
        return None if can_skip else DEFAULT_CHOICE_INDEX
    if can_skip and _read_deck_size(state) > CARD_REWARD_LARGE_DECK_SIZE:
        return None
    for card_type in CARD_REWARD_TYPE_PRIORITY:
        for fallback_index, card in enumerate(cards):
            if _canonical_text(card.get("type")) == card_type:
                return _read_index(card, fallback_index)
    return _read_index(cards[0], DEFAULT_CHOICE_INDEX)



def _pick_card_reward_index(state: dict[str, Any]) -> int | None:
    """LLM card pick. Returns None to skip, matching the heuristic contract.

    Skip is presented as an explicit menu entry rather than inferred: the
    in-sim work showed card-taking is the single decision that most affects
    run depth, so the model must be able to choose it deliberately.
    """
    _fb = _pick_card_reward_index_heuristic(state)
    policy = _LLM_POLICY[0]
    cards = list(state.get("cards", []) or [])
    if policy is None or not cards:
        return _fb
    from sts2_env.bridge.llm_policy import render_options

    menu = list(cards) + [{"label": "Skip (take no card)"}]
    prompt = render_options(state, menu, "Which card reward do you take?")
    local = policy.pick(prompt, menu, lambda: 0, tag="CARD_REWARD")
    if local == len(cards):
        return None
    if 0 <= local < len(cards):
        return _read_index(cards[local], local)
    return _fb

def _pick_reward_screen_option(state: dict[str, Any]) -> int:
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX
    option = _first_matching_option(options, actions=(REWARD_PICK_ACTION,))
    if option is not None:
        return _read_index(option, DEFAULT_CHOICE_INDEX)
    option = _first_matching_option(options, actions=(REWARD_PROCEED_ACTION,)) or options[0]
    return _read_index(option, DEFAULT_CHOICE_INDEX)


def _pick_card_bundle_index(state: dict[str, Any]) -> int:
    bundles = [
        bundle
        for bundle in state.get("bundles", [])
        if bool(bundle.get("enabled", True))
    ]
    if not bundles:
        return DEFAULT_CHOICE_INDEX
    option = _first_matching_option(bundles, actions=(CARD_BUNDLE_PICK_ACTION,))
    if option is None:
        option = bundles[0]
    return _read_index(option, DEFAULT_CHOICE_INDEX)


def _pick_rest_option_heuristic(state: dict[str, Any]) -> int:
    """Choose a rest-site option by option identity, not display order."""
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX
    hp_ratio = _read_hp_ratio(state)
    preferred = (
        REST_HEAL_OPTION_ID
        if hp_ratio is not None and hp_ratio < REST_HP_RATIO_THRESHOLD
        else REST_SMITH_OPTION_ID
    )
    option = _first_matching_option(options, option_ids=(preferred,))
    if option is None and preferred == REST_SMITH_OPTION_ID:
        option = _first_matching_option(options, option_ids=(REST_HEAL_OPTION_ID,))
    if option is None:
        option = options[0]
    return _read_index(option, DEFAULT_CHOICE_INDEX)



def _pick_rest_option(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_rest_option_heuristic(state)
    return _llm_pick(state, _enabled_options(state), 'What do you do at the rest site?', _fb, 'REST')

def _pick_shop_option_heuristic(state: dict[str, Any]) -> int:
    """Buy an enabled shop item when one exists; leave when only exit remains."""
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX
    for action in SHOP_PURCHASE_ACTION_PRIORITY:
        option = _first_matching_option(options, actions=(action,))
        if option is not None:
            return _read_index(option, DEFAULT_CHOICE_INDEX)
    option = _first_matching_option(options, actions=(SHOP_LEAVE_ACTION,)) or options[0]
    return _read_index(option, DEFAULT_CHOICE_INDEX)



def _pick_shop_option(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_shop_option_heuristic(state)
    return _llm_pick(state, _enabled_options(state), 'What do you buy (or leave)?', _fb, 'SHOP')

def _pick_event_option_heuristic(state: dict[str, Any]) -> int:
    """Choose the first enabled event option."""
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX
    return _read_index(options[0], DEFAULT_CHOICE_INDEX)



def _pick_event_option(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_event_option_heuristic(state)
    return _llm_pick(state, _enabled_options(state), 'Which event option do you choose?', _fb, 'EVENT')

def _pick_crystal_sphere_option(state: dict[str, Any]) -> int:
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX
    option = _first_matching_option(options, actions=(CRYSTAL_SPHERE_CELL_ACTION,))
    if option is not None:
        return _read_index(option, DEFAULT_CHOICE_INDEX)
    option = _first_matching_option(options, actions=(REWARD_PROCEED_ACTION,)) or options[0]
    return _read_index(option, DEFAULT_CHOICE_INDEX)


def _pick_treasure_option_heuristic(state: dict[str, Any]) -> int:
    option = _first_matching_option(
        _enabled_options(state),
        actions=(TREASURE_COLLECT_ACTION,),
    )
    return _read_index(option, DEFAULT_CHOICE_INDEX) if option is not None else DEFAULT_CHOICE_INDEX



def _pick_treasure_option(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_treasure_option_heuristic(state)
    return _llm_pick(state, _enabled_options(state), 'Do you open the treasure?', _fb, 'TREASURE')

def _pick_boss_relic_option_heuristic(state: dict[str, Any]) -> int:
    option = _first_matching_option(
        _enabled_options(state),
        actions=(BOSS_RELIC_PICK_ACTION,),
    )
    return _read_index(option, DEFAULT_CHOICE_INDEX) if option is not None else DEFAULT_CHOICE_INDEX



def _pick_boss_relic_option(state: dict[str, Any]) -> int:
    """LLM decision with the heuristic as fallback."""
    _fb = _pick_boss_relic_option_heuristic(state)
    return _llm_pick(state, _enabled_options(state), 'Which boss relic do you take?', _fb, 'BOSS_RELIC')

def _send_choice_or_skip(client: Any, choice_index: int | None) -> None:
    if choice_index is None:
        client.skip()
    else:
        client.choose(choice_index)


#: Set by run_agent when --llm-model is used. The _pick_* helpers are
#: module-level functions called from many places; a one-slot holder keeps
#: their signatures (and the heuristic fallback path) unchanged.
_LLM_POLICY: list[Any] = [None]

#: Combat routing for the session: "planner" (deterministic search) or "llm".
#: Slot 1 caches the capability probe so the "why not" is logged once, not
#: once per combat decision.
_COMBAT_POLICY: list[Any] = ["llm", None]

#: [pending sim-action indices, the game turn they were planned for]
_COMBAT_QUEUE: list[Any] = [[], -1]


def _combat_planner_action(state: dict[str, Any]) -> int | None:
    """Next combat action from the deterministic per-turn planner.

    Returns a SIM combat-action index, or None to fall back.

    Every queued action is VALIDATED against the live payload before being
    sent. A plan is computed on a reconstruction, and replaying it blind was
    a real failure in live play: after one action the game's hand and energy
    moved differently than the simulation, and the rest of the turn was
    aimed at the wrong slots -- observed as two Strikes played from slot 0
    with zero energy remaining. Each queued action therefore carries the
    CARD ID it was chosen for, and if the live hand no longer holds that
    card at that slot the plan is discarded and recomputed from the real
    state.
    """
    if _COMBAT_POLICY[0] != "planner":
        return None
    from sts2_env.bridge.combat_reconstruct import probe_payload, reconstruct_combat

    if _COMBAT_POLICY[1] is None:
        probe = probe_payload(state)
        _COMBAT_POLICY[1] = probe
        if probe.can_plan:
            logger.info("Combat: deterministic per-turn planner ENGAGED.")
        else:
            logger.warning("Combat: falling back -- %s", probe.reason())
    if not _COMBAT_POLICY[1].can_plan:
        return None

    turn = int(state.get("round", -1))
    hand = state.get("hand", []) or []

    def _live_card(idx: int) -> str | None:
        if 0 <= idx < len(hand):
            c = hand[idx]
            return str(c.get("id") or c.get("card_id") or "")
        return None

    # Drop a stale plan: new turn, or the next action no longer matches the
    # card it was planned for.
    if _COMBAT_QUEUE[0] and _COMBAT_QUEUE[1] == turn:
        nxt = _COMBAT_QUEUE[0][0]
        want = nxt.get("card_id")
        idx = nxt.get("card_index")
        if want is not None and idx is not None:
            live = _live_card(idx)
            if live is None or _norm_card(live) != _norm_card(want):
                logger.info("Combat: plan diverged (slot %s holds %s, planned "
                            "%s) -- replanning", idx, live, want)
                _COMBAT_QUEUE[0] = []
    else:
        _COMBAT_QUEUE[0] = []

    if not _COMBAT_QUEUE[0]:
        combat = reconstruct_combat(state)
        if combat is None:
            return None
        from sts2_env.gym_env.action_space import (
            action_to_card_and_target,
            is_potion_action,
        )
        from sts2_env.search.combat_planner import PlannerConfig, plan_turn

        # Whole-combat search, minimising total HP lost. A per-turn
        # objective cannot see that killing an enemy removes its future
        # attacks, that Vulnerable applied now pays off over later turns,
        # that correct blocking depends on what comes after this turn, or
        # that lethal often needs a setup turn first. All four fall out of
        # optimising the whole fight.
        #
        # Sound only because the mod now sends ai_state: without the real
        # enemy move, turns past the first would be planned against a
        # freshly-rolled one.
        from sts2_env.search.combat_planner import plan_combat_min_hp

        # Full-strength search. Budgeted runs measurably starved it: at
        # beam 48 it scored 4/6 wins and 141 HP lost, WORSE than per-turn's
        # 4/6 and 132. Unbounded (beam 512, 5M nodes, no prune margin) it
        # reaches 5/6 wins and 100 HP lost -- an extra win and 24% less
        # damage.
        #
        # The wall clock is capped at 90s despite "no budget": the mod's
        # AutoSlay watchdog aborts a run after 120s without a response, and
        # one measured plan took 139s. Exceeding it does not slow the run,
        # it ENDS the run. 90s leaves headroom for the reply to land.
        whole = plan_combat_min_hp(
            combat,
            PlannerConfig(beam_width=512, max_expansions=5_000_000,
                          time_budget_s=90.0, prune_margin=0.0),
            ai_state_known=True,
        )
        if whole is not None and whole.actions:
            plan = type("P", (), {
                "actions": whole.actions,
                "lethal": whole.won,
                "expansions": whole.expansions,
                "objective": type("O", (), {
                    "lethal": int(whole.won), "hp_preserved": -whole.hp_lost,
                    "setup": 0.0, "damage": 0.0})(),
            })()
            logger.info("Combat: whole-combat plan -- %d actions, %s, "
                        "HP %.0f -> %.0f (lost %.0f)%s",
                        len(whole.actions), "WIN" if whole.won else "no win found",
                        whole.entry_hp, whole.final_hp, whole.hp_lost,
                        "" if whole.exhausted else " [budget-capped]")
        else:
            plan = plan_turn(combat, PlannerConfig(beam_width=24,
                                                   max_expansions=60_000,
                                                   time_budget_s=6.0))
        # Annotate each action with the card it was chosen for, resolved
        # against the SIM hand as it evolves through the plan -- that is the
        # only place the intended card is knowable.
        sim_hand = [str(getattr(c, "card_id", "")).replace("CardId.", "")
                    for c in combat.combat_player_states[0].hand]
        queued = []
        for a in plan.actions:
            entry: dict[str, Any] = {"action": int(a)}
            if a != 0 and not is_potion_action(int(a)):
                try:
                    hidx, _ = action_to_card_and_target(int(a))
                except Exception:
                    hidx = None
                if hidx is not None and 0 <= hidx < len(sim_hand):
                    entry["card_index"] = hidx
                    entry["card_id"] = sim_hand[hidx]
                    sim_hand.pop(hidx)
            queued.append(entry)
        _COMBAT_QUEUE[0] = queued
        _COMBAT_QUEUE[1] = turn
        logger.info("Combat turn %s: planned %d actions%s (obj lethal=%d "
                    "hp=%.0f setup=%.0f dmg=%.0f)", turn, len(plan.actions),
                    " LETHAL" if plan.lethal else "", plan.objective.lethal,
                    plan.objective.hp_preserved, plan.objective.setup,
                    plan.objective.damage)

    if not _COMBAT_QUEUE[0]:
        return None
    return int(_COMBAT_QUEUE[0].pop(0)["action"])


def _norm_card(v: str) -> str:
    return str(v).upper().replace("CARDID.", "").replace("-", "_").strip()

def _llm_pick(state: dict[str, Any], options: list[dict[str, Any]],
              question: str, fallback: Any, tag: str) -> Any:
    """Route one decision through the LLM, falling back to the heuristic.

    ``fallback`` is the heuristic's already-computed answer, so a parse
    failure reproduces exactly the pre-LLM behaviour.
    """
    policy = _LLM_POLICY[0]
    if policy is None or not options:
        return fallback
    from sts2_env.bridge.llm_policy import render_options

    prompt = render_options(state, options, question)
    local = policy.pick(prompt, options, lambda: 0, tag=tag)
    # The model answers in MENU order; translate back to the payload's own
    # index field, which is what the bridge expects and is NOT always
    # positional.
    chosen = options[local] if 0 <= local < len(options) else None
    if chosen is None:
        return fallback
    return _read_index(chosen, local)


_RL_COMBAT = [None]   # [loaded MaskablePPO model]


def _rl_combat_action(state: dict[str, Any]) -> int | None:
    """Next combat action from the trained RL combat agent.

    The bridge's StateAdapter emits a 131-dim observation, but every model
    trained since the observation revamp expects the 4778-dim RICH vector,
    so a current checkpoint cannot be fed from the wire directly. The route
    that works is the one the planner already uses: rebuild a real
    CombatState from the payload, then encode THAT with the same encoder the
    model trained against. Reconstruction is therefore load-bearing for the
    RL agent too, not just for search.
    """
    model = _RL_COMBAT[0]
    if model is None:
        return None
    from sts2_env.bridge.combat_reconstruct import reconstruct_combat
    from sts2_env.gym_env.action_space import get_action_mask
    from sts2_env.gym_env.rich_observation import RichObservationEncoder

    combat = reconstruct_combat(state)
    if combat is None:
        return None
    import numpy as _np

    obs = RichObservationEncoder().encode_combat(combat)
    mask = _np.asarray(get_action_mask(combat), dtype=bool)
    if not mask.any():
        return None
    action, _ = model.predict(obs, action_masks=mask, deterministic=True)
    action = int(action)
    if not (0 <= action < mask.size) or not mask[action]:
        legal = _np.flatnonzero(mask)
        action = int(legal[0]) if legal.size else 0
    return action


def _heuristic_combat_action(state: dict[str, Any], adapter: Any) -> int | None:
    """Play one reasonable combat action without the planner.

    This exists because the previous fallback ended the turn whenever the
    planner declined, which lost a live run outright: the agent passed every
    turn of the first fight until it died. Ending the turn is the single
    worst legal action, so it must never be the default.

    Priority mirrors the planner's objective as closely as a one-ply rule
    can: kill something if a single card can, then commit powers, then
    block against a real incoming attack, then attack the weakest enemy.
    """
    import numpy as _np

    mask = adapter.compute_action_mask(state)
    legal = [int(i) for i in _np.flatnonzero(_np.asarray(mask, dtype=bool))]
    if not legal:
        return None

    hand = state.get("hand", []) or []
    enemies = [e for e in (state.get("enemies") or [])
               if e.get("is_alive", True) and int(e.get("hp", 0) or 0) > 0]
    incoming = 0
    for e in enemies:
        dmg = e.get("intent_damage") or 0
        try:
            incoming += int(dmg) * int(e.get("intent_hits", 1) or 1)
        except Exception:
            pass
    block = int((state.get("player") or {}).get("block", 0) or 0)

    scored: list[tuple[int, int]] = []   # (priority, action)
    for a in legal:
        try:
            d = adapter.decode_action(a, state)
        except Exception:
            continue
        if d.get("type") == ActionType.END_TURN or d.get("out_of_hand"):
            continue
        idx = d.get("card_index", -1)
        if not (0 <= idx < len(hand)):
            continue
        card = hand[idx]
        ctype = str(card.get("type", "")).upper()
        tgt = d.get("target_index", -1)
        target_hp = None
        if 0 <= tgt < len(enemies):
            try:
                target_hp = int(enemies[tgt].get("hp", 0) or 0)
            except Exception:
                target_hp = None

        if ctype == "POWER":
            pri = 2
        elif ctype == "ATTACK":
            # Prefer hitting the weakest live enemy: most likely to remove a
            # source of damage this turn.
            pri = 3 if target_hp is not None and target_hp <= 12 else 4
        else:  # SKILL -- mostly block
            pri = 1 if incoming > block else 6
        scored.append((pri, a))

    if not scored:
        return None
    scored.sort()
    return scored[0][1]


def _llm_combat_action(state: dict[str, Any], adapter: Any) -> int | None:
    """Let the LLM choose one combat action, as a sim action index.

    Only used when the planner cannot run. Options are built from the
    adapter's own legal mask, so the model can only pick a legal play.
    """
    policy = _LLM_POLICY[0]
    if policy is None:
        return None
    import numpy as _np

    from sts2_env.bridge.llm_policy import render_combat

    mask = adapter.compute_action_mask(state)
    legal = [int(i) for i in _np.flatnonzero(_np.asarray(mask, dtype=bool))]
    if not legal:
        return None
    opts = []
    for a in legal:
        try:
            d = adapter.decode_action(a, state)
        except Exception:
            continue
        opts.append({"_action": a, "_label": _combat_option_label(state, a, d)})
    if not opts:
        return None
    prompt = render_combat(state, opts)
    local = policy.pick(prompt, opts, lambda: 0, tag="COMBAT")
    if 0 <= local < len(opts):
        return int(opts[local]["_action"])
    return int(opts[0]["_action"])


def _combat_option_label(state: dict[str, Any], action: int, decoded: dict) -> str:
    if decoded.get("type") == ActionType.END_TURN:
        return "End turn"
    if decoded.get("out_of_hand"):
        return f"Use potion slot {decoded.get('slot', '?')}"
    hand = state.get("hand", []) or []
    idx = decoded.get("card_index", -1)
    name = "?"
    if 0 <= idx < len(hand):
        c = hand[idx]
        name = str(c.get("id") or c.get("card_id") or "?")
        if c.get("cost") is not None:
            name += f" [{c['cost']}e]"
    tgt = decoded.get("target_index", -1)
    return f"Play {name}" + (f" -> enemy {tgt}" if tgt is not None and tgt >= 0 else "")


def _enabled_options(state: dict[str, Any]) -> list[dict[str, Any]]:
    return [
        option
        for option in state.get("options", [])
        if bool(option.get("enabled", True))
    ]


def _first_matching_option(
    options: list[dict[str, Any]],
    *,
    option_ids: tuple[str, ...] = (),
    actions: tuple[str, ...] = (),
) -> dict[str, Any] | None:
    option_id_set = {_canonical_text(value) for value in option_ids}
    action_set = {_canonical_text(value) for value in actions}
    for option in options:
        if option_id_set and _canonical_text(option.get("id")) in option_id_set:
            return option
        if action_set and _canonical_text(option.get("action")) in action_set:
            return option
    return None


def _read_deck_size(state: dict[str, Any]) -> int:
    run_state = state.get("run_state", {})
    if isinstance(run_state, dict):
        deck = run_state.get("deck")
        if isinstance(deck, list):
            return len(deck)
    return int(state.get("deck_size", 0) or 0)


def _read_hp_ratio(state: dict[str, Any]) -> float | None:
    for container in _candidate_player_containers(state):
        hp, max_hp = _read_hp_pair(container)
        if hp is not None and max_hp and max_hp > 0:
            return hp / max_hp
    return None


def _candidate_player_containers(state: dict[str, Any]) -> list[dict[str, Any]]:
    containers: list[dict[str, Any]] = []
    for key in ("player", "run_state", "combat_state"):
        value = state.get(key)
        if isinstance(value, dict):
            if isinstance(value.get("player"), dict):
                containers.append(value["player"])
            containers.append(value)
    containers.append(state)
    return containers


def _read_hp_pair(container: dict[str, Any]) -> tuple[int | None, int | None]:
    hp_value = container.get("hp")
    if isinstance(hp_value, str) and "/" in hp_value:
        hp_text, max_hp_text = hp_value.split("/", 1)
        return _optional_int(hp_text), _optional_int(max_hp_text)
    return _optional_int(hp_value), _optional_int(container.get("max_hp"))


def _read_index(option: dict[str, Any], fallback: int) -> int:
    value = _optional_int(option.get("index"))
    return fallback if value is None else value


def _optional_int(value: Any) -> int | None:
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def _canonical_text(value: Any) -> str:
    return str(value or "").replace("_", "").replace(" ", "").casefold()


# ----------------------------------------------------------------
# Utility functions
# ----------------------------------------------------------------


def _reconnect_with_retry(
    client: STS2GameClient, max_retries: int = 10, delay: float = 3.0
) -> None:
    """Attempt to reconnect to the game server with retries.

    Args:
        client: The game client to reconnect.
        max_retries: Maximum reconnection attempts.
        delay: Seconds between attempts.
    """
    for attempt in range(1, max_retries + 1):
        try:
            logger.info("Reconnect attempt %d/%d...", attempt, max_retries)
            client.reconnect()
            logger.info("Reconnected successfully.")
            return
        except ConnectionError:
            if attempt < max_retries:
                time.sleep(delay)
            else:
                logger.error("Failed to reconnect after %d attempts. Exiting.", max_retries)
                sys.exit(1)


def _log_combat_action(
    state: dict[str, Any], action_int: int, decoded: dict[str, Any]
) -> None:
    """Log a combat action with context for debugging."""
    combat = state.get("combat_state") or state
    player = combat.get("player", {})
    hand = combat.get("hand", [])
    enemies = combat.get("enemies", [])

    if decoded["type"] == ActionType.END_TURN:
        logger.info(
            "COMBAT [HP:%d/%d E:%d] -> END_TURN (round %d)",
            player.get("hp", 0),
            player.get("max_hp", 0),
            player.get("energy", 0),
            combat.get("round", 0),
        )
    elif decoded["type"] == ActionType.POTION or decoded.get("out_of_hand"):
        slot = decoded.get("slot", decoded.get("potion_slot", -1))
        ti = decoded.get("target_index", -1)
        potions = combat.get("potions", [])
        potion_name = "?"
        for potion in potions:
            if int(potion.get("slot", -1)) == slot:
                potion_name = potion.get("id", "?")
                break
        target_name = enemies[ti].get("id", "?") if 0 <= ti < len(enemies) else "N/A"
        logger.info(
            "COMBAT [HP:%d/%d E:%d] -> POTION %s (slot=%d) -> %s (idx=%d)",
            player.get("hp", 0),
            player.get("max_hp", 0),
            player.get("energy", 0),
            potion_name,
            slot,
            target_name,
            ti,
        )
    else:
        ci = decoded.get("card_index", -1)
        ti = decoded.get("target_index", -1)
        card_name = hand[ci].get("id", "?") if ci < len(hand) else "?"
        target_name = enemies[ti].get("id", "?") if 0 <= ti < len(enemies) else "N/A"
        logger.info(
            "COMBAT [HP:%d/%d E:%d] -> PLAY %s (idx=%d) -> %s (idx=%d)",
            player.get("hp", 0),
            player.get("max_hp", 0),
            player.get("energy", 0),
            card_name, ci,
            target_name, ti,
        )


# ----------------------------------------------------------------
# CLI entry point
# ----------------------------------------------------------------


def main() -> None:
    """CLI entry point for the agent runner."""
    parser = argparse.ArgumentParser(
        description="Run a trained RL agent on the real STS2 game.",
        formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )
    parser.add_argument(
        "--llm-model",
        default=None,
        help="Path to a local GGUF model (e.g. models/Qwen3.6-27B-Q3_K_M.gguf). "
             "When set, the LLM makes every decision and no MaskablePPO model "
             "is loaded; unparseable replies fall back to the heuristics.",
    )
    parser.add_argument(
        "--combat-policy",
        choices=["planner", "llm", "rl"],
        default="planner",
        help="Who plays combat: 'planner' = deterministic beam search "
             "(default; needs a mod build that sends pile+deck contents, "
             "otherwise falls back to the LLM with a logged reason), "
             "'llm' = the language model plays combat too.",
    )
    parser.add_argument("--rl-combat-model", default=None,
                        help="Trained 115-action rich-obs combat model for "
                             "--combat-policy rl")
    parser.add_argument("--llm-gpu-layers", type=int, default=34)
    parser.add_argument("--llm-ctx", type=int, default=4096)
    parser.add_argument("--llm-max-tokens", type=int, default=48)
    parser.add_argument("--llm-temperature", type=float, default=0.2)
    parser.add_argument(
        "--model-path",
        default=None,
        help="Path to a trained MaskablePPO model (.zip). Optional when "
             "--llm-model is given.",
    )
    parser.add_argument(
        "--host",
        default="127.0.0.1",
        help="Bridge server hostname.",
    )
    parser.add_argument(
        "--port",
        type=int,
        default=9002,
        help="Bridge server port.",
    )
    parser.add_argument(
        "--deterministic",
        action="store_true",
        default=True,
        help="Use deterministic action selection (no exploration).",
    )
    parser.add_argument(
        "--stochastic",
        action="store_true",
        default=False,
        help="Use stochastic action selection (for diversity).",
    )
    parser.add_argument(
        "--verbose", "-v",
        action="store_true",
        default=False,
        help="Log every action taken.",
    )
    parser.add_argument(
        "--log-level",
        default="INFO",
        choices=["DEBUG", "INFO", "WARNING", "ERROR"],
        help="Logging level.",
    )
    parser.add_argument(
        "--record-replay",
        default=None,
        help="Optional path to save a bridge replay trace JSON while the agent runs.",
    )
    parser.add_argument(
        "--replay-factory",
        default=None,
        help="Optional module:function factory to store in replay metadata for later comparison.",
    )
    parser.add_argument(
        "--action-delay",
        type=float,
        default=0.0,
        help="Seconds to pause before each non-combat decision so a human can follow along (e.g. 1.0).",
    )
    parser.add_argument(
        "--combat-delay",
        type=float,
        default=0.0,
        help="Seconds to pause before each combat action (end turn is always instant).",
    )

    args = parser.parse_args()

    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(asctime)s [%(name)s] %(levelname)s: %(message)s",
        datefmt="%H:%M:%S",
    )

    deterministic = not args.stochastic

    run_agent(
        model_path=args.model_path,
        host=args.host,
        port=args.port,
        deterministic=deterministic,
        verbose=args.verbose,
        record_replay_path=args.record_replay,
        replay_factory=args.replay_factory,
        action_delay=args.action_delay,
        combat_delay=args.combat_delay,
        combat_policy=args.combat_policy,
        rl_combat_model=args.rl_combat_model,
        llm_model=args.llm_model,
        llm_gpu_layers=args.llm_gpu_layers,
        llm_ctx=args.llm_ctx,
        llm_max_tokens=args.llm_max_tokens,
        llm_temperature=args.llm_temperature,
    )


if __name__ == "__main__":
    main()
