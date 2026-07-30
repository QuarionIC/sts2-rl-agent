#!/usr/bin/env python3
"""Expert Iteration with the BEAM PLANNER as the expert (AlphaZero, minus MCTS).

Why not MCTS
------------
``exit_distill.py`` uses determinized PUCT MCTS as the improvement operator,
which is AlphaZero's exact arrangement. Its GO/NO-GO gate returned a hard
NO-GO. ``mcts_purity_check.py`` was written to test whether that verdict was an
artifact of search corrupting the live state; at the combat level it is not
(4/4 identical outcomes with search run and discarded), so the search is
side-effect free and the NO-GO stands on its own merits at that level.

Meanwhile this project already has a search that demonstrably DOES beat the
learned policy in combat: the deterministic whole-combat beam planner. Measured
head to head, unbounded whole-combat search took 100 HP of damage across 6
seeds and won 5, versus 132 HP and 4 wins for the per-turn budgeted variant --
and the planner is what the live bridge uses when it plays well.

So this is the same AlphaZero-shaped loop with the operator swapped for one
that works on this problem:

    policy plays  ->  planner relabels the best action at each visited state
                  ->  train the net toward (planner action, realised return)
                  ->  stronger policy  ->  better states visited  ->  repeat

Targets
-------
* **Policy target** -- a one-hot (optionally label-smoothed) on the planner's
  chosen action. MCTS supplies a visit distribution; a deterministic search
  supplies an argmax, so the target is peaked. ``--smooth`` spreads a little
  mass over the remaining legal actions, which keeps the CE gradient from
  driving log-probs to -inf on states where the planner is merely
  breaking a near-tie.
* **Value target** -- the REALISED discounted return from that state under the
  current reward function, computed after the episode ends. This is AlphaZero's
  own choice (the game outcome), and it is what ties the distillation to the
  reward we actually train on: win +10, death -10 + 4*enemy_down, +1 per elite,
  PBRS shaping that telescopes to zero over an episode.

Shards are written in the format ``sts2_env.search.distill.distill`` consumes,
so the distillation step is shared with the MCTS path and not reimplemented.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
import time
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def collect(model, args) -> dict:
    """Play with the policy; relabel every combat decision with the planner."""
    import sts2_env.events  # noqa: F401

    from sts2_env.core.constants import ACTION_SPACE_SIZE as COMBAT_ACTIONS
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_planner import (
        EVAL_LADDER,
        TRAIN_LADDER,
        PlannedCombatController,
    )

    ladder = EVAL_LADDER if args.planner_ladder == "eval" else TRAIN_LADDER
    obs_buf, mask_buf, tgt_buf = [], [], []
    ret_buf: list[float] = []
    stats = {"episodes": 0, "decisions": 0, "agreements": 0,
             "planner_s": 0.0, "floors": [], "wins": 0}
    t_start = time.time()
    ep = 0

    while (len(obs_buf) < args.decisions
           and (time.time() - t_start) / 60.0 < args.max_minutes):
        env = RichSTS2RunEnv(character_id="Necrobinder",
                             ascension_level=args.ascension,
                             max_act_count=args.max_act_count)
        # Shaping ON: the value target is the return under the reward we train
        # on, and that includes the PBRS term.
        planner = PlannedCombatController(env, ladder=ladder)
        obs, info = env.reset(seed=args.seed_base + ep)
        mgr = env._mgr
        ep += 1

        ep_obs, ep_mask, ep_tgt, ep_rew, ep_step = [], [], [], [], []
        done = tr = False
        n = 0
        while not (done or tr) and n < args.max_steps:
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            in_combat = mgr.phase == RunManager.PHASE_COMBAT

            # The POLICY chooses what actually happens (on-policy state
            # distribution -- the whole point of expert iteration is to label
            # the states the policy visits, not the ones the expert prefers).
            action, _ = model.predict(obs, action_masks=mask, deterministic=False)
            action = int(action)

            if in_combat and legal.size > 1:
                t0 = time.time()
                try:
                    expert = int(planner.act(obs, mask))
                except Exception:
                    expert = None
                stats["planner_s"] += time.time() - t0
                # distill() expects visit_probs over the 115-wide COMBAT
                # slice, while masks are the full 157. combat_start == 0, so a
                # combat action's local index equals its full index; anything
                # at or beyond COMBAT_ACTIONS (player-select) has no slot in
                # the target and is skipped rather than silently folded in.
                if (expert is not None and 0 <= expert < COMBAT_ACTIONS
                        and mask[expert]):
                    legal_in_slice = legal[legal < COMBAT_ACTIONS]
                    tgt = np.zeros(COMBAT_ACTIONS, dtype=np.float32)
                    if args.smooth > 0.0 and legal_in_slice.size > 1:
                        tgt[legal_in_slice] = args.smooth / legal_in_slice.size
                        tgt[expert] += 1.0 - args.smooth
                    else:
                        tgt[expert] = 1.0
                    tgt /= tgt.sum()
                    ep_obs.append(np.asarray(obs, dtype=np.float32))
                    ep_mask.append(mask.astype(np.bool_))
                    ep_tgt.append(tgt)
                    ep_step.append(n)          # for exact value alignment
                    stats["decisions"] += 1
                    stats["agreements"] += int(expert == action)

            obs, r, done, tr, info = env.step(action)
            ep_rew.append(float(r))
            n += 1

        # Realised discounted return-to-go, aligned to the recorded decisions.
        # ep_rew is per env-step; a recorded decision at step i is credited the
        # return from i onward, which is exactly the Monte-Carlo value target.
        if ep_obs:
            g = 0.0
            ret_by_step = [0.0] * len(ep_rew)
            for i in range(len(ep_rew) - 1, -1, -1):
                g = ep_rew[i] + args.gamma * g
                ret_by_step[i] = g
            # Align by the RECORDED STEP INDEX. Crediting by rank instead
            # would assign decision j the return of step j, but recorded
            # decisions sit at scattered steps (only in-combat, only when more
            # than one action was legal), so every value target would be wrong.
            for o, m, tg, s in zip(ep_obs, ep_mask, ep_tgt, ep_step):
                if s < len(ret_by_step):
                    obs_buf.append(o)
                    mask_buf.append(m)
                    tgt_buf.append(tg)
                    ret_buf.append(ret_by_step[s])

        stats["episodes"] += 1
        stats["floors"].append(int(info.get("floor", 0)))
        stats["wins"] += int(bool(info.get("won", False)))
        if stats["episodes"] % 5 == 0:
            print(f"  [collect] ep {stats['episodes']}: {len(obs_buf)}/"
                  f"{args.decisions} decisions, planner "
                  f"{stats['planner_s']/max(stats['decisions'],1):.2f}s/decision, "
                  f"agree {stats['agreements']/max(stats['decisions'],1):.1%}",
                  flush=True)

    out = Path(args.out_dir)
    out.mkdir(parents=True, exist_ok=True)
    shard = out / "planner_shard_00.npz"
    np.savez_compressed(
        shard,
        obs=np.asarray(obs_buf, dtype=np.float32),
        masks=np.asarray(mask_buf, dtype=np.bool_),
        visit_probs=np.asarray(tgt_buf, dtype=np.float32),
        root_values=np.asarray(ret_buf, dtype=np.float32),
    )
    stats["shard"] = str(shard)
    stats["n"] = len(obs_buf)
    stats["mean_floor"] = float(np.mean(stats["floors"])) if stats["floors"] else 0.0
    stats["agreement_rate"] = stats["agreements"] / max(stats["decisions"], 1)
    stats["planner_s_per_decision"] = stats["planner_s"] / max(stats["decisions"], 1)
    stats.pop("floors")
    return stats


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--checkpoint", required=True, help="157-action MaskablePPO zip")
    ap.add_argument("--out-dir", default="output/planner_exit")
    ap.add_argument("--mode", choices=["collect", "distill", "all"], default="all")
    ap.add_argument("--decisions", type=int, default=20000)
    ap.add_argument("--max-minutes", type=float, default=120.0)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--max-steps", type=int, default=4000)
    ap.add_argument("--seed-base", type=int, default=5_000_000,
                    help="TRAIN seeds. Deliberately not the 10_000_000 eval "
                         "block -- collecting on eval seeds would leak them.")
    ap.add_argument("--gamma", type=float, default=0.99)
    ap.add_argument("--smooth", type=float, default=0.05,
                    help="Label smoothing over legal actions. A deterministic "
                         "expert gives a one-hot target; a little mass "
                         "elsewhere stops CE driving log-probs to -inf when the "
                         "planner was breaking a near-tie.")
    ap.add_argument("--planner-ladder", choices=["train", "eval"], default="train")
    ap.add_argument("--epochs", type=int, default=3)
    ap.add_argument("--lr", type=float, default=1e-4)
    ap.add_argument("--batch-size", type=int, default=1024)
    ap.add_argument("--value-coef", type=float, default=0.5)
    ap.add_argument("--device", default="cuda")
    args = ap.parse_args()

    from sb3_contrib import MaskablePPO

    out = Path(args.out_dir)
    out.mkdir(parents=True, exist_ok=True)
    report = out / "planner_exit_report.json"
    rec: dict = {"_args": vars(args)}

    model = MaskablePPO.load(args.checkpoint, device=args.device)
    print(f"checkpoint: {args.checkpoint}  (action space "
          f"{model.policy.action_space.n})", flush=True)

    if args.mode in ("collect", "all"):
        print("=== collect: policy plays, planner relabels ===", flush=True)
        st = collect(model, args)
        rec["collect"] = st
        print(json.dumps(st, indent=2), flush=True)
        if st["n"] == 0:
            print("no decisions collected -- nothing to distil")
            report.write_text(json.dumps(rec, indent=2), encoding="utf-8")
            return 2

    if args.mode in ("distill", "all"):
        from sts2_env.search.distill import distill

        shards = sorted(out.glob("planner_shard_*.npz"))
        if not shards:
            print(f"no shards in {out}")
            return 2
        obs = np.concatenate([np.load(s)["obs"] for s in shards])
        masks = np.concatenate([np.load(s)["masks"] for s in shards])
        probs = np.concatenate([np.load(s)["visit_probs"] for s in shards])
        vals = np.concatenate([np.load(s)["root_values"] for s in shards])
        print(f"=== distil: {len(obs)} decisions, value target range "
              f"[{vals.min():.2f}, {vals.max():.2f}] ===", flush=True)
        stats = distill(model, obs, masks, probs, vals,
                        epochs=args.epochs, lr=args.lr,
                        batch_size=args.batch_size,
                        value_coef=args.value_coef)
        rec["distill"] = getattr(stats, "__dict__", str(stats))
        print(rec["distill"], flush=True)
        dest = out / "distilled.zip"
        model.save(str(dest))
        rec["distilled"] = str(dest)
        print(f"saved {dest}", flush=True)

    report.write_text(json.dumps(rec, indent=2, default=str), encoding="utf-8")
    print(f"wrote {report}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
