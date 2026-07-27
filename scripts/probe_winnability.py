#!/usr/bin/env python3
"""Is the fight lost at combat start, or is the policy losing it?

The MCTS head-to-head produced a hard negative: raising the search budget from
96 to 1200 simulations moved the action choice on only 4 of 183 decisions and
changed zero outcomes. The natural reading is "budget too small", but PUCT has
a second failure mode that looks identical: if the value net rates *every*
continuation as a loss, all Q values collapse together, visits distribute by
prior, and argmax(visits) reproduces the policy exactly no matter how many
simulations run. Search cannot rank lines it believes are all lost.

Those two causes imply opposite interventions, so this separates them. From
each snapshotted combat it runs many STOCHASTIC playouts -- policy sampling
across a temperature ladder, plus uniform-random over legal actions -- and asks
whether *any* line survives.

  - Some lines survive  => the position is winnable and action selection is at
    fault. Search/ExIt is the right lever (and needs a budget that bites).
  - No line survives    => the position is already lost when combat begins. The
    bottleneck is upstream: deck construction, upgrades, relics, routing. No
    amount of in-combat search can recover it.

The deck, relics and HP at each snapshot are dumped alongside so an unwinnable
verdict can be read against what the run actually brought to the fight.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
from collections import Counter
from pathlib import Path

import numpy as np

DEFAULT_CKPT_DIR = Path("output/necrobinder_scratch/G1")
DEFAULT_TARGETS = [
    "EXORDIUM_GREMLIN_NOB", "EXORDIUM_LAGAVULIN", "FOSSIL_STALKER",
    "BYGONE_EFFIGY", "BYRDONIS", "TERROR_EEL",
]


def latest_checkpoint(ckpt_dir: Path) -> Path:
    cands = sorted(ckpt_dir.glob("ckpt_*.zip"), key=lambda p: p.stat().st_mtime)
    if not cands:
        cands = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not cands:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return cands[-1]


def lineup(combat) -> str:
    return " + ".join(sorted(
        str(getattr(e, "monster_id", None) or type(e).__name__)
        for e in (getattr(combat, "enemies", []) or [])
    ))


def main() -> int:
    ap = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--checkpoint", default=None)
    ap.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    ap.add_argument("--episodes", type=int, default=40)
    ap.add_argument("--seed-base", type=int, default=40_000_000)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--acts", type=int, default=2)
    ap.add_argument("--max-snapshots", type=int, default=6)
    ap.add_argument("--playouts", type=int, default=120,
                    help="Stochastic playouts per snapshot (split across the ladder)")
    ap.add_argument("--playout-cap", type=int, default=600)
    ap.add_argument("--targets", nargs="*", default=None)
    ap.add_argument("--all-elites", action="store_true")
    ap.add_argument("--also-monsters", action="store_true",
                    help="Also snapshot ordinary MONSTER rooms (the 57%% death bucket)")
    ap.add_argument("--json-out", default="output/winnability_probe.jsonl")
    args = ap.parse_args()

    targets = args.targets if args.targets is not None else DEFAULT_TARGETS

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.action_space import get_action_mask
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.gym_env.run_env import _LAYOUT
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS, SB3PolicyEvaluator, apply_combat_action,
        clone_combat, make_run_obs_builder,
    )

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint : {ckpt}")
    model = MaskablePPO.load(str(ckpt), device="cpu")
    evaluator = SB3PolicyEvaluator(model)

    env = RichSTS2RunEnv(character_id="Necrobinder",
                         ascension_level=args.ascension,
                         max_act_count=args.acts)
    env.set_shaping_scale(0.0)

    # ---- harvest ----
    snaps: list[dict] = []
    print(f"harvesting up to {args.max_snapshots} snapshots ...")
    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        seen: set[int] = set()
        while not (done or trunc) and steps < 3000:
            if mgr is not None and mgr.phase == RunManager.PHASE_COMBAT:
                c = mgr.get_combat_state()
                if c is not None and not c.is_over and id(c) not in seen:
                    seen.add(id(c))
                    room = getattr(getattr(mgr, "_current_room_type", None), "name", "?")
                    line = lineup(c)
                    hit = ((args.all_elites and room == "ELITE")
                           or (args.also_monsters and room == "MONSTER")
                           or any(t in line for t in targets))
                    if hit and len(snaps) < args.max_snapshots:
                        rs = mgr.run_state
                        deck = Counter(
                            f"{cd.card_id}{'+' if cd.upgraded else ''}" for cd in rs.player.deck)
                        snaps.append({
                            "seed": args.seed_base + i,
                            "floor": int(info.get("floor", 0)),
                            "room": room, "lineup": line,
                            "entry_hp": float(c.primary_player.current_hp),
                            "max_hp": float(c.primary_player.max_hp),
                            "deck_size": len(rs.player.deck),
                            "upgrades": sum(1 for cd in rs.player.deck if cd.upgraded),
                            "deck": dict(deck),
                            "relics": [str(r) for r in rs.relics],
                            "enemy_hp": [int(getattr(e, "current_hp", 0))
                                         for e in (getattr(c, "enemies", []) or [])],
                            "combat": clone_combat(c),
                            "obs_builder": make_run_obs_builder(env),
                        })
            mask = env.action_masks()
            a, _ = model.predict(obs, action_masks=mask, deterministic=True)
            obs, r, done, trunc, info = env.step(int(a))
            steps += 1
        if len(snaps) >= args.max_snapshots:
            break
    if not snaps:
        print("no snapshots harvested")
        return 1
    print(f"harvested {len(snaps)}\n")

    def run_playout(snap, mode: str, temp: float, rng: np.random.Generator) -> tuple[bool, float]:
        combat = clone_combat(snap["combat"])
        build = snap["obs_builder"]
        n = 0
        while not combat.is_over and n < args.playout_cap:
            p = combat.primary_player
            if p is None or not p.is_alive:
                break
            m = get_action_mask(combat).astype(bool)
            if not m.any():
                break
            legal = np.flatnonzero(m)
            if mode == "random":
                a = int(rng.choice(legal))
            else:
                full = np.zeros(int(model.policy.action_space.n), dtype=bool)
                full[:COMBAT_ACTIONS] = m
                probs, _ = evaluator.evaluate(build(combat), full)
                pr = probs[:COMBAT_ACTIONS] * m
                if pr.sum() <= 1e-12:
                    a = int(rng.choice(legal))
                else:
                    if temp <= 0.01:
                        a = int(np.argmax(pr))
                    else:
                        lp = np.log(np.maximum(pr, 1e-12)) / temp
                        lp -= lp.max()
                        w = np.exp(lp) * m
                        a = int(rng.choice(len(w), p=w / w.sum()))
            apply_combat_action(combat, a)
            n += 1
        pl = combat.primary_player
        alive = bool(pl is not None and pl.is_alive)
        return alive, float(max(getattr(pl, "current_hp", 0) or 0, 0))

    # Ladder: greedy, mild/medium/hot policy sampling, then uniform random.
    ladder = [("policy", 0.0), ("policy", 0.5), ("policy", 1.0),
              ("policy", 1.5), ("random", 0.0)]
    per = max(1, args.playouts // len(ladder))

    results = []
    for k, snap in enumerate(snaps):
        rng = np.random.default_rng(9_000 + k)
        best_hp = 0.0
        survivals = Counter()
        attempts = Counter()
        for mode, temp in ladder:
            tag = f"{mode}@{temp}" if mode == "policy" else "random"
            for _ in range(per):
                alive, hp = run_playout(snap, mode, temp, rng)
                attempts[tag] += 1
                if alive:
                    survivals[tag] += 1
                best_hp = max(best_hp, hp)
        tot_att = sum(attempts.values())
        tot_sur = sum(survivals.values())
        verdict = "WINNABLE" if tot_sur else "NO LINE SURVIVED"
        print(f"[{k+1}/{len(snaps)}] {snap['room']:<7} {snap['lineup'][:38]:<38} "
              f"hp {snap['entry_hp']:.0f}/{snap['max_hp']:.0f} "
              f"deck {snap['deck_size']}({snap['upgrades']}up) "
              f"enemyHP {sum(snap['enemy_hp'])}")
        detail = "  ".join(f"{t} {survivals[t]}/{attempts[t]}" for t, _ in
                           [(f"{m}@{tp}" if m == 'policy' else 'random', 0) for m, tp in ladder])
        print(f"          {verdict}: {tot_sur}/{tot_att} survived   best HP left {best_hp:.0f}")
        print(f"          {detail}")
        results.append({
            **{k2: v for k2, v in snap.items() if k2 not in ("combat", "obs_builder")},
            "survivals": dict(survivals), "attempts": dict(attempts),
            "total_survived": tot_sur, "total_attempts": tot_att, "best_hp": best_hp,
            "verdict": verdict,
        })

    n_win = sum(1 for r in results if r["total_survived"] > 0)
    print(f"\n=== VERDICT ===")
    print(f"  snapshots where SOME line survived : {n_win}/{len(results)}")
    print(f"  snapshots where NO line survived   : {len(results) - n_win}/{len(results)}")
    if n_win == 0:
        print("  >> These fights are already lost at combat start. The bottleneck is")
        print("     UPSTREAM of combat (deck/upgrades/relics/routing), not action selection.")
    elif n_win == len(results):
        print("  >> Every fight was winnable. The policy is losing winnable positions:")
        print("     action selection IS the bottleneck.")
    else:
        print("  >> Mixed: some positions recoverable, some already lost.")

    if args.json_out:
        Path(args.json_out).write_text(
            "\n".join(json.dumps(r) for r in results), encoding="utf-8")
        print(f"\nwrote {len(results)} records to {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
