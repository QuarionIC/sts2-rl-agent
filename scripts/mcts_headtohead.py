#!/usr/bin/env python3
"""Paired policy-vs-MCTS head-to-head on the encounters that actually kill runs.

The 200-episode forensics (``diagnose_deaths.py``) put the ceiling in combat
execution: elites are 79% fatal per visit and cost ~46 HP, and Gremlin Nob /
Lagavulin / Fossil Stalker / Bygone Effigy lead the death table. Before
spending real compute on an ExIt cycle, this answers the prerequisite
question: does determinized-PUCT search over the exact simulator actually beat
the raw policy on *those* states?

The design is paired, which is what makes it cheap and sharp. Full runs are
rolled out under the policy; every time a combat begins whose enemy lineup
matches a target, the live CombatState is snapshotted with ``clone_combat``.
Each snapshot is then played to completion TWICE from the identical starting
state -- once by policy argmax, once by MCTS visit-count argmax -- so the two
controllers face the same deck, hand, HP, relics and enemy roll. Differences
are attributable to action selection and nothing else.

Playouts run directly on cloned CombatStates via ``apply_combat_action`` (the
same primitive MCTS searches with) rather than through the env, so no live
run state is ever touched.

Example
-------
    python scripts/mcts_headtohead.py --episodes 60 --sims 96 --determinizations 8
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import time
from collections import defaultdict
from pathlib import Path

import numpy as np

DEFAULT_CKPT_DIR = Path("output/necrobinder_scratch/G1")

#: Encounters leading the death table at the 15.75M checkpoint. Matched as a
#: substring against the sorted monster_id lineup so multi-enemy variants of
#: the same fight (gremlin packs, gardener stacks) all qualify.
DEFAULT_TARGETS = [
    "EXORDIUM_GREMLIN_NOB",
    "EXORDIUM_LAGAVULIN",
    "FOSSIL_STALKER",
    "BYGONE_EFFIGY",
    "BYRDONIS",
    "TERROR_EEL",
]


def latest_checkpoint(ckpt_dir: Path) -> Path:
    candidates = sorted(ckpt_dir.glob("ckpt_*.zip"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        candidates = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return candidates[-1]


def lineup(combat) -> str:
    names = []
    for enemy in getattr(combat, "enemies", []) or []:
        names.append(str(getattr(enemy, "monster_id", None) or type(enemy).__name__))
    return " + ".join(sorted(names))


def outcome(combat) -> tuple[bool, float]:
    """(survived, hp_remaining) for a finished combat."""
    player = combat.primary_player
    if player is None:
        return False, 0.0
    return bool(player.is_alive), float(max(player.current_hp, 0))


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--checkpoint", default=None)
    parser.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    parser.add_argument("--episodes", type=int, default=60,
                        help="Full runs to harvest snapshots from")
    parser.add_argument("--seed-base", type=int, default=40_000_000)
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--acts", type=int, default=2)
    parser.add_argument("--sims", type=int, default=96)
    parser.add_argument("--determinizations", type=int, default=8)
    parser.add_argument("--c-puct", type=float, default=1.5)
    parser.add_argument("--max-snapshots", type=int, default=40)
    parser.add_argument("--playout-cap", type=int, default=600)
    parser.add_argument("--targets", nargs="*", default=None,
                        help=f"Substrings to match (default: {DEFAULT_TARGETS})")
    parser.add_argument("--all-elites", action="store_true",
                        help="Snapshot every ELITE combat instead of matching names")
    parser.add_argument("--json-out", default="output/mcts_headtohead.jsonl")
    args = parser.parse_args()

    targets = args.targets if args.targets is not None else DEFAULT_TARGETS

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.action_space import get_action_mask
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.gym_env.run_env import _LAYOUT
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS,
        MCTSConfig,
        CombatMCTS,
        SB3PolicyEvaluator,
        apply_combat_action,
        clone_combat,
        make_run_obs_builder,
        mcts_action_distribution,
    )

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint : {ckpt}")
    print(f"search     : {args.sims} sims x {args.determinizations} determinizations, "
          f"c_puct {args.c_puct}")
    print(f"targets    : {'ALL ELITES' if args.all_elites else ', '.join(targets)}")

    model = MaskablePPO.load(str(ckpt), device="cpu")
    evaluator = SB3PolicyEvaluator(model)

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        max_act_count=args.acts,
    )
    env.set_shaping_scale(0.0)

    def policy_action(obs: np.ndarray, mask115: np.ndarray) -> int:
        """Greedy policy action restricted to the legal combat slice."""
        full = np.zeros(int(model.policy.action_space.n), dtype=bool)
        full[_LAYOUT.combat_start:_LAYOUT.combat_start + COMBAT_ACTIONS] = mask115
        act, _ = model.predict(obs, action_masks=full, deterministic=True)
        local = int(act) - _LAYOUT.combat_start
        if not (0 <= local < COMBAT_ACTIONS) or not mask115[local]:
            legal = np.flatnonzero(mask115)
            return int(legal[0]) if legal.size else 0
        return local

    # ---------------- harvest snapshots under the policy ----------------
    snapshots: list[dict] = []
    print(f"\nharvesting snapshots from {args.episodes} policy rollouts ...")
    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        seen_combat_ids: set[int] = set()
        while not (done or trunc) and steps < 3000:
            if mgr is not None and mgr.phase == RunManager.PHASE_COMBAT:
                combat = mgr.get_combat_state()
                if combat is not None and not combat.is_over and id(combat) not in seen_combat_ids:
                    seen_combat_ids.add(id(combat))
                    room = getattr(getattr(mgr, "_current_room_type", None), "name", "?")
                    line = lineup(combat)
                    match = (args.all_elites and room == "ELITE") or any(
                        t in line for t in targets
                    )
                    if match and len(snapshots) < args.max_snapshots:
                        snapshots.append({
                            "seed": args.seed_base + i,
                            "floor": int(info.get("floor", 0)),
                            "room": room,
                            "lineup": line,
                            "entry_hp": float(combat.primary_player.current_hp),
                            "combat": clone_combat(combat),
                            "obs_builder": make_run_obs_builder(env),
                        })
            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            obs, reward, done, trunc, info = env.step(int(action))
            steps += 1
        if len(snapshots) >= args.max_snapshots:
            print(f"  reached --max-snapshots after {i + 1} episodes")
            break
        if (i + 1) % 20 == 0:
            print(f"  ... {i + 1}/{args.episodes} episodes, {len(snapshots)} snapshots")

    if not snapshots:
        print("\nNo matching encounters harvested -- widen --targets or raise --episodes.")
        return 1
    print(f"harvested {len(snapshots)} snapshots\n")

    # ---------------- paired playouts ----------------
    def playout(snap: dict, use_mcts: bool, decision_seed: int) -> dict:
        combat = clone_combat(snap["combat"])
        build = snap["obs_builder"]
        cfg = MCTSConfig(
            n_simulations=args.sims,
            n_determinizations=args.determinizations,
            c_puct=args.c_puct,
            dirichlet_eps=0.0,
            seed=decision_seed,
        )
        # Drive CombatMCTS directly rather than via mcts_action_distribution:
        # that helper falls back to the BARE obs builder for a raw CombatState
        # (run segment zeroed), which would hand search a strictly poorer
        # observation than the policy controller gets and make the comparison
        # meaningless. Here both controllers see identical run-context obs.
        decisions = 0
        disagreements = 0
        while not combat.is_over and decisions < args.playout_cap:
            player = combat.primary_player
            if player is None or not player.is_alive:
                break
            mask115 = get_action_mask(combat).astype(bool)
            if not mask115.any():
                break
            greedy = policy_action(build(combat), mask115)
            if use_mcts:
                mcts = CombatMCTS(evaluator, build, cfg)
                visits, _ = mcts.run(
                    combat, root_mask115=mask115,
                    base_seed=decision_seed + decisions,
                )
                masked = visits * mask115
                a = int(np.argmax(masked)) if masked.sum() > 0 else greedy
                if a != greedy:
                    disagreements += 1
            else:
                a = greedy
            apply_combat_action(combat, a)
            decisions += 1
        survived, hp = outcome(combat)
        return {"survived": survived, "hp": hp, "decisions": decisions,
                "disagreements": disagreements,
                "capped": decisions >= args.playout_cap}

    records = []
    t0 = time.time()
    for k, snap in enumerate(snapshots):
        det_seed = 777_000 + k * 101
        pol = playout(snap, use_mcts=False, decision_seed=det_seed)
        mct = playout(snap, use_mcts=True, decision_seed=det_seed)
        rec = {
            "seed": snap["seed"], "floor": snap["floor"], "room": snap["room"],
            "lineup": snap["lineup"], "entry_hp": snap["entry_hp"],
            "policy": pol, "mcts": mct,
        }
        records.append(rec)
        flag = "=" if pol["survived"] == mct["survived"] else ("MCTS+" if mct["survived"] else "POL+")
        print(f"  [{k+1:>3}/{len(snapshots)}] {snap['room']:<7} hp{snap['entry_hp']:>5.0f} "
              f"pol {'LIVE' if pol['survived'] else 'DEAD'} hp{pol['hp']:>5.0f} | "
              f"mcts {'LIVE' if mct['survived'] else 'DEAD'} hp{mct['hp']:>5.0f} "
              f"d{mct['disagreements']}/{mct['decisions']}  {flag}  "
              f"{snap['lineup'][:44]}")

    # ---------------- summary ----------------
    n = len(records)
    pol_live = sum(r["policy"]["survived"] for r in records)
    mct_live = sum(r["mcts"]["survived"] for r in records)
    pol_hp = np.array([r["policy"]["hp"] for r in records], dtype=float)
    mct_hp = np.array([r["mcts"]["hp"] for r in records], dtype=float)

    # McNemar on the discordant pairs: the paired design's proper test.
    b = sum(1 for r in records if r["mcts"]["survived"] and not r["policy"]["survived"])
    c = sum(1 for r in records if r["policy"]["survived"] and not r["mcts"]["survived"])
    # Exact two-sided binomial on b of (b+c) under p=0.5.
    from math import comb
    nb = b + c
    if nb:
        tail = sum(comb(nb, i) for i in range(0, min(b, c) + 1)) / (2 ** nb)
        p_mcnemar = min(1.0, 2 * tail)
    else:
        p_mcnemar = 1.0

    print(f"\n=== HEAD-TO-HEAD ({n} paired combats, {time.time()-t0:.0f}s) ===")
    print(f"  policy survived : {pol_live}/{n} ({pol_live/n:.1%})  mean HP left {pol_hp.mean():.1f}")
    print(f"  MCTS   survived : {mct_live}/{n} ({mct_live/n:.1%})  mean HP left {mct_hp.mean():.1f}")
    print(f"  paired HP delta : {(mct_hp - pol_hp).mean():+.2f} HP/combat "
          f"(MCTS better in {int((mct_hp > pol_hp).sum())}, worse in {int((mct_hp < pol_hp).sum())})")
    print(f"  discordant pairs: MCTS-only survivals {b}, policy-only survivals {c}"
          f"  -> exact McNemar p={p_mcnemar:.4f}")
    tot_dec = sum(r["mcts"]["decisions"] for r in records)
    tot_dis = sum(r["mcts"]["disagreements"] for r in records)
    print(f"  search vs greedy: MCTS chose a DIFFERENT action on {tot_dis}/{tot_dec} "
          f"decisions ({tot_dis / max(tot_dec, 1):.1%})")
    if tot_dis == 0:
        print("  >> search never overrode the policy: priors dominate at this sim budget.")

    by_line: dict[str, list] = defaultdict(list)
    for r in records:
        by_line[r["lineup"]].append(r)
    print(f"\n=== BY ENCOUNTER ===")
    for line, rs in sorted(by_line.items(), key=lambda kv: -len(kv[1])):
        p = sum(x["policy"]["survived"] for x in rs)
        m = sum(x["mcts"]["survived"] for x in rs)
        dhp = np.mean([x["mcts"]["hp"] - x["policy"]["hp"] for x in rs])
        print(f"  n={len(rs):<3} pol {p}/{len(rs)}  mcts {m}/{len(rs)}  "
              f"dHP {dhp:+6.1f}   {line[:56]}")

    capped = sum(r["mcts"]["capped"] or r["policy"]["capped"] for r in records)
    if capped:
        print(f"\n  NOTE: {capped} playouts hit the {args.playout_cap}-decision cap")

    if args.json_out:
        Path(args.json_out).write_text(
            "\n".join(json.dumps(r) for r in records), encoding="utf-8")
        print(f"\nwrote {n} records to {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
