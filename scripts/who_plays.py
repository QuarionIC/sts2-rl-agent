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

#: Where the mod ACTUALLY looks.
#:
#: MainFile.cs:38-44 searches the executing assembly's own directory and
#: AppContext.BaseDirectory -- the DEPLOYED mod folder under Steam. This script
#: previously wrote the flag to REPO/bridge_mod, which the game never reads, so
#: `who_plays.py human` reported success and the agent took the run anyway.
#: A control that silently controls nothing is worse than no control: you stop
#: watching for the thing it claims to prevent.
DEPLOYED_MOD_DIR = Path(
    "C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2"
    "/mods/STS2BridgeMod"
)
DEPLOYED_DLL = DEPLOYED_MOD_DIR / "STS2BridgeMod.dll"

#: The agent's SECOND gate. MainFile.cs:166 requires an autoslay.arm file
#: beside the mod, created only by scripts/launch_agent.sh and consumed on
#: read. Setting the flag to "agent" and launching from Steam therefore does
#: NOT hand over, though this script used to say it would.
ARM_FILE = DEPLOYED_MOD_DIR / "autoslay.arm"

CANDIDATE_DIRS = [DEPLOYED_MOD_DIR] + CANDIDATE_DIRS


def flag_path() -> Path:
    for d in CANDIDATE_DIRS:
        if d.is_dir():
            return d / FLAG_NAME
    return CANDIDATE_DIRS[0] / FLAG_NAME


def mod_honours_flag() -> bool:
    """Whether the DEPLOYED mod honours the flag -- not the source.

    Grepping MainFile.cs answers "has the C# been edited?", which is true the
    instant the file is saved and says nothing about whether the DLL was
    rebuilt and copied. The marker is a plain string literal in the source, so
    it survives into the compiled assembly and can be looked for there.
    """
    try:
        blob = DEPLOYED_DLL.read_bytes()
    except OSError:
        return False
    return MOD_SUPPORT_MARKER.encode("utf-8") in blob \
        or MOD_SUPPORT_MARKER.encode("utf-16-le") in blob


def source_has_marker() -> bool:
    """Whether the SOURCE has the change, regardless of deployment."""
    try:
        return MOD_SUPPORT_MARKER in MOD_SOURCE.read_text(
            encoding="utf-8", errors="ignore")
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
        # Both gates, because the agent plays only if BOTH pass, and reporting
        # the flag alone told you the opposite of what would happen half the
        # time.
        armed = ARM_FILE.exists()
        honoured = mod_honours_flag()
        setting = read_setting()
        print(f"flag file : {p}{'' if p.exists() else '  (absent -> AGENT)'}")
        print(f"flag says : {setting.upper()}")
        print(f"deployed mod honours flag: "
              f"{'yes' if honoured else 'NO -- rebuild + redeploy required'}")
        if not honoured and source_has_marker():
            print("          (MainFile.cs HAS the change; the deployed DLL "
                  "does not. Rebuild and copy it.)")
        print(f"autoslay.arm present: {'yes' if armed else 'no'}"
              f"   (created by scripts/launch_agent.sh, consumed on read)")

        # The resolved outcome, derived from every gate rather than from the
        # flag alone.
        if setting == "human" and honoured:
            verdict = "YOU (flag honoured)"
        elif not armed:
            verdict = "YOU -- no autoslay.arm, so the agent stays out even " \
                      "though the flag says AGENT"
        elif setting == "human" and not honoured:
            verdict = "THE AGENT -- flag says human but the deployed mod " \
                      "does not check it"
        else:
            verdict = "THE AGENT"
        print(f"\non next Steam launch: {verdict}")
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
    elif not ARM_FILE.exists():
        # The claim this replaces was flatly wrong: MainFile.cs:166 also
        # requires autoslay.arm, so setting the flag to "agent" and launching
        # from Steam does NOT hand over.
        print("Flag set to AGENT, but the agent still will NOT play from a")
        print("plain Steam launch: the mod also needs an autoslay.arm file")
        print("beside it (MainFile.cs:166), which scripts/launch_agent.sh")
        print("creates and the mod consumes on read. Launch via that script.")
    else:
        print("The agent will take over on next launch (flag set, "
              "autoslay.arm present).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
