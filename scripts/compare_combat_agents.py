#!/usr/bin/env python3
"""Compare two combat checkpoints on HP cost, PAIRED by seed.

Why paired, and why this script exists
--------------------------------------
Damage taken varies enormously fight to fight -- some encounters are cheap
whatever you do, some are brutal whatever you do. That between-fight variance
dwarfs the difference between two policies, so an UNPAIRED comparison needs an
implausible sample size to see anything.

Measured 2026-07-31, control vs an HP-shaped agent over the same 200 combats:

    unpaired:  17.23 +/- 2.35   vs   16.21 +/- 2.23    "overlapping, no effect"
    PAIRED:    -1.24 HP, 95% CI [+0.17, +2.32], t=2.27  "real effect"

Same data, opposite conclusions. The unpaired reading led to "the reward
approach does not work", which was wrong. Both arms are evaluated on identical
seeds and identical decks, so the pairing is free and roughly an order of
magnitude more sensitive -- there is no reason to ever use the unpaired number
for this question.

Reports damage among seeds where BOTH agents won: comparing HP cost on a fight
one agent lost is meaningless, since a loss ends at 0 HP by definition.

Usage
-----
    python -m scripts.compare_combat_agents A.zip B.zip \
        --deck-file output/.../harvested_decks.pkl --combats 200
"""

from __future__ import annotations

import argparse
import math
import os
import sys
from pathlib import Path

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def _play_all(path: str, combats: int, ascension: int, pools, deck_file, seed_block):
    """Per-seed (won, damage_taken) for one checkpoint."""
    import importlib.util

    from sb3_contrib import MaskablePPO

    spec = importlib.util.spec_from_file_location(
        "th", str(Path(__file__).resolve().parent / "train_hierarchical.py"))
    th = importlib.util.module_from_spec(spec)
    sys.modules["th"] = th
    spec.loader.exec_module(th)

    model = MaskablePPO.load(path, device="cpu")
    # w=0 and shaping off: SCORE both arms under the same pure-sparse reward,
    # whatever they were trained under. Otherwise the comparison measures the
    # reward difference rather than the behaviour difference.
    env = th.make_combat_env(ascension=ascension, seed=seed_block, pools=pools,
                             deck_file=deck_file, w_combat_hp_retained=0.0)
    env.set_shaping_scale(0.0)

    results = {}
    for i in range(combats):
        obs, _ = env.reset(seed=seed_block + i)
        hp_start = env.combat.primary_player.current_hp
        done = truncated = False
        steps = 0
        while not (done or truncated) and steps < 1500:
            action, _ = model.predict(
                obs, action_masks=env.action_masks(), deterministic=True)
            obs, _r, done, truncated, _ = env.step(int(action))
            steps += 1
        player = env.combat.primary_player
        won = bool(player is not None and player.is_alive)
        results[i] = (won, hp_start - player.current_hp)
    return results


def _wilcoxon_signed_rank_p(diffs: np.ndarray) -> float | None:
    """Two-sided p via the normal approximation. None if too few non-zero."""
    d = diffs[diffs != 0]
    n = d.size
    if n < 10:
        return None
    order = np.argsort(np.abs(d))
    ranks = np.empty(n, dtype=float)
    ranks[order] = np.arange(1, n + 1)
    w_plus = ranks[d > 0].sum()
    mean = n * (n + 1) / 4.0
    sd = math.sqrt(n * (n + 1) * (2 * n + 1) / 24.0)
    if sd == 0:
        return None
    z = (w_plus - mean) / sd
    return math.erfc(abs(z) / math.sqrt(2))


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("baseline")
    ap.add_argument("candidate")
    ap.add_argument("--combats", type=int, default=200)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--pools", nargs="+", default=["act1"])
    ap.add_argument("--deck-file", default=None)
    ap.add_argument("--seed-block", type=int, default=10_000_000)
    args = ap.parse_args(argv)

    common = dict(combats=args.combats, ascension=args.ascension,
                  pools=tuple(args.pools), deck_file=args.deck_file,
                  seed_block=args.seed_block)
    base = _play_all(args.baseline, **common)
    cand = _play_all(args.candidate, **common)

    base_wins = sum(1 for w, _ in base.values() if w)
    cand_wins = sum(1 for w, _ in cand.values() if w)
    shared = [i for i in base if base[i][0] and cand[i][0]]
    if not shared:
        print("no seed was won by BOTH agents; nothing to compare")
        return 1

    diffs = np.array([base[i][1] - cand[i][1] for i in shared], dtype=float)
    mean = float(diffs.mean())
    se = float(diffs.std(ddof=1) / math.sqrt(diffs.size))
    lo, hi = mean - 1.96 * se, mean + 1.96 * se
    t = mean / se if se else float("nan")
    p_t = math.erfc(abs(t) / math.sqrt(2)) if se else float("nan")
    p_w = _wilcoxon_signed_rank_p(diffs)

    print(f"\n=== PAIRED COMBAT COMPARISON ({args.combats} seeds) ===")
    print(f"  baseline : {args.baseline}")
    print(f"  candidate: {args.candidate}")
    print(f"  win rate : {base_wins/args.combats:.1%} -> {cand_wins/args.combats:.1%}")
    print(f"  seeds won by BOTH: {len(shared)}")
    print(f"\n  damage saved by candidate: {mean:+.2f} HP per shared win")
    print(f"    95% CI      : [{lo:+.2f}, {hi:+.2f}]")
    print(f"    paired t    : {t:.2f}   p ~ {p_t:.4f}")
    if p_w is not None:
        print(f"    Wilcoxon    : p ~ {p_w:.4f}")
    print(f"    candidate strictly cheaper on {int((diffs > 0).sum())}/{len(shared)}")
    verdict = ("candidate takes LESS damage" if lo > 0 else
               "candidate takes MORE damage" if hi < 0 else
               "no significant difference")
    print(f"\n  verdict: {verdict}")
    # An unpaired view purely to show how much it would have hidden.
    b = np.array([base[i][1] for i in shared], dtype=float)
    c = np.array([cand[i][1] for i in shared], dtype=float)
    bse = 1.96 * b.std(ddof=1) / math.sqrt(b.size)
    cse = 1.96 * c.std(ddof=1) / math.sqrt(c.size)
    print(f"  (unpaired, for contrast: {b.mean():.2f}+/-{bse:.2f} vs "
          f"{c.mean():.2f}+/-{cse:.2f} -- overlapping intervals here do NOT "
          f"mean no effect)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
