#!/usr/bin/env python3
"""Classify logged bridge divergences, offline.

Why offline
-----------
The runner classifies each divergence as it happens, but a long session runs
whatever classifier it was started with. When a new class is added -- DRAW-SHIFT
was split out of CONTENTS on 2026-08-01 -- every session already in flight keeps
reporting the old label, and the sessions already on disk are the best evidence
for whether the new class matters.

This re-reads the sim/live hand pairs the runner logs and applies the CURRENT
classification, so historic logs can be re-scored without replaying anything.

Why the split matters at all: "CONTENTS (different cards)" points an
investigation at card modelling and RNG. A DRAW-SHIFT is a draw-COUNT
disagreement and a SIM-AHEAD is the client outrunning the game -- neither is a
simulator-fidelity failure, and lumping them together both misdirects the work
and overstates the divergence rate. Measured 2026-07-31, two thirds of a
"5.96% divergence" was synchronisation.

Usage
-----
    python -m scripts.classify_divergences output/overnight/session_*.log
"""

from __future__ import annotations

import argparse
import ast
import math
import re
import sys
from collections import Counter
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

_DIVERGED = re.compile(r"plan diverged \[([^\]]*)\]")
_SIM_HAND = re.compile(r"sim  hand: (\[[^\]]*\]) \(energy (-?\d+|None)\)")
_LIVE_HAND = re.compile(r"live hand: (\[[^\]]*\]) \(energy (-?\d+|None)\)")
_ACTION = re.compile(r"COMBAT \[HP:")


def _norm(card: str) -> str:
    from sts2_env.bridge.agent_runner import _norm_card

    return _norm_card(card)


def classify(sim_hand: list[str], live_hand: list[str],
             sim_energy, live_energy) -> str:
    """The runner's current classification, applied to a logged pair."""
    sim_n = [_norm(c) for c in sim_hand]
    live_n = [_norm(c) for c in live_hand]

    # SIM-AHEAD: our hand is the live hand with exactly one card removed and
    # we hold no more energy -- the simulation played something the game
    # has not.
    if len(sim_n) == len(live_n) - 1:
        remaining = list(live_n)
        ok = True
        for card in sim_n:
            if card in remaining:
                remaining.remove(card)
            else:
                ok = False
                break
        if (ok and len(remaining) == 1
                and isinstance(sim_energy, int) and isinstance(live_energy, int)
                and sim_energy <= live_energy):
            return "SIM-AHEAD"

    if len(sim_n) >= 2 and len(live_n) >= 2:
        if sim_n[1:] == live_n[:len(sim_n) - 1]:
            return "DRAW-SHIFT (game drew more)"
        if live_n[1:] == sim_n[:len(live_n) - 1]:
            return "DRAW-SHIFT (sim drew more)"

    # FRONTIER: the two hands agree on every card except the NEWEST one.
    #
    # Measured 2026-08-01, 4 of 10 residual "CONTENTS" divergences were this:
    #   sim  [END_OF_DAYS, DEFEND, DEFEND, INVOKE, DEFEND, REANIMATE]
    #   live [END_OF_DAYS, DEFEND, DEFEND, INVOKE, DEFEND, BORROWED_TIME]
    # Same draw sequence, disagreeing only on the card most recently drawn.
    # That is the draw FRONTIER -- an RNG or pile-order divergence one card
    # deep -- not the card modelling that "CONTENTS" implies, and it would
    # have sent the investigation at card effects rather than at the draw.
    if (len(sim_n) == len(live_n) and len(sim_n) >= 2
            and sim_n[:-1] == live_n[:-1] and sim_n[-1] != live_n[-1]):
        return "FRONTIER (same prefix, newest draw differs)"

    same_len = len(sim_n) == len(live_n)
    same_multiset = sorted(sim_n) == sorted(live_n)
    if same_len and same_multiset:
        return "ORDER-ONLY"
    if same_multiset:
        return "COUNT"
    return "CONTENTS"


def _wilson(k: int, n: int) -> tuple[float, float, float]:
    if n == 0:
        return 0.0, 0.0, 0.0
    p = k / n
    z = 1.96
    c = (p + z * z / (2 * n)) / (1 + z * z / n)
    h = z * math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / (1 + z * z / n)
    return 100 * p, 100 * max(0.0, c - h), 100 * min(1.0, c + h)


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("logs", nargs="+", type=Path)
    ap.add_argument("--examples", type=int, default=2,
                    help="worked examples to print per class")
    args = ap.parse_args(argv)

    counts: Counter[str] = Counter()
    actions = 0
    examples: dict[str, list[str]] = {}

    for path in args.logs:
        if not path.exists():
            continue
        lines = path.read_text(errors="replace").splitlines()
        actions += sum(1 for line in lines if _ACTION.search(line))
        for i, line in enumerate(lines):
            if not _DIVERGED.search(line):
                continue
            sim_m = live_m = None
            for follow in lines[i + 1:i + 4]:
                sim_m = sim_m or _SIM_HAND.search(follow)
                live_m = live_m or _LIVE_HAND.search(follow)
            if not (sim_m and live_m):
                counts["UNPARSED"] += 1
                continue
            try:
                sim_hand = ast.literal_eval(sim_m.group(1))
                live_hand = ast.literal_eval(live_m.group(1))
            except (ValueError, SyntaxError):
                counts["UNPARSED"] += 1
                continue
            se = int(sim_m.group(2)) if sim_m.group(2) != "None" else None
            le = int(live_m.group(2)) if live_m.group(2) != "None" else None
            kind = classify(sim_hand, live_hand, se, le)
            counts[kind] += 1
            examples.setdefault(kind, []).append(
                f"    sim  {sim_hand}\n    live {live_hand}")

    total = sum(counts.values())
    print(f"\n=== DIVERGENCES over {actions} combat actions "
          f"({len(args.logs)} log(s)) ===")
    if not total:
        print("  none found")
        return 0

    # ONLY SIM-AHEAD is an artifact.
    #
    # Being careful here in BOTH directions. Blending everything together
    # overstates divergence -- two thirds of a "5.96%" was synchronisation.
    # But calling every draw disagreement an artifact understates it just as
    # badly, and that error is more tempting because it flatters the number.
    #
    # SIM-AHEAD is provably the mod's serialization racing its own action
    # queue: fixing PlayCardAndWaitAsync cut it 3.92% -> 0.83% (p=9.9e-10)
    # while leaving every other class alone. Nothing about the simulator was
    # wrong.
    #
    # DRAW-SHIFT and FRONTIER are NOT that. They mean the simulator drew a
    # different card, or a different NUMBER of cards, than the game -- a real
    # disagreement about draw order or draw count. They are reported apart
    # from CONTENTS because they point at a different subsystem (the draw and
    # the shuffle, versus card effects), not because they are free.
    ARTIFACT = {"SIM-AHEAD"}
    DRAW_RELATED = {"DRAW-SHIFT (game drew more)", "DRAW-SHIFT (sim drew more)",
                    "FRONTIER (same prefix, newest draw differs)",
                    "ORDER-ONLY", "COUNT"}

    # CONTENTS is deliberately NOT called "card modelling".
    #
    # Measured 2026-08-01 over 17 CONTENTS cases, the cards that differ are the
    # deck's most COMMON ones -- the simulator holding 9 extra DEFEND, the game
    # 5 extra STRIKE. An unmodelled card EFFECT would implicate specific
    # unusual cards; a Strike/Defend imbalance is what draw divergence looks
    # like once it has compounded past the point where the one-card SHIFT and
    # FRONTIER patterns still match.
    #
    # So the honest label is "unclassified": a real disagreement, cause not
    # established, and most likely the same draw divergence seen further
    # along. Calling it card modelling would send the next investigation at
    # card effects, which is where it would waste the most time.
    def _bucket(kind: str) -> str:
        if kind in ARTIFACT:
            return "artifact"
        if kind in DRAW_RELATED:
            return "fidelity:draw"
        return "fidelity:unclassified"

    draw = sum(v for k, v in counts.items() if _bucket(k) == "fidelity:draw")
    cards = sum(v for k, v in counts.items() if _bucket(k) == "fidelity:unclassified")
    artifacts = sum(v for k, v in counts.items() if _bucket(k) == "artifact")

    for kind, n in counts.most_common():
        p, lo, hi = _wilson(n, actions)
        print(f"  {kind:44} {n:4d}  {p:5.2f}%  CI[{lo:.2f},{hi:.2f}]  "
              f"[{_bucket(kind)}]")

    p, lo, hi = _wilson(draw + cards, actions)
    print(f"\n  SIMULATOR FIDELITY: {draw + cards}/{actions} = {p:.2f}%  "
          f"CI[{lo:.2f},{hi:.2f}]")
    dp, *_ = _wilson(draw, actions)
    cp, *_ = _wilson(cards, actions)
    print(f"    of which draw order/count : {draw:3d} ({dp:.2f}%)")
    print(f"    of which unclassified     : {cards:3d} ({cp:.2f}%)"
          f"  [cause not established; composition suggests compounded draw]")
    ap_, *_ = _wilson(artifacts, actions)
    print(f"  client/server artifacts (NOT fidelity): {artifacts} ({ap_:.2f}%)")
    print(f"  blended total: {total}/{actions} = "
          f"{100*total/max(actions,1):.2f}%")

    if args.examples:
        print("\n=== examples ===")
        for kind, rows in examples.items():
            print(f"  [{kind}]")
            for row in rows[:args.examples]:
                print(row)
    return 0


if __name__ == "__main__":
    sys.exit(main())
