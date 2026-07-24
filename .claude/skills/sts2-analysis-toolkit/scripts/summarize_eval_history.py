"""Summarize an eval_history.jsonl produced by scripts/train_necrobinder.py.

Handles both schemas:
  * old (pre-revamp, stages A/B, e.g. output/necrobinder_a10/A/):
    win_rate, episodes, mean_floors, deaths_by_act, steps, wall_s, shaping_scale
  * new (revamp G-stages, commit fe25668+):
    win_rate, episodes, mean_floors, mean_act, truncation_rate, deaths_by_act,
    steps, wall_s

Every win rate is printed with a Wilson 95% CI. The summary verdict says
whether the best/last evals are CI-separated from the first eval (i.e.
whether there is any statistically defensible improvement at all).

Usage (from anywhere; sts2_env not required):
    python summarize_eval_history.py <path-to-eval_history.jsonl> [--last N]
    python summarize_eval_history.py --wilson WINS EPISODES   # ad-hoc CI
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

Z95 = 1.959963984540054  # two-sided 95%


def wilson_ci(wins: float, n: int, z: float = Z95) -> tuple[float, float]:
    """Wilson score interval for a binomial proportion."""
    if n <= 0:
        return (0.0, 1.0)
    p = wins / n
    denom = 1.0 + z * z / n
    center = p + z * z / (2 * n)
    margin = z * math.sqrt(p * (1.0 - p) / n + z * z / (4.0 * n * n))
    return ((center - margin) / denom, (center + margin) / denom)


def fmt_ci(win_rate: float, n: int) -> str:
    lo, hi = wilson_ci(win_rate * n, n)
    return f"[{lo:6.1%}, {hi:6.1%}]"


def overlap(a: tuple[float, float], b: tuple[float, float]) -> bool:
    return a[0] <= b[1] and b[0] <= a[1]


def load_rows(path: Path) -> list[dict]:
    rows = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line:
                rows.append(json.loads(line))
    return rows


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("path", nargs="?", help="eval_history.jsonl (or a stage dir containing one)")
    ap.add_argument("--last", type=int, default=0, help="only show the last N rows (summary uses all)")
    ap.add_argument("--wilson", nargs=2, type=float, metavar=("WINS", "EPISODES"),
                    help="print a Wilson 95%% CI for WINS out of EPISODES and exit")
    args = ap.parse_args()

    if args.wilson:
        wins, n = args.wilson
        lo, hi = wilson_ci(wins, int(n))
        print(f"{wins:.0f}/{n:.0f} = {wins / n:.1%}  Wilson 95% CI [{lo:.1%}, {hi:.1%}] "
              f"(half-width ~{(hi - lo) / 2:.1%})")
        return 0

    if not args.path:
        ap.error("path required unless --wilson is used")
    path = Path(args.path)
    if path.is_dir():
        path = path / "eval_history.jsonl"
    if not path.exists():
        print(f"not found: {path}", file=sys.stderr)
        return 1

    rows = load_rows(path)
    if not rows:
        print(f"empty: {path}", file=sys.stderr)
        return 1

    new_schema = "truncation_rate" in rows[0] or "mean_act" in rows[0]
    print(f"{path}  ({len(rows)} evals, schema: {'revamp' if new_schema else 'pre-revamp'})")
    hdr = f"{'steps':>12} {'win%':>6} {'wilson95':>18} {'floors':>7} {'act':>5} {'trunc%':>7}  deaths_by_act"
    print(hdr)
    print("-" * len(hdr))
    shown = rows[-args.last:] if args.last else rows
    for r in shown:
        n = int(r.get("episodes", 0))
        wr = float(r.get("win_rate", 0.0))
        trunc = r.get("truncation_rate")
        print(f"{r.get('steps', 0):>12,} {wr:>6.1%} {fmt_ci(wr, n):>18} "
              f"{r.get('mean_floors', 0.0):>7.1f} {r.get('mean_act', float('nan')):>5.2f} "
              f"{(f'{trunc:.1%}' if trunc is not None else '  n/a'):>7}  "
              f"{r.get('deaths_by_act', {})}")

    # ---- summary ----
    n0 = int(rows[0]["episodes"])
    first = rows[0]
    last = rows[-1]
    best = max(rows, key=lambda r: r["win_rate"])
    ci_first = wilson_ci(first["win_rate"] * n0, n0)
    ci_best = wilson_ci(best["win_rate"] * int(best["episodes"]), int(best["episodes"]))
    ci_last = wilson_ci(last["win_rate"] * int(last["episodes"]), int(last["episodes"]))
    print("\nsummary:")
    print(f"  first: {first['win_rate']:.1%} @ {first['steps']:,}  CI [{ci_first[0]:.1%}, {ci_first[1]:.1%}]")
    print(f"  best : {best['win_rate']:.1%} @ {best['steps']:,}  CI [{ci_best[0]:.1%}, {ci_best[1]:.1%}]")
    print(f"  last : {last['win_rate']:.1%} @ {last['steps']:,}  CI [{ci_last[0]:.1%}, {ci_last[1]:.1%}]")
    sep_best = not overlap(ci_first, ci_best)
    sep_last = not overlap(ci_first, ci_last)
    print(f"  best vs first CI-separated: {'YES' if sep_best else 'NO'}"
          f"   last vs first CI-separated: {'YES' if sep_last else 'NO'}")
    if not sep_best:
        print("  -> no statistically defensible improvement over the first eval; "
              "suspect a plateau (check optimizer stats in the campaign log "
              "before blaming capability).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
