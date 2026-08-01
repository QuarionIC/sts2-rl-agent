#!/usr/bin/env python3
"""Record the beam planner playing whole combats, as behaviour-cloning data.

Why this and not more reward shaping
------------------------------------
Charging the combat agent for the HP a win costs works, and saturates. Paired
on shared seeds: +0.95 HP at w=2 (95% CI [+0.36, +1.53]) and +1.15 HP at w=14
(CI [+0.12, +2.17]) -- a sevenfold stronger signal bought ~0.2 HP, and the
effect was flat across 3M steps. The gap to the planner is 7.0 HP.

Meanwhile the two win fights EQUALLY often: 76.0% policy vs 73.5% planner over
200 matched combats, McNemar p=0.51. The planner is not better at winning, it
is better at winning CHEAPLY -- 10.2 damage per win against ~17.

That pattern says the missing ingredient is not the objective but the SEARCH.
The planner runs a whole-combat beam search over cloned states; a reactive
policy cannot discover multi-turn low-damage lines from a scalar at the end of
the episode however that scalar is weighted. So: clone the searcher.

What this produces
------------------
One npz shard per worker of ``(obs, mask, expert_action)`` triples, taken from
combats the planner WON -- a losing plan is not a demonstration worth copying.
Each plan yields roughly one label per decision, so a few hundred combats is
tens of thousands of labels.

Deliberately off-policy: these are the planner's states, not the policy's.
That is behaviour cloning, not full Expert Iteration, and it is the cheap half
-- one search per combat instead of one per visited state (~30s each on the
eval ladder). If cloned behaviour degrades off-distribution, the next step is a
DAgger loop that re-plans at policy-visited states; this is what tells us
whether that expense is warranted.

Usage
-----
    python -m scripts.collect_planner_combats --combats 400 --out output/planner_bc
"""

from __future__ import annotations

import argparse
import os
import sys
import time
from pathlib import Path

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))


def collect(args) -> dict:
    import sts2_env.events  # noqa: F401
    from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv
    from sts2_env.search.combat_planner import (
        EVAL_LADDER,
        TRAIN_LADDER,
        plan_combat_escalating,
    )

    ladder = EVAL_LADDER if args.ladder == "eval" else TRAIN_LADDER
    env = RichSTS2CombatEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        encounter_pools=tuple(args.pools),
        deck_sampler=_sampler(args),
    )

    obs_rows: list[np.ndarray] = []
    mask_rows: list[np.ndarray] = []
    act_rows: list[int] = []
    stats = {"combats": 0, "wins": 0, "labels": 0, "plan_s": 0.0,
             "rejected": 0, "damage": []}
    t0 = time.time()

    for i in range(args.combats):
        if (time.time() - t0) / 60.0 > args.max_minutes:
            break
        obs, _info = env.reset(seed=args.seed_base + i)
        combat = env.combat
        hp_start = combat.primary_player.current_hp

        t1 = time.time()
        try:
            result = plan_combat_escalating(combat, ladder)
        except Exception as exc:  # a failed search must not end the harvest
            print(f"  combat {i}: planner raised {type(exc).__name__}: {exc}",
                  flush=True)
            continue
        stats["plan_s"] += time.time() - t1
        stats["combats"] += 1

        if not result.won:
            # A losing line is not a demonstration. Keeping it would teach the
            # policy to reproduce the planner's failures, which are exactly the
            # fights where its judgement is worth least.
            stats["rejected"] += 1
            continue
        stats["wins"] += 1

        # Replay the plan against the env, recording the state the expert saw
        # at each decision. Replaying rather than trusting the planner's own
        # trace because the ENV is what training will feed the policy, and any
        # disagreement between them would be baked into the labels.
        ep_obs, ep_mask, ep_act = [], [], []
        ok = True
        for action in result.actions:
            mask = env.action_masks()
            if not mask[action]:
                # The plan and the env disagree about legality; taking it would
                # record a label the policy can never reproduce.
                ok = False
                break
            ep_obs.append(obs.astype(np.float32, copy=True))
            ep_mask.append(np.asarray(mask, dtype=bool).copy())
            ep_act.append(int(action))
            obs, _r, done, trunc, _info = env.step(int(action))
            if done or trunc:
                break
        if not ok:
            stats["rejected"] += 1
            continue

        player = env.combat.primary_player
        if player is not None and player.is_alive:
            stats["damage"].append(hp_start - player.current_hp)
        obs_rows.extend(ep_obs)
        mask_rows.extend(ep_mask)
        act_rows.extend(ep_act)
        stats["labels"] = len(act_rows)

        if stats["combats"] % 25 == 0:
            mean_plan = stats["plan_s"] / max(stats["combats"], 1)
            print(f"  {stats['combats']}/{args.combats} combats "
                  f"({stats['wins']} won, {stats['labels']} labels, "
                  f"mean plan {mean_plan:.1f}s)", flush=True)

    out = Path(args.out)
    out.mkdir(parents=True, exist_ok=True)
    shard = out / f"planner_bc_{args.seed_base}.npz"
    np.savez_compressed(
        shard,
        obs=np.asarray(obs_rows, dtype=np.float32),
        masks=np.packbits(np.asarray(mask_rows, dtype=bool), axis=1),
        actions=np.asarray(act_rows, dtype=np.int16),
        n_actions=np.int32(mask_rows[0].shape[0] if mask_rows else 0),
    )
    stats["shard"] = str(shard)
    stats["wall_s"] = round(time.time() - t0, 1)
    return stats


def _sampler(args):
    if not args.deck_file:
        return "progressive"
    import importlib.util

    spec = importlib.util.spec_from_file_location(
        "th", str(Path(__file__).resolve().parent / "train_hierarchical.py"))
    th = importlib.util.module_from_spec(spec)
    sys.modules["th"] = th
    spec.loader.exec_module(th)
    return th.HarvestedDeckSampler("Necrobinder", args.deck_file,
                                   args.mix_progressive)


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--combats", type=int, default=400)
    ap.add_argument("--out", default="output/planner_bc")
    ap.add_argument("--deck-file", default=None,
                    help="harvested decks; omit for the progressive sampler")
    ap.add_argument("--mix-progressive", type=float, default=0.3)
    ap.add_argument("--ladder", choices=["train", "eval"], default="train",
                    help="train is ~2-6s/plan, eval ~30s but is the ladder "
                         "measured at 10.2 damage per win")
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--pools", nargs="+", default=["act1"])
    ap.add_argument("--seed-base", type=int, default=7_000_000)
    ap.add_argument("--max-minutes", type=float, default=120.0)
    args = ap.parse_args(argv)

    stats = collect(args)
    dmg = stats.pop("damage", [])
    print("\n=== PLANNER BC COLLECTION ===")
    for key in ("combats", "wins", "rejected", "labels", "wall_s", "shard"):
        print(f"  {key:10} {stats.get(key)}")
    if dmg:
        print(f"  mean damage per won combat: {np.mean(dmg):.2f} "
              f"(planner benchmark: 10.2)")
    if stats.get("combats"):
        print(f"  mean plan time: {stats['plan_s'] / stats['combats']:.1f}s")
    return 0


if __name__ == "__main__":
    sys.exit(main())
