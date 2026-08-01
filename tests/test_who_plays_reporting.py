"""``who_plays.py`` must report the outcome, not one of the inputs to it.

Why this test exists
--------------------
This script answers "will the bot take my run?". Three separate claims in it
were derived from something other than what decides that:

1. ``flag_path()`` searched ``REPO/bridge_mod`` and the Godot app_userdata
   directory. The mod (MainFile.cs:38-44) reads the flag from the executing
   assembly's own directory -- the DEPLOYED folder under Steam. So the script
   wrote the flag somewhere the game never looks, printed success, and the
   agent took the run anyway. A control that silently controls nothing is
   worse than no control, because you stop watching for what it claims to
   prevent.

2. ``mod_honours_flag()`` grepped the SOURCE file. That is true the instant
   the C# is edited and says nothing about whether the DLL was rebuilt and
   copied -- the exact window in which the flag does not work.

3. "The agent will take over on next launch" ignored the SECOND gate.
   MainFile.cs:166 also requires an ``autoslay.arm`` file that only
   ``scripts/launch_agent.sh`` creates, so a plain Steam launch does not hand
   over regardless of the flag.
"""

from __future__ import annotations

import pytest

from scripts import who_plays


def test_the_deployed_mod_dir_is_searched_first():
    """The flag must land where the mod reads it."""
    assert who_plays.CANDIDATE_DIRS[0] == who_plays.DEPLOYED_MOD_DIR, (
        "the deployed mod folder is not searched first; the flag would be "
        "written where the game never looks"
    )


def test_the_repo_copy_is_not_the_only_candidate():
    from pathlib import Path

    repo_copy = Path(who_plays.REPO) / "bridge_mod"
    assert who_plays.CANDIDATE_DIRS[0] != repo_copy


def test_honours_flag_reads_the_deployed_dll_not_the_source(monkeypatch,
                                                            tmp_path):
    """A source edit alone must NOT read as 'the mod honours the flag'.

    This is the window the original check got wrong: C# saved, DLL stale.
    """
    fake_dll = tmp_path / "STS2BridgeMod.dll"
    fake_dll.write_bytes(b"\x00\x01 nothing relevant here \x02")
    monkeypatch.setattr(who_plays, "DEPLOYED_DLL", fake_dll)

    assert who_plays.mod_honours_flag() is False, (
        "a DLL without the marker was reported as honouring the flag"
    )


def test_honours_flag_finds_the_marker_in_a_utf16_dll(monkeypatch, tmp_path):
    """.NET stores string literals as UTF-16; a UTF-8-only search misses them."""
    fake_dll = tmp_path / "STS2BridgeMod.dll"
    fake_dll.write_bytes(b"\x00" + who_plays.MOD_SUPPORT_MARKER.encode("utf-16-le"))
    monkeypatch.setattr(who_plays, "DEPLOYED_DLL", fake_dll)

    assert who_plays.mod_honours_flag() is True


def test_missing_dll_is_not_honoured(monkeypatch, tmp_path):
    monkeypatch.setattr(who_plays, "DEPLOYED_DLL", tmp_path / "absent.dll")
    assert who_plays.mod_honours_flag() is False


class TestResolvedVerdict:
    """The printed outcome must account for BOTH gates."""

    def _run(self, capsys, monkeypatch, tmp_path, *, flag: str,
             honoured: bool, armed: bool) -> str:
        flag_file = tmp_path / who_plays.FLAG_NAME
        flag_file.write_text(flag, encoding="utf-8")
        arm = tmp_path / "autoslay.arm"
        if armed:
            arm.write_text("")
        monkeypatch.setattr(who_plays, "flag_path", lambda: flag_file)
        monkeypatch.setattr(who_plays, "ARM_FILE", arm)
        monkeypatch.setattr(who_plays, "mod_honours_flag", lambda: honoured)
        monkeypatch.setattr(who_plays.sys, "argv", ["who_plays.py"])
        who_plays.main()
        return capsys.readouterr().out

    def test_no_arm_file_means_you_play_even_when_flag_says_agent(
            self, capsys, monkeypatch, tmp_path):
        """The case the old message got backwards."""
        out = self._run(capsys, monkeypatch, tmp_path,
                        flag="agent", honoured=True, armed=False)
        assert "next Steam launch: YOU" in out, out
        assert "autoslay.arm" in out

    def test_flag_human_and_honoured_means_you_play(
            self, capsys, monkeypatch, tmp_path):
        out = self._run(capsys, monkeypatch, tmp_path,
                        flag="human", honoured=True, armed=True)
        assert "next Steam launch: YOU" in out, out

    def test_flag_human_but_stale_dll_means_the_agent_still_plays(
            self, capsys, monkeypatch, tmp_path):
        """The dangerous case: you think you took control and did not."""
        out = self._run(capsys, monkeypatch, tmp_path,
                        flag="human", honoured=False, armed=True)
        assert "next Steam launch: THE AGENT" in out, out
        assert "does not check it" in out

    def test_both_gates_pass_means_the_agent_plays(
            self, capsys, monkeypatch, tmp_path):
        out = self._run(capsys, monkeypatch, tmp_path,
                        flag="agent", honoured=True, armed=True)
        assert "next Steam launch: THE AGENT" in out, out
