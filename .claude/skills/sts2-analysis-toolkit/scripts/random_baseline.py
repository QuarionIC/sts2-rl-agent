"""Random masked-action baseline for the rich envs, with a Wilson 95% CI.

Every trained-policy win rate needs this denominator: a policy that is not
CI-separated from random play on the same env config has learned nothing.
Uses the same uniform-over-valid-actions pattern as
scripts/benchmark_rich_env.py, and the trainer's eval discipline
(shaping_scale=0, dedicated seed block).

Usage (from repo root, venv python; sts2_env must be importable):
    python random_baseline.py --env run --acts 1 --ascension 10 --episodes 200
    python random_baseline.py --env combat --episodes 200

Notes:
  * --env run == RichSTS2RunEnv(character_id="Necrobinder"), the campaign env.
  * --env combat == RichSTS2CombatEnv (Act-1 starter pool by default) -- the
    retired stage-A task, kept for comparisons against old eval history.
  * Do NOT run this while a training run is live: CPU contention biases
    nothing statistically, but it is slow and steals throughput from training.
"""

from __future__ import annotations

import argparse
import math
import time

import numpy as np

Z95 = 1.959963984540054


def wilson_ci(wins: int, n: int, z: float = Z95) -> tuple[float, float]:
    if n <= 0:
        return (0.0, 1.0)
    p = wins / n
    denom = 1.0 + z * z / n
    center = p + z * z / (2 * n)
    margin = z * math.sqrt(p * (1.0 - p) / n + z * z / (4.0 * n * n))
    return ((center - margin) / denom, (center + margin) / denom)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--env", choices=["run", "combat"], default="run")
    ap.add_argument("--episodes", type=int, default=200)
    ap.add_argument("--ascension", type=int, default=10)
    ap.add_argument("--acts", type=int, default=1,
                    help="max_act_count for the run env (1-4)")
    ap.add_argument("--seed-block", type=int, default=20_000_000,
                    help="reset seeds are seed_block+episode (default 20M, "
                         "disjoint from the trainer's eval block 10M)")
    ap.add_argument("--max-episode-steps", type=int, default=3_000)
    args = ap.parse_args()

    if args.env == "run":
        from sts2_env.gym_env.reward_config import RewardConfig
        from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
        env = RichSTS2RunEnv(
            character_id="Necrobinder",
            ascension_level=args.ascension,
            max_act_count=args.acts,
            reward_config=RewardConfig(shaping_scale=0.0),
        )
        desc = f"RichSTS2RunEnv Necrobinder A{args.ascension} acts<={args.acts}"
    else:
        from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv
        env = RichSTS2CombatEnv(character_id="Necrobinder",
                                ascension_level=args.ascension)
        desc = f"RichSTS2CombatEnv Necrobinder A{args.ascension} (default pool)"

    rng = np.random.default_rng(0)
    wins = 0
    truncs = 0
    sim_errors = 0
    floors: list[int] = []
    acts: list[int] = []
    total_steps = 0
    start = time.perf_counter()
    for ep in range(args.episodes):
        obs, info = env.reset(seed=args.seed_block + ep)
        done = False
        steps = 0
        while not done and steps < args.max_episode_steps:
            mask = env.action_masks()
            valid = np.flatnonzero(mask)
            action = int(valid[rng.integers(0, len(valid))]) if len(valid) else 0
            obs, reward, terminated, truncated, info = env.step(action)
            done = terminated or truncated
            steps += 1
        total_steps += steps
        wins += bool(info.get("won", reward > 0))
        truncs += bool(info.get("truncated", False))
        sim_errors += bool(info.get("sim_error", False))
        floors.append(int(info.get("floor", 0)))
        acts.append(int(info.get("act", 0)))
    elapsed = time.perf_counter() - start

    n = args.episodes
    lo, hi = wilson_ci(wins, n)
    print(f"env            : {desc}")
    print(f"episodes       : {n}  (seeds {args.seed_block}..{args.seed_block + n - 1})")
    print(f"random win rate: {wins / n:.1%}  Wilson 95% CI [{lo:.1%}, {hi:.1%}]")
    print(f"truncated      : {truncs / n:.1%}   sim_error: {sim_errors}")
    print(f"mean floors    : {np.mean(floors):.1f}   mean act: {np.mean(acts):.2f}")
    print(f"throughput     : {total_steps / elapsed:.0f} env-steps/s "
          f"({elapsed:.1f}s total)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
