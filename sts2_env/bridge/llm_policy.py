"""Drive the REAL game with a local LLM.

The bridge already speaks in option indices: every non-combat payload carries
an option/node/card list, and each ``_pick_*`` helper in ``agent_runner``
returns an index into it. That is exactly the interface the LLM harness was
built against in-sim, so the model slots in without touching the wire
protocol: render the payload as text, ask, parse an index back.

Fallback is the existing heuristic, not option 0. A parse failure must
degrade to the behaviour the bridge had before this module existed, and it
is COUNTED -- otherwise a broken model silently looks like a playing one,
which is the same trap the in-sim harness hit at a 23% parse rate.

Combat
------
Combat is also routed here, and that is a deliberate compromise worth
stating. In-sim, combat is played by the deterministic beam planner, which
needs the exact draw order to search. The mod only sends
``draw_pile_count`` -- a COUNT, not the ordered contents -- so the planner
cannot be used against the live client without a mod change that exposes
the pile. Until then the LLM plays combat too. It is slower and was never
evaluated on combat decisions, so live combat quality is unmeasured.
"""

from __future__ import annotations

import logging
from typing import Any, Callable

logger = logging.getLogger(__name__)

SYSTEM_PROMPT = """You are playing Slay the Spire 2 as the Necrobinder.

Your goal is to survive as deep into the run as possible. Key principles:
- HP is a resource, but running out ends the run. Below ~40% HP, healing
  usually beats a permanent upgrade, and elites become very dangerous.
- Deck quality matters more than deck size: a few strong cards beat many
  weak ones. Upgrades are permanent value.
- Card tiers are given as (S/A/B/C/D/F); S is best, F is worst. Prefer the
  higher tier unless you have a concrete reason not to.

Reply with ONLY the number of your chosen option, then a brief reason.
Format exactly:
CHOICE: <number>
WHY: <one short sentence>"""


def _txt(v: Any) -> str:
    return "" if v is None else str(v)


def _card_text(card: dict[str, Any]) -> str:
    """Card label with community tier when the id is recognised."""
    name = _txt(card.get("card_id") or card.get("id") or card.get("name") or "?")
    try:
        from sts2_env.knowledge.card_priors import card_prior

        tier = card_prior(name).tier
        suffix = f" ({tier})"
    except Exception:
        suffix = ""
    up = "+" if card.get("upgraded") else ""
    cost = card.get("cost")
    cost_s = f" [{cost}e]" if cost is not None else ""
    return f"{name}{up}{suffix}{cost_s}"


def _header(state: dict[str, Any]) -> list[str]:
    hp, max_hp = state.get("hp"), state.get("max_hp")
    lines = []
    where = []
    if state.get("act") is not None:
        where.append(f"Act {state['act']}")
    if state.get("floor") is not None:
        where.append(f"floor {state['floor']}")
    head = ", ".join(where)
    if hp is not None and max_hp:
        head += f". HP {hp}/{max_hp}"
    if state.get("gold") is not None:
        head += f". Gold {state['gold']}"
    if head:
        lines.append(head.strip(". "))
    relics = state.get("relics")
    if relics:
        lines.append(f"Relics: {', '.join(_txt(r) for r in relics)}")
    # The mod sends deck_size but NOT the deck contents, so the model cannot
    # reason about synergy or duplicates the way it can in-sim. Surfacing the
    # size at least lets it judge dilution.
    if state.get("deck_size") is not None:
        lines.append(f"Deck size: {state['deck_size']} cards")
    return lines


def render_options(state: dict[str, Any], options: list[dict[str, Any]],
                   question: str) -> str:
    """Numbered menu for a non-combat decision."""
    lines = _header(state)
    lines.append("")
    lines.append(question)
    lines.append("")
    for i, opt in enumerate(options):
        label = (opt.get("label") or opt.get("name") or opt.get("type")
                 or opt.get("option_id") or opt.get("id"))
        desc = opt.get("description") or opt.get("desc") or ""
        if opt.get("card_id") or opt.get("cards"):
            label = _card_text(opt) if opt.get("card_id") else _txt(label)
        price = opt.get("price") or opt.get("cost")
        price_s = f" -- {price} gold" if price is not None and opt.get("price") else ""
        line = f"{i}. {_txt(label)}{price_s}"
        if desc:
            line += f" -- {desc}"
        lines.append(line)
    return "\n".join(lines)


def render_combat(state: dict[str, Any], options: list[dict[str, Any]]) -> str:
    """Numbered menu for a combat decision.

    Enemy intents are included because they are the whole basis for choosing
    block vs damage this turn, and the bridge does send them.
    """
    combat = state.get("combat_state") or {}
    player = combat.get("player") or {}
    lines = _header(state)
    lines.append(
        f"Combat -- your HP {player.get('hp', '?')}/{player.get('max_hp', '?')}, "
        f"block {player.get('block', 0)}, energy {combat.get('energy', '?')}"
    )
    for e in combat.get("enemies", []) or []:
        if e.get("is_dead") or e.get("hp", 1) <= 0:
            continue
        intent = e.get("intent") or e.get("intent_type") or "?"
        dmg = e.get("intent_damage")
        intent_s = f"{intent}" + (f" {dmg}" if dmg else "")
        lines.append(
            f"  Enemy {_txt(e.get('name') or e.get('monster_id'))}: "
            f"HP {e.get('hp', '?')}/{e.get('max_hp', '?')} "
            f"block {e.get('block', 0)} intent {intent_s}"
        )
    lines.append("")
    lines.append("Which action do you take?")
    lines.append("")
    for i, opt in enumerate(options):
        lines.append(f"{i}. {opt.get('_label', '?')}")
    return "\n".join(lines)


class BridgeLLMPolicy:
    """Chooses bridge option indices with a local LLM.

    ``pick(state, options, question, fallback)`` is the whole interface:
    ``options`` are the payload's own option dicts, and the return value is
    the index the bridge expects -- identical to what the heuristic
    ``_pick_*`` helpers return.
    """

    def __init__(self, llm: Any, log_decisions: bool = True):
        self.llm = llm
        self.log_decisions = log_decisions
        self.asked = 0
        self.parsed = 0
        self.failures = 0
        self.transcript: list[dict] = []

    @property
    def parse_rate(self) -> float:
        return self.parsed / self.asked if self.asked else 0.0

    def pick(self, prompt: str, options: list[Any],
             fallback: Callable[[], int], tag: str = "") -> int:
        """Ask the model; on any failure use ``fallback`` and count it."""
        from sts2_env.llm.state_text import parse_choice

        if not options:
            return fallback()
        if len(options) == 1:
            return 0

        self.asked += 1
        try:
            reply = self.llm.ask(SYSTEM_PROMPT, prompt)
        except Exception as exc:  # a model failure must never kill the run
            self.failures += 1
            logger.warning("LLM call failed (%s); using heuristic", exc)
            return fallback()

        idx = parse_choice(reply, len(options))
        if idx is None:
            self.failures += 1
            chosen = fallback()
            logger.info("[%s] unparseable reply, heuristic -> %s", tag, chosen)
        else:
            self.parsed += 1
            chosen = idx
            if self.log_decisions:
                why = reply.split("WHY:")[-1].strip()[:120] if "WHY:" in reply else ""
                logger.info("[%s] LLM chose %d%s", tag, idx, f" -- {why}" if why else "")
        self.transcript.append({
            "tag": tag, "prompt": prompt, "reply": (reply or "")[:400],
            "index": idx, "used": chosen,
        })
        return chosen

    def stats(self) -> dict:
        return {
            "asked": self.asked,
            "parsed": self.parsed,
            "failures": self.failures,
            "parse_rate": round(self.parse_rate, 4),
            "llm_calls": getattr(self.llm, "calls", 0),
            "tokens_per_s": round(getattr(self.llm, "tokens_per_s", 0.0), 2),
        }
