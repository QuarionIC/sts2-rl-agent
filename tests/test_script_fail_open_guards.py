"""Scripts whose checks must not resolve to "fine" when they did not run.

Why this file exists
--------------------
An audit for instruments that cannot distinguish "passed" from "did not run"
confirmed eleven cases. Three are here, each one a check whose every failure
mode used to mean GO:

* ``kill_runners.find_runners`` kept only ``.stdout``, so a failed process
  enumeration and a clean machine were the same empty string -- and the same
  function is the post-kill verifier, so a failure there printed
  "all N runner(s) gone". Two earlier generations of this bug each cost
  multiple sessions, and the file's own docstring states the rule: a kill tool
  that reports success having killed nothing is worse than no tool at all.
  Until now nothing tested it.

* ``alphazero_curriculum`` recorded the gate subprocess's return code and never
  checked it, and printed "gate result unparseable; continuing" -- so a crashed
  eval, a renamed print, or a changed format all fell through into a ~90-minute
  collect and a 1M-step PPO stage. The gate exists precisely to prevent that
  burn.

* ``eval_llm_full`` hardcoded ``"combat_played_by": "LLM"`` into the results
  JSON while ``--combat-policy`` accepts planner and random. Provenance that is
  a constant is not provenance, and these files are what a published number
  would be traced back to.
"""

from __future__ import annotations

import ast
import json
import subprocess
from pathlib import Path

import pytest


REPO_ROOT = Path(__file__).resolve().parents[1]


class TestKillRunnersEnumeration:
    def test_a_failed_enumeration_raises_instead_of_reporting_clean(
            self, monkeypatch):
        """The whole point: silence must not read as "no runners"."""
        from scripts import kill_runners

        class _Failed:
            returncode = 1
            stdout = ""
            stderr = "Get-CimInstance : provider load failure"

        monkeypatch.setattr(kill_runners.subprocess, "run",
                            lambda *a, **k: _Failed())
        with pytest.raises(kill_runners.EnumerationFailed):
            kill_runners.find_runners()

    def test_missing_sentinel_raises_even_on_exit_zero(self, monkeypatch):
        """A PowerShell parse error can print nothing and still exit 0."""
        from scripts import kill_runners

        class _Quiet:
            returncode = 0
            stdout = ""
            stderr = ""

        monkeypatch.setattr(kill_runners.subprocess, "run",
                            lambda *a, **k: _Quiet())
        with pytest.raises(kill_runners.EnumerationFailed):
            kill_runners.find_runners()

    def test_a_genuinely_clean_machine_returns_empty(self, monkeypatch):
        """The other half: a real "nothing running" must NOT raise."""
        from scripts import kill_runners

        class _Clean:
            returncode = 0
            stdout = kill_runners._ENUM_OK + "\n"
            stderr = ""

        monkeypatch.setattr(kill_runners.subprocess, "run",
                            lambda *a, **k: _Clean())
        assert kill_runners.find_runners() == []

    def test_a_running_runner_is_found(self, monkeypatch):
        from scripts import kill_runners

        row = ("4242\tpython.exe\tC:/py.exe -u -m "
               "sts2_env.bridge.agent_runner --combat-policy planner")

        class _One:
            returncode = 0
            stdout = row + "\n" + kill_runners._ENUM_OK + "\n"
            stderr = ""

        monkeypatch.setattr(kill_runners.subprocess, "run",
                            lambda *a, **k: _One())
        found = kill_runners.find_runners()
        assert [pid for pid, _ in found] == [4242]

    def test_an_unknown_flag_is_rejected_not_ignored(self):
        """A destructive tool must never treat an unrecognised flag as 'go'.

        main() took no arguments and had no parser, so any flag was silently
        dropped and the script killed unconditionally. On 2026-08-01 a health
        monitor was very nearly armed with ``kill_runners.py --dry-run``, a
        flag that did not exist, which would have killed the live runner every
        thirty minutes while reading as a read-only probe. The check reached
        for to make a dangerous tool safe must not itself be the dangerous
        call.
        """
        from scripts import kill_runners

        with pytest.raises(SystemExit) as exc:
            kill_runners.main(["--bogus"])
        assert exc.value.code == 2

    def test_dry_run_kills_nothing(self, monkeypatch, capsys):
        from scripts import kill_runners

        monkeypatch.setattr(kill_runners, "find_runners",
                            lambda: [(4242, "python.exe ... agent_runner")])

        def _boom(*a, **k):
            raise AssertionError("--dry-run executed a kill")

        monkeypatch.setattr(kill_runners.subprocess, "run", _boom)

        rc = kill_runners.main(["--dry-run"])
        out = capsys.readouterr().out
        assert "would kill pid 4242" in out
        assert "nothing killed" in out
        # Non-zero because runners ARE alive -- callers probe this to decide
        # whether it is safe to launch.
        assert rc == 1

    def test_dry_run_exit_zero_when_nothing_is_running(self, monkeypatch,
                                                       capsys):
        from scripts import kill_runners

        monkeypatch.setattr(kill_runners, "find_runners", lambda: [])
        assert kill_runners.main(["--dry-run"]) == 0

    def test_the_sentinel_is_emitted_by_the_query(self):
        """The Python side is useless if the query never prints the marker."""
        from scripts import kill_runners

        assert kill_runners._ENUM_OK in kill_runners._PS_ENUMERATE
        assert "-ErrorAction Stop" in kill_runners._PS_ENUMERATE, (
            "a WMI failure must terminate rather than fall through to the "
            "sentinel, which would make a broken query look clean"
        )


class TestAlphazeroGate:
    """Checked by parsing the source: running the real thing costs hours."""

    @staticmethod
    def _main_source() -> str:
        src = (REPO_ROOT / "scripts" / "alphazero_curriculum.py").read_text(
            encoding="utf-8")
        tree = ast.parse(src)
        for node in tree.body:
            if isinstance(node, ast.FunctionDef) and node.name == "main":
                return ast.get_source_segment(src, node) or ""
        raise AssertionError("main() not found")

    def test_an_unparseable_gate_stops_the_curriculum(self):
        body = self._main_source()
        assert "gate result unparseable; continuing" not in body, (
            "the gate still falls through to a multi-hour collect when it "
            "cannot read a delta; every failure mode resolves to GO"
        )
        assert "GATE UNREADABLE" in body

    def test_the_unreadable_branch_returns_nonzero(self):
        """Stopping is only real if the process actually exits."""
        body = self._main_source()
        idx = body.index("GATE UNREADABLE")
        assert "return 4" in body[idx:idx + 1200], (
            "the unreadable-gate branch does not return a failure code"
        )

    def test_the_scraped_gate_log_is_truncated_not_appended(self):
        """Otherwise a re-run can report the PREVIOUS run's verdict."""
        body = self._main_source()
        assert "gate_log, truncate=True" in body, (
            "the gate log is still opened in append mode, so parse_gate can "
            "scrape a stale GO from an earlier attempt"
        )

    def test_run_helper_supports_truncation(self):
        from scripts.alphazero_curriculum import run
        import inspect

        assert "truncate" in inspect.signature(run).parameters


class TestEvalProvenance:
    def test_combat_played_by_is_not_a_constant(self):
        src = (REPO_ROOT / "scripts" / "eval_llm_full.py").read_text(
            encoding="utf-8")
        assert '"combat_played_by": "LLM"' not in src, (
            "the results JSON hardcodes the LLM as the combat player even for "
            "--combat-policy planner"
        )
        assert '"combat_played_by": args.combat_policy' in src
        assert '"noncombat_played_by": args.run_policy' in src

    def test_the_header_is_not_hardcoded(self):
        src = (REPO_ROOT / "scripts" / "eval_llm_full.py").read_text(
            encoding="utf-8")
        # The literal banner that printed even when no LLM ran.
        assert '"\\n=== LLM PLAYING EVERYTHING' not in src
        assert "PLAYING EVERYTHING" in src, "the arms-equal case still reports"
