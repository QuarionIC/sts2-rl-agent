#!/usr/bin/env python3
"""Toggle who plays Slay the Spire 2 when you launch it: you, or the agent.

The mod launches AutoSlay unconditionally at startup (``MainFile.cs``,
"Phase 3"), which is why opening the game currently hands the run straight
to the bot. This writes a flag file the mod checks before doing that, so you
can take the controller back without uninstalling anything.

    python scripts/who_plays.py human    # you play; the bot stays out
    python scripts/who_plays.py agent    # the agent plays on launch
    python scripts/who_plays.py          # show current setting

The flag lives next to the mod so the game can find it with no config
plumbing, and absence of the file means AGENT -- preserving today's
behaviour for anyone who never runs this script.

REQUIRES the matching C# change in MainFile.cs (see docs/WHO_PLAYS.md) and
one mod rebuild. Until that rebuild, this script writes the flag and reports
that the mod is not yet honouring it, rather than pretending it took effect.
"""

from __future__ import annotations

import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
#: Checked by the mod at startup. Kept beside the mod DLL so the game finds
#: it without needing a path setting.
FLAG_NAME = "sts2_who_plays.txt"
CANDIDATE_DIRS = [
    REPO / "bridge_mod",
    Path.home() / "AppData" / "Roaming" / "Godot" / "app_userdata" / "Slay the Spire 2",
]

MOD_SOURCE = REPO / "bridge_mod" / "MainFile.cs"
#: Marker proving the rebuilt mod honours the flag.
MOD_SUPPORT_MARKER = "sts2_who_plays"


def flag_path() -> Path:
    for d in CANDIDATE_DIRS:
        if d.is_dir():
            return d / FLAG_NAME
    return CANDIDATE_DIRS[0] / FLAG_NAME


def mod_honours_flag() -> bool:
    try:
        return MOD_SUPPORT_MARKER in MOD_SOURCE.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return False


def read_setting() -> str:
    p = flag_path()
    if not p.exists():
        return "agent"
    val = p.read_text(encoding="utf-8").strip().lower()
    return "human" if val.startswith("human") else "agent"


def main() -> int:
    args = [a.lower() for a in sys.argv[1:]]
    p = flag_path()

    if not args:
        print(f"who plays : {read_setting().upper()}")
        print(f"flag file : {p}{'' if p.exists() else '  (absent -> AGENT)'}")
        print(f"mod honours flag: {'yes' if mod_honours_flag() else 'NO -- rebuild required'}")
        return 0

    choice = args[0]
    if choice not in ("human", "agent"):
        print("usage: who_plays.py [human|agent]")
        return 2

    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(choice, encoding="utf-8")
    print(f"set to {choice.upper()}  ({p})")

    if not mod_honours_flag():
        print()
        print("NOTE: the deployed mod does not check this flag yet, so the")
        print("      setting will NOT take effect until MainFile.cs is patched")
        print("      (docs/WHO_PLAYS.md) and the mod rebuilt. Nothing changes")
        print("      in-game before then.")
    elif choice == "human":
        print("The agent will stay out of your run on next launch.")
    else:
        print("The agent will take over on next launch.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
