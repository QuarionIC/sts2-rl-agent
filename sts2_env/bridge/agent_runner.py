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
    run_policy: str = "heuristic",
    rl_run_model: str | None = None,
    rl_run_phases: str | None = None,
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
    logger.info(
        "Combat policy: %s | out-of-combat: %s",
        combat_policy,
        "LLM" if llm_model else ("RL run agent" if run_policy == "rl"
                                 else "model/heuristic"),
    )

    _RUN_POLICY[0] = run_policy
    _RL_RUN[0] = None
    if rl_run_phases:
        requested = {p.strip().upper() for p in rl_run_phases.split(",") if p.strip()}
        allowed = {getattr(Phase, name) for name in requested
                   if isinstance(getattr(Phase, name, None), str)}
        unknown = {name for name in requested
                   if not isinstance(getattr(Phase, name, None), str)}
        if unknown:
            raise SystemExit(f"--rl-run-phases: unknown phase(s) {sorted(unknown)}")
        _RL_RUN_PHASES_ACTIVE[0] = frozenset(allowed) & _RL_RUN_PHASES
    else:
        _RL_RUN_PHASES_ACTIVE[0] = _RL_RUN_PHASES
    if run_policy == "rl":
        logger.info("RL run agent will drive: %s | heuristics keep: %s",
                    sorted(_RL_RUN_PHASES_ACTIVE[0]) or "(nothing)",
                    sorted(_RL_RUN_PHASES - _RL_RUN_PHASES_ACTIVE[0]) or "(nothing)")
    if run_policy == "rl":
        if not rl_run_model:
            raise SystemExit("--run-policy rl requires --rl-run-model")
        from sb3_contrib import MaskablePPO

        logger.info("Loading RL run agent: %s", rl_run_model)
        _RL_RUN[0] = MaskablePPO.load(rl_run_model, device="cpu")
        obs_n = int(_RL_RUN[0].observation_space.shape[0])
        act_n = int(_RL_RUN[0].policy.action_space.n)
        logger.info("RL run agent loaded (actions=%d, obs=%d)", act_n, obs_n)

        # Fail loudly at startup rather than silently at the first decision.
        # A checkpoint of the wrong shape cannot be fed and would spend the
        # whole session falling back to the heuristics while the log said
        # "run policy: rl".
        from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE

        if obs_n != RICH_OBS_SIZE or act_n != FULL_RUN_ACTION_SPACE_SIZE:
            raise SystemExit(
                f"--rl-run-model has {act_n} actions / {obs_n} obs dims; the "
                f"bridge run path requires {FULL_RUN_ACTION_SPACE_SIZE} / "
                f"{RICH_OBS_SIZE}. This is a hierarchical run agent slot -- "
                f"a combat-only checkpoint will not fit it.")

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
        runs_seen = 0
        run_ended = False   # a terminal message for this run was already counted
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

                # Any non-terminal payload means a new run is under way, so
                # the next ending is a genuinely new one to count.
                if phase not in TERMINAL_PHASES and phase != MSG_TYPE_PONG:
                    run_ended = False

                # Keep the newest combat payload so a mid-combat card
                # discovery, which arrives without one, still has a fight to
                # be evaluated against.
                if msg_type == BridgeStateType.COMBAT_ACTION:
                    _LAST_COMBAT_STATE[0] = state

                if verbose and step_count % 10 == 1:
                    logger.info("Step %d: type=%s phase=%s", step_count, msg_type, phase)

                if verbose and msg_type:
                    logger.debug("Received: type=%s keys=%s", msg_type, list(state.keys()))

                if phase == MSG_TYPE_PONG:
                    continue
                if phase in TERMINAL_PHASES:
                    # One ENDING, not one message. A death emits game_over and
                    # then run_complete, so counting every terminal message
                    # reported 3 runs for the 2 that were actually played --
                    # and any per-run rate computed from it was wrong by that
                    # factor. Count only the first terminal message of a
                    # streak; the next non-terminal payload re-arms it.
                    if not run_ended:
                        run_ended = True
                        runs_seen += 1
                        logger.info("Run finished: %s (run %d this session)",
                                    state.get("result",
                                              state.get("message", "unknown")),
                                    runs_seen)
                    else:
                        logger.debug("Additional terminal message (%s) for the "
                                     "run already counted", msg_type)
                    # DO NOT EXIT. The mod plays N runs back-to-back
                    # (RlAutoSlayer.RunAsync loops over PreferredRunCount), so a
                    # terminal message is a RUN boundary, not a session
                    # boundary. Exiting here left every run after the first
                    # with no agent attached -- the mod would fall back to
                    # playing random cards and the "measurement" would be of
                    # nothing. Reset per-run state and keep serving.
                    _COMBAT_POLICY[1] = None      # re-probe the next payload
                    _COMBAT_QUEUE[0] = []
                    _COMBAT_QUEUE[1] = None
                    continue
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

                # ---- Out of combat: trained hierarchical run agent ----
                # Placed after the combat branches (which all `continue`) and
                # before the heuristics, so combat stays with the planner and
                # only the run-level decisions change hands. Falling through
                # on a False return is the point: every unusable payload,
                # unresolvable card id or mask disagreement degrades to the
                # heuristic rather than to a guess.
                if (_RUN_POLICY[0] == "rl"
                        and phase in (_RL_RUN_PHASES_ACTIVE[0] or _RL_RUN_PHASES)
                        # A mid-combat discovery maps to CARD_REWARD, so the
                        # run agent used to claim it. It cannot: its
                        # observation holds deck, map, relics and potions and
                        # NO combat state, and it was trained to judge cards
                        # as permanent deck additions. A discovered card is
                        # free for one turn and gone after -- the opposite
                        # decision. Left to the combat-aware chooser below.
                        and not _is_in_combat_card_select(state)):
                    if _try_rl_run_action(client, state, verbose):
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
                    elif msg_type == BridgeStateType.REWARD_SCREEN:
                        choice = _pick_reward_screen_option(state)
                        if verbose:
                            logger.info("CARD_REWARD: choosing option %s", choice)
                        _send_choice_or_skip(client, choice)
                    else:
                        choice = _pick_card_reward_index(state)
                        # None means skip. The reward is NOT consumed by the
                        # skip button -- it is consumed by leaving the rewards
                        # screen -- so latch the decision for the reward screen
                        # that follows, or the two screens ping-pong forever.
                        if choice is None:
                            if card_reward_should_force_take(state):
                                logger.warning(
                                    "CARD_REWARD re-offered %d times this room "
                                    "-- taking a card instead of skipping again.",
                                    _CARD_REWARD_LATCH[2],
                                )
                                choice = DEFAULT_CHOICE_INDEX
                            else:
                                note_card_reward_declined(state)
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

def _is_in_combat_card_select(state: dict[str, Any]) -> bool:
    """True when this card_select is a mid-combat discovery.

    The mod sets in_combat from CombatManager.IsInProgress. Payloads from a
    mod build that predates the flag fall back to False, which restores the
    old behaviour rather than guessing.
    """
    return bool(state.get("in_combat", False))


def _score_discovered_card(card: dict[str, Any], combat_state: dict[str, Any] | None) -> float:
    """Rank one mid-combat discovery candidate. Higher is better.

    Deliberately a cheap static score rather than a planner rollout. The
    discovery resolves INSIDE the OnPlay of the card being played, so the
    reconstruction available here is one action stale, and the planner takes
    seconds per call -- long enough for the 30s selector timeout to fire and
    hand the choice back to the mod's fallback.

    The ordering encodes what a discovered card is FOR: it is generated free
    for the turn (CardModel.SetToFreeThisTurn), so it is a tempo card, not a
    deck addition. Attacks and Skills that act now beat Powers that need
    several turns to repay themselves.
    """
    card_type = _canonical_text(card.get("type"))
    score = {
        "attack": 3.0,
        "skill": 2.5,
        "power": 1.0,
        "status": -5.0,
        "curse": -5.0,
    }.get(card_type, 1.5)

    # Free-this-turn means printed cost mostly does not bind, but a cheap card
    # still composes better with whatever else is in hand.
    try:
        cost = int(card.get("cost", 1))
    except (TypeError, ValueError):
        cost = 1
    if cost >= 0:
        score -= 0.15 * cost

    if card.get("upgraded"):
        score += 0.5

    # Low on HP: a Skill (block, defensive utility) outranks another Attack.
    if combat_state:
        player = combat_state.get("player") or {}
        try:
            hp = int(player.get("hp", combat_state.get("hp", 0)) or 0)
            max_hp = int(player.get("max_hp", combat_state.get("max_hp", 0)) or 0)
        except (TypeError, ValueError):
            hp = max_hp = 0
        if max_hp > 0 and hp / max_hp < REST_HP_RATIO_THRESHOLD:
            if card_type == "skill":
                score += 1.5
            elif card_type == "attack":
                score -= 0.5
    return score


def _pick_card_select_indexes(state: dict[str, Any]) -> list[int]:
    """Choose card indexes for upgrade/transform/discovery/select screens.

    Two bugs lived here, both measured live 2026-07-31.

    1. ``min_select == 0`` returned [] -- which the caller sends as a SKIP.
       CardSelectCmd.FromChooseACardScreen passes ``minSelect: 0`` for every
       mid-combat discovery, so the agent skipped every Discovery, Abundance
       and Attack Potion choice it was ever offered. "May take nothing" was
       read as "must take nothing".
    2. Out-of-combat selections returned the first N indexes unconditionally,
       the same fixed-slot degeneracy already rejected for card rewards.

    Optional selections (min_select == 0) now take the best-scoring card when
    one scores positively, and skip only when every option is bad.
    """
    cards = list(state.get("cards", []))
    min_select = max(int(state.get("min_select", 1)), 0)
    max_select = max(int(state.get("max_select", min_select)), 0)
    if not cards or max_select == 0:
        return []

    combat_state = _LAST_COMBAT_STATE[0] if _is_in_combat_card_select(state) else None
    ranked = sorted(
        range(len(cards)),
        key=lambda i: _score_discovered_card(cards[i], combat_state),
        reverse=True,
    )

    if min_select == 0:
        best = ranked[0]
        if _score_discovered_card(cards[best], combat_state) <= 0.0:
            return []
        return [_read_index(cards[best], best)]

    count = min(max(min_select, 1), max_select, len(cards))
    return [_read_index(cards[i], i) for i in ranked[:count]]


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

#: [floor the latch belongs to, declined-a-card-here, times re-offered]
#:
#: Declining a card reward takes TWO screens, and the game only consumes the
#: reward on the second one.
#:
#: Pressing Skip inside the card-selection screen is a CANCEL, not a decline:
#: CardRewardAlternative.Generate builds it with
#: PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward, whose own
#: doc comment reads "end card selection, but don't complete it - the player
#: may re-enter card selection". CardReward.OnSelect returns rewardComplete =
#: false, so the reward stays in the set and the rewards screen offers it
#: again. What actually consumes it is leaving the rewards screen:
#: RewardsSetSynchronizer.SkipRewardsSet calls OnSkipped() on every reward
#: that is not SuccessfullySelected.
#:
#: Live 2026-07-31 the run agent chose skip at card_reward and pick at
#: reward_screen, forever -- 10+ round trips per second until the run timed
#: out. Remembering "I already skipped" would NOT have fixed it on its own;
#: the skip has to become a Proceed.
_CARD_REWARD_LATCH: list[Any] = [None, False, 0]

#: Take a card rather than livelock. Mirrors MaxRepeatedChoices in the mod's
#: event loop: an unwanted card costs deck quality, a spinning run costs
#: everything.
CARD_REWARD_MAX_REOFFERS = 3


def _reward_latch_floor(state: dict[str, Any]) -> Any:
    """Identity for "this room's rewards". Latches must not outlive it."""
    return (state.get("floor"), state.get("act"))


def _reset_card_reward_latch_if_new_room(state: dict[str, Any]) -> None:
    floor = _reward_latch_floor(state)
    if _CARD_REWARD_LATCH[0] != floor:
        _CARD_REWARD_LATCH[0] = floor
        _CARD_REWARD_LATCH[1] = False
        _CARD_REWARD_LATCH[2] = 0


def note_card_reward_declined(state: dict[str, Any]) -> None:
    """Record that the agent wants no card from this room's card reward."""
    _reset_card_reward_latch_if_new_room(state)
    _CARD_REWARD_LATCH[1] = True
    _CARD_REWARD_LATCH[2] += 1


def card_reward_is_declined(state: dict[str, Any]) -> bool:
    _reset_card_reward_latch_if_new_room(state)
    return bool(_CARD_REWARD_LATCH[1])


def card_reward_should_force_take(state: dict[str, Any]) -> bool:
    """True once this room has declined CARD_REWARD_MAX_REOFFERS times.

    Bounds the ping-pong at MAX_REOFFERS round trips rather than letting it
    run one extra lap, which matters because each lap is a full screen open
    and close in the live game.
    """
    _reset_card_reward_latch_if_new_room(state)
    return _CARD_REWARD_LATCH[2] >= CARD_REWARD_MAX_REOFFERS


def _pick_reward_screen_option(state: dict[str, Any]) -> int:
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX

    # Already declined a card in this room? Then LEAVE, do not re-open the
    # entry. Proceeding is what makes the game skip the reward for real.
    proceed = _first_matching_option(options, actions=(REWARD_PROCEED_ACTION,))
    if proceed is not None and card_reward_is_declined(state):
        logger.info("REWARD_SCREEN: card reward already declined this room -- "
                    "proceeding so the game skips it (opening it again would "
                    "just re-offer it).")
        return _read_index(proceed, DEFAULT_CHOICE_INDEX)

    option = _first_matching_option(options, actions=(REWARD_PICK_ACTION,))
    if option is not None:
        return _read_index(option, DEFAULT_CHOICE_INDEX)
    option = proceed or options[0]
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

def _proceed_option(state: dict[str, Any]) -> dict[str, Any] | None:
    """The option that LEAVES this screen, if the payload marks one.

    Events have two kinds of page: a question, whose options are real
    choices, and a result, whose only affordance is Proceed. The wire did not
    distinguish them, so the agent answered a result page as though it were
    still a question.
    """
    for option in _enabled_options(state):
        if option.get("is_proceed") or _canonical_text(
                option.get("action")) == REWARD_PROCEED_ACTION:
            return option
    return None


def _pick_event_option_heuristic(state: dict[str, Any]) -> int:
    """Choose an event option, taking Proceed when it is the only way out."""
    options = _enabled_options(state)
    if not options:
        return DEFAULT_CHOICE_INDEX

    # Only take Proceed automatically when it is the SOLE option. Some events
    # offer "leave" alongside real choices on the first page, and that is a
    # decision worth making, not a reflex.
    proceed = _proceed_option(state)
    if proceed is not None and len(options) == 1:
        return _read_index(proceed, DEFAULT_CHOICE_INDEX)
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

#: The most recent combat payload seen this run.
#:
#: Mid-combat card discoveries (Discovery, Abundance, ...) arrive as a
#: card_select message that carries NO combat block -- CardSelectCmd calls the
#: selector directly, outside the combat handler. Without the fight in view
#: there is nothing to choose against, so the runner keeps the last combat
#: payload and evaluates candidates against it. It is the same turn: the
#: discovery resolves inside the OnPlay of a card the agent just played.
_LAST_COMBAT_STATE: list[Any] = [None]


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

    # PLAY THE WHOLE LINE, ACROSS TURN BOUNDARIES.
    #
    # plan_combat_min_hp returns a complete combat solution, computed on an
    # exact reconstruction. Combat is deterministic given the draw order, so
    # the simulated post-END-TURN hand matches the game's and the queue stays
    # valid past the turn boundary. Requiring _COMBAT_QUEUE[1] == turn threw
    # the rest of that solution away every turn and paid a fresh 90s search
    # to re-derive it -- and worse, each re-search started from a shallower
    # remaining fight, so later turns were planned with the same budget
    # spent on less game.
    #
    # Card identity is now the ONLY correctness guard, and it is the right
    # one: if the live hand no longer holds the card an action was planned
    # for, the determinism assumption broke and we replan from real state.
    # _COMBAT_QUEUE[1] tracks the last seen round purely to notice a NEW
    # combat -- round numbers increase within a fight and reset on the next
    # one, so a decrease means a different fight and the queue must go.
    last_round = _COMBAT_QUEUE[1]
    if _COMBAT_QUEUE[0] and isinstance(last_round, int) and turn < last_round:
        logger.info("Combat: round went %s -> %s, new combat -- dropping plan",
                    last_round, turn)
        _COMBAT_QUEUE[0] = []
    if _COMBAT_QUEUE[0]:
        nxt = _COMBAT_QUEUE[0][0]
        want = nxt.get("card_id")
        idx = nxt.get("card_index")
        if nxt.get("unvalidatable"):
            # The annotator could not resolve a hand slot for this action, so
            # there is nothing to check it against. Replanning is strictly
            # safer than replaying an action we cannot validate -- an
            # unchecked replay is how a stale plan reaches the live game.
            logger.info("Combat: next queued action is unvalidatable -- replanning")
            _COMBAT_QUEUE[0] = []
        elif want is not None and idx is not None:
            live = _live_card(idx)
            if live is None or _norm_card(live) != _norm_card(want):
                # Full diff, not just the one slot. Same length + same multiset
                # => pure ORDERING (draw/shuffle order). Different length =>
                # a draw-count or card-movement difference. Different multiset
                # => cards appearing or vanishing (status cards, retain,
                # unmodelled effects). Each points at a different subsystem.
                sim_hand = list(nxt.get("sim_hand") or [])
                live_hand = [str(c.get("id") or c.get("card_id") or "")
                             for c in hand]
                same_len = len(sim_hand) == len(live_hand)
                same_multiset = (sorted(_norm_card(c) for c in sim_hand)
                                 == sorted(_norm_card(c) for c in live_hand))
                # SIM-AHEAD: our hand is the live hand with exactly one card
                # removed, and we hold less energy. That is not a shuffle
                # disagreement at all -- it is the simulation having played an
                # action the game has not, so the plan and the game are
                # off by one.
                #
                # Worth separating because it was hiding inside CONTENTS and
                # pointing the investigation at the wrong subsystem: a real
                # case read sim [DEFEND, SLICE, DEFEND, STRIKE] energy 2
                # against live [DEFEND, STRIKE, SLICE, DEFEND, STRIKE] energy
                # 3 -- the live hand minus the STRIKE at index 1. Card
                # contents were never the problem there.
                sim_energy = nxt.get("sim_energy")
                live_energy = (state.get("player") or {}).get("energy")
                sim_ahead = False
                if len(sim_hand) == len(live_hand) - 1:
                    remaining = [_norm_card(c) for c in live_hand]
                    for card in (_norm_card(c) for c in sim_hand):
                        if card in remaining:
                            remaining.remove(card)
                        else:
                            break
                    else:
                        sim_ahead = (len(remaining) == 1
                                     and isinstance(sim_energy, int)
                                     and isinstance(live_energy, int)
                                     and sim_energy <= live_energy)

                # DRAW-SHIFT: the two hands are the same window of the draw
                # sequence, offset by exactly one card.
                #
                # Measured overnight 2026-08-01, three of seven "CONTENTS"
                # divergences were really this:
                #
                #   sim  [MELANCHOLY, DEFEND, UNLEASH, DEFILE, STRIKE]
                #   live [            DEFEND, UNLEASH, DEFILE, STRIKE, DEBILITATE]
                #
                # sim[1:] == live[:-1] exactly. That is not "different cards",
                # it is one side having drawn one card more (or fewer) than the
                # other, and it points at the draw COUNT rather than at card
                # modelling or shuffle order. Left inside CONTENTS it sent the
                # investigation at the wrong subsystem -- the same mistake
                # SIM-AHEAD was split out to stop.
                shifted = ""
                if len(sim_hand) >= 2 and len(live_hand) >= 2:
                    sim_n = [_norm_card(c) for c in sim_hand]
                    live_n = [_norm_card(c) for c in live_hand]
                    if sim_n[1:] == live_n[:len(sim_n) - 1]:
                        shifted = "the GAME drew one more than the simulator"
                    elif live_n[1:] == sim_n[:len(live_n) - 1]:
                        shifted = "the SIMULATOR drew one more than the game"

                if sim_ahead:
                    kind = ("SIM-AHEAD (we played an action the game has not; "
                            "off by one, not a shuffle mismatch)")
                elif shifted:
                    kind = (f"DRAW-SHIFT (same draw window offset by one; "
                            f"{shifted})")
                elif same_len and same_multiset:
                    kind = "ORDER-ONLY (same cards, different order)"
                elif same_multiset:
                    kind = "COUNT (same multiset, different length)"
                else:
                    kind = "CONTENTS (different cards)"
                logger.info("Combat: plan diverged [%s] slot %s holds %s, "
                            "planned %s\n    sim  hand: %s (energy %s)\n"
                            "    live hand: %s (energy %s)",
                            kind, idx, live, want,
                            sim_hand, nxt.get("sim_energy"),
                            live_hand,
                            (state.get("player") or {}).get("energy"))
                _COMBAT_QUEUE[0] = []
    _COMBAT_QUEUE[1] = turn

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

        # NARROW AND DEEP, not wide and shallow. plan_combat_min_hp returns
        # a plan ONLY if it reaches a winning terminal, else None -- so
        # depth, not breadth, is what decides whether it returns anything.
        #
        # One beam level costs about 20 * beam_width expansions, and at
        # ~140 nodes/s a 90s budget buys ~12600. Beam 12 is therefore ~52
        # levels deep (deeper than a whole fight, so terminals are
        # reachable); beam 512 is ~1.2 levels and returns None on anything
        # with a real branching factor. Measured: at beam 512 an A10 opener
        # returned "no win in budget" at both 5s and 15s. It only appeared
        # to work live on a 1-energy 3-card A0 opener.
        #
        # 90s is viable ONLY because the mod raises the watchdog to 120s
        # (RlAutoSlayer.RaiseWatchdogTimeout). The stock
        # AutoSlayConfig.watchdogTimeout is 30s, and it measures NO
        # PROGRESS rather than response latency, so the mod's serialize and
        # animation work shares the window with our search. Blowing it does
        # not slow the run, it ENDS the run -- Watchdog.Check() throws
        # AutoSlayTimeoutException and Python gets run_complete/terminated.
        # Measured live 2026-07-30 against the stock 30s: a 28s plan tripped
        # it at 39.9s and killed the run one card in.
        whole = plan_combat_min_hp(
            combat,
            PlannerConfig(beam_width=12, max_expansions=5_000_000,
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
            # Reached only when the whole-combat search found no win. Its
            # budget STACKS on the 10s above, so the pair must stay well
            # inside the 30s watchdog window: 10 + 5 = 15s worst case,
            # leaving ~15s for the mod's own work.
            plan = plan_turn(combat, PlannerConfig(beam_width=24,
                                                   max_expansions=60_000,
                                                   time_budget_s=5.0))
        # Annotate each action with the card it was chosen for by STEPPING A
        # CLONE of the reconstruction through the plan.
        #
        # This used to keep a flat list[str] snapshot of the plan-time hand and
        # mutate it only with pop() on card plays. That cannot cross a turn
        # boundary: ACTION_END_TURN was skipped by the guard, so nothing
        # discarded the hand or drew the next one, and every post-boundary
        # action was labelled with a LEFTOVER card from an earlier turn. The
        # identity check below then compared a stale label against the live
        # hand and reported "plan diverged" at every single turn boundary --
        # 9 of 9 live, and reproducibly so even against a perfectly faithful
        # simulator. It was an instrument fault, not a simulation fault.
        #
        # The silent half was worse: when the stale label happened to match the
        # real card (common with 4 Strikes + 4 Defends in deck) no warning
        # fired and the stale plan replayed into the live game unchecked.
        #
        # Stepping the clone with the planner's own transition makes the label
        # exact by construction, so a divergence line now means what it says.
        from sts2_env.search.combat_mcts import apply_combat_action, clone_combat

        shadow = clone_combat(combat)
        queued = []
        for a in plan.actions:
            entry: dict[str, Any] = {"action": int(a)}
            # During a pending card choice the action indexes CHOICE OPTIONS,
            # not the hand, so a hand-slot label would be meaningless.
            if (a != 0 and not is_potion_action(int(a))
                    and getattr(shadow, "pending_choice", None) is None):
                try:
                    hidx, _ = action_to_card_and_target(int(a))
                except Exception:
                    hidx = None
                shand = shadow.combat_player_states[0].hand
                # Snapshot the WHOLE simulated hand (and energy) alongside the
                # single card, so a divergence can be diagnosed instead of just
                # detected: whether the hand differs in order, in count, or in
                # contents tells you which subsystem is wrong.
                entry["sim_hand"] = [
                    str(getattr(c, "card_id", "")).replace("CardId.", "")
                    for c in shand
                ]
                entry["sim_energy"] = int(
                    getattr(shadow.combat_player_states[0], "energy", -1))
                if hidx is not None and 0 <= hidx < len(shand):
                    entry["card_index"] = hidx
                    entry["card_id"] = str(
                        getattr(shand[hidx], "card_id", "")).replace("CardId.", "")
                else:
                    # Unvalidatable: mark it so the guard REFUSES to replay
                    # blind rather than falling through unchecked.
                    entry["card_index"] = hidx
                    entry["card_id"] = None
                    entry["unvalidatable"] = True
            draw_before = len(shadow.combat_player_states[0].draw)
            try:
                apply_combat_action(shadow, int(a))
            except Exception:
                # The clone could not follow its own plan; everything after
                # this point is unlabelled, so stop the queue here rather than
                # emitting actions we cannot check.
                logger.warning("Combat: plan annotation aborted at action %s -- "
                               "truncating queue (%d/%d actions kept)",
                               a, len(queued), len(plan.actions))
                queued.append(entry)
                break
            queued.append(entry)

            # TRUNCATE AT THE FIRST RESHUFFLE.
            #
            # When the draw pile cannot satisfy a draw, the discard is folded
            # back in and shuffled. The simulator's shuffle RNG has no relation
            # to the game's (and the game sorts first via StableShuffle, which
            # the simulator does not), so from that point on the two decks are
            # genuinely different -- not merely reordered.
            #
            # Measured live: sim hand [BODYGUARD, STRIKE, DEFEND, STRIKE,
            # UNLEASH] against live [DEFEND, DEFEND, STRIKE, DEFEND, UNLEASH]
            # -- same length, same energy, different cards.
            #
            # The plan is sound up to the reshuffle and fiction after it, so
            # keep the sound prefix and let the next decision replan from real
            # state. That is strictly better than replaying actions chosen for
            # a deck the game does not have.
            if len(shadow.combat_player_states[0].draw) > draw_before:
                # ONLY TRUNCATE WHEN THE RESHUFFLE IS ACTUALLY UNPREDICTABLE.
                #
                # The comment above described the state of the world before
                # shuffle parity existed: the simulator drew from a .NET
                # Random stream while the game drew from xoshiro256**, and it
                # skipped the sort that the game's StableShuffle performs. Both
                # are fixed now, and combat_reconstruct marks a combat that
                # carries the game's real stream with _force_stable_reshuffle.
                #
                # For such a combat the post-reshuffle deck is predictable, so
                # truncating throws away good plan for nothing -- 8 of 12 plans
                # were still being cut short after the parity fix landed.
                #
                # Keep going and let the divergence detector adjudicate: if
                # parity is imperfect, the very next validated action reports a
                # CONTENTS mismatch and we learn it from data instead of
                # assuming it either way.
                if getattr(shadow, "_force_stable_reshuffle", False):
                    logger.info("Combat: plan crosses a draw-pile reshuffle -- "
                                "shuffle parity is active, keeping the full "
                                "%d-action plan", len(plan.actions))
                else:
                    logger.info("Combat: plan crosses a draw-pile reshuffle -- "
                                "keeping the %d sound action(s) and replanning "
                                "after (dropped %d)",
                                len(queued), len(plan.actions) - len(queued))
                    break
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
    """Canonical card name for COMPARING a prediction against the live game.

    Must tolerate the ``_CARD`` suffix the way _to_card_id does. The wire and
    the simulator disagree on it for some cards -- the simulator registers
    SERPENT_FORM_CARD where the game sends SERPENT_FORM -- and without this
    the divergence detector reported those as "different cards" and blamed
    shuffle parity for a naming mismatch. Measured live: a sim hand ending
    SERPENT_FORM_CARD against a live hand holding SERPENT_FORM, counted as a
    CONTENTS divergence.

    Also strips mod namespaces for the same reason: the same card can arrive
    as ACTSFROMTHEPAST-X or X depending on which side produced the string.
    """
    name = str(v).upper().replace("CARDID.", "").replace("-", "_").strip()
    for prefix in ("ACTSFROMTHEPAST_", "ACT4HEART_", "DOWNFALL_", "BASE_"):
        if name.startswith(prefix):
            name = name[len(prefix):]
            break
    if name.endswith("_CARD"):
        name = name[: -len("_CARD")]
    return name

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

_RUN_POLICY = ["heuristic"]   # "heuristic" | "rl"
_RL_RUN = [None]              # [loaded hierarchical run-agent model]

# The phases the run agent is allowed to drive. Combat is deliberately absent:
# these checkpoints were trained in HierarchicalRunEnv, which resolves every
# combat through a separate controller before the run policy is asked to act,
# so the run policy has never once emitted a combat action. Letting it try
# would be sampling a slice of its action space that got no gradient.
_RL_RUN_PHASES_ACTIVE = [None]   # [frozenset] -- the subset actually in use

_RL_RUN_PHASES = frozenset({
    Phase.MAP_SELECT, Phase.CARD_REWARD, Phase.REST,
    Phase.SHOP, Phase.EVENT, Phase.TREASURE, Phase.BOSS_RELIC,
})


def _rl_run_action(state: dict[str, Any]) -> int | None:
    """Next out-of-combat action from the trained run agent, or None.

    Same shape as _rl_combat_action, and for the same reason: the bridge's
    own RunStateAdapter emits a 151-dim vector while every trained run agent
    expects the 4778-dim RICH vector, so the payload has to be turned back
    into real simulator objects and encoded with the encoder the model
    trained against. The ACTION side still goes through RunStateAdapter --
    the 157-action layout is shared, and run_env.py is authoritative for both
    the env and the bridge.

    Returns None on any doubt at all (no model, unusable payload, empty or
    disagreeing mask); the caller then plays the heuristic. A wrong action
    here is worse than a heuristic one, because it looks deliberate.
    """
    model = _RL_RUN[0]
    if model is None:
        return None

    import numpy as _np

    from sts2_env.bridge.run_reconstruct import encode_run_observation
    from sts2_env.bridge.run_state_adapter import RunStateAdapter

    obs = encode_run_observation(state)
    if obs is None:
        return None

    mask = _np.asarray(RunStateAdapter().compute_action_mask(state), dtype=bool)
    if not mask.any():
        return None

    action, _ = model.predict(obs, action_masks=mask, deterministic=True)
    action = int(action)
    if not (0 <= action < mask.size) or not mask[action]:
        # predict() should never return a masked-out index; if it does, the
        # mask and the model disagree about the layout and we should not
        # guess which one is right.
        logger.error(
            "run agent returned action %d which the mask forbids -- falling "
            "back to the heuristic for this decision.", action,
        )
        return None
    return action


def _selectable_indexes(state: dict[str, Any]) -> list[int] | None:
    """Indexes this payload can actually accept, or None if it does not say.

    Returning None rather than [] for an unknown payload shape matters: []
    would read as "nothing is selectable" and suppress every choice.
    """
    for key in ("options", "nodes", "bundles", "cards"):
        entries = state.get(key)
        if not isinstance(entries, list) or not entries:
            continue
        out = []
        for fallback, entry in enumerate(entries):
            if not isinstance(entry, dict):
                out.append(fallback)
                continue
            if not bool(entry.get("enabled", True)):
                continue
            out.append(_read_index(entry, fallback))
        return out
    return None


def _validate_choice(
    state: dict[str, Any], decoded: dict[str, Any]
) -> dict[str, Any]:
    """Refuse to send an index the current screen cannot accept.

    _send_noncombat_action called client.<method>(*args) with whatever the
    policy produced, unchecked. Live 2026-07-31 the run agent answered a shop
    with choose([9]) then choose([8]); the game went silent, the runner timed
    out after 60s, and the mod terminated the run.

    A model trained against a differently-sized option list will emit
    out-of-range indexes, and the failure mode is a HANG rather than an error
    -- the worst kind, because it costs the whole run and looks like the game
    is thinking. Falling back to the heuristic keeps the run alive and, more
    usefully, says loudly that the action space and the payload disagree.
    """
    if decoded.get("method") != "choose":
        return decoded
    args = decoded.get("args") or []
    if len(args) != 1 or not isinstance(args[0], int):
        return decoded

    allowed = _selectable_indexes(state)
    if allowed is None or args[0] in allowed:
        return decoded

    fallback = _heuristic_choice_for_log(state)
    if not isinstance(fallback, int) or (allowed and fallback not in allowed):
        fallback = allowed[0] if allowed else None
    if fallback is None:
        logger.error(
            "%s: agent chose index %d but the payload offers %s, and there is "
            "no usable fallback.",
            state.get("type"), args[0], allowed,
        )
        return decoded

    logger.warning(
        "%s: agent chose index %d but the payload only offers %s -- sending "
        "%d instead. An out-of-range index does not error, it HANGS the room "
        "until the mod terminates the run.",
        state.get("type"), args[0], allowed, fallback,
    )
    return {**decoded, "args": [fallback]}


#: [(screen fingerprint, method, args), consecutive repeats]
_LAST_NONCOMBAT_CHOICE: list[Any] = [None, 0]

#: Break at 2 so we stay UNDER the mod's own limit.
#:
#: RlNonCombatRoomHandlers aborts the room after MaxRepeatedChoices = 3
#: identical clicks, and RlAutoSlayer turns that into "run terminated". So a
#: guard that only acted on the 3rd repeat would never run -- the mod would
#: already have killed the run.
NONCOMBAT_REPEAT_LIMIT = 2


def _screen_fingerprint(state: dict[str, Any]) -> tuple:
    """Identify a screen by its offered options, not by arrival order.

    A genuinely new screen must reset the counter, or the guard would start
    rotating choices on unrelated screens that merely share an index.
    """
    options = state.get("options") or state.get("cards") or state.get("nodes") or []
    labels = []
    for opt in options:
        if isinstance(opt, dict):
            labels.append(str(opt.get("id") or opt.get("label")
                              or opt.get("action") or opt.get("index")))
        else:
            labels.append(str(opt))
    return (state.get("type", ""), state.get("floor"), tuple(labels))


def _alternative_choice(state: dict[str, Any], current: int) -> int | None:
    """Another option to try, preferring the one that LEAVES the screen.

    A screen that will not advance is usually an event RESULT page, where the
    only thing left to do is Proceed. Rotating to an arbitrary other option
    would be a guess; Proceed is the actual answer, so try it first.
    """
    proceed = _proceed_option(state)
    if proceed is not None:
        index = _read_index(proceed, -1)
        if index != current and index >= 0:
            return index

    options = _enabled_options(state)
    indexes = [_read_index(opt, i) for i, opt in enumerate(options)]
    others = [i for i in indexes if i != current]
    return others[0] if others else None


def _break_repeated_choice(
    state: dict[str, Any], decoded: dict[str, Any]
) -> dict[str, Any]:
    """Vary a choice that is being made over and over on the same screen.

    run_env has had an anti-dither guard since the forensics that found
    deterministic-argmax policies toggling one option forever; the LIVE path
    had none, so the same dithering ran unchecked until the mod's repeat
    limit terminated the run. Measured 2026-07-31: three identical
    ``event -> choose([0])`` calls, then "Run finished: terminated".

    Rotating to another legal option is a worse decision than the policy
    wanted and a far better one than a dead run -- the same trade the
    card-reward breaker makes.
    """
    key = (_screen_fingerprint(state), decoded.get("method"),
           tuple(decoded.get("args") or ()))
    if key == _LAST_NONCOMBAT_CHOICE[0]:
        _LAST_NONCOMBAT_CHOICE[1] += 1
    else:
        _LAST_NONCOMBAT_CHOICE[0] = key
        _LAST_NONCOMBAT_CHOICE[1] = 1

    if _LAST_NONCOMBAT_CHOICE[1] <= NONCOMBAT_REPEAT_LIMIT:
        return decoded
    if decoded.get("method") != "choose":
        return decoded

    args = decoded.get("args") or []
    if len(args) != 1 or not isinstance(args[0], int):
        return decoded

    alternative = _alternative_choice(state, args[0])
    if alternative is None:
        logger.warning(
            "%s: choice %s repeated %d times and there is no other enabled "
            "option to try. The mod will abort this room shortly.",
            state.get("type"), args, _LAST_NONCOMBAT_CHOICE[1],
        )
        return decoded

    logger.warning(
        "%s: choice %s repeated %d times without the screen changing -- "
        "trying option %d instead so the mod does not terminate the run.",
        state.get("type"), args, _LAST_NONCOMBAT_CHOICE[1], alternative,
    )
    _LAST_NONCOMBAT_CHOICE[1] = 0
    return {**decoded, "args": [alternative]}


def _apply_card_reward_latch(
    state: dict[str, Any], decoded: dict[str, Any]
) -> dict[str, Any]:
    """Carry a card-reward decline from the card screen to the rewards screen.

    The run agent decides "skip" on the card_reward payload but the game only
    honours a decline on the REWARDS screen, so the two have to be joined up
    here. Without this the agent opens the reward, cancels out of it, is
    offered it again, and never leaves the room.

    Also enforces the livelock breaker: after CARD_REWARD_MAX_REOFFERS
    presentations of the same reward, take a card. That is a worse deck and a
    finished run, versus a perfect deck and a run that never ends.
    """
    msg_type = state.get("type", "")

    if msg_type == BridgeStateType.CARD_REWARD:
        if decoded.get("method") == "skip":
            if card_reward_should_force_take(state):
                logger.warning(
                    "CARD_REWARD re-offered %d times this room -- taking a "
                    "card instead of skipping again. Skip only CANCELS the "
                    "selection screen; the reward is not consumed until the "
                    "rewards screen is left.",
                    _CARD_REWARD_LATCH[2],
                )
                return {"phase": "run", "method": "choose", "args": [0]}
            note_card_reward_declined(state)

    elif msg_type == BridgeStateType.REWARD_SCREEN:
        if decoded.get("method") == "choose" and card_reward_is_declined(state):
            options = _enabled_options(state)
            proceed = _first_matching_option(
                options, actions=(REWARD_PROCEED_ACTION,))
            if proceed is not None:
                index = _read_index(proceed, DEFAULT_CHOICE_INDEX)
                if decoded.get("args") != [index]:
                    logger.info(
                        "REWARD_SCREEN: overriding %s(%s) with proceed(%d) -- "
                        "the agent already declined this room's card reward, "
                        "and re-opening it would only re-offer it.",
                        decoded.get("method"), decoded.get("args"), index,
                    )
                return {"phase": "run", "method": "choose", "args": [index]}

    return decoded


def _try_rl_run_action(client: Any, state: dict[str, Any], verbose: bool) -> bool:
    """Play one out-of-combat action with the run agent. False = not handled."""
    action = _rl_run_action(state)
    if action is None:
        return False

    from sts2_env.bridge.run_state_adapter import RunStateAdapter

    decoded = RunStateAdapter().decode_action(action, state)
    if decoded.get("phase") == "combat":
        # Should be unreachable: _RL_RUN_PHASES excludes combat. Refuse
        # rather than dispatch a combat action from the run policy.
        logger.error("run agent decoded a COMBAT action in phase %r -- "
                     "refusing; using the heuristic.", state.get("type"))
        return False

    decoded = _apply_card_reward_latch(state, decoded)
    decoded = _validate_choice(state, decoded)
    decoded = _break_repeated_choice(state, decoded)

    # Log what the HEURISTIC would have done next to what the agent did.
    #
    # The option ORDERING between the training env (which indexes internal sim
    # lists) and the bridge decoder (which indexes wire payload arrays) is
    # only proven aligned for shop and non-first map moves; rest, event, boss
    # relic, treasure and the first map move of each act are unproven. A
    # positionally-trained policy reading a differently-ordered list picks the
    # wrong option while looking perfectly healthy, and nothing in the game
    # would say so.
    #
    # This does not detect that on its own -- agreement is not correctness --
    # but a phase where the two NEVER agree, or where the agent always picks
    # index 0, is the signature of exactly that failure, and it is free to
    # collect.
    heuristic = _heuristic_choice_for_log(state)
    logger.info("RUN-RL (%s): action %d -> %s(%s) | heuristic would pick %s",
                state.get("type"), action, decoded.get("method"),
                decoded.get("args"), heuristic)
    _send_noncombat_action(client, decoded)
    return True


def _heuristic_choice_for_log(state: dict[str, Any]) -> Any:
    """What the hand-written rules would have chosen. Logging only."""
    phase = _phase_for_state(state)
    msg_type = state.get("type", "")
    try:
        if phase == Phase.MAP_SELECT:
            return _pick_map_node(state)
        if phase == Phase.CARD_REWARD:
            if msg_type == BridgeStateType.CARD_BUNDLE:
                return _pick_card_bundle_index(state)
            if msg_type == BridgeStateType.CARD_SELECT:
                return _pick_card_select_indexes(state)
            if msg_type == BridgeStateType.REWARD_SCREEN:
                return _pick_reward_screen_option(state)
            return _pick_card_reward_index(state)
        if phase == Phase.REST:
            return _pick_rest_option(state)
        if phase == Phase.SHOP:
            return _pick_shop_option(state)
        if phase == Phase.EVENT:
            if msg_type == BridgeStateType.CRYSTAL_SPHERE:
                return _pick_crystal_sphere_option(state)
            return _pick_event_option(state)
        if phase == Phase.TREASURE:
            return _pick_treasure_option(state)
        if phase == Phase.BOSS_RELIC:
            return _pick_boss_relic_option(state)
    except Exception as exc:  # logging must never break a decision
        return f"<error {type(exc).__name__}>"
    return None


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

    # DOES THE GAME PLAY THE CARD THE AGENT ACTUALLY CHOSE?
    #
    # The agent picks an action index against the RECONSTRUCTED combat, but
    # that index is decoded and sent against the WIRE payload. Those are two
    # different objects, and the action means "play hand slot i" -- so if the
    # reconstruction's hand order ever differs from the wire's, the game plays
    # a DIFFERENT CARD than the one the agent chose, silently and with a
    # perfectly legal-looking action.
    #
    # Nothing else would catch it: the move is legal, the game accepts it, and
    # there is no plan to validate because an RL combat agent emits one action
    # at a time. Resolve the intended card on both sides and compare.
    _verify_combat_action_targets_intended_card(state, combat, action)
    return action


def _verify_combat_action_targets_intended_card(
        state: dict[str, Any], combat: Any, action: int) -> bool:
    """Log loudly if the wire hand slot holds a different card than the sim's."""
    try:
        from sts2_env.gym_env.action_space import action_to_card_and_target

        decoded = action_to_card_and_target(action)
    except Exception:
        return True
    if not decoded:
        return True
    hand_index = decoded[0] if isinstance(decoded, tuple) else None
    if not isinstance(hand_index, int):
        return True

    try:
        sim_hand = combat.combat_player_states[0].hand
    except Exception:
        return True
    wire_hand = state.get("hand") or []
    if not (0 <= hand_index < len(sim_hand)) or hand_index >= len(wire_hand):
        return True

    intended = _norm_card(str(getattr(sim_hand[hand_index], "card_id", "")))
    actual = _norm_card(str(wire_hand[hand_index].get("id")
                            or wire_hand[hand_index].get("card_id") or ""))
    if intended and actual and intended != actual:
        logger.error(
            "ACTION MISMATCH: agent chose hand slot %d intending %s, but the "
            "game holds %s there -- the wrong card would be played. sim hand "
            "%s vs wire hand %s",
            hand_index, intended, actual,
            [_norm_card(str(getattr(c, "card_id", ""))) for c in sim_hand],
            [_norm_card(str(c.get("id") or c.get("card_id") or ""))
             for c in wire_hand],
        )
        return False
    return True


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
    parser.add_argument(
        "--run-policy",
        choices=["heuristic", "rl"],
        default="heuristic",
        help="Who makes the OUT-OF-COMBAT decisions (map, card rewards, "
             "rest, shop, event, treasure, boss relic): 'heuristic' = the "
             "hand-written _pick_* rules (default), 'rl' = a trained "
             "hierarchical run agent via --rl-run-model. Combat is "
             "unaffected either way; it stays with --combat-policy.",
    )
    parser.add_argument("--rl-run-model", default=None,
                        help="Trained hierarchical run agent (157 actions / "
                             "4778-dim rich obs) for --run-policy rl.")
    parser.add_argument(
        "--rl-run-phases", default=None,
        help="Comma-separated subset of out-of-combat phases the RL run "
             "agent may drive; the rest fall back to the heuristics. "
             "Choices: MAP_SELECT, CARD_REWARD, REST, SHOP, EVENT, "
             "TREASURE, BOSS_RELIC. Default: all of them. "
             "Measured live 2026-07-30 (session 12, 81 decisions): the run "
             "agent picked card-reward slot 2 on 11 of 11 offers -- a "
             "collapsed policy on the primary deck-building decision -- "
             "while map choices varied sensibly. Use this to keep RL where "
             "it is choosing and heuristics where it is not.")
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
        default=0.2,
        help="Seconds to pause before each combat action (end turn is always "
             "instant). Defaults to 0.2s: fast enough not to slow a batch "
             "materially, slow enough that card plays are watchable and that "
             "the game's play/VFX animations settle before the next action.",
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
        run_policy=args.run_policy,
        rl_run_model=args.rl_run_model,
        rl_run_phases=args.rl_run_phases,
        llm_model=args.llm_model,
        llm_gpu_layers=args.llm_gpu_layers,
        llm_ctx=args.llm_ctx,
        llm_max_tokens=args.llm_max_tokens,
        llm_temperature=args.llm_temperature,
    )


if __name__ == "__main__":
    main()
