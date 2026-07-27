#!/usr/bin/env python3
"""Matched planner-vs-policy benchmark on harvested decks.

Same starting combats, same seeds: the learned combat agent plays each fight
by greedy policy, the deterministic planner plans it with the EVAL ladder.
McNemar on the discordant pairs. Also reports damage taken per won combat --
the planner's objective is win with minimum HP loss, so HP retention is a
first-class result, not a tiebreak.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import sys
import time
from math import comb
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--combats", type=int, default=100)
    ap.add_argument("--combat-model", default="output/hier/combat/best_model.zip")
    ap.add_argument("--deck-file", default="output/hier_alt/r1/harvested_decks.pkl")
    ap.add_argument("--ladder", choices=["train", "eval"], default="eval")
    ap.add_argument("--seed-base", type=int, default=888_000)
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.action_space import get_action_mask
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS,
        apply_combat_action,
        clone_combat,
        make_bare_obs_builder,
    )
    from sts2_env.search.combat_planner import (
        EVAL_LADDER,
        TRAIN_LADDER,
        plan_combat_escalating,
    )
    from train_hierarchical import make_combat_env

    ladder = EVAL_LADDER if args.ladder == "eval" else TRAIN_LADDER
    model = MaskablePPO.load(args.combat_model, device="cpu")
    obs_builder = make_bare_obs_builder()
    env = make_combat_env(ascension=0, seed=0, deck_file=args.deck_file,
                          mix_progressive=0.0)

    N = args.combats
    pol_w = plan_w = 0
    pol_dmg, plan_dmg, times = [], [], []
    b = c_dis = 0
    for i in range(N):
        env.reset(seed=args.seed_base + i)
        root = env.combat
        entry = float(root.primary_player.current_hp)

        cl = clone_combat(root)
        n = 0
        while not cl.is_over and n < 400:
            p = cl.primary_player
            if p is None or not p.is_alive:
                break
            m = get_action_mask(cl).astype(bool)
            if not m.any():
                break
            a, _ = model.predict(obs_builder(cl), action_masks=m[:COMBAT_ACTIONS],
                                 deterministic=True)
            a = int(a)
            if not m[a]:
                a = int(np.flatnonzero(m)[0])
            apply_combat_action(cl, a)
            n += 1
        p = cl.primary_player
        pw = bool(p is not None and p.is_alive)
        if pw:
            pol_dmg.append(entry - float(max(0, p.current_hp)))

        t0 = time.time()
        res = plan_combat_escalating(root, ladder)
        times.append(time.time() - t0)
        if res.won:
            plan_dmg.append(res.entry_hp - res.final_hp)

        pol_w += pw
        plan_w += res.won
        if res.won and not pw:
            b += 1
        if pw and not res.won:
            c_dis += 1
        if (i + 1) % 10 == 0:
            print(f"  {i+1}/{N}: policy {pol_w} | planner {plan_w} "
                  f"(mean plan {np.mean(times):.1f}s)", flush=True)

    nb = b + c_dis
    p_mc = min(1.0, 2 * sum(comb(nb, k) for k in range(min(b, c_dis) + 1)) / 2 ** nb) if nb else 1.0
    print(f"\n=== MATCHED BENCHMARK ({N} combats, harvested decks, {args.ladder} ladder) ===")
    print(f"  policy  : {pol_w}/{N} ({pol_w/N:.1%})  mean damage when winning "
          f"{np.mean(pol_dmg) if pol_dmg else 0:.1f}")
    print(f"  planner : {plan_w}/{N} ({plan_w/N:.1%})  mean damage when winning "
          f"{np.mean(plan_dmg) if plan_dmg else 0:.1f}")
    print(f"  discordant: planner-only {b}, policy-only {c_dis} -> McNemar p={p_mc:.5f}")
    print(f"  plan time : mean {np.mean(times):.1f}s  p90 {np.percentile(times,90):.1f}s")
    print(f"  implied combats/run: policy {1/max(1-pol_w/N,1e-9):.1f} "
          f"vs planner {1/max(1-plan_w/N,1e-9):.1f}")
    print(f"  act-1 clear needs ~85.7%, act-2 ~92.9%")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
