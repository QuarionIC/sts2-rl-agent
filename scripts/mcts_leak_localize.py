#!/usr/bin/env python3
"""Localise the run-level MCTS state leak: what does a search MUTATE?

``mcts_purity_check.py --level run`` proved that running a search changes the
run even when its recommendation is discarded (seed 10000002: arm A died at
0 HP, arm B survived at 34). The same check at the COMBAT level is clean, so the
leak is in the env-coupled path, not in ``clone_combat`` itself.

This script snapshots the LIVE state immediately before and after every search
call and reports every field that moved. It names the leak instead of guessing
at it.

Prime suspect, from reading ``make_run_obs_builder``:

    rs = combat_run_state(combat) or live_rs

When a clone carries no run_state of its own, the obs builder falls back to the
LIVE run state and hands it to ``_CloneMgrView`` alongside the CLONE's combat.
Anything in the encode path -- or any sim write-back such as
``sync_back_to_player_state`` reached during simulation -- then operates on live
objects. The direction of the observed error fits: arm B ended with MORE HP than
arm A, i.e. simulated outcomes leaked into the live player.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))


def rng_state(obj, prefix: str, out: dict) -> None:
    """Record every counter-based Rng reachable from obj.

    sts2_env.core.rng.Rng is COUNTER-based: its next draw is a pure function of
    (_base_seed, _seed, _counter). So a search that draws from a stream the live
    run also draws from advances _counter and silently changes every subsequent
    live draw -- no state is corrupted, but the trajectory diverges. That is the
    only mechanism consistent with the evidence so far: the run-level purity
    check fails, yet 60 searches mutated no HP, pile, power or mask.

    combat._run_rng is the suspicious edge -- a direct reference from the combat
    to the RUN-level rng set, which a clone would inherit.
    """
    seen = set()

    def walk(o, name, depth):
        if depth > 3 or id(o) in seen:
            return
        seen.add(id(o))
        if type(o).__name__ == "Rng":
            out[name] = [getattr(o, "_base_seed", None),
                         getattr(o, "_seed", None),
                         getattr(o, "_counter", None)]
            return
        d = getattr(o, "__dict__", None)
        if not d:
            return
        for k, v in list(d.items()):
            if v is None or isinstance(v, (int, float, str, bool, bytes)):
                continue
            if type(v).__name__ == "Rng" or hasattr(v, "__dict__"):
                walk(v, f"{name}.{k}", depth + 1)

    walk(obj, prefix, 0)


def snapshot(mgr) -> dict:
    """Everything a search must not touch."""
    rs = mgr.run_state
    p = rs.player
    snap = {
        "run_hp": int(p.current_hp),
        "run_max_hp": int(p.max_hp),
        "run_gold": int(getattr(p, "gold", 0)),
        "run_deck": len(p.deck),
        "run_floor": int(rs.total_floor),
        "run_act": int(rs.current_act_index),
        "relics": len(getattr(rs, "relics", []) or []),
        "potions": len([x for x in (getattr(p, "potions", []) or []) if x]),
    }
    rng_state(mgr.run_state, "rs", snap)
    # GLOBAL rng state, which the Rng walk above cannot see. If any code path
    # inside the search draws from numpy's or torch's global generator rather
    # than a seeded stream, the search advances it and every subsequent live
    # draw changes -- no state corrupted, trajectory still diverges.
    import hashlib
    try:
        st = np.random.get_state()
        snap["np_global_rng"] = hashlib.sha1(
            np.asarray(st[1]).tobytes() + str(st[2]).encode()).hexdigest()[:16]
    except Exception:
        pass
    try:
        import torch as _th
        snap["torch_cpu_rng"] = hashlib.sha1(
            _th.random.get_rng_state().numpy().tobytes()).hexdigest()[:16]
    except Exception:
        pass
    cb = mgr.get_combat_state()
    if cb is not None:
        rng_state(cb, "cb", snap)
        me = cb.primary_player
        st = cb.current_player_state
        snap.update({
            "cb_hp": int(me.current_hp),
            "cb_block": int(me.block),
            "cb_energy": int(st.energy),
            "cb_hand": len(st.hand),
            "cb_draw": len(st.draw),
            "cb_discard": len(st.discard),
            "cb_exhaust": len(st.exhaust),
            "cb_turn": int(cb.turn_count),
            "cb_powers": len(getattr(me, "powers", {}) or {}),
            "cb_enemy_hp": [int(e.current_hp) for e in cb.enemies],
            "cb_enemy_block": [int(e.block) for e in cb.enemies],
            "cb_enemy_powers": [len(getattr(e, "powers", {}) or {})
                                for e in cb.enemies],
            "cb_hand_ids": [c.card_id.name for c in st.hand],
            # PILE ORDER, not just length. The first version recorded only
            # len(draw/discard/exhaust), so a search that REORDERED a pile
            # without changing its size was invisible -- and that is exactly
            # the shape of the remaining leak: predict_reproducibility.py showed
            # the forward pass is reproducible (0 flips in 25 checks, min top-2
            # logit gap 0.158), so seed 10000013's divergence is state, and the
            # cross-architecture divergence was also pile order.
            "cb_draw_ids": [c.card_id.name for c in st.draw],
            "cb_discard_ids": [c.card_id.name for c in st.discard],
            "cb_exhaust_ids": [c.card_id.name for c in st.exhaust],
            "cb_deck_ids": [c.card_id.name for c in getattr(st, "starting_deck", [])],
            # POWER AMOUNTS, not counts. len(powers) misses Strength 2 -> 3
            # entirely, and power amounts feed the observation directly.
            "cb_powers_amt": sorted(
                (str(k), int(getattr(v, "amount", 0) or 0))
                for k, v in (getattr(me, "powers", {}) or {}).items()),
            "cb_enemy_powers_amt": [
                sorted((str(k), int(getattr(v, "amount", 0) or 0))
                       for k, v in (getattr(e, "powers", {}) or {}).items())
                for e in cb.enemies],
            # ENEMY AI MOVE STATE -- the intent the player sees. determinize()
            # reseeds the clone's streams, but if move selection advances on the
            # LIVE ai object the live intent changes, the observation changes,
            # and the policy legitimately picks differently with no state field
            # above ever moving. Nothing in the snapshot covered this.
            "cb_intents": [
                [(getattr(getattr(i, "intent_type", None), "name", "?"),
                  int(getattr(i, "damage", 0) or 0),
                  int(getattr(i, "hits", 1) or 1))
                 for i in (getattr(getattr(cb.enemy_ais.get(e.combat_id),
                                           "current_move", None),
                                   "intents", None) or [])]
                for e in cb.enemies],
            "cb_ai_repr": [
                repr(getattr(cb.enemy_ais.get(e.combat_id), "current_move", None))[:120]
                for e in cb.enemies],
        })
    return snap


def diff(a: dict, b: dict) -> dict:
    out = {}
    for k in a:
        if k in b and a[k] != b[k]:
            out[k] = [a[k], b[k]]
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--model", required=True, help="157-action MaskablePPO zip")
    ap.add_argument("--seeds", type=int, default=2)
    ap.add_argument("--seed-base", type=int, default=10_000_000)
    ap.add_argument("--n-sims", type=int, default=16)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=1)
    ap.add_argument("--max-searches", type=int, default=40,
                    help="Stop after this many searches per seed")
    ap.add_argument("--device", default="cpu")
    ap.add_argument("--json-out", default="output/mcts_leak.json")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401
    from sb3_contrib import MaskablePPO

    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
    from sts2_env.run.run_manager import RunManager
    from sts2_env.search.combat_mcts import MCTSConfig, mcts_action_distribution

    model = MaskablePPO.load(args.model, device=args.device)
    leaks: list[dict] = []
    n_searches = 0

    for si in range(args.seeds):
        seed = args.seed_base + si
        env = RichSTS2RunEnv(character_id="Necrobinder",
                             ascension_level=args.ascension,
                             max_act_count=args.max_act_count)
        env.set_shaping_scale(0.0)
        obs, info = env.reset(seed=seed)
        mgr = env._mgr
        done = tr = False
        n = searched = 0
        while not (done or tr) and n < 4000 and searched < args.max_searches:
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            if not legal.size:
                break
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)

            if mgr.phase == RunManager.PHASE_COMBAT and legal.size > 1:
                cb = mgr.get_combat_state()
                if cb is not None and not cb.is_over:
                    before = snapshot(mgr)
                    mcts_action_distribution(
                        env, model, n_sims=args.n_sims,
                        config=MCTSConfig(seed=seed * 1000 + n),
                        base_seed=seed * 1000 + n,
                    )
                    after = snapshot(mgr)
                    searched += 1
                    n_searches += 1
                    d = diff(before, after)
                    # The env's own mask must also be unchanged by a search.
                    mask_after = np.asarray(env.action_masks(), dtype=bool)
                    if not np.array_equal(mask, mask_after):
                        d["legal_mask"] = [int(mask.sum()), int(mask_after.sum())]
                    if d:
                        rec = {"seed": seed, "step": n, "changed": d}
                        leaks.append(rec)
                        print(f"LEAK seed {seed} step {n}: "
                              f"{json.dumps(d)[:300]}", flush=True)

            obs, r, done, tr, info = env.step(int(action))
            n += 1
        print(f"  seed {seed}: {searched} searches, "
              f"{sum(1 for l in leaks if l['seed'] == seed)} leaking", flush=True)

    print(f"\nsearches inspected : {n_searches}")
    print(f"searches that mutated live state: {len(leaks)}")
    if leaks:
        from collections import Counter
        fields = Counter(k for l in leaks for k in l["changed"])
        print("\nfields mutated, by frequency:")
        for k, c in fields.most_common():
            print(f"  {c:>5}  {k}")
        print("\nVERDICT: the search WRITES to live state. Every MCTS/ExIt result")
        print("         in this project is invalid, including the recorded")
        print("         NO-GO gate -- it compared a corrupted arm to a clean one.")
    else:
        print("\nVERDICT: no live mutation observed in this sample. The run-level")
        print("         divergence has another cause (e.g. RNG consumption).")

    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(
        {"_meta": vars(args), "searches": n_searches, "leaks": leaks},
        indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
