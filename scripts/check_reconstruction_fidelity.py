#!/usr/bin/env python3
"""Diff a reconstructed CombatState against the live payload it came from.

The planner is only as good as the state it searches. Every instrument so
far measured whether the planner RAN (probe passed, plan produced, no
divergence) -- none checked whether the rebuilt fight actually matches the
one on screen. A faithful-looking log with sub-optimal play is exactly what
a silently wrong reconstruction produces.

This connects to the running game, grabs real combat payloads, rebuilds each
one, and compares field by field: player HP/block/energy, per-enemy HP and
block, and -- the one that decides whether blocking is right -- the enemy's
INTENT and its damage.
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
from pathlib import Path


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=9002)
    ap.add_argument("--samples", type=int, default=3)
    ap.add_argument("--from-file", default=None,
                    help="Diff a saved payload instead of connecting")
    ap.add_argument("--save", default="output/fidelity_payloads.jsonl")
    args = ap.parse_args()

    import sts2_env.events  # noqa: F401

    from sts2_env.bridge.combat_reconstruct import probe_payload, reconstruct_combat
    from sts2_env.search.combat_planner import incoming_damage

    payloads: list[dict] = []
    if args.from_file:
        for line in Path(args.from_file).read_text(encoding="utf-8").splitlines():
            if line.strip():
                payloads.append(json.loads(line))
    else:
        from sts2_env.bridge.client import STS2GameClient

        c = STS2GameClient(host=args.host, port=args.port)
        c.connect()
        print(f"connected; waiting for {args.samples} combat payloads ...")
        for _ in range(80):
            try:
                st = c.receive_state()
            except Exception as e:
                print(f"recv ended: {type(e).__name__}")
                break
            if st and st.get("type") == "combat_action":
                payloads.append(st)
                if len(payloads) >= args.samples:
                    break
        c.disconnect()

    if not payloads:
        print("no combat payloads captured")
        return 1
    Path(args.save).parent.mkdir(parents=True, exist_ok=True)
    Path(args.save).write_text("\n".join(json.dumps(p) for p in payloads),
                               encoding="utf-8")
    print(f"captured {len(payloads)} payloads -> {args.save}\n")

    mismatches = 0
    for n, st in enumerate(payloads):
        print(f"--- payload {n} (round {st.get('round')}) ---")
        pr = probe_payload(st)
        if not pr.can_plan:
            print(f"  PROBE FAILED: {pr.reason()}")
            mismatches += 1
            continue
        combat = reconstruct_combat(st)
        if combat is None:
            print("  RECONSTRUCT RETURNED None")
            mismatches += 1
            continue

        wp = (st.get("player") or {})
        sp = combat.primary_player
        sstate = combat.combat_player_states[0]
        rows = [
            ("player hp", wp.get("hp"), sp.current_hp),
            ("player block", wp.get("block", 0), sp.block),
            ("energy", wp.get("energy"), sstate.energy),
            ("hand size", len(st.get("hand", []) or []), len(sstate.hand)),
            ("draw size", len(st.get("draw_pile", []) or []), len(sstate.draw)),
        ]
        for label, wire, sim in rows:
            ok = (wire is None) or (int(wire) == int(sim))
            if not ok:
                mismatches += 1
            print(f"  {label:<14} wire={wire!s:<6} sim={sim!s:<6} {'ok' if ok else 'MISMATCH'}")

        live_enemies = [e for e in (st.get("enemies") or [])
                        if e.get("is_alive", True) and int(e.get("hp", 0) or 0) > 0]
        sim_enemies = [e for e in combat.enemies if e.is_alive]
        print(f"  enemies        wire={len(live_enemies)} sim={len(sim_enemies)}"
              f" {'ok' if len(live_enemies) == len(sim_enemies) else 'MISMATCH'}")

        wire_incoming = 0
        for e in live_enemies:
            try:
                wire_incoming += int(e.get("intent_damage") or 0) * int(
                    e.get("intent_hits", 1) or 1)
            except Exception:
                pass
        sim_incoming = incoming_damage(combat)
        ok = wire_incoming == sim_incoming
        if not ok:
            mismatches += 1
        print(f"  INCOMING DMG   wire={wire_incoming:<6} sim={sim_incoming:<6} "
              f"{'ok' if ok else 'MISMATCH  <-- planner will block wrong'}")
        for e, se in zip(live_enemies, sim_enemies):
            print(f"    {str(e.get('id'))[:28]:<28} wire hp={e.get('hp')} "
                  f"intent={e.get('intent')} ai_state={e.get('ai_state')} | "
                  f"sim hp={se.current_hp}")
        print()

    print(f"=== TOTAL MISMATCHES: {mismatches} ===")
    if mismatches:
        print("A mismatched INCOMING DMG means the planner is blocking against")
        print("the wrong attack; mismatched hp/energy means it is planning a")
        print("different fight entirely.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
