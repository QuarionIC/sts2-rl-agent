"""Card static metadata parity audit."""

from scripts.audit_card_static_metadata import collect_static_metadata_mismatches


def test_card_static_metadata_matches_decompiled_models() -> None:
    assert collect_static_metadata_mismatches() == []
