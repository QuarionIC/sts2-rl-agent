#!/usr/bin/env python3
"""Does the cloned policy agree with the planner on ITS OWN states?

The question this settles
-------------------------
Behaviour-cloning the planner raised held-out agreement 40.2% -> 53.1% and
changed damage-per-win by -0.65 HP (95% CI [-2.12, +0.82], p=0.39). The policy
learned to imitate and did not learn to play. Two explanations, with very
different price tags:

1. OFF-DISTRIBUTION COLLAPSE -- the classic BC failure. The policy matches the
   expert on states the EXPERT reached and drifts everywhere else. The fix is
   DAgger: re-plan at policy-visited states, roughly one search per label
   instead of one per combat.

2. SEARCH IS IRREDUCIBLE -- the planner's edge is choosing a whole trajectory
   with lookahead, and no per-state imitation can capture it however the data
   is gathered. Then DAgger is money burnt and the route is inference-time
   search.

They are distinguished by WHERE agreement is measured. This plays the policy
and asks the planner, at each state the POLICY actually reached, what it would
have done.

    agreement on policy states ~ agreement on planner states  => (2)
    agreement on policy states << agreement on planner states => (1)

Usage
-----
    python -m scripts.bc_distribution_check --checkpoint distilled.zip \
        --combats 40 --deck-file ...
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


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--checkpoint", required=True)
    ap.add_argument("--combats", type=int, default=40)
    ap.add_argument("--deck-file", default=None)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--pools", nargs="+", default=["act1"])
    ap.add_argument("--seed-base", type=int, default=8_100_000)
    ap.add_argument("--max-minutes", type=float, default=45.0)
    ap.add_argument("--device", default="cpu")
    args = ap.parse_args(argv)

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv
    from sts2_env.search.combat_planner import TRAIN_LADDER, plan_combat_escalating

    import importlib.util

    spec = importlib.util.spec_from_file_location(
        "th", str(Path(__file__).resolve().parent / "train_hierarchical.py"))
    th = importlib.util.module_from_spec(spec)
    sys.modules["th"] = th
    spec.loader.exec_module(th)

    sampler = (th.HarvestedDeckSampler("Necrobinder", args.deck_file, 0.3)
               if args.deck_file else "progressive")
    env = RichSTS2CombatEnv(character_id="Necrobinder",
                            ascension_level=args.ascension,
                            encounter_pools=tuple(args.pools),
                            deck_sampler=sampler)
    model = MaskablePPO.load(args.checkpoint, device=args.device)

    agree = compared = 0
    t0 = time.time()
    for i in range(args.combats):
        if (time.time() - t0) / 60.0 > args.max_minutes:
            break
        obs, _ = env.reset(seed=args.seed_base + i)
        done = trunc = False
        steps = 0
        # The budget is checked in the INNER loop, not only per combat.
        #
        # Checking only at the top of the combat loop let one pathological
        # fight run unbounded: 2026-08-01 a run with --max-minutes 30 was still
        # going 46 minutes in with no log line for 39 of them, because a single
        # combat can take up to 300 steps and every contested step costs a full
        # planner search. A budget that only applies between combats is not a
        # budget.
        while not (done or trunc) and steps < 300:
            if (time.time() - t0) / 60.0 > args.max_minutes:
                print(f"  budget reached mid-combat {i}; stopping", flush=True)
                done = True
                break
            mask = env.action_masks()
            if mask.sum() <= 1:
                # Forced move: agreement here is free and would inflate the
                # number without saying anything about judgement.
                action, _ = model.predict(obs, action_masks=mask,
                                          deterministic=True)
                obs, _r, done, trunc, _ = env.step(int(action))
                steps += 1
                continue

            # What the PLANNER would do from the state the POLICY reached.
            try:
                result = plan_combat_escalating(env.combat, TRAIN_LADDER)
                expert = result.actions[0] if result.actions else None
            except Exception:
                expert = None

            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            action = int(action)
            if expert is not None and mask[expert]:
                compared += 1
                agree += int(action == expert)

            obs, _r, done, trunc, _ = env.step(action)
            steps += 1

        if (time.time() - t0) / 60.0 > args.max_minutes:
            break

        if (i + 1) % 10 == 0:
            rate = agree / max(compared, 1)
            print(f"  {i + 1}/{args.combats} combats, {compared} contested "
                  f"decisions, agreement {rate:.1%}", flush=True)

    print("\n=== AGREEMENT ON POLICY-VISITED STATES ===")
    print(f"  checkpoint : {args.checkpoint}")
    print(f"  contested decisions: {compared}")
    if compared:
        p = agree / compared
        z = 1.96
        n = compared
        c = (p + z * z / (2 * n)) / (1 + z * z / n)
        h = z * ((p * (1 - p) / n + z * z / (4 * n * n)) ** 0.5) / (1 + z * z / n)
        print(f"  agreement  : {p:.1%}  CI[{max(0, c-h):.1%}, {min(1, c+h):.1%}]")
    print(f"  wall       : {time.time() - t0:.0f}s")
    print("\n  Compare against held-out agreement on PLANNER states (53.1%).")
    print("  Much lower here => off-distribution collapse, DAgger is the fix.")
    print("  Similar here    => the gap is search itself, and DAgger will not")
    print("                     close it; inference-time search is the route.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
