#!/usr/bin/env python3
"""What does the policy actually DO at every non-combat decision?

The winnability probe showed runs reaching floor 5-8 elites with a 10-11 card
deck and 0 upgrades -- i.e. the pure starter deck. That is either a learned
pathology (the policy skips rewards) or a plumbing bug (taking a card is not
reachable through the action mask). This distinguishes them by logging, for
every non-combat phase, both the options that were LEGAL and the one chosen.

If pick-card actions never appear in the legal set, it is a bug. If they appear
and are never chosen, it is the policy.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

DEFAULT_CKPT_DIR = Path("output/necrobinder_scratch/G1")


def latest_checkpoint(ckpt_dir: Path) -> Path:
    cands = sorted(ckpt_dir.glob("ckpt_*.zip"), key=lambda p: p.stat().st_mtime)
    if not cands:
        cands = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not cands:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return cands[-1]


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--checkpoint", default=None)
    ap.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    ap.add_argument("--episodes", type=int, default=60)
    ap.add_argument("--seed-base", type=int, default=50_000_000)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--acts", type=int, default=2)
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint: {ckpt}\n")
    model = MaskablePPO.load(str(ckpt), device="cpu")

    env = RichSTS2RunEnv(character_id="Necrobinder",
                         ascension_level=args.ascension,
                         max_act_count=args.acts)
    env.set_shaping_scale(0.0)

    # phase -> Counter of chosen action-type
    chosen: dict[str, Counter] = defaultdict(Counter)
    # phase -> Counter of action-types that were LEGAL (once per visit)
    offered: dict[str, Counter] = defaultdict(Counter)
    visits: Counter = Counter()
    deck_growth: list[tuple[int, int]] = []  # (final_floor, final_deck_size)

    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        while not (done or trunc) and steps < 3000:
            phase = mgr.phase if mgr is not None else "?"
            acts = mgr.get_available_actions() if mgr is not None else []
            if phase != RunManager.PHASE_COMBAT and acts:
                visits[phase] += 1
                for t in {str(a.get("action", "?")) for a in acts}:
                    offered[phase][t] += 1

            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)

            # Resolve the chosen index back to an action dict where the phase
            # uses a simple contiguous slice.
            if phase != RunManager.PHASE_COMBAT and acts:
                from sts2_env.gym_env.run_env import _LAYOUT
                starts = {
                    RunManager.PHASE_MAP_CHOICE: _LAYOUT.map_start,
                    RunManager.PHASE_CARD_REWARD: _LAYOUT.card_reward_start,
                    RunManager.PHASE_BOSS_RELIC: _LAYOUT.boss_relic_start,
                    RunManager.PHASE_SHOP: _LAYOUT.shop_start,
                    RunManager.PHASE_REST_SITE: _LAYOUT.rest_start,
                    RunManager.PHASE_EVENT: _LAYOUT.event_start,
                    RunManager.PHASE_TREASURE: _LAYOUT.treasure_start,
                }
                base = starts.get(phase)
                label = "?"
                if base is not None:
                    idx = int(action) - base
                    if 0 <= idx < len(acts):
                        label = str(acts[idx].get("action", "?"))
                    else:
                        label = f"OOR({idx})"
                chosen[phase][label] += 1

            obs, reward, done, trunc, info = env.step(int(action))
            steps += 1

        deck_growth.append((int(info.get("floor", 0)), int(info.get("deck_size", 0))))
        if (i + 1) % 20 == 0:
            print(f"  ... {i+1}/{args.episodes}")

    print(f"\n=== NON-COMBAT DECISIONS over {args.episodes} runs ===")
    for phase in sorted(visits, key=lambda p: -visits[p]):
        print(f"\n--- {phase}  ({visits[phase]} decision points, "
              f"{visits[phase]/args.episodes:.2f}/run) ---")
        print(f"    offered: " + ", ".join(
            f"{t}({n})" for t, n in offered[phase].most_common()))
        tot = sum(chosen[phase].values())
        print(f"    chosen : " + ", ".join(
            f"{t} {n} ({n/max(tot,1):.0%})" for t, n in chosen[phase].most_common()))

    # The headline: were pick-card actions ever offered, and ever taken?
    cr = RunManager.PHASE_CARD_REWARD
    picks = sum(n for t, n in chosen[cr].items() if "pick" in t.lower())
    skips = sum(n for t, n in chosen[cr].items() if "skip" in t.lower())
    off_picks = sum(n for t, n in offered[cr].items() if "pick" in t.lower())
    print(f"\n=== CARD REWARD VERDICT ===")
    print(f"  pick-card offered at {off_picks} decision points")
    print(f"  pick chosen {picks}   skip chosen {skips}")
    if off_picks == 0:
        print("  >> BUG: taking a card is never reachable through the action mask.")
    elif picks == 0:
        print("  >> POLICY: cards are offered and the agent always declines them.")
    else:
        print(f"  >> Agent takes cards {picks/(picks+skips):.0%} of the time it is offered.")

    fl = np.array([d[0] for d in deck_growth], dtype=float)
    dk = np.array([d[1] for d in deck_growth], dtype=float)
    print(f"\n  final floor mean {fl.mean():.2f} | final deck size mean {dk.mean():.2f} "
          f"(starter = 10)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
