#!/usr/bin/env bash
# Keep the live bridge playing indefinitely, restarting whatever falls over.
#
# Why this exists
# ---------------
# The mod plays PreferredRunCount runs (sts2_agent_config.txt, runs=20) and
# then EXITS. The Python runner does not know that: it sits printing "Timeout
# waiting for state. Sending ping..." forever. Measured 2026-07-31, a session
# finished its 20 runs at 21:27 and idled for over two hours before anyone
# noticed -- which reads as "the agent is stuck" and is really "the session
# ended two hours ago".
#
# It also restarts after a crash or a hang, so an unattended run keeps
# producing divergence data rather than stopping at the first problem.
#
# Each cycle writes its own log so a later analysis can tell sessions apart.
#
# Usage:  bash scripts/overnight_bridge.sh [LOG_DIR]
set -uo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="${1:-$REPO/output/overnight}"
PY="$REPO/.venv/Scripts/python.exe"
MODEL="output/joint_alt_offerobs/r2/run/r1_run_best_v590.zip"

#: No progress for this long => the session ended or wedged; recycle it.
STALL_SECONDS=300
#: How often to check liveness.
POLL_SECONDS=30

mkdir -p "$LOG_DIR"
echo "overnight bridge supervisor starting; logs in $LOG_DIR"

cycle=0
while true; do
    cycle=$((cycle + 1))
    stamp="$(date +%Y%m%d_%H%M%S)"
    log="$LOG_DIR/session_${stamp}_c${cycle}.log"

    echo "[$(date +%H:%M:%S)] cycle $cycle -- launching game + runner -> $log"

    # kill_runners verifies the processes are actually gone before returning;
    # a survivor would hold the bridge and starve the new runner.
    "$PY" "$REPO/scripts/kill_runners.py" >/dev/null 2>&1
    powershell -NoProfile -Command "Stop-Process -Name SlayTheSpire2 -Force -EA SilentlyContinue" >/dev/null 2>&1
    sleep 6

    bash "$REPO/scripts/launch_agent.sh" >>"$log" 2>&1 || {
        echo "[$(date +%H:%M:%S)] launch failed; retrying in 60s"
        sleep 60
        continue
    }

    "$PY" -u -m sts2_env.bridge.agent_runner \
        --combat-policy planner \
        --run-policy rl --rl-run-model "$MODEL" \
        --combat-delay 0.2 --verbose >>"$log" 2>&1 &
    runner_pid=$!

    # Watch for PROGRESS, not for liveness: the failure mode here is a runner
    # that is perfectly alive and waiting on a game that has exited, so
    # "is the process up" would never fire.
    last_size=0
    stalled=0
    while true; do
        sleep "$POLL_SECONDS"
        if ! kill -0 "$runner_pid" 2>/dev/null; then
            echo "[$(date +%H:%M:%S)] runner exited; recycling"
            break
        fi
        size=$(stat -c %s "$log" 2>/dev/null || echo 0)
        # Pings still grow the log, so compare against real activity instead.
        active=$(grep -cE "COMBAT \[HP:|Run finished|RUN-RL" "$log" 2>/dev/null || echo 0)
        if [ "$active" -eq "$last_size" ]; then
            stalled=$((stalled + POLL_SECONDS))
            if [ "$stalled" -ge "$STALL_SECONDS" ]; then
                echo "[$(date +%H:%M:%S)] no progress for ${STALL_SECONDS}s -- recycling"
                kill "$runner_pid" 2>/dev/null
                break
            fi
        else
            stalled=0
            last_size=$active
        fi
    done
done
