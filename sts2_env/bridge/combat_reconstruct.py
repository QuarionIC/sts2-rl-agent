"""Rebuild a simulatable CombatState from a live bridge payload.

The deterministic planner searches by cloning and stepping a real
``CombatState``. To use it against the running game we must reconstruct that
object from the JSON the mod sends. This module does that, and -- just as
importantly -- reports precisely what is missing when it cannot, so the
caller degrades on a stated reason instead of silently planning on a guess.

Why this is not merely a parsing job
------------------------------------
Planning is only sound if the reconstructed state steps identically to the
game's. Three things are required and the current mod build sends none of
them:

* **Deck contents.** Payloads carry ``deck_size``, an integer. Without the
  actual cards we cannot know what is in the draw pile.
* **Ordered piles.** Payloads carry ``draw_pile_count`` /
  ``discard_pile_count`` / ``exhaust_pile_count`` -- counts only. The
  planner's entire premise is that combat is deterministic *given the draw
  order*; with only a count, every simulated draw is a guess and the plan
  diverges from reality on the first shuffle.
* **Enemy AI move state.** ``SerializeEnemy`` sends id/hp/block/powers and
  the current intent, but not the monster's internal move history, which
  decides what it does on subsequent turns. A plan beyond one enemy turn
  would be fiction.

Rather than fabricate these, :func:`probe_payload` names the gaps and
:func:`reconstruct_combat` returns ``None``. The bridge then falls back to
the LLM for combat and says why, once, in the log.

Unlocking the planner
---------------------
Add to ``RlCombatHandler.SerializeCombatState``:

    ["draw_pile"]    = pcs.DrawPile.Cards.Select(SerializeCard).ToList(),
    ["discard_pile"] = pcs.DiscardPile.Cards.Select(SerializeCard).ToList(),
    ["exhaust_pile"] = pcs.ExhaustPile.Cards.Select(SerializeCard).ToList(),
    ["deck"]         = runState.Player.Deck.Cards.Select(SerializeCard).ToList(),

plus the monster's move index/history in ``SerializeEnemy``. ``DrawPile``
is already ordered, so emitting it in order is sufficient. After a rebuild
and redeploy this module's probe passes and the planner engages with no
Python change.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import Any

logger = logging.getLogger(__name__)

#: Payload keys the planner needs beyond what the mod currently sends.
REQUIRED_FOR_PLANNING = ("draw_pile", "discard_pile", "deck")


@dataclass
class ProbeResult:
    """What a payload can and cannot support."""

    can_plan: bool
    missing: list[str] = field(default_factory=list)
    note: str = ""

    def reason(self) -> str:
        if self.can_plan:
            return "payload supports planning"
        return (
            "combat planner unavailable -- the mod does not send "
            + ", ".join(self.missing)
            + (f" ({self.note})" if self.note else "")
        )


def probe_payload(state: dict[str, Any]) -> ProbeResult:
    """Decide whether *state* carries enough to plan on.

    Checked once per session by the bridge; the answer is a property of the
    deployed mod build, not of any individual combat.
    """
    missing = [k for k in REQUIRED_FOR_PLANNING if not state.get(k)]
    if missing:
        return ProbeResult(
            can_plan=False,
            missing=missing,
            note="counts are sent but not contents, so draw order is unknown",
        )
    return ProbeResult(can_plan=True)


def reconstruct_combat(state: dict[str, Any]) -> Any | None:
    """Build a CombatState from *state*, or None when data is insufficient.

    Returns None rather than a best-effort approximation on purpose: a plan
    computed against a wrong draw order is not a weaker plan, it is an
    invalid one, and it would replay into the live game as a sequence of
    illegal or nonsensical actions.
    """
    probe = probe_payload(state)
    if not probe.can_plan:
        return None

    from sts2_env.cards.factory import create_card
    from sts2_env.core.combat import CombatState

    combat_block = state.get("combat_state") or state
    player = combat_block.get("player") or {}

    def _cards(key: str) -> list:
        out = []
        for c in state.get(key, []) or []:
            cid = c.get("id") or c.get("card_id")
            if not cid:
                continue
            try:
                out.append(create_card(cid, upgraded=bool(c.get("upgraded"))))
            except Exception:
                logger.debug("unknown card id in %s: %s", key, cid)
        return out

    deck = _cards("deck")
    if not deck:
        return None

    combat = CombatState(
        player_hp=int(player.get("hp", state.get("hp", 1))),
        player_max_hp=int(player.get("max_hp", state.get("max_hp", 1))),
        deck=deck,
        # Any seed: the planner is handed explicit pile ORDER below, so the
        # RNG only matters for effects that draw randomly mid-fight.
        rng_seed=int(state.get("round", 0)) or 1,
        relics=list(state.get("relics", []) or []),
        gold=int(state.get("gold", 0) or 0),
        character_id="Necrobinder",
        ascension_level=int(state.get("ascension_level", 0) or 0),
    )
    return combat
