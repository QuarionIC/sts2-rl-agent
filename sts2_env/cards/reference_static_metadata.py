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
VAR_CONSTRUCTOR_RE = re.compile(
    r"new\s+"
    r"(?P<type>[A-Za-z][A-Za-z0-9_]*)"
    r"(?:<(?P<generic>[A-Za-z][A-Za-z0-9_]*)>)?"
    r"\s*\("
)
INTEGER_LITERAL_RE = re.compile(r"-?\d+(?:\.0+)?m?")
REFERENCE_CLASS_ALIASES = {
    "Null": ("NULL_CARD",),
    "Sloth": ("SLOTH_STATUS",),
}
DYNAMIC_VAR_DEFAULT_NAMES = {
    "BlockVar": "Block",
    "CalculationBaseVar": "CalculationBase",
    "CalculationExtraVar": "CalculationExtra",
    "CardsVar": "Cards",
    "DamageVar": "Damage",
    "EnergyVar": "Energy",
    "ExtraDamageVar": "ExtraDamage",
    "ForgeVar": "Forge",
    "GoldVar": "Gold",
    "HealVar": "Heal",
    "HpLossVar": "HpLoss",
    "MaxHpVar": "MaxHp",
    "OstyDamageVar": "OstyDamage",
    "RepeatVar": "Repeat",
    "StarsVar": "Stars",
    "SummonVar": "Summon",
}
DYNAMIC_VAR_TYPES_WITH_DYNAMIC_VALUE = frozenset({
    "CalculatedBlockVar",
    "CalculatedDamageVar",
    "CalculatedVar",
})
REFERENCE_DYNAMIC_VAR_ALIASES = {
    "arsenal_power": "arsenal",
    "black_hole_power": "black_hole",
    "block_for_stars": "block_for_stars",
    "calcify_power": "calcify",
    "calculation_base": "calc_base",
    "calculation_extra": "calc_extra",
    "countdown_power": "countdown",
    "danse_macabre_power": "danse_macabre",
    "debilitate_power": "debilitate",
    "devour_life_power": "devour_life",
    "dexterity_power": "dexterity",
    "doom_power": "doom",
    "knockdown_power": "knockdown",
    "lethality_power": "lethality",
    "neurosurge_power": "neurosurge",
    "parry_power": "parry",
    "plating_power": "plating",
    "prep_time_power": "prep_time",
    "rolling_boulder_power": "rolling_boulder",
    "sentry_mode_power": "sentry_mode",
    "sic_em_power": "sic_em",
    "sleight_of_flesh_power": "sleight_of_flesh",
    "stars_per_turn": "stars_per_turn",
    "strength_power": "strength",
    "vigor_power": "vigor",
    "vulnerable_power": "vulnerable",
    "weak_power": "weak",
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


def reference_dynamic_vars_from_source(path: Path) -> dict[str, int]:
    result: dict[str, int] = {}
    for var_type, generic_type, arguments in _canonical_var_constructors(path.read_text()):
        parsed = _dynamic_var_key_value(var_type, generic_type, arguments)
        if parsed is None:
            continue
        key, value = parsed
        if key in result:
            raise ValueError(f"{path} has duplicate dynamic var key {key!r}")
        result[key] = value
    return result


def _canonical_var_constructors(source: str) -> list[tuple[str, str | None, str]]:
    expression = _canonical_vars_expression(source)
    constructors: list[tuple[str, str | None, str]] = []
    search_from = 0
    while True:
        match = VAR_CONSTRUCTOR_RE.search(expression, search_from)
        if match is None:
            break
        open_paren = match.end() - 1
        close_paren = _matching_delimiter(expression, open_paren, "(", ")")
        if close_paren is None:
            break
        constructors.append(
            (
                match.group("type"),
                match.group("generic"),
                expression[open_paren + 1 : close_paren],
            )
        )
        search_from = close_paren + 1
    return constructors


def _canonical_vars_expression(source: str) -> str:
    property_start = source.find("CanonicalVars")
    if property_start < 0:
        return ""
    expression_start = source.find("=>", property_start)
    if expression_start < 0:
        return ""
    expression_start += 2
    expression_end = _expression_statement_end(source, expression_start)
    return source[expression_start:expression_end]


def _expression_statement_end(source: str, start: int) -> int:
    depth = 0
    in_string = False
    escaped = False
    for index in range(start, len(source)):
        char = source[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char in "([{":
            depth += 1
        elif char in ")]}":
            depth -= 1
        elif char == ";" and depth == 0:
            return index
    return len(source)


def _matching_delimiter(source: str, start: int, opener: str, closer: str) -> int | None:
    depth = 0
    in_string = False
    escaped = False
    for index in range(start, len(source)):
        char = source[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == opener:
            depth += 1
        elif char == closer:
            depth -= 1
            if depth == 0:
                return index
    return None


def _split_arguments(arguments: str) -> list[str]:
    result: list[str] = []
    depth = 0
    in_string = False
    escaped = False
    current: list[str] = []
    for char in arguments:
        if in_string:
            current.append(char)
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
            current.append(char)
        elif char in "([{":
            depth += 1
            current.append(char)
        elif char in ")]}":
            depth -= 1
            current.append(char)
        elif char == "," and depth == 0:
            result.append("".join(current).strip())
            current = []
        else:
            current.append(char)
    if current:
        result.append("".join(current).strip())
    return result


def _dynamic_var_key_value(
    var_type: str,
    generic_type: str | None,
    arguments: str,
) -> tuple[str, int] | None:
    if var_type in DYNAMIC_VAR_TYPES_WITH_DYNAMIC_VALUE:
        return None
    args = _split_arguments(arguments)
    if not args:
        return None

    if args[0].startswith('"') and args[0].endswith('"'):
        name = args[0][1:-1]
        if len(args) < 2:
            return None
        value_text = args[1]
    elif var_type == "PowerVar" and generic_type is not None:
        name = generic_type
        value_text = args[0]
    else:
        name = DYNAMIC_VAR_DEFAULT_NAMES.get(var_type)
        value_text = args[0]

    if name is None:
        return None
    value = _integer_literal(value_text)
    if value is None:
        return None
    key = snake_case(name)
    return REFERENCE_DYNAMIC_VAR_ALIASES.get(key, key), value


def _integer_literal(value_text: str) -> int | None:
    normalized = value_text.strip()
    if INTEGER_LITERAL_RE.fullmatch(normalized) is None:
        return None
    normalized = normalized.removesuffix("m")
    if "." in normalized:
        return int(float(normalized))
    return int(normalized)


@lru_cache(maxsize=1)
def reference_metadata_by_card_id() -> dict[CardId, ReferenceCardStaticMetadata]:
    return {
        metadata.card_id: metadata
        for metadata in (
            reference_metadata_from_source(path)
            for path in sorted((REPO_ROOT / REFERENCE_CARD_DIR).glob("*.cs"))
        )
    }


@lru_cache(maxsize=1)
def reference_dynamic_vars_by_card_id() -> dict[CardId, dict[str, int]]:
    return {
        card_id_for_reference_class(path.stem): reference_dynamic_vars_from_source(path)
        for path in sorted((REPO_ROOT / REFERENCE_CARD_DIR).glob("*.cs"))
    }
