#!/usr/bin/env python3
"""Rest-site decision forensics.

The death forensics showed ~1.5 rest sites per episode producing almost no net
healing, while elites were being entered at a mean 45 HP (p10 = 12). Either the
policy is choosing Smith over Rest at low HP, or resting is not healing. This
tells them apart by recording, for every rest site actually visited, the HP the
policy walked in with and the option it picked.
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
    candidates = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return candidates[-1]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--checkpoint", default=None)
    parser.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    parser.add_argument("--episodes", type=int, default=100)
    parser.add_argument("--seed-base", type=int, default=30_000_000)
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--acts", type=int, default=2)
    parser.add_argument("--max-steps", type=int, default=3000)
    args = parser.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint: {ckpt}")
    model = MaskablePPO.load(str(ckpt), device="cpu")

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        max_act_count=args.acts,
    )
    env.set_shaping_scale(0.0)

    picks = Counter()
    hp_at_pick: dict[str, list[float]] = defaultdict(list)
    heal_deltas: list[float] = []
    # HP fraction buckets -> what did it pick there?
    bucket_picks: dict[str, Counter] = defaultdict(Counter)
    visits = 0

    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        while not (done or trunc) and steps < args.max_steps:
            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)

            at_rest = mgr is not None and mgr.phase == RunManager.PHASE_REST_SITE
            if at_rest:
                hp_before = float(mgr.run_state.player.current_hp)
                max_hp = float(mgr.run_state.player.max_hp) or 1.0
                acts = mgr.get_available_actions()
                # Resolve which rest option this action index maps to.
                from sts2_env.gym_env.run_env import _LAYOUT
                local = int(action) - _LAYOUT.rest_start
                opts = [a for a in acts if a.get("action") == "rest_option"]
                label = "?"
                if opts and 0 <= local < len(opts):
                    label = str(opts[local].get("option_id", "?"))
                elif opts:
                    label = f"OOR({local})"

            obs, reward, done, trunc, info = env.step(int(action))
            steps += 1

            if at_rest and label not in ("?",):
                visits += 1
                picks[label] += 1
                hp_at_pick[label].append(hp_before)
                frac = hp_before / max_hp
                b = "<25%" if frac < .25 else "25-50%" if frac < .5 else "50-75%" if frac < .75 else ">=75%"
                bucket_picks[b][label] += 1
                if label == "HEAL":
                    heal_deltas.append(float(info.get("hp", hp_before)) - hp_before)

        if (i + 1) % 25 == 0:
            print(f"  ... {i + 1}/{args.episodes} episodes")

    print(f"\n=== REST SITE DECISIONS ({visits} rest-option picks over {args.episodes} episodes) ===")
    for label, n in picks.most_common():
        a = np.array(hp_at_pick[label])
        print(f"  {label:<10} {n:>5} ({n / max(visits,1):>5.1%})  mean HP at pick {a.mean():>6.1f}  "
              f"p10 {np.percentile(a,10):>5.1f}")

    if heal_deltas:
        a = np.array(heal_deltas)
        print(f"\n  HEAL actually restored: mean {a.mean():.2f} HP  min {a.min():.0f}  max {a.max():.0f}")
    else:
        print("\n  HEAL was never chosen -- cannot measure healing.")

    print(f"\n=== PICK BY HP BUCKET (does it rest when hurt?) ===")
    for b in ("<25%", "25-50%", "50-75%", ">=75%"):
        c = bucket_picks.get(b)
        if not c:
            continue
        tot = sum(c.values())
        detail = "  ".join(f"{k} {v} ({v/tot:.0%})" for k, v in c.most_common())
        print(f"  {b:<8} n={tot:<5} {detail}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
