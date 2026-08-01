#!/usr/bin/env python3
"""Behaviour-clone the beam planner into the combat policy.

Takes the shards written by ``scripts.collect_planner_combats`` and fits the
policy head with masked cross-entropy against the planner's chosen action.

What this is and is not
-----------------------
This is the CHEAP half of expert iteration: it copies what the planner did,
from states the planner visited. It cannot, on its own, teach the policy to
recover from states the planner never reached -- that is what a DAgger loop
would add, at ~30s of search per visited state instead of one search per
combat.

The value head is left ALONE. These demonstrations carry no return labels, so
any value target invented here would be fiction; PPO fine-tuning re-fits it
against the real reward. Distilling the policy while corrupting the critic
would look like an improvement in action agreement and a regression in play.

Masked cross-entropy, not plain: the action space is 115 wide and most of it
is illegal at any given moment. Training against unmasked logits spends
capacity teaching the policy not to pick actions the mask already forbids, and
dilutes the gradient on the choice that actually mattered.

Success is NOT loss going down. It is damage-per-win falling toward the
planner's 10.2 while the win rate holds -- measure it with
``scripts.compare_combat_agents``, which is paired and roughly an order of
magnitude more sensitive than an unpaired comparison.

Usage
-----
    python -m scripts.distill_planner_bc --checkpoint COMBAT.zip \
        --data output/planner_bc --out output/planner_bc/distilled.zip
"""

from __future__ import annotations

import argparse
import os
import sys
import time
from pathlib import Path

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))


def load_shards(data_dir: Path) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    obs, masks, actions = [], [], []
    n_actions = None
    for shard in sorted(data_dir.glob("planner_bc_*.npz")):
        with np.load(shard) as z:
            if z["actions"].size == 0:
                continue
            obs.append(z["obs"])
            n_actions = int(z["n_actions"])
            masks.append(np.unpackbits(z["masks"], axis=1)[:, :n_actions])
            actions.append(z["actions"])
    if not obs:
        raise SystemExit(f"no usable shards in {data_dir}")
    return (np.concatenate(obs), np.concatenate(masks).astype(bool),
            np.concatenate(actions).astype(np.int64))


def main(argv=None) -> int:
    import torch
    from sb3_contrib import MaskablePPO

    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--checkpoint", required=True,
                    help="combat policy to fine-tune (115 actions)")
    ap.add_argument("--data", default="output/planner_bc", type=Path)
    ap.add_argument("--out", default="output/planner_bc/distilled.zip", type=Path)
    ap.add_argument("--epochs", type=int, default=4)
    ap.add_argument("--batch-size", type=int, default=512)
    ap.add_argument("--lr", type=float, default=1e-4)
    ap.add_argument("--val-frac", type=float, default=0.1)
    ap.add_argument("--device", default="cuda")
    args = ap.parse_args(argv)

    obs, masks, actions = load_shards(args.data)
    print(f"loaded {len(actions)} labels, obs {obs.shape[1]}, "
          f"actions {masks.shape[1]}")

    model = MaskablePPO.load(args.checkpoint, device=args.device)
    policy = model.policy
    device = policy.device

    exp_obs = int(model.observation_space.shape[0])
    if exp_obs != obs.shape[1]:
        raise SystemExit(
            f"checkpoint expects obs {exp_obs} but the data is {obs.shape[1]}. "
            f"Migrate one to match (scripts/migrate_checkpoint_powers.py) "
            f"rather than training across a layout mismatch.")

    # A held-out split, because BC training loss falls happily while the policy
    # memorises the planner's states. Validation ACCURACY on unseen states is
    # the honest read on whether anything transferable was learned.
    n = len(actions)
    rng = np.random.default_rng(0)
    order = rng.permutation(n)
    n_val = max(1, int(n * args.val_frac))
    val_idx, train_idx = order[:n_val], order[n_val:]

    t_obs = torch.as_tensor(obs, device=device)
    t_mask = torch.as_tensor(masks, device=device)
    t_act = torch.as_tensor(actions, device=device)

    # Policy parameters only -- the critic keeps its PPO-fitted weights, since
    # these demonstrations carry no returns to fit it against.
    params = [p for name, p in policy.named_parameters()
              if not name.startswith("value_net")
              and not name.startswith("mlp_extractor.value_net")]
    opt = torch.optim.Adam(params, lr=args.lr)

    def evaluate(idx) -> tuple[float, float]:
        policy.set_training_mode(False)
        total, correct, loss_sum = 0, 0, 0.0
        with torch.no_grad():
            for start in range(0, len(idx), 4096):
                b = torch.as_tensor(idx[start:start + 4096], device=device)
                logits = _masked_logits(policy, t_obs[b], t_mask[b])
                loss_sum += torch.nn.functional.cross_entropy(
                    logits, t_act[b], reduction="sum").item()
                correct += (logits.argmax(dim=1) == t_act[b]).sum().item()
                total += len(b)
        return loss_sum / max(total, 1), correct / max(total, 1)

    v_loss, v_acc = evaluate(val_idx)
    print(f"before: val loss {v_loss:.4f}  val agreement {v_acc:.1%}")

    t0 = time.time()
    for epoch in range(args.epochs):
        policy.set_training_mode(True)
        perm = rng.permutation(len(train_idx))
        run_loss, seen = 0.0, 0
        for start in range(0, len(perm), args.batch_size):
            b = torch.as_tensor(train_idx[perm[start:start + args.batch_size]],
                                device=device)
            logits = _masked_logits(policy, t_obs[b], t_mask[b])
            loss = torch.nn.functional.cross_entropy(logits, t_act[b])
            opt.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(params, 0.5)
            opt.step()
            run_loss += loss.item() * len(b)
            seen += len(b)
        v_loss, v_acc = evaluate(val_idx)
        print(f"epoch {epoch + 1}/{args.epochs}: train {run_loss / max(seen,1):.4f}  "
              f"val {v_loss:.4f}  val agreement {v_acc:.1%}")

    args.out.parent.mkdir(parents=True, exist_ok=True)
    model.save(args.out)
    print(f"\nsaved {args.out}  ({time.time() - t0:.0f}s)")
    print("Agreement is NOT the result. Measure damage-per-win against the "
          "baseline with scripts.compare_combat_agents -- paired.")
    return 0


def _masked_logits(policy, obs, mask):
    """Logits with illegal actions driven to -inf, as MaskablePPO does."""
    import torch

    features = policy.extract_features(obs)
    if policy.share_features_extractor:
        latent_pi, _ = policy.mlp_extractor(features)
    else:
        latent_pi, _ = policy.mlp_extractor(features[0])
    logits = policy.action_net(latent_pi)
    return logits.masked_fill(~mask, -torch.inf)


if __name__ == "__main__":
    sys.exit(main())
