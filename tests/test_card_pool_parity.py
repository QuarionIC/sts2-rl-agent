"""Card-pool membership and order parity tests."""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

from sts2_env.cards.factory import eligible_registered_cards
from sts2_env.characters.all import get_character
from sts2_env.core.card_pools import CardPoolId
from sts2_env.core.enums import CardId, CardType


_CAMEL_WORD_BOUNDARY_RE = re.compile(r"(.)([A-Z][a-z]+)")
_LOWER_TO_UPPER_BOUNDARY_RE = re.compile(r"([a-z0-9])([A-Z])")
_CARD_POOL_DIR = Path("decompiled/MegaCrit.Sts2.Core.Models.CardPools")
_CARD_CLASS_DIR = Path("decompiled/MegaCrit.Sts2.Core.Models.Cards")
_CARD_POOL_CLASS_RE = re.compile(r"ModelDb\.Card<([A-Za-z0-9_]+)>\(\)")
_EXPLICIT_CARD_ALIASES = {
    "Sloth": ("SLOTH_STATUS",),
}

_MULTIPLAYER_ONLY_RE = re.compile(
    r"CardMultiplayerConstraint\s+MultiplayerConstraint\s*=>\s*"
    r"CardMultiplayerConstraint\.MultiplayerOnly"
)

#: MultiplayerOnly card classes the simulator does not model. IRunState
#: .CardMultiplayerConstraint returns SingleplayerOnly when Players.Count <= 1
#: and CardPoolModel.GetUnlockedCards then strips every MultiplayerOnly card,
#: so these are unreachable in the single-player runs the agent plays.
#:
#: This is deliberately an explicit list and NOT "every MultiplayerOnly class":
#: 37 classes are MultiplayerOnly and 21 of them ARE modelled and ARE present in
#: the simulator pools (DemonicShield, Tank, ...), because character.card_pool
#: mirrors GenerateAllCards() and the multiplayer filtering happens later via
#: matches_player_count(). A blanket filter would break all six pools below.
_UNMODELLED_MULTIPLAYER_ONLY = frozenset({
    "Blaze", "Midnight", "Outrage",                  # Ironclad
    "BladeSymphony", "Concoct", "Fade",              # Silent
    "Hibernate", "ImitationLearning", "OneForAll",   # Defect
    "Constellation", "Plot", "Tutor",                # Regent
    "Cacophony", "Soulbound", "Underworld",          # Necrobinder
    "TheBall",                                       # Colorless
})


def _snake_case(name: str) -> str:
    first = _CAMEL_WORD_BOUNDARY_RE.sub(r"\1_\2", name)
    return _LOWER_TO_UPPER_BOUNDARY_RE.sub(r"\1_\2", first).upper()


def _card_id_for_reference_class(name: str) -> CardId:
    snake_name = _snake_case(name)
    aliases = {name, snake_name, f"{snake_name}_CARD"}
    aliases.update(_EXPLICIT_CARD_ALIASES.get(name, ()))
    if name.endswith("Card"):
        stripped = name[:-4]
        aliases.update({stripped, _snake_case(stripped)})
    for alias in aliases:
        if alias in CardId.__members__:
            return CardId[alias]
    raise KeyError(f"No CardId alias for reference card class {name}")


def _reference_pool(pool_name: str) -> tuple[CardId, ...]:
    text = (_CARD_POOL_DIR / f"{pool_name}CardPool.cs").read_text()
    return tuple(
        _card_id_for_reference_class(name)
        for name in _CARD_POOL_CLASS_RE.findall(text)
        if name not in _UNMODELLED_MULTIPLAYER_ONLY
    )


def test_unmodelled_skips_are_all_multiplayer_only() -> None:
    for name in _UNMODELLED_MULTIPLAYER_ONLY:
        source = (_CARD_CLASS_DIR / f"{name}.cs").read_text()
        assert _MULTIPLAYER_ONLY_RE.search(source), name


def test_character_card_pools_match_reference_order() -> None:
    assert get_character("Ironclad").card_pool == _reference_pool("Ironclad")
    assert get_character("Silent").card_pool == _reference_pool("Silent")
    assert get_character("Defect").card_pool == _reference_pool("Defect")
    assert get_character("Regent").card_pool == _reference_pool("Regent")
    assert get_character("Necrobinder").card_pool == _reference_pool("Necrobinder")


def test_shared_registered_card_pools_match_reference_order() -> None:
    assert tuple(
        eligible_registered_cards(module_name="sts2_env.cards.colorless", generation_context=None)
    ) == _reference_pool("Colorless")
    assert tuple(
        eligible_registered_cards(card_type=CardType.CURSE, generation_context=None)
    ) == _reference_pool("Curse")
    assert tuple(
        eligible_registered_cards(card_pool=CardPoolId.EVENT, generation_context=None)
    ) == _reference_pool("Event")
    assert tuple(
        eligible_registered_cards(card_type=CardType.QUEST, generation_context=None)
    ) == _reference_pool("Quest")
    assert tuple(
        eligible_registered_cards(card_pool=CardPoolId.STATUS, generation_context=None)
    ) == _reference_pool("Status")
    assert tuple(
        eligible_registered_cards(card_pool=CardPoolId.TOKEN, generation_context=None)
    ) == _reference_pool("Token")


def test_deprecated_card_stays_out_of_normal_generation_pool() -> None:
    assert _reference_pool("Deprecated") == (CardId.DEPRECATED_CARD,)
    assert CardId.DEPRECATED_CARD not in eligible_registered_cards(generation_context=None)


def test_importing_characters_before_factory_does_not_cycle() -> None:
    script = (
        "from sts2_env.characters.all import get_character\n"
        "from sts2_env.cards.factory import eligible_registered_cards\n"
        "assert get_character('Ironclad').starting_hp == 80\n"
        "assert eligible_registered_cards(module_name='sts2_env.cards.colorless', generation_context=None)\n"
    )
    subprocess.run([sys.executable, "-c", script], check=True, capture_output=True, text=True)
