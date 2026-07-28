"""Voyager-style skill library for out-of-combat decisions.

Each skill is a small executable decision function over one class of run-level
choice (card reward, rest site, shop, map route). Skills are composed into a
:class:`KnowledgeRunPolicy` that can drive ``HierarchicalRunEnv`` directly, so
every skill is measurable in the same units as everything else in this repo:
mean floors under the standard eval protocol.

The Voyager loop this supports:

1. PROPOSE  -- author a skill from public knowledge (``card_priors``).
2. VERIFY   -- run it in the simulator against the measured baseline
               (``scripts/verify_skills.py``).
3. KEEP or REJECT -- retain only skills whose measured effect is real, and
               record the rejections so a failed idea is not silently retried.
4. CURRICULUM -- re-verify retained skills at higher ascension / more acts,
               since a skill that helps at A0 may not hold at A10.

Skills deliberately return an *option index* rather than an env action index:
the mapping from options to the unified action space differs per phase, and
keeping skills in option-space makes them independently testable and readable.
"""

from __future__ import annotations

from collections import Counter
from dataclasses import dataclass, field
from typing import Any, Callable

from sts2_env.knowledge.card_priors import TIER_VALUE, card_prior

#: Deck sizes that community play treats as the useful band for act 1-2.
#: Below the floor a deck lacks payoff cards; above the ceiling, draw gets
#: diluted and the good cards appear less often. Used to make card-taking
#: conditional rather than unconditional -- the flat agent's failure was
#: taking nothing, but the first hierarchical agent's failure was taking
#: everything (13.1 cards, 0.57 upgrades, measurably worse than synthetic
#: decks of the same size).
DECK_TARGET_MIN = 12
DECK_TARGET_SOFT_MAX = 18


@dataclass
class Skill:
    """One verified-or-not decision rule."""

    name: str
    description: str
    #: ``fn(context) -> int | None``: chosen option index, or None to abstain.
    fn: Callable[[dict], int | None]
    source: str = ""
    #: Filled in by the verification harness.
    verified: bool | None = None
    measured_effect: float | None = None
    notes: str = ""


# ---------------------------------------------------------------------------
# Card reward
# ---------------------------------------------------------------------------

def score_card(card_id: str, deck_counter: Counter, deck_size: int,
               floor: int) -> float:
    """Value of adding one card, blending prior tier with deck context."""
    prior = card_prior(card_id)
    score = prior.value

    # Archetype synergy: a card matching what the deck is already doing is
    # worth more than its raw tier. Weight scales with how committed the
    # deck already is, so this only fires once an archetype is real.
    from sts2_env.knowledge.card_priors import ARCHETYPES

    for arch in prior.archetypes:
        owned = sum(deck_counter[c] for c in ARCHETYPES[arch])
        if owned >= 2:
            score += min(3.0, 0.6 * owned)

    # Dilution penalty, only for genuinely oversized decks. An earlier,
    # steeper version fired at 18 cards and measurably HURT (see
    # skill_pick_card); at current run lengths a card in hand beats a
    # theoretical draw-rate argument.
    if deck_size >= DECK_TARGET_SOFT_MAX + 6:
        score -= 1.0 * (deck_size - (DECK_TARGET_SOFT_MAX + 6) + 1)

    # Diminishing returns on duplicates of situational cards (powers and
    # one-per-deck effects); attacks and block stack fine.
    dupes = deck_counter.get(prior.card_id, 0)
    if dupes:
        score -= 1.2 * dupes

    return score


def skill_pick_card(ctx: dict) -> int | None:
    """Take the best-scoring offered card; skip only in the greedy variant.

    VERIFIED FALSE (2026-07-27), then corrected. The original version applied
    a dilution penalty and skipped weak cards, on the theory that the
    hierarchical agent's diluted 13.1-card decks were the problem. Measured
    over 30 seed-matched runs it made decks SMALLER (12.6 vs 14.1) and runs
    SHORTER (7.63 vs 8.87 floors), and leave-one-out showed removing the
    skill entirely gained +1.43 +/- 0.74 floors -- it was the worst component
    in the library.

    The theory was wrong about the regime. Runs currently end around floor 8-9
    after only ~5-8 card rewards, so a skipped reward is never recovered;
    dilution only becomes the binding cost in long runs that reach act 2+.
    ``allow_skip`` therefore defaults False, and the dilution penalty is
    gated on genuinely oversized decks.
    """
    options = ctx.get("card_options") or []
    if not options:
        return None
    deck_counter = ctx["deck_counter"]
    deck_size = ctx["deck_size"]
    floor = ctx.get("floor", 0)

    scored = [
        (score_card(cid, deck_counter, deck_size, floor), i)
        for i, cid in enumerate(options)
    ]
    best_score, best_i = max(scored)

    if not ctx.get("allow_skip", False):
        return best_i
    threshold = 1.0 if deck_size < DECK_TARGET_MIN else TIER_VALUE["C"]
    if best_score < threshold and ctx.get("can_skip", True):
        return None
    return best_i


# ---------------------------------------------------------------------------
# Rest site
# ---------------------------------------------------------------------------

def skill_rest_or_smith(ctx: dict) -> str:
    """Choose REST vs SMITH by HP fraction.

    Grounded in a measured failure: the flat agent took REST 100% of the
    time, including 55% of rests at >=75% HP where a 30%-of-max heal is
    largely wasted, and finished runs with ~0 upgrades. Smithing at high HP
    converts a wasted heal into permanent deck strength.
    """
    hp_frac = ctx["hp_frac"]
    has_upgradable = ctx.get("has_upgradable", True)
    if not has_upgradable:
        return "HEAL"
    # Below this, healing is worth more than an upgrade: dying ends the run.
    return "HEAL" if hp_frac < 0.55 else "SMITH"


def skill_smith_target(ctx: dict) -> int | None:
    """Which card to upgrade: the highest-prior unupgraded card.

    Upgrading the best card compounds; upgrading a Strike does little.
    """
    upgradable = ctx.get("upgradable") or []  # list of (index, card_id)
    if not upgradable:
        return None
    best = max(upgradable, key=lambda pair: card_prior(pair[1]).value)
    return best[0]


# ---------------------------------------------------------------------------
# Shop
# ---------------------------------------------------------------------------

def skill_shop(ctx: dict) -> tuple[str, int] | None:
    """Spend gold: card removal first, then high-tier cards.

    Removal is prioritised because thinning a diluted deck raises the draw
    rate of every good card -- the same dilution problem, attacked from the
    other side.
    """
    gold = ctx.get("gold", 0)
    deck_size = ctx.get("deck_size", 0)
    removal_cost = ctx.get("removal_cost")
    if (removal_cost is not None and gold >= removal_cost
            and deck_size > DECK_TARGET_MIN):
        return ("remove", 0)

    cards = ctx.get("shop_cards") or []  # list of (index, card_id, price)
    affordable = [(i, cid, p) for i, cid, p in cards if p <= gold]
    if not affordable:
        return None
    best = max(affordable, key=lambda t: card_prior(t[1]).value)
    if card_prior(best[1]).value >= TIER_VALUE["B"]:
        return ("buy_card", best[0])
    return None


# ---------------------------------------------------------------------------
# Routing
# ---------------------------------------------------------------------------

def skill_route(ctx: dict) -> int | None:
    """Pick the next map node by expected value at the current HP.

    Elites are the highest-value nodes in the game (relics) and, with the
    deterministic planner handling combat, they are far more survivable than
    they were for a learned combat policy -- but the A10 measurement showed
    elite fights still cost ~2x the HP of normal fights, so the HP gate
    stays.
    """
    options = ctx.get("route_options") or []  # list of (index, room_type)
    if not options:
        return None
    hp_frac = ctx["hp_frac"]

    def value(room: str) -> float:
        if room == "ELITE":
            # Worth it only while healthy; catastrophic at low HP.
            return 6.0 if hp_frac > 0.7 else (2.0 if hp_frac > 0.5 else -5.0)
        if room == "REST_SITE":
            # Value is exactly what it repairs (or the upgrade if healthy).
            return 5.0 if hp_frac < 0.6 else 2.0
        if room == "SHOP":
            return 3.0 if ctx.get("gold", 0) >= 100 else 1.0
        if room == "TREASURE":
            return 3.5
        if room == "MONSTER":
            return 2.5 if hp_frac > 0.45 else 0.5
        if room == "EVENT":
            return 2.0
        return 1.0

    scored = [(value(room), i) for i, room in options]
    return max(scored)[1]


# ---------------------------------------------------------------------------
# Library
# ---------------------------------------------------------------------------

def default_skills() -> list[Skill]:
    src = "nat1 tier list + pcgamesn archetypes (see card_priors.SOURCES)"
    return [
        Skill("pick_card", "Take best-prior card; skip weak picks when the "
                           "deck is already large", skill_pick_card, src),
        Skill("rest_or_smith", "Smith above 55% HP, else heal",
              skill_rest_or_smith, "measured rest-site failure + priors"),
        Skill("smith_target", "Upgrade the highest-prior unupgraded card",
              skill_smith_target, src),
        Skill("shop", "Removal first, then B-tier-or-better cards",
              skill_shop, src),
        Skill("route", "HP-gated node preference; elites only while healthy",
              skill_route, "priors + measured A10 elite HP cost"),
    ]
