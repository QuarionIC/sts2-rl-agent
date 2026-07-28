#!/usr/bin/env python3
"""Evaluate a local LLM as the out-of-combat run agent at ascension 0.

Same env, same seed block, same metrics as every other run-side result in
this project, so the number lands directly beside the measured baselines:

    random routing + planner : 10.27 +/- 0.62 floors
    knowledge policy         : 10.67 +/- 0.76 floors

Combat is played by the deterministic planner, so this measures exactly one
thing: the quality of the model's out-of-combat decisions.

Beyond floors it reports the diagnostics that decide whether a weak result
means "the model plays badly" or "the harness is failing it" -- parse rate,
seconds per decision, and tokens/sec. A low parse rate invalidates the
floors number, so it is printed first.

Example
-------
    python scripts/eval_llm_agent.py --model models/Qwen3.6-27B-Q3_K_M.gguf \
        --episodes 10 --n-gpu-layers 20
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import subprocess
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def git_rev() -> tuple[str, bool]:
    root = Path(__file__).resolve().parent.parent
    try:
        rev = subprocess.run(["git", "rev-parse", "--short", "HEAD"],
                             capture_output=True, text=True, cwd=root).stdout.strip()
        dirty = bool(subprocess.run(["git", "status", "--porcelain"],
                                    capture_output=True, text=True,
                                    cwd=root).stdout.strip())
        return rev or "unknown", dirty
    except Exception:
        return "unknown", False


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True, help="Path to a GGUF file")
    ap.add_argument("--episodes", type=int, default=10)
    ap.add_argument("--seed-base", type=int, default=10_000_000)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--n-ctx", type=int, default=8192)
    ap.add_argument("--n-gpu-layers", type=int, default=-1)
    ap.add_argument("--temperature", type=float, default=0.3)
    ap.add_argument("--max-tokens", type=int, default=160)
    ap.add_argument("--fallback", choices=["knowledge", "random"], default="knowledge")
    ap.add_argument("--transcript", default="output/llm_transcript.jsonl")
    ap.add_argument("--json-out", default="output/llm_eval.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401

    from sts2_env.llm.runner import LLMConfig, LLMRunPolicy, LocalLLM
    from train_hierarchical import make_run_env

    rev, dirty = git_rev()
    print(f"code version : {rev}{' (DIRTY)' if dirty else ''}")
    print(f"model        : {args.model}")
    print(f"loading (n_gpu_layers={args.n_gpu_layers}, n_ctx={args.n_ctx}) ...",
          flush=True)

    llm = LocalLLM(LLMConfig(
        model_path=args.model, n_ctx=args.n_ctx,
        n_gpu_layers=args.n_gpu_layers, max_tokens=args.max_tokens,
        temperature=args.temperature,
    ))
    print(f"loaded in {llm.load_s:.0f}s\n", flush=True)

    env = make_run_env(None, ascension=args.ascension,
                       max_act_count=args.max_act_count, seed=args.seed_base,
                       use_planner=True, planner_ladder="train")
    env.set_shaping_scale(0.0)
    policy = LLMRunPolicy(env, llm, fallback=args.fallback,
                          log_path=args.transcript)

    floors, decks, ups, wins, acts = [], [], [], [], []
    t0 = time.time()
    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 2000:
            mask = np.asarray(env.action_masks(), dtype=bool)
            obs, r, done, tr, info = env.step(int(policy.act(obs, mask)))
            n += 1
        floors.append(int(info.get("floor", 0)))
        wins.append(bool(info.get("won", False)))
        acts.append(int(info.get("act", 0)))
        rs = env._mgr.run_state
        decks.append(len(rs.player.deck))
        ups.append(sum(1 for c in rs.player.deck if c.upgraded))
        st = policy.stats()
        print(f"  ep {i+1}/{args.episodes}: floor {floors[-1]:>2} "
              f"deck {decks[-1]} upgr {ups[-1]} | parse {st['parse_rate']:.0%} "
              f"{st['s_per_decision']:.1f}s/decision {st['llm_tokens_per_s']:.1f} tok/s "
              f"({time.time()-t0:.0f}s elapsed)", flush=True)

    st = policy.stats()
    res = {
        "_meta": {"git_rev": rev, "dirty": dirty, "model": args.model,
                  "episodes": args.episodes, "ascension": args.ascension,
                  "max_act_count": args.max_act_count,
                  "n_gpu_layers": args.n_gpu_layers,
                  "temperature": args.temperature},
        "mean_floors": float(np.mean(floors)),
        "se_floors": float(np.std(floors, ddof=1) / np.sqrt(len(floors)))
        if len(floors) > 1 else 0.0,
        "mean_deck": float(np.mean(decks)),
        "mean_upgrades": float(np.mean(ups)),
        "win_rate": float(np.mean(wins)),
        "mean_act": float(np.mean(acts)),
        "floors": floors,
        "llm": st,
        "wall_s": round(time.time() - t0, 1),
    }

    print(f"\n=== LLM RUN AGENT ({args.episodes} eps, asc {args.ascension}) ===")
    print(f"  parse rate     : {st['parse_rate']:.1%}  "
          f"({st['parsed']}/{st['asked']} decisions)")
    if st["parse_rate"] < 0.9:
        print("  >> LOW PARSE RATE: the floors number below reflects the fallback "
              "policy as much as the model. Fix parsing before reading it.")
    print(f"  floors         : {res['mean_floors']:.2f} +/- {res['se_floors']:.2f}")
    print(f"  deck / upgrades: {res['mean_deck']:.1f} / {res['mean_upgrades']:.2f}")
    print(f"  speed          : {st['s_per_decision']:.1f}s per decision, "
          f"{st['llm_tokens_per_s']:.1f} tok/s")
    print(f"  baselines      : random 10.27 +/- 0.62 | knowledge 10.67 +/- 0.76")

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(res, indent=2), encoding="utf-8")
    if args.transcript and policy.transcript:
        Path(args.transcript).write_text(
            "\n".join(json.dumps(t) for t in policy.transcript), encoding="utf-8")
        print(f"\n  transcript: {args.transcript} ({len(policy.transcript)} decisions)")
    print(f"  results   : {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
