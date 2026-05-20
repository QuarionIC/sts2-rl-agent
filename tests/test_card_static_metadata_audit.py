"""Card static metadata parity audit."""

from scripts.audit_card_dynamic_vars import collect_card_dynamic_var_mismatches
from scripts.audit_card_static_metadata import collect_static_metadata_mismatches


def test_card_static_metadata_matches_decompiled_models() -> None:
    assert collect_static_metadata_mismatches() == []


def test_selected_card_dynamic_vars_match_decompiled_models() -> None:
    assert collect_card_dynamic_var_mismatches() == []
