"""Human-readable content descriptions for the web/CLI UIs.

This package produces short effect-text strings for cards, powers, potions and
relics. The simulator itself carries no localization/description strings, so the
text here is derived from the simulator's own effect model (card data, the
decompiled-parity reference effect list, and the power classes' docstrings) so
that a tooltip always reflects what *this* simulator actually does.
"""

from sts2_env.content.descriptions import (
    card_damage_clause,
    card_description,
    potion_description,
    power_description,
    relic_description,
)
from sts2_env.content.preview import card_preview

__all__ = [
    "card_damage_clause",
    "card_description",
    "card_preview",
    "potion_description",
    "power_description",
    "relic_description",
]
