"""Generate BC bootstrap data from the heuristic agent (spec Phase 6).

Multiprocess whole-run rollout collection following the torch-free worker
pattern from ``scripts/slim_vecenv.py``: each worker imports ONLY the
simulator + numpy (never stable_baselines3/torch), plays complete runs with
:class:`scripts.heuristic_agent.HeuristicNecrobinderAgent`, and writes its
own ``.npz`` shards directly to disk -- there is no per-step IPC at all.

Each shard stores (obs, action, mask, mc_return) tuples:

* ``obs`` in a minimal CSR encoding (``obs_data``/``obs_indices``/
  ``obs_indptr`` + ``obs_dim``) -- the rich obs is ~95% zeros, so CSR cuts
  ~19 KB/sample to ~1-2 KB;
* ``actions`` int16, ``masks`` packed bits (``np.packbits`` over 157 bools),
  ``returns`` float32 -- the discounted (gamma=0.997) Monte-Carlo return of
  the TRAINING reward (PBRS shaping_scale=1.0 + terminals), which is exactly
  what the PPO value head must predict.

Usage:

    python scripts/gen_bc_data.py --samples 2000000 --workers 8 \
        --out output/bc_data --ascension 0 --max-act-count 2
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np

GAMMA = 0.997
SHARD_SIZE = 100_000
MASK_BYTES = 20  # ceil(157 / 8)


def _rollout_worker(args: tuple) -> dict:
    """Play whole runs and write npz shards. Torch-free by construction."""
    (worker_id, n_samples, out_dir, seed_base, ascension, max_act_count,
     shard_size) = args
    sys.path.insert(0, str(Path(__file__).resolve().parent))
    from heuristic_agent import HeuristicNecrobinderAgent

    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        max_act_count=max_act_count,
        reward_config=RewardConfig(shaping_scale=1.0),  # training reward
    )
    agent = HeuristicNecrobinderAgent()
    out = Path(out_dir)

    # Shard buffers (CSR pieces built incrementally).
    obs_data: list[np.ndarray] = []
    obs_indices: list[np.ndarray] = []
    obs_lens: list[int] = []
    actions: list[int] = []
    masks: list[np.ndarray] = []
    returns: list[np.ndarray] = []

    collected = 0
    shard_idx = 0
    episodes = 0
    wins = 0
    seed = seed_base

    def flush_shard() -> None:
        nonlocal shard_idx, obs_data, obs_indices, obs_lens, actions, masks, returns
        if not actions:
            return
        indptr = np.zeros(len(obs_lens) + 1, dtype=np.int64)
        indptr[1:] = np.cumsum(np.asarray(obs_lens, dtype=np.int64))
        path = out / f"shard_w{worker_id:02d}_{shard_idx:04d}.npz"
        np.savez_compressed(
            path,
            obs_data=np.concatenate(obs_data) if obs_data else np.zeros(0, np.float32),
            obs_indices=np.concatenate(obs_indices) if obs_indices else np.zeros(0, np.int32),
            obs_indptr=indptr,
            obs_dim=np.int64(env.observation_space.shape[0]),
            actions=np.asarray(actions, dtype=np.int16),
            masks=np.stack(masks) if masks else np.zeros((0, MASK_BYTES), np.uint8),
            returns=np.concatenate(returns) if returns else np.zeros(0, np.float32),
        )
        shard_idx += 1
        obs_data, obs_indices, obs_lens = [], [], []
        actions, masks, returns = [], [], []

    while collected < n_samples:
        obs, info = env.reset(seed=seed)
        seed += 1
        episodes += 1
        ep_rewards: list[float] = []
        ep_n = 0
        done = False
        steps = 0
        while not done and steps < 3000:
            mask = np.asarray(env.action_masks(), dtype=bool)
            action = agent.act(env)
            nz = np.flatnonzero(obs).astype(np.int32)
            obs_indices.append(nz)
            obs_data.append(obs[nz].astype(np.float32))
            obs_lens.append(len(nz))
            actions.append(int(action))
            masks.append(np.packbits(mask))
            obs, reward, terminated, truncated, info = env.step(int(action))
            ep_rewards.append(float(reward))
            ep_n += 1
            done = terminated or truncated
            steps += 1
        # Discounted MC return per step (bootstrap 0 at truncation too:
        # with truncation scored -1 like death this is the honest target).
        g = 0.0
        rets = np.zeros(ep_n, dtype=np.float32)
        for t in range(ep_n - 1, -1, -1):
            g = ep_rewards[t] + GAMMA * g
            rets[t] = g
        returns.append(rets)
        collected += ep_n
        wins += bool(info.get("won", False))
        if len(actions) >= shard_size:
            flush_shard()
    flush_shard()
    return {
        "worker": worker_id,
        "samples": collected,
        "episodes": episodes,
        "wins": wins,
        "shards": shard_idx,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate BC data with the heuristic")
    parser.add_argument("--samples", type=int, default=2_000_000)
    parser.add_argument("--workers", type=int, default=8)
    parser.add_argument("--out", type=str, default="output/bc_data")
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--max-act-count", type=int, default=2)
    parser.add_argument("--seed-base", type=int, default=30_000_000)
    parser.add_argument("--shard-size", type=int, default=SHARD_SIZE)
    args = parser.parse_args()

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)

    per = [args.samples // args.workers] * args.workers
    for i in range(args.samples % args.workers):
        per[i] += 1
    jobs = [
        (i, per[i], str(out), args.seed_base + i * 1_000_000,
         args.ascension, args.max_act_count, args.shard_size)
        for i in range(args.workers)
    ]

    start = time.perf_counter()
    if args.workers <= 1:
        results = [_rollout_worker(jobs[0])]
    else:
        import multiprocessing as mp

        ctx = mp.get_context("spawn")
        with ctx.Pool(args.workers) as pool:
            results = pool.map(_rollout_worker, jobs)
    wall = time.perf_counter() - start

    total = sum(r["samples"] for r in results)
    episodes = sum(r["episodes"] for r in results)
    wins = sum(r["wins"] for r in results)
    summary = {
        "samples": total,
        "episodes": episodes,
        "win_rate": wins / max(1, episodes),
        "ascension": args.ascension,
        "max_act_count": args.max_act_count,
        "gamma": GAMMA,
        "workers": args.workers,
        "wall_s": round(wall, 1),
        "steps_per_s": round(total / wall, 1),
    }
    (out / "summary.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    print(json.dumps(summary, indent=2))


if __name__ == "__main__":
    main()
