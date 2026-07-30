#!/usr/bin/env bash
# MCTS search-budget sweep -- the experiment the gate result actually calls for.
#
# The controlled 48-sim gate returned a paired delta of -0.56 +/- 1.15 with only
# 24 overrides in 2552 searched decisions (1%). At a 1% override rate the search
# cannot move the outcome in either direction, so that number does not measure
# "does search help" -- it measures "48 simulations is not a search". AlphaZero
# used ~800 per move; 48 spread over 10-30 legal actions is barely one visit
# each, so PUCT never accumulates enough evidence to overcome the policy prior.
#
# The discriminating question is how OVERRIDE RATE scales with budget:
#   * if it stays near 1% while the budget grows ~17x, the priors dominate and
#     MCTS is genuinely the wrong operator for this problem -- adopt the planner
#     as the expert instead (scripts/planner_distill.py)
#   * if it climbs with budget, the earlier NO-GO was a budget artifact and the
#     curriculum is worth running at a real budget
#
# Deliberately waits for the LLM thinking eval to finish first. Running them
# together degraded the thinking arm from 44s to 76s per decision, and the user's
# stated order was thinking runs first.
set -uo pipefail
cd ~/sts2 && source .venv/bin/activate

SEEDS=${SEEDS:-6}
WORKERS=${WORKERS:-6}
log(){ echo "[$(date +%H:%M:%S)] $*"; }

log "waiting for the thinking eval to finish ..."
while pgrep -f "enable-thinking" >/dev/null; do sleep 120; done
log "thinking eval done -- starting the budget sweep"

for SIMS in 48 200 800; do
  log "=========== $SIMS sims x 8 determinizations, $SEEDS seeds ==========="
  python scripts/exit_distill.py --mode eval \
    --checkpoint models/run_gate_ckpt.zip \
    --out-dir "out/sweep_${SIMS}" \
    --episodes "$SEEDS" --ascension 0 --max-act-count 1 \
    --sims "$SIMS" --determinizations 8 \
    --workers "$WORKERS" --device cpu 2>&1 | tail -12
done

log "=========== SWEEP SUMMARY ==========="
python - <<'PY'
import json, numpy as np, math
print(f"{'sims':>6} {'pairs':>6} {'policy':>8} {'search':>8} {'paired delta':>17} "
      f"{'override%':>10} {'ms/dec':>8}")
for sims in (48, 200, 800):
    try:
        d = json.load(open(f"out/sweep_{sims}/go_no_go.json"))
    except Exception:
        print(f"{sims:>6}   (no result)")
        continue
    rows = d.get("records") or []
    # PAIR BY SEED. Records come back from a worker pool, so positional
    # pairing would silently compare different seeds to each other.
    pol = {r["seed"]: r for r in rows if r.get("arm") == "policy"}
    mct = {r["seed"]: r for r in rows if r.get("arm") == "mcts"}
    seeds = sorted(set(pol) & set(mct))
    diff = np.array([mct[s]["floor"] - pol[s]["floor"] for s in seeds], dtype=float)
    n = len(seeds)
    dtxt = (f"{diff.mean():+.2f} +/- {diff.std(ddof=1)/math.sqrt(n):.2f}"
            if n > 1 else "?")
    nd = sum(mct[s].get("mcts_decisions", 0) for s in seeds)
    ndis = sum(mct[s].get("mcts_disagree", 0) for s in seeds)
    ovr = (100.0 * ndis / nd) if nd else float("nan")
    ms = (1000.0 * sum(mct[s].get("mcts_seconds", 0.0) for s in seeds) / nd) if nd else float("nan")
    print(f"{sims:>6} {n:>6} {d['policy']['mean_floor']:>8.2f} "
          f"{d['mcts']['mean_floor']:>8.2f} {dtxt:>17} {ovr:>9.1f}% {ms:>8.0f}")
print()
print("Read the override% column FIRST. If it is flat across a 17x budget")
print("increase, MCTS is not the right operator here and the planner-expert")
print("path (scripts/planner_distill.py, 26.6% policy/planner disagreement)")
print("is the one to pursue.")
PY
log "SWEEP DONE"
