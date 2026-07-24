r"""Standalone checkpoint evaluator for the Necrobinder full-run campaign.

Evaluates a MaskablePPO checkpoint on RichSTS2RunEnv with shaping OFF
(shaping_scale=0, pure sparse reward), deterministic actions, and a fixed
seed block, then prints win rate with a Wilson 95% CI plus the per-act
death breakdown. This is the tool for house-doctrine-compliant win-rate
claims (>= 1000 episodes for a final claim).

Usage (from the repo root, always the venv python):

    .venv\Scripts\python.exe .claude/skills/sts2-run-and-operate/scripts/eval_checkpoint.py ^
        --ckpt output/necrobinder_run/G1/best_model.zip --stage G1 --episodes 1000

    # or explicit difficulty instead of a stage preset:
    .venv\Scripts\python.exe .claude/skills/sts2-run-and-operate/scripts/eval_checkpoint.py ^
        --ckpt output/necrobinder_run/G5/best_model.zip --ascension 10 --max-act-count 4

Only run-env (Discrete(157)) checkpoints are compatible. Old combat-only
stage-A/B checkpoints (output/necrobinder_a10/A/) are Discrete(115) and will
fail on the mask shape -- that is expected, not a bug.
"""

from __future__ import annotations

import argparse
import json
import math
import sys
import time
from pathlib import Path

# Keep in sync with STAGES in scripts/train_necrobinder.py (as of 2026-07-24).
STAGE_PRESETS: dict[str, tuple[int, int]] = {
    # stage: (ascension_level, max_act_count)
    "G1": (0, 2),
    "G2": (0, 4),
    "G3": (4, 4),
    "G4": (8, 4),
    "G5": (10, 4),
}


def wilson_ci(wins: int, n: int, z: float = 1.959964) -> tuple[float, float]:
    """Wilson score 95% interval for a binomial proportion."""
    if n == 0:
        return (0.0, 1.0)
    p = wins / n
    denom = 1.0 + z * z / n
    center = (p + z * z / (2 * n)) / denom
    half = z * math.sqrt(p * (1.0 - p) / n + z * z / (4 * n * n)) / denom
    return (max(0.0, center - half), min(1.0, center + half))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--ckpt", required=True,
                        help="Path to a MaskablePPO .zip checkpoint (run-env, Discrete(157))")
    parser.add_argument("--stage", choices=sorted(STAGE_PRESETS),
                        help="Difficulty preset (sets --ascension/--max-act-count)")
    parser.add_argument("--ascension", type=int, default=10)
    parser.add_argument("--max-act-count", type=int, default=4)
    parser.add_argument("--episodes", type=int, default=1000,
                        help="House doctrine: >=1000 for a final claim (default 1000)")
    parser.add_argument("--seed-block", type=int, default=10_000_000,
                        help="First reset seed; episode i uses seed-block + i (default 10,000,000 "
                             "= the trainer's eval block; pick a different block for held-out evals)")
    parser.add_argument("--max-episode-steps", type=int, default=3000)
    parser.add_argument("--device", default="cuda", choices=["cuda", "cpu"],
                        help="Use cpu to avoid contending with a live training run")
    parser.add_argument("--json-out", type=Path, default=None,
                        help="Optionally append the result as one JSON line to this file")
    args = parser.parse_args()

    if args.stage:
        args.ascension, args.max_act_count = STAGE_PRESETS[args.stage]

    ckpt = Path(args.ckpt)
    if not ckpt.exists():
        print(f"error: checkpoint not found: {ckpt}", file=sys.stderr)
        return 2

    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        max_act_count=args.max_act_count,
        reward_config=RewardConfig(shaping_scale=0.0),  # pure sparse eval
        max_steps=args.max_episode_steps,
    )
    model = MaskablePPO.load(str(ckpt.with_suffix("")), device=args.device)

    wins = 0
    truncations = 0
    sim_errors = 0
    floors: list[int] = []
    acts: list[int] = []
    deaths_by_act: dict[int, int] = {}
    start = time.perf_counter()
    for ep in range(args.episodes):
        obs, info = env.reset(seed=args.seed_block + ep)
        done = False
        while not done:
            masks = env.action_masks()
            action, _ = model.predict(obs, action_masks=masks, deterministic=True)
            obs, reward, terminated, truncated, info = env.step(int(action))
            done = terminated or truncated
        won = bool(info.get("won", False))
        wins += won
        truncations += bool(info.get("truncated", False))
        sim_errors += bool(info.get("sim_error", False))
        floors.append(int(info.get("floor", 0)))
        acts.append(int(info.get("act", 0)))
        if not won:
            act = int(info.get("act", 0))
            deaths_by_act[act] = deaths_by_act.get(act, 0) + 1
        if (ep + 1) % 100 == 0:
            rate = wins / (ep + 1)
            print(f"  [{ep + 1}/{args.episodes}] running win_rate={rate:.1%}", flush=True)
    elapsed = time.perf_counter() - start

    n = args.episodes
    lo, hi = wilson_ci(wins, n)
    result = {
        "ckpt": str(ckpt),
        "ascension": args.ascension,
        "max_act_count": args.max_act_count,
        "episodes": n,
        "seed_block": args.seed_block,
        "deterministic": True,
        "shaping_scale": 0.0,
        "win_rate": wins / n,
        "wilson95": [round(lo, 4), round(hi, 4)],
        "truncation_rate": truncations / n,
        "sim_error_rate": sim_errors / n,
        "mean_floors": sum(floors) / n if floors else 0.0,
        "mean_act": sum(acts) / n if acts else 0.0,
        "deaths_by_act": {str(k): v for k, v in sorted(deaths_by_act.items())},
        "wall_s": round(elapsed, 1),
    }
    print(json.dumps(result, indent=2))
    print(f"\nwin_rate = {wins}/{n} = {wins / n:.1%}  "
          f"(Wilson 95% CI [{lo:.1%}, {hi:.1%}])")
    if sim_errors:
        print(f"WARNING: {sim_errors} episodes ended via sim_error (simulator bug, "
              f"scored 0 not death) -- check the log / file a bug before trusting this eval")
    if args.json_out:
        with open(args.json_out, "a", encoding="utf-8") as f:
            f.write(json.dumps(result) + "\n")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
