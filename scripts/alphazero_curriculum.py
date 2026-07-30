#!/usr/bin/env python3
"""AlphaZero-style curriculum for STS2: iterated search -> distil -> train.

What "AlphaZero-style" can and cannot mean here
-----------------------------------------------
AlphaZero proper assumes a deterministic, perfect-information, two-player
zero-sum game. STS2 is none of those: it is single-agent, stochastic (card
draws, enemy AI rolls) and imperfect-information (draw order is hidden). There
is no opponent to self-play against, so the "self-play" half of AlphaZero has
no analogue.

What DOES transfer is the other half, and it is the half that does the work:
a search and a learned network improving each other in a loop. That is Expert
Iteration (ExIt) / policy-improvement-by-search, and the repo already has every
piece --

  sts2_env/search/combat_mcts.py   determinized PUCT MCTS whose evaluator is
                                   the policy/value net (AlphaZero's exact
                                   arrangement), producing a visit
                                   distribution per decision
  sts2_env/search/distill.py       masked CE(policy -> visit distribution)
                                   + value_coef * MSE(value -> root value):
                                   AlphaZero's loss, term for term
  scripts/exit_distill.py          collect / distill / eval, multiprocess

-- so this script does not reimplement any of it. It chains them into the
ITERATED loop that none of them does on its own, and adds the gates that stop
it burning compute on a loop that is not improving.

One iteration
-------------
  1. GATE   search vs policy on held-out seeds. If search does not beat the
            raw policy, distilling search targets provably cannot help, so the
            loop stops rather than continuing on faith. This is the same gate
            exit_distill --mode eval implements; it is run FIRST because a
            previous run of it returned a hard NO-GO.
  2. COLLECT play with the current policy; at each non-forced combat decision
            run MCTS and record (obs, mask, visit distribution, root value).
  3. DISTIL  train the net on those targets -> distilled.zip
  4. RL      continue PPO from the distilled weights (train_hierarchical
            --init-from), so search targets are a warm start rather than the
            whole training signal.
  5. EVAL    measure, append to the ledger, and feed the new checkpoint into
            iteration n+1.

Read the GATE result before trusting anything downstream of it. A previous
NO-GO for this search was traced to a suspicious pattern -- zero action
overrides yet wildly different outcomes -- which is why
scripts/mcts_purity_check.py exists; run that first if the gate fails again.
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

REPO = Path(__file__).resolve().parent.parent
PY = str(REPO / ".venv" / "bin" / "python")
if not Path(PY).exists():
    PY = str(REPO / ".venv" / "Scripts" / "python.exe")
if not Path(PY).exists():
    PY = sys.executable


def run(cmd: list[str], log: Path) -> int:
    log.parent.mkdir(parents=True, exist_ok=True)
    print(f"  $ {' '.join(str(c) for c in cmd)}", flush=True)
    with log.open("a", encoding="utf-8") as fh:
        fh.write(f"\n=== {' '.join(str(c) for c in cmd)} ===\n")
        fh.flush()
        return subprocess.run(cmd, stdout=fh, stderr=subprocess.STDOUT,
                              cwd=str(REPO)).returncode


def pick_model(d: Path) -> str | None:
    for name in ("best_model.zip", "final_model.zip", "distilled.zip"):
        if (d / name).exists():
            return str(d / name)
    return None


def read_json(p: Path):
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return None


def parse_gate(log: Path) -> dict:
    """Extract the policy-vs-search comparison from an eval log.

    Kept tolerant on purpose: the eval prints per-seed lines and a table, and a
    format change should degrade to "unknown" rather than crash a multi-hour
    curriculum.
    """
    out = {"policy_floors": [], "search_floors": []}
    if not log.exists():
        return out
    import re
    rx = re.compile(r"policy floor (\d+).*?mcts floor (\d+)")
    for line in log.read_text(encoding="utf-8", errors="ignore").splitlines():
        m = rx.search(line)
        if m:
            out["policy_floors"].append(int(m.group(1)))
            out["search_floors"].append(int(m.group(2)))
    if out["policy_floors"]:
        import numpy as np
        out["policy_mean"] = float(np.mean(out["policy_floors"]))
        out["search_mean"] = float(np.mean(out["search_floors"]))
        out["delta"] = out["search_mean"] - out["policy_mean"]
        out["n"] = len(out["policy_floors"])
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--checkpoint", required=True,
                    help="Starting MaskablePPO zip (the policy/value net)")
    ap.add_argument("--iterations", type=int, default=4)
    ap.add_argument("--out-root", default="output/alphazero")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1,
                    help="1 = the current goal, beat act 1")
    ap.add_argument("--sims", type=int, default=64)
    ap.add_argument("--determinizations", type=int, default=8)
    ap.add_argument("--decisions", type=int, default=20000,
                    help="Search-labelled decisions collected per iteration")
    ap.add_argument("--gate-episodes", type=int, default=24)
    ap.add_argument("--collect-minutes", type=float, default=90.0)
    ap.add_argument("--rl-steps", type=int, default=1_000_000,
                    help="PPO steps after each distillation (0 to skip)")
    ap.add_argument("--workers", type=int, default=10)
    ap.add_argument("--device", default="cuda")
    ap.add_argument("--skip-gate", action="store_true",
                    help="Run the loop even if search does not beat the policy. "
                         "Only with a reason -- the gate exists because a "
                         "previous NO-GO was recorded for this search.")
    args = ap.parse_args()

    root = REPO / args.out_root
    root.mkdir(parents=True, exist_ok=True)
    ledger = root / "iterations.jsonl"
    ckpt = args.checkpoint

    print("=" * 74)
    print("  AlphaZero-style curriculum (Expert Iteration)")
    print(f"  start checkpoint : {ckpt}")
    print(f"  iterations       : {args.iterations}")
    print(f"  goal             : beat act {args.max_act_count}, ascension {args.ascension}")
    print(f"  search           : {args.sims} sims x {args.determinizations} determinizations")
    print("=" * 74, flush=True)

    for it in range(1, args.iterations + 1):
        t0 = time.time()
        idir = root / f"iter{it}"
        idir.mkdir(parents=True, exist_ok=True)
        print(f"\n{'='*74}\nITERATION {it}/{args.iterations}\n{'='*74}", flush=True)
        rec: dict = {"iteration": it, "checkpoint_in": ckpt}

        # ---- 1. gate: does search beat the raw policy? ----
        print("[1/5] gate: search vs policy", flush=True)
        gate_log = root / f"iter{it}_gate.log"
        rc = run([PY, "scripts/exit_distill.py", "--mode", "eval",
                  "--checkpoint", ckpt, "--out-dir", str(idir),
                  "--episodes", str(args.gate_episodes),
                  "--ascension", str(args.ascension),
                  "--max-act-count", str(args.max_act_count),
                  "--sims", str(args.sims),
                  "--determinizations", str(args.determinizations),
                  "--workers", str(args.workers),
                  "--device", args.device], gate_log)
        gate = parse_gate(gate_log)
        rec["gate"] = gate
        rec["gate_rc"] = rc
        if "delta" in gate:
            print(f"      policy {gate['policy_mean']:.2f} floors | "
                  f"search {gate['search_mean']:.2f} | delta {gate['delta']:+.2f} "
                  f"(n={gate['n']})", flush=True)
            if gate["delta"] <= 0 and not args.skip_gate:
                rec["stopped"] = "GATE FAILED: search does not beat the policy"
                with ledger.open("a", encoding="utf-8") as fh:
                    fh.write(json.dumps(rec) + "\n")
                print("\n  STOPPING. Search does not beat the raw policy, so its "
                      "visit\n  distributions are not an improvement target and "
                      "distilling them\n  cannot help. Run "
                      "scripts/mcts_purity_check.py before concluding the\n  "
                      "search is merely weak -- a previous NO-GO here was "
                      "accompanied by\n  zero action overrides, which points at "
                      "corruption, not weakness.", flush=True)
                return 3
        else:
            print("      gate result unparseable; continuing (see log)", flush=True)

        # ---- 2 + 3. collect search targets, then distil ----
        print("[2/5] collect search-labelled decisions", flush=True)
        col_log = root / f"iter{it}_collect.log"
        rc = run([PY, "scripts/exit_distill.py", "--mode", "all",
                  "--checkpoint", ckpt, "--out-dir", str(idir),
                  "--decisions", str(args.decisions),
                  "--ascension", str(args.ascension),
                  "--max-act-count", str(args.max_act_count),
                  "--sims", str(args.sims),
                  "--determinizations", str(args.determinizations),
                  "--workers", str(args.workers),
                  "--max-minutes", str(args.collect_minutes),
                  "--device", args.device], col_log)
        rec["collect_distill_rc"] = rc
        distilled = idir / "distilled.zip"
        if rc != 0 or not distilled.exists():
            rec["stopped"] = f"collect/distill failed (rc={rc})"
            with ledger.open("a", encoding="utf-8") as fh:
                fh.write(json.dumps(rec) + "\n")
            print(f"  collect/distill FAILED (rc={rc}); see {col_log}", flush=True)
            return 4
        print(f"[3/5] distilled -> {distilled}", flush=True)
        rec["distilled"] = str(distilled)

        # ---- 4. continue RL from the distilled weights ----
        next_ckpt = str(distilled)
        if args.rl_steps > 0:
            print(f"[4/5] PPO {args.rl_steps:,} steps from distilled weights",
                  flush=True)
            rl_dir = idir / "rl"
            rl_log = root / f"iter{it}_rl.log"
            rc = run([PY, "scripts/train_hierarchical.py", "--phase", "combat",
                      "--init-from", str(distilled),
                      "--total-steps", str(args.rl_steps),
                      "--n-envs", str(args.workers),
                      "--ascension", str(args.ascension),
                      "--eval-freq", str(max(50_000, args.rl_steps // 4)),
                      "--eval-episodes", "200",
                      "--device", args.device,
                      "--output-dir", str(rl_dir)], rl_log)
            rec["rl_rc"] = rc
            got = pick_model(rl_dir)
            if rc == 0 and got:
                next_ckpt = got
            else:
                print("      PPO stage produced no model; carrying the distilled "
                      "weights forward instead", flush=True)
            hist = rl_dir / "eval_history.jsonl"
            if hist.exists():
                rows = [json.loads(l) for l in hist.read_text().splitlines() if l.strip()]
                if rows:
                    rec["rl_eval"] = rows[-1]
        else:
            print("[4/5] PPO stage skipped (--rl-steps 0)", flush=True)

        # ---- 5. record ----
        rec["checkpoint_out"] = next_ckpt
        rec["wall_s"] = round(time.time() - t0, 1)
        with ledger.open("a", encoding="utf-8") as fh:
            fh.write(json.dumps(rec) + "\n")
        ev = rec.get("rl_eval") or {}
        print(f"[5/5] iteration {it} done in {rec['wall_s']/3600:.2f}h  "
              f"win_rate {ev.get('win_rate', float('nan')):.3f}  "
              f"-> {next_ckpt}", flush=True)
        ckpt = next_ckpt

    print(f"\ncurriculum complete -> {ledger}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
