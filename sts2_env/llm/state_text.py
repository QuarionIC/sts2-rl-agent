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


# ---------------------------------------------------------------------------
# Combat rendering (the LLM playing fights itself)
# ---------------------------------------------------------------------------

COMBAT_SYSTEM_PROMPT = """You are playing Slay the Spire 2 as the Necrobinder.

You are IN COMBAT and choosing ONE action. You will be asked again after each
action, so play one card at a time and end the turn when you are done.

How to decide, in priority order:
1. LETHAL FIRST. If you can kill every living enemy this turn, do it -- the
   fight ends and you take no more damage. Add up your available damage
   before deciding to block.
2. Killing ONE enemy of several reduces the damage you take every future
   turn. Focus fire rather than spreading damage.
3. Only block against INCOMING ATTACKS. "Incoming damage this turn" is given
   to you. If it is 0, blocking is wasted -- develop instead. Block is lost
   at end of turn.
4. Debuffs and powers played EARLY pay off over the whole fight. Vulnerable
   (+50% damage taken) applied before your big hits is worth more than one
   extra small attack.
5. Unspent energy is wasted. Try to use it all.

Costs are shown as (cost E). You have limited energy per turn.
Card tiers are (S/A/B/C/D/F); S is best.

Reply with ONLY the number of your chosen action, then a brief reason.
Format exactly:
CHOICE: <number>
WHY: <one short sentence>"""


def _enemy_label(index: int, enemy) -> str:
    """Stable, unambiguous enemy name: ``#0 EXORDIUM_LOUSE_RED``.

    Duplicate encounters (2x Louse, 3x Gremlin) share a monster_id, so without
    the index two 'Play Strike -> EXORDIUM_LOUSE_RED' options are literally
    indistinguishable in the prompt and the focus-fire instruction in the
    system prompt is unusable. The index matches the enemy list order.
    """
    return f"#{index} {getattr(enemy, 'monster_id', '?')}"


def _powers_text(creature) -> str:
    powers = getattr(creature, "powers", None) or {}
    if not powers:
        return ""
    return ", ".join(str(v) for v in powers.values())


def _intent_text(combat, enemy) -> str:
    """What this enemy will do next turn, as the player sees it."""
    ai = combat.enemy_ais.get(enemy.combat_id)
    move = getattr(ai, "current_move", None)
    intents = getattr(move, "intents", None) or []
    if not intents:
        return "unknown"
    parts = []
    for it in intents:
        kind = getattr(getattr(it, "intent_type", None), "name", "?")
        if getattr(it, "is_attack", False):
            hits = int(getattr(it, "hits", 1) or 1)
            dmg = int(getattr(it, "damage", 0) or 0)
            parts.append(f"ATTACK {dmg}x{hits} = {dmg * hits}" if hits > 1
                         else f"ATTACK {dmg}")
        else:
            parts.append(kind)
    return ", ".join(parts)


def render_combat_decision(mgr: RunManager, mask) -> Decision | None:
    """Build the prompt for the current in-combat decision, or None.

    Deliberately separate from :func:`render_decision` rather than folded into
    it. ``render_decision`` returns None in combat and its caller then falls
    back to the first legal action -- which in the combat slice is END TURN.
    Adding combat to ``LLM_PHASES`` instead would silently change what the
    existing out-of-combat-only evaluation measures, and that number
    (13.1 floors) is a published baseline that has to stay reproducible.

    Options are built by decoding the env's LEGAL MASK, not by listing
    ``get_available_actions()``. The combat action space is a fixed 115-wide
    encoding (end turn, hand-slot x target, potion-slot x target), NOT a dense
    list in available-actions order, so matching dicts back to indices would
    need a hand-written inverse that could silently mis-target. Decoding the
    mask instead makes option *i* the env action ``legal[i]`` by construction:
    every option is legal because it came from the mask, and the mapping
    cannot drift from the env's own encoding.

    Each returned option carries ``env_action`` -- the index to pass to
    ``env.step`` -- so the caller needs no resolution step at all.
    """
    if mgr.phase != RunManager.PHASE_COMBAT:
        return None
    combat = mgr.get_combat_state()
    if combat is None or combat.is_over:
        return None

    import numpy as np

    from sts2_env.gym_env.action_space import (
        action_to_card_and_target,
        action_to_potion_and_target,
        is_potion_action,
    )
    from sts2_env.gym_env.run_env import _LAYOUT

    legal = np.flatnonzero(np.asarray(mask, dtype=bool))
    if not legal.size:
        return None

    st = combat.current_player_state
    me = combat.primary_player
    rs = mgr.run_state
    hand = list(st.hand)
    enemies = list(combat.enemies)

    select_actions = [a for a in mgr.get_available_actions()
                      if a.get("action") == "select_player"]

    options: list[dict] = []
    for idx in legal.tolist():
        idx = int(idx)
        ps_start, ps_size = _LAYOUT.player_select_start, _LAYOUT.player_select_size
        if ps_start <= idx < ps_start + ps_size:
            j = idx - ps_start
            if 0 <= j < len(select_actions):
                options.append({"env_action": idx, "kind": "select_player",
                                "character_id": select_actions[j].get("character_id")})
            continue
        local = idx - _LAYOUT.combat_start
        if not (0 <= local < _LAYOUT.combat_size):
            continue
        if combat.pending_choice is not None:
            if local == 0:
                options.append({"env_action": idx, "kind": "confirm_choice"})
            else:
                ci = local - 1
                opts = combat.pending_choice.options
                cid = (opts[ci].card.card_id.name
                       if 0 <= ci < len(opts) else None)
                options.append({"env_action": idx, "kind": "choose",
                                "card_id": cid, "index": ci})
            continue
        if is_potion_action(local):
            slot, tgt = action_to_potion_and_target(local)
            potion = (st.potions[slot]
                      if slot is not None and 0 <= slot < len(st.potions) else None)
            options.append({
                "env_action": idx, "kind": "use_potion",
                "potion_id": getattr(potion, "potion_id", None),
                "target_index": tgt,
                "target_name": (getattr(enemies[tgt], "monster_id", None)
                                if tgt is not None and 0 <= tgt < len(enemies)
                                else None),
            })
            continue
        hand_idx, tgt = action_to_card_and_target(local)
        if hand_idx is None:
            options.append({"env_action": idx, "kind": "end_turn"})
            continue
        card = hand[hand_idx] if 0 <= hand_idx < len(hand) else None
        if card is None:
            continue  # mask allowed a slot the hand no longer fills
        options.append({
            "env_action": idx, "kind": "play_card",
            "card_id": card.card_id.name, "upgraded": bool(card.upgraded),
            "cost": card.cost, "hand_index": hand_idx,
            "target_index": tgt,
            "target_name": (getattr(enemies[tgt], "monster_id", None)
                            if tgt is not None and 0 <= tgt < len(enemies) else None),
        })

    if not options:
        return None

    # Incoming damage comes from the planner's reader rather than a second
    # copy of the intent-walking logic here -- two readers of the same AI
    # state is exactly the kind of thing that drifts apart silently.
    from sts2_env.search.combat_planner import incoming_damage
    incoming = incoming_damage(combat)

    lines = [
        f"COMBAT -- Act {rs.current_act_index + 1}, floor {rs.total_floor}, "
        f"turn {combat.turn_count}.",
        f"You: HP {me.current_hp}/{me.max_hp}"
        + (f", Block {me.block}" if me.block else "")
        + f", Energy {st.energy}",
    ]
    mine = _powers_text(me)
    if mine:
        lines.append(f"Your powers: {mine}")

    lines.append("")
    lines.append("Enemies:")
    for i, e in enumerate(enemies):
        if not e.is_alive:
            continue
        bits = [f"HP {e.current_hp}/{e.max_hp}"]
        if e.block:
            bits.append(f"Block {e.block}")
        ep = _powers_text(e)
        if ep:
            bits.append(ep)
        lines.append(f"  - {_enemy_label(i, e)}: {', '.join(bits)}"
                     f" | intent: {_intent_text(combat, e)}")
    lines.append(f"Incoming damage this turn: {incoming}"
                 + ("  (nothing is attacking -- blocking is wasted)"
                    if incoming == 0 else ""))

    if hand:
        lines.append("")
        lines.append(f"Your hand ({len(hand)}): "
                     + ", ".join(f"{_card_label(c.card_id.name, c.upgraded)}"
                                 f"({c.cost}E)" for c in hand))
    lines.append(f"Piles: draw {len(st.draw)}, discard {len(st.discard)}, "
                 f"exhaust {len(st.exhaust)}")

    if combat.pending_choice is not None:
        lines.append("")
        lines.append(f"PROMPT: {combat.pending_choice.prompt}")

    lines.append("")
    lines.append("Choose ONE action:")
    lines.append("")
    for i, a in enumerate(options):
        lines.append(f"{i}. {_describe_combat_option(a)}")

    return Decision("\n".join(lines), options, RunManager.PHASE_COMBAT)


def _target_text(a: dict) -> str:
    """``#1 EXORDIUM_LOUSE_RED`` -- index first, matching the enemy list."""
    idx = a.get("target_index")
    name = a.get("target_name", "?")
    return f"#{idx} {name}" if idx is not None else str(name)


def _describe_combat_option(a: dict) -> str:
    kind = a.get("kind") or a.get("action", "?")
    if kind == "end_turn":
        return "End turn"
    if kind == "play_card":
        label = _card_label(a.get("card_id", "?"), a.get("upgraded", False))
        s = f"Play {label} ({a.get('cost', '?')}E)"
        if a.get("target_name"):
            s += f" -> {_target_text(a)}"
        return s
    if kind == "use_potion":
        s = f"Use potion {a.get('potion_id', '?')}"
        if a.get("target_name"):
            s += f" -> {_target_text(a)}"
        return s
    if kind == "select_player":
        return f"Switch to {a.get('character_id', '?')}"
    # Pending in-combat choices reuse the out-of-combat wording.
    return _describe_option(a)


#: Accepts "CHOICE: 3", "choice 3", a bare leading integer, or "**3**".
_CHOICE_RE = re.compile(r"choice\s*[:\-]?\s*\**\s*(\d+)", re.IGNORECASE)
_BARE_RE = re.compile(r"^\D{0,12}?(\d+)")
#: Qwen3.6 is a hybrid reasoning model: unless thinking is disabled it emits a
#: <think> block first. Its contents routinely mention option numbers while
#: deliberating ("option 3 is tempting, but..."), so parsing before stripping
#: it reads the model's discarded candidates instead of its answer.
_THINK_RE = re.compile(r"<think>.*?</think>", re.DOTALL | re.IGNORECASE)
_OPEN_THINK_RE = re.compile(r"<think>.*", re.DOTALL | re.IGNORECASE)


def parse_choice(reply: str, n_options: int) -> int | None:
    """Extract the option index from a model reply, or None if unparseable.

    Returning None rather than guessing is deliberate: a silent fallback to
    option 0 would make a broken model look like a playing one, and option 0
    is frequently 'skip' or the first map node.
    """
    if not reply:
        return None
    reply = _THINK_RE.sub(" ", reply)
    # An unterminated <think> means generation was cut off mid-reasoning:
    # there is no answer to parse, and whatever numbers appear are candidates
    # the model was still weighing.
    if _OPEN_THINK_RE.search(reply):
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


def render_run_decision_masked(mgr: RunManager, mask) -> Decision | None:
    """Out-of-combat decision offering ONLY options the env can actually take.

    ``render_decision`` builds its menu from ``get_available_actions()`` and
    the caller then maps the chosen option back to an env index. Measured with
    a stub that always answers legally, 20% of out-of-combat choices failed to
    resolve and were silently replaced by the fallback policy. Three causes,
    all "the prompt offered something unselectable":

    * **Pending run choices** (card selection from an event, 23 of 30 observed
      failures). When a NON-combat phase raises a ``choose``/``confirm_choice``
      prompt, the env masks those into the COMBAT slice -- see
      ``run_env._compute_mask`` -- while the resolver looked in the event
      slice. Every such decision fell through to the knowledge policy, so the
      model was never actually making them.
    * **Card-reward potion/relic offers**, where skip lives at
      ``card_reward_start + 3`` rather than ``+ 1``.
    * **Map choices beyond ``map_size``**, which have no index at all.

    This wrapper fixes the first case explicitly and makes the rest
    fail-safe: any option that does not land on a legal mask bit is DROPPED
    and the menu renumbered, so the model can no longer spend a decision on
    something that will be thrown away. Each surviving option carries
    ``env_action``, matching :func:`render_combat_decision`, so callers never
    need a second resolution step.

    ``render_decision`` itself is left untouched -- the published
    out-of-combat baseline (13.1 floors) was measured through it and has to
    stay reproducible. That baseline is affected by these bugs and should be
    re-measured through this path before it is compared against anything.
    """
    import numpy as np

    from sts2_env.gym_env.run_env import _LAYOUT

    base_decision = render_decision(mgr)
    if base_decision is None:
        return None
    mask = np.asarray(mask, dtype=bool)
    unfiltered = mgr.get_available_actions()

    starts = {
        RunManager.PHASE_MAP_CHOICE: _LAYOUT.map_start,
        RunManager.PHASE_CARD_REWARD: _LAYOUT.card_reward_start,
        RunManager.PHASE_SHOP: _LAYOUT.shop_start,
        RunManager.PHASE_REST_SITE: _LAYOUT.rest_start,
        RunManager.PHASE_EVENT: _LAYOUT.event_start,
        RunManager.PHASE_TREASURE: _LAYOUT.treasure_start,
        RunManager.PHASE_BOSS_RELIC: _LAYOUT.boss_relic_start,
    }

    # A pending run choice is masked into the combat slice regardless of which
    # phase raised it.
    pending = [a for a in unfiltered
               if a.get("action") in {"choose", "confirm_choice"}]
    choose_actions = [a for a in unfiltered if a.get("action") == "choose"]

    kept: list[dict] = []
    for opt in base_decision.options:
        idx = None
        if pending:
            if opt.get("action") == "confirm_choice":
                idx = _LAYOUT.combat_start
            elif opt.get("action") == "choose":
                try:
                    idx = _LAYOUT.combat_start + 1 + choose_actions.index(opt)
                except ValueError:
                    idx = None
        else:
            b = starts.get(mgr.phase)
            if b is not None:
                try:
                    idx = b + unfiltered.index(opt)
                except ValueError:
                    idx = None
        if idx is None or not (0 <= idx < mask.size and mask[idx]):
            continue
        o = dict(opt)
        o["env_action"] = int(idx)
        kept.append(o)

    if not kept:
        return None

    # Rebuild the menu so the numbers the model sees match `kept`.
    head = base_decision.prompt.split("\n0. ")[0] if "\n0. " in base_decision.prompt \
        else "\n".join(base_decision.prompt.splitlines()[:-len(base_decision.options)])
    lines = [head, ""]
    for i, a in enumerate(kept):
        lines.append(f"{i}. {_describe_option(a)}")
    return Decision("\n".join(lines), kept, base_decision.phase)
