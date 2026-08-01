#!/usr/bin/env python3
"""Health of the CURRENT bridge session, scoped so old logs cannot leak in.

Why this exists
---------------
The overnight monitor grepped ``output/overnight/*.log`` as one corpus while
its header named a single session. Anything ever logged therefore read as
current. Measured 2026-08-01: every notification for five hours reported
"5 unrecognised (e.g. ACTSFROMTHEPAST-SPORE_CLOUD_POWER)" against the live
session. All five were in a log from 23:42 the previous night, written BEFORE
the fix landed at 00:44; the live session had zero. The operator (me) chased a
resolved bug repeatedly because the report said it was still happening.

A stale finding presented as a live one is worse than no monitoring: it spends
attention on something already fixed, and it erodes trust in the counts that
ARE current.

So this refuses to blend sessions. Every number is attributed to exactly one
log file. Findings from older logs are reported only under an explicit
``--history`` flag, and are labelled with the file and its age so they can
never be mistaken for live.

Usage
-----
    python -m scripts.session_health                  # newest session only
    python -m scripts.session_health --history        # + older, clearly marked
    python -m scripts.session_health --log PATH       # a specific session
"""

from __future__ import annotations

import argparse
import re
import sys
from datetime import datetime
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

LOG_DIR = Path(__file__).resolve().parents[1] / "output" / "overnight"

#: session_<YYYYmmdd>_<HHMMSS>_c<cycle>.log
_STAMP = re.compile(r"session_(\d{8})_(\d{6})_c\d+\.log$")

#: Every pattern is pinned to a real captured log line by
#: tests/test_session_health.py.
#:
#: The first draft of this file counted runs with ``run_ended|RUN OVER|run
#: ended`` -- none of which the runner ever emits; it logs "Run finished". The
#: counter read 0 while the session had finished 17 runs, and nothing about a
#: zero looks wrong. That is the exact defect this script exists to stop being
#: reported, reproduced inside the script itself on the first attempt, which is
#: how easily it happens: a monitoring regex is never exercised by the code it
#: watches, so only a test against real output can hold it honest.
PATTERNS = {
    "combat actions": re.compile(r"COMBAT \[HP:"),
    "runs finished": re.compile(r"Run finished"),
    "plan divergences": re.compile(r"plan diverged"),
    "combat declines": re.compile(r"combat planner declining"),
    "unresolved ids": re.compile(r"unrecognised|unresolved"),
    "tracebacks": re.compile(r"Traceback \(most recent call last\)"),
}


def _started_at(path: Path) -> datetime | None:
    m = _STAMP.search(path.name)
    if not m:
        return None
    try:
        return datetime.strptime(m.group(1) + m.group(2), "%Y%m%d%H%M%S")
    except ValueError:
        return None


def scan(path: Path) -> dict[str, int]:
    text = path.read_text(errors="replace")
    return {name: len(rx.findall(text)) for name, rx in PATTERNS.items()}


def _report(path: Path, now: datetime, live: bool) -> dict[str, int]:
    started = _started_at(path)
    age = "" if started is None else f"  (started {started:%H:%M:%S}, " \
                                     f"{(now - started).total_seconds() / 3600:.1f}h ago)"
    print(f"\n{'LIVE SESSION' if live else 'HISTORIC'}: {path.name}{age}")
    counts = scan(path)
    for name, n in counts.items():
        flag = ""
        if n and name in ("combat declines", "tracebacks"):
            flag = "   <-- investigate"
        print(f"    {name:18} {n:6d}{flag}")
    acts = counts["combat actions"]
    if acts:
        div = counts["plan divergences"]
        print(f"    {'divergence rate':18} {100 * div / acts:5.2f}%  "
              f"({div}/{acts})")
    return counts


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--log", type=Path, default=None,
                    help="a specific session log (default: newest)")
    ap.add_argument("--history", action="store_true",
                    help="also show older sessions, explicitly labelled")
    ap.add_argument("--history-limit", type=int, default=5)
    args = ap.parse_args(argv)

    logs = sorted(LOG_DIR.glob("session_*.log"), key=lambda p: p.name)
    if not logs:
        print(f"no session logs in {LOG_DIR}")
        return 1

    now = datetime.now()
    current = args.log if args.log else logs[-1]
    if not current.exists():
        print(f"no such log: {current}")
        return 1

    counts = _report(current, now, live=True)

    if args.history:
        older = [p for p in logs if p != current][-args.history_limit:]
        if older:
            print(f"\n--- {len(older)} EARLIER session(s). These are NOT live; "
                  f"findings here may already be fixed. ---")
            for path in reversed(older):
                _report(path, now, live=False)

    # The exit code reflects the LIVE session only, for the same reason the
    # report is scoped: a monitor that alarms on history never clears.
    return 2 if (counts["tracebacks"] or counts["combat declines"]) else 0


if __name__ == "__main__":
    sys.exit(main())
