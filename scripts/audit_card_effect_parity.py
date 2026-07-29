#!/usr/bin/env python3
"""Compare every simulator card effect against the decompiled OnPlay body.

The existing audits check names, dynamic vars and static metadata. None of
them look at what a card DOES, which is how Dominate shipped wrong: the
decompile applies Vulnerable and then reads the target's Vulnerable to size
a Strength gain, while the simulator only read it. On a clean target the sim
card did nothing, so the planner -- correctly optimising a wrong model --
played three Strikes for 18 instead of Dominate for 20.

That bug is invisible to in-simulator testing, because the planner and any
sim-side test consult the same wrong model. The only ground truth is the
decompiled C#.

Method
------
For each card, extract the set of *effects* each side performs:

* From the decompiled ``OnPlay``/``OnUpgrade``: ``PowerCmd.Apply<XPower>``,
  ``GetPower<XPower>``, damage/block/draw/energy commands.
* From the registered simulator effect function: ``PowerId.X`` applications
  and reads, and the corresponding sim helpers.

Then compare. Two findings are reported separately because they mean
different things:

* **APPLY-MISSING** -- the game applies a power the sim never applies. This
  is the Dominate class and is the highest-signal finding.
* **READ-WITHOUT-APPLY** -- both sides read a power, but the game applies it
  first in the same method. Ordering bugs hide here and are invisible to any
  test that starts from an already-debuffed target.

This is a heuristic over source text, not a proof. It is tuned to surface
candidates for human review with few misses, so expect false positives --
every hit needs checking against the decompile before changing anything.
"""

from __future__ import annotations

import argparse
import json
import re
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
DECOMP = REPO / "decompiled_v0.109.0" / "MegaCrit.Sts2.Core.Models.Cards"

#: C# power class -> simulator PowerId member.
POWER_MAP = {
    "VulnerablePower": "VULNERABLE",
    "StrengthPower": "STRENGTH",
    "WeakPower": "WEAK",
    "FrailPower": "FRAIL",
    "DexterityPower": "DEXTERITY",
    "PoisonPower": "POISON",
    "ArtifactPower": "ARTIFACT",
    "ThornsPower": "THORNS",
    "MetallicizePower": "METALLICIZE",
    "PlatedArmorPower": "PLATED_ARMOR",
    "RegenPower": "REGEN",
    "RitualPower": "RITUAL",
    "IntangiblePower": "INTANGIBLE",
    "BufferPower": "BUFFER",
    "BarricadePower": "BARRICADE",
    "DoubleTapPower": "DOUBLE_TAP",
    "BurstPower": "BURST",
    "EnergizedPower": "ENERGIZED",
    "FocusPower": "FOCUS",
    "WrathPower": "WRATH",
    "CalmPower": "CALM",
    "NoDrawPower": "NO_DRAW",
    "ConfusionPower": "CONFUSION",
    "EntanglePower": "ENTANGLE",
    "FlamePower": "FLAME",
    "DoomPower": "DOOM",
}

APPLY_RE = re.compile(r"PowerCmd\.Apply<(\w+)>")
GETPOWER_RE = re.compile(r"GetPower<(\w+)>")
ONPLAY_RE = re.compile(r"OnPlay\s*\([^)]*\)\s*\{(.*?)\n\t\}", re.DOTALL)


def csharp_effects(path: Path) -> dict:
    text = path.read_text(encoding="utf-8", errors="ignore")
    m = ONPLAY_RE.search(text)
    body = m.group(1) if m else text
    def _pid(p: str) -> str:
        """C# power class -> PowerId member, falling back to CamelCase split.

        Without the fallback every power missing from POWER_MAP looked like
        an unimplemented apply, which is most of the pool.
        """
        if p in POWER_MAP:
            return POWER_MAP[p]
        base = p[:-5] if p.endswith("Power") else p
        return camel_to_upper(base)

    applies = {_pid(p) for p in APPLY_RE.findall(body)}
    reads = {_pid(p) for p in GETPOWER_RE.findall(body)}
    return {
        "applies": applies,
        "reads": reads,
        "damage": bool(re.search(r"DamageCmd|DealDamage|AttackDamage", body)),
        "block": bool(re.search(r"BlockCmd|GainBlock", body)),
        "draw": bool(re.search(r"DrawCmd|DrawCards", body)),
        "has_onplay": m is not None,
    }


def sim_effects(src: str) -> dict:
    applies = set(re.findall(r"apply_power_to\([^)]*PowerId\.(\w+)", src))
    applies |= set(re.findall(r"apply_power\([^)]*PowerId\.(\w+)", src))
    reads = set(re.findall(r"get_power_amount\(\s*PowerId\.(\w+)", src))
    mentioned = set(re.findall(r"PowerId\.(\w+)", src))
    return {
        "applies": applies,
        "reads": reads,
        "mentioned": mentioned,
        # Match the simulator's ACTUAL vocabulary. The first version guessed
        # (deal_damage / gain_block) and produced 137 false "sim does nothing"
        # findings on cards like Bash that plainly deal damage -- the tool was
        # wrong, not the simulator.
        "damage": bool(re.search(r"apply_damage|calculate_damage|deal_damage"
                                 r"|_deal_damage", src)),
        "block": bool(re.search(r"gain_block|add_block|apply_block|_gain_block"
                                r"|calculate_block", src)),
        "draw": bool(re.search(r"draw_card|draw_cards|_draw", src)),
    }


def load_sim_effect_sources() -> dict[str, str]:
    """card_id -> source text of its registered effect function."""
    out: dict[str, str] = {}
    for py in (REPO / "sts2_env" / "cards").glob("*.py"):
        text = py.read_text(encoding="utf-8", errors="ignore")
        # Split on the decorator so each chunk is one card's effect.
        parts = re.split(r"@register_effect\(CardId\.(\w+)\)", text)
        for i in range(1, len(parts) - 1, 2):
            cid = parts[i]
            body = parts[i + 1]
            nxt = body.find("@register_effect")
            out[cid] = body[:nxt] if nxt > 0 else body
    return out


def camel_to_upper(name: str) -> str:
    s1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    return re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", s1).upper()


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--json-out", default="output/card_effect_parity.json")
    ap.add_argument("--show", type=int, default=40)
    args = ap.parse_args()

    if not DECOMP.is_dir():
        print(f"decompiled cards not found at {DECOMP}")
        return 2

    sim_src = load_sim_effect_sources()
    print(f"simulator effect functions: {len(sim_src)}")
    cs_files = sorted(DECOMP.glob("*.cs"))
    print(f"decompiled card models    : {len(cs_files)}\n")

    apply_missing, read_without_apply, kind_mismatch, unmatched = [], [], [], []

    for f in cs_files:
        cid = camel_to_upper(f.stem)
        src = sim_src.get(cid)
        if src is None:
            # Try a few spellings before calling it unmatched.
            for alt in (cid + "_CARD", cid.replace("_", "")):
                if alt in sim_src:
                    src = sim_src[alt]
                    break
        if src is None:
            unmatched.append(cid)
            continue

        cs = csharp_effects(f)
        py = sim_effects(src)
        if not cs["has_onplay"]:
            continue

        missing = cs["applies"] - py["applies"] - py["mentioned"]
        if missing:
            apply_missing.append({"card": cid, "game_applies": sorted(cs["applies"]),
                                  "sim_applies": sorted(py["applies"]),
                                  "missing": sorted(missing)})

        # The Dominate signature: game applies X then reads X; sim only reads.
        for p in (cs["applies"] & cs["reads"]):
            if p in py["reads"] and p not in py["applies"]:
                read_without_apply.append({
                    "card": cid, "power": p,
                    "note": "game applies then reads; sim only reads",
                })

        for kind in ("damage", "block", "draw"):
            if cs[kind] and not py[kind]:
                kind_mismatch.append({"card": cid, "kind": kind,
                                      "note": f"game does {kind}, sim shows none"})

    print(f"=== READ-WITHOUT-APPLY (the Dominate signature) : {len(read_without_apply)} ===")
    for r in read_without_apply[: args.show]:
        print(f"  {r['card']:<28} {r['power']}")

    print(f"\n=== APPLY-MISSING : {len(apply_missing)} ===")
    for r in apply_missing[: args.show]:
        print(f"  {r['card']:<28} game applies {r['missing']}, sim applies {r['sim_applies']}")

    print(f"\n=== EFFECT-KIND MISMATCH : {len(kind_mismatch)} ===")
    for r in kind_mismatch[: args.show]:
        print(f"  {r['card']:<28} {r['note']}")

    print(f"\n  unmatched card names (no sim effect found): {len(unmatched)}")

    out = {
        "read_without_apply": read_without_apply,
        "apply_missing": apply_missing,
        "kind_mismatch": kind_mismatch,
        "unmatched": unmatched,
    }
    Path(args.json_out).parent.mkdir(parents=True, exist_ok=True)
    Path(args.json_out).write_text(json.dumps(out, indent=2), encoding="utf-8")
    print(f"\nwrote {args.json_out}")
    print("\nThese are CANDIDATES from a source-text heuristic, not proven bugs.")
    print("Check each against the decompile before changing anything.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
