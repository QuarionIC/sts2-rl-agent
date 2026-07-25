"""One Expert-Iteration cycle: kill trainer -> distill -> relaunch.

TRAINING_REVAMP_SPEC Phase 8 trainer integration, v1 (the MANUAL cycle,
deliberately chosen over an in-process --exit-every pause: the training
process stays a plain, resumable train_necrobinder.py run, and the 1-2 h
MCTS collection cannot destabilize or hold up the learner -- the cycle is
just stop -> distill -> relaunch with --init-model).

Steps
-----
1. Kill every running ``train_necrobinder.py`` process tree (unless
   ``--no-kill``).
2. Rotate logs with ``--attempt N``:
   ``output/<campaign>.log            -> <campaign>.attemptN.log``
   ``<stage-dir>/eval_history.jsonl   -> eval_history.attemptN.jsonl``
   and snapshot ``<stage-dir>/best_model.zip -> attemptN_best.zip``.
3. Run exit_distill collect+distill on ``--checkpoint`` (default: the
   rotated best_model snapshot) -> ``<out-dir>/distilled.zip``.
   ``--skip-collect`` reuses existing shards in ``--out-dir``.
4. Relaunch the trainer DETACHED with ``--relaunch-args`` (the string may
   contain ``{distilled}``; if it contains no ``--init-model``, one is
   appended automatically), stdout/stderr -> the fresh campaign log.

Example (attempt 10 -> 11):
    python scripts/run_exit_cycle.py --attempt 10 \
        --stage-dir output/necrobinder_g1/G1 \
        --campaign-log output/necrobinder_g1_campaign.log \
        --out-dir output/exit_cycle1 --workers 4 \
        --relaunch-args "--stage G1 --total-steps 20000000 --n-envs 16 \
            --eval-freq 1000000 --eval-episodes 200 \
            --anchor-model output/bc_init/bc_init.zip --anchor-coef 0.1 \
            --anchor-coef-final 0.02 --anchor-decay-steps 4000000 \
            --lr 5e-5 --target-kl 0.015 --sil --tensorboard \
            --output-dir output/necrobinder_g1"
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import shlex
import shutil
import subprocess
import sys
import time
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

TRAINER = "train_necrobinder.py"


def kill_trainer() -> int:
    """Terminate every running train_necrobinder.py process tree."""
    import psutil

    killed = 0
    for proc in psutil.process_iter(["pid", "cmdline"]):
        try:
            cmdline = " ".join(proc.info["cmdline"] or [])
        except (psutil.NoSuchProcess, psutil.AccessDenied):
            continue
        if TRAINER not in cmdline or proc.pid == os.getpid():
            continue
        print(f"[kill] {proc.pid}: {cmdline[:110]}")
        try:
            children = proc.children(recursive=True)
            proc.terminate()
            gone, alive = psutil.wait_procs([proc], timeout=15)
            for p in alive:
                p.kill()
            for child in children:
                try:
                    child.terminate()
                except psutil.NoSuchProcess:
                    pass
            psutil.wait_procs(children, timeout=10)
            for child in children:
                if child.is_running():
                    child.kill()
            killed += 1
        except psutil.NoSuchProcess:
            pass
    if killed:
        time.sleep(2.0)  # let file handles (log, ckpt) close
    print(f"[kill] terminated {killed} trainer process tree(s)")
    return killed


def rotate(stage_dir: Path, campaign_log: Path | None, attempt: int) -> Path | None:
    """Rotate logs to .attempt{N}.*; snapshot best_model. Returns snapshot."""
    suffix = f"attempt{attempt}"
    if campaign_log is not None and campaign_log.exists():
        target = campaign_log.with_name(
            campaign_log.stem + f".{suffix}" + campaign_log.suffix
        )
        campaign_log.rename(target)
        print(f"[rotate] {campaign_log} -> {target}")
    hist = stage_dir / "eval_history.jsonl"
    if hist.exists():
        target = stage_dir / f"eval_history.{suffix}.jsonl"
        hist.rename(target)
        print(f"[rotate] {hist} -> {target}")
    best = stage_dir / "best_model.zip"
    if best.exists():
        snap = stage_dir / f"{suffix}_best.zip"
        shutil.copy2(best, snap)
        print(f"[rotate] snapshot {best} -> {snap}")
        return snap
    return None


def relaunch(python: str, relaunch_args: str, distilled: str, log_path: Path) -> int:
    """Start the trainer detached; stdout/stderr -> log_path. Returns pid."""
    args = relaunch_args.replace("{distilled}", distilled)
    if "--init-model" not in args:
        args += f" --init-model {distilled}"
    cmd = [python, str(REPO_ROOT / "scripts" / TRAINER)] + shlex.split(args)
    print(f"[relaunch] {' '.join(cmd)}")
    print(f"[relaunch] log -> {log_path}")
    log_f = open(log_path, "w", encoding="utf-8")
    flags = 0
    if os.name == "nt":
        flags = subprocess.CREATE_NEW_PROCESS_GROUP | 0x00000008  # DETACHED_PROCESS
    proc = subprocess.Popen(
        cmd, stdout=log_f, stderr=subprocess.STDOUT,
        cwd=str(REPO_ROOT), creationflags=flags,
    )
    print(f"[relaunch] trainer pid {proc.pid}")
    return proc.pid


def main() -> None:
    p = argparse.ArgumentParser(description="kill -> distill -> relaunch ExIt cycle")
    p.add_argument("--attempt", type=int, required=True,
                   help="The attempt number BEING RETIRED (logs get .attemptN)")
    p.add_argument("--stage-dir", default="output/necrobinder_g1/G1")
    p.add_argument("--campaign-log", default="output/necrobinder_g1_campaign.log")
    p.add_argument("--checkpoint", default=None,
                   help="Model to distill (default: the rotated best_model snapshot)")
    p.add_argument("--out-dir", default=None,
                   help="ExIt working dir (default output/exit_cycle<attempt>)")
    p.add_argument("--no-kill", action="store_true")
    p.add_argument("--skip-collect", action="store_true",
                   help="Reuse existing shards in --out-dir")
    p.add_argument("--no-relaunch", action="store_true")
    p.add_argument("--relaunch-args", default="",
                   help="train_necrobinder.py args; {distilled} is substituted, "
                        "--init-model appended if absent")
    # passthrough to exit_distill
    p.add_argument("--workers", type=int, default=4)
    p.add_argument("--decisions", type=int, default=20_000)
    p.add_argument("--sims", type=int, default=96)
    p.add_argument("--determinizations", type=int, default=12)
    p.add_argument("--dirichlet-eps", type=float, default=0.25)
    p.add_argument("--max-minutes", type=float, default=120.0)
    p.add_argument("--epochs", type=int, default=3)
    p.add_argument("--lr", type=float, default=1.0e-4)
    p.add_argument("--batch-size", type=int, default=1024)
    p.add_argument("--value-coef", type=float, default=0.5)
    p.add_argument("--device", default="cuda")
    p.add_argument("--ascension", type=int, default=0)
    p.add_argument("--max-act-count", type=int, default=2)
    p.add_argument("--seed-base", type=int, default=10_000_000)
    args = p.parse_args()

    stage_dir = Path(args.stage_dir)
    out_dir = Path(args.out_dir or f"output/exit_cycle{args.attempt}")
    campaign_log = Path(args.campaign_log) if args.campaign_log else None

    if not args.no_kill:
        kill_trainer()
    snapshot = rotate(stage_dir, campaign_log, args.attempt)
    checkpoint = args.checkpoint or (str(snapshot) if snapshot else None)
    if checkpoint is None:
        raise SystemExit("no --checkpoint and no best_model.zip to snapshot")

    from exit_distill import run_collect, run_distill  # same scripts/ dir

    ed_args = argparse.Namespace(
        mode="all", checkpoint=checkpoint, out_dir=str(out_dir),
        ascension=args.ascension, max_act_count=args.max_act_count,
        sims=args.sims, determinizations=args.determinizations,
        workers=args.workers, seed_base=args.seed_base,
        decisions=args.decisions, dirichlet_eps=args.dirichlet_eps,
        max_minutes=args.max_minutes, epochs=args.epochs, lr=args.lr,
        batch_size=args.batch_size, value_coef=args.value_coef,
        device=args.device, episodes=0,
    )
    shards = None if args.skip_collect else run_collect(ed_args)
    distilled = run_distill(ed_args, shards)

    if args.no_relaunch:
        print(f"[cycle] done (no relaunch). Distilled: {distilled}")
        return
    if not args.relaunch_args:
        raise SystemExit("--relaunch-args required unless --no-relaunch")
    log_path = campaign_log if campaign_log is not None else out_dir / "campaign.log"
    relaunch(sys.executable, args.relaunch_args, distilled, log_path)


if __name__ == "__main__":
    main()
