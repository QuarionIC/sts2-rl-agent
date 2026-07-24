"""Full-run Gymnasium environment with the rich observation (v1).

Subclasses :class:`~sts2_env.gym_env.run_env.STS2RunEnv` (same ``Discrete(157)``
action space, masks, and step dispatch -- those are bridge-aligned and
unchanged) but replaces the observation with the rich vector and the sparse
reward with terminal win/death/truncation plus the potential-based shaping
term (PBRS) from :class:`~sts2_env.gym_env.reward_config.RewardConfig`:
every step emits ``F = gamma_shape * Phi(s') - Phi(s)`` (times the constant
``shaping_scale``; set to 0 for pure-sparse eval), with ``Phi := 0`` at
terminal states.

Adds ``max_act_count``: the episode terminates with a WIN as soon as the
player advances past act ``max_act_count - 1`` (i.e. that act's boss died
and the boss-relic screen resolved), enabling the act-count curriculum axis.

Truncation (step-limit timeout) is NOT scored as a death: it adds
``cfg.truncation`` (-1.0 by default: with combats capped at 30 turns, a
step-limit truncation is a non-combat stall and must score as a loss) and
tags ``info["truncated"]``.
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
        # PBRS bookkeeping: previous potential Phi(s) and the enemy-HP
        # depletion value carried between combats (see RewardConfig).
        self._phi_prev: float = 0.0
        self._enemy_down: float = 0.0

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
        assert self._mgr is not None
        combat = self._mgr.get_combat_state()
        self._enemy_down = (
            self.reward_config.enemy_down(combat) if combat is not None else 0.0
        )
        self._phi_prev = self.reward_config.potential(self._mgr, self._enemy_down)
        return obs, info

    def step(
        self, action: int,
    ) -> tuple[np.ndarray, float, bool, bool, dict[str, Any]]:
        assert self._mgr is not None, "Must call reset() before step()"
        mgr = self._mgr
        rs = mgr.run_state
        cfg = self.reward_config

        was_in_combat = mgr.phase == RunManager.PHASE_COMBAT

        # Parent handles dispatch, terminal detection, and the sparse
        # terminal reward (recomputed below). It calls the overridden
        # ``_encode_obs``, so ``obs`` is already the rich vector.
        obs, _, terminated, truncated, info = super().step(action)

        reward = 0.0

        # --- PBRS bookkeeping: enemy-HP depletion, carried between combats ---
        combat_now = mgr.get_combat_state()
        if combat_now is not None:
            self._enemy_down = cfg.enemy_down(combat_now)
        elif was_in_combat and not rs.player.is_dead:
            # Combat just ended with the player alive: all enemies down.
            # (get_combat_state() is None once the phase advances, so the
            # final kill's depletion is credited here.)
            self._enemy_down = 1.0

        # --- act cap: win early when max_act_count acts are cleared ---
        won = terminated and mgr.player_won
        if not terminated and not truncated and rs.current_act_index >= self.max_act_count:
            terminated = True
            won = True

        # --- PBRS shaping: F = gamma_shape * Phi(s') - Phi(s), Phi=0 at terminal ---
        phi_next = (
            0.0 if (terminated or truncated)
            else cfg.potential(mgr, self._enemy_down)
        )
        reward += cfg.shaping_reward(self._phi_prev, phi_next)
        self._phi_prev = phi_next

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
