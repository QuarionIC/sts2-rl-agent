#!/usr/bin/env python3
"""Is a deterministic policy forward pass reproducible around an MCTS search?

The last open question from the gate investigation. On seed 10000013 the two
arms had taken 57 IDENTICAL actions, and then arm B's own policy returned action
29 where arm A's returned 1. Two explanations remain:

  (a) state diverged with no action differing (a leak the snapshots missed), or
  (b) the policy forward pass is not reproducible once a search has run through
      the same network.

This isolates (b) with no env stepping at all. At a real combat state:

  1. predict(obs, mask, deterministic=True)          -> a0
  2. run an MCTS search through the SAME network
  3. predict(obs, mask, deterministic=True)          -> a1   (identical inputs)
  4. repeat

If a0 != a1 the forward pass is context-dependent, which fully explains the
"impossible" divergences AND means the gate must evaluate the policy action
BEFORE searching. If a0 == a1 every time, the cause is (a) and the leak hunt
resumes.

Also logs the top-2 logit gap, because the failure mode this would imply is a
near-tied argmax flipping on last-bit noise -- rare, which matches 1 seed in 16.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True)
    ap.add_argument("--seed", type=int, default=10_000_013)
    ap.add_argument("--sims", type=int, default=48)
    ap.add_argument("--determinizations", type=int, default=8)
    ap.add_argument("--checks", type=int, default=25,
                    help="How many combat decisions to test")
    ap.add_argument("--json-out", default="output/predict_repro.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    import torch as th
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS,
        CombatMCTS,
        MCTSConfig,
        SB3PolicyEvaluator,
        make_run_obs_builder,
    )

    model = MaskablePPO.load(args.model, device="cpu")
    evaluator = SB3PolicyEvaluator(model)
    cfg = MCTSConfig(n_simulations=args.sims,
                     n_determinizations=args.determinizations,
                     dirichlet_eps=0.0)

    env = RichSTS2RunEnv(character_id="Necrobinder", ascension_level=0,
                         max_act_count=1)
    env.set_shaping_scale(0.0)
    obs, info = env.reset(seed=args.seed)
    mgr = env._mgr

    def logit_gap(o, m):
        """Top-2 masked logit gap -- how close the argmax was to flipping."""
        with th.no_grad():
            ot, _ = model.policy.obs_to_tensor(np.asarray(o))
            dist = model.policy.get_distribution(
                ot, action_masks=np.asarray(m, dtype=bool))
            lg = dist.distribution.logits.squeeze(0).cpu().numpy()
        finite = lg[np.isfinite(lg)]
        if finite.size < 2:
            return float("inf")
        s = np.sort(finite)[::-1]
        return float(s[0] - s[1])

    flips, checked, rows = 0, 0, []
    n = 0
    while checked < args.checks and n < 4000:
        mask = np.asarray(env.action_masks(), dtype=bool)
        if not mask.any():
            break
        if mgr.phase == RunManager.PHASE_COMBAT and int(mask.sum()) > 1:
            combat = mgr.get_combat_state()
            if combat is not None and not combat.is_over:
                obs_c = np.array(obs, copy=True)
                mask_c = mask.copy()
                a0, _ = model.predict(obs_c, action_masks=mask_c,
                                      deterministic=True)
                gap = logit_gap(obs_c, mask_c)
                mcts = CombatMCTS(evaluator, make_run_obs_builder(env), cfg)
                mcts.run(combat,
                         root_mask115=mask_c[:COMBAT_ACTIONS].astype(bool),
                         base_seed=(args.seed * 8191 + n) & 0x7FFFFFFF)
                # IDENTICAL inputs, after the search.
                a1, _ = model.predict(obs_c, action_masks=mask_c,
                                      deterministic=True)
                same = int(a0) == int(a1)
                checked += 1
                flips += (not same)
                rows.append({"step": n, "a_before": int(a0), "a_after": int(a1),
                             "same": same, "top2_logit_gap": round(gap, 6)})
                if not same:
                    print(f"  FLIP at step {n}: {int(a0)} -> {int(a1)}, "
                          f"top-2 logit gap {gap:.2e}", flush=True)
        pred, _ = model.predict(obs, action_masks=mask, deterministic=True)
        obs, r, done, tr, info = env.step(int(pred))
        n += 1
        if done or tr:
            break

    print(f"\nchecked {checked} combat decisions around a live search")
    print(f"argmax flips on IDENTICAL inputs: {flips}")
    if rows:
        gaps = [r["top2_logit_gap"] for r in rows if np.isfinite(r["top2_logit_gap"])]
        if gaps:
            print(f"top-2 logit gap: min {min(gaps):.2e}  median "
                  f"{np.median(gaps):.2e}")
    print()
    if flips:
        print("VERDICT: the forward pass is NOT reproducible around a search.")
        print("         That explains the 'impossible' 0-override divergences,")
        print("         and the gate must evaluate the policy action BEFORE")
        print("         searching for its override count to mean anything.")
    else:
        print("VERDICT: the forward pass IS reproducible here. The seed-10000013")
        print("         divergence is therefore state, not numerics -- resume the")
        print("         leak hunt with pile ORDER included in the snapshot (the")
        print("         cross-architecture bug was also hand order).")

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(
        {"_meta": vars(args), "checked": checked, "flips": flips, "rows": rows},
        indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
