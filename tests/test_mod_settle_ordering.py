"""The settle handshake's reference fingerprint must predate the enqueue.

Why this test exists
--------------------
``WaitForActionToSettleAsync`` waits for two things in order: that the action
has become VISIBLE, then that the state has stopped changing. The first check
compares the current fingerprint against a reference one. If that reference is
sampled after ``RequestEnqueue``, an action that lands before the first poll is
already baked into it, the visibility check can never fire, and the loop runs
the whole ``ActionSettleTimeoutMs``.

That is not a subtle slowdown. Measured 2026-08-01 over 1804 live combat
actions, the inter-action gap was bimodal -- 451 at 0-1s, 1015 on a single 5s
spike, and 19 in the whole 2s-4s range. 74% of actions hit the timeout, costing
2.1 hours of a 2.5-hour session, and the effect was exactly inverted from the
intent: FAST actions timed out while slow ones settled in 150ms.

The C# is not exercised by this suite, so the invariant is checked as an
ordering property of the source. Crude, but it is the difference between the
bug being caught here and being caught after another night of quarter-speed
data.
"""

from __future__ import annotations

import re
from pathlib import Path

import pytest


HANDLER = (Path(__file__).resolve().parents[1] / "bridge_mod"
           / "RlCombatHandler.cs")

pytestmark = pytest.mark.skipif(
    not HANDLER.is_file(), reason=f"mod source not present at {HANDLER}")


def _source() -> str:
    return HANDLER.read_text(encoding="utf-8", errors="replace")


def _lines() -> list[str]:
    return _source().splitlines()


def _line_of(pattern: str, lines: list[str], after: int = 0) -> int:
    """First 1-indexed line at or after ``after`` containing ``pattern``.

    Line numbers rather than character offsets into a method "body". The first
    version of this file sliced the source between a method-name match and the
    next ``private static``, which anchored on whichever textual mention came
    first -- a call site or a doc comment, not necessarily the definition -- and
    reported "UsePotionAndWaitAsync never enqueues" about a method whose
    enqueue is plainly there. Guessing at structure to test an ordering
    property is how a guard ends up testing its own parser.
    """
    for i, line in enumerate(lines[after:], start=after + 1):
        if pattern in line:
            return i
    return -1


def test_the_settle_wait_takes_the_reference_fingerprint_as_a_parameter():
    """It must not sample its own reference -- it runs after the enqueue."""
    src = _source()
    sig = re.search(
        r"private static async Task WaitForActionToSettleAsync\(([^)]*)\)",
        src, re.S)
    assert sig, "WaitForActionToSettleAsync not found"
    params = sig.group(1)
    assert "before" in params, (
        "the settle wait no longer receives the pre-action fingerprint; "
        "sampling it internally makes every fast action wait out the full "
        "timeout, because the visibility check can never fire"
    )


def test_every_enqueue_is_preceded_by_a_fingerprint():
    """Checked over EVERY enqueue, so a third action path cannot slip in.

    Derived from the source rather than from a hardcoded list of method names,
    which is what the first draft got wrong.
    """
    lines = _lines()
    enqueues = [i for i, line in enumerate(lines, start=1)
                if "RequestEnqueue(" in line]
    assert enqueues, "no RequestEnqueue call found; has the mod been rewritten?"

    checked = 0
    for eq in enqueues:
        # Only enqueues whose action is awaited via the settle wait are in
        # scope: some enqueues are fire-and-forget.
        window = "\n".join(lines[eq:eq + 40])
        if "WaitForActionToSettleAsync" not in window:
            continue
        checked += 1
        preceding = "\n".join(lines[max(0, eq - 12):eq])
        assert "StateFingerprint(player)" in preceding, (
            f"line {eq}: {lines[eq - 1].strip()!r} is awaited via the settle "
            f"wait, but no fingerprint is sampled in the 12 lines before it. "
            f"An action that lands before the first poll is then already "
            f"reflected in the reference, the visibility check can never "
            f"fire, and the wait burns the full ActionSettleTimeoutMs -- "
            f"measured at 74% of all combat actions."
        )
    assert checked >= 2, (
        f"only {checked} settle-guarded enqueue(s) found; expected the card "
        f"and potion paths. A rename would make this test vacuous."
    )


def test_the_settle_call_passes_a_fingerprint_at_every_site():
    src = _source()
    calls = re.findall(r"await WaitForActionToSettleAsync\(([^)]*)\)", src)
    assert calls, "no settle calls found"
    for call in calls:
        args = [a.strip() for a in call.split(",")]
        assert len(args) >= 3, (
            f"settle call `WaitForActionToSettleAsync({call})` omits the "
            f"pre-action fingerprint"
        )


def test_the_timeout_constant_is_still_bounded():
    """A timeout must exist -- the fix is to stop HITTING it, not to remove it.

    Deleting the bound would trade a 4s stall for an unbounded one, which is
    strictly worse: the AutoSlay watchdog would kill the run instead.
    """
    src = _source()
    m = re.search(r"ActionSettleTimeoutMs\s*=\s*(\d+)", src)
    assert m, "the settle timeout was removed entirely"
    assert 500 <= int(m.group(1)) <= 10000, (
        f"implausible settle timeout {m.group(1)}ms"
    )
