#!/usr/bin/env python3
"""Step two identical runs in lockstep and find the FIRST divergent action.

Where this sits in the chain of evidence:

* The recorded ExIt GO/NO-GO was a hard NO-GO whose log is self-contradictory:
  zero action overrides, yet arms finishing 13 vs 2 floors apart.
* ``mcts_purity_check --level combat``: clean, 4/4 identical.
* ``mcts_purity_check --level run``: FAILS, 3/4 -- seed 10000002 died at 0 HP
  without search and survived at 34 HP with search-then-discard.
* ``mcts_leak_localize``: across 82 searches, ZERO mutation of live HP, block,
  energy, piles, powers, enemy state, legal mask, every reachable counter-based
  ``Rng``, or numpy's and torch's global generators.

So the search neither corrupts state nor consumes shared randomness, yet the
trajectory still diverges on some seeds. The remaining candidate is the policy
itself: MCTS runs hundreds of extra forward passes through the same network, and
if that leaves torch in a slightly different numerical state, a near-tied argmax
can flip. One flipped action early cascades into a completely different run.

That distinction matters a great deal. State corruption would be a simulator
bug invalidating the SIM. Argmax nondeterminism is a MEASUREMENT bug: it
invalidates the gate's A/B comparison (the two arms are not controlled) while
leaving the simulator sound.

This script settles it by running both arms inside one process, stepping them
together, and reporting at the first differing action whether the observation
and legal mask were identical at that moment. Identical inputs with different
outputs isolates the nondeterminism to the forward pass.
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
    ap.add_argument("--seed", type=int, default=10_000_002,
                   help="Default is the seed that diverged in the purity check")
    ap.add_argument("--n-sims", type=int, default=16)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--max-steps", type=int, default=4000)
    ap.add_argument("--device", default="cpu")
    ap.add_argument("--json-out", default="output/mcts_lockstep.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import MCTSConfig, mcts_action_distribution

    model = MaskablePPO.load(args.model, device=args.device)

    def mk():
        e = RichSTS2RunEnv(character_id="Necrobinder",
                           ascension_level=args.ascension,
                           max_act_count=args.max_act_count)
        e.set_shaping_scale(0.0)
        return e

    ea, eb = mk(), mk()
    oa, _ = ea.reset(seed=args.seed)
    ob, _ = eb.reset(seed=args.seed)
    mga, mgb = ea._mgr, eb._mgr

    print(f"seed {args.seed}  n_sims {args.n_sims}")
    print("A = policy only | B = policy + search-then-discard\n", flush=True)

    rec: dict = {"seed": args.seed, "first_divergence": None, "steps": 0}
    da = db = ta = tb = False
    n = 0
    while n < args.max_steps and not (da or db or ta or tb):
        ma = np.asarray(ea.action_masks(), dtype=bool)
        mb = np.asarray(eb.action_masks(), dtype=bool)
        obs_same = bool(np.array_equal(np.asarray(oa), np.asarray(ob)))
        mask_same = bool(np.array_equal(ma, mb))
        if not ma.any() or not mb.any():
            break

        aa, _ = model.predict(oa, action_masks=ma, deterministic=True)
        # Arm B: run the search FIRST (so any effect it has is in play), then
        # predict -- the ordering the gate uses.
        if mgb.phase == RunManager.PHASE_COMBAT and int(mb.sum()) > 1:
            cb = mgb.get_combat_state()
            if cb is not None and not cb.is_over:
                mcts_action_distribution(
                    eb, model, n_sims=args.n_sims,
                    config=MCTSConfig(seed=args.seed * 1000 + n),
                    base_seed=args.seed * 1000 + n,
                )
        ab, _ = model.predict(ob, action_masks=mb, deterministic=True)
        aa, ab = int(aa), int(ab)

        if aa != ab or not obs_same or not mask_same:
            rec["first_divergence"] = {
                "step": n,
                "action_a": aa, "action_b": ab,
                "obs_identical": obs_same,
                "mask_identical": mask_same,
                "phase_a": mga.phase, "phase_b": mgb.phase,
                "hp_a": int(mga.run_state.player.current_hp),
                "hp_b": int(mgb.run_state.player.current_hp),
                "floor_a": int(mga.run_state.total_floor),
                "floor_b": int(mgb.run_state.total_floor),
            }
            print(json.dumps(rec["first_divergence"], indent=2))
            print()
            if obs_same and mask_same and aa != ab:
                print("VERDICT: IDENTICAL observation and IDENTICAL legal mask, "
                      "DIFFERENT action.")
                print("         The divergence is nondeterminism in the policy "
                      "forward pass,")
                print("         not a simulator bug. The recorded ExIt gate is "
                      "therefore an")
                print("         UNCONTROLLED comparison -- but the simulator is "
                      "sound.")
            elif not obs_same:
                print("VERDICT: observations already differ at the first "
                      "divergent action, so")
                print("         state diverged EARLIER than the action. The "
                      "search does affect")
                print("         simulator state by some route the snapshot did "
                      "not cover.")
            break

        oa, _, da, ta, _ = ea.step(aa)
        ob, _, db, tb, _ = eb.step(ab)
        n += 1

    rec["steps"] = n
    if rec["first_divergence"] is None:
        print(f"no divergence in {n} steps -- arms identical for this seed")
    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(rec, indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
