#!/usr/bin/env python3
"""Death-cause forensics for trained full-run checkpoints.

``inspect_episodes.py`` answers *how far* a policy gets. This answers *what
kills it*: the room type and the exact enemy lineup of the encounter the run
died in, plus the HP the policy walked into that encounter with.

That distinction matters. A policy that dies at floor 8 because it keeps
walking into elites at 20 HP has a routing/economy problem; one that dies at
floor 8 in ordinary monster rooms it entered at full HP has a combat-execution
problem. Those call for completely different interventions.

Examples
--------
Break down 200 deaths of the newest scratch-lineage checkpoint::

    python scripts/diagnose_deaths.py --ckpt-dir output/necrobinder_scratch/G1 --episodes 200

Same, but also report where HP was actually lost across the whole run::

    python scripts/diagnose_deaths.py --episodes 200 --hp-ledger
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

import numpy as np

DEFAULT_CKPT_DIR = Path("output/necrobinder_scratch/G1")
DEFAULT_SEED_BASE = 30_000_000


def latest_checkpoint(ckpt_dir: Path) -> Path:
    candidates = sorted(ckpt_dir.glob("*.zip"), key=lambda p: p.stat().st_mtime)
    if not candidates:
        raise SystemExit(f"No checkpoints found in {ckpt_dir}")
    return candidates[-1]


def encounter_signature(mgr) -> tuple[str, str]:
    """(room_type, enemy lineup) for the combat the manager is currently in.

    Returns ("none", "") outside combat -- deaths outside combat are real
    (events can kill) and worth counting separately rather than mislabelling.
    """
    room_type = getattr(mgr, "_current_room_type", None)
    room_name = getattr(room_type, "name", str(room_type)) if room_type is not None else "NONE"
    combat = mgr.get_combat_state()
    if combat is None:
        return room_name, ""
    names: list[str] = []
    for enemy in getattr(combat, "enemies", []) or []:
        # Creature is a single generic class; identity lives in monster_id.
        name = getattr(enemy, "monster_id", None) or type(enemy).__name__
        names.append(str(name))
    return room_name, " + ".join(sorted(names))


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--checkpoint", default=None)
    parser.add_argument("--ckpt-dir", default=str(DEFAULT_CKPT_DIR))
    parser.add_argument("--episodes", type=int, default=200)
    parser.add_argument("--seed-base", type=int, default=DEFAULT_SEED_BASE)
    parser.add_argument("--ascension", type=int, default=0)
    parser.add_argument("--acts", type=int, default=2)
    parser.add_argument("--max-steps", type=int, default=3000)
    parser.add_argument("--hp-ledger", action="store_true",
                        help="Also report net HP change attributed to each room type")
    parser.add_argument("--json-out", default=None,
                        help="Write the raw per-episode records here for further analysis")
    parser.add_argument("--top", type=int, default=12)
    args = parser.parse_args()

    import sts2_env.events  # noqa: F401  (registry side effects)
    from sb3_contrib import MaskablePPO
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    ckpt = Path(args.checkpoint) if args.checkpoint else latest_checkpoint(Path(args.ckpt_dir))
    print(f"checkpoint: {ckpt}")
    model = MaskablePPO.load(str(ckpt), device="cpu")

    env = RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=args.ascension,
        max_act_count=args.acts,
    )
    env.set_shaping_scale(0.0)

    outcomes = Counter()
    death_room = Counter()
    death_encounter = Counter()
    death_floor: list[int] = []
    entry_hp_by_room: dict[str, list[float]] = defaultdict(list)
    hp_delta_by_room: dict[str, list[float]] = defaultdict(list)
    records: list[dict] = []

    for i in range(args.episodes):
        seed = args.seed_base + i
        obs, info = env.reset(seed=seed)
        mgr = env._mgr
        done = trunc = False
        steps = 0
        prev_hp = float(info.get("hp", 0.0))
        # Attribute HP purely on ROOM-OBJECT identity. An earlier version keyed
        # on (room_type, floor); because _current_room_type persists after the
        # room is left, that minted phantom visits on the following floors and
        # badly skewed both visit counts and net-HP totals. The room object is
        # replaced on every real room entry, so identity is the honest key.
        prev_room_id = None
        prev_room_name = None
        room_entry_hp = prev_hp
        # Signature of the encounter that was live on the LAST step -- once the
        # run ends the manager may already have torn the combat down.
        last_sig = ("NONE", "")

        while not (done or trunc) and steps < args.max_steps:
            mask = env.action_masks()
            action, _ = model.predict(obs, action_masks=mask, deterministic=True)
            sig = encounter_signature(mgr)
            if sig[0] != "NONE":
                last_sig = sig
            room_obj = getattr(mgr, "_current_room", None)
            room_id = id(room_obj) if room_obj is not None else None

            obs, reward, done, trunc, info = env.step(int(action))
            steps += 1
            hp = float(info.get("hp", prev_hp))

            if room_id is not None and room_id != prev_room_id:
                if prev_room_id is not None and prev_room_name is not None:
                    hp_delta_by_room[prev_room_name].append(prev_hp - room_entry_hp)
                entry_hp_by_room[sig[0]].append(prev_hp)
                room_entry_hp = prev_hp
                prev_room_id = room_id
                prev_room_name = sig[0]
            prev_hp = hp

        # Close out the final room so fatal encounters are not silently dropped.
        if prev_room_name is not None:
            hp_delta_by_room[prev_room_name].append(prev_hp - room_entry_hp)

        floor = int(info.get("floor", 0))
        rec = {
            "seed": seed,
            "floor": floor,
            "act": int(info.get("act", 0)),
            "steps": steps,
            "room": last_sig[0],
            "encounter": last_sig[1],
        }

        if done and info.get("won", False):
            outcomes["win"] += 1
            rec["outcome"] = "win"
        elif done:
            outcomes["death"] += 1
            rec["outcome"] = "death"
            death_floor.append(floor)
            death_room[last_sig[0]] += 1
            death_encounter[f"{last_sig[0]}: {last_sig[1] or '(no combat)'}"] += 1
        else:
            outcomes["truncated"] += 1
            rec["outcome"] = "truncated"
        records.append(rec)

        if (i + 1) % 50 == 0:
            print(f"  ... {i + 1}/{args.episodes} episodes")

    total = sum(outcomes.values())
    print(f"\n=== OUTCOMES ({total} episodes, asc {args.ascension}, acts {args.acts}) ===")
    print(f"wins {outcomes['win']}  deaths {outcomes['death']}  truncated {outcomes['truncated']}")
    if death_floor:
        arr = np.array(death_floor)
        print(f"death floor: mean {arr.mean():.2f}  median {np.median(arr):.0f}  "
              f"p10 {np.percentile(arr, 10):.0f}  p90 {np.percentile(arr, 90):.0f}")

    print(f"\n=== DEATHS BY ROOM TYPE ===")
    for room, n in death_room.most_common():
        print(f"  {room:<12} {n:>4}  ({n / max(total, 1):.1%} of episodes)")

    print(f"\n=== DEATHS BY ENCOUNTER (top {args.top}) ===")
    for enc, n in death_encounter.most_common(args.top):
        print(f"  {n:>4}  {enc}")

    if args.hp_ledger:
        print(f"\n=== HP LEDGER (net HP change per room type, all episodes) ===")
        rows = sorted(hp_delta_by_room.items(),
                      key=lambda kv: sum(kv[1]))
        for room, deltas in rows:
            a = np.array(deltas)
            print(f"  {room:<12} visits {len(a):>5}  net {a.sum():>8.0f}  "
                  f"mean {a.mean():>6.2f}  worst {a.min():>5.0f}")
        print(f"\n=== ENTRY HP BY ROOM TYPE ===")
        for room, hps in sorted(entry_hp_by_room.items()):
            a = np.array(hps)
            print(f"  {room:<12} n {len(a):>5}  mean entry HP {a.mean():>6.1f}  "
                  f"p10 {np.percentile(a, 10):>5.1f}")

    if args.json_out:
        Path(args.json_out).write_text(
            "\n".join(json.dumps(r) for r in records), encoding="utf-8"
        )
        print(f"\nwrote {len(records)} records to {args.json_out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
