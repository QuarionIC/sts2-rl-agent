"""Full-run Gymnasium environment with the rich observation (v1).

Subclasses :class:`~sts2_env.gym_env.run_env.STS2RunEnv` (same ``Discrete(157)``
action space, masks, and step dispatch -- those are bridge-aligned and
unchanged) but replaces the observation with the rich vector and the sparse
reward with the shaped reward from
:class:`~sts2_env.gym_env.reward_config.RewardConfig` (constant
``shaping_scale``; set to 0 for pure-sparse eval).

Adds ``max_act_count``: the episode terminates with a WIN as soon as the
player advances past act ``max_act_count - 1`` (i.e. that act's boss died
and the boss-relic screen resolved), enabling the act-count curriculum axis.

Truncation (step-limit timeout) is NOT scored as a death: it adds
``cfg.truncation`` (0.0 by default) and tags ``info["truncated"]``.
A forced loss from a simulator bug (``info["sim_error"]``) is also not
scored as a death (terminal reward 0.0).
"""

from __future__ import annotations

import logging
from typing import Any

import numpy as np
from gymnasium import spaces

from sts2_env.gym_env.reward_config import RewardConfig
from sts2_env.gym_env.rich_observation import (
    RICH_OBS_HIGH,
    RICH_OBS_LOW,
    RICH_OBS_SIZE,
    RichObservationEncoder,
)
from sts2_env.gym_env.run_env import (
    STS2RunEnv,
)
from sts2_env.run.run_manager import RunManager

logger = logging.getLogger(__name__)

#: Default step cap for full-run episodes (a full 4-act run finishes well
#: under this; the cap only catches pathological non-combat stalls, e.g.
#: toggling a selection screen forever -- those don't consume combat turns).
DEFAULT_RUN_MAX_STEPS = 3_000

#: Combat-turn cap for training/eval runs. A real fight essentially never
#: needs 30 turns; a combat that drags this long is a stall (e.g. infinite
#: blocking) and is scored as a DEATH (-1), giving an unambiguous "stalling
#: loses" signal instead of an unlabeled truncation. (STS2RunEnv's own
#: default of 200 is kept for non-training uses.)
DEFAULT_RICH_MAX_COMBAT_TURNS = 30


class RichSTS2RunEnv(STS2RunEnv):
    """Full-run env with rich observation, shaped reward, and act cap.

    Parameters
    ----------
    character_id : character to play (default ``"Necrobinder"``).
    ascension_level : ascension level (default 10).
    max_act_count : how many acts the episode covers (1-4). The episode
        terminates with a WIN when the player finishes act
        ``max_act_count`` (act indices 0..max_act_count-1). 4 = full run
        including the Act 4 Heart.
    reward_config : reward terms; ``shaping_scale`` is a constant knob
        settable via :meth:`set_shaping_scale` (0 for pure-sparse eval).
    max_steps / max_combat_turns / render_mode : as in STS2RunEnv
        (defaults: max_steps=DEFAULT_RUN_MAX_STEPS=3000; max_combat_turns=
        DEFAULT_RICH_MAX_COMBAT_TURNS=30 -- combats exceeding 30 turns are
        scored as deaths).
    """

    def __init__(
        self,
        character_id: str = "Necrobinder",
        ascension_level: int = 10,
        max_act_count: int = 4,
        reward_config: RewardConfig | None = None,
        max_steps: int = DEFAULT_RUN_MAX_STEPS,
        max_combat_turns: int = DEFAULT_RICH_MAX_COMBAT_TURNS,
        render_mode: str | None = None,
    ):
        if not 1 <= max_act_count <= 4:
            raise ValueError(f"max_act_count must be in 1..4, got {max_act_count}")
        super().__init__(
            character_id=character_id,
            ascension_level=ascension_level,
            max_steps=max_steps,
            max_combat_turns=max_combat_turns,
            render_mode=render_mode,
        )
        # Replace the observation space with the rich one (action space,
        # masks, and step dispatch are inherited unchanged).
        self.observation_space = spaces.Box(
            low=RICH_OBS_LOW, high=RICH_OBS_HIGH, shape=(RICH_OBS_SIZE,), dtype=np.float32
        )
        self.max_act_count = max_act_count
        self.reward_config = reward_config or RewardConfig()
        self._encoder = RichObservationEncoder()
        self._combat_hp_start: int | None = None

    # ------------------------------------------------------------------

    def set_shaping_scale(self, scale: float) -> None:
        self.reward_config.shaping_scale = scale
        self.reward_config.clamp()

    # ------------------------------------------------------------------
    # Gymnasium API
    # ------------------------------------------------------------------

    def reset(
        self,
        seed: int | None = None,
        options: dict[str, Any] | None = None,
    ) -> tuple[np.ndarray, dict[str, Any]]:
        # The parent calls the (overridden) ``_encode_obs``, so the returned
        # obs is already the rich vector.
        obs, info = super().reset(seed=seed, options=options)
        self._combat_hp_start = None
        return obs, info

    def step(
        self, action: int,
    ) -> tuple[np.ndarray, float, bool, bool, dict[str, Any]]:
        assert self._mgr is not None, "Must call reset() before step()"
        mgr = self._mgr
        rs = mgr.run_state
        cfg = self.reward_config

        prev_act = rs.current_act_index
        prev_floor = rs.total_floor
        was_in_combat = mgr.phase == RunManager.PHASE_COMBAT
        if was_in_combat and self._combat_hp_start is None:
            combat = mgr.get_combat_state()
            if combat is not None:
                self._combat_hp_start = combat.primary_player.current_hp

        # Parent handles dispatch, terminal detection, and the sparse
        # terminal reward (recomputed below). It calls the overridden
        # ``_encode_obs``, so ``obs`` is already the rich vector.
        obs, _, terminated, truncated, info = super().step(action)

        reward = 0.0

        # --- shaping: floor progression ---
        floors = rs.total_floor - prev_floor
        if floors > 0:
            reward += cfg.floor_reward(floors)

        # --- shaping: act completion ---
        acts_done = rs.current_act_index - prev_act
        if terminated and mgr.player_won:
            acts_done = max(acts_done, 1)  # final act completion on a won run
        if acts_done > 0:
            reward += cfg.act_completion_reward(acts_done)

        # --- shaping: combat win HP retention ---
        in_combat = mgr.phase == RunManager.PHASE_COMBAT
        if was_in_combat and not in_combat:
            if not rs.player.is_dead and not (terminated and not mgr.player_won):
                hp_start = self._combat_hp_start or rs.player.current_hp
                reward += cfg.combat_win_reward(hp_start, rs.player.current_hp)
            self._combat_hp_start = None
        elif in_combat and self._combat_hp_start is None:
            combat = mgr.get_combat_state()
            if combat is not None:
                self._combat_hp_start = combat.primary_player.current_hp

        # --- act cap: win early when max_act_count acts are cleared ---
        won = terminated and mgr.player_won
        if not terminated and not truncated and rs.current_act_index >= self.max_act_count:
            terminated = True
            won = True

        # --- terminal rewards (never annealed) ---
        if terminated:
            if info.get("sim_error"):
                # Forced loss from a simulator bug: do not score as death.
                reward += 0.0
            else:
                reward += cfg.terminal_reward(won)
        elif truncated:
            # Step-limit timeout is NOT a death; bootstrap instead.
            reward += cfg.truncation
            info["truncated"] = True

        if terminated or truncated:
            info["won"] = won
        return obs, float(reward), terminated, truncated, info

    # ------------------------------------------------------------------
    # Observation encoding (overrides the 151-dim parent encoding)
    # ------------------------------------------------------------------

    def _encode_obs(self) -> np.ndarray:
        if self._mgr is None:
            return np.zeros(RICH_OBS_SIZE, dtype=np.float32)
        return self._encoder.encode_run(self._mgr)
