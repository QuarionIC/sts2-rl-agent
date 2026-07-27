#!/usr/bin/env python3
"""How much does the combat agent lose on the run agent's REAL decks?

The run agent plateaued at ~9.0 floors with a healthy 15-card deck, and the
survival arithmetic points at the combat agent: with independent per-combat
survival p, a run lasts 1/(1-p) fights, so p=0.84 caps it near 6 combats no
matter how good the routing is. That makes per-combat survival the binding
constraint -- but only if 84% is what the combat agent actually achieves in
situ. It was measured on ProgressiveDeckSampler, a synthetic guess at mid-run
decks, and the run agent now builds decks of its own.

This measures both numbers on the same combat agent:

  * synthetic  -- ProgressiveDeckSampler, the training distribution
  * harvested  -- decks recorded at real combat entries under the run agent

A large gap means the combat agent is being evaluated on a distribution it
never trained on, and refitting (the alternation loop) should recover it. A
small gap means 84% is simply the agent's ceiling and the combat agent needs
to get stronger, not merely re-aimed.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import pickle
import sys
from collections import Counter
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def harvest(run_model: str, combat_model: str, episodes: int, ascension: int,
            max_act_count: int, out: Path) -> list:
    """Record (deck, relics, potions, hp_frac) at each real combat entry."""
    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.hierarchical_run_env import (
        HierarchicalRunEnv,
        PolicyCombatController,
    )

    combat = MaskablePPO.load(combat_model, device="cpu")
    run = MaskablePPO.load(run_model, device="cpu")
    env = HierarchicalRunEnv(
        character_id="Necrobinder", ascension_level=ascension,
        max_act_count=max_act_count,
        combat_controller=PolicyCombatController(combat, deterministic=False),
    )
    env.set_shaping_scale(0.0)

    samples: list = []
    seen: set[int] = set()
    orig = env._combat_action

    def spy(mask):
        c = env._mgr.get_combat_state()
        if c is not None and not c.is_over and id(c) not in seen:
            seen.add(id(c))
            rs = env._mgr.run_state
            p = rs.player
            samples.append((
                [(cd.card_id, bool(cd.upgraded)) for cd in p.deck],
                [str(r) for r in rs.relics],
                [],
                float(p.current_hp) / float(max(p.max_hp, 1)),
            ))
        return orig(mask)

    env._combat_action = spy

    for i in range(episodes):
        obs, info = env.reset(seed=90_000_000 + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 2000:
            m = env.action_masks()
            a, _ = run.predict(obs, action_masks=m, deterministic=False)
            obs, r, done, tr, info = env.step(int(a))
            n += 1
        if (i + 1) % 25 == 0:
            print(f"    harvested {len(samples)} decks after {i+1} runs", flush=True)

    with out.open("wb") as fh:
        pickle.dump(samples, fh)
    return samples


def evaluate(combat_model: str, n_episodes: int, ascension: int, pools,
             deck_file: str | None) -> dict:
    from sb3_contrib import MaskablePPO

    from train_hierarchical import make_combat_env

    model = MaskablePPO.load(combat_model, device="cpu")
    env = make_combat_env(ascension=ascension, seed=12_345, pools=pools,
                          deck_file=deck_file)
    env.set_shaping_scale(0.0)
    wins, hp = [], []
    for i in range(n_episodes):
        obs, info = env.reset(seed=12_345 + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 1500:
            m = env.action_masks()
            a, _ = model.predict(obs, action_masks=m, deterministic=True)
            obs, r, done, tr, info = env.step(int(a))
            n += 1
        p = env.combat.primary_player if env.combat is not None else None
        alive = bool(p is not None and p.is_alive)
        wins.append(alive)
        if p is not None and p.max_hp:
            hp.append(max(0.0, p.current_hp) / p.max_hp)
    return {"win_rate": float(np.mean(wins)),
            "mean_hp_frac": float(np.mean(hp)) if hp else 0.0,
            "n": n_episodes}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--combat-model", default="output/hier/combat/best_model.zip")
    ap.add_argument("--run-model", default="output/hier/run/best_model.zip")
    ap.add_argument("--harvest-episodes", type=int, default=80)
    ap.add_argument("--eval-episodes", type=int, default=300)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--pools", nargs="*", default=["act1"])
    ap.add_argument("--deck-out", default="output/harvested_decks.pkl")
    args = ap.parse_args()

    out = Path(args.deck_out)
    out.parent.mkdir(parents=True, exist_ok=True)

    print(f"harvesting real combat-entry decks from {args.run_model} ...", flush=True)
    samples = harvest(args.run_model, args.combat_model, args.harvest_episodes,
                      args.ascension, args.max_act_count, out)
    if not samples:
        print("no decks harvested -- aborting")
        return 1
    sizes = np.array([len(s[0]) for s in samples], dtype=float)
    ups = np.array([sum(1 for _, u in s[0] if u) for s in samples], dtype=float)
    hpf = np.array([s[3] for s in samples], dtype=float)
    print(f"\nharvested {len(samples)} decks: size mean {sizes.mean():.1f} "
          f"(min {sizes.min():.0f} max {sizes.max():.0f}), "
          f"upgrades mean {ups.mean():.2f}, entry HP frac mean {hpf.mean():.2f}")

    print(f"\nevaluating the SAME combat agent on both distributions "
          f"({args.eval_episodes} combats each) ...", flush=True)
    synth = evaluate(args.combat_model, args.eval_episodes, args.ascension,
                     tuple(args.pools), None)
    real = evaluate(args.combat_model, args.eval_episodes, args.ascension,
                    tuple(args.pools), str(out))

    print(f"\n=== DISTRIBUTION SHIFT ===")
    print(f"  synthetic (training)  win {synth['win_rate']:.1%}  "
          f"hp_frac {synth['mean_hp_frac']:.3f}")
    print(f"  harvested (real)      win {real['win_rate']:.1%}  "
          f"hp_frac {real['mean_hp_frac']:.3f}")
    gap = synth["win_rate"] - real["win_rate"]
    print(f"  gap                   {gap:+.1%}")

    print(f"\n=== IMPLIED RUN LENGTH (1/(1-p) combats) ===")
    for label, r in (("synthetic", synth), ("harvested", real)):
        p = min(r["win_rate"], 0.999)
        print(f"  {label:<10} p={p:.3f} -> {1/(1-p):>5.1f} combats survived")
    if gap > 0.05:
        print("\n  >> Real decks are materially harder for this combat agent than its\n"
              "     training distribution. Refitting on harvested decks should help.")
    else:
        print("\n  >> No material shift: ~this win rate IS the combat agent's ceiling.\n"
              "     Refitting will not move it; the combat agent must get stronger.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
