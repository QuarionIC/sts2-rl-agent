#!/usr/bin/env python3
"""Re-test MCTS on the COMBAT agent, on contested positions.

An earlier head-to-head found determinized PUCT overriding a full-run policy on
1/197 decisions and changing zero outcomes. That result was confounded: the
snapshots were starter-deck elite fights where 0/150 stochastic playouts
survived. Search cannot rank lines when every line loses, so the test could
only ever return "no effect" -- it said nothing about whether search helps in
general.

The situation is now different in exactly the way that matters. The run agent
builds ~15-card decks and the combat agent wins ~75% of fights, so positions
are contested rather than hopeless, and the value function is not saturated at
"lost". That makes this a real test of the hypothesis instead of a rerun of a
broken one.

Two other confounds from the first attempt are removed:

* The combat agent is native 115-action and trained on ``encode_combat``,
  which is exactly what ``mcts_action_distribution`` supplies for a bare
  CombatState. The earlier test had to fight an obs/action-space mismatch.
* Snapshots are drawn from the HARVESTED deck distribution -- the decks the
  run agent actually brings to fights -- not from whatever a flat policy
  happened to reach.

Reports the disagreement rate (does search override the policy at all?)
alongside paired survival, since a zero disagreement rate means the sim budget
never bit and the survival comparison is uninformative either way.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
import time
from math import comb
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def _same_card(combat, a: int, b: int) -> bool:
    """True when two combat actions play the same card id at the same target.

    Hands routinely hold duplicates (4x Strike), so different action indices
    frequently denote an identical play.
    """
    from sts2_env.gym_env.action_space import (
        action_to_card_and_target,
        is_potion_action,
    )

    if a == b:
        return True
    if a == 0 or b == 0 or is_potion_action(a) or is_potion_action(b):
        return False
    try:
        ia, ta = action_to_card_and_target(a)
        ib, tb = action_to_card_and_target(b)
    except Exception:
        return False
    if ia is None or ib is None or ta != tb:
        return False
    state = combat.combat_player_states[0]
    hand = getattr(state, "hand", []) or []
    if ia >= len(hand) or ib >= len(hand):
        return False
    ca, cb = hand[ia], hand[ib]
    return (getattr(ca, "card_id", None) == getattr(cb, "card_id", None)
            and bool(getattr(ca, "upgraded", False)) == bool(getattr(cb, "upgraded", False)))


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--combat-model", default="output/hier/combat/best_model.zip")
    ap.add_argument("--deck-file", default="output/hier_alt/r1/harvested_decks.pkl")
    ap.add_argument("--snapshots", type=int, default=40)
    ap.add_argument("--sims", type=int, default=400)
    ap.add_argument("--determinizations", type=int, default=4)
    ap.add_argument("--c-puct", type=float, default=1.5)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--pools", nargs="*", default=["act1"])
    ap.add_argument("--playout-cap", type=int, default=400)
    ap.add_argument("--json-out", default="output/combat_mcts_retest.jsonl")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.action_space import get_action_mask
    from sts2_env.search.combat_mcts import (
        COMBAT_ACTIONS,
        CombatMCTS,
        MCTSConfig,
        SB3PolicyEvaluator,
        apply_combat_action,
        clone_combat,
        make_bare_obs_builder,
    )
    from train_hierarchical import make_combat_env

    print(f"combat model : {args.combat_model}")
    print(f"decks        : {args.deck_file}")
    print(f"search       : {args.sims} sims x {args.determinizations} determinizations")

    model = MaskablePPO.load(args.combat_model, device="cpu")
    evaluator = SB3PolicyEvaluator(model)
    obs_builder = make_bare_obs_builder()

    env = make_combat_env(ascension=args.ascension, seed=555,
                          pools=tuple(args.pools), deck_file=args.deck_file,
                          mix_progressive=0.0)

    # ---- snapshot contested opening positions ----
    snaps = []
    for i in range(args.snapshots):
        env.reset(seed=555_000 + i)
        c = env.combat
        if c is None or c.is_over:
            continue
        snaps.append({
            "i": i,
            "entry_hp": float(c.primary_player.current_hp),
            "max_hp": float(c.primary_player.max_hp),
            "enemy_hp": int(sum(int(getattr(e, "current_hp", 0))
                                for e in (c.enemies or []))),
            "combat": clone_combat(c),
        })
    print(f"snapshots    : {len(snaps)}\n")

    def playout(snap, use_mcts: bool, seed: int) -> dict:
        combat = clone_combat(snap["combat"])
        cfg = MCTSConfig(n_simulations=args.sims,
                         n_determinizations=args.determinizations,
                         c_puct=args.c_puct, dirichlet_eps=0.0, seed=seed)
        n = dis = dis_sym = dis_real = 0
        while not combat.is_over and n < args.playout_cap:
            p = combat.primary_player
            if p is None or not p.is_alive:
                break
            m = get_action_mask(combat).astype(bool)
            if not m.any():
                break
            full = np.zeros(COMBAT_ACTIONS, dtype=bool)
            full[:COMBAT_ACTIONS] = m
            greedy, _ = model.predict(obs_builder(combat), action_masks=full,
                                      deterministic=True)
            greedy = int(greedy)
            if not m[greedy]:
                greedy = int(np.flatnonzero(m)[0])
            if use_mcts:
                mcts = CombatMCTS(evaluator, obs_builder, cfg)
                visits, _ = mcts.run(combat, root_mask115=m, base_seed=seed + n)
                masked = visits * m
                a = int(np.argmax(masked)) if masked.sum() > 0 else greedy
                if a != greedy:
                    dis += 1
                    # A "disagreement" that swaps hand[0]=Strike for
                    # hand[3]=Strike is not a different decision at all.
                    # Hand-index symmetry inflates the raw rate, so classify
                    # each override by the CARD it resolves to.
                    same = _same_card(combat, a, greedy)
                    if same:
                        dis_sym += 1
                    else:
                        dis_real += 1
            else:
                a = greedy
            apply_combat_action(combat, a)
            n += 1
        p = combat.primary_player
        return {"survived": bool(p is not None and p.is_alive),
                "hp": float(max(getattr(p, "current_hp", 0) or 0, 0)),
                "decisions": n, "disagreements": dis,
                "dis_symmetric": dis_sym, "dis_real": dis_real}

    recs = []
    t0 = time.time()
    for k, s in enumerate(snaps):
        seed = 31_000 + k * 97
        pol = playout(s, False, seed)
        mct = playout(s, True, seed)
        recs.append({**{q: s[q] for q in ("i", "entry_hp", "max_hp", "enemy_hp")},
                     "policy": pol, "mcts": mct})
        flag = "=" if pol["survived"] == mct["survived"] else (
            "MCTS+" if mct["survived"] else "POL+")
        print(f"  [{k+1:>3}/{len(snaps)}] hp{s['entry_hp']:>4.0f} vs {s['enemy_hp']:>4} "
              f"| pol {'LIVE' if pol['survived'] else 'DEAD'} hp{pol['hp']:>4.0f} "
              f"| mcts {'LIVE' if mct['survived'] else 'DEAD'} hp{mct['hp']:>4.0f} "
              f"d{mct['disagreements']}/{mct['decisions']} {flag}", flush=True)

    n = len(recs)
    pl = sum(r["policy"]["survived"] for r in recs)
    ml = sum(r["mcts"]["survived"] for r in recs)
    php = np.array([r["policy"]["hp"] for r in recs], float)
    mhp = np.array([r["mcts"]["hp"] for r in recs], float)
    b = sum(1 for r in recs if r["mcts"]["survived"] and not r["policy"]["survived"])
    c = sum(1 for r in recs if r["policy"]["survived"] and not r["mcts"]["survived"])
    nb = b + c
    p_mc = min(1.0, 2 * sum(comb(nb, i) for i in range(min(b, c) + 1)) / 2 ** nb) if nb else 1.0
    tot_d = sum(r["mcts"]["decisions"] for r in recs)
    tot_dis = sum(r["mcts"]["disagreements"] for r in recs)

    print(f"\n=== RESULT ({n} paired combats, {time.time()-t0:.0f}s) ===")
    print(f"  policy survived : {pl}/{n} ({pl/n:.1%})  mean HP left {php.mean():.1f}")
    print(f"  MCTS   survived : {ml}/{n} ({ml/n:.1%})  mean HP left {mhp.mean():.1f}")
    print(f"  paired HP delta : {(mhp-php).mean():+.2f} "
          f"(MCTS better {int((mhp>php).sum())}, worse {int((mhp<php).sum())})")
    print(f"  discordant      : MCTS-only {b}, policy-only {c} -> exact McNemar p={p_mc:.4f}")
    tot_sym = sum(r["mcts"]["dis_symmetric"] for r in recs)
    tot_real = sum(r["mcts"]["dis_real"] for r in recs)
    print(f"  disagreement    : {tot_dis}/{tot_d} decisions ({tot_dis/max(tot_d,1):.1%})")
    print(f"    of which same-card (hand-index symmetry) : {tot_sym}")
    print(f"    genuinely different plays                : {tot_real} "
          f"({tot_real/max(tot_d,1):.1%} of decisions)")
    if tot_real == 0:
        print("  >> Every override was a same-card permutation: search made no "
              "genuinely different play. Budget did not bite.")
    elif ml > pl:
        lift = ml / n - pl / n
        print(f"  >> Search LIFTS survival by {lift:+.1%}. At p={ml/n:.3f} a run lasts "
              f"~{1/max(1-ml/n,1e-6):.1f} combats vs ~{1/max(1-pl/n,1e-6):.1f}.")
    else:
        print("  >> Search does not improve survival even on contested positions.")

    if args.json_out:
        Path(args.json_out).write_text("\n".join(json.dumps(r) for r in recs),
                                       encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
