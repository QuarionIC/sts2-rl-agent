#!/usr/bin/env python3
"""Trace both gate arms' ACTION SEQUENCES and find the first real difference.

Seed 10000013 reports `0 overrides` yet finishes policy floor 16 vs mcts floor 4.
That survived giving each arm a fresh env, and `mcts_purity_check --level run`
(which runs the search and DISCARDS it) is identical on this seed. So the
divergence needs the search's ANSWER to be taken, while the override counter
claims the answer never differed.

Suspicion: the counter is measuring the wrong thing. In `_play_episode` the
comparison is

    action     = argmax(visit_probs)            # over the 115 combat slice
    pol_action = model.predict(obs, mask, ...)   # computed AFTER the search ran

so `n_disagree` compares the search's choice against a policy evaluation made in
the post-search context, not against the action the POLICY-ONLY arm actually
took at that step. Anything that makes the two policy evaluations differ -- or
any action outside the 115 slice -- would be invisible to it.

This script replays both arms step by step and records, per step, the action each
arm took plus whether obs and mask matched. It reports the first index where the
ACTIONS differ, and whether the override counter noticed.
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
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--max-steps", type=int, default=4000)
    ap.add_argument("--json-out", default="output/gate_action_trace.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS,
        CombatMCTS,
        MCTSConfig,
        SB3PolicyEvaluator,
        make_run_obs_builder,
    )
    from sts2_env.run.run_manager import RunManager

    model = MaskablePPO.load(args.model, device="cpu")
    evaluator = SB3PolicyEvaluator(model)
    cfg = MCTSConfig(n_simulations=args.sims,
                     n_determinizations=args.determinizations,
                     dirichlet_eps=0.0)

    def mk():
        e = RichSTS2RunEnv(character_id="Necrobinder",
                           ascension_level=args.ascension,
                           max_act_count=args.max_act_count)
        e.set_shaping_scale(0.0)
        return e

    def play(use_mcts: bool):
        env = mk()
        obs, info = env.reset(seed=args.seed)
        mgr = env._mgr
        acts, notes = [], []
        done = tr = False
        n = 0
        overrides = 0
        while not (done or tr) and n < args.max_steps:
            mask = np.asarray(env.action_masks(), dtype=bool)
            if not mask.any():
                break
            action = None
            if use_mcts and mgr.phase == RunManager.PHASE_COMBAT \
                    and int(mask.sum()) > 1:
                combat = mgr.get_combat_state()
                if combat is not None and not combat.is_over:
                    mcts = CombatMCTS(evaluator, make_run_obs_builder(env), cfg)
                    visits, _ = mcts.run(
                        combat,
                        root_mask115=mask[:COMBAT_ACTIONS].astype(bool),
                        base_seed=(args.seed * 8191 + n) & 0x7FFFFFFF,
                    )
                    action = int(np.argmax(visits))
                    pol, _ = model.predict(obs, action_masks=mask,
                                           deterministic=True)
                    overrides += int(action != int(pol))
                    notes.append({"step": n, "mcts": action,
                                  "pol_after_search": int(pol),
                                  "visits_sum": float(visits.sum()),
                                  "legal": bool(mask[action]) if action < mask.size else False})
            if action is None:
                pred, _ = model.predict(obs, action_masks=mask,
                                        deterministic=True)
                action = int(pred)
            acts.append(int(action))
            obs, r, done, tr, info = env.step(int(action))
            n += 1
        return {
            "actions": acts, "notes": notes, "overrides": overrides,
            "floor": int(info.get("floor", 0)),
            "won": bool(info.get("won", False)),
            "steps": n,
        }

    print(f"seed {args.seed}: replaying both arms ...", flush=True)
    a = play(False)
    print(f"  policy arm: floor {a['floor']} steps {a['steps']}", flush=True)
    b = play(True)
    print(f"  mcts   arm: floor {b['floor']} steps {b['steps']} "
          f"overrides {b['overrides']}", flush=True)

    first = None
    for i, (x, y) in enumerate(zip(a["actions"], b["actions"])):
        if x != y:
            first = i
            break
    if first is None and len(a["actions"]) != len(b["actions"]):
        first = min(len(a["actions"]), len(b["actions"]))

    print()
    if first is None:
        print("ACTION SEQUENCES IDENTICAL -- yet floors differ. The divergence "
              "is not in the\nactions at all; something else about the mcts arm "
              "changes the run.")
    else:
        print(f"FIRST DIFFERING ACTION at step {first}: "
              f"policy took {a['actions'][first]}, mcts took {b['actions'][first]}")
        note = next((n for n in b["notes"] if n["step"] == first), None)
        print(f"  mcts note at that step: {note}")
        print(f"  overrides COUNTED by the gate metric: {b['overrides']}")
        if b["overrides"] == 0:
            print()
            print("VERDICT: the arms DID take different actions, but the gate's")
            print("         override counter reported ZERO. The counter compares")
            print("         argmax(visits) against a policy evaluation made AFTER")
            print("         the search, not against the policy-only arm's action,")
            print("         so it cannot see this. The 'impossible' 0-override")
            print("         divergences were a BROKEN METRIC, not a broken sim.")

    out = {"seed": args.seed, "first_diff_step": first,
           "policy": {k: v for k, v in a.items() if k != "actions"},
           "mcts": {k: v for k, v in b.items() if k != "actions"},
           "policy_actions": a["actions"][:200],
           "mcts_actions": b["actions"][:200]}
    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(out, indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
