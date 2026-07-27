"""Hierarchical full-run environment: a RUN agent that delegates fights.

Why this exists
---------------
Forensics on the flat 20M-step policy found the ceiling was not combat
execution but deck construction: runs reached floor 5-8 elites carrying the
pure 10-card starter deck with zero upgrades, and 5 of 6 such positions had
0/150 stochastic playouts survive -- arithmetically lost before the first
card was played. The cause is credit assignment. In the flat env a run is
~250 steps, the overwhelming majority of them in-combat, so the handful of
deck-shaping decisions (take a card, smith, buy) are buried. With gamma 0.99
a terminal reward reaches a floor-3 card pick discounted by 0.99^250 ~= 0.08;
the policy consequently learned to skip 88% of card rewards and never smith.

Splitting the problem fixes exactly that. Here a COMBAT agent plays fights
atomically and the RUN agent only makes the decisions you actually deliberate
over: where to move on the map, what to do in a non-combat room, and which
rewards to take after a win. That collapses an episode to ~20-40 decisions, so
the same gamma 0.99 discounts the terminal reward by ~0.99^40 = 0.67 at a
card pick -- an order of magnitude more signal on precisely the choices that
were being starved.

Design notes
------------
* The action space, masks and step dispatch are inherited UNCHANGED from
  :class:`RichSTS2RunEnv`. No mask surgery is needed: combats are consumed
  inside :meth:`step`, so the run agent is only ever asked to act while the
  phase is non-combat, and the parent's mask is already correct there
  (including pending run choices, which legitimately reuse the combat slice).
* Shaping stays policy-invariant. Summing the parent's per-step PBRS terms
  across an auto-played combat telescopes to ``gamma^k*Phi(end) - Phi(start)``,
  so the run agent sees a consistent potential-based signal over the fight
  rather than an ad-hoc combat bonus.
* The combat controller is injectable, so the same env serves bootstrap
  training (scripted controller), joint training (frozen combat policy), and
  evaluation (best combat policy).
"""

from __future__ import annotations

import logging
from typing import Any, Protocol

import numpy as np

from sts2_env.core.constants import ACTION_SPACE_SIZE as COMBAT_ACTIONS
from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
from sts2_env.run.run_manager import RunManager

logger = logging.getLogger(__name__)

#: Hard cap on env-steps spent inside a single auto-played combat. A real
#: fight resolves in well under this; exceeding it means the combat
#: controller is stuck in a selection cycle, which is surfaced rather than
#: silently spun on.
DEFAULT_MAX_COMBAT_STEPS = 400


class CombatController(Protocol):
    """Chooses one combat-slice action given the env observation and mask."""

    def act(self, obs: np.ndarray, mask: np.ndarray) -> int:
        ...


class PolicyCombatController:
    """Wraps a trained SB3 (Maskable)PPO model as the combat controller.

    Accepts models trained on EITHER action space:

    * 115 -- a dedicated combat agent from ``RichSTS2CombatEnv``. It is handed
      the 115-wide combat slice, and since ``_LAYOUT.combat_start == 0`` the
      index it returns is already a valid full-run action.
    * 157 -- a full-run policy reused as a combat controller.

    ``expects_combat_obs`` mirrors the same split on the observation side:
    ``RichSTS2CombatEnv`` trains on ``encode_combat`` (run segment zeroed)
    while the run env emits ``encode_run``. Feeding the wrong one is silent --
    the vectors are both 4778-wide -- so it is resolved from the model's
    action space rather than left to the caller.
    """

    def __init__(self, model: Any, deterministic: bool = True,
                 expects_combat_obs: bool | None = None):
        self.model = model
        self.deterministic = deterministic
        self.action_dim = int(model.policy.action_space.n)
        self.expects_combat_obs = (
            (self.action_dim == COMBAT_ACTIONS) if expects_combat_obs is None
            else bool(expects_combat_obs)
        )

    def act(self, obs: np.ndarray, mask: np.ndarray) -> int:
        action, _ = self.model.predict(
            obs[: self.model.observation_space.shape[0]],
            action_masks=mask[: self.action_dim],
            deterministic=self.deterministic,
        )
        return int(action)


class FirstLegalCombatController:
    """Degenerate controller: always the lowest legal action.

    Only useful as a plumbing check -- action 0 of the combat slice is END
    TURN, so this ends every turn immediately and loses nearly everything.
    """

    def act(self, obs: np.ndarray, mask: np.ndarray) -> int:
        legal = np.flatnonzero(np.asarray(mask, dtype=bool))
        return int(legal[0]) if legal.size else 0


class RandomCombatController:
    """Uniform over legal actions. A real (weak) baseline for the run agent."""

    def __init__(self, seed: int = 0):
        self.rng = np.random.default_rng(seed)

    def act(self, obs: np.ndarray, mask: np.ndarray) -> int:
        legal = np.flatnonzero(np.asarray(mask, dtype=bool))
        return int(self.rng.choice(legal)) if legal.size else 0


class HierarchicalRunEnv(RichSTS2RunEnv):
    """Full-run env in which combats are played by a separate combat agent.

    Parameters
    ----------
    combat_controller : object with ``act(obs, mask) -> int``. When ``None``
        the env falls back to :class:`RandomCombatController`, which is weak
        but non-degenerate -- useful for smoke tests, not for training.
    max_combat_steps : per-combat env-step cap (see module constant).
    Remaining parameters are forwarded to :class:`RichSTS2RunEnv`.

    Extra ``info`` keys
    -------------------
    ``combats_played`` / ``combats_won``
        Cumulative counts for the episode.
    ``combat_hp_lost``
        HP lost in the combat resolved during this step (0 when none was).
    ``run_decisions``
        Number of run-agent decisions taken this episode -- the quantity the
        whole design exists to shrink.
    """

    def __init__(
        self,
        *args: Any,
        combat_controller: CombatController | None = None,
        max_combat_steps: int = DEFAULT_MAX_COMBAT_STEPS,
        **kwargs: Any,
    ):
        super().__init__(*args, **kwargs)
        self.combat_controller: CombatController = (
            combat_controller if combat_controller is not None
            else RandomCombatController()
        )
        self.max_combat_steps = int(max_combat_steps)
        self._combats_played = 0
        self._combats_won = 0
        self._run_decisions = 0
        self._combat_stuck = 0
        self._player_select_fallbacks = 0

    # ------------------------------------------------------------------

    def set_combat_controller(self, controller: CombatController) -> None:
        """Swap the combat agent (e.g. after a fresh distillation round)."""
        self.combat_controller = controller

    # ------------------------------------------------------------------

    def reset(self, seed=None, options=None):
        obs, info = super().reset(seed=seed, options=options)
        self._combats_played = 0
        self._combats_won = 0
        self._run_decisions = 0
        self._combat_stuck = 0
        self._player_select_fallbacks = 0
        # A run never opens in combat, but resolve one defensively so the
        # contract "the run agent is only ever asked to act out of combat"
        # holds unconditionally.
        obs, extra, terminated, truncated, info = self._resolve_combats(obs, info)
        if terminated or truncated:
            logger.warning("episode ended during reset-time combat resolution")
        info = self._augment_info(info, hp_lost=0.0)
        return obs, info

    # ------------------------------------------------------------------

    def step(self, action: int):
        assert self._mgr is not None, "Must call reset() before step()"
        self._run_decisions += 1

        obs, reward, terminated, truncated, info = super().step(int(action))

        if not (terminated or truncated):
            obs, combat_reward, terminated, truncated, info = self._resolve_combats(
                obs, info
            )
            reward += combat_reward

        info = self._augment_info(info, hp_lost=info.get("_hp_lost", 0.0))
        return obs, float(reward), terminated, truncated, info

    # ------------------------------------------------------------------

    def _resolve_combats(self, obs, info):
        """Play out every combat until the next run-level decision point.

        Returns ``(obs, summed_reward, terminated, truncated, info)``. Summing
        the parent's PBRS terms here preserves the telescoping property, so
        the run agent's shaping stays potential-based across the fight.
        """
        mgr = self._mgr
        total = 0.0
        terminated = truncated = False
        hp_lost_total = 0.0

        while (
            mgr is not None
            and mgr.phase == RunManager.PHASE_COMBAT
            and not (terminated or truncated)
        ):
            combat = mgr.get_combat_state()
            hp_start = (
                float(combat.primary_player.current_hp)
                if combat is not None and combat.primary_player is not None
                else float(mgr.run_state.player.current_hp)
            )
            steps = 0
            while (
                mgr.phase == RunManager.PHASE_COMBAT
                and not (terminated or truncated)
                and steps < self.max_combat_steps
            ):
                mask = np.asarray(self.action_masks(), dtype=bool)
                act = self._combat_action(mask)
                if act is None:
                    break
                obs, r, terminated, truncated, info = super().step(act)
                total += r
                steps += 1

            if steps >= self.max_combat_steps and mgr.phase == RunManager.PHASE_COMBAT:
                # The controller is cycling. Surface it and abandon the
                # episode rather than spinning: a silent stall would be
                # scored as ordinary progress and corrupt training signal.
                self._combat_stuck += 1
                logger.warning(
                    "combat controller exceeded %d steps (floor %s) -- truncating",
                    self.max_combat_steps, mgr.run_state.total_floor,
                )
                truncated = True
                info = dict(info)
                info["combat_stuck"] = True
                break

            self._combats_played += 1
            alive = not mgr.run_state.player.is_dead
            if alive:
                self._combats_won += 1
            hp_lost_total += max(0.0, hp_start - float(mgr.run_state.player.current_hp))

        info = dict(info)
        info["_hp_lost"] = hp_lost_total
        return obs, total, terminated, truncated, info

    # ------------------------------------------------------------------

    def _combat_action(self, mask: np.ndarray) -> int | None:
        """Resolve one in-combat action from the combat controller.

        Handles the two representation gaps between the combat agent and the
        run env, and the one case the combat agent cannot express at all.
        """
        combat = self._mgr.get_combat_state() if self._mgr is not None else None
        legal_all = np.flatnonzero(mask)
        if not legal_all.size:
            return None

        combat_legal = mask[:COMBAT_ACTIONS]
        if not combat_legal.any():
            # Only out-of-slice actions are available -- in practice the
            # player-select slice (which creature acts). A 115-action combat
            # agent cannot represent this, so take the first legal option and
            # count it; if this ever becomes common the combat agent needs the
            # wider action space rather than a silent default.
            self._player_select_fallbacks += 1
            return int(legal_all[0])

        ctrl = self.combat_controller
        if getattr(ctrl, "expects_combat_obs", False) and combat is not None:
            obs_for_ctrl = self._encoder.encode_combat(combat)
        else:
            obs_for_ctrl = self._encode_obs()

        act = int(ctrl.act(obs_for_ctrl, mask))
        if not (0 <= act < mask.size) or not mask[act]:
            legal_combat = np.flatnonzero(combat_legal)
            act = int(legal_combat[0]) if legal_combat.size else int(legal_all[0])
        return act

    # ------------------------------------------------------------------

    def _augment_info(self, info: dict[str, Any], hp_lost: float) -> dict[str, Any]:
        info = dict(info)
        info.pop("_hp_lost", None)
        info["combats_played"] = self._combats_played
        info["combats_won"] = self._combats_won
        info["combat_hp_lost"] = float(hp_lost)
        info["run_decisions"] = self._run_decisions
        info["combat_stuck_count"] = self._combat_stuck
        info["player_select_fallbacks"] = self._player_select_fallbacks
        return info
