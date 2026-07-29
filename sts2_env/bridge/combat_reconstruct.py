"""Rebuild a simulatable CombatState from a live bridge payload.

The deterministic planner searches by cloning and stepping a real
``CombatState``. To use it against the running game we must reconstruct that
object from the JSON the mod sends. This module does that, and -- just as
importantly -- reports precisely what is missing when it cannot, so the
caller degrades on a stated reason instead of silently planning on a guess.

Why this is not merely a parsing job
------------------------------------
Planning is only sound if the reconstructed state steps identically to the
game's. Three things are required. The first two are now sent by the
rebuilt mod; the third is still missing:

* **Deck contents.** SENT (``deck``). Previously only ``deck_size``.
* **Ordered piles.** SENT (``draw_pile`` / ``discard_pile`` /
  ``exhaust_pile``). Previously counts only, which was fatal: the planner's
  premise is that combat is deterministic *given the draw order*, so with
  only a count every simulated draw is a guess.
* **Enemy AI move state.** ``SerializeEnemy`` sends id/hp/block/powers and
  the current intent, but not the monster's internal move history, which
  decides what it does on subsequent turns. A plan beyond one enemy turn
  would be fiction.

Rather than fabricate what is missing, :func:`probe_payload` names the gaps
and :func:`reconstruct_combat` returns ``None``. The bridge then falls back
and says why, once, in the log.

Note the wire sends card ids as STRINGS while ``create_card`` takes a
``CardId`` enum. Passing the string through raised AttributeError on every
card, emptying the deck and making reconstruction return None on a payload
that was actually complete -- the reason live play logged "planner ENGAGED"
and then never planned. :func:`_to_card_id` does the conversion.

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


#: Mod id prefixes the game puts on monster ids. "Acts from the Past"
#: namespaces its content, so the wire sends ACTSFROMTHEPAST-CULTIST while
#: the simulator calls the same creature EXORDIUM_CULTIST.
_MOD_PREFIXES = ("ACTSFROMTHEPAST-", "ACT4HEART-", "DOWNFALL-", "BASE-")

_MONSTER_FACTORIES: dict[str, Any] | None = None


def _normalize_monster_id(raw: str, registry: dict[str, Any]) -> str | None:
    """Resolve a wire monster id to a registry key, or None.

    Live play sent ACTSFROMTHEPAST-CULTIST, which matched nothing and made
    the planner decline every fight in the legacy acts -- the agent then
    ended its turn until it died. Matching now strips the mod namespace and
    falls back to a suffix match, since the simulator qualifies legacy
    creatures by act (CULTIST -> EXORDIUM_CULTIST).
    """
    name = str(raw).strip().upper().replace("-", "_")
    if name in registry:
        return name
    for pref in _MOD_PREFIXES:
        p2 = pref.replace("-", "_")
        if name.startswith(p2):
            bare = name[len(p2):]
            if bare in registry:
                return bare
            name = bare
            break
    # Act-qualified variants: CULTIST -> EXORDIUM_CULTIST / ACT1_CULTIST ...
    suffix_hits = [k for k in registry if k.endswith("_" + name)]
    if len(suffix_hits) == 1:
        return suffix_hits[0]
    if suffix_hits:
        # Prefer the legacy-act spelling, which is what the mod supplies.
        for pref in ("EXORDIUM_", "THECITY_", "THEBEYOND_"):
            for k in suffix_hits:
                if k.startswith(pref):
                    return k
        return sorted(suffix_hits)[0]
    flat = name.replace("_", "")
    for k in registry:
        if k.replace("_", "") == flat:
            return k
    # Size suffixes: the mod spells them out (ACID_SLIME_SMALL) while the
    # simulator abbreviates (EXORDIUM_ACID_SLIME_S).
    for long, short in (("_SMALL", "_S"), ("_MEDIUM", "_M"), ("_LARGE", "_L"),
                        ("_TINY", "_S"), ("_BIG", "_L")):
        if name.endswith(long):
            alt = name[: -len(long)] + short
            if alt in registry:
                return alt
            hits = [k for k in registry if k.endswith("_" + alt)]
            if hits:
                for pref in ("EXORDIUM_", "THECITY_", "THEBEYOND_"):
                    for k in hits:
                        if k.startswith(pref):
                            return k
                return sorted(hits)[0]
    return None


def _monster_factories() -> dict[str, Any]:
    """Map monster_id -> factory, built by probing every create_* function.

    The simulator has no id->factory registry: encounters call the concrete
    ``create_gremlin_nob(rng, asc)`` helpers directly. Rebuilding a live
    fight needs the reverse lookup, so each factory is called once with a
    throwaway RNG and indexed by the monster_id it produces. Built lazily
    and cached; factories that need combat context are skipped rather than
    allowed to raise.
    """
    global _MONSTER_FACTORIES
    if _MONSTER_FACTORIES is not None:
        return _MONSTER_FACTORIES

    import importlib
    import inspect

    from sts2_env.core.rng import Rng

    reg: dict[str, Any] = {}
    modules = ("act1", "act1_weak", "act2", "act3", "act4", "act4_heart",
               "exordium", "thecity", "thebeyond", "shared")
    for mod_name in modules:
        try:
            mod = importlib.import_module(f"sts2_env.monsters.{mod_name}")
        except Exception:
            continue
        for name, fn in inspect.getmembers(mod, inspect.isfunction):
            if not name.startswith("create_"):
                continue
            try:
                out = fn(Rng(1), 0)
            except Exception:
                try:
                    out = fn(Rng(1))
                except Exception:
                    continue
            if not (isinstance(out, tuple) and len(out) == 2):
                continue
            creature = out[0]
            mid = getattr(creature, "monster_id", None)
            if mid:
                reg.setdefault(str(mid), fn)
    _MONSTER_FACTORIES = reg
    logger.info("monster factory registry: %d ids", len(reg))
    return reg


def _rebuild_potions(state: dict[str, Any]) -> list:
    """Rebuild the player's potion belt from the wire.

    This was missing entirely: reconstruct_combat never passed potions to
    CombatState, so every rebuilt fight had empty slots. get_action_mask
    then marked all 54 potion actions illegal, and both the RL agent and the
    planner searched a game in which potions did not exist. The agent was
    not declining to drink -- it was never offered the option.

    Slots are positional, so a consumed or automatic potion must leave a
    HOLE rather than shift the ones after it; the action space indexes by
    slot number.
    """
    from sts2_env.potions import create_potion

    wire = state.get("potions") or []
    if not wire:
        return []
    size = max(int(p.get("slot", i) or 0) for i, p in enumerate(wire)) + 1
    belt: list = [None] * max(size, 3)
    for i, spec in enumerate(wire):
        slot = int(spec.get("slot", i) or 0)
        pid = spec.get("id") or spec.get("potion_id")
        if not pid or not (0 <= slot < len(belt)):
            continue
        # Automatic potions fire on their own; the agent cannot spend them,
        # and offering them as actions would produce moves the game rejects.
        if spec.get("can_use") is False:
            continue
        try:
            belt[slot] = create_potion(str(pid), slot=slot)
        except Exception:
            logger.debug("unknown potion id %r", pid)
    return belt


def _restore_ai_state(ai: Any, spec: dict[str, Any]) -> bool:
    """Point the monster's state machine at the move it is actually on.

    Without this the reconstruction rolls a FRESH move, so the simulated
    enemy does something different from the one on screen -- and every turn
    planned past the first is against a fiction. The mod sends the move id
    it is about to perform; the simulator's state ids are the same move
    names (JAW_WORM: BELLOW / CHOMP / THRASH), so the id maps directly.

    Returns whether the state was recognised, so callers can decline to plan
    rather than plan blind.
    """
    raw = (spec.get("ai_state") or spec.get("intent_move_id")
           or spec.get("next_move_id") or spec.get("move_id"))
    states = getattr(ai, "states", None)
    if not raw or not isinstance(states, dict):
        return False
    def _is_move(key: Any) -> bool:
        """Only MOVE states are valid here.

        The state machine also holds BRANCH nodes (LAGAVULIN_AWAKE_BRANCH),
        which route between moves and have no intents -- assigning one makes
        ``current_move`` assert. A wire id that lands on a branch must be
        rejected, not set.
        """
        st = states.get(key)
        return bool(getattr(st, "is_move", False))

    want = str(raw).strip().upper().replace("-", "_")
    if want in states and _is_move(want):
        ai._current_state_id = want
        return True
    flat = want.replace("_", "")
    for k in states:
        if str(k).upper().replace("_", "") == flat and _is_move(k):
            ai._current_state_id = k
            return True
    logger.debug("unmapped AI move id %r (known: %s)", raw, sorted(states)[:6])
    return False


def _character_from(state: dict[str, Any]) -> str:
    """Character for the reconstructed combat.

    Taken from the payload when present, else the agent config the mod also
    reads, else Ironclad. Hardcoding this was a real bug: the planner would
    have rebuilt an Ironclad fight as a Necrobinder one, giving every card
    and relic the wrong owner.
    """
    for key in ("character", "character_id", "player_class"):
        v = state.get(key)
        if v:
            return str(v)
    try:
        from pathlib import Path as _P

        for d in (_P(__file__).resolve().parents[2] / "bridge_mod",):
            f = d / "sts2_agent_config.txt"
            if f.exists():
                for line in f.read_text(encoding="utf-8").splitlines():
                    if line.strip().lower().startswith("character="):
                        return line.split("=", 1)[1].strip()
    except Exception:
        pass
    return "Ironclad"


def _to_card_id(raw: str):
    """Wire card-id string -> CardId enum, or None if unrecognised.

    Tried in order: exact name, upper-snake, and a normalised form that
    strips non-alphanumerics, so wire spellings like "Strike_Ironclad" or
    "strike ironclad" all resolve to STRIKE_IRONCLAD.
    """
    from sts2_env.core.enums import CardId

    name = raw.replace("CardId.", "").strip()
    for cand in (name, name.upper(), name.upper().replace(" ", "_").replace("-", "_")):
        try:
            return CardId[cand]
        except KeyError:
            continue
    flat = "".join(ch for ch in name.upper() if ch.isalnum())
    for member in CardId:
        if "".join(ch for ch in member.name if ch.isalnum()) == flat:
            return member
    return None


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
    # Test for PRESENCE, not truthiness. An empty pile is a legitimate game
    # state -- on turn 1 the discard pile is [] -- and `not state.get(k)`
    # treated that as "the mod does not send this field", so the planner
    # refused to plan on every opening turn of every fight. Only `deck` must
    # additionally be non-empty, since a real run always has cards.
    missing = [k for k in REQUIRED_FOR_PLANNING if k not in state]
    if "deck" in state and not state.get("deck"):
        missing.append("deck (present but empty)")
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
    from sts2_env.core.enums import CardId

    combat_block = state.get("combat_state") or state
    player = combat_block.get("player") or {}

    def _cards(key: str) -> list:
        """Materialise wire card entries into CardInstances.

        The wire sends the card id as a STRING (``card.Id.Entry``), but
        create_card takes a CardId ENUM -- passing the string raised
        AttributeError on every card, so the deck came back empty and
        reconstruction silently returned None even though the payload was
        complete. That is why live play logged "planner ENGAGED" and then
        never planned.
        """
        out, unknown = [], []
        for c in state.get(key, []) or []:
            raw = c.get("id") or c.get("card_id")
            if not raw:
                continue
            enum_id = _to_card_id(str(raw))
            if enum_id is None:
                unknown.append(str(raw))
                continue
            try:
                out.append(create_card(enum_id, upgraded=bool(c.get("upgraded"))))
            except Exception:
                unknown.append(str(raw))
        if unknown:
            logger.warning("%s: %d/%d card ids unrecognised (e.g. %s)",
                           key, len(unknown), len(unknown) + len(out),
                           ", ".join(sorted(set(unknown))[:4]))
        return out

    deck = _cards("deck")
    if not deck:
        return None

    combat = CombatState(
        player_hp=int(player.get("hp", state.get("hp", 1))),
        player_max_hp=int(player.get("max_hp", state.get("max_hp", 1))),
        deck=deck,
        # Any seed: pile ORDER is imposed explicitly below, so the RNG only
        # matters for effects that draw randomly mid-fight.
        rng_seed=int(state.get("round", 0)) or 1,
        relics=list(state.get("relics", []) or []),
        gold=int(state.get("gold", 0) or 0),
        character_id=_character_from(state),
        ascension_level=int(state.get("ascension_level", 0) or 0),
        potions=_rebuild_potions(state),
    )

    # --- enemies -----------------------------------------------------------
    # Without this the reconstruction had NO enemies, so "all enemies dead"
    # was trivially true: the planner reported LETHAL every turn, planned a
    # single END TURN, dealt 0 damage, and the real player bled out.
    from sts2_env.core.rng import Rng

    factories = _monster_factories()
    wire_enemies = [e for e in (state.get("enemies") or [])
                    if e.get("is_alive", True) and int(e.get("hp", 0) or 0) > 0]
    if not wire_enemies:
        return None
    built = 0
    for spec in wire_enemies:
        raw_mid = str(spec.get("id") or spec.get("monster_id") or "")
        mid = _normalize_monster_id(raw_mid, factories)
        fn = factories.get(mid) if mid else None
        if fn is None:
            logger.warning("unknown monster id %r; cannot plan this fight", raw_mid)
            return None
        try:
            creature, ai = fn(Rng(int(state.get("round", 1)) or 1),
                              int(state.get("ascension_level", 0) or 0))
        except TypeError:
            creature, ai = fn(Rng(int(state.get("round", 1)) or 1))
        except Exception:
            return None
        creature.max_hp = int(spec.get("max_hp", creature.max_hp) or creature.max_hp)
        creature.current_hp = int(spec.get("hp", creature.current_hp) or 0)
        creature.block = int(spec.get("block", 0) or 0)
        _restore_ai_state(ai, spec)
        combat.add_enemy(creature, ai)
        built += 1
    if not built:
        return None

    combat.start_combat()

    # --- piles: impose the REAL contents over whatever start_combat dealt ---
    # start_combat shuffles and draws; the live game has a specific hand and
    # a specific draw order, and planning against a different one would be
    # planning a different fight.
    st = combat.combat_player_states[0]
    hand = _cards("hand")
    draw = _cards("draw_pile")
    disc = _cards("discard_pile")
    exh = _cards("exhaust_pile")
    if hand or draw:
        st.hand[:] = hand
        st.draw[:] = draw
        st.discard[:] = disc
        st.exhaust[:] = exh
    player_block = int(player.get("block", 0) or 0)
    combat.primary_player.block = player_block
    energy = player.get("energy")
    if energy is not None:
        try:
            st.energy = int(energy)
        except Exception:
            pass
    return combat
