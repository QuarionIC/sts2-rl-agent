"""TheCity (Act-2-slot legacy act) monsters -- "Acts from the Past" mod.

Recreates Slay the Spire 1's Act 2 ("The City") monster roster as an
alternate for the vanilla Act-2 slot. Standalone content, same status as
``sts2_env/monsters/exordium.py``: the run's act-slot-candidate extension
point exists (``sts2_env/map/acts.py``) but TheCity is intentionally NOT
registered into it yet -- see ``sts2_env/encounters/thecity.py`` for the
encounter pools and the scope note there.

All HP ranges, damage values, and state machines are ported from the
decompiled "Acts from the Past" mod source
(``decompiled_mods/ActsFromThePast/ActsFromThePast/*.cs``) and cross checked
against the task spec. Ascension convention (matching Exordium and every
other act in this codebase): HP/toughness scales at Ascension 8,
damage/debuff amounts/status counts scale at Ascension 9.

Uses the existing ``MoveState``/``RandomBranchState``/``ConditionalBranchState``
state-machine framework from ``sts2_env/monsters/state_machine.py`` exactly,
plus the ``BranchState`` (rng+history driven custom chooser) helper class
that ``sts2_env/monsters/exordium.py`` introduced for "reroll on repeat"
branching -- imported from there rather than duplicated, since it has no
Exordium-specific logic.

Monsters/moves whose reference source (``ShelledParasite.cs``,
``BookOfStabbing.cs``, ``Chosen.cs``, ``Byrd.cs``, ``SnakePlant.cs``,
``Collector.cs``, ``BronzeAutomaton.cs``, ``BronzeOrb.cs``, ``Champ.cs``,
``GremlinLeader.cs``, ``Mugger.cs``, ``SphericGuardian.cs``) needed powers
not already present anywhere in this simulator (vanilla or Exordium's own
additions) got new ``PowerId``/power-class pairs appended to
``sts2_env/core/enums.py`` and ``sts2_env/powers/monster.py``: ``FLIGHT``
(Byrd), ``HEX_ORIGINAL`` (Chosen -- distinct from this simulator's existing,
mechanically unrelated vanilla ``HexPower``), ``PLATED_ARMOR``
(ShelledParasite -- distinct from vanilla ``CurlUpPower``, which is a
one-shot trigger and does not fit ShelledParasite's persistent
regenerate-every-turn-until-broken shield), ``MALLEABLE`` (SnakePlant),
``STASIS`` (BronzeOrb), ``MINION_MASTER`` (Collector/BronzeAutomaton -- kill
minions on the master's own death) and ``GREMLIN_LEADER_PRESENCE``
(GremlinLeader -- un-minion allies on its own death). Everything else
(``CONFUSED``, ``ARTIFACT``, ``BARRICADE``, ``METALLICIZE``,
``PAINFUL_STABS``, ``MINION``) already exists as a vanilla power and is
reused directly.

Mugger isn't in this task's explicit "new monsters" spec list (only
referenced by the ``TwoThievesWeak`` encounter, which needs it alongside
Exordium's own ``Looter``), so it's implemented directly against
``Mugger.cs`` to the same standard as everything else here.
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import CardRarity, MoveRepeatType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import INT_MAX, Rng
from sts2_env.cards.status import make_dazed, make_wound
from sts2_env.monsters.intents import (
    Intent, IntentType, attack_intent, multi_attack_intent,
    buff_intent, debuff_intent, status_intent, defend_intent,
)
from sts2_env.monsters.state_machine import (
    ConditionalBranchState, MonsterAI, MonsterState, MoveState, RandomBranchState,
)
from sts2_env.monsters.block import gain_move_block
from sts2_env.monsters.targets import (
    apply_power_to_living_player_targets,
    living_player_targets,
)
from sts2_env.monsters.exordium import (
    BranchState,
    create_cultist,
    create_fungi_beast,
    create_gremlin_fat,
    create_gremlin_mad,
    create_gremlin_shield,
    create_gremlin_sneaky,
    create_gremlin_wizard,
    create_looter,
    create_sentry,
    create_slaver_blue,
    create_slaver_red,
    _LooterThieveryPower,
)
from sts2_env.powers.monster import (
    FlightPower,
    GremlinLeaderPresencePower,
    HexOriginalPower,
    MalleablePower,
    MinionMasterPower,
    PlatedArmorPower,
    StasisPower,
)

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ========================================================================
# Helpers (mirrors the per-act convention used by act1.py/act2.py/exordium.py)
# ========================================================================

TOUGH_ENEMIES_ASCENSION_LEVEL = 8
DEADLY_ENEMIES_ASCENSION_LEVEL = 9


def _ascension_value(ascension_level: int, threshold: int, ascension_value: int, base_value: int) -> int:
    return ascension_value if ascension_level >= threshold else base_value


def _combat_ascension_level(combat: "CombatState") -> int:
    return getattr(combat, "ascension_level", 0)


def _deal_damage_to_player(combat: "CombatState", creature: Creature, base_dmg: int, hits: int = 1) -> None:
    for _ in range(hits):
        targets = living_player_targets(combat)
        if not targets:
            break
        for target in targets:
            dmg = calculate_damage(base_dmg, creature, target, ValueProp.MOVE, combat)
            apply_damage(target, dmg, ValueProp.MOVE, combat, creature)
        combat._check_combat_end()  # noqa: SLF001
        if combat.is_over:
            break


def _gain_block(creature: Creature, amount: int, combat: "CombatState") -> None:
    gain_move_block(creature, amount, combat)


def _other_living_enemies(combat: "CombatState", creature: Creature) -> list[Creature]:
    return [e for e in combat.enemies if e.is_alive and e is not creature]


# ========================================================================
# NORMAL / WEAK MONSTERS
# ========================================================================

# ---- Byrd (HP 25-31 / 26-33 asc) ----

BYRD_MONSTER_ID = "THECITY_BYRD"
BYRD_BASE_MIN_HP = 25
BYRD_BASE_MAX_HP = 31
BYRD_TOUGH_MIN_HP = 26
BYRD_TOUGH_MAX_HP = 33
BYRD_PECK_DAMAGE = 1
BYRD_BASE_PECK_COUNT = 5
BYRD_DEADLY_PECK_COUNT = 6
BYRD_BASE_SWOOP_DAMAGE = 12
BYRD_DEADLY_SWOOP_DAMAGE = 14
BYRD_CAW_STRENGTH = 1
BYRD_HEADBUTT_DAMAGE = 3
BYRD_BASE_FLIGHT_PER_PLAYER = 3
BYRD_TOUGH_FLIGHT_PER_PLAYER = 4
BYRD_PECK_MOVE = "PECK"
BYRD_SWOOP_MOVE = "SWOOP"
BYRD_CAW_MOVE = "CAW"
BYRD_GO_AIRBORNE_MOVE = "GO_AIRBORNE"
BYRD_HEADBUTT_MOVE = "HEADBUTT"
BYRD_FIRST_MOVE_BRANCH = "BYRD_FIRST_MOVE_BRANCH"
BYRD_FLYING_BRANCH = "BYRD_FLYING_BRANCH"


class _ByrdFlightPower(FlightPower):
    """Byrd-specific ``FlightPower``: when stacks fully deplete, Byrd falls
    (grounds itself and its next move is forced to HEADBUTT). See
    ``FlightPower`` in ``sts2_env/powers/monster.py`` for the generic
    damage-halving/decrement mechanic this subclasses.
    """

    def __init__(self, amount: int, on_depleted: Callable[[Creature, "CombatState"], None]):
        super().__init__(amount)
        self._on_depleted = on_depleted

    def after_damage_received(
        self, owner: Creature, target: Creature, dealer: Creature | None,
        damage: int, props: ValueProp, combat: "CombatState",
    ) -> None:
        super().after_damage_received(owner, target, dealer, damage, props, combat)
        if target is owner and self.power_id not in owner.powers:
            self._on_depleted(owner, combat)


def _byrd_flight_amount(ascension_level: int, combat: "CombatState") -> int:
    per_player = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BYRD_TOUGH_FLIGHT_PER_PLAYER, BYRD_BASE_FLIGHT_PER_PLAYER)
    num_players = len(getattr(combat, "combat_player_states", None) or []) if combat is not None else 1
    num_players = max(num_players, 1)
    return per_player + max(0, num_players - 1) * 2


def create_byrd(rng: Rng, ascension_level: int = 0, combat: "CombatState | None" = None) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BYRD_TOUGH_MIN_HP, BYRD_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BYRD_TOUGH_MAX_HP, BYRD_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=BYRD_MONSTER_ID)

    def _apply_flight(target_combat: "CombatState") -> None:
        amount = _byrd_flight_amount(ascension_level, target_combat)
        creature.powers[PowerId.FLIGHT] = _ByrdFlightPower(amount, on_depleted=_on_flight_broken)

    def _on_flight_broken(owner: Creature, target_combat: "CombatState") -> None:
        target_combat.set_enemy_state(owner, BYRD_HEADBUTT_MOVE)

    def peck(combat_: "CombatState") -> None:
        count = _ascension_value(_combat_ascension_level(combat_), DEADLY_ENEMIES_ASCENSION_LEVEL, BYRD_DEADLY_PECK_COUNT, BYRD_BASE_PECK_COUNT)
        _deal_damage_to_player(combat_, creature, BYRD_PECK_DAMAGE, hits=count)

    def swoop(combat_: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat_), DEADLY_ENEMIES_ASCENSION_LEVEL, BYRD_DEADLY_SWOOP_DAMAGE, BYRD_BASE_SWOOP_DAMAGE)
        _deal_damage_to_player(combat_, creature, dmg)

    def caw(combat_: "CombatState") -> None:
        creature.apply_power(PowerId.STRENGTH, BYRD_CAW_STRENGTH, applier=creature)

    def go_airborne(combat_: "CombatState") -> None:
        _apply_flight(combat_)

    def headbutt(combat_: "CombatState") -> None:
        _deal_damage_to_player(combat_, creature, BYRD_HEADBUTT_DAMAGE)

    def flying_chooser(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        if r < 50:
            if len(state_log) >= 2 and state_log[-1] == BYRD_PECK_MOVE and state_log[-2] == BYRD_PECK_MOVE:
                return BYRD_SWOOP_MOVE if rng_.next_float(100.0) < 40 else BYRD_CAW_MOVE
            return BYRD_PECK_MOVE
        if r < 70:
            if state_log and state_log[-1] == BYRD_SWOOP_MOVE:
                return BYRD_CAW_MOVE if rng_.next_float(100.0) < 37.5 else BYRD_PECK_MOVE
            return BYRD_SWOOP_MOVE
        if state_log and state_log[-1] == BYRD_CAW_MOVE:
            return BYRD_SWOOP_MOVE if rng_.next_float(100.0) < 28.57 else BYRD_PECK_MOVE
        return BYRD_CAW_MOVE

    peck_intent_count = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BYRD_DEADLY_PECK_COUNT, BYRD_BASE_PECK_COUNT)
    swoop_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BYRD_DEADLY_SWOOP_DAMAGE, BYRD_BASE_SWOOP_DAMAGE)

    first_move_branch = RandomBranchState(BYRD_FIRST_MOVE_BRANCH)
    first_move_branch.add_branch(BYRD_CAW_MOVE, weight=37.5)
    first_move_branch.add_branch(BYRD_PECK_MOVE, weight=62.5)

    states: dict[str, MonsterState] = {
        BYRD_PECK_MOVE: MoveState(BYRD_PECK_MOVE, peck, [multi_attack_intent(BYRD_PECK_DAMAGE, peck_intent_count)], follow_up_id=BYRD_FLYING_BRANCH),
        BYRD_SWOOP_MOVE: MoveState(BYRD_SWOOP_MOVE, swoop, [attack_intent(swoop_intent_damage)], follow_up_id=BYRD_FLYING_BRANCH),
        BYRD_CAW_MOVE: MoveState(BYRD_CAW_MOVE, caw, [buff_intent()], follow_up_id=BYRD_FLYING_BRANCH),
        BYRD_GO_AIRBORNE_MOVE: MoveState(BYRD_GO_AIRBORNE_MOVE, go_airborne, [buff_intent()], follow_up_id=BYRD_FLYING_BRANCH),
        BYRD_HEADBUTT_MOVE: MoveState(BYRD_HEADBUTT_MOVE, headbutt, [attack_intent(BYRD_HEADBUTT_DAMAGE)], follow_up_id=BYRD_GO_AIRBORNE_MOVE),
        BYRD_FLYING_BRANCH: BranchState(BYRD_FLYING_BRANCH, flying_chooser),
        BYRD_FIRST_MOVE_BRANCH: first_move_branch,
    }
    ai = MonsterAI(states, BYRD_FIRST_MOVE_BRANCH, rng)
    _apply_flight(combat)
    return creature, ai


# ---- Chosen (HP 95-99 / 98-103 asc) ----

CHOSEN_MONSTER_ID = "THECITY_CHOSEN"
CHOSEN_BASE_MIN_HP = 95
CHOSEN_BASE_MAX_HP = 99
CHOSEN_TOUGH_MIN_HP = 98
CHOSEN_TOUGH_MAX_HP = 103
CHOSEN_BASE_DEBILITATE_DAMAGE = 10
CHOSEN_DEADLY_DEBILITATE_DAMAGE = 12
CHOSEN_DEBILITATE_VULN = 2
CHOSEN_DRAIN_WEAK = 3
CHOSEN_DRAIN_STRENGTH = 3
CHOSEN_BASE_ZAP_DAMAGE = 18
CHOSEN_DEADLY_ZAP_DAMAGE = 21
CHOSEN_BASE_POKE_DAMAGE = 5
CHOSEN_DEADLY_POKE_DAMAGE = 6
CHOSEN_POKE_HITS = 2
CHOSEN_HEX_MOVE = "HEX"
CHOSEN_DEBILITATE_MOVE = "DEBILITATE"
CHOSEN_DRAIN_MOVE = "DRAIN"
CHOSEN_ZAP_MOVE = "ZAP"
CHOSEN_POKE_MOVE = "POKE"
CHOSEN_BRANCH = "CHOSEN_BRANCH"


def create_chosen(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CHOSEN_TOUGH_MIN_HP, CHOSEN_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CHOSEN_TOUGH_MAX_HP, CHOSEN_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=CHOSEN_MONSTER_ID)
    # HEX is forced as the very first move via ``initial_state_id`` below
    # (not resolved through ``chooser``), so this starts pre-marked used --
    # otherwise the branch's own "not used yet" check would force a SECOND
    # Hex the first time the branch actually runs (right after Hex's own
    # follow-up transition).
    used_hex = {"done": True}

    def hex_move(combat: "CombatState") -> None:
        for target in living_player_targets(combat):
            target.powers[PowerId.HEX_ORIGINAL] = target.powers.get(PowerId.HEX_ORIGINAL) or HexOriginalPower(1)
            target.powers[PowerId.HEX_ORIGINAL].applier = creature

    def debilitate(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_DEBILITATE_DAMAGE, CHOSEN_BASE_DEBILITATE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, CHOSEN_DEBILITATE_VULN, applier=creature)

    def drain(combat: "CombatState") -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, CHOSEN_DRAIN_WEAK, applier=creature)
        creature.apply_power(PowerId.STRENGTH, CHOSEN_DRAIN_STRENGTH, applier=creature)

    def zap(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_ZAP_DAMAGE, CHOSEN_BASE_ZAP_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def poke(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_POKE_DAMAGE, CHOSEN_BASE_POKE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=CHOSEN_POKE_HITS)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        if not used_hex["done"]:
            used_hex["done"] = True
            return CHOSEN_HEX_MOVE
        last = state_log[-1] if state_log else None
        if last != CHOSEN_DEBILITATE_MOVE and last != CHOSEN_DRAIN_MOVE:
            return CHOSEN_DEBILITATE_MOVE if rng_.next_float(100.0) < 50 else CHOSEN_DRAIN_MOVE
        return CHOSEN_ZAP_MOVE if rng_.next_float(100.0) < 40 else CHOSEN_POKE_MOVE

    debilitate_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_DEBILITATE_DAMAGE, CHOSEN_BASE_DEBILITATE_DAMAGE)
    zap_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_ZAP_DAMAGE, CHOSEN_BASE_ZAP_DAMAGE)
    poke_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CHOSEN_DEADLY_POKE_DAMAGE, CHOSEN_BASE_POKE_DAMAGE)
    states: dict[str, MonsterState] = {
        CHOSEN_HEX_MOVE: MoveState(CHOSEN_HEX_MOVE, hex_move, [debuff_intent()], follow_up_id=CHOSEN_BRANCH),
        CHOSEN_DEBILITATE_MOVE: MoveState(CHOSEN_DEBILITATE_MOVE, debilitate, [attack_intent(debilitate_intent_damage), debuff_intent()], follow_up_id=CHOSEN_BRANCH),
        CHOSEN_DRAIN_MOVE: MoveState(CHOSEN_DRAIN_MOVE, drain, [debuff_intent(), buff_intent()], follow_up_id=CHOSEN_BRANCH),
        CHOSEN_ZAP_MOVE: MoveState(CHOSEN_ZAP_MOVE, zap, [attack_intent(zap_intent_damage)], follow_up_id=CHOSEN_BRANCH),
        CHOSEN_POKE_MOVE: MoveState(CHOSEN_POKE_MOVE, poke, [multi_attack_intent(poke_intent_damage, CHOSEN_POKE_HITS)], follow_up_id=CHOSEN_BRANCH),
        CHOSEN_BRANCH: BranchState(CHOSEN_BRANCH, chooser),
    }
    return creature, MonsterAI(states, CHOSEN_HEX_MOVE, rng)


# ---- Centurion (HP 76-80 / 78-83 asc) ----

CENTURION_MONSTER_ID = "THECITY_CENTURION"
CENTURION_BASE_MIN_HP = 76
CENTURION_BASE_MAX_HP = 80
CENTURION_TOUGH_MIN_HP = 78
CENTURION_TOUGH_MAX_HP = 83
CENTURION_BASE_SLASH_DAMAGE = 12
CENTURION_DEADLY_SLASH_DAMAGE = 14
CENTURION_BASE_PROTECT_BLOCK = 15
CENTURION_TOUGH_PROTECT_BLOCK = 20
CENTURION_BASE_FURY_DAMAGE = 6
CENTURION_DEADLY_FURY_DAMAGE = 7
CENTURION_FURY_HITS = 3
CENTURION_SLASH_MOVE = "SLASH"
CENTURION_PROTECT_MOVE = "PROTECT"
CENTURION_FURY_MOVE = "FURY"
CENTURION_BRANCH = "CENTURION_BRANCH"


def create_centurion(
    rng: Rng, ascension_level: int = 0, combat: "CombatState | None" = None,
) -> tuple[Creature, MonsterAI]:
    """``combat`` is optional and only used to seed the very first move's
    branch resolution with real ally information -- ``MonsterAI.__init__``
    resolves the initial branch state immediately (before this creature has
    been added to any combat via ``combat.add_enemy``), so
    ``creature.combat_state`` isn't set yet at that point. Pass the
    in-progress ``CombatState`` (with any ally already added to it) from the
    encounter setup for a faithful first move; omitting it safely falls
    back to "no allies known yet" for the first resolution only."""
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CENTURION_TOUGH_MIN_HP, CENTURION_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CENTURION_TOUGH_MAX_HP, CENTURION_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=CENTURION_MONSTER_ID)

    def slash(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CENTURION_DEADLY_SLASH_DAMAGE, CENTURION_BASE_SLASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def protect(combat: "CombatState") -> None:
        block = _ascension_value(_combat_ascension_level(combat), TOUGH_ENEMIES_ASCENSION_LEVEL, CENTURION_TOUGH_PROTECT_BLOCK, CENTURION_BASE_PROTECT_BLOCK)
        others = _other_living_enemies(combat, creature)
        target = combat.rng.choice(others) if others else creature
        _gain_block(target, block, combat)

    def fury(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CENTURION_DEADLY_FURY_DAMAGE, CENTURION_BASE_FURY_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=CENTURION_FURY_HITS)

    def _has_allies(combat_: "CombatState | None") -> bool:
        if combat_ is None:
            return False
        return len(_other_living_enemies(combat_, creature)) > 0

    def chooser(state_log: list[str], rng_: Rng) -> str:
        combat_ = creature.combat_state or combat
        r = rng_.next_float(100.0)
        last_two_protect_or_fury = len(state_log) >= 2 and state_log[-1] in (CENTURION_PROTECT_MOVE, CENTURION_FURY_MOVE) and state_log[-2] in (CENTURION_PROTECT_MOVE, CENTURION_FURY_MOVE)
        if r >= 65 and not last_two_protect_or_fury:
            return CENTURION_PROTECT_MOVE if _has_allies(combat_) else CENTURION_FURY_MOVE
        last_two_slash = len(state_log) >= 2 and state_log[-1] == CENTURION_SLASH_MOVE and state_log[-2] == CENTURION_SLASH_MOVE
        if not last_two_slash:
            return CENTURION_SLASH_MOVE
        return CENTURION_PROTECT_MOVE if _has_allies(combat_) else CENTURION_FURY_MOVE

    slash_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CENTURION_DEADLY_SLASH_DAMAGE, CENTURION_BASE_SLASH_DAMAGE)
    fury_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CENTURION_DEADLY_FURY_DAMAGE, CENTURION_BASE_FURY_DAMAGE)
    states: dict[str, MonsterState] = {
        CENTURION_SLASH_MOVE: MoveState(CENTURION_SLASH_MOVE, slash, [attack_intent(slash_intent_damage)], follow_up_id=CENTURION_BRANCH),
        CENTURION_PROTECT_MOVE: MoveState(CENTURION_PROTECT_MOVE, protect, [defend_intent()], follow_up_id=CENTURION_BRANCH),
        CENTURION_FURY_MOVE: MoveState(CENTURION_FURY_MOVE, fury, [multi_attack_intent(fury_intent_damage, CENTURION_FURY_HITS)], follow_up_id=CENTURION_BRANCH),
        CENTURION_BRANCH: BranchState(CENTURION_BRANCH, chooser),
    }
    return creature, MonsterAI(states, CENTURION_BRANCH, rng)


# ---- Mystic (HP 48-56 / 50-58 asc) ----

MYSTIC_MONSTER_ID = "THECITY_MYSTIC"
MYSTIC_BASE_MIN_HP = 48
MYSTIC_BASE_MAX_HP = 56
MYSTIC_TOUGH_MIN_HP = 50
MYSTIC_TOUGH_MAX_HP = 58
MYSTIC_BASE_ATTACK_DAMAGE = 8
MYSTIC_DEADLY_ATTACK_DAMAGE = 9
MYSTIC_ATTACK_FRAIL = 2
MYSTIC_HEAL_PER_PLAYER = 20
MYSTIC_BASE_BUFF_STRENGTH = 3
MYSTIC_DEADLY_BUFF_STRENGTH = 4
MYSTIC_ATTACK_MOVE = "ATTACK"
MYSTIC_HEAL_MOVE = "HEAL"
MYSTIC_BUFF_MOVE = "BUFF"
MYSTIC_BRANCH = "MYSTIC_BRANCH"


def create_mystic(
    rng: Rng, ascension_level: int = 0, combat: "CombatState | None" = None,
) -> tuple[Creature, MonsterAI]:
    """See ``create_centurion`` for why ``combat`` is an optional parameter
    here (seeds the very first move's branch resolution before this
    creature is actually added to any combat)."""
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, MYSTIC_TOUGH_MIN_HP, MYSTIC_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, MYSTIC_TOUGH_MAX_HP, MYSTIC_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=MYSTIC_MONSTER_ID)

    def _num_players(combat_: "CombatState | None") -> int:
        if combat_ is None:
            return 1
        return max(1, len(getattr(combat_, "combat_player_states", None) or []))

    def attack(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MYSTIC_DEADLY_ATTACK_DAMAGE, MYSTIC_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, MYSTIC_ATTACK_FRAIL, applier=creature)

    def heal(combat: "CombatState") -> None:
        amount = MYSTIC_HEAL_PER_PLAYER * _num_players(combat)
        for ally in [creature] + _other_living_enemies(combat, creature):
            ally.heal(amount)

    def buff(combat: "CombatState") -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MYSTIC_DEADLY_BUFF_STRENGTH, MYSTIC_BASE_BUFF_STRENGTH)
        for ally in [creature] + _other_living_enemies(combat, creature):
            ally.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        combat_ = creature.combat_state or combat
        allies = [creature] + (_other_living_enemies(combat_, creature) if combat_ is not None else [])
        missing_total = sum(a.max_hp - a.current_hp for a in allies)
        threshold = MYSTIC_HEAL_PER_PLAYER * _num_players(combat_)
        last_two_heal = len(state_log) >= 2 and state_log[-1] == MYSTIC_HEAL_MOVE and state_log[-2] == MYSTIC_HEAL_MOVE
        if missing_total > threshold and not last_two_heal:
            return MYSTIC_HEAL_MOVE
        r = rng_.next_float(100.0)
        if r >= 40 and (not state_log or state_log[-1] != MYSTIC_ATTACK_MOVE):
            return MYSTIC_ATTACK_MOVE
        last_two_buff = len(state_log) >= 2 and state_log[-1] == MYSTIC_BUFF_MOVE and state_log[-2] == MYSTIC_BUFF_MOVE
        if not last_two_buff:
            return MYSTIC_BUFF_MOVE
        return MYSTIC_ATTACK_MOVE

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MYSTIC_DEADLY_ATTACK_DAMAGE, MYSTIC_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        MYSTIC_ATTACK_MOVE: MoveState(MYSTIC_ATTACK_MOVE, attack, [attack_intent(attack_intent_damage), debuff_intent()], follow_up_id=MYSTIC_BRANCH),
        MYSTIC_HEAL_MOVE: MoveState(MYSTIC_HEAL_MOVE, heal, [Intent(IntentType.HEAL)], follow_up_id=MYSTIC_BRANCH),
        MYSTIC_BUFF_MOVE: MoveState(MYSTIC_BUFF_MOVE, buff, [buff_intent()], follow_up_id=MYSTIC_BRANCH),
        MYSTIC_BRANCH: BranchState(MYSTIC_BRANCH, chooser),
    }
    return creature, MonsterAI(states, MYSTIC_BRANCH, rng)


# ---- SnakePlant (HP 75-79 / 78-82 asc) ----

SNAKE_PLANT_MONSTER_ID = "THECITY_SNAKE_PLANT"
SNAKE_PLANT_BASE_MIN_HP = 75
SNAKE_PLANT_BASE_MAX_HP = 79
SNAKE_PLANT_TOUGH_MIN_HP = 78
SNAKE_PLANT_TOUGH_MAX_HP = 82
SNAKE_PLANT_MALLEABLE_AMOUNT = 3
SNAKE_PLANT_BASE_CHOMP_DAMAGE = 7
SNAKE_PLANT_DEADLY_CHOMP_DAMAGE = 8
SNAKE_PLANT_CHOMP_HITS = 3
SNAKE_PLANT_SPORES_FRAIL = 2
SNAKE_PLANT_SPORES_WEAK = 2
SNAKE_PLANT_CHOMP_MOVE = "CHOMP"
SNAKE_PLANT_SPORES_MOVE = "SPORES"
SNAKE_PLANT_BRANCH = "SNAKE_PLANT_BRANCH"


def create_snake_plant(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SNAKE_PLANT_TOUGH_MIN_HP, SNAKE_PLANT_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SNAKE_PLANT_TOUGH_MAX_HP, SNAKE_PLANT_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SNAKE_PLANT_MONSTER_ID)
    creature.powers[PowerId.MALLEABLE] = MalleablePower(SNAKE_PLANT_MALLEABLE_AMOUNT)

    def chomp(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SNAKE_PLANT_DEADLY_CHOMP_DAMAGE, SNAKE_PLANT_BASE_CHOMP_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=SNAKE_PLANT_CHOMP_HITS)

    def spores(combat: "CombatState") -> None:
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, SNAKE_PLANT_SPORES_FRAIL, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, SNAKE_PLANT_SPORES_WEAK, applier=creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        if r < 65:
            if len(state_log) >= 2 and state_log[-1] == SNAKE_PLANT_CHOMP_MOVE and state_log[-2] == SNAKE_PLANT_CHOMP_MOVE:
                return SNAKE_PLANT_SPORES_MOVE
            return SNAKE_PLANT_CHOMP_MOVE
        if (state_log and state_log[-1] == SNAKE_PLANT_SPORES_MOVE) or (len(state_log) >= 2 and state_log[-2] == SNAKE_PLANT_SPORES_MOVE):
            return SNAKE_PLANT_CHOMP_MOVE
        return SNAKE_PLANT_SPORES_MOVE

    chomp_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SNAKE_PLANT_DEADLY_CHOMP_DAMAGE, SNAKE_PLANT_BASE_CHOMP_DAMAGE)
    states: dict[str, MonsterState] = {
        SNAKE_PLANT_CHOMP_MOVE: MoveState(SNAKE_PLANT_CHOMP_MOVE, chomp, [multi_attack_intent(chomp_intent_damage, SNAKE_PLANT_CHOMP_HITS)], follow_up_id=SNAKE_PLANT_BRANCH),
        SNAKE_PLANT_SPORES_MOVE: MoveState(SNAKE_PLANT_SPORES_MOVE, spores, [debuff_intent()], follow_up_id=SNAKE_PLANT_BRANCH),
        SNAKE_PLANT_BRANCH: BranchState(SNAKE_PLANT_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SNAKE_PLANT_BRANCH, rng)


# ---- Snecko (HP 114-120 / 120-125 asc) ----

SNECKO_MONSTER_ID = "THECITY_SNECKO"
SNECKO_BASE_MIN_HP = 114
SNECKO_BASE_MAX_HP = 120
SNECKO_TOUGH_MIN_HP = 120
SNECKO_TOUGH_MAX_HP = 125
SNECKO_BASE_BITE_DAMAGE = 15
SNECKO_DEADLY_BITE_DAMAGE = 18
SNECKO_BASE_TAIL_DAMAGE = 8
SNECKO_DEADLY_TAIL_DAMAGE = 10
SNECKO_TAIL_VULN = 2
SNECKO_DEADLY_TAIL_WEAK = 2
SNECKO_GLARE_MOVE = "GLARE"
SNECKO_BITE_MOVE = "BITE"
SNECKO_TAIL_WHIP_MOVE = "TAIL_WHIP"
SNECKO_RANDOM = "SNECKO_RANDOM"


def create_snecko(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SNECKO_TOUGH_MIN_HP, SNECKO_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SNECKO_TOUGH_MAX_HP, SNECKO_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SNECKO_MONSTER_ID)

    def glare(combat: "CombatState") -> None:
        apply_power_to_living_player_targets(combat, PowerId.CONFUSED, 1, applier=creature)

    def bite(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SNECKO_DEADLY_BITE_DAMAGE, SNECKO_BASE_BITE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def tail_whip(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        dmg = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, SNECKO_DEADLY_TAIL_DAMAGE, SNECKO_BASE_TAIL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, SNECKO_TAIL_VULN, applier=creature)
        if asc >= DEADLY_ENEMIES_ASCENSION_LEVEL:
            apply_power_to_living_player_targets(combat, PowerId.WEAK, SNECKO_DEADLY_TAIL_WEAK, applier=creature)

    bite_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SNECKO_DEADLY_BITE_DAMAGE, SNECKO_BASE_BITE_DAMAGE)
    tail_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SNECKO_DEADLY_TAIL_DAMAGE, SNECKO_BASE_TAIL_DAMAGE)

    random_branch = RandomBranchState(SNECKO_RANDOM)
    random_branch.add_branch(SNECKO_BITE_MOVE, MoveRepeatType.CAN_REPEAT_X_TIMES, max_times=2, weight=60.0)
    random_branch.add_branch(SNECKO_TAIL_WHIP_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER, weight=40.0)

    states: dict[str, MonsterState] = {
        SNECKO_GLARE_MOVE: MoveState(SNECKO_GLARE_MOVE, glare, [debuff_intent()], follow_up_id=SNECKO_RANDOM),
        SNECKO_BITE_MOVE: MoveState(SNECKO_BITE_MOVE, bite, [attack_intent(bite_intent_damage)], follow_up_id=SNECKO_RANDOM),
        SNECKO_TAIL_WHIP_MOVE: MoveState(SNECKO_TAIL_WHIP_MOVE, tail_whip, [attack_intent(tail_intent_damage), debuff_intent()], follow_up_id=SNECKO_RANDOM),
        SNECKO_RANDOM: random_branch,
    }
    return creature, MonsterAI(states, SNECKO_GLARE_MOVE)


# ---- ShelledParasite (HP 68-72 / 70-75 asc) ----

SHELLED_PARASITE_MONSTER_ID = "THECITY_SHELLED_PARASITE"
SHELLED_PARASITE_BASE_MIN_HP = 68
SHELLED_PARASITE_BASE_MAX_HP = 72
SHELLED_PARASITE_TOUGH_MIN_HP = 70
SHELLED_PARASITE_TOUGH_MAX_HP = 75
SHELLED_PARASITE_PLATED_ARMOR = 14
SHELLED_PARASITE_BASE_FELL_DAMAGE = 18
SHELLED_PARASITE_DEADLY_FELL_DAMAGE = 21
SHELLED_PARASITE_FELL_FRAIL = 2
SHELLED_PARASITE_BASE_DOUBLE_STRIKE_DAMAGE = 6
SHELLED_PARASITE_DEADLY_DOUBLE_STRIKE_DAMAGE = 7
SHELLED_PARASITE_DOUBLE_STRIKE_HITS = 2
SHELLED_PARASITE_BASE_LIFE_SUCK_DAMAGE = 10
SHELLED_PARASITE_DEADLY_LIFE_SUCK_DAMAGE = 12
SHELLED_PARASITE_FELL_MOVE = "FELL"
SHELLED_PARASITE_DOUBLE_STRIKE_MOVE = "DOUBLE_STRIKE"
SHELLED_PARASITE_LIFE_SUCK_MOVE = "LIFE_SUCK"
SHELLED_PARASITE_STUNNED_MOVE = "STUNNED"
SHELLED_PARASITE_BRANCH = "SHELLED_PARASITE_BRANCH"


class _ShelledParasitePlatedArmorPower(PlatedArmorPower):
    """ShelledParasite-specific ``PlatedArmorPower``: forces the STUNNED
    move once the armor fully depletes. See ``PlatedArmorPower`` in
    ``sts2_env/powers/monster.py`` for the generic regenerating-shield
    mechanic this subclasses.
    """

    def __init__(self, amount: int, on_depleted: Callable[[Creature, "CombatState"], None]):
        super().__init__(amount)
        self._on_depleted = on_depleted

    def after_damage_received(
        self, owner: Creature, target: Creature, dealer: Creature | None,
        damage: int, props: ValueProp, combat: "CombatState",
    ) -> None:
        super().after_damage_received(owner, target, dealer, damage, props, combat)
        if target is owner and self.power_id not in owner.powers:
            self._on_depleted(owner, combat)


def create_shelled_parasite(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_TOUGH_MIN_HP, SHELLED_PARASITE_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_TOUGH_MAX_HP, SHELLED_PARASITE_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SHELLED_PARASITE_MONSTER_ID)

    def _on_armor_break(owner: Creature, combat: "CombatState") -> None:
        combat.set_enemy_state(owner, SHELLED_PARASITE_STUNNED_MOVE)

    creature.powers[PowerId.PLATED_ARMOR] = _ShelledParasitePlatedArmorPower(
        SHELLED_PARASITE_PLATED_ARMOR, on_depleted=_on_armor_break,
    )

    def fell(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_FELL_DAMAGE, SHELLED_PARASITE_BASE_FELL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, SHELLED_PARASITE_FELL_FRAIL, applier=creature)

    def double_strike(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_DOUBLE_STRIKE_DAMAGE, SHELLED_PARASITE_BASE_DOUBLE_STRIKE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=SHELLED_PARASITE_DOUBLE_STRIKE_HITS)

    def life_suck(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_LIFE_SUCK_DAMAGE, SHELLED_PARASITE_BASE_LIFE_SUCK_DAMAGE)
        total_unblocked = 0
        for target in living_player_targets(combat):
            before = target.current_hp
            computed = calculate_damage(dmg, creature, target, ValueProp.MOVE, combat)
            apply_damage(target, computed, ValueProp.MOVE, combat, creature)
            total_unblocked += before - target.current_hp
        combat._check_combat_end()  # noqa: SLF001
        if total_unblocked > 0:
            creature.heal(total_unblocked)

    def stunned(combat: "CombatState") -> None:
        pass

    def chooser(state_log: list[str], rng_: Rng, *, _min: float = 0.0) -> str:
        r = rng_.next_float(100.0 - _min) + _min
        if r < 20:
            if state_log and state_log[-1] == SHELLED_PARASITE_FELL_MOVE:
                return chooser(state_log, rng_, _min=20.0)
            return SHELLED_PARASITE_FELL_MOVE
        if r < 60:
            if len(state_log) >= 2 and state_log[-1] == SHELLED_PARASITE_DOUBLE_STRIKE_MOVE and state_log[-2] == SHELLED_PARASITE_DOUBLE_STRIKE_MOVE:
                return SHELLED_PARASITE_LIFE_SUCK_MOVE
            return SHELLED_PARASITE_DOUBLE_STRIKE_MOVE
        if len(state_log) >= 2 and state_log[-1] == SHELLED_PARASITE_LIFE_SUCK_MOVE and state_log[-2] == SHELLED_PARASITE_LIFE_SUCK_MOVE:
            return SHELLED_PARASITE_DOUBLE_STRIKE_MOVE
        return SHELLED_PARASITE_LIFE_SUCK_MOVE

    fell_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_FELL_DAMAGE, SHELLED_PARASITE_BASE_FELL_DAMAGE)
    double_strike_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_DOUBLE_STRIKE_DAMAGE, SHELLED_PARASITE_BASE_DOUBLE_STRIKE_DAMAGE)
    life_suck_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SHELLED_PARASITE_DEADLY_LIFE_SUCK_DAMAGE, SHELLED_PARASITE_BASE_LIFE_SUCK_DAMAGE)
    states: dict[str, MonsterState] = {
        SHELLED_PARASITE_FELL_MOVE: MoveState(SHELLED_PARASITE_FELL_MOVE, fell, [attack_intent(fell_intent_damage), debuff_intent()], follow_up_id=SHELLED_PARASITE_BRANCH),
        SHELLED_PARASITE_DOUBLE_STRIKE_MOVE: MoveState(SHELLED_PARASITE_DOUBLE_STRIKE_MOVE, double_strike, [multi_attack_intent(double_strike_intent_damage, SHELLED_PARASITE_DOUBLE_STRIKE_HITS)], follow_up_id=SHELLED_PARASITE_BRANCH),
        SHELLED_PARASITE_LIFE_SUCK_MOVE: MoveState(SHELLED_PARASITE_LIFE_SUCK_MOVE, life_suck, [attack_intent(life_suck_intent_damage), Intent(IntentType.HEAL)], follow_up_id=SHELLED_PARASITE_BRANCH),
        SHELLED_PARASITE_STUNNED_MOVE: MoveState(SHELLED_PARASITE_STUNNED_MOVE, stunned, [Intent(IntentType.STUN)], follow_up_id=SHELLED_PARASITE_BRANCH),
        SHELLED_PARASITE_BRANCH: BranchState(SHELLED_PARASITE_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SHELLED_PARASITE_FELL_MOVE, rng)


# ---- BookOfStabbing (Elite, HP 160-164 / 168-172 asc) ----

BOOK_OF_STABBING_MONSTER_ID = "THECITY_BOOK_OF_STABBING"
BOOK_OF_STABBING_BASE_MIN_HP = 160
BOOK_OF_STABBING_BASE_MAX_HP = 164
BOOK_OF_STABBING_TOUGH_MIN_HP = 168
BOOK_OF_STABBING_TOUGH_MAX_HP = 172
BOOK_OF_STABBING_BASE_STAB_DAMAGE = 6
BOOK_OF_STABBING_DEADLY_STAB_DAMAGE = 7
BOOK_OF_STABBING_BASE_BIG_STAB_DAMAGE = 21
BOOK_OF_STABBING_DEADLY_BIG_STAB_DAMAGE = 24
BOOK_OF_STABBING_PAINFUL_STABS = 1
BOOK_OF_STABBING_STAB_MOVE = "STAB"
BOOK_OF_STABBING_BIG_STAB_MOVE = "BIG_STAB"
BOOK_OF_STABBING_BRANCH = "BOOK_OF_STABBING_BRANCH"


def create_book_of_stabbing(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_TOUGH_MIN_HP, BOOK_OF_STABBING_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_TOUGH_MAX_HP, BOOK_OF_STABBING_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=BOOK_OF_STABBING_MONSTER_ID)
    creature.apply_power(PowerId.PAINFUL_STABS, BOOK_OF_STABBING_PAINFUL_STABS)
    stab_count = {"n": 1}

    def stab(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_DEADLY_STAB_DAMAGE, BOOK_OF_STABBING_BASE_STAB_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=stab_count["n"])

    def big_stab(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_DEADLY_BIG_STAB_DAMAGE, BOOK_OF_STABBING_BASE_BIG_STAB_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        if r < 15:
            stab_count["n"] += 1
            return BOOK_OF_STABBING_STAB_MOVE if (state_log and state_log[-1] == BOOK_OF_STABBING_BIG_STAB_MOVE) else BOOK_OF_STABBING_BIG_STAB_MOVE
        if len(state_log) >= 2 and state_log[-1] == BOOK_OF_STABBING_STAB_MOVE and state_log[-2] == BOOK_OF_STABBING_STAB_MOVE:
            stab_count["n"] += 1
            return BOOK_OF_STABBING_BIG_STAB_MOVE
        stab_count["n"] += 1
        return BOOK_OF_STABBING_STAB_MOVE

    big_stab_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_DEADLY_BIG_STAB_DAMAGE, BOOK_OF_STABBING_BASE_BIG_STAB_DAMAGE)
    stab_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BOOK_OF_STABBING_DEADLY_STAB_DAMAGE, BOOK_OF_STABBING_BASE_STAB_DAMAGE)
    states: dict[str, MonsterState] = {
        BOOK_OF_STABBING_STAB_MOVE: MoveState(BOOK_OF_STABBING_STAB_MOVE, stab, [Intent(IntentType.MULTI_ATTACK, damage=stab_intent_damage, hits=stab_count["n"])], follow_up_id=BOOK_OF_STABBING_BRANCH),
        BOOK_OF_STABBING_BIG_STAB_MOVE: MoveState(BOOK_OF_STABBING_BIG_STAB_MOVE, big_stab, [attack_intent(big_stab_intent_damage)], follow_up_id=BOOK_OF_STABBING_BRANCH),
        BOOK_OF_STABBING_BRANCH: BranchState(BOOK_OF_STABBING_BRANCH, chooser),
    }
    return creature, MonsterAI(states, BOOK_OF_STABBING_BRANCH, rng)


# ---- Taskmaster (HP 54-60 / 57-64 asc) ----

TASKMASTER_MONSTER_ID = "THECITY_TASKMASTER"
TASKMASTER_BASE_MIN_HP = 54
TASKMASTER_BASE_MAX_HP = 60
TASKMASTER_TOUGH_MIN_HP = 57
TASKMASTER_TOUGH_MAX_HP = 64
TASKMASTER_WHIP_DAMAGE = 7  # flat, not ascension-scaled
TASKMASTER_BASE_WOUND_COUNT = 1
TASKMASTER_DEADLY_WOUND_COUNT = 3
TASKMASTER_WHIP_MOVE = "SCOURING_WHIP"


def create_taskmaster(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TASKMASTER_TOUGH_MIN_HP, TASKMASTER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TASKMASTER_TOUGH_MAX_HP, TASKMASTER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=TASKMASTER_MONSTER_ID)

    def scouring_whip(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        _deal_damage_to_player(combat, creature, TASKMASTER_WHIP_DAMAGE)
        wound_count = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, TASKMASTER_DEADLY_WOUND_COUNT, TASKMASTER_BASE_WOUND_COUNT)
        for target in living_player_targets(combat):
            for _ in range(wound_count):
                combat.add_generated_card_to_creature_discard(target, make_wound(), added_by_player=False)
        if asc >= DEADLY_ENEMIES_ASCENSION_LEVEL:
            creature.apply_power(PowerId.STRENGTH, 1, applier=creature)

    wound_intent_count = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, TASKMASTER_DEADLY_WOUND_COUNT, TASKMASTER_BASE_WOUND_COUNT)
    states: dict[str, MonsterState] = {
        TASKMASTER_WHIP_MOVE: MoveState(TASKMASTER_WHIP_MOVE, scouring_whip, [attack_intent(TASKMASTER_WHIP_DAMAGE), status_intent()], follow_up_id=TASKMASTER_WHIP_MOVE),
    }
    del wound_intent_count
    return creature, MonsterAI(states, TASKMASTER_WHIP_MOVE)


# ---- SphericGuardian (HP fixed 20, not ascension-scaled) ----

SPHERIC_GUARDIAN_MONSTER_ID = "THECITY_SPHERIC_GUARDIAN"
SPHERIC_GUARDIAN_HP = 20
SPHERIC_GUARDIAN_ARTIFACT = 3
SPHERIC_GUARDIAN_STARTING_BLOCK = 40
SPHERIC_GUARDIAN_BASE_ACTIVATE_BLOCK = 25
SPHERIC_GUARDIAN_TOUGH_ACTIVATE_BLOCK = 35
SPHERIC_GUARDIAN_BASE_DAMAGE = 10
SPHERIC_GUARDIAN_DEADLY_DAMAGE = 11
SPHERIC_GUARDIAN_FRAIL_AMOUNT = 5
SPHERIC_GUARDIAN_HARDEN_BLOCK = 15
SPHERIC_GUARDIAN_ACTIVATE_MOVE = "ACTIVATE"
SPHERIC_GUARDIAN_FRAIL_ATTACK_MOVE = "FRAIL_ATTACK"
SPHERIC_GUARDIAN_SLAM_MOVE = "SLAM"
SPHERIC_GUARDIAN_HARDEN_MOVE = "HARDEN"


def create_spheric_guardian(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    del rng  # HP is fixed, not rolled.
    creature = Creature(max_hp=SPHERIC_GUARDIAN_HP, monster_id=SPHERIC_GUARDIAN_MONSTER_ID)
    creature.apply_power(PowerId.BARRICADE, 1, applier=creature)
    creature.apply_power(PowerId.ARTIFACT, SPHERIC_GUARDIAN_ARTIFACT, applier=creature)
    creature.block = SPHERIC_GUARDIAN_STARTING_BLOCK

    def activate(combat: "CombatState") -> None:
        block = _ascension_value(_combat_ascension_level(combat), TOUGH_ENEMIES_ASCENSION_LEVEL, SPHERIC_GUARDIAN_TOUGH_ACTIVATE_BLOCK, SPHERIC_GUARDIAN_BASE_ACTIVATE_BLOCK)
        _gain_block(creature, block, combat)

    def frail_attack(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPHERIC_GUARDIAN_DEADLY_DAMAGE, SPHERIC_GUARDIAN_BASE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, SPHERIC_GUARDIAN_FRAIL_AMOUNT, applier=creature)

    def slam(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPHERIC_GUARDIAN_DEADLY_DAMAGE, SPHERIC_GUARDIAN_BASE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=2)

    def harden(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPHERIC_GUARDIAN_DEADLY_DAMAGE, SPHERIC_GUARDIAN_BASE_DAMAGE)
        _gain_block(creature, SPHERIC_GUARDIAN_HARDEN_BLOCK, combat)
        _deal_damage_to_player(combat, creature, dmg)

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPHERIC_GUARDIAN_DEADLY_DAMAGE, SPHERIC_GUARDIAN_BASE_DAMAGE)
    states: dict[str, MonsterState] = {
        SPHERIC_GUARDIAN_ACTIVATE_MOVE: MoveState(SPHERIC_GUARDIAN_ACTIVATE_MOVE, activate, [defend_intent()], follow_up_id=SPHERIC_GUARDIAN_FRAIL_ATTACK_MOVE),
        SPHERIC_GUARDIAN_FRAIL_ATTACK_MOVE: MoveState(SPHERIC_GUARDIAN_FRAIL_ATTACK_MOVE, frail_attack, [attack_intent(attack_intent_damage), debuff_intent()], follow_up_id=SPHERIC_GUARDIAN_SLAM_MOVE),
        SPHERIC_GUARDIAN_SLAM_MOVE: MoveState(SPHERIC_GUARDIAN_SLAM_MOVE, slam, [multi_attack_intent(attack_intent_damage, 2)], follow_up_id=SPHERIC_GUARDIAN_HARDEN_MOVE),
        SPHERIC_GUARDIAN_HARDEN_MOVE: MoveState(SPHERIC_GUARDIAN_HARDEN_MOVE, harden, [attack_intent(attack_intent_damage), defend_intent()], follow_up_id=SPHERIC_GUARDIAN_SLAM_MOVE),
    }
    return creature, MonsterAI(states, SPHERIC_GUARDIAN_ACTIVATE_MOVE)


# ---- Mugger (HP 48-52 / 50-54 asc) ----
# Not in the task's explicit "new monsters" spec list -- only referenced by
# the TwoThievesWeak encounter (alongside Exordium's own Looter), so
# implemented directly against the decompiled Mugger.cs to the same
# standard as everything else here. Same "mug twice, then permanently
# branch into smoke-bomb-and-flee or one-big-swipe-then-flee" shape as
# Exordium's Looter, reusing Looter's exact ThieveryPower subclass (refunds
# stolen gold if killed before it escapes) since that behavior isn't
# Looter-specific despite the class name.

MUGGER_MONSTER_ID = "THECITY_MUGGER"
MUGGER_BASE_MIN_HP = 48
MUGGER_BASE_MAX_HP = 52
MUGGER_TOUGH_MIN_HP = 50
MUGGER_TOUGH_MAX_HP = 54
MUGGER_BASE_MUG_DAMAGE = 10
MUGGER_DEADLY_MUG_DAMAGE = 11
MUGGER_BASE_BIG_SWIPE_DAMAGE = 16
MUGGER_DEADLY_BIG_SWIPE_DAMAGE = 18
MUGGER_BASE_ESCAPE_BLOCK = 11
MUGGER_TOUGH_ESCAPE_BLOCK = 17
MUGGER_BASE_GOLD = 15
MUGGER_DEADLY_GOLD = 20
MUGGER_MUG_MOVE = "MUG"
MUGGER_SMOKE_BOMB_MOVE = "SMOKE_BOMB"
MUGGER_ESCAPE_MOVE = "ESCAPE"
MUGGER_BIG_SWIPE_MOVE = "BIG_SWIPE"
MUGGER_MUG_BRANCH = "MUGGER_MUG_BRANCH"
MUGGER_AFTER_SECOND_MUG = "MUGGER_AFTER_SECOND_MUG"


def create_mugger(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, MUGGER_TOUGH_MIN_HP, MUGGER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, MUGGER_TOUGH_MAX_HP, MUGGER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=MUGGER_MONSTER_ID)
    mug_count = {"n": 0}

    def mug(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MUGGER_DEADLY_MUG_DAMAGE, MUGGER_BASE_MUG_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        mug_count["n"] += 1

    def smoke_bomb(combat: "CombatState") -> None:
        block = _ascension_value(_combat_ascension_level(combat), TOUGH_ENEMIES_ASCENSION_LEVEL, MUGGER_TOUGH_ESCAPE_BLOCK, MUGGER_BASE_ESCAPE_BLOCK)
        _gain_block(creature, block, combat)

    def escape(combat: "CombatState") -> None:
        combat.escape_creature(creature)

    def big_swipe(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MUGGER_DEADLY_BIG_SWIPE_DAMAGE, MUGGER_BASE_BIG_SWIPE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        mug_count["n"] += 1

    def mug_branch(state_log: list[str], rng_: Rng) -> str:
        del state_log, rng_
        if mug_count["n"] < 2:
            return MUGGER_MUG_MOVE
        return MUGGER_AFTER_SECOND_MUG

    after_second_mug = RandomBranchState(MUGGER_AFTER_SECOND_MUG)
    after_second_mug.add_branch(MUGGER_SMOKE_BOMB_MOVE, MoveRepeatType.USE_ONLY_ONCE, weight=50.0)
    after_second_mug.add_branch(MUGGER_BIG_SWIPE_MOVE, MoveRepeatType.USE_ONLY_ONCE, weight=50.0)

    mug_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MUGGER_DEADLY_MUG_DAMAGE, MUGGER_BASE_MUG_DAMAGE)
    big_swipe_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MUGGER_DEADLY_BIG_SWIPE_DAMAGE, MUGGER_BASE_BIG_SWIPE_DAMAGE)
    states: dict[str, MonsterState] = {
        MUGGER_MUG_MOVE: MoveState(MUGGER_MUG_MOVE, mug, [attack_intent(mug_intent_damage)], follow_up_id=MUGGER_MUG_BRANCH),
        MUGGER_MUG_BRANCH: BranchState(MUGGER_MUG_BRANCH, mug_branch),
        MUGGER_AFTER_SECOND_MUG: after_second_mug,
        MUGGER_SMOKE_BOMB_MOVE: MoveState(MUGGER_SMOKE_BOMB_MOVE, smoke_bomb, [defend_intent()], follow_up_id=MUGGER_ESCAPE_MOVE),
        MUGGER_ESCAPE_MOVE: MoveState(MUGGER_ESCAPE_MOVE, escape, [Intent(IntentType.ESCAPE)], follow_up_id=MUGGER_ESCAPE_MOVE),
        MUGGER_BIG_SWIPE_MOVE: MoveState(MUGGER_BIG_SWIPE_MOVE, big_swipe, [attack_intent(big_swipe_intent_damage)], follow_up_id=MUGGER_SMOKE_BOMB_MOVE),
    }
    gold_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MUGGER_DEADLY_GOLD, MUGGER_BASE_GOLD)
    creature.powers[PowerId.THIEVERY] = _LooterThieveryPower(gold_amount)
    return creature, MonsterAI(states, MUGGER_MUG_MOVE, rng)


# ========================================================================
# ELITE MONSTERS
# ========================================================================

# ---- GremlinLeader (Elite, HP 140-148 / 145-155 asc) ----
# Only spawned via GremlinLeaderElite alongside 2 already-placed gremlin
# allies. The "apply Minion to every other currently-living ally" on-spawn
# step needs combat context this factory doesn't have (mirrors every other
# Exordium/TheCity "AfterAddedToRoom" ally-scan effect) -- done by
# ``setup_gremlin_leader_elite`` in sts2_env/encounters/thecity.py right
# after all 3 monsters are added, exactly like the decompiled
# ``AfterAddedToRoom`` firing once the whole encounter is in the room.

GREMLIN_LEADER_MONSTER_ID = "THECITY_GREMLIN_LEADER"
GREMLIN_LEADER_BASE_MIN_HP = 140
GREMLIN_LEADER_BASE_MAX_HP = 148
GREMLIN_LEADER_TOUGH_MIN_HP = 145
GREMLIN_LEADER_TOUGH_MAX_HP = 155
GREMLIN_LEADER_BASE_STRENGTH = 4
GREMLIN_LEADER_DEADLY_STRENGTH = 5
GREMLIN_LEADER_BASE_BLOCK = 6
GREMLIN_LEADER_DEADLY_BLOCK = 10
GREMLIN_LEADER_STAB_DAMAGE = 6  # flat, not ascension-scaled
GREMLIN_LEADER_STAB_HITS = 3
GREMLIN_LEADER_MAX_ALLIES = 2
GREMLIN_LEADER_RALLY_MOVE = "RALLY"
GREMLIN_LEADER_ENCOURAGE_MOVE = "ENCOURAGE"
GREMLIN_LEADER_STAB_MOVE = "STAB"
GREMLIN_LEADER_BRANCH = "GREMLIN_LEADER_BRANCH"

# Weighted gremlin pool -- identical to Exordium's GremlinGangNormal pool
# ({Mad x2, Sneaky x2, Fat x2, Shield x1, Wizard x1}, total 8). The task
# spec's own restated weights for RALLY ("GremlinWizard 2/8") total 9/8 and
# don't match either this pool or the decompiled ``SummonRandomGremlin``
# (which rolls 0-7 and gives Wizard exactly the single leftover value,
# 1/8) -- treated as a typo and resolved to 1/8 for Wizard, consistent with
# both the source and the pool this same task cites moments earlier for
# GremlinLeaderElite's own initial 2-gremlin draw.
GREMLIN_LEADER_RALLY_POOL = (
    [create_gremlin_mad] * 2
    + [create_gremlin_sneaky] * 2
    + [create_gremlin_fat] * 2
    + [create_gremlin_shield]
    + [create_gremlin_wizard]
)


def create_gremlin_leader(
    rng: Rng, ascension_level: int = 0, combat: "CombatState | None" = None,
) -> tuple[Creature, MonsterAI]:
    """See ``create_centurion`` for why ``combat`` is an optional parameter
    here. ``setup_gremlin_leader_elite`` (``sts2_env/encounters/thecity.py``)
    adds the 2 initial gremlins to ``combat`` *before* calling this, so
    passing that same ``combat`` through lets the very first move correctly
    see them as living allies."""
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_LEADER_TOUGH_MIN_HP, GREMLIN_LEADER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_LEADER_TOUGH_MAX_HP, GREMLIN_LEADER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_LEADER_MONSTER_ID)
    creature.powers[PowerId.GREMLIN_LEADER_PRESENCE] = GremlinLeaderPresencePower(1)

    def rally(combat: "CombatState") -> None:
        empty = GREMLIN_LEADER_MAX_ALLIES - len(_other_living_enemies(combat, creature))
        for _ in range(max(0, empty)):
            gremlin_creator = combat.rng.choice(GREMLIN_LEADER_RALLY_POOL)
            gremlin, gremlin_ai = gremlin_creator(Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level)
            combat.add_enemy(gremlin, gremlin_ai)
            gremlin.apply_power(PowerId.MINION, 1, applier=creature)

    def encourage(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        strength = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_LEADER_DEADLY_STRENGTH, GREMLIN_LEADER_BASE_STRENGTH)
        block = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_LEADER_DEADLY_BLOCK, GREMLIN_LEADER_BASE_BLOCK)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)
        for ally in _other_living_enemies(combat, creature):
            ally.apply_power(PowerId.STRENGTH, strength, applier=creature)
            _gain_block(ally, block, combat)

    def stab(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, GREMLIN_LEADER_STAB_DAMAGE, hits=GREMLIN_LEADER_STAB_HITS)

    def _encourage_or_stab(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        last = state_log[-1] if state_log else None
        if r < 66:
            return GREMLIN_LEADER_ENCOURAGE_MOVE if last != GREMLIN_LEADER_ENCOURAGE_MOVE else GREMLIN_LEADER_STAB_MOVE
        return GREMLIN_LEADER_STAB_MOVE if last != GREMLIN_LEADER_STAB_MOVE else GREMLIN_LEADER_ENCOURAGE_MOVE

    def chooser(state_log: list[str], rng_: Rng) -> str:
        combat_ = creature.combat_state or combat
        alive = len(_other_living_enemies(combat_, creature)) if combat_ is not None else 0
        last = state_log[-1] if state_log else None
        if alive == 0:
            r = rng_.next_float(100.0)
            if r < 75:
                return GREMLIN_LEADER_RALLY_MOVE if last != GREMLIN_LEADER_RALLY_MOVE else GREMLIN_LEADER_STAB_MOVE
            return GREMLIN_LEADER_STAB_MOVE if last != GREMLIN_LEADER_STAB_MOVE else GREMLIN_LEADER_RALLY_MOVE
        if alive == 1:
            r = rng_.next_float(100.0)
            if r < 50 and last != GREMLIN_LEADER_RALLY_MOVE:
                return GREMLIN_LEADER_RALLY_MOVE
            return _encourage_or_stab(state_log, rng_)
        return _encourage_or_stab(state_log, rng_)

    states: dict[str, MonsterState] = {
        GREMLIN_LEADER_RALLY_MOVE: MoveState(GREMLIN_LEADER_RALLY_MOVE, rally, [Intent(IntentType.SUMMON)], follow_up_id=GREMLIN_LEADER_BRANCH),
        GREMLIN_LEADER_ENCOURAGE_MOVE: MoveState(GREMLIN_LEADER_ENCOURAGE_MOVE, encourage, [defend_intent(), buff_intent()], follow_up_id=GREMLIN_LEADER_BRANCH),
        GREMLIN_LEADER_STAB_MOVE: MoveState(GREMLIN_LEADER_STAB_MOVE, stab, [multi_attack_intent(GREMLIN_LEADER_STAB_DAMAGE, GREMLIN_LEADER_STAB_HITS)], follow_up_id=GREMLIN_LEADER_BRANCH),
        GREMLIN_LEADER_BRANCH: BranchState(GREMLIN_LEADER_BRANCH, chooser),
    }
    return creature, MonsterAI(states, GREMLIN_LEADER_BRANCH, rng)


# ========================================================================
# BOSS MONSTERS
# ========================================================================

# ---- Champ (Boss, HP fixed 420/440, not ranged) ----

CHAMP_MONSTER_ID = "THECITY_CHAMP"
CHAMP_BASE_HP = 420
CHAMP_TOUGH_HP = 440
CHAMP_BASE_SLASH_DAMAGE = 16
CHAMP_DEADLY_SLASH_DAMAGE = 18
CHAMP_BASE_BLOCK = 15
CHAMP_TOUGH_BLOCK = 20
CHAMP_BASE_FORGE = 5
CHAMP_TOUGH_FORGE = 7
CHAMP_EXECUTE_DAMAGE = 10  # flat, not ascension-scaled
CHAMP_EXECUTE_HITS = 2
CHAMP_BASE_SLAP_DAMAGE = 12
CHAMP_DEADLY_SLAP_DAMAGE = 14
CHAMP_SLAP_DEBUFF = 2
CHAMP_BASE_STRENGTH = 3
CHAMP_DEADLY_STRENGTH = 4
CHAMP_TAUNT_DEBUFF = 2
CHAMP_MAX_DEFENSIVE_STANCES = 2
CHAMP_HEAVY_SLASH_MOVE = "HEAVY_SLASH"
CHAMP_DEFENSIVE_STANCE_MOVE = "DEFENSIVE_STANCE"
CHAMP_EXECUTE_MOVE = "EXECUTE"
CHAMP_FACE_SLAP_MOVE = "FACE_SLAP"
CHAMP_GLOAT_MOVE = "GLOAT"
CHAMP_TAUNT_MOVE = "TAUNT"
CHAMP_ANGER_MOVE = "ANGER"
CHAMP_BRANCH = "CHAMP_BRANCH"


def create_champ(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    # HP is fixed, not rolled -- ``rng`` is still needed below, to resolve
    # the initial CHAMP_BRANCH state (Champ's move selection, unlike its
    # HP, is a real branch from the very first turn).
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CHAMP_TOUGH_HP, CHAMP_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=CHAMP_MONSTER_ID)
    state = {"turns": 0, "threshold_reached": False, "forge_times": 0}

    def heavy_slash(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_SLASH_DAMAGE, CHAMP_BASE_SLASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def defensive_stance(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        block = _ascension_value(asc, TOUGH_ENEMIES_ASCENSION_LEVEL, CHAMP_TOUGH_BLOCK, CHAMP_BASE_BLOCK)
        forge = _ascension_value(asc, TOUGH_ENEMIES_ASCENSION_LEVEL, CHAMP_TOUGH_FORGE, CHAMP_BASE_FORGE)
        _gain_block(creature, block, combat)
        creature.apply_power(PowerId.METALLICIZE, forge, applier=creature)

    def execute(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, CHAMP_EXECUTE_DAMAGE, hits=CHAMP_EXECUTE_HITS)

    def face_slap(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_SLAP_DAMAGE, CHAMP_BASE_SLAP_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, CHAMP_SLAP_DEBUFF, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, CHAMP_SLAP_DEBUFF, applier=creature)

    def gloat(combat: "CombatState") -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_STRENGTH, CHAMP_BASE_STRENGTH)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def taunt(combat: "CombatState") -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, CHAMP_TAUNT_DEBUFF, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, CHAMP_TAUNT_DEBUFF, applier=creature)

    def anger(combat: "CombatState") -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_STRENGTH, CHAMP_BASE_STRENGTH)
        for power_id in [pid for pid, power in creature.powers.items() if power.power_type.name == "DEBUFF"]:
            creature.powers.pop(power_id, None)
        creature.apply_power(PowerId.STRENGTH, strength * 3, applier=creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        state["turns"] += 1
        if creature.current_hp <= creature.max_hp // 2 and not state["threshold_reached"]:
            state["threshold_reached"] = True
            return CHAMP_ANGER_MOVE
        last = state_log[-1] if state_log else None
        last2 = state_log[-2] if len(state_log) >= 2 else None
        if state["threshold_reached"] and last != CHAMP_EXECUTE_MOVE and last2 != CHAMP_EXECUTE_MOVE:
            return CHAMP_EXECUTE_MOVE
        if state["turns"] == 4 and not state["threshold_reached"]:
            state["turns"] = 0
            return CHAMP_TAUNT_MOVE
        r = rng_.next_float(100.0)
        if last != CHAMP_DEFENSIVE_STANCE_MOVE and state["forge_times"] < CHAMP_MAX_DEFENSIVE_STANCES and r < 30:
            state["forge_times"] += 1
            return CHAMP_DEFENSIVE_STANCE_MOVE
        if last != CHAMP_GLOAT_MOVE and last != CHAMP_DEFENSIVE_STANCE_MOVE and r < 30:
            return CHAMP_GLOAT_MOVE
        if last != CHAMP_FACE_SLAP_MOVE and r < 55:
            return CHAMP_FACE_SLAP_MOVE
        if last != CHAMP_HEAVY_SLASH_MOVE:
            return CHAMP_HEAVY_SLASH_MOVE
        return CHAMP_FACE_SLAP_MOVE

    slash_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_SLASH_DAMAGE, CHAMP_BASE_SLASH_DAMAGE)
    slap_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CHAMP_DEADLY_SLAP_DAMAGE, CHAMP_BASE_SLAP_DAMAGE)
    states: dict[str, MonsterState] = {
        CHAMP_HEAVY_SLASH_MOVE: MoveState(CHAMP_HEAVY_SLASH_MOVE, heavy_slash, [attack_intent(slash_intent_damage)], follow_up_id=CHAMP_BRANCH),
        CHAMP_DEFENSIVE_STANCE_MOVE: MoveState(CHAMP_DEFENSIVE_STANCE_MOVE, defensive_stance, [defend_intent(), buff_intent()], follow_up_id=CHAMP_BRANCH),
        CHAMP_EXECUTE_MOVE: MoveState(CHAMP_EXECUTE_MOVE, execute, [multi_attack_intent(CHAMP_EXECUTE_DAMAGE, CHAMP_EXECUTE_HITS)], follow_up_id=CHAMP_BRANCH),
        CHAMP_FACE_SLAP_MOVE: MoveState(CHAMP_FACE_SLAP_MOVE, face_slap, [attack_intent(slap_intent_damage), debuff_intent()], follow_up_id=CHAMP_BRANCH),
        CHAMP_GLOAT_MOVE: MoveState(CHAMP_GLOAT_MOVE, gloat, [buff_intent()], follow_up_id=CHAMP_BRANCH),
        CHAMP_TAUNT_MOVE: MoveState(CHAMP_TAUNT_MOVE, taunt, [debuff_intent()], follow_up_id=CHAMP_BRANCH),
        CHAMP_ANGER_MOVE: MoveState(CHAMP_ANGER_MOVE, anger, [buff_intent()], follow_up_id=CHAMP_BRANCH),
        CHAMP_BRANCH: BranchState(CHAMP_BRANCH, chooser),
    }
    return creature, MonsterAI(states, CHAMP_BRANCH, rng)


# ---- TorchHead (Collector's minion, HP 38-40 / 40-45 asc) ----

TORCH_HEAD_MONSTER_ID = "THECITY_TORCH_HEAD"
TORCH_HEAD_BASE_MIN_HP = 38
TORCH_HEAD_BASE_MAX_HP = 40
TORCH_HEAD_TOUGH_MIN_HP = 40
TORCH_HEAD_TOUGH_MAX_HP = 45
TORCH_HEAD_TACKLE_DAMAGE = 7  # flat, not ascension-scaled
TORCH_HEAD_TACKLE_MOVE = "TACKLE"


def create_torch_head(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TORCH_HEAD_TOUGH_MIN_HP, TORCH_HEAD_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TORCH_HEAD_TOUGH_MAX_HP, TORCH_HEAD_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=TORCH_HEAD_MONSTER_ID)

    def tackle(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, TORCH_HEAD_TACKLE_DAMAGE)

    states: dict[str, MonsterState] = {
        TORCH_HEAD_TACKLE_MOVE: MoveState(TORCH_HEAD_TACKLE_MOVE, tackle, [attack_intent(TORCH_HEAD_TACKLE_DAMAGE)], follow_up_id=TORCH_HEAD_TACKLE_MOVE),
    }
    return creature, MonsterAI(states, TORCH_HEAD_TACKLE_MOVE)


# ---- Collector (Boss, HP fixed 282/300, not ranged) ----

COLLECTOR_MONSTER_ID = "THECITY_COLLECTOR"
COLLECTOR_BASE_HP = 282
COLLECTOR_TOUGH_HP = 300
COLLECTOR_BASE_FIREBALL_DAMAGE = 18
COLLECTOR_DEADLY_FIREBALL_DAMAGE = 21
COLLECTOR_BASE_BLOCK = 15
COLLECTOR_TOUGH_BLOCK = 18
COLLECTOR_BASE_STRENGTH = 4
COLLECTOR_DEADLY_STRENGTH = 5
COLLECTOR_BASE_MEGA_DEBUFF = 3
COLLECTOR_DEADLY_MEGA_DEBUFF = 5
COLLECTOR_TORCH_SLOTS = 2
COLLECTOR_SPAWN_MOVE = "SPAWN"
COLLECTOR_FIREBALL_MOVE = "FIREBALL"
COLLECTOR_BUFF_MOVE = "BUFF"
COLLECTOR_MEGA_DEBUFF_MOVE = "MEGA_DEBUFF"
COLLECTOR_REVIVE_MOVE = "REVIVE"
COLLECTOR_BRANCH = "COLLECTOR_BRANCH"


def create_collector(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    del rng  # HP is fixed, not rolled.
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, COLLECTOR_TOUGH_HP, COLLECTOR_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=COLLECTOR_MONSTER_ID)
    creature.powers[PowerId.MINION_MASTER] = MinionMasterPower(1)
    state = {"turns": 0, "ult_used": False}

    def _spawn_torch_heads(combat: "CombatState") -> None:
        empty = COLLECTOR_TORCH_SLOTS - len(_other_living_enemies(combat, creature))
        for _ in range(max(0, empty)):
            torch, torch_ai = create_torch_head(Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level)
            combat.add_enemy(torch, torch_ai)
            torch.apply_power(PowerId.MINION, 1, applier=creature)

    def spawn(combat: "CombatState") -> None:
        _spawn_torch_heads(combat)

    def fireball(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, COLLECTOR_DEADLY_FIREBALL_DAMAGE, COLLECTOR_BASE_FIREBALL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def buff(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        block = _ascension_value(asc, TOUGH_ENEMIES_ASCENSION_LEVEL, COLLECTOR_TOUGH_BLOCK, COLLECTOR_BASE_BLOCK)
        strength = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, COLLECTOR_DEADLY_STRENGTH, COLLECTOR_BASE_STRENGTH)
        _gain_block(creature, block, combat)
        for ally in [creature] + _other_living_enemies(combat, creature):
            ally.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def mega_debuff(combat: "CombatState") -> None:
        amount = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, COLLECTOR_DEADLY_MEGA_DEBUFF, COLLECTOR_BASE_MEGA_DEBUFF)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, amount, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, amount, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, amount, applier=creature)
        state["ult_used"] = True

    def revive(combat: "CombatState") -> None:
        _spawn_torch_heads(combat)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        combat_ = creature.combat_state
        state["turns"] += 1
        last = state_log[-1] if state_log else None
        last2 = state_log[-2] if len(state_log) >= 2 else None
        if state["turns"] >= 3 and not state["ult_used"]:
            return COLLECTOR_MEGA_DEBUFF_MOVE
        r = rng_.next_float(100.0)
        empty_slot = len(_other_living_enemies(combat_, creature)) < COLLECTOR_TORCH_SLOTS
        if r <= 25 and empty_slot and last != COLLECTOR_REVIVE_MOVE:
            return COLLECTOR_REVIVE_MOVE
        if r <= 70 and not (last == COLLECTOR_FIREBALL_MOVE and last2 == COLLECTOR_FIREBALL_MOVE):
            return COLLECTOR_FIREBALL_MOVE
        if last != COLLECTOR_BUFF_MOVE:
            return COLLECTOR_BUFF_MOVE
        return COLLECTOR_FIREBALL_MOVE

    fireball_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, COLLECTOR_DEADLY_FIREBALL_DAMAGE, COLLECTOR_BASE_FIREBALL_DAMAGE)
    states: dict[str, MonsterState] = {
        COLLECTOR_SPAWN_MOVE: MoveState(COLLECTOR_SPAWN_MOVE, spawn, [Intent(IntentType.SUMMON)], follow_up_id=COLLECTOR_BRANCH),
        COLLECTOR_FIREBALL_MOVE: MoveState(COLLECTOR_FIREBALL_MOVE, fireball, [attack_intent(fireball_intent_damage)], follow_up_id=COLLECTOR_BRANCH),
        COLLECTOR_BUFF_MOVE: MoveState(COLLECTOR_BUFF_MOVE, buff, [defend_intent(), buff_intent()], follow_up_id=COLLECTOR_BRANCH),
        COLLECTOR_MEGA_DEBUFF_MOVE: MoveState(COLLECTOR_MEGA_DEBUFF_MOVE, mega_debuff, [debuff_intent()], follow_up_id=COLLECTOR_BRANCH),
        COLLECTOR_REVIVE_MOVE: MoveState(COLLECTOR_REVIVE_MOVE, revive, [Intent(IntentType.SUMMON)], follow_up_id=COLLECTOR_BRANCH),
        COLLECTOR_BRANCH: BranchState(COLLECTOR_BRANCH, chooser),
    }
    return creature, MonsterAI(states, COLLECTOR_SPAWN_MOVE)


# ---- BronzeOrb (BronzeAutomaton's minion, HP 52-58 / 54-60 asc) ----

BRONZE_ORB_MONSTER_ID = "THECITY_BRONZE_ORB"
BRONZE_ORB_BASE_MIN_HP = 52
BRONZE_ORB_BASE_MAX_HP = 58
BRONZE_ORB_TOUGH_MIN_HP = 54
BRONZE_ORB_TOUGH_MAX_HP = 60
BRONZE_ORB_BEAM_DAMAGE = 8  # flat, not ascension-scaled
BRONZE_ORB_SUPPORT_BLOCK = 12
BRONZE_ORB_BEAM_MOVE = "BEAM"
BRONZE_ORB_SUPPORT_BEAM_MOVE = "SUPPORT_BEAM"
BRONZE_ORB_STASIS_MOVE = "STASIS"
BRONZE_ORB_BRANCH = "BRONZE_ORB_BRANCH"

_RARITY_PRIORITY = (CardRarity.RARE, CardRarity.UNCOMMON, CardRarity.COMMON)


def _bronze_orb_choose_card(combat: "CombatState", target: Creature):
    state = combat.combat_player_state_for(target)
    if state is None:
        return None
    pool = list(state.draw) if state.draw else list(state.discard)
    if not pool:
        return None
    shuffled = list(pool)
    combat.shuffle_rng.shuffle(shuffled)
    for rarity in _RARITY_PRIORITY:
        for card in shuffled:
            if getattr(card, "rarity", None) == rarity:
                return card
    return shuffled[0]


def create_bronze_orb(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BRONZE_ORB_TOUGH_MIN_HP, BRONZE_ORB_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BRONZE_ORB_TOUGH_MAX_HP, BRONZE_ORB_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=BRONZE_ORB_MONSTER_ID)
    used_stasis = {"done": False}

    def beam(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, BRONZE_ORB_BEAM_DAMAGE)

    def support_beam(combat: "CombatState") -> None:
        automaton = next((e for e in _other_living_enemies(combat, creature) if e.monster_id == BRONZE_AUTOMATON_MONSTER_ID), None)
        if automaton is not None:
            _gain_block(automaton, BRONZE_ORB_SUPPORT_BLOCK, combat)

    def stasis(combat: "CombatState") -> None:
        for target in living_player_targets(combat):
            card = _bronze_orb_choose_card(combat, target)
            if card is None:
                continue
            combat._remove_card_from_piles(card)  # noqa: SLF001
            power = StasisPower(1)
            power.capture(card, target)
            creature.powers[PowerId.STASIS] = power

    def chooser(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        last = state_log[-1] if state_log else None
        last2 = state_log[-2] if len(state_log) >= 2 else None
        if r >= 25 and not used_stasis["done"]:
            used_stasis["done"] = True
            return BRONZE_ORB_STASIS_MOVE
        if r >= 70 and not (last == BRONZE_ORB_SUPPORT_BEAM_MOVE and last2 == BRONZE_ORB_SUPPORT_BEAM_MOVE):
            return BRONZE_ORB_SUPPORT_BEAM_MOVE
        if not (last == BRONZE_ORB_BEAM_MOVE and last2 == BRONZE_ORB_BEAM_MOVE):
            return BRONZE_ORB_BEAM_MOVE
        return BRONZE_ORB_SUPPORT_BEAM_MOVE

    states: dict[str, MonsterState] = {
        BRONZE_ORB_BEAM_MOVE: MoveState(BRONZE_ORB_BEAM_MOVE, beam, [attack_intent(BRONZE_ORB_BEAM_DAMAGE)], follow_up_id=BRONZE_ORB_BRANCH),
        BRONZE_ORB_SUPPORT_BEAM_MOVE: MoveState(BRONZE_ORB_SUPPORT_BEAM_MOVE, support_beam, [defend_intent()], follow_up_id=BRONZE_ORB_BRANCH),
        BRONZE_ORB_STASIS_MOVE: MoveState(BRONZE_ORB_STASIS_MOVE, stasis, [Intent(IntentType.CARD_DEBUFF)], follow_up_id=BRONZE_ORB_BRANCH),
        BRONZE_ORB_BRANCH: BranchState(BRONZE_ORB_BRANCH, chooser),
    }
    return creature, MonsterAI(states, BRONZE_ORB_BRANCH, rng)


# ---- BronzeAutomaton (Boss, HP fixed 300/320, not ranged) ----

BRONZE_AUTOMATON_MONSTER_ID = "THECITY_BRONZE_AUTOMATON"
BRONZE_AUTOMATON_BASE_HP = 300
BRONZE_AUTOMATON_TOUGH_HP = 320
BRONZE_AUTOMATON_ARTIFACT = 3
BRONZE_AUTOMATON_BASE_BLOCK = 9
BRONZE_AUTOMATON_TOUGH_BLOCK = 12
BRONZE_AUTOMATON_BASE_STRENGTH = 3
BRONZE_AUTOMATON_DEADLY_STRENGTH = 4
BRONZE_AUTOMATON_BASE_FLAIL_DAMAGE = 7
BRONZE_AUTOMATON_DEADLY_FLAIL_DAMAGE = 8
BRONZE_AUTOMATON_FLAIL_HITS = 2
BRONZE_AUTOMATON_BASE_BEAM_DAMAGE = 45
BRONZE_AUTOMATON_DEADLY_BEAM_DAMAGE = 50
BRONZE_AUTOMATON_ORB_SLOTS = 2
BRONZE_AUTOMATON_SPAWN_ORBS_MOVE = "SPAWN_ORBS"
BRONZE_AUTOMATON_BOOST_MOVE = "BOOST"
BRONZE_AUTOMATON_FLAIL_MOVE = "FLAIL"
BRONZE_AUTOMATON_HYPER_BEAM_MOVE = "HYPER_BEAM"
BRONZE_AUTOMATON_BRANCH = "BRONZE_AUTOMATON_BRANCH"


def create_bronze_automaton(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    del rng  # HP is fixed, not rolled.
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_TOUGH_HP, BRONZE_AUTOMATON_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=BRONZE_AUTOMATON_MONSTER_ID)
    creature.apply_power(PowerId.ARTIFACT, BRONZE_AUTOMATON_ARTIFACT, applier=creature)
    creature.powers[PowerId.MINION_MASTER] = MinionMasterPower(1)
    state = {"turns": 0}

    def spawn_orbs(combat: "CombatState") -> None:
        empty = BRONZE_AUTOMATON_ORB_SLOTS - len(_other_living_enemies(combat, creature))
        for _ in range(max(0, empty)):
            orb, orb_ai = create_bronze_orb(Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level)
            combat.add_enemy(orb, orb_ai)
            orb.apply_power(PowerId.MINION, 1, applier=creature)

    def boost(combat: "CombatState") -> None:
        asc = _combat_ascension_level(combat)
        block = _ascension_value(asc, TOUGH_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_TOUGH_BLOCK, BRONZE_AUTOMATON_BASE_BLOCK)
        strength = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_DEADLY_STRENGTH, BRONZE_AUTOMATON_BASE_STRENGTH)
        _gain_block(creature, block, combat)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def flail(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_DEADLY_FLAIL_DAMAGE, BRONZE_AUTOMATON_BASE_FLAIL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=BRONZE_AUTOMATON_FLAIL_HITS)

    def hyper_beam(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_DEADLY_BEAM_DAMAGE, BRONZE_AUTOMATON_BASE_BEAM_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        state["turns"] = 0

    def chooser(state_log: list[str], rng_: Rng) -> str:
        # Only the single most recent move is checked here (not the last
        # two) -- the decompiled BronzeAutomaton.cs ``SelectNextMove`` only
        # ever calls the single-move ``LastMove`` helper (no
        # ``LastTwoMoves`` exists for this class at all), which contradicts
        # the task spec's own paraphrase ("neither the last move nor the
        # move before that"). Resolved in favor of the decompiled source.
        last = state_log[-1] if state_log else None
        if state["turns"] == 4:
            state["turns"] = 0
            return BRONZE_AUTOMATON_HYPER_BEAM_MOVE
        if last == BRONZE_AUTOMATON_HYPER_BEAM_MOVE:
            return BRONZE_AUTOMATON_BOOST_MOVE
        state["turns"] += 1
        boost_or_spawn = {BRONZE_AUTOMATON_BOOST_MOVE, BRONZE_AUTOMATON_SPAWN_ORBS_MOVE}
        if last not in boost_or_spawn:
            return BRONZE_AUTOMATON_BOOST_MOVE
        return BRONZE_AUTOMATON_FLAIL_MOVE

    flail_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_DEADLY_FLAIL_DAMAGE, BRONZE_AUTOMATON_BASE_FLAIL_DAMAGE)
    beam_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BRONZE_AUTOMATON_DEADLY_BEAM_DAMAGE, BRONZE_AUTOMATON_BASE_BEAM_DAMAGE)
    states: dict[str, MonsterState] = {
        BRONZE_AUTOMATON_SPAWN_ORBS_MOVE: MoveState(BRONZE_AUTOMATON_SPAWN_ORBS_MOVE, spawn_orbs, [Intent(IntentType.SUMMON)], follow_up_id=BRONZE_AUTOMATON_BRANCH),
        BRONZE_AUTOMATON_BOOST_MOVE: MoveState(BRONZE_AUTOMATON_BOOST_MOVE, boost, [defend_intent(), buff_intent()], follow_up_id=BRONZE_AUTOMATON_BRANCH),
        BRONZE_AUTOMATON_FLAIL_MOVE: MoveState(BRONZE_AUTOMATON_FLAIL_MOVE, flail, [multi_attack_intent(flail_intent_damage, BRONZE_AUTOMATON_FLAIL_HITS)], follow_up_id=BRONZE_AUTOMATON_BRANCH),
        BRONZE_AUTOMATON_HYPER_BEAM_MOVE: MoveState(BRONZE_AUTOMATON_HYPER_BEAM_MOVE, hyper_beam, [attack_intent(beam_intent_damage)], follow_up_id=BRONZE_AUTOMATON_BRANCH),
        BRONZE_AUTOMATON_BRANCH: BranchState(BRONZE_AUTOMATON_BRANCH, chooser),
    }
    return creature, MonsterAI(states, BRONZE_AUTOMATON_SPAWN_ORBS_MOVE)


# ========================================================================
# EVENT-ONLY MONSTERS (MaskedBandits event -- RedMaskBanditsEvent encounter)
# ========================================================================
# Decompiled references: Pointy.cs / Romeo.cs / Bear.cs. These three only
# ever appear in the MaskedBandits event fight (RedMaskBanditsEvent.cs) --
# they are deliberately NOT added to any weak/normal/elite/boss pool in
# sts2_env/encounters/thecity.py.

# ---- Pointy (HP 30 fixed / 34 asc) ----

POINTY_MONSTER_ID = "THECITY_POINTY"
POINTY_BASE_HP = 30
POINTY_TOUGH_HP = 34
POINTY_BASE_ATTACK_DAMAGE = 5
POINTY_DEADLY_ATTACK_DAMAGE = 6
POINTY_ATTACK_HITS = 2
POINTY_STAB_MOVE = "STAB"


def create_pointy(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, POINTY_TOUGH_HP, POINTY_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=POINTY_MONSTER_ID)

    def stab(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, POINTY_DEADLY_ATTACK_DAMAGE, POINTY_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=POINTY_ATTACK_HITS)

    stab_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, POINTY_DEADLY_ATTACK_DAMAGE, POINTY_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        # STAB.FollowUpState = STAB (loops forever).
        POINTY_STAB_MOVE: MoveState(POINTY_STAB_MOVE, stab, [multi_attack_intent(stab_intent_damage, POINTY_ATTACK_HITS)], follow_up_id=POINTY_STAB_MOVE),
    }
    return creature, MonsterAI(states, POINTY_STAB_MOVE, rng)


# ---- Romeo (HP 35-39 / 37-41 asc) ----

ROMEO_MONSTER_ID = "THECITY_ROMEO"
ROMEO_BASE_MIN_HP = 35
ROMEO_BASE_MAX_HP = 39
ROMEO_TOUGH_MIN_HP = 37
ROMEO_TOUGH_MAX_HP = 41
ROMEO_BASE_CROSS_SLASH_DAMAGE = 15
ROMEO_DEADLY_CROSS_SLASH_DAMAGE = 17
ROMEO_BASE_AGONIZE_DAMAGE = 10
ROMEO_DEADLY_AGONIZE_DAMAGE = 12
ROMEO_AGONIZE_WEAK = 3
ROMEO_MOCK_MOVE = "MOCK"
ROMEO_CROSS_SLASH_MOVE = "CROSS_SLASH"
ROMEO_AGONIZING_SLASH_MOVE = "AGONIZING_SLASH"
ROMEO_BRANCH = "ROMEO_MOVE_BRANCH"


def create_romeo(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ROMEO_TOUGH_MIN_HP, ROMEO_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ROMEO_TOUGH_MAX_HP, ROMEO_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=ROMEO_MONSTER_ID)

    def mock(combat: "CombatState") -> None:
        # Pure dialogue (TalkCmd) in the decompiled source -- no gameplay
        # effect. C# intent is UnknownIntent.
        pass

    def cross_slash(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ROMEO_DEADLY_CROSS_SLASH_DAMAGE, ROMEO_BASE_CROSS_SLASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def agonizing_slash(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ROMEO_DEADLY_AGONIZE_DAMAGE, ROMEO_BASE_AGONIZE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, ROMEO_AGONIZE_WEAK, applier=creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        # Decompiled SelectNextMove: CROSS_SLASH unless the last TWO moves
        # were both CROSS_SLASH, else AGONIZING_SLASH.
        last_two_cross = (
            len(state_log) >= 2
            and state_log[-1] == ROMEO_CROSS_SLASH_MOVE
            and state_log[-2] == ROMEO_CROSS_SLASH_MOVE
        )
        return ROMEO_AGONIZING_SLASH_MOVE if last_two_cross else ROMEO_CROSS_SLASH_MOVE

    cross_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ROMEO_DEADLY_CROSS_SLASH_DAMAGE, ROMEO_BASE_CROSS_SLASH_DAMAGE)
    agonize_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ROMEO_DEADLY_AGONIZE_DAMAGE, ROMEO_BASE_AGONIZE_DAMAGE)
    states: dict[str, MonsterState] = {
        # MOCK's UnknownIntent is rendered as buff_intent() -- same
        # translation as GremlinWizard's UnknownIntent charging move in
        # sts2_env/monsters/exordium.py.
        ROMEO_MOCK_MOVE: MoveState(ROMEO_MOCK_MOVE, mock, [buff_intent()], follow_up_id=ROMEO_AGONIZING_SLASH_MOVE),
        ROMEO_CROSS_SLASH_MOVE: MoveState(ROMEO_CROSS_SLASH_MOVE, cross_slash, [attack_intent(cross_intent_damage)], follow_up_id=ROMEO_BRANCH),
        ROMEO_AGONIZING_SLASH_MOVE: MoveState(ROMEO_AGONIZING_SLASH_MOVE, agonizing_slash, [attack_intent(agonize_intent_damage), debuff_intent()], follow_up_id=ROMEO_BRANCH),
        ROMEO_BRANCH: BranchState(ROMEO_BRANCH, chooser),
    }
    return creature, MonsterAI(states, ROMEO_MOCK_MOVE, rng)


# ---- Bear (HP 38-42 / 40-44 asc) ----

BEAR_MONSTER_ID = "THECITY_BEAR"
BEAR_BASE_MIN_HP = 38
BEAR_BASE_MAX_HP = 42
BEAR_TOUGH_MIN_HP = 40
BEAR_TOUGH_MAX_HP = 44
BEAR_BASE_MAUL_DAMAGE = 18
BEAR_DEADLY_MAUL_DAMAGE = 20
BEAR_BASE_LUNGE_DAMAGE = 9
BEAR_DEADLY_LUNGE_DAMAGE = 10
BEAR_LUNGE_BLOCK = 9
BEAR_BASE_DEX_REDUCTION = 2
BEAR_DEADLY_DEX_REDUCTION = 4
BEAR_BEAR_HUG_MOVE = "BEAR_HUG"
BEAR_LUNGE_MOVE = "LUNGE"
BEAR_MAUL_MOVE = "MAUL"


def create_bear(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BEAR_TOUGH_MIN_HP, BEAR_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, BEAR_TOUGH_MAX_HP, BEAR_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=BEAR_MONSTER_ID)

    def bear_hug(combat: "CombatState") -> None:
        dex = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BEAR_DEADLY_DEX_REDUCTION, BEAR_BASE_DEX_REDUCTION)
        apply_power_to_living_player_targets(combat, PowerId.DEXTERITY, -dex, applier=creature)

    def lunge(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BEAR_DEADLY_LUNGE_DAMAGE, BEAR_BASE_LUNGE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        if not combat.is_over:
            _gain_block(creature, BEAR_LUNGE_BLOCK, combat)

    def maul(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, BEAR_DEADLY_MAUL_DAMAGE, BEAR_BASE_MAUL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    lunge_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BEAR_DEADLY_LUNGE_DAMAGE, BEAR_BASE_LUNGE_DAMAGE)
    maul_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, BEAR_DEADLY_MAUL_DAMAGE, BEAR_BASE_MAUL_DAMAGE)
    states: dict[str, MonsterState] = {
        # Fixed cycle: BEAR_HUG -> LUNGE -> MAUL -> LUNGE -> MAUL -> ...
        BEAR_BEAR_HUG_MOVE: MoveState(BEAR_BEAR_HUG_MOVE, bear_hug, [debuff_intent()], follow_up_id=BEAR_LUNGE_MOVE),
        BEAR_LUNGE_MOVE: MoveState(BEAR_LUNGE_MOVE, lunge, [attack_intent(lunge_intent_damage), defend_intent()], follow_up_id=BEAR_MAUL_MOVE),
        BEAR_MAUL_MOVE: MoveState(BEAR_MAUL_MOVE, maul, [attack_intent(maul_intent_damage)], follow_up_id=BEAR_LUNGE_MOVE),
    }
    return creature, MonsterAI(states, BEAR_BEAR_HUG_MOVE, rng)
