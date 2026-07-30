#!/usr/bin/env python3
"""Decision-level benchmark: grammar vs thinking, refereed by the planner.

Why this exists. Three thinking episodes cost ~5 hours and yield THREE floor
samples. With a per-episode spread of roughly +/-0.8 floors that cannot resolve
anything smaller than a few floors, so "did reasoning help?" is not answerable
from the episode outcome at that sample size.

The same compute buys a far tighter measurement if it is spent per DECISION
rather than per run. At every combat state along one trajectory, ask all three:

    grammar  -- the LLM, forced-format, no visible reasoning
    thinking -- the LLM, reason within budget then budget-forced answer
    planner  -- the deterministic whole-combat beam search

and score the two LLM configs by how often each agrees with the planner. The
planner is the strongest combat policy in this project (it beats the learned
agent, and `random x planner` reaches 11.80 floors versus 8.94 for the LLM
playing everything), so planner agreement is a defensible proxy for combat
decision quality -- and it turns ~150 decisions into ~150 paired comparisons
instead of 3 noisy run outcomes.

Both LLM configs see the IDENTICAL state, because the trajectory is driven by a
single fixed actor. That is the control the episode-level comparison lacks: there,
the two configs diverge after the first differing action and are thereafter
answering different questions.

Reported per config: planner-agreement rate with a Wilson interval, plus a
McNemar exact test on the discordant pairs (the correct test for two methods
scored on the same items).
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import math
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def wilson(k: int, n: int, z: float = 1.96) -> tuple[float, float]:
    if n == 0:
        return (0.0, 0.0)
    p = k / n
    d = 1 + z * z / n
    c = p + z * z / (2 * n)
    m = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n))
    return (max(0.0, (c - m) / d), min(1.0, (c + m) / d))


def mcnemar_exact(b: int, c: int) -> float:
    """Two-sided exact McNemar p-value on discordant counts b and c."""
    n = b + c
    if n == 0:
        return 1.0
    k = min(b, c)
    tail = sum(math.comb(n, i) for i in range(0, k + 1)) / (2 ** n)
    return min(1.0, 2 * tail)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True)
    ap.add_argument("--decisions", type=int, default=120,
                    help="Combat decisions to score (each costs one grammar "
                         "call + one thinking call + one planner search)")
    ap.add_argument("--seed-base", type=int, default=10_000_000)
    ap.add_argument("--episodes", type=int, default=6,
                    help="Trajectories to walk while gathering decisions")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--n-ctx", type=int, default=8192)
    ap.add_argument("--n-gpu-layers", type=int, default=-1)
    ap.add_argument("--temperature", type=float, default=0.3)
    ap.add_argument("--think-tokens", type=int, default=700)
    ap.add_argument("--actor", choices=["planner", "grammar"], default="planner",
                    help="Who DRIVES the trajectory. Fixed for both configs so "
                         "they are always scored on the same state. 'planner' "
                         "walks strong states; 'grammar' walks LLM-reachable ones.")
    ap.add_argument("--json-out", default="output/llm_decision_bench.json")
    ap.add_argument("--transcript", default="output/llm_decision_bench.jsonl")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.llm.runner import LLMConfig, LocalLLM
    from sts2_env.llm.state_text import (
        COMBAT_SYSTEM_PROMPT,
        parse_choice,
        render_combat_decision,
        render_run_decision_masked,
        SYSTEM_PROMPT,
    )
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_planner import (
        EVAL_LADDER,
        PlannedCombatController,
    )

    # Two LocalLLMs would load 28GB twice. Load once and flip the config, which
    # is safe because enable_thinking/grammar are read per call.
    llm = LocalLLM(LLMConfig(
        model_path=args.model, n_ctx=args.n_ctx,
        n_gpu_layers=args.n_gpu_layers, max_tokens=16,
        temperature=args.temperature, enable_thinking=False,
    ))
    print(f"model loaded in {llm.load_s:.0f}s", flush=True)

    def ask(system, prompt, thinking: bool):
        llm.cfg.enable_thinking = thinking
        llm.cfg.max_tokens = args.think_tokens if thinking else 16
        t0 = time.time()
        reply = llm.ask(system, prompt)
        return reply, time.time() - t0

    rows = []
    tfh = open(args.transcript, "w", encoding="utf-8")
    t_start = time.time()

    for ep in range(args.episodes):
        if len(rows) >= args.decisions:
            break
        env = RichSTS2RunEnv(character_id="Necrobinder",
                             ascension_level=args.ascension,
                             max_act_count=args.max_act_count)
        env.set_shaping_scale(0.0)
        planner = PlannedCombatController(env, ladder=EVAL_LADDER)
        obs, info = env.reset(seed=args.seed_base + ep)
        mgr = env._mgr
        done = tr = False
        n = 0
        while not (done or tr) and n < 4000 and len(rows) < args.decisions:
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            in_combat = mgr.phase == RunManager.PHASE_COMBAT

            drive = None
            if in_combat and legal.size > 1:
                dec = render_combat_decision(mgr, mask)
                if dec is not None:
                    # Planner referee.
                    t0 = time.time()
                    try:
                        p_act = int(planner.act(obs, mask))
                    except Exception:
                        p_act = None
                    p_s = time.time() - t0

                    g_reply, g_s = ask(COMBAT_SYSTEM_PROMPT, dec.prompt, False)
                    g_idx = parse_choice(g_reply, len(dec.options))
                    t_reply, t_s = ask(COMBAT_SYSTEM_PROMPT, dec.prompt, True)
                    t_idx = parse_choice(t_reply, len(dec.options))

                    g_act = (dec.options[g_idx].get("env_action")
                             if g_idx is not None and g_idx < len(dec.options) else None)
                    t_act = (dec.options[t_idx].get("env_action")
                             if t_idx is not None and t_idx < len(dec.options) else None)

                    row = {
                        "seed": args.seed_base + ep, "step": n,
                        "n_options": len(dec.options),
                        "planner": p_act, "grammar": g_act, "thinking": t_act,
                        "g_parsed": g_idx is not None, "t_parsed": t_idx is not None,
                        "g_agrees": (g_act is not None and g_act == p_act),
                        "t_agrees": (t_act is not None and t_act == p_act),
                        "g_s": round(g_s, 2), "t_s": round(t_s, 2),
                        "planner_s": round(p_s, 2),
                    }
                    rows.append(row)
                    tfh.write(json.dumps(row) + "\n")
                    tfh.flush()
                    if len(rows) % 10 == 0:
                        ga = sum(r["g_agrees"] for r in rows)
                        ta = sum(r["t_agrees"] for r in rows)
                        print(f"  {len(rows)}/{args.decisions}: planner-agreement "
                              f"grammar {ga/len(rows):.1%} thinking {ta/len(rows):.1%} "
                              f"({time.time()-t_start:.0f}s)", flush=True)
                    drive = p_act if args.actor == "planner" else g_act

            if drive is None:
                if in_combat:
                    drive = int(legal[0])
                else:
                    d2 = render_run_decision_masked(mgr, mask)
                    if d2 is None:
                        drive = int(legal[0])
                    else:
                        r2, _ = ask(SYSTEM_PROMPT, d2.prompt, False)
                        i2 = parse_choice(r2, len(d2.options))
                        drive = (d2.options[i2].get("env_action")
                                 if i2 is not None and i2 < len(d2.options)
                                 else int(legal[0]))
            obs, r, done, tr, info = env.step(int(drive))
            n += 1
    tfh.close()

    n = len(rows)
    if n == 0:
        print("no decisions scored")
        return 2
    g = sum(r["g_agrees"] for r in rows)
    t = sum(r["t_agrees"] for r in rows)
    # discordant pairs for McNemar
    b = sum(1 for r in rows if r["t_agrees"] and not r["g_agrees"])
    c = sum(1 for r in rows if r["g_agrees"] and not r["t_agrees"])
    glo, ghi = wilson(g, n)
    tlo, thi = wilson(t, n)
    p = mcnemar_exact(b, c)

    print(f"\n=== decision-level benchmark ({n} combat decisions, "
          f"identical states) ===")
    print(f"  planner agreement, grammar : {g}/{n} = {g/n:.1%}  "
          f"95% CI [{glo:.1%}, {ghi:.1%}]")
    print(f"  planner agreement, thinking: {t}/{n} = {t/n:.1%}  "
          f"95% CI [{tlo:.1%}, {thi:.1%}]")
    print(f"  discordant: thinking-only-right {b}, grammar-only-right {c}")
    print(f"  McNemar exact p = {p:.4f}"
          + ("  (significant at 0.05)" if p < 0.05 else "  (not significant)"))
    print(f"  parse rate  grammar {sum(r['g_parsed'] for r in rows)/n:.1%}  "
          f"thinking {sum(r['t_parsed'] for r in rows)/n:.1%}")
    print(f"  cost/decision  grammar {np.mean([r['g_s'] for r in rows]):.1f}s  "
          f"thinking {np.mean([r['t_s'] for r in rows]):.1f}s  "
          f"planner {np.mean([r['planner_s'] for r in rows]):.1f}s")

    out = {"_meta": vars(args), "n": n,
           "grammar_agree": g, "thinking_agree": t,
           "grammar_ci": [glo, ghi], "thinking_ci": [tlo, thi],
           "mcnemar_b": b, "mcnemar_c": c, "mcnemar_p": p,
           "rows": rows}
    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(out, indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
