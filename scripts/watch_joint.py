#!/usr/bin/env python3
"""Live status for the joint (alternating) training campaign.

Reads only files the campaign already writes -- the per-round eval histories
and the phase logs -- so it never touches the training processes and is safe
to run at any time, including several copies at once.

    python scripts/watch_joint.py            # one snapshot
    python scripts/watch_joint.py --follow    # refresh until Ctrl-C
"""

from __future__ import annotations

import argparse
import json
import re
import time
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

#: Rate is measured from the phase log rather than assumed, because it swings
#: with memory pressure -- the first campaign attempt ran 43% slower purely
#: from another job competing for RAM.
STEP_RE = re.compile(r"total_timesteps\s*\|\s*(\d+)")
REW_RE = re.compile(r"ep_rew_mean\s*\|\s*(-?[\d.]+)")


def tail_metric(path: Path, rx: re.Pattern, n: int = 1):
    if not path.exists():
        return None
    hits = rx.findall(path.read_text(encoding="utf-8", errors="ignore"))
    return hits[-n] if hits else None


def read_evals(path: Path) -> list[dict]:
    if not path.exists():
        return []
    out = []
    for line in path.read_text(encoding="utf-8", errors="ignore").splitlines():
        line = line.strip()
        if line:
            try:
                out.append(json.loads(line))
            except json.JSONDecodeError:
                pass
    return out


def running() -> bool:
    """True when the campaign driver is still alive."""
    try:
        import subprocess
        r = subprocess.run(
            ["powershell", "-NoProfile", "-Command",
             "@(Get-CimInstance Win32_Process -Filter \"Name like '%python%'\" | "
             "Where-Object {$_.CommandLine -like '*alternate_hierarchical*'}).Count"],
            capture_output=True, text=True, timeout=20)
        return int((r.stdout or "0").strip() or 0) > 0
    except Exception:
        return False


def render(root: Path, total_rounds: int) -> str:
    L = []
    alive = running()
    L.append("=" * 78)
    L.append(f"  JOINT TRAINING  ({root})   "
             f"{'RUNNING' if alive else 'NOT RUNNING'}")
    L.append("=" * 78)

    ledger = read_evals(root / "rounds.jsonl")
    if ledger:
        L.append("")
        L.append("  completed rounds")
        L.append(f"  {'rnd':>3} {'floors':>7} {'win%':>6} {'deck':>6} {'upg':>5} "
                 f"{'combat win%':>12} {'wall':>8}")
        for r in ledger:
            run = r.get("run") or {}
            cb = r.get("combat") or {}
            L.append(f"  {r.get('round', '?'):>3} {run.get('mean_floors', 0):>7.2f} "
                     f"{100 * run.get('win_rate', 0):>5.1f}% "
                     f"{run.get('mean_deck', 0):>6.1f} {run.get('mean_upgrades', 0):>5.2f} "
                     f"{100 * cb.get('win_rate', 0):>11.1f}% "
                     f"{r.get('wall_s', 0) / 3600:>7.1f}h")

    # In-flight phase: highest-numbered round directory with a log.
    for rnd in range(total_rounds, 0, -1):
        for phase in ("run", "combat"):
            log = root / f"r{rnd}_{phase}.log"
            if not log.exists():
                continue
            steps = tail_metric(log, STEP_RE)
            if steps is None:
                continue
            rew = tail_metric(log, REW_RE)
            evals = read_evals(root / f"r{rnd}" / phase / "eval_history.jsonl")
            L.append("")
            L.append(f"  in flight: round {rnd}/{total_rounds}, phase {phase.upper()}"
                     f"   {int(steps):,} steps   ep_rew_mean {rew}")
            if evals:
                L.append("")
                if phase == "run":
                    L.append(f"    {'steps':>10} {'floors':>7} {'win%':>6} "
                             f"{'deck':>6} {'upg':>5} {'decisions':>10}")
                    for e in evals[-12:]:
                        L.append(f"    {e.get('steps', 0):>10,} "
                                 f"{e.get('mean_floors', 0):>7.2f} "
                                 f"{100 * e.get('win_rate', 0):>5.1f}% "
                                 f"{e.get('mean_deck', 0):>6.1f} "
                                 f"{e.get('mean_upgrades', 0):>5.2f} "
                                 f"{e.get('mean_decisions', 0):>10.1f}")
                else:
                    L.append(f"    {'steps':>10} {'win%':>6} {'hp_frac':>8}")
                    for e in evals[-12:]:
                        L.append(f"    {e.get('steps', 0):>10,} "
                                 f"{100 * e.get('win_rate', 0):>5.1f}% "
                                 f"{e.get('mean_hp_frac', 0):>8.3f}")
            L.append("")
            L.append("  win% = fraction of runs that BEAT ACT 1 (the goal).")
            return "\n".join(L)

    L.append("")
    L.append("  no phase log yet -- the campaign is still starting up.")
    return "\n".join(L)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--root", default="output/joint")
    ap.add_argument("--rounds", type=int, default=6)
    ap.add_argument("--follow", action="store_true")
    ap.add_argument("--interval", type=int, default=60)
    args = ap.parse_args()

    root = REPO / args.root
    while True:
        text = render(root, args.rounds)
        if args.follow:
            print("\033[2J\033[H", end="")  # clear + home
        print(text, flush=True)
        if not args.follow:
            return 0
        time.sleep(args.interval)


if __name__ == "__main__":
    raise SystemExit(main())
