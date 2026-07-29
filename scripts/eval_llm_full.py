#!/usr/bin/env python3
"""Evaluate a local LLM making EVERY decision -- combat and out of combat.

The companion script ``eval_llm_agent.py`` leaves fights to the deterministic
beam planner, so its 13.1-floor result measures out-of-combat play only. Here
the model plays the fights too. That is the configuration the live bridge
already runs, and whose combat quality has never been measured (see the
"combat quality is unmeasured" note in ``sts2_env/bridge/llm_policy.py``).

Same env, same seed block, same metrics as every other run-side result, so the
number lands beside the measured baselines:

    random routing + planner       : 10.27 +/- 0.62 floors
    knowledge policy + planner     : 10.67 +/- 0.76 floors
    LLM out-of-combat + planner    : 13.13 +/- 1.52 floors  (8 eps, Q3_K_M)

Read the diagnostics before the outcome. Combat is where nearly all the
decisions are, so a combat parse collapse would drag floors down while the
out-of-combat parse rate still looked fine -- the per-arena rates exist to
make that visible. Parse failures in combat fall back to a RANDOM legal
action, never the planner (which would contaminate the measurement) and never
option 0 (which is END TURN).

Examples
--------
    python scripts/eval_llm_full.py --model models/Qwen3.6-27B-Q8_0.gguf \
        --episodes 20 --max-act-count 1

    # validate the harness with no model at all
    python scripts/eval_llm_full.py --stub --episodes 3
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


class _StubLLM:
    """Answers every prompt with a legal choice, with no model loaded.

    Exists so the harness -- rendering, parsing, mask decoding, combat
    boundary accounting -- can be validated end to end on a laptop before
    committing GPU hours. It picks the LAST option, which in combat is never
    'End turn' (option 0), so a stub run actually plays cards and exercises
    the interesting paths.
    """

    def __init__(self):
        self.calls = 0
        self.total_s = 0.0
        self.total_out_tokens = 0
        self.load_s = 0.0
        self._n = 0

    @property
    def tokens_per_s(self) -> float:
        return 0.0

    def ask(self, system: str, user: str) -> str:
        self.calls += 1
        n = user.count("\n0. ") and 0
        # Count the numbered options actually offered.
        opts = [ln for ln in user.splitlines()
                if ln[:1].isdigit() and ". " in ln[:5]]
        self._n = max(1, len(opts))
        return f"CHOICE: {self._n - 1}\nWHY: stub"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", default=None, help="Path to a GGUF file")
    ap.add_argument("--stub", action="store_true",
                    help="Run with a stub model to validate the harness")
    ap.add_argument("--episodes", type=int, default=20)
    ap.add_argument("--seed-base", type=int, default=10_000_000,
                    help="Held-out eval seed block, shared with every other "
                         "run-side result so numbers are comparable")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1,
                    help="Acts needed to WIN. 1 = the current goal, beat act 1")
    ap.add_argument("--character", default="Necrobinder")
    ap.add_argument("--n-ctx", type=int, default=8192)
    ap.add_argument("--n-gpu-layers", type=int, default=-1)
    ap.add_argument("--temperature", type=float, default=0.3)
    ap.add_argument("--max-tokens", type=int, default=160)
    ap.add_argument("--enable-thinking", action="store_true",
                    help="Let the model emit a <think> block. Hundreds of "
                         "tokens per decision; only with throughput to spare")
    ap.add_argument("--max-steps", type=int, default=4000,
                    help="Hard per-episode decision cap (safety valve)")
    ap.add_argument("--transcript", default="output/llm_full_transcript.jsonl")
    ap.add_argument("--json-out", default="output/llm_full_eval.json")
    args = ap.parse_args()

    if not args.stub and not args.model:
        ap.error("--model is required unless --stub is given")

    import sts2_env.events  # noqa: F401

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.llm.runner import LLMConfig, LLMFullPolicy, LocalLLM

    rev, dirty = git_rev()
    print(f"code version : {rev}{' (DIRTY)' if dirty else ''}")

    if args.stub:
        print("model        : STUB (harness validation, no model loaded)")
        llm = _StubLLM()
    else:
        print(f"model        : {args.model}")
        print(f"loading (n_gpu_layers={args.n_gpu_layers}, n_ctx={args.n_ctx}, "
              f"thinking={args.enable_thinking}) ...", flush=True)
        llm = LocalLLM(LLMConfig(
            model_path=args.model, n_ctx=args.n_ctx,
            n_gpu_layers=args.n_gpu_layers, max_tokens=args.max_tokens,
            temperature=args.temperature, enable_thinking=args.enable_thinking,
        ))
        print(f"loaded in {llm.load_s:.0f}s\n", flush=True)

    # A PLAIN run env: no planner, no combat controller. Combat therefore
    # reaches the policy, which is the whole point of this script.
    env = RichSTS2RunEnv(character_id=args.character,
                         ascension_level=args.ascension,
                         max_act_count=args.max_act_count)
    env.set_shaping_scale(0.0)  # pure-sparse eval, as everywhere else
    policy = LLMFullPolicy(env, llm, fallback="knowledge",
                           log_path=args.transcript)

    floors, decks, ups, wins, acts, steps_used = [], [], [], [], [], []
    t0 = time.time()
    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < args.max_steps:
            mask = np.asarray(env.action_masks(), dtype=bool)
            obs, r, done, tr, info = env.step(int(policy.act(obs, mask)))
            n += 1
        floors.append(int(info.get("floor", 0)))
        wins.append(bool(info.get("won", False)))
        acts.append(int(info.get("act", 0)))
        steps_used.append(n)
        rs = env._mgr.run_state
        decks.append(len(rs.player.deck))
        ups.append(sum(1 for c in rs.player.deck if c.upgraded))
        st = policy.stats()
        print(f"  ep {i+1}/{args.episodes}: floor {floors[-1]:>2} "
              f"won={str(wins[-1]):<5} deck {decks[-1]} upgr {ups[-1]} | "
              f"combat parse {st['combat_parse_rate']:.0%} "
              f"@{st['combat_s_per_decision']:.1f}s | "
              f"fights {st['combats_won']}/{st['combats_entered']} | "
              f"{time.time()-t0:.0f}s elapsed", flush=True)

    st = policy.stats()
    n_ep = len(floors)

    def _wilson(k, n, z=1.96):
        if n == 0:
            return (0.0, 0.0)
        import math
        p = k / n
        d = 1 + z * z / n
        c = p + z * z / (2 * n)
        m = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n))
        return ((c - m) / d, (c + m) / d)

    lo, hi = _wilson(int(sum(wins)), n_ep)
    res = {
        "_meta": {
            "git_rev": rev, "dirty": dirty,
            "model": "STUB" if args.stub else args.model,
            "episodes": args.episodes, "ascension": args.ascension,
            "max_act_count": args.max_act_count, "character": args.character,
            "n_gpu_layers": args.n_gpu_layers, "temperature": args.temperature,
            "enable_thinking": args.enable_thinking,
            "seed_base": args.seed_base,
            "combat_played_by": "LLM",
        },
        "mean_floors": float(np.mean(floors)),
        "se_floors": (float(np.std(floors, ddof=1) / np.sqrt(n_ep))
                      if n_ep > 1 else 0.0),
        "win_rate": float(np.mean(wins)),
        "win_rate_ci95": [round(lo, 4), round(hi, 4)],
        "mean_act": float(np.mean(acts)),
        "mean_deck": float(np.mean(decks)),
        "mean_upgrades": float(np.mean(ups)),
        "floors": floors,
        "mean_decisions_per_episode": float(np.mean(steps_used)),
        "llm": st,
        "wall_s": round(time.time() - t0, 1),
    }

    print(f"\n=== LLM PLAYING EVERYTHING ({n_ep} eps, asc {args.ascension}, "
          f"act-{args.max_act_count} goal) ===")
    print(f"  parse rate     : overall {st['parse_rate']:.1%}  |  "
          f"combat {st['combat_parse_rate']:.1%} "
          f"({st['combat_asked']} asked)  |  "
          f"out-of-combat {st['noncombat_parse_rate']:.1%} "
          f"({st['noncombat_asked']} asked)")
    if st["combat_parse_rate"] < 0.9 or st["noncombat_parse_rate"] < 0.9:
        print("  >> LOW PARSE RATE: outcomes below reflect the random/knowledge "
              "fallback as much as the model. Fix parsing before reading them.")
    print(f"  floors         : {res['mean_floors']:.2f} +/- {res['se_floors']:.2f}")
    print(f"  win rate       : {res['win_rate']:.1%}  "
          f"95% CI [{lo:.1%}, {hi:.1%}]")
    print(f"  fights         : {st['combats_won']}/{st['combats_entered']} won "
          f"({st['combat_win_rate']:.1%}), "
          f"mean {st['mean_combat_hp_lost']:.1f} HP lost per fight")
    print(f"  deck / upgrades: {res['mean_deck']:.1f} / {res['mean_upgrades']:.2f}")
    print(f"  speed          : combat {st['combat_s_per_decision']:.2f}s/decision, "
          f"out-of-combat {st['noncombat_s_per_decision']:.2f}s, "
          f"{st['llm_tokens_per_s']:.1f} tok/s")
    print(f"  decisions/ep   : {res['mean_decisions_per_episode']:.0f}")
    print("  baselines      : random+planner 10.27 +/- 0.62 | "
          "knowledge+planner 10.67 +/- 0.76 | LLM-ooc+planner 13.13 +/- 1.52")

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(res, indent=2), encoding="utf-8")
    if args.transcript and policy.transcript:
        Path(args.transcript).parent.mkdir(parents=True, exist_ok=True)
        Path(args.transcript).write_text(
            "\n".join(json.dumps(t) for t in policy.transcript), encoding="utf-8")
        print(f"\n  transcript: {args.transcript} "
              f"({len(policy.transcript)} decisions)")
    print(f"  results   : {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
