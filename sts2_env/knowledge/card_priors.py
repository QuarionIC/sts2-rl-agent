"""Community card-strength priors for Necrobinder, grounded to real CardIds.

Voyager (Wang et al. 2023) bootstraps a Minecraft agent from an LLM's prior
knowledge of the game, then keeps only the skills that survive execution in
the world. The analogue here: community tier lists and guides encode a great
deal of human knowledge about which cards win runs, and that knowledge maps
directly onto the run agent's decision space (card rewards, shop buys,
upgrades) now that combat is handled by the deterministic planner.

What is stored here is FACTUAL tier assignment plus provenance -- the rank a
public source assigned to a card -- not the sources' prose. Rationale strings
are our own one-line summaries written for the code, and every entry carries
the URL it came from so a claim can be re-checked.

Crucially these are PRIORS, not ground truth. Sources disagree (see
``KNOWN_CONFLICTS``), tier lists are written for the live retail game rather
than this simulator's exact patch + mod set, and a card's value is deeply
contextual. Nothing here is trusted until ``scripts/verify_skills.py``
measures it against the in-sim baseline; a prior that fails verification is
recorded as failed rather than quietly kept.
"""

from __future__ import annotations

from dataclasses import dataclass

#: Where the tier assignments came from.
SOURCES = {
    "nat1": {
        "url": "https://nat1gaming.com/sts2/tier-list/necrobinder-card-tier-list/",
        "retrieved": "2026-07-27",
        "note": "Card tier list, dated 2026-07-03, post 7/2 beta patch.",
    },
    "wiki": {
        "url": "https://slaythespire.wiki.gg/wiki/Slay_the_Spire_2:Necrobinder",
        "retrieved": "2026-07-27",
        "note": "Mechanics: Osty/Bound Phylactery, 66 starting HP, card pool structure.",
    },
    "pcgamesn": {
        "url": "https://www.pcgamesn.com/slay-the-spire-2/necrobinder",
        "retrieved": "2026-07-27",
        "note": "Archetype overview (Souls / Osty / Doom); early-attack advice.",
    },
}

#: Numeric weight per tier, used to rank card-reward options. The scale is
#: deliberately steep at the top: in Slay the Spire the difference between an
#: S card and a B card is far larger than between C and D, because the
#: bottom half is mostly deck filler either way.
TIER_VALUE = {"S": 10.0, "A": 7.0, "B": 4.0, "C": 2.0, "D": 0.5, "F": 0.0}

#: card_id (CardId enum NAME) -> tier, per SOURCES["nat1"].
#: Starter cards (STRIKE_NECROBINDER, DEFEND_NECROBINDER) are not ranked.
CARD_TIERS: dict[str, str] = {
    # -- S --
    "NEUROSURGE": "S",
    "FRIENDSHIP": "S",
    # -- A --
    "CALL_OF_THE_VOID": "A",
    "GLIMPSE_BEYOND": "A",
    "HANG": "A",
    "ERADICATE": "A",
    "BORROWED_TIME": "A",
    "NO_ESCAPE": "A",
    "TRANSFIGURE": "A",
    "BANSHEES_CRY": "A",
    "END_OF_DAYS": "A",
    "DIRGE": "A",
    "SPIRIT_OF_ASH": "A",
    "UNDEATH": "A",
    "REANIMATE": "A",
    "DEATHBRINGER": "A",
    "SQUEEZE": "A",
    "THE_SCYTHE": "A",
    "FETCH": "A",
    "SLEIGHT_OF_FLESH": "A",
    "DEFY": "A",
    "COUNTDOWN_CARD": "A",
    # -- B --
    "BONE_SHARDS": "B",
    "DEMESNE": "B",
    "ENFEEBLING_TOUCH": "B",
    "SEANCE": "B",
    "PROTECTOR": "B",
    "HIGH_FIVE": "B",
    "LETHALITY_CARD": "B",
    "CAPTURE_SPIRIT": "B",
    "OBLIVION": "B",
    "GRAVE_WARDEN": "B",
    "NECRO_MASTERY_CARD": "B",
    "CALCIFY_CARD": "B",
    "MISERY": "B",
    "FLATTEN": "B",
    "DEATHS_DOOR": "B",
    "DEBILITATE_CARD": "B",
    "NEGATIVE_PULSE": "B",
    "PAGESTORM": "B",
    "TIMES_UP": "B",
    "SCOURGE": "B",
    "RATTLE": "B",
    "VEILPIERCER": "B",
    "MELANCHOLY": "B",
    "SOUL_STORM": "B",
    "REAPER_FORM": "B",
    "FORBIDDEN_GRIMOIRE": "B",
    "SHARED_FATE": "B",
    "DREDGE": "B",
    "SHROUD": "B",
    "SNAP": "B",
    "UNLEASH": "B",
    "INVOKE": "B",
    # -- C --
    "REAP": "C",
    "HAUNT": "C",
    "SENTRY_MODE": "C",
    "SPUR": "C",
    "SOW": "C",
    "SCULPTING_STRIKE": "C",
    "DEATH_MARCH": "C",
    "SEVERANCE": "C",
    "DEVOUR_LIFE_CARD": "C",
    "DEFILE": "C",
    "PULL_AGGRO": "C",
    "PUTREFY": "C",
    "LEGION_OF_BONE": "C",
    "RIGHT_HAND_HAND": "C",
    "GRAVEBLAST": "C",
    "BLIGHT_STRIKE": "C",
    "FEAR": "C",
    "REAVE": "C",
    "DRAIN_POWER": "C",
    "BODYGUARD": "C",
    "POKE": "C",
    "DELAY": "C",
    "SACRIFICE": "C",
    "PULL_FROM_BELOW": "C",
    "AFTERLIFE": "C",
    "CLEANSE": "C",
    # -- D --
    "PARSE": "D",
    "DANSE_MACABRE": "D",
    "WISP": "D",
    "SIC_EM": "D",
    "BURY": "D",
    # -- F --
    "EIDOLON": "F",
}

#: Where public sources contradict each other. Recorded rather than silently
#: resolved -- these are the entries most worth settling empirically, since a
#: disagreement means the community itself is unsure.
KNOWN_CONFLICTS = {
    "SEVERANCE": (
        "nat1 ranks C (two energy is expensive); pcgamesn calls it a class "
        "cornerstone. Large disagreement -- verify before trusting either."
    ),
    "DEVOUR_LIFE_CARD": (
        "nat1 ranks C (minimal impact without many Souls); pcgamesn lists it "
        "among the cornerstones."
    ),
    "DIRGE": (
        "Both sources rate it highly (nat1 A, pcgamesn cornerstone) -- listed "
        "here only because it is the one cornerstone claim that agrees."
    ),
}

#: Archetypes and their enabling cards, per SOURCES["pcgamesn"] and the
#: mechanics described on the wiki. Used by the synergy skill: a card that
#: matches the deck's emerging archetype is worth more than its raw tier.
ARCHETYPES: dict[str, set[str]] = {
    "souls": {
        "GLIMPSE_BEYOND", "DIRGE", "GRAVE_WARDEN", "CAPTURE_SPIRIT",
        "SEVERANCE", "SOUL_STORM", "HAUNT", "DEATH_MARCH", "UNDEATH",
        "DEVOUR_LIFE_CARD", "OBLIVION",
    },
    "osty": {
        "SQUEEZE", "PROTECTOR", "NECRO_MASTERY_CARD", "SPUR", "SENTRY_MODE",
        "INVOKE", "RIGHT_HAND_HAND", "FLATTEN", "RATTLE", "FETCH", "SNAP",
        "PULL_AGGRO",
    },
    "doom": {
        "NO_ESCAPE", "END_OF_DAYS", "DEATHBRINGER", "COUNTDOWN_CARD",
        "OBLIVION", "NEGATIVE_PULSE", "TIMES_UP", "SHROUD", "DEATHS_DOOR",
    },
    "ethereal": {
        "CALL_OF_THE_VOID", "BANSHEES_CRY", "SPIRIT_OF_ASH", "PAGESTORM",
        "VEILPIERCER", "SCULPTING_STRIKE",
    },
}


@dataclass(frozen=True)
class CardPrior:
    card_id: str
    tier: str
    value: float
    archetypes: tuple[str, ...]
    contested: bool


def card_prior(card_id: str) -> CardPrior:
    """Prior for a CardId name. Unknown cards get a neutral C-ish value so an
    unranked or modded card is never treated as worthless."""
    name = str(card_id).replace("CardId.", "")
    tier = CARD_TIERS.get(name, "C")
    arch = tuple(a for a, cards in ARCHETYPES.items() if name in cards)
    return CardPrior(
        card_id=name,
        tier=tier,
        value=TIER_VALUE.get(tier, 2.0),
        archetypes=arch,
        contested=name in KNOWN_CONFLICTS,
    )


def coverage_report(pool: list[str]) -> dict:
    """How much of an actual card pool these priors cover -- guards against
    the tier list silently drifting from the simulator's card set."""
    names = [str(c).replace("CardId.", "") for c in pool]
    ranked = [n for n in names if n in CARD_TIERS]
    missing = sorted(set(names) - set(CARD_TIERS) - {
        "STRIKE_NECROBINDER", "DEFEND_NECROBINDER"})
    stale = sorted(set(CARD_TIERS) - set(names))
    return {
        "pool_size": len(names),
        "ranked": len(ranked),
        "missing_from_priors": missing,
        "priors_not_in_pool": stale,
    }
