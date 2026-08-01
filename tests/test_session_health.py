"""Every monitoring pattern must match real runner output.

Why this test exists
--------------------
A monitoring regex is never exercised by the code it watches. If the runner
renames a message, the counter silently reads zero, and a zero looks like good
news -- "0 declines", "0 tracebacks" -- so nothing ever draws attention to it.

This is not hypothetical. ``scripts/session_health.py`` was written on
2026-08-01 specifically to stop stale findings being reported as live, and its
FIRST draft counted finished runs with ``run_ended|RUN OVER|run ended``. The
runner emits "Run finished". The counter reported 0 while the session had
finished 17 runs. The bug the script existed to prevent, reproduced inside the
script, on the first attempt.

So each pattern below is pinned against a line copied verbatim from a real
session log, plus a NEGATIVE sample, so a pattern that matches everything fails
just as loudly as one that matches nothing.
"""

from __future__ import annotations

import re

import pytest

from scripts.session_health import PATTERNS, _started_at, scan


#: Copied from real output, NOT written from memory.
#:
#: The first draft of this dict invented plausible-looking lines
#: ("Run finished (floor 9). Runs this session: 4") which pinned the test to
#: fiction -- it would have passed against patterns that never match anything
#: the runner emits. Each entry below was taken from
#: output/overnight/*.log, or, for the decline path, rendered from the
#: logger.error format string at combat_reconstruct.py:957 because no session
#: has declined since the Downfall removal.
REAL_LINES = {
    "combat actions":
        "06:12:44 [__main__] INFO: [f3t2] COMBAT [HP:70/80 E:2] -> "
        "PLAY STRIKE_NECROBINDER (idx=0) -> JAW_WORM (idx=0)",
    "runs finished":
        "23:46:26 [__main__] INFO: Run finished: terminated (run 1 this session)",
    "plan divergences":
        "23:46:18 [__main__] INFO: Combat: plan diverged [CONTENTS (different "
        "cards)] slot 1 holds FISTICUFFS, planned REND",
    "combat declines":
        "23:44:02 [sts2_env.bridge.combat_reconstruct] ERROR: combat planner "
        "declining: 2 unresolvable card id(s) (SIDESTEP, SNAP). The simulator "
        "would hold a different deck than the game, so every simulated draw "
        "would be wrong.",
    "unresolved ids":
        "00:15:56 [sts2_env.bridge.combat_reconstruct] WARNING: enemy "
        "ACTSFROMTHEPAST-SENTRY powers: 1 power id(s) unrecognised (e.g. "
        "METALLICIZE_POWER_A4H) -- the planner will search without them",
    "tracebacks":
        "Traceback (most recent call last):",
}

#: Lines that must NOT match anything -- ordinary chatter from the same logs.
NEGATIVE_LINES = [
    "04:49:54 [__main__] INFO: Connected. Starting agent loop.",
    "04:49:54 [__main__] INFO: Waiting for game state...",
    "04:50:03 [__main__] INFO: RUN-RL (event): action 145 -> choose([0])",
    "04:49:54 [__main__] INFO: RL run agent loaded (actions=158, obs=4936)",
]


def test_every_pattern_is_pinned():
    """No pattern may exist without a real line proving it matches."""
    assert set(PATTERNS) == set(REAL_LINES), (
        "a pattern was added or renamed without a captured log line to pin it; "
        "an unpinned monitoring regex silently reads zero forever"
    )


@pytest.mark.parametrize("name", sorted(REAL_LINES))
def test_pattern_matches_real_output(name: str):
    assert PATTERNS[name].search(REAL_LINES[name]), (
        f"{name!r} does not match the line the runner actually emits:\n"
        f"  pattern: {PATTERNS[name].pattern}\n"
        f"  line   : {REAL_LINES[name]}\n"
        f"This counter would report 0 regardless of what happened."
    )


@pytest.mark.parametrize("line", NEGATIVE_LINES)
def test_patterns_do_not_match_ordinary_chatter(line: str):
    hits = [n for n, rx in PATTERNS.items() if rx.search(line)]
    assert not hits, f"{hits} matched routine output, inflating the count: {line}"


def test_scan_counts_each_category(tmp_path):
    log = tmp_path / "session_20260801_044925_c3.log"
    body = []
    for name, line in REAL_LINES.items():
        body.extend([line] * (2 if name == "combat actions" else 1))
    body.extend(NEGATIVE_LINES)
    log.write_text("\n".join(body))

    counts = scan(log)
    assert counts["combat actions"] == 2
    assert counts["runs finished"] == 1
    assert counts["tracebacks"] == 1


def test_decline_pattern_matches_the_actual_format_string():
    """Pinned against the source, since no recent session has declined.

    A pattern for a rare event is the easiest kind to get wrong: it reads 0
    whether it is correct or broken, and 0 is the value everyone wants to see.
    So this one is checked against the logger call itself rather than a log.
    """
    from pathlib import Path

    src = (Path(__file__).resolve().parents[1] / "sts2_env" / "bridge" /
           "combat_reconstruct.py").read_text(encoding="utf-8")
    assert "combat planner declining:" in src, (
        "the decline message was renamed; session_health would silently "
        "report 0 declines forever"
    )
    assert PATTERNS["combat declines"].search(
        "combat planner declining: 2 unresolvable card id(s)")


def test_unresolved_powers_are_not_counted_as_declines():
    """Two different severities that must not be conflated.

    An unrecognised POWER degrades gracefully -- "the planner will search
    without them". An unresolvable CARD id makes reconstruction return None and
    forfeits the whole fight. Reporting them as one number would hide a fight
    being forfeited behind a benign warning.
    """
    power_line = REAL_LINES["unresolved ids"]
    assert not PATTERNS["combat declines"].search(power_line)
    assert PATTERNS["unresolved ids"].search(power_line)

    decline_line = REAL_LINES["combat declines"]
    assert PATTERNS["combat declines"].search(decline_line)


def test_session_timestamp_parses():
    from pathlib import Path

    got = _started_at(Path("session_20260801_044925_c3.log"))
    assert got is not None and (got.hour, got.minute, got.second) == (4, 49, 25)


def test_unparseable_name_does_not_crash():
    # Diagnostics must never be the thing that fails.
    from pathlib import Path

    assert _started_at(Path("not-a-session.log")) is None
    assert _started_at(Path("session_BAD_BAD_c1.log")) is None
