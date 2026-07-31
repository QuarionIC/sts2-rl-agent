#!/usr/bin/env python3
"""Does the run agent choose a CARD, or just a SLOT?

Choice-distribution statistics cannot answer this. An agent that always takes
slot 1 and an agent that happens to prefer the card usually offered in slot 1
produce similar histograms. The distinction is causal, so test it causally:
freeze the run state, permute which slot each offered card occupies, and see
what moves.

* Follows the CARD  -> the same card is picked whatever slot it sits in.
  The offer observation is being used.
* Follows the SLOT  -> the same index is picked whatever card sits there.
  The policy is degenerate, exactly as measured live before the offer block
  existed (joint_r5 took slot 1 on 14 of 14 offers, joint_r2 slot 2 on 11 of
  11 -- different constants, same pathology, because the observation did not
  describe the cards at all).

Usage
-----
    python scripts/probe_card_reward_choice.py --model output/.../best_model.zip
    python scripts/probe_card_reward_choice.py --model M --episodes 40 --seed 7
"""
from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import itertools
from collections import Counter

import numpy as np


def _card_names(mgr) -> list[str]:
    return [c.card_id.name for c in (getattr(mgr, "_offered_cards", None) or [])]


def probe_one(model, env, encoder, layout) -> dict | None:
    """Permute the offer at the current card-reward node; report what moves.

    Returns None when the node has fewer than two offers (nothing to permute).
    """
    mgr = env._mgr
    offered = list(getattr(mgr, "_offered_cards", None) or [])
    if len(offered) < 2:
        return None

    names = [c.card_id.name for c in offered]
    n = min(len(offered), 3)          # slots 0..2 are the base card_reward block
    picks_by_permutation: list[tuple[tuple[str, ...], int, str]] = []

    for perm in itertools.permutations(range(n)):
        mgr._offered_cards = [offered[i] for i in perm] + offered[n:]
        obs = encoder.encode_run(mgr)
        mask = np.asarray(env.action_masks(), dtype=bool)
        action, _ = model.predict(obs, action_masks=mask, deterministic=True)
        action = int(action)
        slot = action - layout.card_reward_start
        chosen_name = (mgr._offered_cards[slot].card_id.name
                       if 0 <= slot < n else f"<non-pick action {action}>")
        picks_by_permutation.append(
            (tuple(c.card_id.name for c in mgr._offered_cards[:n]), slot, chosen_name))

    mgr._offered_cards = offered      # restore before the env steps on

    chosen_cards = {p[2] for p in picks_by_permutation}
    chosen_slots = {p[1] for p in picks_by_permutation}
    return {
        "offered": names,
        "picks": picks_by_permutation,
        "same_card_every_time": len(chosen_cards) == 1,
        "same_slot_every_time": len(chosen_slots) == 1,
    }


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True)
    ap.add_argument("--episodes", type=int, default=25)
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--combat-model", default=None)
    args = ap.parse_args()

    from sb3_contrib import MaskablePPO

    from scripts.train_hierarchical import make_run_env
    from sts2_env.gym_env.rich_observation import RichObservationEncoder
    from sts2_env.gym_env.run_env import _build_action_layout

    model = MaskablePPO.load(args.model, device="cpu")
    layout = _build_action_layout()
    encoder = RichObservationEncoder()

    card_following = slot_following = neither = 0
    examples: list[dict] = []
    seen_cards: Counter = Counter()

    for ep in range(args.episodes):
        env = make_run_env(args.combat_model, ascension=args.ascension,
                           max_act_count=args.max_act_count, seed=args.seed + ep,
                           use_planner=args.combat_model is None)
        env.set_shaping_scale(0.0)
        env.reset(seed=args.seed + ep)

        for _ in range(200):
            mgr = env._mgr
            if mgr is None:
                break
            if mgr.phase == "CARD_REWARD":
                result = probe_one(model, env, encoder, layout)
                if result is not None:
                    if result["same_card_every_time"]:
                        card_following += 1
                        seen_cards[result["picks"][0][2]] += 1
                    elif result["same_slot_every_time"]:
                        slot_following += 1
                    else:
                        neither += 1
                    if len(examples) < 4:
                        examples.append(result)
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            obs = encoder.encode_run(env._mgr) if env._mgr else None
            if obs is None:
                break
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            _, _, done, trunc, _ = env.step(int(action))
            if done or trunc:
                break

    total = card_following + slot_following + neither
    print(f"card-reward nodes probed: {total}")
    if not total:
        print("no multi-option card rewards reached; try more episodes")
        return
    print(f"  follows the CARD (choice tracks the card across permutations): "
          f"{card_following} ({100*card_following/total:.0f}%)")
    print(f"  follows the SLOT (same index regardless of cards):            "
          f"{slot_following} ({100*slot_following/total:.0f}%)")
    print(f"  neither (mixed)                                               "
          f"{neither} ({100*neither/total:.0f}%)")
    if seen_cards:
        print(f"\nmost-picked cards: {dict(seen_cards.most_common(8))}")
    print("\nexamples:")
    for ex in examples:
        print(f"  offered {ex['offered']}")
        for arrangement, slot, name in ex["picks"]:
            print(f"    {list(arrangement)} -> slot {slot} = {name}")


if __name__ == "__main__":
    main()
