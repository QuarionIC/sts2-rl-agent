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
"""

from __future__ import annotations

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
