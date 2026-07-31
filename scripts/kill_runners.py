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
import time

NEEDLE = "sts2_env.bridge.agent_runner"

# Shells that merely LAUNCHED a runner carry the module name in their own
# command line, so a bare substring match kills the calling shell too. Only
# python interpreters are ever the runner itself.
EXCLUDE = ("bash.exe", "cmd.exe", "powershell", "kill_runners")


#: PowerShell CIM, because wmic's CSV WRAPS long command lines.
#:
#: The runner's command line carries a full --rl-run-model path, which pushes
#: it past wmic's line width; the module name and the PID then land on
#: different output lines and neither matches. 2026-07-31 this script printed
#: "all 3 runner(s) gone" while a python3.13.exe runner was still holding the
#: bridge -- the same silent-success failure the wmic version was written to
#: fix, one layer down. A kill tool that reports success having killed
#: nothing is worse than no tool at all.
_PS_ENUMERATE = (
    "Get-CimInstance Win32_Process | "
    "Where-Object { $_.CommandLine -like '*sts2_env.bridge.agent_runner*' } | "
    "ForEach-Object { \"$($_.ProcessId)`t$($_.Name)`t$($_.CommandLine)\" }"
)


def find_runners() -> list[tuple[int, str]]:
    out = subprocess.run(
        ["powershell", "-NoProfile", "-NonInteractive", "-Command", _PS_ENUMERATE],
        capture_output=True, text=True,
    ).stdout
    found: list[tuple[int, str]] = []
    for line in out.splitlines():
        parts = line.split("\t", 2)
        if len(parts) < 3 or not parts[0].strip().isdigit():
            continue
        pid, name, cmd = int(parts[0]), parts[1].strip(), parts[2]
        if NEEDLE not in cmd:
            continue
        # Only a python interpreter is ever the runner itself; a shell that
        # merely LAUNCHED one carries the module name too, and killing the
        # calling shell would take this script with it.
        if not name.lower().startswith("python"):
            continue
        if any(token in cmd for token in EXCLUDE):
            continue
        found.append((pid, f"{name} {cmd}"[:110]))
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

    # taskkill returns before the process is reaped, so re-check with a short
    # wait rather than once immediately. Reporting success early is how a
    # stale runner keeps the bridge while the new one waits forever.
    for _ in range(10):
        remaining = find_runners()
        if not remaining:
            print(f"all {len(runners)} runner(s) gone")
            return 0
        time.sleep(0.5)

    print(f"WARNING: {len(remaining)} runner(s) STILL ALIVE: "
          f"{[pid for pid, _ in remaining]} -- do NOT launch another runner; "
          f"the mod serves whichever client connected first, so the new one "
          f"would sit waiting forever while the stale one plays.")
    return 1


if __name__ == "__main__":
    sys.exit(main())
