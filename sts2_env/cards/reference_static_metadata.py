"""Static card metadata parsed from decompiled card models."""

from __future__ import annotations

import re
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path

from sts2_env.core.enums import CardId, CardRarity, CardTag, CardType, TargetType


REPO_ROOT = Path(__file__).resolve().parents[2]
REFERENCE_CARD_DIR = Path("decompiled/MegaCrit.Sts2.Core.Models.Cards")
BASE_CONSTRUCTOR_RE = re.compile(
    r":\s*base\(\s*"
    r"(?P<cost>-?\d+)\s*,\s*"
    r"CardType\.(?P<card_type>[A-Za-z]+)\s*,\s*"
    r"CardRarity\.(?P<rarity>[A-Za-z]+)\s*,\s*"
    r"TargetType\.(?P<target_type>[A-Za-z]+)",
    re.DOTALL,
)
ENERGY_X_RE = re.compile(r"override\s+bool\s+HasEnergyCostX\s*=>\s*true\s*;")
STAR_X_RE = re.compile(r"override\s+bool\s+HasStarCostX\s*=>\s*true\s*;")
STAR_COST_RE = re.compile(r"override\s+int\s+CanonicalStarCost\s*=>\s*(?P<star_cost>-?\d+)\s*;")
CARD_KEYWORD_RE = re.compile(r"CardKeyword\.(?P<keyword>[A-Za-z]+)")
CARD_TAG_RE = re.compile(r"CardTag\.(?P<tag>[A-Za-z]+)")
CAMEL_WORD_BOUNDARY_RE = re.compile(r"(.)([A-Z][a-z]+)")
LOWER_TO_UPPER_BOUNDARY_RE = re.compile(r"([a-z0-9])([A-Z])")
REFERENCE_CLASS_ALIASES = {
    "Null": ("NULL_CARD",),
    "Sloth": ("SLOTH_STATUS",),
}


@dataclass(frozen=True)
class ReferenceCardStaticMetadata:
    card_id: CardId
    cost: int
    card_type: CardType
    target_type: TargetType
    rarity: CardRarity
    keywords: frozenset[str]
    tags: frozenset[CardTag]
    has_energy_cost_x: bool
    star_cost: int
    has_star_cost_x: bool


def snake_case(name: str) -> str:
    first = CAMEL_WORD_BOUNDARY_RE.sub(r"\1_\2", name)
    return LOWER_TO_UPPER_BOUNDARY_RE.sub(r"\1_\2", first).lower()


def card_id_for_reference_class(name: str) -> CardId:
    snake_name = snake_case(name).upper()
    aliases = {snake_name, f"{snake_name}_CARD", f"{snake_name}_STATUS"}
    aliases.update(REFERENCE_CLASS_ALIASES.get(name, ()))
    if name.endswith("Card"):
        stripped = name.removesuffix("Card")
        stripped_snake = snake_case(stripped).upper()
        aliases.update({stripped_snake, f"{stripped_snake}_CARD", f"{stripped_snake}_STATUS"})
    for alias in aliases:
        if alias in CardId.__members__:
            return CardId[alias]
    raise KeyError(f"No CardId alias for reference card class {name}")


def _property_expression(source: str, property_name: str) -> str:
    start = source.find(property_name)
    if start < 0:
        return ""
    end = source.find(";", start)
    if end < 0:
        return source[start:]
    return source[start : end + 1]


def _coerce_reference_rarity(name: str) -> CardRarity:
    if name == "Token":
        return CardRarity.STATUS
    return CardRarity[name.upper()]


def reference_metadata_from_source(path: Path) -> ReferenceCardStaticMetadata:
    source = path.read_text()
    constructor_match = BASE_CONSTRUCTOR_RE.search(source)
    if constructor_match is None:
        raise ValueError(f"{path} is missing a literal CardModel base constructor")

    keywords = frozenset(
        snake_case(keyword)
        for keyword in CARD_KEYWORD_RE.findall(_property_expression(source, "CanonicalKeywords"))
    )
    tags = frozenset(
        CardTag[snake_case(tag).upper()]
        for tag in CARD_TAG_RE.findall(_property_expression(source, "CanonicalTags"))
    )
    star_cost_match = STAR_COST_RE.search(source)

    return ReferenceCardStaticMetadata(
        card_id=card_id_for_reference_class(path.stem),
        cost=int(constructor_match.group("cost")),
        card_type=CardType[constructor_match.group("card_type").upper()],
        target_type=TargetType[snake_case(constructor_match.group("target_type")).upper()],
        rarity=_coerce_reference_rarity(constructor_match.group("rarity")),
        keywords=keywords,
        tags=tags,
        has_energy_cost_x=ENERGY_X_RE.search(source) is not None,
        star_cost=int(star_cost_match.group("star_cost")) if star_cost_match is not None else 0,
        has_star_cost_x=STAR_X_RE.search(source) is not None,
    )


@lru_cache(maxsize=1)
def reference_metadata_by_card_id() -> dict[CardId, ReferenceCardStaticMetadata]:
    return {
        metadata.card_id: metadata
        for metadata in (
            reference_metadata_from_source(path)
            for path in sorted((REPO_ROOT / REFERENCE_CARD_DIR).glob("*.cs"))
        )
    }
