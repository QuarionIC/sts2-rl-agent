"""Expert Iteration distillation (TRAINING_REVAMP_SPEC Phase 8, part b).

Given MCTS-labeled combat decisions ``(obs, legal mask, visit distribution
over the 115 combat actions, root value)``, distill them into the current
policy:

    L = CE(pi_theta(.|s, mask) -> visit distribution)
      + value_coef * MSE(V_theta(s) -> root_value)

The CE is over the policy's FULL (157) masked action distribution; the
visit targets live entirely inside the combat slice and sum to 1, so any
probability the policy puts on legal non-combat actions is pushed down by
normalization exactly as intended.

Pure functions here (loss math, epoch loop) so tests can verify the
arithmetic on synthetic batches; scripts/exit_distill.py is the CLI.
"""

from __future__ import annotations

from dataclasses import dataclass

import numpy as np

from sts2_env.search.combat_mcts import COMBAT_ACTIONS

#: Weight of the value-MSE term (spec: "0.5*MSE(value -> root value)").
DEFAULT_VALUE_COEF = 0.5
DEFAULT_LR = 1.0e-4
DEFAULT_EPOCHS = 3
DEFAULT_BATCH_SIZE = 1024


def distill_losses(log_probs, values, target_probs, target_values):
    """(policy CE, value MSE) for one minibatch. All torch tensors:

    ``log_probs``     (B, A) masked log-probs (illegal = -inf is fine),
    ``values``        (B,)   value head outputs,
    ``target_probs``  (B, A) visit distributions (rows sum to 1, zero on
                             illegal/non-combat actions),
    ``target_values`` (B,)   MCTS root values.

    CE = -sum_a target(a) * log pi(a); the ``target > 0`` guard keeps
    0 * (-inf) from poisoning rows (targets are zero exactly where the
    log-prob may be -inf).
    """
    import torch as th

    safe_logp = th.where(
        target_probs > 0, log_probs, th.zeros((), dtype=log_probs.dtype, device=log_probs.device)
    )
    ce = -(target_probs * safe_logp).sum(dim=-1)
    policy_loss = ce.mean()
    value_loss = ((values - target_values) ** 2).mean()
    return policy_loss, value_loss


@dataclass
class DistillStats:
    epochs: int
    batches: int
    policy_loss_first: float
    policy_loss_last: float
    value_loss_first: float
    value_loss_last: float
    top1_agree_before: float
    top1_agree_after: float


def _masked_log_probs(policy, obs_t, mask_t):
    """(B, A) masked log-probs from a MaskableActorCriticPolicy, with grads."""
    dist = policy.get_distribution(obs_t, action_masks=mask_t)
    return dist.distribution.logits  # torch Categorical: normalized log-probs


def _top1_agreement(policy, obs, masks, targets, device, batch_size) -> float:
    import torch as th

    agree = 0
    with th.no_grad():
        for lo in range(0, len(obs), batch_size):
            hi = min(lo + batch_size, len(obs))
            obs_t = th.as_tensor(obs[lo:hi], device=device)
            mask_t = th.as_tensor(masks[lo:hi], device=device)
            logp = _masked_log_probs(policy, obs_t, mask_t)
            pred = logp.argmax(dim=-1).cpu().numpy()
            tgt = targets[lo:hi].argmax(axis=-1)
            agree += int((pred == tgt).sum())
    return agree / max(len(obs), 1)


def distill(
    model,
    obs: np.ndarray,
    masks: np.ndarray,
    visit_probs: np.ndarray,
    root_values: np.ndarray,
    epochs: int = DEFAULT_EPOCHS,
    lr: float = DEFAULT_LR,
    batch_size: int = DEFAULT_BATCH_SIZE,
    value_coef: float = DEFAULT_VALUE_COEF,
    seed: int = 0,
    verbose: bool = True,
) -> DistillStats:
    """Distill MCTS targets into ``model.policy`` in place.

    ``obs`` (N, obs_dim) float32; ``masks`` (N, 157) bool -- the FULL legal
    masks recorded at collection time; ``visit_probs`` (N, 115);
    ``root_values`` (N,). Uses a FRESH Adam (the PPO optimizer state is
    neither meaningful for this objective nor harmed -- SB3 rebuilds
    schedules on resume, and the relaunch path loads tensors only).
    """
    import torch as th

    policy = model.policy
    device = policy.device
    n = len(obs)
    assert masks.shape[0] == n and visit_probs.shape[0] == n and root_values.shape[0] == n
    action_dim = int(policy.action_space.n)

    # Targets over the full action space (combat slice filled, rest zero).
    full_targets = np.zeros((n, action_dim), dtype=np.float32)
    full_targets[:, :COMBAT_ACTIONS] = visit_probs

    top1_before = _top1_agreement(policy, obs, masks, full_targets, device, batch_size)

    optimizer = th.optim.Adam(policy.parameters(), lr=lr)
    rng = np.random.default_rng(seed)
    policy.set_training_mode(True)

    first_pl = first_vl = last_pl = last_vl = float("nan")
    batches = 0
    for epoch in range(epochs):
        order = rng.permutation(n)
        for lo in range(0, n, batch_size):
            idx = order[lo: lo + batch_size]
            obs_t = th.as_tensor(obs[idx], device=device)
            mask_t = th.as_tensor(masks[idx], device=device)
            tgt_p = th.as_tensor(full_targets[idx], device=device)
            tgt_v = th.as_tensor(root_values[idx], device=device)

            logp = _masked_log_probs(policy, obs_t, mask_t)
            values = policy.predict_values(obs_t).flatten()
            policy_loss, value_loss = distill_losses(logp, values, tgt_p, tgt_v)
            loss = policy_loss + value_coef * value_loss

            optimizer.zero_grad()
            loss.backward()
            th.nn.utils.clip_grad_norm_(policy.parameters(), 0.5)
            optimizer.step()

            last_pl, last_vl = float(policy_loss.item()), float(value_loss.item())
            if batches == 0:
                first_pl, first_vl = last_pl, last_vl
            batches += 1
        if verbose:
            print(
                f"[distill] epoch {epoch + 1}/{epochs}: "
                f"policy_ce={last_pl:.4f} value_mse={last_vl:.4f}",
                flush=True,
            )

    policy.set_training_mode(False)
    top1_after = _top1_agreement(policy, obs, masks, full_targets, device, batch_size)
    stats = DistillStats(
        epochs=epochs,
        batches=batches,
        policy_loss_first=first_pl,
        policy_loss_last=last_pl,
        value_loss_first=first_vl,
        value_loss_last=last_vl,
        top1_agree_before=top1_before,
        top1_agree_after=top1_after,
    )
    if verbose:
        print(
            f"[distill] done: CE {first_pl:.4f} -> {last_pl:.4f}, "
            f"value MSE {first_vl:.4f} -> {last_vl:.4f}, "
            f"top-1 agreement {top1_before:.1%} -> {top1_after:.1%}",
            flush=True,
        )
    return stats
