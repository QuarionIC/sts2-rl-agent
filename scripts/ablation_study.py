"""One-factor-at-a-time ablation study for the from-scratch Necrobinder G1 task.

Motivation (attempts 1-10 post-mortem): every BC-lineage attempt (7-10)
started high (~12 floors) but was FLAT; the from-scratch attempts (5/6, old
shaping + old obs, mean-pool policy) were lower but showed a STEADY floor
slope (7.3 -> 8.5 over 5-6M). BC is abandoned; the goal is to recover a
positive learning slope from scratch and maximize it. Each arm changes
EXACTLY ONE factor vs the A0 baseline; the winning components are then
combined ("Rainbow-style") into a FINAL config.

Arms (3M steps each, evals at ~1M/2M/3M: 200 episodes, deterministic,
shaping off), run SEQUENTIALLY (one training process at a time -- 16GB box):

====  ==================  =====================================================
Arm   Factor              Change vs A0
====  ==================  =====================================================
A0    (baseline)          from scratch, per-slot arch, PBRS 1.0, lr 2e-4,
                          target_kl 0.03, ent 0.01, gamma 0.997, 16 envs, G1
A1    reward shaping      legacy attempt-6 event shaping instead of PBRS
A2    hand encoding       mean-pool (pre-per-slot) instead of per-slot concat
A3    deck visibility     deck-bag + archetype obs segment zeroed
A4    self-imitation      --sil (SIL from scratch, no BC anywhere)
A5    entropy             ent_coef 0.03
A6    discount            gamma 0.99 (PBRS gamma_shape follows)
A7    step size           lr 4e-4 with target_kl 0.05
====  ==================  =====================================================

Usage
-----
    python scripts/ablation_study.py                  # all 8 arms
    python scripts/ablation_study.py --arms A0,A4     # subset
    python scripts/ablation_study.py --through-final  # arms + selection + FINAL
    python scripts/ablation_study.py --select-only    # print verdicts from results

Results persist incrementally to output/ablation/results.json (one record per
arm: flags, evals, least-squares floor slope, final floors, act-2 reaches,
wall time, status). Completed arms are skipped on relaunch; a crashed arm is
recorded and the study continues. Early stop: only if the 1M eval shows
mean_floors < 3.0 (a policy that cannot clear the opening rooms). The bar is
low on purpose -- selection is by SLOPE, so a slow-starting arm must be
allowed to show its trajectory.

Selection rule (applied by --through-final / --select-only): an arm's factor
is adopted only if it beats A0 by more than the noise floor (0.3 mean
floors) on, in priority order: (1) implied floor gain from the eval-to-eval
slope, (2) final floors, (3) act-2 reaches (>10/200 episodes). Crashed or
early-stopped arms never win. FINAL = A0 + all adopted factors, run as a 3M
sanity arm before any long launch.
"""

from __future__ import annotations

import os

# Same BLAS caps as the trainer (defensive: children inherit them even
# before the trainer's own setdefault runs).
for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import ctypes
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
TRAINER = REPO / "scripts" / "train_necrobinder.py"
STUDY_DIR = REPO / "output" / "ablation"
RESULTS_PATH = STUDY_DIR / "results.json"

ARM_STEPS = 3_000_000
EVAL_FREQ = 1_000_000
EVAL_EPISODES = 200
N_ENVS = 16
# Single-seed noise band in mean_floors units. MEASURED, not assumed: the same
# baseline config produced 5.445 and 6.68 mean floors at its 1M eval across two
# runs (~1.2 floors of run-to-run variance from seed/init alone), which dwarfs
# the ~0.28-floor standard error of a 200-episode eval. The original 0.3 band
# would have promoted pure seed noise. 1.0 is the smallest defensible band for
# a ONE-SEED screen; effects this coarse screen can legitimately detect are
# large ones (e.g. mean-pool finishing 2.3 floors behind). Anything inside the
# band must go to the 3-seed confirmation phase (scripts/confirm_phase.py)
# before it is believed.
NOISE_FLOORS = 1.0
ACT2_NOISE = 10             # episodes out of 200
# 1M-eval floor below which an arm is hopeless. Deliberately LOW: the study's
# selection criterion is the floor SLOPE, and a from-scratch arm can sit at
# 4.5-6 floors at 1M and still have the best slope (the first calibration used
# 6.0 and early-stopped the BASELINE arm A0 at 5.45 and the mean-pool arm A2 at
# 4.67, destroying the comparison the study exists to make). Only truly broken
# arms -- a policy that cannot clear the first few rooms -- are cut here.
EARLY_STOP_FLOORS = 3.0
ARM_TIMEOUT_S = 4 * 3600    # hard per-arm wall clock cap
POLL_S = 30

BASE_FLAGS = [
    "--stage", "G1",
    "--total-steps", str(ARM_STEPS),
    "--n-envs", str(N_ENVS),
    "--eval-freq", str(EVAL_FREQ),
    "--eval-episodes", str(EVAL_EPISODES),
    # only the ~3M checkpoint + best_model.zip per arm (disk economy)
    "--checkpoint-freq", str(ARM_STEPS),
]

#: name -> (factor description, extra trainer flags)
ARMS: dict[str, tuple[str, list[str]]] = {
    "A0": ("baseline (from scratch, PBRS, per-slot, deck obs)", []),
    "A1": ("legacy attempt-6 event shaping instead of PBRS", ["--legacy-shaping"]),
    "A2": ("mean-pool hand encoding instead of per-slot", ["--hand-encoding", "meanpool"]),
    "A3": ("deck-bag + archetype obs zeroed", ["--no-deck-obs"]),
    "A4": ("self-imitation learning (no BC)", ["--sil"]),
    "A5": ("higher entropy: ent_coef 0.03", ["--ent-coef", "0.03"]),
    "A6": ("shorter horizon: gamma 0.99", ["--gamma", "0.99"]),
    "A7": ("wider steps: lr 4e-4, target_kl 0.05", ["--lr", "4e-4", "--target-kl", "0.05"]),
}
ARM_ORDER = list(ARMS)


# ---------------------------------------------------------------------------
# Results persistence
# ---------------------------------------------------------------------------

def load_results() -> dict:
    if RESULTS_PATH.exists():
        return json.loads(RESULTS_PATH.read_text(encoding="utf-8"))
    return {"config": {
        "arm_steps": ARM_STEPS, "eval_freq": EVAL_FREQ,
        "eval_episodes": EVAL_EPISODES, "n_envs": N_ENVS,
        "noise_floors": NOISE_FLOORS, "act2_noise": ACT2_NOISE,
        "early_stop_floors": EARLY_STOP_FLOORS,
    }, "arms": {}}


def save_results(results: dict) -> None:
    STUDY_DIR.mkdir(parents=True, exist_ok=True)
    results["updated"] = time.strftime("%Y-%m-%d %H:%M:%S")
    tmp = RESULTS_PATH.with_suffix(".json.tmp")
    tmp.write_text(json.dumps(results, indent=2), encoding="utf-8")
    tmp.replace(RESULTS_PATH)


# ---------------------------------------------------------------------------
# Eval parsing / metrics
# ---------------------------------------------------------------------------

def read_evals(arm_dir: Path) -> list[dict]:
    path = arm_dir / "G1" / "eval_history.jsonl"
    if not path.exists():
        return []
    evals = []
    for line in path.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line:
            try:
                evals.append(json.loads(line))
            except json.JSONDecodeError:
                pass  # partial trailing line while the trainer writes
    return evals


def act2_reaches(ev: dict) -> int:
    """Episodes that got past act 0 (died in act>=1, won, or truncated late)."""
    deaths_act0 = int(ev.get("deaths_by_act", {}).get("0", 0))
    return int(ev.get("episodes", EVAL_EPISODES)) - deaths_act0


def floor_slope(evals: list[dict]) -> float | None:
    """Least-squares slope of mean_floors vs steps (floors per 1M steps)."""
    if len(evals) < 2:
        return None
    xs = [ev["steps"] / 1e6 for ev in evals]
    ys = [ev["mean_floors"] for ev in evals]
    n = len(xs)
    mx, my = sum(xs) / n, sum(ys) / n
    denom = sum((x - mx) ** 2 for x in xs)
    if denom == 0:
        return None
    return sum((x - mx) * (y - my) for x, y in zip(xs, ys)) / denom


def summarize(arm: str, flags: list[str], evals: list[dict],
              status: str, wall_s: float) -> dict:
    last = evals[-1] if evals else None
    return {
        "arm": arm,
        "factor": ARMS.get(arm, (flags and " ".join(flags) or "final", None))[0],
        "flags": flags,
        "status": status,
        "wall_s": round(wall_s, 1),
        "evals": evals,
        "floor_slope_per_m": floor_slope(evals),
        "final_floors": last["mean_floors"] if last else None,
        "final_act2": act2_reaches(last) if last else None,
        "final_win_rate": last["win_rate"] if last else None,
    }


# ---------------------------------------------------------------------------
# Arm runner
# ---------------------------------------------------------------------------

def tree_cpu_seconds(pid: int) -> float | None:
    """Total CPU seconds used by a process tree, or None if unmeasurable.

    Used instead of wall clock for the arm timeout: a suspended machine
    (Modern Standby) advances wall time but not CPU time, so a sleeping
    laptop can no longer kill a healthy arm mid-run.
    """
    try:
        import psutil
    except ImportError:
        return None
    try:
        proc = psutil.Process(pid)
        total = sum(proc.cpu_times()[:2])
        for child in proc.children(recursive=True):
            try:
                total += sum(child.cpu_times()[:2])
            except (psutil.NoSuchProcess, psutil.AccessDenied):
                continue
        return total
    except (psutil.NoSuchProcess, psutil.AccessDenied, Exception):
        return None


def kill_tree(pid: int) -> None:
    subprocess.run(
        ["taskkill", "/PID", str(pid), "/T", "/F"],
        capture_output=True, check=False,
    )


class _MEMORYSTATUSEX(ctypes.Structure):
    _fields_ = [
        ("dwLength", ctypes.c_ulong), ("dwMemoryLoad", ctypes.c_ulong),
        ("ullTotalPhys", ctypes.c_ulonglong), ("ullAvailPhys", ctypes.c_ulonglong),
        ("ullTotalPageFile", ctypes.c_ulonglong), ("ullAvailPageFile", ctypes.c_ulonglong),
        ("ullTotalVirtual", ctypes.c_ulonglong), ("ullAvailVirtual", ctypes.c_ulonglong),
        ("ullAvailExtendedVirtual", ctypes.c_ulonglong),
    ]


def commit_available_mb() -> int:
    """Available Windows commit (pagefile-backed) in MB. The first study
    launch died to commit exhaustion: a 16GB orphaned multiprocessing worker
    from an unrelated session starved the trainer until numpy's 299MiB
    rollout flatten (16,1024,4778) failed, and every later arm OOM'd on
    startup within seconds."""
    st = _MEMORYSTATUSEX()
    st.dwLength = ctypes.sizeof(_MEMORYSTATUSEX)
    ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(st))
    return int(st.ullAvailPageFile // (1024 * 1024))


#: Commit headroom an arm needs to start safely (trainer + 16 workers + CUDA
#: host allocations peak well under this; attempts 1-10 ran 24 envs).
PREFLIGHT_COMMIT_MB = 8_000
PREFLIGHT_WAIT_S = 300


def preflight_commit(arm: str) -> int:
    """Wait (bounded) for commit headroom before launching an arm, so a
    still-dying previous arm's workers cannot cascade an OOM into this one."""
    deadline = time.time() + PREFLIGHT_WAIT_S
    avail = commit_available_mb()
    while avail < PREFLIGHT_COMMIT_MB and time.time() < deadline:
        print(f"[{arm}] preflight: only {avail} MB commit available; "
              f"waiting for {PREFLIGHT_COMMIT_MB} MB ...", flush=True)
        time.sleep(15)
        avail = commit_available_mb()
    if avail < PREFLIGHT_COMMIT_MB:
        print(f"[{arm}] preflight: proceeding anyway with {avail} MB "
              f"(watch for OOM)", flush=True)
    return avail


def run_arm(arm: str, flags: list[str], results: dict,
            fresh: bool = True, retries: int = 1) -> dict:
    arm_dir = STUDY_DIR / arm
    if fresh and arm_dir.exists():
        shutil.rmtree(arm_dir)  # stale partial output would corrupt parsing
    arm_dir.mkdir(parents=True, exist_ok=True)
    log_path = arm_dir / "train.log"

    cmd = [sys.executable, str(TRAINER), *BASE_FLAGS,
           "--output-dir", str(arm_dir), *flags]
    print(f"\n[{arm}] {ARMS.get(arm, ('FINAL combined config',))[0]}")
    print(f"[{arm}] launching: {' '.join(cmd)}", flush=True)
    commit_at_start = preflight_commit(arm)

    start = time.time()
    status = "completed"
    with open(log_path, "w", encoding="utf-8") as log_f:
        proc = subprocess.Popen(
            cmd, stdout=log_f, stderr=subprocess.STDOUT,
            cwd=str(REPO), env=os.environ.copy(),
        )
        try:
            reported = 0
            while True:
                try:
                    proc.wait(timeout=POLL_S)
                    break
                except subprocess.TimeoutExpired:
                    pass
                evals = read_evals(arm_dir)
                if len(evals) > reported:
                    for ev in evals[reported:]:
                        print(f"[{arm}] eval @ {ev['steps']:,}: "
                              f"floors={ev['mean_floors']:.2f} "
                              f"act2={act2_reaches(ev)} "
                              f"win={ev['win_rate']:.1%}", flush=True)
                    reported = len(evals)
                # early stop: catastrophic first eval
                if evals and evals[0]["mean_floors"] < EARLY_STOP_FLOORS:
                    print(f"[{arm}] EARLY STOP: 1M eval floors "
                          f"{evals[0]['mean_floors']:.2f} < {EARLY_STOP_FLOORS}",
                          flush=True)
                    kill_tree(proc.pid)
                    proc.wait(timeout=60)
                    status = "early_stopped"
                    break
                # Timeout on the arm's CPU TIME, not wall clock. A0's first
                # study lost arm A3 this way: the laptop entered Modern
                # Standby for 10h37m mid-arm, and on wake the wall-clock check
                # saw ">4h elapsed" and killed a run that had used only ~38
                # minutes of CPU and was training normally at ~1400 fps. The
                # arm's truncated 2-eval history then produced a meaningless
                # -1.3 slope. CPU time does not advance while suspended.
                cpu_s = tree_cpu_seconds(proc.pid)
                if cpu_s is None:  # psutil unavailable -> wall clock w/ slack
                    if time.time() - start > ARM_TIMEOUT_S * 3:
                        print(f"[{arm}] TIMEOUT (wall fallback) after "
                              f"{(time.time()-start)/3600:.1f}h; killing", flush=True)
                        kill_tree(proc.pid)
                        proc.wait(timeout=60)
                        status = "timeout"
                        break
                elif cpu_s > ARM_TIMEOUT_S:
                    print(f"[{arm}] TIMEOUT after {cpu_s/3600:.1f}h CPU "
                          f"({(time.time()-start)/3600:.1f}h wall); killing",
                          flush=True)
                    kill_tree(proc.pid)
                    proc.wait(timeout=60)
                    status = "timeout"
                    break
        except BaseException:
            kill_tree(proc.pid)  # never leave a half-dead arm running
            raise
        finally:
            # ALWAYS reap the tree: a crashed trainer can strand its 16 env
            # workers, whose resident commit then OOMs every later arm (the
            # exact first-launch cascade). No-op if they exited cleanly.
            kill_tree(proc.pid)
    if status == "completed" and proc.returncode != 0:
        status = f"crashed (rc={proc.returncode})"

    wall = time.time() - start
    record = summarize(arm, flags, read_evals(arm_dir), status, wall)
    record["commit_available_mb_at_start"] = commit_at_start
    if status.startswith("crashed") and not record["evals"] and retries > 0:
        # Zero-eval crash smells transient (commit/CUDA OOM at startup);
        # one clean retry after the reap.
        print(f"[{arm}] zero-eval crash; retrying "
              f"({retries} left)", flush=True)
        return run_arm(arm, flags, results, fresh=True, retries=retries - 1)
    results["arms"][arm] = record
    save_results(results)
    print(f"[{arm}] {status} in {wall/60:.1f} min; "
          f"slope={fmt(record['floor_slope_per_m'])} "
          f"final={fmt(record['final_floors'])} "
          f"act2={record['final_act2']}", flush=True)
    return record


# ---------------------------------------------------------------------------
# Table / selection
# ---------------------------------------------------------------------------

def fmt(v, spec: str = ".2f") -> str:
    return "-" if v is None else format(v, spec)


def print_table(results: dict) -> None:
    print("\n=== ablation results so far ===")
    print(f"{'arm':<6}{'status':<16}{'slope/M':>9}{'final':>8}{'act2':>6}"
          f"{'win%':>7}{'wall_min':>10}  factor")
    for arm in [*ARM_ORDER, "FINAL"]:
        rec = results["arms"].get(arm)
        if rec is None:
            continue
        win = "-" if rec["final_win_rate"] is None else f"{rec['final_win_rate']:.1%}"
        print(f"{arm:<6}{rec['status']:<16}"
              f"{fmt(rec['floor_slope_per_m'], '+.3f'):>9}"
              f"{fmt(rec['final_floors']):>8}"
              f"{str(rec['final_act2'] if rec['final_act2'] is not None else '-'):>6}"
              f"{win:>7}{rec['wall_s']/60:>10.1f}  {rec['factor']}")
    print(flush=True)


def select_winners(results: dict) -> tuple[list[str], list[str], list[str]]:
    """Apply the decision rule. Returns (winning arm names, combined FINAL
    flags, per-arm verdict lines)."""
    base = results["arms"].get("A0")
    verdicts: list[str] = []
    winners: list[str] = []
    if base is None or base["floor_slope_per_m"] is None:
        return [], [], ["A0 baseline missing or unevaluated -- no selection possible"]
    b_slope = base["floor_slope_per_m"]
    b_final = base["final_floors"]
    b_act2 = base["final_act2"]
    # Implied extra floors over the 2M span between the 1M and 3M evals.
    span_m = 2.0
    for arm in ARM_ORDER[1:]:
        rec = results["arms"].get(arm)
        if rec is None:
            verdicts.append(f"{arm}: not run -- keep baseline")
            continue
        if rec["status"] != "completed" or rec["floor_slope_per_m"] is None:
            verdicts.append(f"{arm}: {rec['status']} -- keep baseline")
            continue
        d_slope_floors = (rec["floor_slope_per_m"] - b_slope) * span_m
        d_final = rec["final_floors"] - b_final
        d_act2 = rec["final_act2"] - b_act2
        # LEVEL GATE (must precede the slope test). A high slope means nothing
        # if the arm is simply climbing out of a hole: A2 (mean-pool) scored
        # slope +0.55/M -- the best in the screen -- while finishing 2.3 floors
        # BEHIND the baseline, because it started ~3.3 floors lower. Without
        # this gate the old rule adopted it on slope alone.
        if d_final < -NOISE_FLOORS:
            verdicts.append(
                f"{arm}: DISQUALIFIED on level (final {d_final:+.2f} floors vs "
                f"baseline, worse than -{NOISE_FLOORS}) despite slope "
                f"{d_slope_floors:+.2f} -- keep baseline")
            continue
        if d_slope_floors > NOISE_FLOORS:
            winners.append(arm)
            verdicts.append(
                f"{arm}: WIN on slope (+{d_slope_floors:.2f} implied floors "
                f"> {NOISE_FLOORS})")
        elif d_slope_floors < -NOISE_FLOORS:
            verdicts.append(
                f"{arm}: LOSS on slope ({d_slope_floors:+.2f} implied floors) "
                f"-- keep baseline")
        elif d_final > NOISE_FLOORS:
            winners.append(arm)
            verdicts.append(
                f"{arm}: slope tie ({d_slope_floors:+.2f}); WIN on final floors "
                f"({d_final:+.2f} > {NOISE_FLOORS})")
        elif d_final < -NOISE_FLOORS:
            verdicts.append(
                f"{arm}: slope tie; LOSS on final floors ({d_final:+.2f}) "
                f"-- keep baseline")
        elif d_act2 > ACT2_NOISE:
            winners.append(arm)
            verdicts.append(
                f"{arm}: slope+floors tie; WIN on act-2 reaches ({d_act2:+d} "
                f"> {ACT2_NOISE})")
        else:
            verdicts.append(
                f"{arm}: within noise on all criteria "
                f"(slope {d_slope_floors:+.2f}, final {d_final:+.2f}, "
                f"act2 {d_act2:+d}) -- keep baseline")
    combined: list[str] = []
    for arm in winners:
        combined.extend(ARMS[arm][1])
    return winners, combined, verdicts


# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------

def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    parser.add_argument("--arms", type=str, default=",".join(ARM_ORDER),
                        help="Comma-separated arm subset (default: all).")
    parser.add_argument("--force", action="store_true",
                        help="Re-run arms already recorded in results.json.")
    parser.add_argument("--through-final", action="store_true",
                        help="After the arms: apply the selection rule and "
                             "run the combined FINAL sanity arm (3M).")
    parser.add_argument("--select-only", action="store_true",
                        help="Print verdicts from existing results and exit.")
    args = parser.parse_args()

    results = load_results()

    if args.select_only:
        print_table(results)
        winners, combined, verdicts = select_winners(results)
        for v in verdicts:
            print(v)
        print(f"\nwinners: {winners or 'none'}")
        print(f"FINAL flags: {combined or '(pure baseline)'}")
        return

    wanted = [a.strip() for a in args.arms.split(",") if a.strip()]
    for arm in wanted:
        if arm not in ARMS:
            raise SystemExit(f"unknown arm {arm!r} (choose from {ARM_ORDER})")

    study_start = time.time()
    for arm in wanted:
        prior = results["arms"].get(arm)
        if prior is not None and not args.force:
            print(f"[{arm}] already recorded ({prior['status']}); skipping "
                  f"(--force to redo)", flush=True)
            continue
        try:
            run_arm(arm, ARMS[arm][1], results)
        except Exception as exc:  # arm crash must not sink the study
            results["arms"][arm] = summarize(arm, ARMS[arm][1], [],
                                             f"harness error: {exc}", 0.0)
            save_results(results)
            print(f"[{arm}] harness error: {exc}", flush=True)
        print_table(results)

    if args.through_final:
        winners, combined, verdicts = select_winners(results)
        print("\n=== selection ===")
        for v in verdicts:
            print(v)
        print(f"winners: {winners or 'none'}; FINAL flags: {combined or '(pure baseline)'}",
              flush=True)
        results["selection"] = {
            "winners": winners, "final_flags": combined, "verdicts": verdicts,
        }
        save_results(results)
        prior = results["arms"].get("FINAL")
        if prior is not None and not args.force:
            print("[FINAL] already recorded; skipping")
        elif not winners:
            print("[FINAL] no factor beat baseline; FINAL == A0 -- reusing "
                  "the A0 record as the sanity result")
            results["arms"]["FINAL"] = {**results["arms"]["A0"],
                                        "arm": "FINAL",
                                        "factor": "combined == baseline (no winners)"}
            save_results(results)
        else:
            run_arm("FINAL", combined, results)
        print_table(results)

    print(f"study done in {(time.time() - study_start)/3600:.2f} h", flush=True)


if __name__ == "__main__":
    main()
