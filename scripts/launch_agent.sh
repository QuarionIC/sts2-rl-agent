#!/usr/bin/env bash
# Arm the mod and launch Slay the Spire 2 so the RL agent plays.
#
# The mod only auto-plays when it finds an "autoslay.arm" file beside itself,
# and it DELETES that file on startup. So:
#
#   * launching the game yourself from Steam  -> you play, agent stays out
#   * running this script                     -> the agent plays one session
#
# It fails closed on purpose. A crash-and-relaunch, or a second launch after
# this one, gets a normal interactive game rather than an agent that quietly
# abandons the run you just started.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_DIR="/c/Program Files (x86)/Steam/steamapps/common/Slay the Spire 2/mods/STS2BridgeMod"
ARM_FILE="$MOD_DIR/autoslay.arm"
PY="$REPO/.venv/Scripts/python.exe"

if [ ! -d "$MOD_DIR" ]; then
    echo "mod directory not found: $MOD_DIR" >&2
    exit 1
fi

# Never leave a stale runner holding the bridge connection: the mod serves
# whichever client connected first, so an old runner silently plays the game
# while the new one waits forever for state that never arrives.
"$PY" "$REPO/scripts/kill_runners.py"

echo "arming: $ARM_FILE"
date > "$ARM_FILE"

echo "launching Slay the Spire 2 via Steam"
cmd //c start "" "steam://rungameid/2868840" >/dev/null 2>&1

for i in $(seq 1 180); do
    if netstat -ano 2>/dev/null | grep -qE ":9002 .*LISTENING"; then
        echo "bridge listening after ${i}s"
        exit 0
    fi
    sleep 1
done

echo "bridge never came up; removing the arm file so the next launch is interactive" >&2
rm -f "$ARM_FILE"
exit 1
