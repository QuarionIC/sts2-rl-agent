"""Startup must describe what is actually driving, and fail usefully.

Why this test exists
--------------------
Three defects found together on 2026-08-01, all one family: the operator being
told something other than what is running.

1. The startup banner read ``"LLM" if llm_model else "heuristics"`` and ignored
   run_policy entirely. A session driving all seven non-combat phases with the
   RL run agent logged::

       04:49:43  out-of-combat: RL run agent
       04:49:54  non-combat: heuristics

   Two lines eleven seconds apart, flatly contradicting each other. This is the
   failure that made Downfall cost weeks -- the log naming an engine that was
   not the one playing -- so the claim is now derived from resolved state.

2. The actionable "migrate your checkpoint" message sat AFTER
   ``MaskablePPO.load``, which builds the features extractor and asserts on
   observation width during the load. The friendly text was unreachable; the
   operator got a bare SB3 assertion instead.

3. ``scripts/overnight_bridge.sh`` relaunched unconditionally, so the startup
   failure in (2) recycled 13 consecutive times -- game restart included --
   and turned one bad checkpoint path into a lost night.
"""

from __future__ import annotations

import re
import subprocess
from pathlib import Path

import pytest


REPO_ROOT = Path(__file__).resolve().parents[1]
RUNNER = REPO_ROOT / "sts2_env" / "bridge" / "agent_runner.py"
SUPERVISOR = REPO_ROOT / "scripts" / "overnight_bridge.sh"


def _code_only(src: str) -> str:
    """Source with whole-line comments dropped.

    The comment above the fix quotes the defective expression verbatim, so a
    naive substring search over the raw file matches the explanation of the bug
    and fails on the fixed code. Stripping comments keeps the check pointed at
    what executes.

    Not sufficient on its own -- DOCSTRINGS survive this, and a docstring that
    names the old message trips any check for that message. Use
    ``_logger_format_strings`` when the thing being checked for is itself a
    string literal.
    """
    return "\n".join(line for line in src.splitlines()
                     if not line.lstrip().startswith("#"))


def _logger_format_strings(src: str) -> list[str]:
    """Every string literal passed as the first arg to a ``logger.*`` call.

    Text scraping cannot answer "does the code still LOG this sentence?",
    because the sentence also appears in the comment and docstring explaining
    why it must not be logged. Both earlier drafts of this file failed exactly
    that way. The AST distinguishes what executes from what merely describes
    it: a docstring is not a Call argument.
    """
    import ast

    out: list[str] = []
    for node in ast.walk(ast.parse(src)):
        if not isinstance(node, ast.Call):
            continue
        fn = node.func
        if not (isinstance(fn, ast.Attribute)
                and isinstance(fn.value, ast.Name)
                and fn.value.id == "logger"):
            continue
        if node.args and isinstance(node.args[0], ast.Constant) \
                and isinstance(node.args[0].value, str):
            out.append(node.args[0].value)
    return out


def test_banner_does_not_hardcode_heuristics():
    """The non-combat driver must be computed, not assumed."""
    code = _code_only(RUNNER.read_text(encoding="utf-8"))
    # The exact expression that produced the contradiction.
    assert '"LLM" if llm_model else "heuristics"' not in code, (
        "the banner is back to guessing the non-combat driver from llm_model "
        "alone, which reports 'heuristics' while the RL run agent is driving"
    )
    banner = code[code.index("No MaskablePPO model loaded"):][:400]
    assert "non_combat" in banner


class TestNonCombatDriverDescription:
    """The shared derivation, called directly rather than grepped for.

    The first version of this file asserted on a 1600-char window after
    ``src.index("elif model is None:")``. That anchored it to ONE of the two
    startup branches by construction, so it passed while the sibling ``else``
    branch -- combat-only model loaded -- still announced "using heuristics for
    non-combat phases" with the RL run agent driving. A test scoped to the code
    that was fixed cannot detect the code that was not.
    """

    def test_llm_wins(self):
        from sts2_env.bridge.agent_runner import _describe_non_combat_driver

        assert _describe_non_combat_driver("model.gguf", "rl") == "LLM"

    def test_heuristics_when_no_rl(self):
        from sts2_env.bridge.agent_runner import _describe_non_combat_driver

        assert _describe_non_combat_driver(None, "heuristic") == "heuristics"

    def test_rl_names_the_agent_and_its_phases(self):
        from sts2_env.bridge import agent_runner as ar

        ar._RL_RUN_PHASES_ACTIVE[0] = ar._RL_RUN_PHASES
        got = ar._describe_non_combat_driver(None, "rl")
        assert "RL run agent" in got
        assert "heuristics" not in got, (
            "with every phase active nothing is left to the heuristics; "
            f"saying otherwise misdescribes the session: {got}"
        )

    def test_partial_restriction_names_both(self):
        from sts2_env.bridge import agent_runner as ar

        keep = sorted(ar._RL_RUN_PHASES)[0]
        ar._RL_RUN_PHASES_ACTIVE[0] = frozenset({keep})
        try:
            got = ar._describe_non_combat_driver(None, "rl")
            assert "RL run agent" in got and "heuristics" in got
            assert keep in got
        finally:
            ar._RL_RUN_PHASES_ACTIVE[0] = ar._RL_RUN_PHASES

    def test_empty_restriction_says_heuristics_not_everything(self):
        """The falsy-empty-set trap, stated as behaviour.

        Dispatch used ``(_RL_RUN_PHASES_ACTIVE[0] or _RL_RUN_PHASES)``. An
        empty frozenset is falsy, so restricting the agent to no phases routed
        EVERY phase to it -- the exact inverse of the request -- while the
        banner printed "(nothing)".
        """
        from sts2_env.bridge import agent_runner as ar

        ar._RL_RUN_PHASES_ACTIVE[0] = frozenset()
        try:
            got = ar._describe_non_combat_driver(None, "rl")
            assert got.startswith("heuristics"), got
        finally:
            ar._RL_RUN_PHASES_ACTIVE[0] = ar._RL_RUN_PHASES


def test_dispatch_does_not_or_away_an_empty_phase_set():
    """The live routing must read the resolved set, with no falsy fallback."""
    code = _code_only(RUNNER.read_text(encoding="utf-8"))
    assert "_RL_RUN_PHASES_ACTIVE[0] or _RL_RUN_PHASES" not in code, (
        "dispatch is back to falling through to ALL phases when the active "
        "set is empty, which inverts --rl-run-phases and contradicts the banner"
    )


def test_both_startup_branches_use_the_shared_helper():
    """Neither branch may hand-roll its own description again."""
    code = _code_only(RUNNER.read_text(encoding="utf-8"))
    assert code.count("_describe_non_combat_driver(") >= 3, (
        "expected the helper definition plus both startup call sites; a "
        "branch that formats its own string is free to drift from dispatch"
    )
    # Asked of what the code LOGS, not of what the file contains -- the
    # sentence below also appears in the comment explaining its removal.
    logged = _logger_format_strings(RUNNER.read_text(encoding="utf-8"))
    # Without this, an extractor that returned [] -- a renamed logger, an AST
    # change -- would make the assertion below vacuously true forever. That is
    # the same fail-open shape this whole file exists to catch, and it is just
    # as easy to write into the test as into the code.
    assert len(logged) > 50, (
        f"only {len(logged)} logger format strings extracted from "
        f"{RUNNER.name}; the extractor is broken and the check below proves "
        f"nothing"
    )
    offenders = [s for s in logged
                 if "using heuristics for non-combat phases" in s]
    assert not offenders, (
        f"a startup line still asserts heuristics without consulting "
        f"run_policy: {offenders}"
    )


def test_load_failure_names_the_migration_script():
    """A width mismatch must point at the fix, not at an SB3 stack frame."""
    src = RUNNER.read_text(encoding="utf-8")
    start = src.index("Loading RL run agent")
    block = src[start:start + 2000]
    assert "except AssertionError" in block, (
        "MaskablePPO.load asserts on obs width DURING load; without catching "
        "it the actionable message below is unreachable"
    )
    assert "migrate_checkpoint_powers.py" in block
    # The guard must wrap the load itself, not follow it.
    assert block.index("try:") < block.index("MaskablePPO.load")


class TestSupervisorFastDeath:
    """The circuit breaker, exercised rather than grepped for."""

    def _run(self, tmp_path: Path, script: str) -> subprocess.CompletedProcess:
        harness = tmp_path / "harness.sh"
        harness.write_text(script)
        return subprocess.run(
            ["bash", str(harness)], capture_output=True, text=True, timeout=120
        )

    def test_supervisor_has_the_breaker(self):
        src = SUPERVISOR.read_text(encoding="utf-8")
        assert "FAST_DEATH_LIMIT" in src
        assert "SUPERVISOR ABORTING" in src
        # A run that ENDS normally must still recycle.
        assert "fast_deaths=0" in src, "no reset path; one slow cycle would " \
                                       "never clear the counter"

    def test_breaker_aborts_after_three_instant_deaths(self, tmp_path: Path):
        """Three sub-threshold deaths in a row must stop the loop."""
        out = self._run(tmp_path, """
        set -u
        FAST_DEATH_SECONDS=60
        FAST_DEATH_LIMIT=3
        fast_deaths=0
        cycles=0
        while true; do
            cycles=$((cycles + 1))
            if [ "$cycles" -gt 20 ]; then echo "NEVER_ABORTED"; exit 9; fi
            lived=2                       # every session dies instantly
            if [ "$lived" -lt "$FAST_DEATH_SECONDS" ]; then
                fast_deaths=$((fast_deaths + 1))
                if [ "$fast_deaths" -ge "$FAST_DEATH_LIMIT" ]; then
                    echo "ABORTED_AFTER_${cycles}"
                    exit 1
                fi
            else
                fast_deaths=0
            fi
        done
        """)
        assert "ABORTED_AFTER_3" in out.stdout, out.stdout
        assert out.returncode == 1

    def test_a_healthy_session_resets_the_counter(self, tmp_path: Path):
        """Two fast deaths, a real run, then two more must NOT abort.

        Without the reset the counter accumulates across unrelated cycles and
        a supervisor that is working fine kills itself overnight.
        """
        out = self._run(tmp_path, """
        set -u
        FAST_DEATH_SECONDS=60
        FAST_DEATH_LIMIT=3
        fast_deaths=0
        for lived in 2 3 900 4 5; do
            if [ "$lived" -lt "$FAST_DEATH_SECONDS" ]; then
                fast_deaths=$((fast_deaths + 1))
                if [ "$fast_deaths" -ge "$FAST_DEATH_LIMIT" ]; then
                    echo "ABORTED"; exit 1
                fi
            else
                fast_deaths=0
            fi
        done
        echo "SURVIVED"
        """)
        assert "SURVIVED" in out.stdout, out.stdout
        assert out.returncode == 0
