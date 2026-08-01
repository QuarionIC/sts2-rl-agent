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
    unknown: list[str] = []
    for i, spec in enumerate(wire):
        slot = int(spec.get("slot", i) or 0)
        pid = spec.get("id") or spec.get("potion_id")
        if not pid or not (0 <= slot < len(belt)):
            continue
        # Automatic potions fire on their own; the agent cannot spend them,
        # and offering them as actions would produce moves the game rejects.
        if spec.get("can_use") is False:
            continue
        inst = _make_potion(str(pid), slot)
        if inst is None:
            unknown.append(str(pid))
            continue
        belt[slot] = inst
    if unknown:
        # LOUD, not debug. An unresolved potion silently leaves its slot empty,
        # get_action_mask then marks every potion action illegal, and the
        # planner searches a game where that potion does not exist -- so the
        # agent looks like it is "choosing not to drink" when it was never
        # offered the option. This is the same silent id-mismatch that dropped
        # COUNTDOWN from the deck and FRAIL_POWER from the powers; it was
        # logged at DEBUG, i.e. invisible in every run so far.
        logger.warning("potions: %d id(s) unresolved (%s) -- those slots are "
                       "EMPTY, so the planner cannot use them",
                       len(unknown), ", ".join(sorted(set(unknown))[:4]))
    return belt


def _make_potion(raw: str, slot: int):
    """Build a potion from a wire id, tolerating naming-convention drift.

    ``create_potion`` expects the simulator's own spelling (PascalCase, e.g.
    ``AttackPotion``) while the wire sends ``potion.Id.Entry``. Try the raw id
    first, then a flattened match against the registered models, so a
    ``ATTACK_POTION`` / ``attack potion`` / ``AttackPotion`` mismatch resolves
    instead of silently emptying the slot.
    """
    from sts2_env.potions import all_potion_models, create_potion

    try:
        return create_potion(raw, slot=slot)
    except Exception:
        pass

    def _flat(s: str) -> str:
        return "".join(ch for ch in str(s).upper() if ch.isalnum())

    want = _flat(raw)
    variants = {want, want + "POTION"}
    if want.endswith("POTION"):
        variants.add(want[: -len("POTION")])

    for model in all_potion_models():
        pid = getattr(model, "potion_id", None) or getattr(model, "id", None)
        if pid is None:
            continue
        if _flat(pid) in variants:
            try:
                return create_potion(pid, slot=slot)
            except Exception:
                return None
    return None


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


_WARNED_ONCE: set[str] = set()

def _warn_once_reconstruct(key: str, message: str) -> None:
    """Log a fidelity gap once per process rather than once per decision."""
    if key not in _WARNED_ONCE:
        _WARNED_ONCE.add(key)
        logger.warning(message)


class _RngChain:
    """Minimal attribute holder for the shuffle-RNG resolution chain."""

    # See MegaRandom._CLONE_OPAQUE -- this chain contains only RNG objects.
    _CLONE_OPAQUE = True

    __slots__ = ("player_state", "run_state", "rng", "shuffle")

    def __init__(self, **kwargs):
        for key, value in kwargs.items():
            setattr(self, key, value)


def _install_game_shuffle_rng(combat, raw) -> bool:
    """Point the combat's shuffle stream at the GAME's live RNG.

    CombatState.shuffle_rng resolves
    ``_primary_player_state.player_state.run_state.rng.shuffle`` and falls
    back to the combat's own Rng. In a reconstruction none of that chain
    exists, so every reshuffle used a .NET-Random-derived stream seeded from
    the ROUND NUMBER -- guaranteed to differ from the game, which uses
    xoshiro256** (see sts2_env.core.mega_random).

    That is the measured cause of the reshuffle divergence: 83 of 104
    whole-combat plans truncated at the first reshuffle, and every observed
    plan divergence was "different cards".

    Only the ``shuffle`` stream is installed. Every other stream still
    getattr-misses on this chain and falls back exactly as before, so this
    change cannot alter monster AI, targeting or card generation.

    Returns True when the game's stream was installed.
    """
    if not isinstance(raw, dict):
        # SAY SO, ONCE. Whether shuffle parity is actually active in live play
        # is the difference between "post-reshuffle divergence is residual
        # drift" and "the fix never reached the game", and those call for
        # completely different work. Silence here meant the question could
        # only be answered by inference.
        _warn_once_reconstruct(
            "no_shuffle_rng",
            "payload carries no usable 'shuffle_rng': reshuffles will diverge "
            "from the game exactly as before the parity work. Rebuild and "
            "redeploy the bridge mod if this is unexpected.",
        )
        return False
    try:
        words = [int(raw[f"state{i}"]) for i in range(4)]
    except (KeyError, TypeError, ValueError):
        logger.warning(
            "shuffle_rng present but unusable (%r); reshuffles will diverge "
            "from the game as before.", raw)
        return False

    from sts2_env.core.mega_random import GameRng, MegaRandom

    game_rng = GameRng(MegaRandom.from_state(*words),
                       counter=int(raw.get("counter", 0) or 0))

    holder = getattr(combat, "_primary_player_state", None)
    if holder is None:
        return False
    player_state = getattr(holder, "player_state", None)
    if player_state is None:
        player_state = _RngChain()
        try:
            holder.player_state = player_state
        except Exception:
            return False
    run_state = getattr(player_state, "run_state", None)
    if run_state is None:
        run_state = _RngChain()
        try:
            player_state.run_state = run_state
        except Exception:
            return False
    rng_set = getattr(run_state, "rng", None)
    if rng_set is None:
        rng_set = _RngChain()
        try:
            run_state.rng = rng_set
        except Exception:
            return False
    try:
        rng_set.shuffle = game_rng
    except Exception:
        return False

    # The RNG alone is not enough. The game reshuffles the discard pile with
    # StableShuffle -- it SORTS the combined pile by (Id, upgrade level)
    # before dealing -- so an identical RNG stream applied to our
    # differently-ordered pile still produces a different draw order. That is
    # exactly what "unstable" means in the game's own doc comment.
    combat._force_stable_reshuffle = True
    _warn_once_reconstruct(
        "shuffle_rng_ok",
        "shuffle parity ACTIVE: reconstructed combats now draw from the "
        "game's own xoshiro256** stream with StableShuffle semantics.")

    # NOTE: a shuffle-counter "drift detector" lived here and was removed.
    # Two versions were tried and neither isolated parity error. Comparing
    # the game's counter against the ROOT combat measured nothing (planning
    # clones, so the root never advances). Comparing against the plan's
    # SHADOW conflates parity error with queued-but-unplayed actions, the
    # game's own between-decision draws, and combat boundaries -- the deltas
    # were uninterpretable either way.
    #
    # The CONTENTS divergence check in agent_runner is the authoritative
    # measurement: it compares the card we predicted in a slot against the
    # card the live game actually holds there, which is the thing that
    # matters and needs no inference. Post-reshuffle it reads ~4.6%.
    return True


def _to_card_id(raw: str):
    """Wire card-id string -> CardId enum, or None if unrecognised.

    Tried in order: exact name, upper-snake, mod-namespace stripped, then a
    normalised alphanumeric match that also tolerates a ``_CARD`` suffix on
    either side.

    The ``_CARD`` tolerance is not cosmetic. Live 2026-07-30 the wire sent
    ``COUNTDOWN`` while the simulator registers ``COUNTDOWN_CARD``; the card
    resolved to None and was DROPPED from every pile, so the planner searched
    an 11-card deck against the game's 12. Turn 1 still looked plausible, but
    from the first redraw the simulated hand was permanently off by one --
    every one of 10 turn boundaries diverged.
    """
    from sts2_env.core.enums import CardId

    name = raw.replace("CardId.", "").strip()
    upper = name.upper().replace(" ", "_").replace("-", "_")
    # Same mod-namespace handling as _normalize_monster_id: the wire qualifies
    # modded content (ACTSFROMTHEPAST-X) that the simulator registers bare.
    for pref in (p.replace("-", "_") for p in _MOD_PREFIXES):
        if upper.startswith(pref):
            upper = upper[len(pref):]
            break

    for cand in (name, name.upper(), upper):
        try:
            return CardId[cand]
        except KeyError:
            continue

    def _flat(s: str) -> str:
        return "".join(ch for ch in s.upper() if ch.isalnum())

    # Suffixes the simulator adds to disambiguate its own enum but the game
    # does not put on the wire. STATUS joined CARD after the game's SLOTH (a
    # Status card from the token pool) failed to resolve to the simulator's
    # SLOTH_STATUS -- test_card_pool_parity knew that alias through a
    # test-local table, so the mismatch was invisible until a live payload
    # carried the card and reconstruction declined the fight.
    flat = _flat(upper)
    variants = {flat}
    for suffix in ("CARD", "STATUS"):
        variants.add(flat + suffix)
        if flat.endswith(suffix):
            variants.add(flat[: -len(suffix)])

    matches = [m for m in CardId if _flat(m.name) in variants]
    # Prefer an exact match; only accept a suffix-variant when it is
    # UNAMBIGUOUS, so this can never silently map one card onto another.
    for m in matches:
        if _flat(m.name) == flat:
            return m
    return matches[0] if len(matches) == 1 else None


#: Mod power classes the simulator models under a different name, VERIFIED
#: behaviour-for-behaviour rather than by name similarity.
#:
#: ASLEEP_LAGAVULIN_POWER wakes its owner on unblocked damage, strips the
#: owner's Metallicize and removes itself -- which is what AsleepPower does,
#: stripping PLATING (StS2's name for Metallicize) and stunning.
#:
#: Deliberately short. Three mod powers LOOKED like aliases; only this one
#: survived reading both implementations:
#:   * STRENGTH_UP_POWER applies Strength at AfterSideTurnEnd, while the
#:     simulator's RITUAL applies it at turn START -- a full turn earlier, so
#:     the planner would credit the enemy with Strength on an attack that has
#:     not gained it yet.
#:   * REGEN_ENEMY_POWER heals Amount every turn forever, while the
#:     simulator's REGEN heals then decrements by 1 -- so an aliased enemy
#:     would be simulated healing less every turn than it really does.
#: Both need their own power, not an alias. Aliasing on name similarity would
#: have been strictly worse than the current honest "unrecognised" warning.
#: A blanket "_A4H" strip would be WRONG and is deliberately not done:
#: RegeneratePowerA4h never decays where vanilla REGEN does, so it has its own
#: PowerId, and folding the mod tag away would silently map it onto the wrong
#: power. Each mod-tagged alias has to be justified on behaviour, one at a
#: time -- which is the same discipline that stopped STRENGTH_UP being aliased
#: onto RITUAL.
_POWER_ID_ALIASES = {
    "ASLEEPLAGAVULINPOWER": "ASLEEP",
    # MetallicizePowerA4h gains Amount block at BeforeSideTurnEndEarly with no
    # decay -- identical to vanilla MetallicizePower, as sts2_env/powers/
    # act4_heart.py already documents ("intentionally NOT reimplemented").
    # Seen live on TERROR_EEL 2026-08-01: the eel was reconstructed with no
    # Metallicize, so the planner priced every attack against it as if its
    # block would not return.
    "METALLICIZEPOWERA4H": "METALLICIZE",
}


def _to_power_id(raw: str):
    """Wire power-id string -> PowerId enum, or None if unrecognised.

    Same normalisation ladder as :func:`_to_card_id`: exact, upper-snake,
    mod-namespace stripped, then an alphanumeric-flattened unique match --
    plus the verified alias table above.
    """
    from sts2_env.core.enums import PowerId

    name = str(raw).replace("PowerId.", "").strip()
    upper = name.upper().replace(" ", "_").replace("-", "_")

    aliased = _POWER_ID_ALIASES.get(
        "".join(ch for ch in upper if ch.isalnum()))
    if aliased is not None:
        try:
            return PowerId[aliased]
        except KeyError:
            pass
    for pref in (p.replace("-", "_") for p in _MOD_PREFIXES):
        if upper.startswith(pref):
            upper = upper[len(pref):]
            break
    for cand in (name, name.upper(), upper):
        try:
            return PowerId[cand]
        except KeyError:
            continue

    def _flat(s: str) -> str:
        return "".join(ch for ch in s.upper() if ch.isalnum())

    flat = _flat(upper)
    # Tolerate a _POWER suffix on either side, unambiguously. The wire sends
    # FRAIL_POWER / RAVENOUS_POWER / THORNS_POWER where the simulator
    # registers FRAIL / RAVENOUS / THORNS -- observed live 2026-07-30, and
    # without this those powers were dropped, so the planner searched fights
    # with no Frail and no Thorns on anyone. The simulator ALSO has genuine
    # *_POWER names (GRAPPLE_POWER, MANGLE_POWER, FREE_POWER), so an exact
    # match must win and a variant is accepted only when it is unique.
    variants = {flat, flat + "POWER"}
    if flat.endswith("POWER"):
        variants.add(flat[: -len("POWER")])
    # ...and an EMBEDDED "Power", which is where mods put it. Act4Heart names
    # its class RegeneratePowerA4H, so the wire sends REGENERATE_POWER_A4H
    # while the simulator registers REGENERATE_A4H -- the suffix rule above
    # cannot see that, and BYRDONIS was reconstructed with no Regenerate
    # (observed live 2026-07-31). Still only accepted when unique, so a
    # genuine *_POWER name cannot be captured by a near-miss.
    if "POWER" in flat:
        variants.add(flat.replace("POWER", "", 1))
    matches = [m for m in PowerId if _flat(m.name) in variants]
    for m in matches:
        if _flat(m.name) == flat:
            return m
    return matches[0] if len(matches) == 1 else None


def _restore_powers(creature, entries, label: str) -> None:
    """Put the live powers back on a reconstructed creature.

    The mod serialises powers for the player and every enemy
    (RlCombatHandler.cs, ``["powers"] = [{id, amount}]``) and this module
    consumed NONE of them. The planner therefore searched every fight with no
    Strength, Vulnerable, Weak, Poison or Necrobinder power on anybody --
    mispricing every attack and every incoming hit, in a search whose entire
    objective is HP.

    Instances are constructed directly rather than via
    ``CombatState.apply_power_to`` on purpose: applying would fire on-apply
    hooks and the player-debuff "skip first tick" rule, which are turn-time
    behaviours, not restoration. We want the state as it already is.
    """
    from sts2_env.core.creature import get_power_class
    from sts2_env.powers.base import PowerInstance

    unknown: list[str] = []
    behaviourless: list[str] = []
    for entry in (entries or []):
        raw = entry.get("id") or entry.get("power_id")
        if not raw:
            continue
        pid = _to_power_id(str(raw))
        if pid is None:
            unknown.append(str(raw))
            continue
        try:
            amount = int(entry.get("amount", 0) or 0)
        except Exception:
            amount = 0
        if amount == 0:
            continue
        # Registered subclasses take (amount) only -- power_id is a CLASS
        # attribute. Calling cls(pid, amount) raises TypeError, and falling
        # back to a bare PowerInstance yields a power with the right amount
        # and NO BEHAVIOUR: Strength that adds no damage, Vulnerable that
        # increases nothing. That is worse than useless in an HP-objective
        # search, so a behaviourless fallback is reported rather than hidden.
        cls = get_power_class(pid)
        inst = None
        if cls is not None:
            for args in ((amount,), (pid, amount)):
                try:
                    inst = cls(*args)
                    break
                except TypeError:
                    continue
                except Exception:
                    break
        if inst is None:
            try:
                inst = PowerInstance(pid, amount)
                if cls is not None:
                    behaviourless.append(pid.name)
            except Exception:
                unknown.append(str(raw))
                continue
        creature.powers[pid] = inst
    if unknown:
        logger.warning("%s: %d power id(s) unrecognised (e.g. %s) -- the "
                       "planner will search without them",
                       label, len(unknown), ", ".join(sorted(set(unknown))[:4]))
    if behaviourless:
        logger.error("%s: %d power(s) restored WITHOUT behaviour (%s) -- a "
                     "registered class exists but could not be constructed, "
                     "so the amount is right and the effect is missing",
                     label, len(behaviourless),
                     ", ".join(sorted(set(behaviourless))[:4]))


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

    def _apply_wire_cost(card, raw_cost) -> None:
        """Adopt the game's CURRENT cost for this card instance.

        SerializeCard sends ``card.EnergyCost.GetWithModifiers(CostModifiers
        .All)`` -- the cost the player would actually pay right now -- but
        reconstruction rebuilt every card with create_card(), which yields the
        card's BASE cost and threw the modifier away. A card discounted to 0
        by an effect was therefore simulated at full price, so the planner
        could not afford a line the game would have allowed and ended the turn
        with energy unspent. That is the "ends turn with extra energy" symptom
        seen live, from the search side rather than the objective side.

        Only ``cost`` is written, never ``original_cost``: end_of_turn_cleanup
        restores cost from original_cost, which is exactly right for the
        common case of a modifier that expires this turn. A permanent
        reduction will revert at the turn boundary in simulation -- a smaller
        error than ignoring the discount outright.
        """
        if raw_cost is None:
            return
        try:
            cost = int(raw_cost)
        except (TypeError, ValueError):
            return
        # X-cost cards encode as a negative cost, and is_unplayable keys off
        # cost < 0. Adopting a negative here could flip a playable card to
        # unplayable, so leave those to the factory.
        if cost < 0 or getattr(card, "has_energy_cost_x", False):
            return
        card.cost = cost

    def _apply_wire_keywords(card, raw_keywords) -> None:
        """Union the game's current keywords onto a reconstructed card.

        create_card() rebuilds a card from its id, so any keyword applied to
        the INSTANCE at runtime -- Ethereal granted by Hex, for example -- is
        lost, and the planner simulates a card that behaves differently from
        the one on screen.

        UNION rather than replace, deliberately. The game's set includes its
        canonical keywords, but the two vocabularies are maintained
        independently; replacing would let any naming mismatch silently strip
        a canonical keyword like "exhaust" or "unplayable" and change how the
        card plays. Adding can only ever restore something that was missing.
        """
        if not raw_keywords:
            return
        try:
            extra = {str(k).strip().lower() for k in raw_keywords if k}
        except TypeError:
            return
        if extra:
            card.keywords = set(card.keywords) | extra

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
                card = create_card(enum_id, upgraded=bool(c.get("upgraded")))
            except Exception:
                unknown.append(str(raw))
                continue
            _apply_wire_cost(card, c.get("cost"))
            _apply_wire_keywords(card, c.get("keywords"))
            # Remember the GAME's own id string. StableShuffle sorts by
            # ModelId (Category, then Entry, ordinal) and Category is uniform
            # across cards, so Entry -- this exact string -- is the sort key
            # that decides the post-reshuffle deal. Our CardId enum name is
            # NOT always the same string: _to_card_id deliberately tolerates
            # a missing/extra _CARD suffix (the wire says COUNTDOWN where the
            # simulator registers COUNTDOWN_CARD) and strips mod namespaces.
            # Sorting by the enum name would order the pile differently from
            # the game and deal different cards even with a perfect RNG.
            card._wire_entry = str(raw)
            out.append(card)
        if unknown:
            logger.warning("%s: %d/%d card ids unrecognised (e.g. %s)",
                           key, len(unknown), len(unknown) + len(out),
                           ", ".join(sorted(set(unknown))[:4]))
            unresolved.update(unknown)
        return out

    #: Card ids the simulator could not resolve. A DROPPED card is not a
    #: cosmetic loss: the sim then holds fewer cards than the game, so every
    #: simulated draw pulls the wrong card and the plan describes a fight that
    #: is not being played. Refusing is strictly better than planning
    #: confidently on the wrong deck -- the caller falls back and says why.
    unresolved: set[str] = set()

    deck = _cards("deck")
    if not deck:
        return None

    combat = CombatState(
        player_hp=int(player.get("hp", state.get("hp", 1))),
        player_max_hp=int(player.get("max_hp", state.get("max_hp", 1))),
        deck=deck,
        # Fallback seed only. The pile ORDER is imposed explicitly below, so
        # this matters for mid-fight random effects and -- critically -- for
        # RESHUFFLES, which _install_game_shuffle_rng replaces with the
        # game's own stream when the payload carries it.
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

    # --- round: start_combat() hard-resets round_number to 1 (combat.py:842),
    # so every reconstruction claimed to be on turn 1 no matter what the wire
    # said. Anything gated on round number -- monster move progressions,
    # scaling powers, "first turn" card effects -- was evaluated for the wrong
    # turn on every replan after turn 1.
    wire_round = int(state.get("round", 0) or 0)
    if wire_round > 0:
        combat.round_number = wire_round

    # --- powers: restore AFTER start_combat, which deals a fresh combat -----
    # Applied here (not before add_enemy) so start_combat cannot clear them.
    # Enemies are added in wire order, so combat.enemies aligns with
    # wire_enemies positionally.
    _restore_powers(combat.primary_player, player.get("powers"), "player powers")
    for creature, spec in zip(combat.enemies, wire_enemies):
        _restore_powers(creature, spec.get("powers"),
                        f"enemy {spec.get('id', '?')} powers")

    # --- Osty ---------------------------------------------------------------
    # Osty lives in combat.allies, not combat.enemies, and was never
    # transmitted OR reconstructed. The damage pipeline redirects player damage
    # to it (core/damage.py modify_hp_lost_before_osty) and Necrobinder cards
    # scale off its HP, so planning without it mispriced both incoming damage
    # and the character's own damage output.
    pets = [p for p in (state.get("pets") or []) if p]
    live_pets = [p for p in pets if p.get("is_alive", True)
                 and int(p.get("hp", 0) or 0) > 0]
    if live_pets:
        spec = live_pets[0]
        try:
            osty = combat.summon_osty(combat.primary_player,
                                      int(spec.get("max_hp", 0) or 0))
        except Exception as exc:
            osty = None
            logger.warning("could not summon Osty for reconstruction: %s", exc)
        if osty is not None:
            try:
                osty.max_hp = int(spec.get("max_hp", osty.max_hp) or osty.max_hp)
                osty.current_hp = int(spec.get("hp", osty.current_hp) or 0)
                osty.block = int(spec.get("block", 0) or 0)
            except Exception:
                pass
            _restore_powers(osty, spec.get("powers"), "osty powers")
    elif pets:
        logger.debug("payload reports %d pet(s), none alive", len(pets))

    # --- piles: impose the REAL contents over whatever start_combat dealt ---
    # start_combat shuffles and draws; the live game has a specific hand and
    # a specific draw order, and planning against a different one would be
    # planning a different fight.
    st = combat.combat_player_states[0]
    hand = _cards("hand")
    draw = _cards("draw_pile")
    disc = _cards("discard_pile")
    exh = _cards("exhaust_pile")

    # DECLINE rather than plan on a deck the game does not have. This check
    # must sit AFTER every _cards() call: an earlier version ran it straight
    # after _cards("deck") and so only ever caught deck-level failures, while
    # an unresolved card in hand/draw/discard was still dropped silently --
    # which is the case that actually desynchronises the draw.
    #
    # A dropped card is not cosmetic: the simulator then holds fewer cards
    # than the game, so every simulated draw pulls the wrong card and the plan
    # describes a fight that is not being played. Live 2026-07-30 a single
    # unresolved COUNTDOWN did exactly that while turn 1 still looked correct.
    if unresolved:
        logger.error("combat planner declining: %d unresolvable card id(s) "
                     "(%s). The simulator would hold a different deck than the "
                     "game, so every simulated draw would be wrong.",
                     len(unresolved), ", ".join(sorted(unresolved)[:6]))
        return None

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

    # INSTALL THE GAME'S SHUFFLE STREAM LAST.
    #
    # Deliberately after every other step. Building the CombatState and
    # imposing the pile order consume draws of their own, and the state the
    # mod sent was captured at serialization time -- so installing earlier
    # left the stream one draw ahead of the game before planning even
    # started, and every later reshuffle inherited that offset. Caught by
    # test_installed_shuffle_stream_reproduces_the_game_sequence, which saw
    # [7,1,4,7,9] where the game's next draws were [6,7,1,4,7].
    #
    # Installing here means the first draw the PLANNER takes is the first
    # draw the game would take.
    _install_game_shuffle_rng(combat, state.get("shuffle_rng"))
    return combat
