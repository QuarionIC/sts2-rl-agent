#!/usr/bin/env python3
"""Verify knowledge-derived skills in the simulator (the Voyager keep/reject step).

Public guides encode a lot of human knowledge, but a tier list is written for
the live retail game, not this simulator's exact patch + mod set, and its
advice was never tested against a deterministic combat planner. So nothing
from ``sts2_env.knowledge`` is trusted until it is measured here.

Two modes:

``--mode full``
    Full knowledge policy vs the random-routing baseline. Answers "does
    community knowledge help at all?"

``--mode ablate``
    Leave-one-out: disable each skill in turn and measure the drop. Answers
    "which skills are actually carrying the result?" A skill whose removal
    costs nothing is not verified, however sensible it sounds -- that is the
    whole point of the exercise.

Runs are seed-matched across arms, and the paired difference is reported with
a standard error, because the run-to-run spread in this game is large (~0.6
floors SE at n=30) and a raw mean comparison invites exactly the kind of
small-sample over-reading this project has already been burned by.
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

sys.path.insert(0, str(Path(__file__).resolve().parent))

ALL_SKILLS = ["pick_card", "rest_or_smith", "smith_target", "route", "shop"]


def run_arm(label: str, enabled: set[str] | None, episodes: int, seed_base: int,
            ascension: int, max_act_count: int, ladder: str,
            random_policy: bool = False) -> dict:
    """Play ``episodes`` seed-matched runs; return per-episode metrics."""
    import sts2_env.events  # noqa: F401

    from sts2_env.knowledge.policy import KnowledgeRunPolicy
    from train_hierarchical import make_run_env

    env = make_run_env(None, ascension=ascension, max_act_count=max_act_count,
                       seed=seed_base, use_planner=True, planner_ladder=ladder)
    env.set_shaping_scale(0.0)
    policy = None if random_policy else KnowledgeRunPolicy(env, enabled=enabled)
    rng = np.random.default_rng(12345)

    floors, decks, ups, wins, acts = [], [], [], [], []
    t0 = time.time()
    for i in range(episodes):
        obs, info = env.reset(seed=seed_base + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 2000:
            mask = np.asarray(env.action_masks(), dtype=bool)
            if policy is None:
                action = int(rng.choice(np.flatnonzero(mask)))
            else:
                action = policy.act(obs, mask)
            obs, r, done, tr, info = env.step(int(action))
            n += 1
        floors.append(int(info.get("floor", 0)))
        wins.append(bool(info.get("won", False)))
        acts.append(int(info.get("act", 0)))
        rs = env._mgr.run_state
        decks.append(len(rs.player.deck))
        ups.append(sum(1 for c in rs.player.deck if c.upgraded))
        if (i + 1) % 10 == 0:
            print(f"    {label}: {i+1}/{episodes} ({time.time()-t0:.0f}s)",
                  flush=True)

    return {
        "label": label,
        "floors": floors,
        "mean_floors": float(np.mean(floors)),
        "se_floors": float(np.std(floors, ddof=1) / np.sqrt(len(floors))),
        "mean_deck": float(np.mean(decks)),
        "mean_upgrades": float(np.mean(ups)),
        "win_rate": float(np.mean(wins)),
        "mean_act": float(np.mean(acts)),
        "wall_s": round(time.time() - t0, 1),
    }


def paired_delta(a: list[int], b: list[int]) -> tuple[float, float]:
    """(mean paired difference a-b, its standard error)."""
    d = np.asarray(a, float) - np.asarray(b, float)
    return float(d.mean()), float(d.std(ddof=1) / np.sqrt(len(d)))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--mode", choices=["full", "ablate"], default="full")
    ap.add_argument("--episodes", type=int, default=30)
    ap.add_argument("--seed-base", type=int, default=10_000_000,
                    help="Defaults to the training eval seed block for "
                         "comparability with eval_history numbers")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--ladder", choices=["train", "eval"], default="train")
    ap.add_argument("--json-out", default="output/skill_verification.json")
    args = ap.parse_args()

    # Pin the code version into the result file. The v2 run was invalidated
    # by a mid-flight planner change: Python caches imported modules, so a
    # process that started before an edit runs the OLD code to completion
    # while its output looks current. Recording the commit makes that
    # detectable after the fact instead of by timestamp archaeology.
    import subprocess
    try:
        rev = subprocess.run(["git", "rev-parse", "--short", "HEAD"],
                             capture_output=True, text=True,
                             cwd=Path(__file__).resolve().parent.parent).stdout.strip()
        dirty = bool(subprocess.run(["git", "status", "--porcelain"],
                                    capture_output=True, text=True,
                                    cwd=Path(__file__).resolve().parent.parent).stdout.strip())
    except Exception:
        rev, dirty = "unknown", False

    results = {"_meta": {"git_rev": rev, "dirty_worktree": dirty,
                         "episodes": args.episodes, "ladder": args.ladder,
                         "ascension": args.ascension,
                         "max_act_count": args.max_act_count,
                         "seed_base": args.seed_base}}
    tag = " (DIRTY)" if dirty else ""
    print(f"code version: {rev}{tag}\n")
    print(f"=== baseline: random routing + planner ===", flush=True)
    base = run_arm("random", None, args.episodes, args.seed_base,
                   args.ascension, args.max_act_count, args.ladder,
                   random_policy=True)
    results["random"] = base
    print(f"  floors {base['mean_floors']:.2f} +/- {base['se_floors']:.2f} "
          f"deck {base['mean_deck']:.1f} upgr {base['mean_upgrades']:.2f}\n")

    print(f"=== full knowledge policy ===", flush=True)
    full = run_arm("knowledge", set(ALL_SKILLS), args.episodes, args.seed_base,
                   args.ascension, args.max_act_count, args.ladder)
    results["knowledge"] = full
    d, se = paired_delta(full["floors"], base["floors"])
    print(f"  floors {full['mean_floors']:.2f} +/- {full['se_floors']:.2f} "
          f"deck {full['mean_deck']:.1f} upgr {full['mean_upgrades']:.2f}")
    print(f"  paired delta vs random: {d:+.2f} +/- {se:.2f} floors\n")
    results["knowledge_vs_random"] = {"delta": d, "se": se}

    if args.mode == "ablate":
        print(f"=== leave-one-out ablation ===", flush=True)
        for skill in ALL_SKILLS:
            enabled = set(ALL_SKILLS) - {skill}
            arm = run_arm(f"no_{skill}", enabled, args.episodes, args.seed_base,
                          args.ascension, args.max_act_count, args.ladder)
            results[f"no_{skill}"] = arm
            d2, se2 = paired_delta(arm["floors"], full["floors"])
            verdict = ("CARRIES the result" if d2 < -2 * se2 else
                       "no measurable contribution" if abs(d2) <= 2 * se2 else
                       "HURTS -- removing it helps")
            print(f"  without {skill:<14} floors {arm['mean_floors']:.2f}  "
                  f"delta vs full {d2:+.2f} +/- {se2:.2f}  -> {verdict}")
            results[f"no_{skill}"]["delta_vs_full"] = {"delta": d2, "se": se2,
                                                       "verdict": verdict}

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(results, indent=2),
                                   encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
