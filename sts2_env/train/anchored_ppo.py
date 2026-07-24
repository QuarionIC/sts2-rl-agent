"""MaskablePPO with an auxiliary KL anchor to a frozen BC reference policy.

Problem (attempts 7/8 of the G1 campaign): a BC-initialized MaskablePPO
steadily erodes its behavior-cloning prior -- eval floors decayed
9.76 -> 8.13 (lr 2e-4) and 10.43 -> 9.40 (lr 5e-5 + target_kl 0.015) --
because nothing in the PPO objective prefers staying near the prior once
the rollout advantages point elsewhere. Conservative optimization only
slows the walk away from the prior; it does not stop it.

Standard fix implemented here: add an auxiliary anchor term

    anchor_coef * KL(pi_theta(.|s) || pi_BC(.|s))

to the PPO loss, where pi_BC is a FROZEN copy of the BC policy (no grads,
eval mode) and the KL is between the *masked* action distributions,
averaged over the minibatch states. The coefficient decays linearly on
absolute ``num_timesteps`` (resume-safe) from ``anchor_coef`` to
``anchor_coef_final`` over ``anchor_decay_steps``, so early training is
strongly tethered to the prior and late training is free to improve on it.

``train()`` below is a copy of ``sb3_contrib.ppo_mask.MaskablePPO.train``
(sb3-contrib 2.9.0) with the additions delimited by ``# --- BC anchor``
comments; everything else (clipping, value loss, entropy, target_kl early
stop, logging) is byte-identical so behavior without an anchor matches the
base class exactly (and with ``anchor_policy is None`` we simply defer to
``super().train()``).

Self-Imitation Learning (attempt 10)
------------------------------------
:class:`SILAnchoredMaskablePPO` additionally implements SIL (Oh et al. 2018,
"Self-Imitation Learning"): a ring buffer stores the agent's own CLOSED
episodes as ``(obs, action, mask, R)`` transitions, where ``R`` is the
episodic discounted return computed backwards from the episode's end.
After each PPO ``train()``, ``sil_updates`` minibatch updates minimize

    L_sil = -mean( (R - V(s))_+ . log pi(a|s) )          (policy term,
                                                          advantage detached)
          + 0.01 * mean( (R - V(s))_+ ^ 2 )              (value term,
                                                          grads through V)

scaled by ``sil_coef``. The ``(R - V(s))_+ = max(R - V(s), 0)`` clamp IS the
SIL filter: only transitions whose realized return beat the current value
estimate (the rare boss-killing episodes) contribute gradient, so all closed
episodes can be stored ring-buffer style with no storage-time filtering.
Episodes are ingested from the rollout buffer after every
``collect_rollouts`` (per-env pending lists carry partial episodes across
rollout boundaries; episode boundaries come from ``episode_starts``).
"""

from __future__ import annotations

from collections import deque

import numpy as np
import torch as th
from gymnasium import spaces
from stable_baselines3.common.utils import explained_variance
from torch.nn import functional as F

from sb3_contrib import MaskablePPO
from sb3_contrib.common.maskable.distributions import MaskableCategorical

DEFAULT_ANCHOR_COEF = 0.5
DEFAULT_ANCHOR_COEF_FINAL = 0.02
DEFAULT_ANCHOR_DECAY_STEPS = 10_000_000

DEFAULT_SIL_COEF = 0.1
DEFAULT_SIL_UPDATES = 4
DEFAULT_SIL_BATCH_SIZE = 512
DEFAULT_SIL_BUFFER_CAPACITY = 100_000
#: beta from Oh et al. 2018: weight of the SIL value term inside L_sil.
SIL_VALUE_COEF = 0.01


def anchor_coef_at(t: int, initial: float, final: float, decay_steps: int) -> float:
    """Linear decay from ``initial`` (t=0) to ``final`` (t>=decay_steps).

    Uses absolute timesteps, NOT progress_remaining, so the schedule
    survives resume (the old linear_lr bug class).
    """
    if decay_steps <= 0 or t >= decay_steps:
        return final
    frac = t / decay_steps
    return initial + frac * (final - initial)


def masked_kl(p: MaskableCategorical, q: MaskableCategorical) -> th.Tensor:
    """KL(p || q) per batch row for two masked categorical distributions.

    Both distributions must have had the SAME mask applied. torch's
    Categorical normalizes ``.logits`` to log-probs, so
    KL = sum_a p(a) * (log p(a) - log q(a)); masked-out actions are forced
    to contribute exactly 0 (their probs underflow to 0 but the explicit
    ``where`` guards against 0 * large = NaN pathologies).
    """
    t = p.probs * (p.logits - q.logits)
    if p.masks is not None:
        t = th.where(p.masks, t, th.zeros((), dtype=t.dtype, device=t.device))
    return t.sum(-1)


class AnchoredMaskablePPO(MaskablePPO):
    """MaskablePPO + ``anchor_coef * KL(pi_theta || pi_BC)`` in the loss.

    The frozen reference is attached AFTER construction via
    :meth:`set_anchor` (it is deliberately excluded from ``save()`` --
    checkpoints stay plain MaskablePPO-compatible and the anchor must be
    re-attached on resume). Without an anchor this class trains
    identically to MaskablePPO.
    """

    def __init__(
        self,
        policy,
        env,
        anchor_coef: float = DEFAULT_ANCHOR_COEF,
        anchor_coef_final: float = DEFAULT_ANCHOR_COEF_FINAL,
        anchor_decay_steps: int = DEFAULT_ANCHOR_DECAY_STEPS,
        **kwargs,
    ):
        super().__init__(policy, env, **kwargs)
        self.anchor_coef = float(anchor_coef)
        self.anchor_coef_final = float(anchor_coef_final)
        self.anchor_decay_steps = int(anchor_decay_steps)
        self.anchor_policy = None
        self.anchor_source: str | None = None

    # ------------------------------------------------------------------
    # Anchor management
    # ------------------------------------------------------------------

    def set_anchor(self, path: str) -> None:
        """Load a frozen copy of the BC policy from a MaskablePPO zip.

        The zip's policy state dict must match this model's architecture
        exactly (strict load) -- a partially-loaded anchor would anchor to
        garbage.
        """
        from stable_baselines3.common.save_util import load_from_zip_file

        _, params, _ = load_from_zip_file(path, device=self.device)
        anchor = self.policy_class(
            self.observation_space,
            self.action_space,
            lambda _: 0.0,  # lr schedule; the anchor is never optimized
            **self.policy_kwargs,
        ).to(self.device)
        anchor.load_state_dict(params["policy"], strict=True)
        anchor.set_training_mode(False)
        for p in anchor.parameters():
            p.requires_grad_(False)
        self.anchor_policy = anchor
        self.anchor_source = str(path)
        print(
            f"[anchor] frozen BC reference loaded from {path} "
            f"(KL coef {self.anchor_coef} -> {self.anchor_coef_final} "
            f"over {self.anchor_decay_steps:,} steps)",
            flush=True,
        )

    def current_anchor_coef(self) -> float:
        return anchor_coef_at(
            self.num_timesteps,
            self.anchor_coef,
            self.anchor_coef_final,
            self.anchor_decay_steps,
        )

    def _excluded_save_params(self) -> list[str]:
        # The frozen reference is a runtime attachment, not part of the
        # checkpoint: saves stay MaskablePPO-compatible; re-attach on load.
        return super()._excluded_save_params() + ["anchor_policy"]

    # ------------------------------------------------------------------
    # Training (copy of MaskablePPO.train, sb3-contrib 2.9.0, plus the
    # blocks marked "--- BC anchor")
    # ------------------------------------------------------------------

    def train(self) -> None:
        if self.anchor_policy is None:
            return super().train()

        # Switch to train mode (this affects batch norm / dropout)
        self.policy.set_training_mode(True)
        # Update optimizer learning rate
        self._update_learning_rate(self.policy.optimizer)
        # Compute current clip range
        clip_range = self.clip_range(self._current_progress_remaining)  # type: ignore[operator]
        # Optional: clip range for the value function
        if self.clip_range_vf is not None:
            clip_range_vf = self.clip_range_vf(self._current_progress_remaining)  # type: ignore[operator]

        # --- BC anchor: coefficient for this update (absolute-step decay)
        anchor_coef_now = self.current_anchor_coef()
        anchor_kls = []

        entropy_losses = []
        pg_losses, value_losses = [], []
        clip_fractions = []

        continue_training = True

        # train for n_epochs epochs
        for epoch in range(self.n_epochs):
            approx_kl_divs = []
            # Do a complete pass on the rollout buffer
            for rollout_data in self.rollout_buffer.get(self.batch_size):
                actions = rollout_data.actions
                if isinstance(self.action_space, spaces.Discrete):
                    # Convert discrete action from float to long
                    actions = rollout_data.actions.long().flatten()

                values, log_prob, entropy = self.policy.evaluate_actions(
                    rollout_data.observations,
                    actions,
                    action_masks=rollout_data.action_masks,
                )

                values = values.flatten()
                # Normalize advantage
                advantages = rollout_data.advantages
                if self.normalize_advantage:
                    advantages = (advantages - advantages.mean()) / (advantages.std() + 1e-8)

                # ratio between old and new policy, should be one at the first iteration
                ratio = th.exp(log_prob - rollout_data.old_log_prob)

                # clipped surrogate loss
                policy_loss_1 = advantages * ratio
                policy_loss_2 = advantages * th.clamp(ratio, 1 - clip_range, 1 + clip_range)
                policy_loss = -th.min(policy_loss_1, policy_loss_2).mean()

                # Logging
                pg_losses.append(policy_loss.item())
                clip_fraction = th.mean((th.abs(ratio - 1) > clip_range).float()).item()
                clip_fractions.append(clip_fraction)

                if self.clip_range_vf is None:
                    # No clipping
                    values_pred = values
                else:
                    # Clip the different between old and new value
                    # NOTE: this depends on the reward scaling
                    values_pred = rollout_data.old_values + th.clamp(
                        values - rollout_data.old_values, -clip_range_vf, clip_range_vf
                    )
                # Value loss using the TD(gae_lambda) target
                value_loss = F.mse_loss(rollout_data.returns, values_pred)
                value_losses.append(value_loss.item())

                # Entropy loss favor exploration
                if entropy is None:
                    # Approximate entropy when no analytical form
                    entropy_loss = -th.mean(-log_prob)
                else:
                    entropy_loss = -th.mean(entropy)

                entropy_losses.append(entropy_loss.item())

                loss = policy_loss + self.ent_coef * entropy_loss + self.vf_coef * value_loss

                # --- BC anchor: + anchor_coef * KL(pi_theta || pi_BC) over
                # the minibatch states, masked action distributions.
                cur_dist = self.policy.get_distribution(
                    rollout_data.observations, action_masks=rollout_data.action_masks
                )
                with th.no_grad():
                    ref_dist = self.anchor_policy.get_distribution(
                        rollout_data.observations, action_masks=rollout_data.action_masks
                    )
                anchor_kl = masked_kl(
                    cur_dist.distribution, ref_dist.distribution
                ).mean()
                anchor_kls.append(anchor_kl.item())
                loss = loss + anchor_coef_now * anchor_kl
                # --- end BC anchor

                # Calculate approximate form of reverse KL Divergence for early stopping
                # see issue #417: https://github.com/DLR-RM/stable-baselines3/issues/417
                # and discussion in PR #419: https://github.com/DLR-RM/stable-baselines3/pull/419
                # and Schulman blog: http://joschu.net/blog/kl-approx.html
                with th.no_grad():
                    log_ratio = log_prob - rollout_data.old_log_prob
                    approx_kl_div = th.mean((th.exp(log_ratio) - 1) - log_ratio).cpu().numpy()
                    approx_kl_divs.append(approx_kl_div)

                if self.target_kl is not None and approx_kl_div > 1.5 * self.target_kl:
                    continue_training = False
                    if self.verbose >= 1:
                        print(f"Early stopping at step {epoch} due to reaching max kl: {approx_kl_div:.2f}")
                    break

                # Optimization step
                self.policy.optimizer.zero_grad()
                loss.backward()
                # Clip grad norm
                th.nn.utils.clip_grad_norm_(self.policy.parameters(), self.max_grad_norm)
                self.policy.optimizer.step()

            self._n_updates += 1
            if not continue_training:
                break
        explained_var = explained_variance(self.rollout_buffer.values.flatten(), self.rollout_buffer.returns.flatten())

        # Logs
        self.logger.record("train/entropy_loss", np.mean(entropy_losses))
        self.logger.record("train/policy_gradient_loss", np.mean(pg_losses))
        self.logger.record("train/value_loss", np.mean(value_losses))
        self.logger.record("train/approx_kl", np.mean(approx_kl_divs))
        self.logger.record("train/clip_fraction", np.mean(clip_fractions))
        self.logger.record("train/loss", loss.item())
        self.logger.record("train/explained_variance", explained_var)
        # --- BC anchor logs
        self.logger.record("train/anchor_kl", np.mean(anchor_kls))
        self.logger.record("train/anchor_coef", anchor_coef_now)
        # --- end BC anchor logs
        self.logger.record("train/n_updates", self._n_updates, exclude="tensorboard")
        self.logger.record("train/clip_range", clip_range)
        if self.clip_range_vf is not None:
            self.logger.record("train/clip_range_vf", clip_range_vf)


# ---------------------------------------------------------------------------
# Self-Imitation Learning (Oh et al. 2018)
# ---------------------------------------------------------------------------

def discounted_returns(rewards: np.ndarray, gamma: float) -> np.ndarray:
    """Episodic discounted returns ``R_t = r_t + gamma * R_{t+1}`` computed
    backwards from the episode's last reward (``R_T = r_T``).

    Correct for episodes whose HEAD is missing (e.g. collection started
    mid-episode after a resume): each stored transition's return only sums
    FUTURE rewards. It would be wrong for a missing TAIL, which is why
    episodes are only closed at observed episode boundaries.
    """
    out = np.empty(len(rewards), dtype=np.float32)
    acc = 0.0
    for k in range(len(rewards) - 1, -1, -1):
        acc = float(rewards[k]) + gamma * acc
        out[k] = acc
    return out


def sil_terms(
    values: th.Tensor, log_prob: th.Tensor, returns: th.Tensor
) -> tuple[th.Tensor, th.Tensor, th.Tensor]:
    """(policy_term, value_term, clamped_advantages) of the SIL loss.

    ``adv = (returns - values)_+``. The policy term uses the DETACHED
    advantage as a weight (no value gradient through the weighting); the
    value term keeps gradients through ``values`` so V is pushed up toward
    the realized return on better-than-expected transitions only.
    """
    adv = (returns - values).clamp_min(0.0)
    policy_term = -(adv.detach() * log_prob).mean()
    value_term = (adv ** 2).mean()
    return policy_term, value_term, adv


class SILReplayBuffer:
    """Transition-granular ring buffer of CLOSED episodes for SIL.

    Stores ``(obs float32, action int64, mask bool, R float32)`` per
    transition in flat preallocated arrays (lazily allocated on first add so
    the obs/mask dims come from the data). Eviction is oldest-first at
    transition granularity; ``num_episodes`` counts episodes with at least
    one transition still resident (deque bookkeeping, O(1) amortized).
    """

    def __init__(self, capacity: int = DEFAULT_SIL_BUFFER_CAPACITY):
        if capacity <= 0:
            raise ValueError(f"capacity must be positive, got {capacity}")
        self.capacity = int(capacity)
        self.obs: np.ndarray | None = None
        self.actions: np.ndarray | None = None
        self.masks: np.ndarray | None = None
        self.returns: np.ndarray | None = None
        self.pos = 0
        self.full = False
        self._written = 0                       # transitions ever written
        self._episodes: deque[tuple[int, int]] = deque()  # (end_written, len)

    def __len__(self) -> int:
        return self.capacity if self.full else self.pos

    @property
    def num_episodes(self) -> int:
        """Episodes with >= 1 transition still in the buffer."""
        return len(self._episodes)

    def add_episode(
        self,
        obs: np.ndarray,
        actions: np.ndarray,
        masks: np.ndarray,
        returns: np.ndarray,
    ) -> None:
        n = len(actions)
        if n == 0:
            return
        if n > self.capacity:
            # Keep the episode's TAIL (returns are per-transition, any
            # subset is a valid (s, a, R) sample set).
            obs, actions = obs[-self.capacity:], actions[-self.capacity:]
            masks, returns = masks[-self.capacity:], returns[-self.capacity:]
            n = self.capacity
        if self.obs is None:
            self.obs = np.zeros((self.capacity, obs.shape[1]), dtype=np.float32)
            self.actions = np.zeros(self.capacity, dtype=np.int64)
            self.masks = np.zeros((self.capacity, masks.shape[1]), dtype=bool)
            self.returns = np.zeros(self.capacity, dtype=np.float32)
        idx = (self.pos + np.arange(n)) % self.capacity
        self.obs[idx] = obs
        self.actions[idx] = actions
        self.masks[idx] = masks
        self.returns[idx] = returns
        self.pos = (self.pos + n) % self.capacity
        self._written += n
        if self._written >= self.capacity:
            self.full = True
        self._episodes.append((self._written, n))
        # Drop bookkeeping for episodes fully overwritten by newer data.
        low = self._written - self.capacity
        while self._episodes and self._episodes[0][0] <= low:
            self._episodes.popleft()

    def sample(
        self, batch_size: int, rng: np.random.Generator
    ) -> tuple[np.ndarray, np.ndarray, np.ndarray, np.ndarray]:
        n = len(self)
        if n == 0:
            raise ValueError("cannot sample from an empty SIL buffer")
        idx = rng.integers(0, n, size=batch_size)
        return self.obs[idx], self.actions[idx], self.masks[idx], self.returns[idx]


def new_sil_pending() -> dict[str, list]:
    """Per-env accumulator for the transitions of the (open) current episode."""
    return {"obs": [], "actions": [], "masks": [], "rewards": []}


def _append_segment(pend: dict[str, list], obs, actions, masks, rewards) -> None:
    n = len(rewards)
    pend["obs"].append(np.array(obs, dtype=np.float32))
    # Rollout-buffer actions are (n, 1) float; accept (n,) too (tests).
    pend["actions"].append(
        np.asarray(actions).reshape(n, -1)[:, 0].astype(np.int64)
    )
    pend["masks"].append(np.asarray(masks) > 0.5)
    pend["rewards"].append(np.array(rewards, dtype=np.float32))


def _close_pending(pend: dict[str, list], gamma: float, buffer: SILReplayBuffer) -> None:
    rewards = np.concatenate(pend["rewards"])
    buffer.add_episode(
        np.concatenate(pend["obs"], axis=0),
        np.concatenate(pend["actions"]),
        np.concatenate(pend["masks"], axis=0),
        discounted_returns(rewards, gamma),
    )
    for v in pend.values():
        v.clear()


def sil_ingest_rollout(
    pending_per_env: list[dict[str, list]],
    observations: np.ndarray,   # (n_steps, n_envs, obs_dim)
    actions: np.ndarray,        # (n_steps, n_envs, 1) or (n_steps, n_envs)
    rewards: np.ndarray,        # (n_steps, n_envs)
    episode_starts: np.ndarray, # (n_steps, n_envs); [t]=True <=> ep ended at t-1
    action_masks: np.ndarray,   # (n_steps, n_envs, n_actions)
    gamma: float,
    buffer: SILReplayBuffer,
) -> int:
    """Walk one rollout's arrays, close episodes at ``episode_starts``
    boundaries into ``buffer`` (computing discounted returns at close), and
    carry the still-open episode of each env in ``pending_per_env``.

    ``episode_starts[t, i]`` is True when env ``i`` BEGINS a new episode at
    step ``t`` -- i.e. the previous episode's final transition (terminal
    reward included; SB3 already added the truncation bootstrap to it) was
    at ``t - 1`` (possibly in a previous rollout, hence the pending carry).
    Returns the number of episodes closed.
    """
    n_steps, n_envs = rewards.shape[0], rewards.shape[1]
    closed = 0
    for i in range(n_envs):
        pend = pending_per_env[i]
        bounds = np.flatnonzero(episode_starts[:, i] > 0.5)
        prev = 0
        for b in bounds:
            b = int(b)
            if b > prev:  # tail of the episode that ends at b-1
                _append_segment(
                    pend, observations[prev:b, i], actions[prev:b, i],
                    action_masks[prev:b, i], rewards[prev:b, i],
                )
            if pend["rewards"]:
                _close_pending(pend, gamma, buffer)
                closed += 1
            prev = b
        if n_steps > prev:  # still-open episode: carry to the next rollout
            _append_segment(
                pend, observations[prev:, i], actions[prev:, i],
                action_masks[prev:, i], rewards[prev:, i],
            )
    return closed


class SILAnchoredMaskablePPO(AnchoredMaskablePPO):
    """AnchoredMaskablePPO + Self-Imitation Learning.

    Composes with the BC anchor (both, either, or neither: with no anchor
    attached PPO training defers to plain MaskablePPO). After every PPO
    ``train()``, runs ``sil_updates`` minibatch SIL updates (batch
    ``sil_batch_size``) sampled uniformly from a ``sil_buffer_capacity``-
    transition ring buffer of the agent's own closed episodes; masked
    log-probs throughout. The replay buffer, pending episode fragments, and
    RNG are runtime state, never checkpointed -- saves stay
    MaskablePPO-compatible and the buffer refills after a resume.
    """

    def __init__(
        self,
        policy,
        env,
        sil_coef: float = DEFAULT_SIL_COEF,
        sil_updates: int = DEFAULT_SIL_UPDATES,
        sil_batch_size: int = DEFAULT_SIL_BATCH_SIZE,
        sil_buffer_capacity: int = DEFAULT_SIL_BUFFER_CAPACITY,
        **kwargs,
    ):
        super().__init__(policy, env, **kwargs)
        self.sil_coef = float(sil_coef)
        self.sil_updates = int(sil_updates)
        self.sil_batch_size = int(sil_batch_size)
        self.sil_buffer_capacity = int(sil_buffer_capacity)
        self.sil_buffer = SILReplayBuffer(self.sil_buffer_capacity)
        self._sil_pending: list[dict[str, list]] | None = None
        self._sil_rng = np.random.default_rng(self.seed)

    def _excluded_save_params(self) -> list[str]:
        # Runtime-only state: the ~GB replay buffer and pending fragments
        # must never be pickled into checkpoints.
        return super()._excluded_save_params() + [
            "sil_buffer", "_sil_pending", "_sil_rng",
        ]

    # ------------------------------------------------------------------
    # Collection: ingest closed episodes after every rollout
    # ------------------------------------------------------------------

    def collect_rollouts(  # type: ignore[override]
        self, env, callback, rollout_buffer, n_rollout_steps, use_masking: bool = True,
    ) -> bool:
        ok = super().collect_rollouts(
            env, callback, rollout_buffer, n_rollout_steps, use_masking=use_masking
        )
        if ok:
            # Runs BEFORE train() calls rollout_buffer.get(), i.e. while the
            # buffer arrays are still (n_steps, n_envs, ...)-shaped (get()
            # swaps-and-flattens them in place).
            n_envs = rollout_buffer.n_envs
            if self._sil_pending is None or len(self._sil_pending) != n_envs:
                self._sil_pending = [new_sil_pending() for _ in range(n_envs)]
            sil_ingest_rollout(
                self._sil_pending,
                rollout_buffer.observations,
                rollout_buffer.actions,
                rollout_buffer.rewards,
                rollout_buffer.episode_starts,
                rollout_buffer.action_masks,
                self.gamma,
                self.sil_buffer,
            )
        return ok

    # ------------------------------------------------------------------
    # Training: PPO update, then SIL updates
    # ------------------------------------------------------------------

    def train(self) -> None:
        super().train()
        self._sil_train()

    def _sil_train(self) -> None:
        self.logger.record("train/sil_buffer_eps", self.sil_buffer.num_episodes)
        self.logger.record("train/sil_buffer_size", len(self.sil_buffer))
        if (
            self.sil_updates <= 0
            or self.sil_coef <= 0.0
            or len(self.sil_buffer) < self.sil_batch_size
        ):
            return
        self.policy.set_training_mode(True)
        losses: list[float] = []
        mean_advs: list[float] = []
        for _ in range(self.sil_updates):
            obs_np, act_np, mask_np, ret_np = self.sil_buffer.sample(
                self.sil_batch_size, self._sil_rng
            )
            obs_t = th.as_tensor(obs_np, device=self.device)
            act_t = th.as_tensor(act_np, device=self.device)
            mask_t = th.as_tensor(mask_np, device=self.device)
            ret_t = th.as_tensor(ret_np, device=self.device)
            values, log_prob, _ = self.policy.evaluate_actions(
                obs_t, act_t, action_masks=mask_t
            )
            policy_term, value_term, adv = sil_terms(
                values.flatten(), log_prob, ret_t
            )
            loss = self.sil_coef * (policy_term + SIL_VALUE_COEF * value_term)
            self.policy.optimizer.zero_grad()
            loss.backward()
            th.nn.utils.clip_grad_norm_(self.policy.parameters(), self.max_grad_norm)
            self.policy.optimizer.step()
            losses.append(loss.item())
            mean_advs.append(adv.mean().item())
        self.logger.record("train/sil_loss", float(np.mean(losses)))
        self.logger.record("train/sil_mean_adv", float(np.mean(mean_advs)))
