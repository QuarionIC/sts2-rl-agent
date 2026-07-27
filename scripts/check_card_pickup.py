#!/usr/bin/env python3
"""Do card picks actually land in the deck?

The decision probe logged 209 pick_card choices across 60 runs while mean final
deck size moved 10.0 -> 10.3. Those cannot both be true, so this measures the
thing directly: deck size immediately before and after every step taken from a
CARD_REWARD phase, attributed to the resolved action.

Rather than trusting an index->action mapping (the card-reward slice is only 4
wide and pending run choices reuse the combat slice, so labelling by offset is
unreliable), this records the SIM's own report: the action dict RunManager
actually executed, taken from take_action's return, plus the true deck delta.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
from collections import Counter
from pathlib import Path

import numpy as np

DEFAULT_CKPT = "output/necrobinder_scratch/G1/ckpt_0020500000.zip"


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--checkpoint", default=DEFAULT_CKPT)
    ap.add_argument("--episodes", type=int, default=40)
    ap.add_argument("--seed-base", type=int, default=50_000_000)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--acts", type=int, default=2)
    ap.add_argument("--verbose", action="store_true")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager

    model = MaskablePPO.load(args.checkpoint, device="cpu")
    env = RichSTS2RunEnv(character_id="Necrobinder",
                         ascension_level=args.ascension,
                         max_act_count=args.acts)
    env.set_shaping_scale(0.0)

    executed = Counter()          # action type actually run by the sim
    delta_by_action = Counter()   # summed deck delta per action type
    n_by_action = Counter()
    peak_decks, final_decks = [], []

    for i in range(args.episodes):
        obs, info = env.reset(seed=args.seed_base + i)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        peak = len(mgr.run_state.player.deck)

        # Wrap take_action so we see exactly what the sim ran.
        real_take = mgr.take_action
        last: dict = {}

        def spy(action: dict, _real=real_take, _last=last):
            _last.clear()
            _last.update(action or {})
            return _real(action)

        mgr.take_action = spy  # type: ignore[method-assign]

        while not (done or trunc) and steps < 3000:
            phase = mgr.phase
            before = len(mgr.run_state.player.deck)
            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            last.clear()
            obs, reward, done, trunc, info = env.step(int(action))
            steps += 1
            after = len(mgr.run_state.player.deck) if mgr.run_state is not None else before
            peak = max(peak, after)
            if phase == RunManager.PHASE_CARD_REWARD and last:
                t = str(last.get("action", "?"))
                executed[t] += 1
                delta_by_action[t] += after - before
                n_by_action[t] += 1
                if args.verbose and t.startswith("pick") and after == before:
                    print(f"    seed {args.seed_base+i} floor {info.get('floor')}: "
                          f"{t} {last} -> deck unchanged at {after}")

        final_decks.append(len(mgr.run_state.player.deck))
        peak_decks.append(peak)

    print(f"\n=== CARD_REWARD ACTIONS ACTUALLY EXECUTED ({args.episodes} runs) ===")
    for t, n in executed.most_common():
        d = delta_by_action[t]
        print(f"  {t:<22} n={n:<5} total deck delta {d:+5d}   mean {d/max(n,1):+.3f}")

    picks = sum(n for t, n in executed.items() if t.startswith("pick_card"))
    pick_delta = sum(d for t, d in delta_by_action.items() if t.startswith("pick_card"))
    print(f"\n=== VERDICT ===")
    print(f"  pick_card executed {picks} times, total deck growth {pick_delta:+d}")
    if picks and pick_delta == 0:
        print("  >> BUG: picks execute but never add a card to the deck.")
    elif picks and pick_delta < picks:
        print(f"  >> PARTIAL: only {pick_delta}/{picks} picks added a card.")
    elif picks:
        print("  >> Picks do add cards. Deck stagnation must come from elsewhere.")

    fd, pd = np.array(final_decks, float), np.array(peak_decks, float)
    print(f"  final deck mean {fd.mean():.2f}  peak deck mean {pd.mean():.2f}  (starter 10)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
