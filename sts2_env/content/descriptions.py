"""Effect-text generation for cards, powers, potions and relics.

Nothing in the simulator stores human-readable descriptions, so this module
*produces* them from the simulator's own data:

* ``card_description`` combines a :class:`CardInstance`'s live data (cost, type,
  damage, block, keywords, effect vars) with the decompiled-parity effect list
  in ``docs/CARDS_REFERENCE.md`` and a small table of hand-written overrides for
  the cards whose effect is too conditional to derive from data.
* ``power_description`` uses a curated table for the common buffs/debuffs and
  falls back to the first sentence of the power class's own docstring (which is
  the sim's source of truth for that power's behaviour), then to a humanized
  name. This covers every ``PowerId`` a run can encounter.
* ``potion_description`` / ``relic_description`` are best-effort curated tables
  with a humanized-name fallback.

Every entry point is wrapped so it never raises: a missing/unknown id degrades
to a sensible partial description rather than crashing the caller.
"""

from __future__ import annotations

import re
from functools import lru_cache
from pathlib import Path

from sts2_env.core.enums import CardType, PowerId, TargetType

# ─────────────────────────────────────────────────────────────────────────
# Text helpers
# ─────────────────────────────────────────────────────────────────────────


def _humanize(name: object) -> str:
    """Turn ``SUMMON_NEXT_TURN`` / ``EnergyNextTurn`` into ``Summon Next Turn``."""
    text = str(name)
    if "." in text:
        text = text.rsplit(".", 1)[-1]
    text = text.replace("_", " ")
    text = re.sub(r"(?<=[a-z0-9])(?=[A-Z])", " ", text)
    text = re.sub(r"(?<=[A-Z])(?=[A-Z][a-z])", " ", text)
    return " ".join(part.capitalize() for part in text.split())


def _camel_to_snake(name: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).lower()


# ─────────────────────────────────────────────────────────────────────────
# Powers
# ─────────────────────────────────────────────────────────────────────────

# Curated one-line descriptions for the powers a player most often sees. Keys
# are ``PowerId`` names. ``{amount}`` is substituted with the live stack amount.
# Anything not listed here falls back to the power class's docstring.
_POWER_DESCRIPTIONS: dict[str, str] = {
    # -- core debuffs --
    "VULNERABLE": "Takes 50% more damage from attacks. Drops by 1 each turn.",
    "WEAK": "Deals 25% less attack damage. Drops by 1 each turn.",
    "FRAIL": "Gains 25% less Block from cards. Drops by 1 each turn.",
    "POISON": "Loses {amount} HP at the start of its turn, then Poison drops by 1.",
    "CONSTRICT": "Takes {amount} damage at the end of its turn.",
    "DOOM": "At the end of its turn, if its HP is at or below its Doom, it dies.",
    "ENTANGLED": "Cannot play Attack cards this turn.",
    "NO_DRAW": "Cannot draw any more cards this turn.",
    "DEBILITATE": "Amplifies its own Vulnerable and Weak so they hit even harder.",
    "SHRINK": "Deals less damage (a temporary Strength reduction).",
    "MANGLE": "Strength reduced by {amount} until the end of the turn.",
    # -- core buffs --
    "STRENGTH": "Increases attack damage by {amount} per hit.",
    "DEXTERITY": "Increases Block gained from cards by {amount}.",
    "FOCUS": "Increases the effectiveness of orbs by {amount}.",
    "VIGOR": "The next Attack deals {amount} additional damage, then Vigor is lost.",
    "ARTIFACT": "Negates the next {amount} debuff(s) applied.",
    "INTANGIBLE": "Reduces all damage and HP loss taken to 1. Drops by 1 each turn.",
    "BUFFER": "Prevents the next {amount} instance(s) of HP loss.",
    "BARRICADE": "Block is no longer removed at the start of your turn.",
    "METALLICIZE": "Gain {amount} Block at the end of each turn.",
    "PLATING": "Gain {amount} Block at the end of the turn, then it drops by 1.",
    "REGEN": "Heal {amount} HP at the end of the turn, then it drops by 1.",
    "RITUAL": "Gain {amount} Strength at the end of its turn.",
    "THORNS": "When hit by an attack, deal {amount} damage back to the attacker.",
    "RUPTURE": "Whenever you lose HP from a card, gain {amount} Strength.",
    "DEMON_FORM": "Gain {amount} Strength at the start of each turn.",
    "CALCIFY": "Your Osty's attacks deal {amount} extra damage.",
    "BLOCK_NEXT_TURN": "Gain {amount} Block at the start of your next turn.",
    "ENERGY_NEXT_TURN": "Gain {amount} extra energy at the start of your next turn.",
    "DRAW_CARDS_NEXT_TURN": "Draw {amount} extra card(s) at the start of your next turn.",
    "SUMMON_NEXT_TURN": "Summon {amount} Osty next turn.",
    "STAR_NEXT_TURN": "Gain {amount} Star(s) at the start of your next turn.",
    "COUNTDOWN": "At the start of your turn, apply {amount} Doom to a random enemy.",
    # -- Act 4 Heart / bosses --
    "BEAT_OF_DEATH": "Whenever a card is played, everyone takes {amount} damage.",
    "INVINCIBLE": "Can lose at most {amount} HP per turn.",
    "REGENERATE_A4H": "Heals {amount} HP at the end of its turn.",
    # -- legacy-act monster staples --
    "CURL_UP": "The first time it is hit by an attack, it gains {amount} Block.",
    "PLATED_ARMOR": "Gains {amount} Block each turn; drops by 1 when hit by an unblocked attack.",
    "MALLEABLE": "Gains {amount} Block each time it is hit by an unblocked attack.",
    "FLIGHT": "Halves attack damage taken. Drops by 1 when hit; grounded at 0.",
    "ANGRY": "Gains {amount} Strength whenever it takes unblocked damage.",
    "MODE_SHIFT": "Switches to defensive mode after taking {amount} damage.",
    "SPLIT": "Splits into two copies when reduced to half HP.",
    "TIME_WARP": "After you play 12 cards, its turn triggers and it gains Strength.",
    "SHIFTING": "Whenever it takes damage, its Strength drops for the rest of the turn.",
    "STASIS": "Holding one of your cards; returns it when this monster dies.",
    "THIEVERY": "Steals {amount} gold each time it attacks you.",
    "NEMESIS": "Every other turn it becomes Intangible (takes only 1 damage).",
    "INFERNO": "At the start of its turn, it burns itself for damage.",
    "STEAM_ERUPTION": "When it dies, it detonates in an explosion.",
    "LIFE_LINK": "Revives after death unless all linked Darklings die on the same turn.",
    "REACTIVE": "Whenever it takes unblocked attack damage, it changes its next move.",
    "SANDPIT": "Counts down each turn; when it hits 0 the enemy escapes.",
}

# Powers whose amount is a percentage/flag where "{amount}" reads oddly; keep the
# amount out of the sentence for these even when we have one.
_POWER_NO_AMOUNT = {"VULNERABLE", "WEAK", "FRAIL", "BARRICADE", "INTANGIBLE"}


@lru_cache(maxsize=1)
def _power_classes() -> dict:
    """Runtime ``PowerId -> class`` map (imports register the classes)."""
    import importlib
    import pkgutil

    import sts2_env.powers as powers_pkg

    for module in pkgutil.iter_modules(powers_pkg.__path__):
        try:
            importlib.import_module(f"sts2_env.powers.{module.name}")
        except Exception:
            continue
    from sts2_env.core.creature import _POWER_CLASSES  # noqa: SLF001

    return dict(_POWER_CLASSES)


def _docstring_sentence(power_id: PowerId) -> str | None:
    cls = _power_classes().get(power_id)
    doc = (getattr(cls, "__doc__", None) or "").strip()
    if not doc:
        return None
    # Collapse the whole docstring to a single spaced string, then take the
    # first sentence (docstrings here lead with the behavioural summary).
    flat = re.sub(r"\s+", " ", doc).strip()
    match = re.match(r"(.+?[.!?])(?:\s|$)", flat)
    sentence = match.group(1) if match else flat
    if len(sentence) > 200:
        sentence = sentence[:197].rstrip() + "..."
    return sentence


def _coerce_power_id(power_id: object) -> PowerId | None:
    if isinstance(power_id, PowerId):
        return power_id
    name = str(power_id)
    if "." in name:
        name = name.rsplit(".", 1)[-1]
    try:
        return PowerId[name]
    except KeyError:
        return None


def _apply_amount(text: str, amount: int | None, *, power_name: str = "") -> str:
    if amount is not None and power_name not in _POWER_NO_AMOUNT:
        text = text.replace("{amount}", str(amount))
        # Power docstrings use the literal word "Amount".
        text = re.sub(r"\bAmount\b", str(amount), text)
    else:
        text = text.replace("{amount}", "X")
        text = re.sub(r"\bAmount\b", "X", text)
    # Drop internal jargon that only makes sense to the engine.
    text = text.replace(" (unpowered)", "").replace(" (powered)", "")
    return re.sub(r"\s{2,}", " ", text).strip()


def power_description(power_id: object, amount: int | None = None) -> str:
    """Return a one-line description of a power/buff/debuff.

    ``power_id`` may be a :class:`PowerId` or its name. ``amount`` (the live
    stack count) is woven into the text where relevant. Never raises.
    """
    try:
        pid = _coerce_power_id(power_id)
        if pid is None:
            return f"{_humanize(power_id)}."
        curated = _POWER_DESCRIPTIONS.get(pid.name)
        if curated is not None:
            return _apply_amount(curated, amount, power_name=pid.name)
        doc = _docstring_sentence(pid)
        if doc:
            return _apply_amount(doc, amount, power_name=pid.name)
        return f"{_humanize(pid.name)}."
    except Exception:
        return f"{_humanize(power_id)}."


# ─────────────────────────────────────────────────────────────────────────
# Cards
# ─────────────────────────────────────────────────────────────────────────

_CARD_TYPE_LABEL = {
    CardType.ATTACK: "Attack",
    CardType.SKILL: "Skill",
    CardType.POWER: "Power",
    CardType.STATUS: "Status",
    CardType.CURSE: "Curse",
    CardType.QUEST: "Quest",
}

_KEYWORD_LABEL = {
    "exhaust": "Exhaust",
    "ethereal": "Ethereal",
    "innate": "Innate",
    "retain": "Retain",
    "unplayable": "Unplayable",
    "sly": "Sly",
    "eternal": "Eternal",
    "returns_to_hand_on_exhaust": "Returns to hand when Exhausted",
}

# Map a power word from the reference effect text to the effect_vars key that
# stores its amount.
_POWER_WORD_VAR = {
    "vulnerable": "vulnerable",
    "weak": "weak",
    "strength": "strength",
    "dexterity": "dexterity",
    "poison": "poison_power",
    "doom": "doom",
    "focus": "focus",
    "vigor": "vigor",
    "plating": "plating",
    "thorns": "thorns",
    "energynextturn": "energy",
    "summonnextturn": "summon",
    "drawcardsnextturn": "cards",
    "starnextturn": "stars",
    "blocknextturn": "block",
    "haunt": "hp_loss",
    "enfeeblingtouch": "strength_loss",
    "shroud": "block",
}

# Hand-written bodies for cards whose real effect is scaling / X-cost /
# conditional and therefore cannot be derived faithfully from card data alone.
# The header (type + cost) and keyword line are still added automatically.
_CARD_OVERRIDES: dict[str, str] = {
    "UNLEASH": "Osty attacks for {damage} damage plus 1 per point of Osty's current HP.",
    "FLATTEN": "Osty attacks for {osty_damage} damage. Costs 0 if Osty dealt damage this turn.",
    "DEATH_MARCH": "Deal {damage} damage, +3 for each card drawn outside your draw step this turn.",
    "SOUL_STORM": "Deal {damage} damage, +2 for each Soul in your exhaust pile.",
    "SQUEEZE": "Osty attacks for {damage} damage, +5 for each other Osty Attack card you own.",
    "PROTECTOR": "Osty attacks for {damage} damage plus 1 per point of Osty's max HP.",
    "TIMES_UP": "Deal 1 damage for each stack of Doom on the target.",
    "NO_ESCAPE": "Apply {calc_base} Doom, plus {calc_extra} more for every 10 Doom already on the target.",
    "PULL_FROM_BELOW": "Deal {damage} damage once for each Ethereal card played this combat.",
    "RATTLE": "Osty attacks for {osty_damage} damage once per hit Osty already dealt this turn.",
    "BLIGHT_STRIKE": "Deal {damage} damage. Apply Doom to the target equal to the unblocked damage dealt.",
    "SACRIFICE": "Sacrifice your Osty to gain Block equal to twice its max HP.",
    "BONE_SHARDS": "Osty hits all enemies for {osty_damage}, you gain {block} Block, then Osty dies.",
    "DIRGE": "X-cost. For each energy spent, summon an Osty ({summon} HP) and add a Soul to your draw pile.",
    "ERADICATE": "X-cost. Deal {damage} damage once for each energy spent.",
    "THE_SCYTHE": "Deal {damage} damage. Its damage permanently increases by {increase} each time it is played.",
    "REANIMATE": "Summon an Osty with {summon} HP.",
    "DEATHS_DOOR": "Gain {block} Block. Gain it two more times if you applied Doom this turn.",
    "EIDOLON": "Exhaust. Auto-play every Ethereal card in your exhaust pile.",
    "END_OF_DAYS": "Apply {doom} Doom to all enemies, then kill any whose HP is at or below their Doom.",
    "MISERY": "Deal {damage} damage, then copy every debuff on the target to all other enemies.",
    "UNDEATH": "Gain {block} Block and add a copy of this card to your discard pile.",
    "SEVERANCE": "Deal {damage} damage and create 3 Souls (one to draw, discard and hand).",
    "PUTREFY": "Apply {power} Weak and {power} Vulnerable to the target.",
    "SPUR": "Summon an Osty with {summon} HP and heal it for {heal}.",
    "FRIENDSHIP": "Lose {strength} Strength and gain Friendship (extra max energy each turn).",
    "SHARED_FATE": "You and the target each lose Strength this turn.",
    "CAPTURE_SPIRIT": "Deal {damage} damage (ignores block) and create 3 Souls in your draw pile.",
    "BORROWED_TIME": "Gain {energy} energy. Your cards cost {extra_cost} more this turn (Borrowed Time).",
    "REAPER_FORM": "Whenever you deal attack damage, apply that much Doom to the target.",
    "DEMESNE": "Each turn, draw 1 extra card and gain 1 extra energy.",
    "PAGESTORM": "Whenever you draw an Ethereal card, draw an additional card.",
    "FORBIDDEN_GRIMOIRE": "After each combat, remove a card from your deck.",
    "CALL_OF_THE_VOID": "At the start of each turn, add a random Ethereal card to your hand.",
    "SPIRIT_OF_ASH": "Before you play an Ethereal card, gain {block_on_exhaust} Block.",
    "NECRO_MASTERY_CARD": "Summon an Osty ({summon} HP). When your Osty is hit, deal that damage to all enemies.",
    "VEILPIERCER": "Deal {damage} damage. Your Ethereal cards cost 0 this turn (Veilpiercer).",
    "COUNTDOWN_CARD": "Gain Countdown: at the start of each turn, apply {countdown} Doom to a random enemy.",
    "SENTRY_MODE": "At the start of each turn, add a Sweeping Gaze card to your hand.",
    "DANSE_MACABRE": "Before you play a card costing 2 or more, gain {danse_macabre} Block.",
}

_SIMPLE_CLAUSES: dict[str, str] = {
    "Draw card(s)": "Draw {cards} card(s).",
    "Gain Energy": "Gain {energy} energy.",
    "Gain Stars": "Gain {stars} Star(s).",
    "Gain Gold": "Gain gold.",
    "Gain Max HP": "Gain Max HP.",
    "Lose Max HP": "Lose Max HP.",
    "Lose Block": "Lose your Block.",
    "Heal HP": "Heal HP.",
    "Summon minion": "Summon an Osty minion with {summon} HP.",
    "Create Soul(s)": "Add Soul card(s) to your piles.",
    "Create Shiv(s) in hand": "Add Shiv(s) to your hand.",
    "Add generated card(s) to hand": "Add card(s) to your hand.",
    "Add generated card(s) to discard": "Add card(s) to your discard pile.",
    "Add generated card(s) to draw pile": "Add card(s) to your draw pile.",
    "Add card to Hand pile": "Add a card to your hand.",
    "Add card to Draw pile": "Add a card to your draw pile.",
    "Add card to Discard pile": "Add a card to your discard pile.",
    "Exhaust card(s) from hand": "Exhaust card(s) from your hand.",
    "Discard card(s)": "Discard card(s).",
    "Upgrade card(s)": "Upgrade card(s) for this combat.",
    "Forge (upgrade random card in deck)": "Forge: upgrade a random card in your deck.",
    "Choose from card grid": "Choose a card.",
    "Select card(s) from hand": "Choose card(s) from your hand.",
    "Choose card(s)": "Choose a card.",
    "Transform card(s)": "Transform a card.",
    "Transform into Soul": "Transform into a Soul.",
    "Set card(s) to cost 0": "Set card(s) to cost 0 this turn.",
    "Kill creature": "Instantly kill the target if able.",
    "End turn": "End your turn.",
    "Shuffle draw pile": "Shuffle your draw pile.",
    "Grant Replay to card": "Grant Replay to a card.",
    "Auto-play card(s)": "Auto-play card(s).",
    "Auto-play card(s) from draw pile": "Auto-play card(s) from your draw pile.",
    "Stun enemy": "Stun the enemy.",
    "Generate potion": "Generate a potion.",
    "Trigger orb passive(s)": "Trigger your orbs' passive effects.",
    "Evoke front orb": "Evoke your front orb.",
    "Channel random orb": "Channel a random orb.",
    "Gain orb slot(s)": "Gain orb slot(s).",
    "Remove orb slot(s)": "Remove orb slot(s).",
    "Remove Artifact": "Remove an Artifact charge from the target.",
    "Discard and redraw": "Discard your hand and redraw.",
}


@lru_cache(maxsize=1)
def _reference_effects() -> dict[str, str]:
    """Parse ``docs/CARDS_REFERENCE.md`` into ``{CARD_ID_NAME: effect_text}``."""
    result: dict[str, str] = {}
    try:
        text = Path("docs/CARDS_REFERENCE.md").read_text(encoding="utf-8")
    except OSError:
        return result
    for entry in re.split(r"^### ", text, flags=re.MULTILINE)[1:]:
        card_id = None
        effect = None
        for line in entry.splitlines():
            m = re.match(r"- \*\*(.+?):\*\* (.+)", line)
            if not m:
                continue
            if m.group(1) == "ID":
                card_id = m.group(2).strip()
            elif m.group(1) == "Effect":
                effect = m.group(2).strip()
        if card_id:
            result[card_id] = effect or ""
    return result


def _reference_effect(card_id) -> str | None:
    effects = _reference_effects()
    name = getattr(card_id, "name", str(card_id))
    candidates = [name]
    if name.endswith("_CARD"):
        candidates.append(name[:-5])
    if name.endswith("_STATUS"):
        candidates.append(name[:-7])
    if name == "NULL_CARD":
        candidates.append("NULL")
    for candidate in candidates:
        if candidate in effects:
            return effects[candidate]
    return None


def _cost_label(card) -> str:
    if getattr(card, "has_energy_cost_x", False):
        cost = "X energy"
    elif getattr(card, "is_unplayable", False):
        return "Unplayable"
    else:
        cost = f"{card.cost} energy"
    star_cost = getattr(card, "star_cost", 0) or 0
    if getattr(card, "has_star_cost_x", False):
        cost += " · X★"
    elif star_cost:
        cost += f" · {star_cost}★"
    return cost


def _card_header(card) -> str:
    type_label = _CARD_TYPE_LABEL.get(card.card_type, _humanize(card.card_type))
    return f"{type_label} · {_cost_label(card)}"


def _keyword_line(card) -> str:
    labels = [
        _KEYWORD_LABEL[keyword]
        for keyword in _KEYWORD_LABEL
        if keyword in getattr(card, "keywords", frozenset())
    ]
    return " ".join(labels)


def _main_damage(card) -> int | None:
    if card.base_damage is not None:
        return card.base_damage
    for key in ("osty_damage", "calc_base", "damage"):
        if key in card.effect_vars:
            return card.effect_vars[key]
    return None


def _main_block(card) -> int | None:
    if card.base_block is not None:
        return card.base_block
    return card.effect_vars.get("block")


def _fill(template: str, card) -> str:
    """Substitute ``{var}`` placeholders from ``effect_vars``; drop unknowns."""

    def repl(match: re.Match) -> str:
        key = match.group(1)
        value = card.effect_vars.get(key)
        if value is None and key == "damage":
            value = card.base_damage
        if value is None and key == "block":
            value = card.base_block
        return str(value) if value is not None else ""

    filled = re.sub(r"\{(\w+)\}", repl, template)
    # Tidy up "Draw  card(s)." when the number was missing.
    filled = re.sub(r"\s{2,}", " ", filled).replace(" .", ".")
    return filled.strip()


def _damage_clause(clause: str, dmg: int | None) -> str:
    low = clause.lower()
    scope = " to all enemies" if "all enemies" in low else ""
    times = ""
    if "x times" in low:
        times = " X times (once per energy spent)"
    elif re.search(r"\b(\d+) times", low):
        times = f" {re.search(r'(\d+) times', low).group(1)} times"
    elif "multiple times" in low:
        times = " several times"
    if "non-attack" in low:
        head = f"Deal {dmg} damage" if dmg is not None else "Deal damage"
        return f"{head}{scope} (ignores Strength/Weak)."
    head = f"Deal {dmg} damage" if dmg is not None else "Deal damage"
    return f"{head}{scope}{times}."


def _apply_clause(clause: str, card) -> str:
    m = re.match(r"Apply (.+?) to (.+)", clause)
    if not m:
        return _humanize_clause(clause)
    power_word, target = m.group(1).strip(), m.group(2).strip().lower()
    amount = None
    var_key = _POWER_WORD_VAR.get(power_word.lower().replace(" ", ""))
    if var_key is not None:
        amount = card.effect_vars.get(var_key)
    if amount is None:
        snake = _camel_to_snake(power_word.replace(" ", ""))
        for cand in (snake, f"{snake}_power", snake.replace("_power", "")):
            if cand in card.effect_vars:
                amount = card.effect_vars[cand]
                break
    power_label = _humanize(power_word)
    number = f"{amount} " if amount is not None else ""
    if "self" in target:
        # For Power cards the applied self-power *is* the card's whole effect,
        # so surface the mechanic rather than a bare "Gain X".
        if card.card_type == CardType.POWER:
            pid = _coerce_power_id(_camel_to_snake(power_word.replace(" ", "")).upper())
            if pid is not None:
                mechanic = power_description(pid, amount)
                if mechanic and mechanic.rstrip(".") != power_label:
                    return f"Gain {power_label}: {mechanic}"
        return f"Gain {number}{power_label}.".replace("  ", " ")
    where = " to all enemies" if "all" in target else ""
    return f"Apply {number}{power_label}{where}.".replace("  ", " ")


def _humanize_clause(clause: str) -> str:
    clause = clause.strip()
    if not clause:
        return ""
    if clause.lower().startswith("channel ") and clause.lower().endswith(" orb"):
        return f"Channel a {clause[len('Channel '):-len(' orb')]} orb."
    text = clause[0].upper() + clause[1:]
    if not text.endswith((".", "!", "?")):
        text += "."
    return text


_SKIP_CLAUSES = {
    "Unplayable",
    "No OnPlay effect",
    "Preview card(s)",
    "may have rider effect",
    "Uses X (remaining energy)",
}


def _render_clause(clause: str, card, dmg: int | None, blk: int | None) -> str | None:
    clause = clause.strip()
    if not clause or clause in _SKIP_CLAUSES:
        return None
    low = clause.lower()
    if low.startswith("deal damage") or low.startswith("deal non-attack damage"):
        return _damage_clause(clause, dmg)
    if clause == "Gain Block":
        return f"Gain {blk} Block." if blk is not None else "Gain Block."
    if clause in _SIMPLE_CLAUSES:
        return _fill(_SIMPLE_CLAUSES[clause], card)
    if low.startswith("apply "):
        return _apply_clause(clause, card)
    return _humanize_clause(clause)


def _derive_body(card) -> str:
    dmg = _main_damage(card)
    blk = _main_block(card)
    effect = _reference_effect(card.card_id)
    lines: list[str] = []
    if effect:
        for clause in effect.split(";"):
            rendered = _render_clause(clause, card, dmg, blk)
            if rendered and rendered not in lines:
                lines.append(rendered)
    else:
        # No reference row: fall back to raw card data.
        if dmg:
            scope = " to all enemies" if card.target_type == TargetType.ALL_ENEMIES else ""
            lines.append(f"Deal {dmg} damage{scope}.")
        if blk:
            lines.append(f"Gain {blk} Block.")
    return "\n".join(lines)


def card_damage_clause(card) -> dict | None:
    """Static metadata about a card's main damage clause, shared by the
    description text and the live preview so both stay in sync.

    Returns ``{"hits": int | None, "all_enemies": bool, "non_attack": bool}``:

    * ``hits`` is the fixed hit count from the reference effect text
      (``"... 2 times"``), ``1`` for a plain damage clause, or ``None`` when
      the count is variable (``"X times"`` / ``"multiple times"``).
    * ``all_enemies`` mirrors the reference "to ALL enemies" scope.
    * ``non_attack`` marks clauses that bypass Strength/Weak (unpowered).

    Returns ``None`` when the card has no damage clause at all. Never raises.
    """
    try:
        effect = _reference_effect(card.card_id)
        if effect:
            for clause in effect.split(";"):
                low = clause.strip().lower()
                if not (low.startswith("deal damage") or low.startswith("deal non-attack damage")):
                    continue
                if "x times" in low or "multiple times" in low:
                    hits: int | None = None
                else:
                    match = re.search(r"\b(\d+) times", low)
                    hits = int(match.group(1)) if match else 1
                return {
                    "hits": hits,
                    "all_enemies": "all enemies" in low,
                    "non_attack": "non-attack" in low,
                }
        if card.base_damage is not None:
            return {
                "hits": 1,
                "all_enemies": card.target_type == TargetType.ALL_ENEMIES,
                "non_attack": False,
            }
        return None
    except Exception:
        return None


def _card_description(card) -> str:
    header = _card_header(card)
    name = getattr(card.card_id, "name", str(card.card_id))
    override = _CARD_OVERRIDES.get(name)
    if override is not None:
        body = _fill(override, card)
    else:
        body = _derive_body(card)
    keyword_line = _keyword_line(card)
    parts = [header]
    if body:
        parts.append(body)
    if keyword_line:
        parts.append(keyword_line)
    return "\n".join(parts)


def card_description(card) -> str:
    """Return a concise, multi-line effect description for a card instance.

    Falls back to the type/cost header plus any keyword line if the effect
    cannot be derived. Never raises.
    """
    try:
        return _card_description(card)
    except Exception:
        try:
            header = _card_header(card)
            keyword_line = _keyword_line(card)
            return f"{header}\n{keyword_line}" if keyword_line else header
        except Exception:
            return ""


# ─────────────────────────────────────────────────────────────────────────
# Potions (best effort)
# ─────────────────────────────────────────────────────────────────────────

_POTION_DESCRIPTIONS: dict[str, str] = {
    "FirePotion": "Deal 20 damage to target enemy.",
    "BlockPotion": "Gain 12 Block.",
    "StrengthPotion": "Gain 2 Strength.",
    "DexterityPotion": "Gain 2 Dexterity.",
    "FocusPotion": "Gain 2 Focus.",
    "EnergyPotion": "Gain 2 energy.",
    "SwiftPotion": "Draw 3 cards.",
    "BloodPotion": "Heal 20% of your max HP.",
    "FruitJuice": "Permanently gain 5 max HP.",
    "AttackPotion": "Add 1 of 3 random Attack cards to your hand (costs 0 this turn).",
    "SkillPotion": "Add 1 of 3 random Skill cards to your hand (costs 0 this turn).",
    "PowerPotion": "Add 1 of 3 random Power cards to your hand (costs 0 this turn).",
    "ColorlessPotion": "Add 1 of 3 random Colorless cards to your hand (costs 0 this turn).",
    "WeakPotion": "Apply 3 Weak to target enemy.",
    "VulnerablePotion": "Apply 3 Vulnerable to target enemy.",
    "PoisonPotion": "Apply 6 Poison to target enemy.",
    "FearPotion": "Apply 3 Vulnerable to target enemy.",
    "FairyInABottle": "When you would die, heal to 30% max HP instead (used automatically).",
    "RegenPotion": "Gain 5 Regen (heal each turn, then it drops).",
    "FlexPotion": "Gain 5 Strength this turn (removed at end of turn).",
    "SpeedPotion": "Gain 5 Dexterity this turn (removed at end of turn).",
    "CunningPotion": "Add 3 Shivs to your hand.",
    "PotionOfCapacity": "Gain 2 orb slots.",
    "EssenceOfDarkness": "Channel 1 Dark orb per orb slot.",
    "DistilledChaos": "Auto-play the top 3 cards of your draw pile.",
    "LiquidBronze": "Gain 3 Thorns.",
    "LiquidMemories": "Return 1 card from your discard pile to your hand (costs 0).",
    "HeartOfIron": "Gain 6 Metallicize.",
    "GamblersBrew": "Discard any number of cards, then draw that many.",
    "EntropicBrew": "Fill all your empty potion slots with random potions.",
    "Duplicator": "The next card you play this turn is played twice.",
    "SneckoOil": "Draw 5 cards and randomize the cost of cards in your hand.",
    "StarPotion": "Gain 3 Stars.",
    "PotionOfDoom": "Apply Doom to all enemies.",
    "ShacklingPotion": "Reduce target enemy's Strength by 6 this turn.",
    "CureAll": "Remove all debuffs from yourself.",
}


def potion_description(potion) -> str:
    """Return a short description of a potion (accepts an instance or id)."""
    try:
        potion_id = getattr(potion, "potion_id", potion)
        curated = _POTION_DESCRIPTIONS.get(str(potion_id))
        if curated is not None:
            return curated
        return f"{_humanize(potion_id)}."
    except Exception:
        return ""


# ─────────────────────────────────────────────────────────────────────────
# Relics (best effort, curated commons + humanized fallback)
# ─────────────────────────────────────────────────────────────────────────

_RELIC_DESCRIPTIONS: dict[str, str] = {
    "BURNING_BLOOD": "At the end of combat, heal 6 HP.",
    "RING_OF_THE_SNAKE": "At the start of each combat, draw 2 extra cards.",
    "CRACKED_CORE": "At the start of each combat, channel 1 Lightning orb.",
    "PURE_WATER": "At the start of each combat, add a Miracle to your hand.",
    "AKABEKO": "Your first Attack each combat deals 8 additional damage.",
    "ANCHOR": "Start each combat with 10 Block.",
    "BAG_OF_MARBLES": "At the start of each combat, apply 1 Vulnerable to all enemies.",
    "BAG_OF_PREPARATION": "At the start of each combat, draw 2 extra cards.",
    "BLOOD_VIAL": "At the start of each combat, heal 2 HP.",
    "BRONZE_SCALES": "Start each combat with 3 Thorns.",
    "ORICHALCUM": "If you end your turn without Block, gain 6 Block.",
    "VAJRA": "Start each combat with 1 Strength.",
    "ODDLY_SMOOTH_STONE": "Start each combat with 1 Dexterity.",
    "LANTERN": "Gain 1 energy on the first turn of each combat.",
    "SOZU": "Gain 1 energy at the start of each turn, but you can no longer obtain potions.",
    "ART_OF_WAR": "If you play no Attacks during your turn, gain 1 energy next turn.",
    "CENTENNIAL_PUZZLE": "The first time you lose HP each combat, draw 3 cards.",
    "PRESERVED_INSECT": "Elites start each combat with 25% less HP.",
}


def relic_description(relic_id) -> str:
    """Return a short description of a relic (best effort)."""
    try:
        name = getattr(relic_id, "name", str(relic_id))
        curated = _RELIC_DESCRIPTIONS.get(name)
        if curated is not None:
            return curated
        return f"{_humanize(name)}."
    except Exception:
        return ""
