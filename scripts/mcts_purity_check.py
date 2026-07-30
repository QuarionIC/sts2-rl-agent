#!/usr/bin/env python3
"""Does RUNNING the search perturb the live run, independently of its advice?

Motivation. The recorded ExIt GO/NO-GO verdict was a hard NO-GO, but its log is
internally inconsistent: on most seeds MCTS made ZERO overrides yet the arms
finished wildly apart (policy floor 13/16/19/26 vs MCTS floor 2/1/1/4). If
search never changes the chosen action, the two runs must be identical. They
were not, which points at the search CORRUPTING the live state rather than at
search being unhelpful -- and the module already has history here: a bare
deepcopy left monster-AI closures bound to the live creatures, and
"enemy block climbing 0 -> 196 across searches" was the symptom before
clone_combat was written.

If that fix is incomplete, every MCTS/ExIt result in this project measures
corruption, not decision quality, and an AlphaZero-style curriculum built on it
would be training on noise.

The experiment isolates it exactly. Two arms, same seed, same policy, same
actions:

  A  policy-only, deterministic
  B  identical, except MCTS is run at every non-forced combat decision and its
     recommendation is THROWN AWAY -- the policy's action is taken regardless

Arm B differs from A only in that search executed. Any divergence in floors is
therefore caused by the act of searching. Identical floors mean the search is
side-effect free and the NO-GO verdict stands on its merits.
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


def play(make_env, model, seeds, run_search: bool, n_sims: int,
         max_steps: int = 2000):
    """Play each combat; optionally run (and discard) MCTS at each decision.

    Scoped to RichSTS2CombatEnv rather than the full run: the corruption
    concern is entirely about cloning a live CombatState, the combat agent's
    115-wide action space matches this env directly (a run env would need mask
    slicing that is itself a confound), and a combat is ~30 decisions instead
    of ~250 so the check is cheap enough to run at a useful sim budget.
    """
    if run_search:
        from sts2_env.search.combat_mcts import (
            MCTSConfig,
            mcts_action_distribution,
        )

    out = []
    for seed in seeds:
        env = make_env()
        obs, info = env.reset(seed=seed)
        done = tr = False
        n = searched = 0
        t0 = time.time()
        while not (done or tr) and n < max_steps:
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            action = int(action)

            if run_search and legal.size > 1:
                combat = env.combat
                if combat is not None and not combat.is_over:
                    # Run the search and DISCARD it. The only purpose is to
                    # execute whatever side effects searching has.
                    try:
                        mcts_action_distribution(
                            combat, model, n_sims=n_sims,
                            config=MCTSConfig(seed=seed * 1000 + n),
                            root_mask=mask,
                            base_seed=seed * 1000 + n,
                        )
                        searched += 1
                    except Exception as exc:   # a crash is itself a finding
                        out.append({"seed": seed, "error": repr(exc)[:220]})
                        break
            else:
                pass

            obs, r, done, tr, info = env.step(action)
            n += 1
        else:
            pass
        if out and out[-1].get("seed") == seed and "error" in out[-1]:
            continue
        cb = env.combat
        p = cb.primary_player if cb is not None else None
        out.append({
            "seed": seed,
            "hp": int(p.current_hp) if p is not None else -1,
            "survived": bool(p is not None and p.is_alive),
            "enemy_hp": int(sum(max(0, e.current_hp) for e in cb.enemies))
                        if cb is not None else -1,
            "steps": n,
            "searched": searched,
            "wall_s": round(time.time() - t0, 1),
        })
    return out


def play_run(make_env, model, seeds, run_search: bool, n_sims: int,
             max_steps: int = 4000):
    """Same experiment at the RUN level -- the configuration the gate used.

    The combat-level check passing does not clear the gate's setup. The gate
    calls ``mcts_action_distribution(env, ...)`` with a RichSTS2RunEnv, which
    additionally builds run-level observations from the live env
    (``make_run_obs_builder``) and takes its root mask from the env's combat
    slice. That extra coupling to the live env is untested by the combat-level
    check, and it is precisely where "0 overrides yet floor 16 vs floor 1"
    would come from. A 157-action policy is required.
    """
    from sts2_env.run.run_manager import RunManager

    if run_search:
        from sts2_env.search.combat_mcts import (
            MCTSConfig,
            mcts_action_distribution,
        )

    out = []
    for seed in seeds:
        env = make_env()
        obs, info = env.reset(seed=seed)
        mgr = env._mgr
        done = tr = False
        n = searched = 0
        t0 = time.time()
        err = None
        while not (done or tr) and n < max_steps:
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            action = int(action)

            if (run_search and mgr.phase == RunManager.PHASE_COMBAT
                    and legal.size > 1):
                combat = mgr.get_combat_state()
                if combat is not None and not combat.is_over:
                    try:
                        mcts_action_distribution(
                            env, model, n_sims=n_sims,
                            config=MCTSConfig(seed=seed * 1000 + n),
                            base_seed=seed * 1000 + n,
                        )
                        searched += 1
                    except Exception as exc:
                        err = repr(exc)[:220]
                        break

            obs, r, done, tr, info = env.step(action)
            n += 1
        rec = {
            "seed": seed,
            "hp": int(mgr.run_state.player.current_hp),
            "survived": not bool(mgr.run_state.player.is_dead),
            "floor": int(info.get("floor", 0)),
            "won": bool(info.get("won", False)),
            "enemy_hp": 0,
            "steps": n,
            "searched": searched,
            "wall_s": round(time.time() - t0, 1),
        }
        if err:
            rec["error"] = err
        out.append(rec)
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True)
    ap.add_argument("--seeds", type=int, default=8)
    ap.add_argument("--seed-base", type=int, default=10_000_000)
    ap.add_argument("--n-sims", type=int, default=32)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--device", default="cpu")
    ap.add_argument("--level", choices=["combat", "run"], default="combat",
                    help="combat: bare CombatState search (needs a 115-action "
                         "model). run: search via the run env, the exact "
                         "configuration the failing ExIt gate used (needs a "
                         "157-action model).")
    ap.add_argument("--json-out", default="output/mcts_purity.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    seeds = [args.seed_base + i for i in range(args.seeds)]

    if args.level == "run":
        from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

        def fresh_env():
            e = RichSTS2RunEnv(character_id="Necrobinder",
                               ascension_level=args.ascension,
                               max_act_count=args.max_act_count)
            e.set_shaping_scale(0.0)
            return e
        runner = play_run
    else:
        from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

        def fresh_env():
            e = RichSTS2CombatEnv(character_id="Necrobinder",
                                  ascension_level=args.ascension,
                                  encounter_pools=("act1",),
                                  deck_sampler="progressive")
            e.set_shaping_scale(0.0)
            return e
        runner = play

    model = MaskablePPO.load(args.model, device=args.device)

    print(f"model  : {args.model}")
    print(f"seeds  : {seeds[0]}..{seeds[-1]}  n_sims={args.n_sims}\n")

    print("arm A: policy only ...", flush=True)
    a = runner(fresh_env, model, seeds, run_search=False, n_sims=args.n_sims)
    print("arm B: policy + search-then-discard ...", flush=True)
    b = runner(fresh_env, model, seeds, run_search=True, n_sims=args.n_sims)

    print(f"\n{'seed':>10} {'A hp':>6} {'B hp':>6} {'A surv':>7} {'B surv':>7} "
          f"{'A enHP':>7} {'B enHP':>7} {'searches':>9} {'same?':>6}")
    print("-" * 78)
    same = 0
    for ra, rb in zip(a, b):
        if "error" in rb:
            print(f"{rb['seed']:>10} {'':>8} {'CRASH':>8}  {rb['error'][:40]}")
            continue
        ok = (ra["hp"] == rb["hp"] and ra["survived"] == rb["survived"]
              and ra["enemy_hp"] == rb["enemy_hp"])
        same += ok
        print(f"{ra['seed']:>10} {ra['hp']:>6} {rb['hp']:>6} "
              f"{str(ra['survived']):>7} {str(rb['survived']):>7} "
              f"{ra['enemy_hp']:>7} {rb['enemy_hp']:>7} {rb['searched']:>9} "
              f"{'yes' if ok else 'NO':>6}")

    n = len([r for r in b if "error" not in r])
    print("-" * 60)
    print(f"identical outcomes: {same}/{n}")
    fa = float(np.mean([r["hp"] for r in a]))
    fb = float(np.mean([r["hp"] for r in b if "error" not in r]))
    print(f"mean end HP: A {fa:.2f}  B {fb:.2f}  (delta {fb - fa:+.2f})")
    sa = float(np.mean([r["survived"] for r in a]))
    sb = float(np.mean([r["survived"] for r in b if "error" not in r]))
    print(f"survival   : A {sa:.1%}  B {sb:.1%}")
    print()
    if same == n:
        print("VERDICT: search is SIDE-EFFECT FREE. The recorded NO-GO stands on")
        print("         its merits, and an ExIt/AlphaZero loop can be built on it.")
    else:
        print("VERDICT: RUNNING the search CHANGES the run even when its advice is")
        print("         discarded. Every MCTS/ExIt number in this project measures")
        print("         that corruption, not decision quality. Fix before building")
        print("         any curriculum on this search.")

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(
        {"_meta": vars(args), "arm_a": a, "arm_b": b,
         "identical": same, "n": n,
         "mean_hp_a": fa, "mean_hp_b": fb}, indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
