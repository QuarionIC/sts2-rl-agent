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

#: Out-of-combat policy. "rl" needs RUN_MODEL to match the CURRENT observation
#: width; "heuristic" always works.
#:
#: Pinned to heuristic 2026-08-01. Adding the six ActsFromThePast PowerIds took
#: the observation 4900 -> 4936, and because powers are raw one-hot rather than
#: embedded the growth lands INSIDE the extractor's flat region -- its output
#: went 4210 -> 4246, so migrating a checkpoint means inserting 36 zero columns
#: into the first MLP layer at exact offsets, not just adding embedding rows.
#: That is worth doing carefully, not at 2am.
#:
#: Nothing important is lost meanwhile: divergence hunting exercises the
#: PLANNER, which drives combat. The run agent only picks map/reward/shop, and
#: for measuring simulator fidelity the heuristic does that just as well.
RUN_POLICY="${RUN_POLICY:-heuristic}"
RUN_MODEL="output/joint_alt_offerobs/r2/run/r1_run_best_v590p299.zip"

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

    if [ "$RUN_POLICY" = "rl" ]; then
        "$PY" -u -m sts2_env.bridge.agent_runner \
            --combat-policy planner \
            --run-policy rl --rl-run-model "$RUN_MODEL" \
            --combat-delay 0.2 --verbose >>"$log" 2>&1 &
    else
        "$PY" -u -m sts2_env.bridge.agent_runner \
            --combat-policy planner \
            --combat-delay 0.2 --verbose >>"$log" 2>&1 &
    fi
    runner_pid=$!

    # Watch for PROGRESS, not for liveness: the failure mode here is a runner
    # that is perfectly alive and waiting on a game that has exited, so
    # "is the process up" would never fire.
    last_activity=0
    stalled=0
    while true; do
        sleep "$POLL_SECONDS"
        if ! kill -0 "$runner_pid" 2>/dev/null; then
            echo "[$(date +%H:%M:%S)] runner exited; recycling"
            break
        fi
        # Pings still grow the log, so compare against real ACTIVITY instead
        # of file size.
        #
        # `grep -c` exits 1 when the count is zero, so `|| echo 0` appended a
        # SECOND zero and the test below saw "0\n0" -- bash reported "integer
        # expression expected" and the comparison never succeeded, which meant
        # a genuinely wedged session was never recycled. The guard that exists
        # to catch a stall was itself broken by its own error handling.
        active=$(grep -cE "COMBAT \[HP:|Run finished|RUN-RL" "$log" 2>/dev/null | head -1)
        active=${active:-0}
        if [ "$active" -eq "$last_activity" ]; then
            stalled=$((stalled + POLL_SECONDS))
            if [ "$stalled" -ge "$STALL_SECONDS" ]; then
                echo "[$(date +%H:%M:%S)] no progress for ${STALL_SECONDS}s -- recycling"
                kill "$runner_pid" 2>/dev/null
                break
            fi
        else
            stalled=0
            last_activity=$active
        fi
    done
done
