"""Render run state as text for an LLM, and parse its reply back to an action.

Design constraints that shaped this:

* The model chooses among an EXPLICIT numbered option list built from
  ``RunManager.get_available_actions()``. It never emits coordinates, card
  indices, or action-space integers directly. Every reply is therefore either
  a legal option or a parse failure -- there is no way for the model to
  produce a plausible-looking illegal move, which is the usual failure mode
  when an LLM drives a game.
* Card names carry their community tier from ``sts2_env.knowledge``. The model
  may know Slay the Spire 2 from pretraining, but this simulator runs a
  specific beta patch plus two mods, and the tier data is grounded to the
  actual card pool (verified 86/86 coverage). Supplying it costs a few tokens
  and removes a whole class of hallucinated card knowledge.
* The prompt is compact by construction. Local inference on this hardware runs
  at a few tokens/sec, so prompt length is a direct wall-clock cost; anything
  the model cannot act on this turn is omitted.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

from sts2_env.knowledge.card_priors import card_prior
from sts2_env.run.run_manager import RunManager

#: Phases the LLM is asked to decide. Combat is handled by the deterministic
#: planner and never reaches the model.
LLM_PHASES = {
    RunManager.PHASE_MAP_CHOICE,
    RunManager.PHASE_CARD_REWARD,
    RunManager.PHASE_REST_SITE,
    RunManager.PHASE_SHOP,
    RunManager.PHASE_EVENT,
    RunManager.PHASE_TREASURE,
    RunManager.PHASE_BOSS_RELIC,
}

SYSTEM_PROMPT = """You are playing Slay the Spire 2 as the Necrobinder.

Combat is played for you by a solver, so you only make out-of-combat
decisions: where to move on the map, which card rewards to take, what to do
at rest sites, shops, and events.

Your goal is to survive as deep into the run as possible. Key principles:
- HP is a resource, but running out ends the run. Elites are dangerous.
- Deck quality matters more than deck size: a few strong cards beat many
  weak ones. Upgrades are permanent value.
- Card tiers are given as (S/A/B/C/D/F); S is best, F is worst.

Reply with ONLY the number of your chosen option, then a brief reason.
Format exactly:
CHOICE: <number>
WHY: <one short sentence>"""


@dataclass
class Decision:
    """One rendered decision: the prompt shown, and the options behind it."""

    prompt: str
    options: list[dict]
    phase: str


def _card_label(card_id: str, upgraded: bool = False) -> str:
    name = str(card_id).replace("CardId.", "")
    prior = card_prior(name)
    pretty = name.replace("_NECROBINDER", "").replace("_CARD", "").title().replace("_", " ")
    return f"{pretty}{'+' if upgraded else ''} ({prior.tier})"


def _deck_summary(deck) -> str:
    """Compact deck line: counts by card, tier-annotated, upgrades marked."""
    from collections import Counter

    counts = Counter()
    for c in deck:
        key = (str(c.card_id).replace("CardId.", ""), bool(c.upgraded))
        counts[key] += 1
    parts = []
    for (name, up), n in sorted(counts.items(), key=lambda kv: -kv[1]):
        parts.append(f"{n}x {_card_label(name, up)}")
    return ", ".join(parts)


def render_decision(mgr: RunManager) -> Decision | None:
    """Build the prompt for the current non-combat decision, or None."""
    phase = mgr.phase
    actions = [a for a in mgr.get_available_actions() if a.get("enabled", True)]
    if phase not in LLM_PHASES or not actions:
        return None

    rs = mgr.run_state
    p = rs.player
    lines = [
        f"Act {rs.current_act_index + 1}, floor {rs.total_floor}. "
        f"HP {p.current_hp}/{p.max_hp}. Gold {p.gold}.",
        f"Relics: {', '.join(str(r) for r in rs.relics) or 'none'}",
        f"Deck ({len(p.deck)} cards): {_deck_summary(p.deck)}",
    ]
    potions = [str(x) for x in (p.potions or []) if x is not None]
    if potions:
        lines.append(f"Potions: {', '.join(potions)}")

    lines.append("")
    lines.append(_phase_question(phase))
    lines.append("")

    for i, a in enumerate(actions):
        lines.append(f"{i}. {_describe_option(a)}")

    return Decision("\n".join(lines), actions, phase)


def _phase_question(phase: str) -> str:
    return {
        RunManager.PHASE_MAP_CHOICE: "Which room do you move to next?",
        RunManager.PHASE_CARD_REWARD: "Which card reward do you take?",
        RunManager.PHASE_REST_SITE: "What do you do at the rest site?",
        RunManager.PHASE_SHOP: "What do you buy (or leave)?",
        RunManager.PHASE_EVENT: "Which event option do you choose?",
        RunManager.PHASE_TREASURE: "Do you open the treasure?",
        RunManager.PHASE_BOSS_RELIC: "Which boss relic do you take?",
    }.get(phase, "Choose an option:")


def _describe_option(a: dict) -> str:
    kind = a.get("action", "?")
    if kind == "move":
        return f"Move to {a.get('point_type', '?')}"
    if kind == "pick_card":
        return (f"Take {_card_label(a.get('card_id', '?'), a.get('upgraded', False))}"
                f" [{a.get('rarity', '')}]")
    if kind == "skip":
        return "Skip (take no card)"
    if kind == "rest_option":
        return f"{a.get('label', a.get('option_id', '?'))} -- {a.get('description', '')}"
    if kind == "event_choice":
        return f"{a.get('label', '?')} -- {a.get('description', '')}"
    if kind == "buy_card":
        return (f"Buy {_card_label(a.get('card_id', '?'))} for "
                f"{a.get('price', '?')} gold")
    if kind == "buy_relic":
        return f"Buy relic {a.get('relic_id', '?')} for {a.get('price', '?')} gold"
    if kind == "buy_potion":
        return f"Buy potion {a.get('potion_id', '?')} for {a.get('price', '?')} gold"
    if kind == "remove_card":
        return f"Remove a card from your deck for {a.get('price', '?')} gold"
    if kind == "leave_shop":
        return "Leave the shop"
    if kind == "collect":
        return "Open the treasure chest"
    if kind == "pick_relic_reward":
        return f"Take relic {a.get('relic_id', '?')}"
    if kind == "pick_potion":
        return f"Take potion {a.get('potion_id', '?')}"
    if kind in ("skip_potion", "skip_relic"):
        return "Decline it"
    if kind == "choose":
        cid = a.get("card_id") or a.get("option_id") or "?"
        return f"Choose {_card_label(cid) if a.get('card_id') else cid}"
    if kind == "confirm_choice":
        return "Confirm"
    return str(kind)


#: Accepts "CHOICE: 3", "choice 3", a bare leading integer, or "**3**".
_CHOICE_RE = re.compile(r"choice\s*[:\-]?\s*\**\s*(\d+)", re.IGNORECASE)
_BARE_RE = re.compile(r"^\D{0,12}?(\d+)")


def parse_choice(reply: str, n_options: int) -> int | None:
    """Extract the option index from a model reply, or None if unparseable.

    Returning None rather than guessing is deliberate: a silent fallback to
    option 0 would make a broken model look like a playing one, and option 0
    is frequently 'skip' or the first map node.
    """
    if not reply:
        return None
    m = _CHOICE_RE.search(reply)
    if not m:
        m = _BARE_RE.search(reply.strip())
    if not m:
        return None
    try:
        idx = int(m.group(1))
    except ValueError:
        return None
    return idx if 0 <= idx < n_options else None
