#!/usr/bin/env python3
"""Kill every live agent_runner process, whatever the interpreter is called.

Why this exists
---------------
The Microsoft Store Python reports itself as ``python3.13.exe``, not
``python.exe``. Filtering ``wmic process where name='python.exe'`` therefore
misses the runner entirely, and the kill silently succeeds having killed
nothing.

The failure mode that causes is nasty and non-obvious: the OLD runner keeps
its bridge connection, the mod serves whichever client connected first, and
the newly-launched runner sits printing "Waiting for game state..." forever
while the game plays on under the stale code. It reads as "the bridge is
broken" rather than "you have two runners". It cost three sessions on
2026-07-30 and had already been recorded as a project gotcha before it bit
again.

Match on the COMMAND LINE, which is the only stable identifier here.
"""
from __future__ import annotations

import subprocess
import sys

NEEDLE = "sts2_env.bridge.agent_runner"

# Shells that merely LAUNCHED a runner carry the module name in their own
# command line, so a bare substring match kills the calling shell too. Only
# python interpreters are ever the runner itself.
EXCLUDE = ("bash.exe", "cmd.exe", "powershell", "kill_runners")


def find_runners() -> list[tuple[int, str]]:
    out = subprocess.run(
        ["wmic", "process", "get", "ProcessId,CommandLine", "/format:csv"],
        capture_output=True, text=True,
    ).stdout
    found: list[tuple[int, str]] = []
    for line in out.splitlines():
        if NEEDLE not in line or "wmic" in line:
            continue
        if any(token in line for token in EXCLUDE):
            continue
        pid_text = line.rsplit(",", 1)[-1].strip()
        if not pid_text.isdigit():
            continue
        found.append((int(pid_text), line.strip()[:110]))
    return found


def main() -> int:
    runners = find_runners()
    if not runners:
        print("no agent_runner processes found")
        return 0
    for pid, cmd in runners:
        result = subprocess.run(["taskkill", "/F", "/PID", str(pid)],
                                capture_output=True, text=True)
        status = "killed" if result.returncode == 0 else "FAILED"
        print(f"{status} pid {pid}: {cmd}")

    remaining = find_runners()
    if remaining:
        print(f"WARNING: {len(remaining)} runner(s) still alive: "
              f"{[pid for pid, _ in remaining]}")
        return 1
    print(f"all {len(runners)} runner(s) gone")
    return 0


if __name__ == "__main__":
    sys.exit(main())
