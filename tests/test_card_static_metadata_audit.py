"""Card static metadata parity audit."""

from scripts.audit_card_dynamic_vars import collect_card_dynamic_var_mismatches
from scripts.audit_card_static_metadata import collect_static_metadata_mismatches
from sts2_env.cards.factory import create_card
from sts2_env.core.enums import CardId, OrbEvokeType


def test_card_static_metadata_matches_decompiled_models() -> None:
    assert collect_static_metadata_mismatches() == []


def test_selected_card_dynamic_vars_match_decompiled_models() -> None:
    assert collect_card_dynamic_var_mismatches() == []


def test_orb_evoke_type_matches_decompiled_card_properties() -> None:
    assert create_card(CardId.DUALCAST).orb_evoke_type is OrbEvokeType.FRONT
    assert create_card(CardId.QUADCAST).orb_evoke_type is OrbEvokeType.FRONT
    assert create_card(CardId.MULTI_CAST).orb_evoke_type is OrbEvokeType.ALL
    assert create_card(CardId.SHATTER).orb_evoke_type is OrbEvokeType.ALL
    assert create_card(CardId.ZAP).orb_evoke_type is OrbEvokeType.NONE
