"""Throughput sanity benchmark for the rich envs (docs/TRAINING_REDESIGN.md).

Measures:
 1. single-env steps/sec: old vs rich, combat env and run env (random
    masked actions);
 2. 16-env SubprocVecEnv steps/sec for the rich combat + run envs;
 3. a short MaskablePPO ``.learn()`` wall-clock on GPU with the rich
    policy (training steps/sec).

Usage:
    python scripts/benchmark_rich_env.py [--seconds 10] [--n-envs 16]
    [--learn-steps 20000] [--skip-learn] [--skip-subproc]
"""

from __future__ import annotations

import argparse
import time
from functools import partial

import numpy as np


def bench_single_env(env, seconds: float, seed: int = 0) -> float:
    """Random masked-action stepping; returns steps/sec."""
    rng = np.random.default_rng(seed)
    obs, info = env.reset(seed=seed)
    steps = 0
    start = time.perf_counter()
    deadline = start + seconds
    while time.perf_counter() < deadline:
        mask = env.action_masks()
        valid = np.flatnonzero(mask)
        action = int(valid[rng.integers(0, len(valid))]) if len(valid) else 0
        obs, reward, terminated, truncated, info = env.step(action)
        steps += 1
        if terminated or truncated:
            obs, info = env.reset()
    return steps / (time.perf_counter() - start)


def make_rich_combat_env():
    from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

    return RichSTS2CombatEnv(character_id="Necrobinder", ascension_level=10)


def make_rich_run_env():
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    return RichSTS2RunEnv(character_id="Necrobinder", ascension_level=10, max_act_count=1)


def bench_subproc(factory, n_envs: int, seconds: float) -> float:
    from stable_baselines3.common.vec_env import SubprocVecEnv

    vec = SubprocVecEnv([factory for _ in range(n_envs)])
    try:
        rng = np.random.default_rng(0)
        vec.reset()
        steps = 0
        start = time.perf_counter()
        deadline = start + seconds
        while time.perf_counter() < deadline:
            masks = np.stack(vec.env_method("action_masks"))
            actions = np.array([
                int(v[rng.integers(0, len(v))]) if len(v := np.flatnonzero(m)) else 0
                for m in masks
            ])
            vec.step(actions)
            steps += n_envs
        return steps / (time.perf_counter() - start)
    finally:
        vec.close()


def bench_learn(n_envs: int, total_steps: int) -> float:
    import torch
    from sb3_contrib import MaskablePPO
    from stable_baselines3.common.vec_env import SubprocVecEnv

    from sts2_env.train.policy import rich_policy_kwargs

    print(f"  cuda available: {torch.cuda.is_available()}")
    vec = SubprocVecEnv([make_rich_combat_env for _ in range(n_envs)])
    try:
        model = MaskablePPO(
            "MlpPolicy", vec,
            learning_rate=2.5e-4, n_steps=512, batch_size=4096, n_epochs=4,
            gamma=0.999, ent_coef=0.01, policy_kwargs=rich_policy_kwargs(),
            device="cuda", verbose=0,
        )
        start = time.perf_counter()
        model.learn(total_timesteps=total_steps)
        elapsed = time.perf_counter() - start
        return total_steps / elapsed
    finally:
        vec.close()


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--seconds", type=float, default=10.0)
    parser.add_argument("--n-envs", type=int, default=16)
    parser.add_argument("--learn-steps", type=int, default=20_000)
    parser.add_argument("--skip-learn", action="store_true")
    parser.add_argument("--skip-subproc", action="store_true")
    args = parser.parse_args()

    from sts2_env.gym_env.combat_env import STS2CombatEnv
    from sts2_env.gym_env.run_env import STS2RunEnv

    results: dict[str, float] = {}

    print("-- single env, random masked actions --")
    results["old_combat"] = bench_single_env(
        STS2CombatEnv(character_id="Necrobinder", ascension_level=10), args.seconds)
    print(f"old combat env : {results['old_combat']:8.0f} steps/s")
    results["rich_combat"] = bench_single_env(make_rich_combat_env(), args.seconds)
    print(f"rich combat env: {results['rich_combat']:8.0f} steps/s "
          f"(x{results['old_combat'] / results['rich_combat']:.2f} slower)")
    results["old_run"] = bench_single_env(
        STS2RunEnv(character_id="Necrobinder", ascension_level=10), args.seconds)
    print(f"old run env    : {results['old_run']:8.0f} steps/s")
    results["rich_run"] = bench_single_env(make_rich_run_env(), args.seconds)
    print(f"rich run env   : {results['rich_run']:8.0f} steps/s "
          f"(x{results['old_run'] / results['rich_run']:.2f} slower)")

    if not args.skip_subproc:
        print(f"\n-- {args.n_envs}-env SubprocVecEnv --")
        rate = bench_subproc(make_rich_combat_env, args.n_envs, args.seconds)
        print(f"rich combat env: {rate:8.0f} steps/s total")
        rate = bench_subproc(make_rich_run_env, args.n_envs, args.seconds)
        print(f"rich run env   : {rate:8.0f} steps/s total")

    if not args.skip_learn:
        print(f"\n-- MaskablePPO.learn({args.learn_steps}) on GPU, rich combat env --")
        rate = bench_learn(args.n_envs, args.learn_steps)
        print(f"training       : {rate:8.0f} steps/s (rollout + optimize)")


if __name__ == "__main__":
    main()
