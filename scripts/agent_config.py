#!/usr/bin/env python3
"""Configure which character and ascension the agent plays in the real game.

The mod used to hardcode Necrobinder / Ascension 10 as constants in
``RlAutoSlayer.cs``, so changing either meant editing C# and rebuilding. It
now reads ``sts2_agent_config.txt`` at character-select time, which this
script writes.

    python scripts/agent_config.py                      # show current
    python scripts/agent_config.py --character Ironclad
    python scripts/agent_config.py --ascension 0
    python scripts/agent_config.py --character Ironclad --ascension 0

Defaults if the file is missing are Ironclad / A0, matching the current
campaign target. The mod falls back to those on any read or parse failure,
so a malformed config can never stop a run from starting.

The value is matched case-insensitively against the character id shown in
the select screen, so "ironclad" works.
"""

from __future__ import annotations

import argparse
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
FILE_NAME = "sts2_agent_config.txt"
#: Written beside the mod so the game finds it with no path setting, and to
#: the deployed mod folder so it takes effect without a redeploy.
TARGET_DIRS = [
    REPO / "bridge_mod",
    Path("C:/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2BridgeMod"),
]

#: Characters the simulator implements. Guarding here gives a clear error
#: instead of the mod silently falling back at character select.
KNOWN_CHARACTERS = ("Ironclad", "Silent", "Defect", "Necrobinder", "Watcher")

DEFAULTS = {"character": "Ironclad", "ascension": "0"}


def read_config() -> dict[str, str]:
    cfg = dict(DEFAULTS)
    for d in TARGET_DIRS:
        f = d / FILE_NAME
        if not f.exists():
            continue
        for line in f.read_text(encoding="utf-8").splitlines():
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, v = line.split("=", 1)
            if v.strip():
                cfg[k.strip().lower()] = v.strip()
        break
    return cfg


def write_config(cfg: dict[str, str]) -> list[Path]:
    body = (
        "# Written by scripts/agent_config.py -- read by RlAutoSlayer at\n"
        "# character select. Missing/malformed values fall back to the mod's\n"
        "# defaults (Ironclad / ascension 0).\n"
        f"character={cfg['character']}\n"
        f"ascension={cfg['ascension']}\n"
    )
    written = []
    for d in TARGET_DIRS:
        if not d.is_dir():
            continue
        try:
            (d / FILE_NAME).write_text(body, encoding="utf-8")
            written.append(d / FILE_NAME)
        except OSError:
            pass
    return written


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--character", default=None,
                    help=f"One of: {', '.join(KNOWN_CHARACTERS)}")
    ap.add_argument("--ascension", type=int, default=None, help="0-10")
    args = ap.parse_args()

    cfg = read_config()
    if args.character is None and args.ascension is None:
        print(f"character : {cfg['character']}")
        print(f"ascension : {cfg['ascension']}")
        for d in TARGET_DIRS:
            f = d / FILE_NAME
            print(f"  {f}{'' if f.exists() else '  (absent -> defaults)'}")
        return 0

    if args.character is not None:
        match = next((c for c in KNOWN_CHARACTERS
                      if c.lower() == args.character.lower()), None)
        if match is None:
            print(f"Unknown character {args.character!r}. "
                  f"Known: {', '.join(KNOWN_CHARACTERS)}")
            return 2
        cfg["character"] = match
    if args.ascension is not None:
        if not 0 <= args.ascension <= 10:
            print("ascension must be 0-10")
            return 2
        cfg["ascension"] = str(args.ascension)

    written = write_config(cfg)
    print(f"character : {cfg['character']}")
    print(f"ascension : {cfg['ascension']}")
    for w in written:
        print(f"  wrote {w}")
    if not written:
        print("  WARNING: no target directory was writable; nothing took effect.")
    print("\nApplies to the NEXT run the agent starts (read at character select).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
