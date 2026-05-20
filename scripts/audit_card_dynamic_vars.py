#!/usr/bin/env python3
"""Audit selected card dynamic vars against decompiled CanonicalVars."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from sts2_env.cards.factory import create_card
from sts2_env.cards.reference_static_metadata import reference_dynamic_vars_by_card_id
from sts2_env.core.enums import CardId


CARD_DYNAMIC_VAR_AUDIT_IDS = frozenset({
    CardId.BRIGHTEST_FLAME,
    CardId.BURN,
    CardId.CAPACITOR,
    CardId.CELESTIAL_MIGHT,
    CardId.DECAY,
    CardId.FIGHT_ME,
    CardId.GLITTERSTREAM,
    CardId.GUNK_UP,
    CardId.ICE_LANCE,
    CardId.MODDED,
    CardId.NORMALITY,
    CardId.QUADCAST,
    CardId.REFRACT,
    CardId.SEVEN_STARS,
})


def collect_card_dynamic_var_mismatches() -> list[str]:
    mismatches: list[str] = []
    reference_vars = reference_dynamic_vars_by_card_id()
    for card_id in sorted(CARD_DYNAMIC_VAR_AUDIT_IDS, key=lambda item: item.name):
        card = create_card(card_id)
        for key, expected_value in reference_vars[card_id].items():
            actual_value = card.effect_vars.get(key)
            if actual_value is None and key == "damage":
                actual_value = card.base_damage
            if actual_value is None and key == "block":
                actual_value = card.base_block
            if actual_value != expected_value:
                mismatches.append(
                    f"{card_id.name}.{key}: {actual_value!r} != {expected_value!r}"
                )
    return mismatches


def main() -> int:
    mismatches = collect_card_dynamic_var_mismatches()
    for mismatch in mismatches:
        print(mismatch)
    if mismatches:
        print(f"{len(mismatches)} card dynamic var mismatch(es) found")
        return 1
    print("card dynamic var audit passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
