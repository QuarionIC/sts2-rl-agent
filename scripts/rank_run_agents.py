#!/usr/bin/env python3
"""Rank hierarchical run-agent checkpoints on a common, paired seed block.

Why this exists
---------------
The recorded eval_history.jsonl numbers for the candidate run agents sit
within noise of each other (mean_floors 8.7-9.8, win rate 0-4% over 200
episodes), and each was written by a DIFFERENT training run at a different
moment. Picking the largest of those numbers picks the luckiest one, not the
best one.

This runs every candidate over the SAME seeds with the SAME combat
controller and records PER-EPISODE outcomes, so checkpoints can be compared
with a paired test rather than by eyeballing two independent means.

The combat controller is held fixed across candidates on purpose: we are
ranking out-of-combat policies, so combat must not be a free variable. A
random-action policy is included as a floor -- if a trained checkpoint cannot
beat it, the checkpoint is not contributing anything.

Usage
-----
    python scripts/rank_run_agents.py \
        --models output/cand/joint_r2.zip output/cand/joint_r5.zip \
        --combat-model output/cand/combat.zip \
        --episodes 300 --out output/run_agent_ranking.json
"""
from __future__ import annotations

import argparse
import json
import time
from pathlib import Path

import numpy as np


def eval_one(model, env, n_episodes: int, seed_block: int, label: str) -> list[dict]:
    """Play n_episodes and return one record per episode.

    Per-episode records are the point: aggregates alone cannot support a
    paired test, and a paired test is the only way to compare two policies
    this close together without burning an enormous number of episodes.
    """
    rows: list[dict] = []
    for i in range(n_episodes):
        seed = seed_block + i
        obs, info = env.reset(seed=seed)
        done = tr = False
        n = 0
        while not (done or tr) and n < 2000:
            mask = env.action_masks()
            if model is None:
                legal = np.flatnonzero(np.asarray(mask, dtype=bool))
                # Deterministic given the seed: index by step count, not RNG,
                # so the random baseline is reproducible across arms.
                a = int(legal[n % len(legal)]) if legal.size else 0
            else:
                a, _ = model.predict(obs, action_masks=mask, deterministic=True)
                a = int(a)
            obs, r, done, tr, info = env.step(a)
            n += 1

        rs = env._mgr.run_state if env._mgr is not None else None
        rows.append({
            "label": label,
            "seed": seed,
            "floor": int(info.get("floor", 0)),
            "act": int(info.get("act", 0)),
            "won": bool(info.get("won", False)),
            "truncated": bool(tr),
            "decisions": int(info.get("run_decisions", n)),
            "deck": len(rs.player.deck) if rs is not None else 0,
            "upgrades": (sum(1 for c in rs.player.deck if c.upgraded)
                         if rs is not None else 0),
        })
        if (i + 1) % 25 == 0:
            print(f"  [{label}] {i + 1}/{n_episodes}", flush=True)
    return rows


def summarize(rows: list[dict]) -> dict:
    return {
        "episodes": len(rows),
        "mean_floors": float(np.mean([r["floor"] for r in rows])),
        "mean_act": float(np.mean([r["act"] for r in rows])),
        "win_rate": float(np.mean([r["won"] for r in rows])),
        "truncation_rate": float(np.mean([r["truncated"] for r in rows])),
        "mean_deck": float(np.mean([r["deck"] for r in rows])),
        "mean_upgrades": float(np.mean([r["upgrades"] for r in rows])),
        "mean_decisions": float(np.mean([r["decisions"] for r in rows])),
    }


def paired_delta(a: list[dict], b: list[dict], key: str) -> dict:
    """Paired mean difference (a - b) on the shared seed set."""
    by_seed_b = {r["seed"]: r for r in b}
    diffs = [float(r[key] - by_seed_b[r["seed"]][key])
             for r in a if r["seed"] in by_seed_b]
    if not diffs:
        return {"n": 0}
    arr = np.asarray(diffs, dtype=float)
    n = arr.size
    mean = float(arr.mean())
    # Standard error of the paired mean difference. With n>=30 the normal
    # approximation is fine; below that treat the interval as indicative.
    se = float(arr.std(ddof=1) / np.sqrt(n)) if n > 1 else 0.0
    return {
        "n": n,
        "mean_diff": mean,
        "se": se,
        "ci95": [mean - 1.96 * se, mean + 1.96 * se],
        "a_better": int((arr > 0).sum()),
        "b_better": int((arr < 0).sum()),
        "tied": int((arr == 0).sum()),
    }


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    # Not required: arms are cheap to run one-per-process and the seed block
    # is identical across processes, so pairing survives being split up. A
    # single sequential process over 6 arms x 150 planner-driven episodes is
    # most of a day on 1 core; 6 processes on a 20-core box is ~an hour.
    ap.add_argument("--models", nargs="*", default=[],
                    help="Run-agent checkpoints to rank.")
    ap.add_argument("--combat-model", default=None,
                    help="Combat checkpoint used as the fixed combat controller.")
    ap.add_argument("--use-planner", action="store_true",
                    help="Use the deterministic combat planner instead "
                         "(matches live play; much slower).")
    ap.add_argument("--episodes", type=int, default=300)
    ap.add_argument("--seed-block", type=int, default=None,
                    help="Defaults to the training module's EVAL_SEED_BLOCK.")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--include-random", action="store_true",
                    help="Add a random-legal-action arm as a floor.")
    ap.add_argument("--out", default="output/run_agent_ranking.json")
    args = ap.parse_args()

    from sb3_contrib import MaskablePPO

    from scripts.train_hierarchical import EVAL_SEED_BLOCK, make_run_env

    seed_block = args.seed_block if args.seed_block is not None else EVAL_SEED_BLOCK

    arms: list[tuple[str, object]] = []
    if args.include_random:
        arms.append(("random", None))
    for path in args.models:
        label = Path(path).stem
        print(f"loading {label} from {path}", flush=True)
        arms.append((label, MaskablePPO.load(path, device="cpu")))
    if not arms:
        raise SystemExit("nothing to evaluate: pass --models and/or --include-random")

    all_rows: dict[str, list[dict]] = {}
    t0 = time.time()
    for label, model in arms:
        # A FRESH env per arm. Reusing one env across arms would let episode
        # N of arm 2 inherit internal state from arm 1, which silently breaks
        # the pairing the whole comparison rests on.
        env = make_run_env(args.combat_model, ascension=args.ascension,
                           max_act_count=args.max_act_count, seed=seed_block,
                           use_planner=args.use_planner)
        env.set_shaping_scale(0.0)
        print(f"=== arm: {label} ===", flush=True)
        all_rows[label] = eval_one(model, env, args.episodes, seed_block, label)

    summaries = {k: summarize(v) for k, v in all_rows.items()}
    ranked = sorted(summaries.items(),
                    key=lambda kv: (kv[1]["win_rate"], kv[1]["mean_floors"]),
                    reverse=True)
    best_label = ranked[0][0]

    comparisons = {
        f"{label}_vs_{best_label}": {
            "floors": paired_delta(all_rows[label], all_rows[best_label], "floor"),
            "won": paired_delta(all_rows[label], all_rows[best_label], "won"),
        }
        for label, _ in ranked[1:]
    }

    result = {
        # PER-EPISODE ROWS ARE THE POINT. Arms run on an identical seed
        # block, so they can be compared with a paired test -- but only if
        # the rows survive. The first run of this script persisted summaries
        # only, which forced an unpaired comparison across separately-launched
        # arms and threw away most of the statistical power the shared seeds
        # were there to provide.
        "rows": all_rows,
        "config": {
            "episodes": args.episodes,
            "seed_block": seed_block,
            "ascension": args.ascension,
            "max_act_count": args.max_act_count,
            "combat_model": args.combat_model,
            "use_planner": args.use_planner,
        },
        "summaries": summaries,
        "ranking": [label for label, _ in ranked],
        "paired_vs_best": comparisons,
        "wall_s": round(time.time() - t0, 1),
    }

    out = Path(args.out)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))
    print(f"\nwrote {out}")

    print("\n=== RANKING ===")
    for label, s in ranked:
        print(f"  {label:16s} win {s['win_rate']:.3f}  floors {s['mean_floors']:.2f}"
              f"  deck {s['mean_deck']:.1f}  ups {s['mean_upgrades']:.2f}")
    print("\nPaired vs best (positive mean_diff = that arm beat the best arm "
          "on shared seeds):")
    for name, cmp in comparisons.items():
        f = cmp["floors"]
        if f.get("n"):
            print(f"  {name}: floors {f['mean_diff']:+.2f} "
                  f"(95% CI {f['ci95'][0]:+.2f}..{f['ci95'][1]:+.2f}), "
                  f"better on {f['a_better']}, worse on {f['b_better']}, "
                  f"tied {f['tied']}")


if __name__ == "__main__":
    main()
