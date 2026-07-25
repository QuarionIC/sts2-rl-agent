"""Expert Iteration for the Necrobinder campaign (TRAINING_REVAMP_SPEC Phase 8).

Three modes sharing one CLI:

``collect``   Play episodes with the CURRENT policy (sampled actions, i.e.
              policy-visited states); at every non-forced combat decision
              run combat MCTS (sts2_env/search/combat_mcts.py) and record
              ``(obs, legal mask, visit distribution, root value)``.
              Multiprocess: each worker owns a CPU copy of the policy
              (BLAS/torch capped at 1 thread) and writes an npz shard.

``distill``   Load the shards and the checkpoint, run
              ``sts2_env.search.distill.distill`` (masked CE -> visit
              distribution + 0.5 * MSE(value -> root value), 2-3 epochs),
              save ``<out-dir>/distilled.zip``. Relaunch training with
              ``--init-model <out-dir>/distilled.zip`` (tensors are loaded
              name+shape matched, so the zip's algo class is irrelevant).

``eval``      GO/NO-GO: over N seeds, play each episode twice -- policy-only
              (deterministic) vs MCTS-assisted (policy everywhere, MCTS
              argmax on non-forced combat decisions) -- and print the
              comparison table. This is the gate for relaunching training
              on distilled weights: if search does not beat the raw policy,
              distilling search targets cannot help.

``all``       collect + distill.

Usage (from the repo root, venv python):
    python scripts/exit_distill.py --mode eval    --checkpoint output/necrobinder_g1/G1/best_model.zip --episodes 60 --workers 4
    python scripts/exit_distill.py --mode all     --checkpoint output/necrobinder_g1/G1/best_model.zip --decisions 20000 --workers 4
    python scripts/exit_distill.py --mode distill --checkpoint ... --out-dir output/exit_cycle1
"""

from __future__ import annotations

import os

# MUST run before numpy is imported, here and in every spawned worker (the
# worker re-imports this module): see scripts/train_necrobinder.py.
for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import multiprocessing as mp
import sys
import time
from pathlib import Path

import numpy as np

REPO_ROOT = Path(__file__).resolve().parents[1]
if str(REPO_ROOT) not in sys.path:
    sys.path.insert(0, str(REPO_ROOT))

DEFAULT_SIMS = 96
DEFAULT_DETERMINIZATIONS = 12
DEFAULT_DECISIONS = 20_000
DEFAULT_EVAL_EPISODES = 60
DEFAULT_SEED_BLOCK = 10_000_000
DEFAULT_DIRICHLET_EPS = 0.25
MAX_EPISODE_STEPS = 3_000

#: Terminal value MCTS assigns to player death. The value net is trained on
#: PBRS-shaped returns (gamma 0.997, terminal -1 PLUS the terminal potential
#: drop), so its "alive but losing" predictions sit around -1.1..-1.35 --
#: BELOW the naive -1.0 death terminal, which would make search PREFER dying
#: over continuing. -1.5 sits below the observed alive band on the same
#: scale (death's true shaped return is roughly -1 - Phi(s) <= -1).
DEFAULT_LOSS_VALUE = -1.5


# ---------------------------------------------------------------------------
# Shared worker plumbing
# ---------------------------------------------------------------------------

def _load_cpu_model(checkpoint: str):
    import torch

    torch.set_num_threads(1)
    from sb3_contrib import MaskablePPO

    # SIL/anchored checkpoints are saved MaskablePPO-compatible (runtime
    # attachments are excluded from save); base-class load is sufficient
    # everywhere the policy alone is needed.
    return MaskablePPO.load(checkpoint, device="cpu")


def _make_env(ascension: int, max_act_count: int):
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    return RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        max_act_count=max_act_count,
        reward_config=RewardConfig(shaping_scale=0.0),
        max_steps=MAX_EPISODE_STEPS,
    )


def _combat_decision(env, mask) -> bool:
    """True when the env sits on a NON-FORCED combat-slice decision that the
    combat MCTS can own (no legal action outside the slice -- multiplayer
    player-select etc. stays with the policy)."""
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import COMBAT_ACTIONS

    if env._mgr is None or env._mgr.phase != RunManager.PHASE_COMBAT:
        return False
    combat = env._mgr.get_combat_state()
    if combat is None or combat.is_over:
        return False
    if int(mask[:COMBAT_ACTIONS].sum()) <= 1:
        return False
    if int(mask[COMBAT_ACTIONS:].sum()) > 0:
        return False
    return True


# ---------------------------------------------------------------------------
# collect mode
# ---------------------------------------------------------------------------

def _collect_worker(args: dict) -> dict:
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS, CombatMCTS, MCTSConfig, SB3PolicyEvaluator,
        make_run_obs_builder,
    )

    worker_id = args["worker_id"]
    model = _load_cpu_model(args["checkpoint"])
    env = _make_env(args["ascension"], args["max_act_count"])
    evaluator = SB3PolicyEvaluator(model)
    cfg = MCTSConfig(
        n_simulations=args["sims"],
        n_determinizations=args["determinizations"],
        dirichlet_eps=args["dirichlet_eps"],
        loss_value=args["loss_value"],
        seed=worker_id,
    )

    obs_buf, mask_buf, visit_buf, value_buf = [], [], [], []
    quota = args["quota"]
    deadline = time.time() + args["max_minutes"] * 60.0
    episodes = 0
    mcts_time = 0.0

    while len(obs_buf) < quota and time.time() < deadline:
        seed = args["seed_base"] + worker_id * 100_000 + episodes
        obs, _ = env.reset(seed=seed)
        done = False
        steps = 0
        while not done and steps < MAX_EPISODE_STEPS:
            mask = env.action_masks()
            if len(obs_buf) < quota and _combat_decision(env, mask):
                combat = env._mgr.get_combat_state()
                mcts = CombatMCTS(evaluator, make_run_obs_builder(env), cfg)
                t0 = time.perf_counter()
                visits, root_value = mcts.run(
                    combat,
                    root_mask115=mask[:COMBAT_ACTIONS].astype(bool),
                    base_seed=(seed * 8191 + steps) & 0x7FFFFFFF,
                )
                mcts_time += time.perf_counter() - t0
                obs_buf.append(np.asarray(obs, dtype=np.float32))
                mask_buf.append(mask.astype(bool))
                visit_buf.append(visits.astype(np.float32))
                value_buf.append(np.float32(root_value))
            # Execute the POLICY's sampled action: states stay policy-visited
            # (the spec's "relabel policy-visited states with search").
            action, _ = model.predict(obs, action_masks=mask, deterministic=False)
            obs, _, term, trunc, _ = env.step(int(action))
            done = term or trunc
            steps += 1
        episodes += 1
        if episodes % 5 == 0:
            print(
                f"[collect w{worker_id}] ep {episodes}: {len(obs_buf)}/{quota} "
                f"decisions ({mcts_time:.0f}s in MCTS)",
                flush=True,
            )

    shard = Path(args["out_dir"]) / f"exit_shard_{worker_id:02d}.npz"
    np.savez_compressed(
        shard,
        obs=np.stack(obs_buf) if obs_buf else np.zeros((0, 1), np.float32),
        masks=np.stack(mask_buf) if mask_buf else np.zeros((0, 1), bool),
        visits=np.stack(visit_buf) if visit_buf else np.zeros((0, 1), np.float32),
        values=np.asarray(value_buf, dtype=np.float32),
    )
    return {
        "worker_id": worker_id,
        "shard": str(shard),
        "decisions": len(obs_buf),
        "episodes": episodes,
        "mcts_seconds": round(mcts_time, 1),
        "cache_hit_rate": round(
            evaluator.cache_hits / max(evaluator.cache_hits + evaluator.cache_misses, 1), 3
        ),
    }


def run_collect(args) -> list[str]:
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    n_workers = max(1, args.workers)
    quota = (args.decisions + n_workers - 1) // n_workers
    worker_args = [
        dict(
            worker_id=i,
            checkpoint=args.checkpoint,
            out_dir=str(out_dir),
            ascension=args.ascension,
            max_act_count=args.max_act_count,
            sims=args.sims,
            determinizations=args.determinizations,
            dirichlet_eps=args.dirichlet_eps,
            loss_value=args.loss_value,
            quota=quota,
            seed_base=args.seed_base + 5_000_000,  # disjoint from eval seeds
            max_minutes=args.max_minutes,
        )
        for i in range(n_workers)
    ]
    t0 = time.perf_counter()
    if n_workers == 1:
        results = [_collect_worker(worker_args[0])]
    else:
        ctx = mp.get_context("spawn")
        with ctx.Pool(n_workers) as pool:
            results = pool.map(_collect_worker, worker_args)
    wall = time.perf_counter() - t0
    total = sum(r["decisions"] for r in results)
    print(f"\n[collect] {total} labeled decisions in {wall/60:.1f} min")
    for r in results:
        print(f"  w{r['worker_id']}: {r['decisions']} decisions, "
              f"{r['episodes']} episodes, cache hit {r['cache_hit_rate']:.0%}")
    (out_dir / "collect_stats.json").write_text(
        json.dumps({"wall_minutes": wall / 60, "results": results}, indent=2),
        encoding="utf-8",
    )
    return [r["shard"] for r in results]


# ---------------------------------------------------------------------------
# distill mode
# ---------------------------------------------------------------------------

def run_distill(args, shards: list[str] | None = None) -> str:
    from sts2_env.search.distill import distill

    out_dir = Path(args.out_dir)
    if shards is None:
        shards = sorted(str(p) for p in out_dir.glob("exit_shard_*.npz"))
    if not shards:
        raise SystemExit(f"no exit_shard_*.npz in {out_dir}")

    obs, masks, visits, values = [], [], [], []
    for s in shards:
        with np.load(s) as z:
            if len(z["values"]) == 0:
                continue
            obs.append(z["obs"])
            masks.append(z["masks"])
            visits.append(z["visits"])
            values.append(z["values"])
    obs = np.concatenate(obs)
    masks = np.concatenate(masks)
    visits = np.concatenate(visits)
    values = np.concatenate(values)
    print(f"[distill] {len(obs)} labeled decisions from {len(shards)} shards "
          f"(mean root value {values.mean():+.3f})")

    from sb3_contrib import MaskablePPO

    model = MaskablePPO.load(args.checkpoint, device=args.device)
    stats = distill(
        model, obs, masks, visits, values,
        epochs=args.epochs, lr=args.lr, batch_size=args.batch_size,
        value_coef=args.value_coef, seed=0,
    )
    out_path = out_dir / "distilled"
    model.save(str(out_path))
    meta = {
        "checkpoint": args.checkpoint,
        "decisions": int(len(obs)),
        "epochs": args.epochs,
        "lr": args.lr,
        "value_coef": args.value_coef,
        "stats": stats.__dict__,
    }
    (out_dir / "distill_stats.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")
    print(f"[distill] saved {out_path}.zip")
    return str(out_path) + ".zip"


# ---------------------------------------------------------------------------
# eval mode (GO/NO-GO)
# ---------------------------------------------------------------------------

def _play_episode(env, model, seed: int, mcts_cfg=None, evaluator=None) -> dict:
    """One deterministic episode; if ``mcts_cfg`` is given, MCTS argmax
    replaces the policy on non-forced combat decisions."""
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS, CombatMCTS, make_run_obs_builder,
    )

    obs, info = env.reset(seed=seed)
    done = False
    steps = 0
    n_mcts = 0
    n_disagree = 0
    mcts_time = 0.0
    while not done and steps < MAX_EPISODE_STEPS:
        mask = env.action_masks()
        action = None
        if mcts_cfg is not None and _combat_decision(env, mask):
            combat = env._mgr.get_combat_state()
            mcts = CombatMCTS(evaluator, make_run_obs_builder(env), mcts_cfg)
            t0 = time.perf_counter()
            visits, _ = mcts.run(
                combat,
                root_mask115=mask[:COMBAT_ACTIONS].astype(bool),
                base_seed=(seed * 8191 + steps) & 0x7FFFFFFF,
            )
            mcts_time += time.perf_counter() - t0
            action = int(np.argmax(visits))
            pol_action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            n_mcts += 1
            n_disagree += int(action != int(pol_action))
        if action is None:
            pred, _ = model.predict(obs, action_masks=mask, deterministic=True)
            action = int(pred)
        obs, _, term, trunc, info = env.step(action)
        done = term or trunc
        steps += 1
    return {
        "seed": seed,
        "won": bool(info.get("won", False)),
        "floor": int(info.get("floor", 0)),
        "act": int(info.get("act", 0)),
        "steps": steps,
        "mcts_decisions": n_mcts,
        "mcts_disagree": n_disagree,
        "mcts_seconds": round(mcts_time, 1),
    }


def _eval_worker(args: dict) -> list[dict]:
    from sts2_env.search.combat_mcts import MCTSConfig, SB3PolicyEvaluator

    model = _load_cpu_model(args["checkpoint"])
    env = _make_env(args["ascension"], args["max_act_count"])
    evaluator = SB3PolicyEvaluator(model)
    cfg = MCTSConfig(
        n_simulations=args["sims"],
        n_determinizations=args["determinizations"],
        dirichlet_eps=0.0,
        loss_value=args["loss_value"],
    )
    records = []
    for seed in args["seeds"]:
        pol = _play_episode(env, model, seed)
        pol["arm"] = "policy"
        mcts = _play_episode(env, model, seed, mcts_cfg=cfg, evaluator=evaluator)
        mcts["arm"] = "mcts"
        records.extend([pol, mcts])
        print(
            f"[eval w{args['worker_id']}] seed {seed}: "
            f"policy floor {pol['floor']} won={pol['won']} | "
            f"mcts floor {mcts['floor']} won={mcts['won']} "
            f"({mcts['mcts_decisions']} searched, "
            f"{mcts['mcts_disagree']} overrides, {mcts['mcts_seconds']}s)",
            flush=True,
        )
    return records


def _summarize_arm(records: list[dict], arm: str) -> dict:
    rs = [r for r in records if r["arm"] == arm]
    floors = [r["floor"] for r in rs]
    return {
        "arm": arm,
        "episodes": len(rs),
        "wins": sum(r["won"] for r in rs),
        "win_rate": sum(r["won"] for r in rs) / max(len(rs), 1),
        "mean_floor": float(np.mean(floors)) if floors else 0.0,
        "mean_act": float(np.mean([r["act"] for r in rs])) if rs else 0.0,
    }


def run_eval(args) -> None:
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    n_workers = max(1, args.workers)
    seeds = [args.seed_base + i for i in range(args.episodes)]
    chunks = [seeds[i::n_workers] for i in range(n_workers)]
    worker_args = [
        dict(
            worker_id=i,
            checkpoint=args.checkpoint,
            ascension=args.ascension,
            max_act_count=args.max_act_count,
            sims=args.sims,
            determinizations=args.determinizations,
            loss_value=args.loss_value,
            seeds=chunk,
        )
        for i, chunk in enumerate(chunks) if chunk
    ]
    t0 = time.perf_counter()
    if n_workers == 1:
        all_records = _eval_worker(worker_args[0])
    else:
        ctx = mp.get_context("spawn")
        with ctx.Pool(len(worker_args)) as pool:
            all_records = [r for rs in pool.map(_eval_worker, worker_args) for r in rs]
    wall = time.perf_counter() - t0

    pol = _summarize_arm(all_records, "policy")
    mct = _summarize_arm(all_records, "mcts")
    mcts_rs = [r for r in all_records if r["arm"] == "mcts"]
    n_dec = sum(r["mcts_decisions"] for r in mcts_rs)
    n_dis = sum(r["mcts_disagree"] for r in mcts_rs)
    sec = sum(r["mcts_seconds"] for r in mcts_rs)

    print("\n" + "=" * 68)
    print(f"GO/NO-GO eval: {args.episodes} episodes, same seeds, "
          f"A{args.ascension} acts 1-{args.max_act_count}, "
          f"MCTS-{args.sims} ({wall/60:.1f} min wall)")
    print("=" * 68)
    print(f"{'arm':<10} {'wins':>6} {'win_rate':>9} {'mean_floor':>11} {'mean_act':>9}")
    for s in (pol, mct):
        print(f"{s['arm']:<10} {s['wins']:>6} {s['win_rate']:>9.1%} "
              f"{s['mean_floor']:>11.2f} {s['mean_act']:>9.2f}")
    print(f"\nMCTS: {n_dec} searched decisions, "
          f"{n_dis} policy overrides ({n_dis / max(n_dec, 1):.0%}), "
          f"{sec / max(n_dec, 1) * 1e3:.0f} ms/decision")
    delta = mct["mean_floor"] - pol["mean_floor"]
    print(f"\nDelta mean floors (mcts - policy): {delta:+.2f} | "
          f"delta wins: {mct['wins'] - pol['wins']:+d}")
    (out_dir / "go_no_go.json").write_text(
        json.dumps(
            {"policy": pol, "mcts": mct, "records": all_records,
             "sims": args.sims, "episodes": args.episodes,
             "wall_minutes": wall / 60},
            indent=2,
        ),
        encoding="utf-8",
    )
    print(f"[eval] wrote {out_dir / 'go_no_go.json'}")


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def main() -> None:
    p = argparse.ArgumentParser(description="Expert Iteration: MCTS collect / distill / GO-NO-GO eval")
    p.add_argument("--mode", choices=["collect", "distill", "eval", "all"], default="all")
    p.add_argument("--checkpoint", required=True, help="MaskablePPO zip (policy source)")
    p.add_argument("--out-dir", default="output/exit", help="Shards + distilled zip + reports")
    p.add_argument("--ascension", type=int, default=0)
    p.add_argument("--max-act-count", type=int, default=2)
    p.add_argument("--sims", type=int, default=DEFAULT_SIMS)
    p.add_argument("--determinizations", type=int, default=DEFAULT_DETERMINIZATIONS)
    p.add_argument("--loss-value", type=float, default=DEFAULT_LOSS_VALUE,
                   help="MCTS terminal value for player death (default -1.5: "
                        "must sit BELOW the value net's alive-state range, "
                        "which is ~-1.1..-1.35 on the PBRS-trained scale; "
                        "-1.0 would make search prefer dying)")
    p.add_argument("--workers", type=int, default=4)
    p.add_argument("--seed-base", type=int, default=DEFAULT_SEED_BLOCK)
    # collect
    p.add_argument("--decisions", type=int, default=DEFAULT_DECISIONS)
    p.add_argument("--dirichlet-eps", type=float, default=DEFAULT_DIRICHLET_EPS)
    p.add_argument("--max-minutes", type=float, default=120.0,
                   help="Per-worker wall-clock cap for collection")
    # distill
    p.add_argument("--epochs", type=int, default=3)
    p.add_argument("--lr", type=float, default=1.0e-4)
    p.add_argument("--batch-size", type=int, default=1024)
    p.add_argument("--value-coef", type=float, default=0.5)
    p.add_argument("--device", default="cuda")
    # eval
    p.add_argument("--episodes", type=int, default=DEFAULT_EVAL_EPISODES)
    args = p.parse_args()

    if args.mode == "eval":
        run_eval(args)
    elif args.mode == "collect":
        run_collect(args)
    elif args.mode == "distill":
        run_distill(args)
    else:
        shards = run_collect(args)
        run_distill(args, shards)


if __name__ == "__main__":
    main()
