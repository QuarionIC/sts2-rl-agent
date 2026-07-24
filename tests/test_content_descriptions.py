"""Tests for the generated effect-text descriptions (cards, powers, potions)."""

import sts2_env.powers  # noqa: F401  (ensure power registration happens)

from sts2_env.cards.factory import create_card
from sts2_env.content import (
    card_description,
    potion_description,
    power_description,
    relic_description,
)
from sts2_env.core.enums import CardId, PowerId

# A cross-section of Necrobinder cards: basic, damage, block, power-apply,
# summon, scaling, X-cost and Power cards.
NECROBINDER_SAMPLE = [
    "STRIKE_NECROBINDER",
    "DEFEND_NECROBINDER",
    "BODYGUARD",
    "UNLEASH",
    "BLIGHT_STRIKE",
    "FEAR",
    "GRAVE_WARDEN",
    "DIRGE",
    "ERADICATE",
    "SOUL_STORM",
    "REAPER_FORM",
    "HAUNT",
    "CALCIFY_CARD",
    "END_OF_DAYS",
    "BORROWED_TIME",
]

COMMON_POWERS = [
    (PowerId.VULNERABLE, 2),
    (PowerId.WEAK, 1),
    (PowerId.STRENGTH, 3),
    (PowerId.FRAIL, 2),
    (PowerId.ARTIFACT, 1),
    (PowerId.POISON, 5),
    (PowerId.DOOM, 21),
    (PowerId.INTANGIBLE, 1),
    (PowerId.METALLICIZE, 6),
    (PowerId.BEAT_OF_DEATH, 1),  # Act 4 Heart
    (PowerId.CURL_UP, 5),        # legacy-act monster power
]


def test_card_description_is_nonempty_for_necrobinder_sample() -> None:
    for name in NECROBINDER_SAMPLE:
        card = create_card(CardId[name])
        for upgraded in (False, True):
            text = card_description(create_card(CardId[name], upgraded=upgraded))
            assert text and text.strip(), name
            # First line is always the "Type · cost" header.
            header = text.splitlines()[0]
            assert "·" in header, (name, header)


def test_card_description_reflects_damage_and_block_numbers() -> None:
    strike = create_card(CardId.STRIKE_NECROBINDER)
    assert "Deal 6 damage" in card_description(strike)
    strike_up = create_card(CardId.STRIKE_NECROBINDER, upgraded=True)
    assert "Deal 9 damage" in card_description(strike_up)

    defend = create_card(CardId.DEFEND_NECROBINDER)
    assert "Gain 5 Block" in card_description(defend)


def test_card_description_includes_keywords() -> None:
    # Afterlife is an Exhaust card; Reap has Retain.
    assert "Exhaust" in card_description(create_card(CardId.AFTERLIFE))
    assert "Retain" in card_description(create_card(CardId.REAP))


def test_card_description_surfaces_souls_and_osty() -> None:
    assert "Osty" in card_description(create_card(CardId.BODYGUARD))
    assert "Soul" in card_description(create_card(CardId.GRAVE_WARDEN))


def test_power_description_curated_common_powers() -> None:
    vulnerable = power_description(PowerId.VULNERABLE, 2)
    assert "50%" in vulnerable and "more damage" in vulnerable

    weak = power_description(PowerId.WEAK, 1)
    assert "25%" in weak

    strength = power_description(PowerId.STRENGTH, 3)
    assert "3" in strength

    artifact = power_description(PowerId.ARTIFACT, 2)
    assert "2" in artifact


def test_power_description_nonempty_for_sample_and_no_amount_leak() -> None:
    for power_id, amount in COMMON_POWERS:
        text = power_description(power_id, amount)
        assert text and text.strip(), power_id
        # The docstring placeholder word must never leak into player-facing text.
        assert "Amount" not in text, power_id


def test_power_description_covers_every_power_id() -> None:
    """Every PowerId a run can encounter yields non-empty text (no crash)."""
    for power_id in PowerId:
        text = power_description(power_id, 3)
        assert text and text.strip(), power_id
        assert "Amount" not in text, power_id


def test_power_description_accepts_name_string_and_missing_amount() -> None:
    assert power_description("VULNERABLE").strip()
    assert power_description(PowerId.STRENGTH).strip()


def test_potion_description_nonempty() -> None:
    assert "20 damage" in potion_description("FirePotion")
    # Unknown / uncurated potions still get a humanized fallback.
    assert potion_description("DeprecatedPotion").strip()


def test_descriptions_never_crash_on_exotic_input() -> None:
    # Exotic / fallback paths must degrade, not raise.
    assert power_description("NOT_A_REAL_POWER").strip()
    assert power_description(None).strip()
    assert relic_description("SOME_UNKNOWN_RELIC").strip()

    class _Broken:
        card_id = "NOT_A_CARD"

        def __getattr__(self, name):  # pragma: no cover - defensive
            raise RuntimeError("boom")

    # Should return a string (possibly empty) rather than raising.
    assert isinstance(card_description(_Broken()), str)


def test_status_and_curse_cards_describe_without_crash() -> None:
    for name in ("WOUND", "DAZED", "BURN", "SLIMED"):
        try:
            card = create_card(CardId[name])
        except KeyError:
            continue
        assert isinstance(card_description(card), str)
