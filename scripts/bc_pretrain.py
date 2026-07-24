"""BC pretrain: masked cross-entropy + value regression -> bc_init.zip.

Trains the Phase-3 per-slot policy architecture (rich extractor + [1024,
1024, 512] torso) on (obs, action, mask, mc_return) shards produced by
``scripts/gen_bc_data.py``:

* masked CE: illegal actions' logits are floored to -1e8 before the
  cross-entropy to the heuristic action (matches MaskablePPO's masking);
* value regression: MSE of the value head to the discounted (gamma=0.997)
  Monte-Carlo return of the training reward (PBRS + terminals);
* 3 epochs, Adam 3e-4, batch 4096 (spec Phase 6 hyperparameters).

The result is saved as a full MaskablePPO checkpoint
(``output/bc_init/bc_init.zip``) loadable by ``MaskablePPO.load`` or via
``model.set_parameters(..., exact_match=False)``; the round-trip is
verified after saving. Finishes with a deterministic post-BC eval
(default 100 episodes, A0 acts 1-2, shaping off).

Usage:

    python scripts/bc_pretrain.py --data output/bc_data --out output/bc_init \
        --epochs 3 --eval-episodes 100
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import glob
import json
import time
from pathlib import Path

import numpy as np

N_ACTIONS = 157
MASK_BITS = 157


class ShardDataset:
    """All shards concatenated in RAM (CSR obs, packed masks)."""

    def __init__(self, data_dir: str):
        files = sorted(glob.glob(str(Path(data_dir) / "shard_*.npz")))
        if not files:
            raise FileNotFoundError(f"no shards in {data_dir}")
        data, indices, indptrs = [], [], [np.zeros(1, dtype=np.int64)]
        actions, masks, returns = [], [], []
        offset = 0
        self.obs_dim = None
        for f in files:
            z = np.load(f)
            self.obs_dim = int(z["obs_dim"])
            data.append(z["obs_data"])
            indices.append(z["obs_indices"])
            indptrs.append(z["obs_indptr"][1:] + offset)
            offset += len(z["obs_data"])
            actions.append(z["actions"])
            masks.append(z["masks"])
            returns.append(z["returns"])
        self.data = np.concatenate(data)
        self.indices = np.concatenate(indices)
        self.indptr = np.concatenate(indptrs)
        self.actions = np.concatenate(actions).astype(np.int64)
        self.masks = np.concatenate(masks)  # (N, 20) packed bits
        self.returns = np.concatenate(returns).astype(np.float32)
        self.n = len(self.actions)

    def batch(self, rows: np.ndarray):
        obs = np.zeros((len(rows), self.obs_dim), dtype=np.float32)
        for k, r in enumerate(rows):
            s, e = self.indptr[r], self.indptr[r + 1]
            obs[k, self.indices[s:e]] = self.data[s:e]
        masks = np.unpackbits(self.masks[rows], axis=1)[:, :MASK_BITS].astype(bool)
        return obs, self.actions[rows], masks, self.returns[rows]


def build_model(device: str = "cuda"):
    """Fresh MaskablePPO with the Phase-3 architecture on the run env."""
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.train.policy import rich_policy_kwargs

    env = RichSTS2RunEnv(
        character_id="Necrobinder", ascension_level=0, max_act_count=2,
        reward_config=RewardConfig(shaping_scale=1.0),
    )
    return MaskablePPO(
        "MlpPolicy", env,
        learning_rate=2.0e-4, n_steps=64, batch_size=64, n_epochs=1,
        gamma=0.997, policy_kwargs=rich_policy_kwargs(),
        device=device, verbose=0,
    )


def train(model, ds: ShardDataset, epochs: int, batch_size: int, lr: float,
          vf_coef: float = 0.5) -> dict:
    import torch
    import torch.nn.functional as F

    policy = model.policy
    device = policy.device
    optimizer = torch.optim.Adam(policy.parameters(), lr=lr)
    rng = np.random.default_rng(0)
    stats = {}
    for epoch in range(epochs):
        perm = rng.permutation(ds.n)
        ce_sum = v_sum = acc_sum = 0.0
        n_batches = 0
        t0 = time.perf_counter()
        for start in range(0, ds.n - batch_size + 1, batch_size):
            rows = perm[start:start + batch_size]
            obs_np, act_np, mask_np, ret_np = ds.batch(rows)
            obs = torch.as_tensor(obs_np, device=device)
            acts = torch.as_tensor(act_np, device=device)
            masks = torch.as_tensor(mask_np, device=device)
            rets = torch.as_tensor(ret_np, device=device)

            features = policy.extract_features(obs)
            latent_pi, latent_vf = policy.mlp_extractor(features)
            logits = policy.action_net(latent_pi)
            masked_logits = torch.where(
                masks, logits, torch.tensor(-1e8, device=device))
            ce = F.cross_entropy(masked_logits, acts)
            values = policy.value_net(latent_vf).squeeze(-1)
            v_loss = F.mse_loss(values, rets)
            loss = ce + vf_coef * v_loss

            optimizer.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(policy.parameters(), 0.5)
            optimizer.step()

            with torch.no_grad():
                acc = (masked_logits.argmax(dim=1) == acts).float().mean()
            ce_sum += float(ce.detach())
            v_sum += float(v_loss.detach())
            acc_sum += float(acc)
            n_batches += 1
        stats = {
            "epoch": epoch + 1,
            "ce": round(ce_sum / n_batches, 4),
            "value_mse": round(v_sum / n_batches, 4),
            "action_acc": round(acc_sum / n_batches, 4),
            "wall_s": round(time.perf_counter() - t0, 1),
        }
        print(f"[bc] {stats}", flush=True)
    return stats


def verify_roundtrip(out_path: Path, model) -> None:
    """bc_init.zip must load via MaskablePPO.load AND set_parameters."""
    import torch
    from sb3_contrib import MaskablePPO

    loaded = MaskablePPO.load(str(out_path), device=model.device)
    p0 = dict(model.policy.state_dict())
    p1 = dict(loaded.policy.state_dict())
    assert set(p0) == set(p1), "state dict key mismatch after load"
    for k in p0:
        assert torch.equal(p0[k].cpu(), p1[k].cpu()), f"tensor mismatch: {k}"
    del loaded

    fresh = build_model(device=str(model.device))
    fresh.set_parameters(str(out_path), exact_match=False, device=fresh.device)
    p2 = dict(fresh.policy.state_dict())
    for k in p0:
        assert torch.equal(p0[k].cpu(), p2[k].cpu()), f"set_parameters mismatch: {k}"
    del fresh
    print("[bc] round-trip verified: MaskablePPO.load + set_parameters OK", flush=True)


def eval_model(model, n_episodes: int = 100, ascension: int = 0,
               max_act_count: int = 2, seed_block: int = 40_000_000) -> dict:
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    env = RichSTS2RunEnv(
        character_id="Necrobinder", ascension_level=ascension,
        max_act_count=max_act_count,
        reward_config=RewardConfig(shaping_scale=0.0),
    )
    wins = 0
    floors = []
    for ep in range(n_episodes):
        obs, info = env.reset(seed=seed_block + ep)
        done = False
        steps = 0
        while not done and steps < 3000:
            masks = env.action_masks()
            action, _ = model.predict(obs, action_masks=masks, deterministic=True)
            obs, reward, terminated, truncated, info = env.step(int(action))
            done = terminated or truncated
            steps += 1
        wins += bool(info.get("won", False))
        floors.append(int(info.get("floor", 0)))
    return {
        "episodes": n_episodes,
        "win_rate": wins / n_episodes,
        "mean_floors": float(np.mean(floors)),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="BC pretrain -> bc_init.zip")
    parser.add_argument("--data", type=str, default="output/bc_data")
    parser.add_argument("--out", type=str, default="output/bc_init")
    parser.add_argument("--epochs", type=int, default=3)
    parser.add_argument("--batch-size", type=int, default=4096)
    parser.add_argument("--lr", type=float, default=3e-4)
    parser.add_argument("--eval-episodes", type=int, default=100)
    parser.add_argument("--device", type=str, default="cuda")
    args = parser.parse_args()

    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    print(f"[bc] loading shards from {args.data} ...", flush=True)
    ds = ShardDataset(args.data)
    print(f"[bc] dataset: {ds.n:,} samples, obs_dim={ds.obs_dim}", flush=True)

    model = build_model(device=args.device)
    train_stats = train(model, ds, args.epochs, args.batch_size, args.lr)

    out_path = out_dir / "bc_init"
    model.save(str(out_path))
    print(f"[bc] saved {out_path}.zip", flush=True)
    verify_roundtrip(out_path, model)

    print(f"[bc] post-BC deterministic eval ({args.eval_episodes} episodes, "
          f"A0 acts 1-2, shaping off) ...", flush=True)
    metrics = eval_model(model, args.eval_episodes)
    print(f"[bc] post-BC eval: {metrics}", flush=True)
    (out_dir / "bc_report.json").write_text(
        json.dumps({"train": train_stats, "eval": metrics,
                    "dataset_samples": ds.n}, indent=2),
        encoding="utf-8")


if __name__ == "__main__":
    main()
