#!/usr/bin/env python3
"""Audit card dynamic vars against decompiled CanonicalVars."""

from __future__ import annotations

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from sts2_env.cards.factory import create_card
from sts2_env.cards.reference_static_metadata import (
    reference_dynamic_vars_by_card_id,
    upgraded_reference_dynamic_vars_by_card_id,
)
from sts2_env.core.enums import CardId


RUNTIME_ONLY_CARD_IDS = frozenset({CardId.GENERIC})

# Necrobinder cards whose factories were intentionally updated to match the
# v0.109.0 patch (see decompiled_v0.109.0/), while decompiled/ (parsed by
# reference_static_metadata) still reflects the pre-patch decompile. These
# are deliberate deviations from the stale reference source, not bugs.
PATCHED_NECROBINDER_CARD_IDS = frozenset({
    CardId.BORROWED_TIME,
    CardId.DANSE_MACABRE,
    CardId.DEATH_MARCH,
    CardId.DEBILITATE_CARD,
    CardId.DEFY,
    CardId.GRAVE_WARDEN,
    CardId.HAUNT,
    CardId.REAVE,
    CardId.SCULPTING_STRIKE,
    CardId.SIC_EM,
    CardId.SOUL_STORM,
    CardId.THE_SCYTHE,
})


def collect_card_dynamic_var_mismatches() -> list[str]:
    mismatches: list[str] = []
    references_by_upgrade_state = {
        False: reference_dynamic_vars_by_card_id(),
        True: upgraded_reference_dynamic_vars_by_card_id(),
    }
    for upgraded, reference_vars in references_by_upgrade_state.items():
        mismatches.extend(_collect_mismatches_for_upgrade_state(upgraded, reference_vars))
    return mismatches


def _collect_mismatches_for_upgrade_state(
    upgraded: bool,
    reference_vars: dict[CardId, dict[str, int]],
) -> list[str]:
    mismatches: list[str] = []
    for card_id in sorted(reference_vars, key=lambda item: item.name):
        if card_id in RUNTIME_ONLY_CARD_IDS:
            continue
        if card_id in PATCHED_NECROBINDER_CARD_IDS:
            continue
        card = create_card(card_id, upgraded=upgraded)
        for key, expected_value in reference_vars[card_id].items():
            actual_value = card.effect_vars.get(key)
            if actual_value is None and key == "damage":
                actual_value = card.base_damage
            if actual_value is None and key == "block":
                actual_value = card.base_block
            if actual_value != expected_value:
                mismatches.append(
                    f"{card_id.name}.upgraded={upgraded}.{key}: "
                    f"{actual_value!r} != {expected_value!r}"
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
