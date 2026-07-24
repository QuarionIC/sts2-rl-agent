"""TheBeyond (Act-3-slot legacy act) monsters -- "Acts from the Past" mod.

Recreates Slay the Spire 1's Act 3 ("The Beyond") monster roster as an
alternate for the vanilla Act-3 slot. Standalone content, same status as
``sts2_env/monsters/exordium.py`` and ``sts2_env/monsters/thecity.py``: the
run's act-slot-candidate extension point exists (``sts2_env/map/acts.py``)
but TheBeyond is intentionally NOT registered into it yet -- see
``sts2_env/encounters/thebeyond.py`` for the encounter pools and the scope
note there.

All HP ranges, damage values, and state machines are ported from the
decompiled "Acts from the Past" mod source
(``decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.TheBeyond.Enemies/*.cs``)
-- every monster below was read directly from that source (not just the
prior research summary), since several specs were flagged as needing that
verification. Ascension convention (matching Exordium/TheCity/every other
act in this codebase): HP/toughness scales at Ascension 8, damage/debuff
amounts/status counts scale at Ascension 9.

Uses the existing ``MoveState``/``RandomBranchState``/``ConditionalBranchState``
state-machine framework from ``sts2_env/monsters/state_machine.py`` exactly,
plus the ``BranchState`` (rng+history driven custom chooser) helper class
that ``sts2_env/monsters/exordium.py`` introduced for "reroll on repeat"
branching -- imported from there rather than duplicated.

Reused monsters: ``Cultist``, ``JawWorm`` (Exordium) and ``SphericGuardian``
(TheCity) are reused directly via import, matching the source mod's own
cross-act reuse (``AwakenedOneBoss``/``JawWormHordeNormal`` and
``SphereAndTwoShapesNormal`` reference these exact same classes in the
decompiled mod).

New powers (none of these existed anywhere in this simulator -- vanilla,
Exordium, or TheCity -- so they were appended to ``sts2_env/core/enums.py``'s
``PowerId`` and implemented in ``sts2_env/powers/monster.py``, all
documented there in detail): ``CuriosityPower``, ``UnawakenedPower``
(AwakenedOne's two-phase rebirth), ``LifeLinkPower`` (Darkling's revival),
``ConstrictedPower`` (SpireGrowth), ``ReactivePower`` (WrithingMass's
reroll-on-hit), ``ShiftingPower``/``ShiftingStrengthDownPower`` (Transient),
``DrawReductionPower`` (TimeEater), ``TimeWarpPower`` (TimeEater's Time
Warp), ``NemesisFlickerPower`` (Nemesis's alternating Intangible).

Reused existing powers: ``PowerId.REGENERATE_A4H``/``RegenerateA4hPower``
(AwakenedOne's Regenerate -- Act4Heart's non-decaying "heal Amount every own
turn end" power is mechanically identical to STS1 Regen and there is no
other non-decaying-regen power anywhere in this simulator, so it is reused
here rather than adding a third near-duplicate), ``PowerId.SLOW`` (GiantHead),
``PowerId.INTANGIBLE`` (Nemesis, via ``NemesisFlickerPower``),
``PowerId.ARTIFACT`` (Donu/Deca), ``PowerId.RITUAL`` (OrbWalker's
StrengthUpPower and JawWorm-hard-mode's opening Strength -- "gain Amount
Strength every own turn/at start, forever" is exactly Ritual's shape),
``PowerId.MINION`` (Reptomancer's SnakeDagger summons), ``PowerId.THORNS``
(Spiker), ``PowerId.PLATED_ARMOR``/``PlatedArmorPower`` and ``MalleablePower``
(TheCity -- Deca's Square of Protection and WrithingMass respectively),
``PowerId.WEAK``/``VULNERABLE``/``FRAIL``/``STRENGTH`` (vanilla).
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import IntentType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import INT_MAX, Rng
from sts2_env.cards.status import make_burn, make_dazed, make_parasite, make_slimed, make_void, make_wound
from sts2_env.monsters.intents import (
    Intent, attack_intent, multi_attack_intent,
    buff_intent, debuff_intent, status_intent, defend_intent,
)
from sts2_env.monsters.state_machine import MonsterAI, MonsterState, MoveState
from sts2_env.monsters.block import gain_move_block
from sts2_env.monsters.targets import (
    add_generated_cards_to_living_player_discards,
    apply_power_to_living_player_targets,
    living_player_targets,
)
from sts2_env.monsters.exordium import (
    BranchState,
    JAW_WORM_BASE_BELLOW_BLOCK,
    JAW_WORM_BASE_BELLOW_STRENGTH,
    JAW_WORM_BRANCH,
    JAW_WORM_DEADLY_BELLOW_BLOCK,
    JAW_WORM_DEADLY_BELLOW_STRENGTH,
    create_cultist,
    create_jaw_worm,
)
from sts2_env.monsters.thecity import create_spheric_guardian
from sts2_env.powers.monster import MalleablePower, PlatedArmorPower

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ========================================================================
# Helpers (mirrors the per-act convention used by exordium.py/thecity.py)
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


def _deal_damage_to_targets(
    combat: "CombatState", creature: Creature, targets: list[Creature], base_dmg: int, hits: int = 1,
) -> None:
    for _ in range(hits):
        live = [t for t in targets if t.is_alive]
        if not live:
            break
        for target in live:
            dmg = calculate_damage(base_dmg, creature, target, ValueProp.MOVE, combat)
            apply_damage(target, dmg, ValueProp.MOVE, combat, creature)
        combat._check_combat_end()  # noqa: SLF001
        if combat.is_over:
            break


def _gain_block(creature: Creature, amount: int, combat: "CombatState") -> None:
    gain_move_block(creature, amount, combat)


def _other_living_enemies(combat: "CombatState", creature: Creature) -> list[Creature]:
    return [e for e in combat.enemies if e.is_alive and e is not creature]


def _add_generated_cards_to_living_player_draw(
    combat: "CombatState",
    card_factory: Callable[[], object],
    count: int,
    *,
    random_position: bool = True,
) -> None:
    """Shuffle ``count`` generated status cards into each living player's
    draw pile at a random position -- the "PileType 1 / CardPilePosition 3"
    combination seen throughout TheBeyond's decompiled source (Sludge's
    Void, Repulsor's Dazed, OrbWalker's second Burn), distinct from the more
    common "PileType 3 / CardPilePosition 1" (top of discard) combination
    already covered by ``add_generated_cards_to_living_player_discards``.
    """
    for target in living_player_targets(combat):
        for _ in range(count):
            card = card_factory()
            if card is None:
                continue
            combat.add_generated_card_to_creature_draw_pile(
                target, card, added_by_player=False, random_position=random_position,
            )


# ========================================================================
# WEAK MONSTERS
# ========================================================================

# ---- Darkling (HP 48-59 / 50-59 asc) ----

DARKLING_MONSTER_ID = "THEBEYOND_DARKLING"
DARKLING_BASE_MIN_HP = 48
DARKLING_BASE_MAX_HP = 59
DARKLING_TOUGH_MIN_HP = 50
DARKLING_TOUGH_MAX_HP = 59
DARKLING_BASE_CHOMP_DAMAGE = 8
DARKLING_DEADLY_CHOMP_DAMAGE = 9
DARKLING_CHOMP_HITS = 2
DARKLING_HARDEN_BLOCK = 12
DARKLING_BASE_HARDEN_STRENGTH = 0
DARKLING_DEADLY_HARDEN_STRENGTH = 2
DARKLING_NIP_BASE_MIN = 7
DARKLING_NIP_BASE_MAX_EXCLUSIVE = 12
DARKLING_NIP_DEADLY_MIN = 9
DARKLING_NIP_DEADLY_MAX_EXCLUSIVE = 14
DARKLING_CHOMP_MOVE = "CHOMP"
DARKLING_HARDEN_MOVE = "HARDEN"
DARKLING_NIP_MOVE = "NIP"
DARKLING_DEAD_MOVE = "DEAD_MOVE"
DARKLING_REATTACH_MOVE = "REATTACH_MOVE"
DARKLING_BRANCH = "DARKLING_BRANCH"


def create_darkling(rng: Rng, ascension_level: int = 0, slot_index: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, DARKLING_TOUGH_MIN_HP, DARKLING_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, DARKLING_TOUGH_MAX_HP, DARKLING_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=DARKLING_MONSTER_ID)

    heal_amount = hp // 2
    creature.powers[PowerId.LIFE_LINK] = LifeLinkPowerCls(heal_amount, DARKLING_MONSTER_ID, DARKLING_DEAD_MOVE)

    nip_lo = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DARKLING_NIP_DEADLY_MIN, DARKLING_NIP_BASE_MIN)
    nip_hi_excl = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DARKLING_NIP_DEADLY_MAX_EXCLUSIVE, DARKLING_NIP_BASE_MAX_EXCLUSIVE)
    nip_damage = rng.next_int_exclusive(nip_lo, nip_hi_excl)

    harden_strength = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DARKLING_DEADLY_HARDEN_STRENGTH, DARKLING_BASE_HARDEN_STRENGTH)

    def chomp(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, DARKLING_DEADLY_CHOMP_DAMAGE, DARKLING_BASE_CHOMP_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=DARKLING_CHOMP_HITS)

    def harden(combat: "CombatState") -> None:
        _gain_block(creature, DARKLING_HARDEN_BLOCK, combat)
        if harden_strength > 0:
            creature.apply_power(PowerId.STRENGTH, harden_strength, applier=creature)

    def nip(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, nip_damage)

    def dead_move(combat: "CombatState") -> None:
        pass

    def reattach_move(combat: "CombatState") -> None:
        life_link = creature.powers.get(PowerId.LIFE_LINK)
        if life_link is not None:
            life_link.do_reattach(creature, combat)

    def chooser(state_log: list[str], rng_: Rng, *, forced_roll: float | None = None) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        r = forced_roll if forced_roll is not None else rng_.next_float(100.0)
        if r < 40:
            if not last_is(DARKLING_CHOMP_MOVE) and slot_index % 2 == 0:
                return DARKLING_CHOMP_MOVE
            if forced_roll is not None:
                return DARKLING_HARDEN_MOVE if r < 20 else DARKLING_NIP_MOVE
            return chooser(state_log, rng_, forced_roll=rng_.next_float(60.0) + 40.0)
        if r < 70:
            if not last_is(DARKLING_HARDEN_MOVE):
                return DARKLING_HARDEN_MOVE
            return DARKLING_NIP_MOVE
        if not last_two_is(DARKLING_NIP_MOVE):
            return DARKLING_NIP_MOVE
        if forced_roll is not None:
            return DARKLING_HARDEN_MOVE if not last_is(DARKLING_HARDEN_MOVE) else DARKLING_CHOMP_MOVE
        return chooser(state_log, rng_, forced_roll=rng_.next_float(100.0))

    def first_move_chooser(state_log: list[str], rng_: Rng) -> str:
        return DARKLING_HARDEN_MOVE if rng_.next_float(100.0) < 50 else DARKLING_NIP_MOVE

    chomp_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DARKLING_DEADLY_CHOMP_DAMAGE, DARKLING_BASE_CHOMP_DAMAGE)
    harden_intents = [defend_intent(), buff_intent()] if harden_strength > 0 else [defend_intent()]
    states: dict[str, MonsterState] = {
        DARKLING_CHOMP_MOVE: MoveState(DARKLING_CHOMP_MOVE, chomp, [multi_attack_intent(chomp_intent_damage, DARKLING_CHOMP_HITS)], follow_up_id=DARKLING_BRANCH),
        DARKLING_HARDEN_MOVE: MoveState(DARKLING_HARDEN_MOVE, harden, harden_intents, follow_up_id=DARKLING_BRANCH),
        DARKLING_NIP_MOVE: MoveState(DARKLING_NIP_MOVE, nip, [Intent(IntentType.ATTACK, damage=nip_damage)], follow_up_id=DARKLING_BRANCH),
        DARKLING_DEAD_MOVE: MoveState(DARKLING_DEAD_MOVE, dead_move, [], follow_up_id=DARKLING_REATTACH_MOVE),
        DARKLING_REATTACH_MOVE: MoveState(DARKLING_REATTACH_MOVE, reattach_move, [Intent(IntentType.HEAL)], follow_up_id=DARKLING_BRANCH, must_perform_once=True),
        DARKLING_BRANCH: BranchState(DARKLING_BRANCH, lambda log, r: chooser(log, r)),
        "FIRST_MOVE_BRANCH": BranchState("FIRST_MOVE_BRANCH", first_move_chooser),
    }
    return creature, MonsterAI(states, "FIRST_MOVE_BRANCH", rng)


# ---- OrbWalker (HP 90-102 / 92-102 asc) ----

ORB_WALKER_MONSTER_ID = "THEBEYOND_ORB_WALKER"
ORB_WALKER_BASE_MIN_HP = 90
ORB_WALKER_BASE_MAX_HP = 102
ORB_WALKER_TOUGH_MIN_HP = 92
ORB_WALKER_TOUGH_MAX_HP = 102
ORB_WALKER_BASE_LASER_DAMAGE = 10
ORB_WALKER_DEADLY_LASER_DAMAGE = 11
ORB_WALKER_BASE_CLAW_DAMAGE = 15
ORB_WALKER_DEADLY_CLAW_DAMAGE = 16
ORB_WALKER_BASE_STRENGTH_UP = 3
ORB_WALKER_DEADLY_STRENGTH_UP = 5
ORB_WALKER_LASER_MOVE = "LASER"
ORB_WALKER_CLAW_MOVE = "CLAW"
ORB_WALKER_BRANCH = "ORB_WALKER_BRANCH"


def create_orb_walker(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_TOUGH_MIN_HP, ORB_WALKER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_TOUGH_MAX_HP, ORB_WALKER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=ORB_WALKER_MONSTER_ID)

    # StrengthUpPower ("gain Amount Strength at end of every own turn,
    # forever, no decay") is mechanically identical to this simulator's
    # existing vanilla Ritual, so it is reused directly instead of adding a
    # near-duplicate power.
    strength_up = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_DEADLY_STRENGTH_UP, ORB_WALKER_BASE_STRENGTH_UP)
    creature.apply_power(PowerId.RITUAL, strength_up, applier=creature)

    def laser(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_DEADLY_LASER_DAMAGE, ORB_WALKER_BASE_LASER_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        add_generated_cards_to_living_player_discards(combat, make_burn, 1)
        _add_generated_cards_to_living_player_draw(combat, make_burn, 1)

    def claw(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_DEADLY_CLAW_DAMAGE, ORB_WALKER_BASE_CLAW_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        r = rng_.next_float(100.0)
        if r < 40:
            return ORB_WALKER_CLAW_MOVE if not last_two_is(ORB_WALKER_CLAW_MOVE) else ORB_WALKER_LASER_MOVE
        return ORB_WALKER_LASER_MOVE if not last_two_is(ORB_WALKER_LASER_MOVE) else ORB_WALKER_CLAW_MOVE

    laser_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_DEADLY_LASER_DAMAGE, ORB_WALKER_BASE_LASER_DAMAGE)
    claw_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ORB_WALKER_DEADLY_CLAW_DAMAGE, ORB_WALKER_BASE_CLAW_DAMAGE)
    states: dict[str, MonsterState] = {
        ORB_WALKER_LASER_MOVE: MoveState(ORB_WALKER_LASER_MOVE, laser, [attack_intent(laser_intent_damage), status_intent()], follow_up_id=ORB_WALKER_BRANCH),
        ORB_WALKER_CLAW_MOVE: MoveState(ORB_WALKER_CLAW_MOVE, claw, [attack_intent(claw_intent_damage)], follow_up_id=ORB_WALKER_BRANCH),
        ORB_WALKER_BRANCH: BranchState(ORB_WALKER_BRANCH, chooser),
    }
    return creature, MonsterAI(states, ORB_WALKER_BRANCH, rng)


# ---- Repulsor (Shape family, HP 29-38 / 31-38 asc) ----

REPULSOR_MONSTER_ID = "THEBEYOND_REPULSOR"
REPULSOR_BASE_MIN_HP = 29
REPULSOR_BASE_MAX_HP = 38
REPULSOR_TOUGH_MIN_HP = 31
REPULSOR_TOUGH_MAX_HP = 38
REPULSOR_BASE_ATTACK_DAMAGE = 11
REPULSOR_DEADLY_ATTACK_DAMAGE = 13
REPULSOR_DAZE_COUNT = 2
REPULSOR_ATTACK_MOVE = "ATTACK"
REPULSOR_DAZE_MOVE = "DAZE"
REPULSOR_BRANCH = "REPULSOR_BRANCH"


def create_repulsor(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, REPULSOR_TOUGH_MIN_HP, REPULSOR_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, REPULSOR_TOUGH_MAX_HP, REPULSOR_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=REPULSOR_MONSTER_ID)

    def attack(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, REPULSOR_DEADLY_ATTACK_DAMAGE, REPULSOR_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def daze(combat: "CombatState") -> None:
        _add_generated_cards_to_living_player_draw(combat, make_dazed, REPULSOR_DAZE_COUNT)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        last = state_log[-1] if state_log else None
        if rng_.next_float(100.0) < 20 and last != REPULSOR_ATTACK_MOVE:
            return REPULSOR_ATTACK_MOVE
        return REPULSOR_DAZE_MOVE

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, REPULSOR_DEADLY_ATTACK_DAMAGE, REPULSOR_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        REPULSOR_ATTACK_MOVE: MoveState(REPULSOR_ATTACK_MOVE, attack, [attack_intent(attack_intent_damage)], follow_up_id=REPULSOR_BRANCH),
        REPULSOR_DAZE_MOVE: MoveState(REPULSOR_DAZE_MOVE, daze, [status_intent()], follow_up_id=REPULSOR_BRANCH),
        REPULSOR_BRANCH: BranchState(REPULSOR_BRANCH, chooser),
    }
    return creature, MonsterAI(states, REPULSOR_BRANCH, rng)


# ---- Spiker (Shape family, HP 42-60 / 44-60 asc) ----

SPIKER_MONSTER_ID = "THEBEYOND_SPIKER"
SPIKER_BASE_MIN_HP = 42
SPIKER_BASE_MAX_HP = 60
SPIKER_TOUGH_MIN_HP = 44
SPIKER_TOUGH_MAX_HP = 60
SPIKER_BASE_ATTACK_DAMAGE = 7
SPIKER_DEADLY_ATTACK_DAMAGE = 9
SPIKER_BASE_STARTING_THORNS = 4
SPIKER_DEADLY_STARTING_THORNS = 7
SPIKER_THORNS_PER_BUFF = 2
SPIKER_MAX_BUFF_USES = 5
SPIKER_ATTACK_MOVE = "ATTACK"
SPIKER_BUFF_THORNS_MOVE = "BUFF_THORNS"
SPIKER_BRANCH = "SPIKER_BRANCH"


def create_spiker(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKER_TOUGH_MIN_HP, SPIKER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKER_TOUGH_MAX_HP, SPIKER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SPIKER_MONSTER_ID)

    starting_thorns = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKER_DEADLY_STARTING_THORNS, SPIKER_BASE_STARTING_THORNS)
    creature.apply_power(PowerId.THORNS, starting_thorns, applier=creature)
    thorns_count = {"n": 0}

    def attack(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKER_DEADLY_ATTACK_DAMAGE, SPIKER_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def buff_thorns(combat: "CombatState") -> None:
        thorns_count["n"] += 1
        creature.apply_power(PowerId.THORNS, SPIKER_THORNS_PER_BUFF, applier=creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        if thorns_count["n"] > SPIKER_MAX_BUFF_USES:
            return SPIKER_ATTACK_MOVE
        last = state_log[-1] if state_log else None
        if rng_.next_float(100.0) < 50 and last != SPIKER_ATTACK_MOVE:
            return SPIKER_ATTACK_MOVE
        return SPIKER_BUFF_THORNS_MOVE

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKER_DEADLY_ATTACK_DAMAGE, SPIKER_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        SPIKER_ATTACK_MOVE: MoveState(SPIKER_ATTACK_MOVE, attack, [attack_intent(attack_intent_damage)], follow_up_id=SPIKER_BRANCH),
        SPIKER_BUFF_THORNS_MOVE: MoveState(SPIKER_BUFF_THORNS_MOVE, buff_thorns, [buff_intent()], follow_up_id=SPIKER_BRANCH),
        SPIKER_BRANCH: BranchState(SPIKER_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SPIKER_BRANCH, rng)


# ---- Exploder (Shape family, HP 30-35 (asc<8) / fixed 30 (asc>=8)) ----

EXPLODER_MONSTER_ID = "THEBEYOND_EXPLODER"
EXPLODER_BASE_MIN_HP = 30
EXPLODER_BASE_MAX_HP = 35
EXPLODER_TOUGH_MIN_HP = 30
EXPLODER_TOUGH_MAX_HP = 30
EXPLODER_BASE_ATTACK_DAMAGE = 9
EXPLODER_DEADLY_ATTACK_DAMAGE = 11
EXPLODER_EXPLODE_DAMAGE = 30  # flat, not ascension-scaled
EXPLODER_ATTACK_MOVE = "ATTACK"
EXPLODER_EXPLODE_MOVE = "EXPLODE"
EXPLODER_BRANCH = "EXPLODER_BRANCH"


def create_exploder(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, EXPLODER_TOUGH_MIN_HP, EXPLODER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, EXPLODER_TOUGH_MAX_HP, EXPLODER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=EXPLODER_MONSTER_ID)
    turn_count = {"n": 0}

    def attack(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, EXPLODER_DEADLY_ATTACK_DAMAGE, EXPLODER_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def explode(combat: "CombatState") -> None:
        if not creature.is_alive:
            return
        _deal_damage_to_player(combat, creature, EXPLODER_EXPLODE_DAMAGE)
        combat.kill_creature(creature)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        turn_count["n"] += 1
        return EXPLODER_ATTACK_MOVE if turn_count["n"] <= 2 else EXPLODER_EXPLODE_MOVE

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, EXPLODER_DEADLY_ATTACK_DAMAGE, EXPLODER_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        EXPLODER_ATTACK_MOVE: MoveState(EXPLODER_ATTACK_MOVE, attack, [attack_intent(attack_intent_damage)], follow_up_id=EXPLODER_BRANCH),
        EXPLODER_EXPLODE_MOVE: MoveState(EXPLODER_EXPLODE_MOVE, explode, [Intent(IntentType.DEATH_BLOW, damage=EXPLODER_EXPLODE_DAMAGE)], follow_up_id=EXPLODER_BRANCH),
        EXPLODER_BRANCH: BranchState(EXPLODER_BRANCH, chooser),
    }
    return creature, MonsterAI(states, EXPLODER_BRANCH, rng)


# ========================================================================
# NORMAL MONSTERS
# ========================================================================

# ---- WrithingMass (HP 160-175) ----

WRITHING_MASS_MONSTER_ID = "THEBEYOND_WRITHING_MASS"
WRITHING_MASS_BASE_MIN_HP = 160
WRITHING_MASS_BASE_MAX_HP = 175
WRITHING_MASS_TOUGH_MIN_HP = 160
WRITHING_MASS_TOUGH_MAX_HP = 175
WRITHING_MASS_BASE_BIG_HIT_DAMAGE = 32
WRITHING_MASS_DEADLY_BIG_HIT_DAMAGE = 38
WRITHING_MASS_BASE_MULTI_HIT_DAMAGE = 7
WRITHING_MASS_DEADLY_MULTI_HIT_DAMAGE = 9
WRITHING_MASS_MULTI_HIT_HITS = 3
WRITHING_MASS_BASE_ATTACK_BLOCK_DAMAGE = 15
WRITHING_MASS_DEADLY_ATTACK_BLOCK_DAMAGE = 16
WRITHING_MASS_BASE_ATTACK_BLOCK_BLOCK = 15
WRITHING_MASS_DEADLY_ATTACK_BLOCK_BLOCK = 16
WRITHING_MASS_BASE_ATTACK_DEBUFF_DAMAGE = 10
WRITHING_MASS_DEADLY_ATTACK_DEBUFF_DAMAGE = 12
WRITHING_MASS_ATTACK_DEBUFF_AMOUNT = 2
WRITHING_MASS_MALLEABLE_AMOUNT = 3
WRITHING_MASS_BIG_HIT_MOVE = "BIG_HIT"
WRITHING_MASS_MULTI_HIT_MOVE = "MULTI_HIT"
WRITHING_MASS_ATTACK_BLOCK_MOVE = "ATTACK_BLOCK"
WRITHING_MASS_ATTACK_DEBUFF_MOVE = "ATTACK_DEBUFF"
WRITHING_MASS_MEGA_DEBUFF_MOVE = "MEGA_DEBUFF"
WRITHING_MASS_BRANCH = "WRITHING_MASS_BRANCH"
WRITHING_MASS_FIRST_MOVE_BRANCH = "WRITHING_MASS_FIRST_MOVE_BRANCH"


def create_writhing_mass(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_TOUGH_MIN_HP, WRITHING_MASS_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_TOUGH_MAX_HP, WRITHING_MASS_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=WRITHING_MASS_MONSTER_ID)

    state = {"used_mega_debuff": False, "first_move": True}
    creature.powers[PowerId.REACTIVE] = ReactivePowerCls(
        1, excluded_move_ids=lambda: {WRITHING_MASS_MEGA_DEBUFF_MOVE} if state["used_mega_debuff"] else set(),
    )
    creature.powers[PowerId.MALLEABLE] = MalleablePower(WRITHING_MASS_MALLEABLE_AMOUNT)

    def big_hit(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_BIG_HIT_DAMAGE, WRITHING_MASS_BASE_BIG_HIT_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def multi_hit(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_MULTI_HIT_DAMAGE, WRITHING_MASS_BASE_MULTI_HIT_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=WRITHING_MASS_MULTI_HIT_HITS)

    def attack_block(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_ATTACK_BLOCK_DAMAGE, WRITHING_MASS_BASE_ATTACK_BLOCK_DAMAGE)
        block = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_ATTACK_BLOCK_BLOCK, WRITHING_MASS_BASE_ATTACK_BLOCK_BLOCK)
        _deal_damage_to_player(combat, creature, dmg)
        _gain_block(creature, block, combat)

    def attack_debuff(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_ATTACK_DEBUFF_DAMAGE, WRITHING_MASS_BASE_ATTACK_DEBUFF_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, WRITHING_MASS_ATTACK_DEBUFF_AMOUNT, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, WRITHING_MASS_ATTACK_DEBUFF_AMOUNT, applier=creature)

    def mega_debuff(combat: "CombatState") -> None:
        state["used_mega_debuff"] = True
        # C# ref: ``CardPileCmd.AddCurseToDeck<Parasite>`` -- this simulator
        # has no separate "straight into the persistent deck" pile op, but
        # curse cards added to the discard pile mid-combat naturally fold
        # back into the deck once combat ends (same mechanism every other
        # mid-combat curse/status insertion in this codebase already uses),
        # so this is functionally equivalent.
        add_generated_cards_to_living_player_discards(combat, make_parasite, 1)

    def first_move_chooser(state_log: list[str], rng_: Rng) -> str:
        r = rng_.next_float(100.0)
        if r < 33:
            return WRITHING_MASS_MULTI_HIT_MOVE
        if r < 66:
            return WRITHING_MASS_ATTACK_BLOCK_MOVE
        return WRITHING_MASS_ATTACK_DEBUFF_MOVE

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        r = rng_.next_float(100.0)
        if r < 10:
            if not last_is(WRITHING_MASS_BIG_HIT_MOVE):
                return WRITHING_MASS_BIG_HIT_MOVE
            r = 10.0 + rng_.next_float(90.0)
        if r < 20:
            if not state["used_mega_debuff"] and not last_is(WRITHING_MASS_MEGA_DEBUFF_MOVE):
                state["used_mega_debuff"] = True
                return WRITHING_MASS_MEGA_DEBUFF_MOVE
            if rng_.next_float(1.0) < 0.1 and not last_is(WRITHING_MASS_BIG_HIT_MOVE):
                return WRITHING_MASS_BIG_HIT_MOVE
            r = 20.0 + rng_.next_float(80.0)
        if r < 40:
            if not last_is(WRITHING_MASS_ATTACK_DEBUFF_MOVE):
                return WRITHING_MASS_ATTACK_DEBUFF_MOVE
            if rng_.next_float(1.0) < 0.4 and not last_is(WRITHING_MASS_BIG_HIT_MOVE):
                return WRITHING_MASS_BIG_HIT_MOVE
            r = 40.0 + rng_.next_float(60.0)
        if r < 70:
            if not last_is(WRITHING_MASS_MULTI_HIT_MOVE):
                return WRITHING_MASS_MULTI_HIT_MOVE
            if rng_.next_float(1.0) < 0.3:
                return WRITHING_MASS_ATTACK_BLOCK_MOVE
            if not last_is(WRITHING_MASS_ATTACK_DEBUFF_MOVE):
                return WRITHING_MASS_ATTACK_DEBUFF_MOVE
            return WRITHING_MASS_BIG_HIT_MOVE
        if not last_is(WRITHING_MASS_ATTACK_BLOCK_MOVE):
            return WRITHING_MASS_ATTACK_BLOCK_MOVE
        r2 = rng_.next_float(70.0)
        if r2 < 10 and not last_is(WRITHING_MASS_BIG_HIT_MOVE):
            return WRITHING_MASS_BIG_HIT_MOVE
        if r2 < 40 and not last_is(WRITHING_MASS_ATTACK_DEBUFF_MOVE):
            return WRITHING_MASS_ATTACK_DEBUFF_MOVE
        return WRITHING_MASS_MULTI_HIT_MOVE

    big_hit_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_BIG_HIT_DAMAGE, WRITHING_MASS_BASE_BIG_HIT_DAMAGE)
    multi_hit_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_MULTI_HIT_DAMAGE, WRITHING_MASS_BASE_MULTI_HIT_DAMAGE)
    attack_block_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_ATTACK_BLOCK_DAMAGE, WRITHING_MASS_BASE_ATTACK_BLOCK_DAMAGE)
    attack_debuff_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, WRITHING_MASS_DEADLY_ATTACK_DEBUFF_DAMAGE, WRITHING_MASS_BASE_ATTACK_DEBUFF_DAMAGE)
    states: dict[str, MonsterState] = {
        WRITHING_MASS_BIG_HIT_MOVE: MoveState(WRITHING_MASS_BIG_HIT_MOVE, big_hit, [attack_intent(big_hit_intent)], follow_up_id=WRITHING_MASS_BRANCH),
        WRITHING_MASS_MULTI_HIT_MOVE: MoveState(WRITHING_MASS_MULTI_HIT_MOVE, multi_hit, [multi_attack_intent(multi_hit_intent, WRITHING_MASS_MULTI_HIT_HITS)], follow_up_id=WRITHING_MASS_BRANCH),
        WRITHING_MASS_ATTACK_BLOCK_MOVE: MoveState(WRITHING_MASS_ATTACK_BLOCK_MOVE, attack_block, [attack_intent(attack_block_intent), defend_intent()], follow_up_id=WRITHING_MASS_BRANCH),
        WRITHING_MASS_ATTACK_DEBUFF_MOVE: MoveState(WRITHING_MASS_ATTACK_DEBUFF_MOVE, attack_debuff, [attack_intent(attack_debuff_intent), debuff_intent()], follow_up_id=WRITHING_MASS_BRANCH),
        WRITHING_MASS_MEGA_DEBUFF_MOVE: MoveState(WRITHING_MASS_MEGA_DEBUFF_MOVE, mega_debuff, [debuff_intent()], follow_up_id=WRITHING_MASS_BRANCH),
        WRITHING_MASS_BRANCH: BranchState(WRITHING_MASS_BRANCH, chooser),
        WRITHING_MASS_FIRST_MOVE_BRANCH: BranchState(WRITHING_MASS_FIRST_MOVE_BRANCH, first_move_chooser),
    }
    return creature, MonsterAI(states, WRITHING_MASS_FIRST_MOVE_BRANCH, rng)


# ---- SpireGrowth (HP 170-190) ----

SPIRE_GROWTH_MONSTER_ID = "THEBEYOND_SPIRE_GROWTH"
SPIRE_GROWTH_BASE_MIN_HP = 170
SPIRE_GROWTH_BASE_MAX_HP = 190
SPIRE_GROWTH_TOUGH_MIN_HP = 170
SPIRE_GROWTH_TOUGH_MAX_HP = 190
SPIRE_GROWTH_BASE_TACKLE_DAMAGE = 16
SPIRE_GROWTH_DEADLY_TACKLE_DAMAGE = 18
SPIRE_GROWTH_BASE_SMASH_DAMAGE = 22
SPIRE_GROWTH_DEADLY_SMASH_DAMAGE = 25
SPIRE_GROWTH_BASE_CONSTRICT_AMOUNT = 10
SPIRE_GROWTH_DEADLY_CONSTRICT_AMOUNT = 12
SPIRE_GROWTH_QUICK_TACKLE_MOVE = "QUICK_TACKLE"
SPIRE_GROWTH_CONSTRICT_MOVE = "CONSTRICT"
SPIRE_GROWTH_SMASH_MOVE = "SMASH"
SPIRE_GROWTH_BRANCH = "SPIRE_GROWTH_BRANCH"


def create_spire_growth(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_TOUGH_MIN_HP, SPIRE_GROWTH_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_TOUGH_MAX_HP, SPIRE_GROWTH_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SPIRE_GROWTH_MONSTER_ID)

    def quick_tackle(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_DEADLY_TACKLE_DAMAGE, SPIRE_GROWTH_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def constrict(combat: "CombatState") -> None:
        amount = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_DEADLY_CONSTRICT_AMOUNT, SPIRE_GROWTH_BASE_CONSTRICT_AMOUNT)
        for target in living_player_targets(combat):
            existing = target.powers.get(PowerId.CONSTRICTED)
            if existing is not None:
                target.apply_power(PowerId.CONSTRICTED, amount, applier=creature)
            else:
                target.powers[PowerId.CONSTRICTED] = ConstrictedPowerCls(amount)
                target.powers[PowerId.CONSTRICTED].applier = creature

    def smash(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_DEADLY_SMASH_DAMAGE, SPIRE_GROWTH_BASE_SMASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        targets = living_player_targets(combat_ref["combat"]) if combat_ref["combat"] is not None else []
        player_constricted = any(t.powers.get(PowerId.CONSTRICTED) is not None for t in targets)
        if not player_constricted and not last_is(SPIRE_GROWTH_CONSTRICT_MOVE):
            return SPIRE_GROWTH_CONSTRICT_MOVE
        if rng_.next_float(100.0) < 50 and not last_two_is(SPIRE_GROWTH_QUICK_TACKLE_MOVE):
            return SPIRE_GROWTH_QUICK_TACKLE_MOVE
        if not player_constricted and not last_is(SPIRE_GROWTH_CONSTRICT_MOVE):
            return SPIRE_GROWTH_CONSTRICT_MOVE
        if not last_two_is(SPIRE_GROWTH_SMASH_MOVE):
            return SPIRE_GROWTH_SMASH_MOVE
        return SPIRE_GROWTH_QUICK_TACKLE_MOVE

    # Chooser needs live access to the CombatState (to check the player's
    # current Constricted status), but the first branch resolution happens
    # before this creature is added to combat -- ``combat_ref`` is filled in
    # by the encounter setup / first move roll, matching how Centurion/
    # Mystic (thecity.py) thread an optional combat reference through.
    combat_ref: dict[str, "CombatState | None"] = {"combat": None}

    def wrapped_chooser(state_log: list[str], rng_: Rng) -> str:
        combat_ref["combat"] = creature.combat_state or combat_ref["combat"]
        return chooser(state_log, rng_)

    tackle_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_DEADLY_TACKLE_DAMAGE, SPIRE_GROWTH_BASE_TACKLE_DAMAGE)
    smash_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_GROWTH_DEADLY_SMASH_DAMAGE, SPIRE_GROWTH_BASE_SMASH_DAMAGE)
    states: dict[str, MonsterState] = {
        SPIRE_GROWTH_QUICK_TACKLE_MOVE: MoveState(SPIRE_GROWTH_QUICK_TACKLE_MOVE, quick_tackle, [attack_intent(tackle_intent)], follow_up_id=SPIRE_GROWTH_BRANCH),
        SPIRE_GROWTH_CONSTRICT_MOVE: MoveState(SPIRE_GROWTH_CONSTRICT_MOVE, constrict, [debuff_intent()], follow_up_id=SPIRE_GROWTH_BRANCH),
        SPIRE_GROWTH_SMASH_MOVE: MoveState(SPIRE_GROWTH_SMASH_MOVE, smash, [attack_intent(smash_intent)], follow_up_id=SPIRE_GROWTH_BRANCH),
        SPIRE_GROWTH_BRANCH: BranchState(SPIRE_GROWTH_BRANCH, wrapped_chooser),
    }
    return creature, MonsterAI(states, SPIRE_GROWTH_BRANCH, rng)


# ---- Maw (HP fixed 300, no ascension scaling) ----

MAW_MONSTER_ID = "THEBEYOND_MAW"
MAW_HP = 300
MAW_BASE_SLAM_DAMAGE = 25
MAW_DEADLY_SLAM_DAMAGE = 30
MAW_BASE_STR_UP = 3
MAW_DEADLY_STR_UP = 5
MAW_BASE_TERRIFY_DURATION = 3
MAW_DEADLY_TERRIFY_DURATION = 5
MAW_NOM_DAMAGE = 5  # flat
MAW_ROAR_MOVE = "ROAR"
MAW_SLAM_MOVE = "SLAM"
MAW_DROOL_MOVE = "DROOL"
MAW_NOM_SINGLE_MOVE = "NOMNOMNOM_SINGLE"
MAW_NOM_MULTI_MOVE = "NOMNOMNOM_MULTI"
MAW_BRANCH = "MAW_BRANCH"


def create_maw(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    creature = Creature(max_hp=MAW_HP, monster_id=MAW_MONSTER_ID)
    state = {"roared": False, "turn_count": 1}

    def roar(combat: "CombatState") -> None:
        duration = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MAW_DEADLY_TERRIFY_DURATION, MAW_BASE_TERRIFY_DURATION)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, duration, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, duration, applier=creature)
        state["roared"] = True

    def slam(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MAW_DEADLY_SLAM_DAMAGE, MAW_BASE_SLAM_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def drool(combat: "CombatState") -> None:
        str_up = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, MAW_DEADLY_STR_UP, MAW_BASE_STR_UP)
        creature.apply_power(PowerId.STRENGTH, str_up, applier=creature)

    def nom_nom_nom(combat: "CombatState") -> None:
        hits = max(1, state["turn_count"] // 2)
        _deal_damage_to_player(combat, creature, MAW_NOM_DAMAGE, hits=hits)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        state["turn_count"] += 1
        if not state["roared"]:
            return MAW_ROAR_MOVE
        nom_hit_count = state["turn_count"] // 2
        last_was_nom = bool(state_log) and state_log[-1] in (MAW_NOM_SINGLE_MOVE, MAW_NOM_MULTI_MOVE)
        r = rng_.next_float(100.0)
        if r < 50 and not last_was_nom:
            return MAW_NOM_SINGLE_MOVE if nom_hit_count <= 1 else MAW_NOM_MULTI_MOVE
        last_is_slam = bool(state_log) and state_log[-1] == MAW_SLAM_MOVE
        if not last_is_slam and not last_was_nom:
            return MAW_SLAM_MOVE
        return MAW_DROOL_MOVE

    states: dict[str, MonsterState] = {
        MAW_ROAR_MOVE: MoveState(MAW_ROAR_MOVE, roar, [debuff_intent()], follow_up_id=MAW_BRANCH),
        MAW_SLAM_MOVE: MoveState(MAW_SLAM_MOVE, slam, [attack_intent(_ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, MAW_DEADLY_SLAM_DAMAGE, MAW_BASE_SLAM_DAMAGE))], follow_up_id=MAW_BRANCH),
        MAW_DROOL_MOVE: MoveState(MAW_DROOL_MOVE, drool, [buff_intent()], follow_up_id=MAW_BRANCH),
        MAW_NOM_SINGLE_MOVE: MoveState(MAW_NOM_SINGLE_MOVE, nom_nom_nom, [attack_intent(MAW_NOM_DAMAGE)], follow_up_id=MAW_BRANCH),
        # Hit count grows over the fight (turn_count // 2); the declared
        # intent below is a snapshot at creation time, like Hexaghost's
        # Divider placeholder intent in exordium.py.
        MAW_NOM_MULTI_MOVE: MoveState(MAW_NOM_MULTI_MOVE, nom_nom_nom, [multi_attack_intent(MAW_NOM_DAMAGE, 2)], follow_up_id=MAW_BRANCH),
        MAW_BRANCH: BranchState(MAW_BRANCH, chooser),
    }
    return creature, MonsterAI(states, MAW_BRANCH, rng)


# ---- Transient (HP fixed 999 -- self-timer, not damage, is the real kill) ----

TRANSIENT_MONSTER_ID = "THEBEYOND_TRANSIENT"
TRANSIENT_HP = 999
TRANSIENT_BASE_FADING_TURNS = 5
TRANSIENT_TOUGH_FADING_TURNS = 6
TRANSIENT_BASE_STARTING_DAMAGE = 30
TRANSIENT_DEADLY_STARTING_DAMAGE = 40
TRANSIENT_DAMAGE_INCREMENT = 10
TRANSIENT_ATTACK_MOVE = "ATTACK"


def create_transient(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    creature = Creature(max_hp=TRANSIENT_HP, monster_id=TRANSIENT_MONSTER_ID)

    fading_turns = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TRANSIENT_TOUGH_FADING_TURNS, TRANSIENT_BASE_FADING_TURNS)
    # FADING isn't a distinct PowerId in this port -- Transient's self-timer
    # (C# ref: FadingPower.cs -- BeforeSideTurnEndEarly on owner's own side:
    # decrement, or kill self at 0) is implemented directly below via a
    # plain closure counter instead of a dedicated power, since (unlike
    # LifeLink/Constricted/Reactive/Shifting) nothing else in TheBeyond
    # needs a reusable "kill self after N of my own turns" power and
    # PowerInstance has no built-in "kill owner" primitive worth
    # generalizing for a single user.
    state = {"turns_left": fading_turns, "count": 0}
    starting_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, TRANSIENT_DEADLY_STARTING_DAMAGE, TRANSIENT_BASE_STARTING_DAMAGE)
    creature.powers[PowerId.SHIFTING] = ShiftingPowerCls(1)

    def current_damage() -> int:
        return starting_damage + state["count"] * TRANSIENT_DAMAGE_INCREMENT

    def attack(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, current_damage())
        state["count"] += 1
        if not creature.is_alive:
            return
        state["turns_left"] -= 1
        if state["turns_left"] <= 0:
            combat.kill_creature(creature)

    states: dict[str, MonsterState] = {
        # Damage grows every turn (starting_damage + 10*count); the declared
        # intent is a creation-time snapshot, matching Hexaghost's/Maw's
        # placeholder-intent convention elsewhere in this port.
        TRANSIENT_ATTACK_MOVE: MoveState(TRANSIENT_ATTACK_MOVE, attack, [attack_intent(starting_damage)], follow_up_id=TRANSIENT_ATTACK_MOVE),
    }
    return creature, MonsterAI(states, TRANSIENT_ATTACK_MOVE, rng)


# ========================================================================
# JawWorm "Hard Mode" wrapper (TheBeyond's JawWormHordeNormal only)
# ========================================================================

def create_jaw_worm_hard_mode(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    """Reuses Exordium's ``create_jaw_worm`` (same monster, same
    ``JawWorm.cs`` source file backs both Exordium's ``JawWormWeak`` and
    TheBeyond's ``JawWormHordeNormal``) but applies the ``HardMode=true``
    behavior from that shared source file directly against the returned
    creature/AI, since ``create_jaw_worm`` itself can't be edited here
    (Exordium's file is out of scope for this task):

    - ``AfterAddedToRoom`` (HardMode): immediately gain Strength = Bellow's
      Strength amount.
    - ``BeforeSideTurnStart`` (HardMode, first PLAYER turn only): silently
      gain Block = Bellow's Block amount before the player ever acts.
      Simplified to a direct ``creature.block`` assignment at creation time
      (before this creature has even been added to combat) rather than a
      dedicated one-shot power, since no combat/hook context exists yet at
      this point and no relic/power in this simulator would plausibly
      modify a monster's own opening block gain.
    - ``GenerateMoveStateMachine`` (HardMode): the state machine's entry
      point is the random branch directly (skips the forced opening CHOMP
      normal JawWorm always uses) -- achieved by constructing a second
      ``MonsterAI`` over the SAME ``states`` dict Exordium's JawWorm AI
      already built, just rooted at ``JAW_WORM_BRANCH`` instead of CHOMP.
    """
    creature, ai = create_jaw_worm(rng, ascension_level=ascension_level, hard_mode=True)

    strength = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_BELLOW_STRENGTH, JAW_WORM_BASE_BELLOW_STRENGTH)
    block = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_BELLOW_BLOCK, JAW_WORM_BASE_BELLOW_BLOCK)
    creature.apply_power(PowerId.STRENGTH, strength, applier=creature)
    creature.block += block

    hard_mode_ai = MonsterAI(ai.states, JAW_WORM_BRANCH, rng)
    return creature, hard_mode_ai


# ========================================================================
# ELITE MONSTERS
# ========================================================================

# ---- GiantHead (HP fixed 500 / 520 asc) ----

GIANT_HEAD_MONSTER_ID = "THEBEYOND_GIANT_HEAD"
GIANT_HEAD_BASE_HP = 500
GIANT_HEAD_TOUGH_HP = 520
GIANT_HEAD_COUNT_DAMAGE = 13  # flat
GIANT_HEAD_BASE_STARTING_DEATH_DMG = 30
GIANT_HEAD_DEADLY_STARTING_DEATH_DMG = 40
GIANT_HEAD_DEATH_DMG_STEP = 5
GIANT_HEAD_BASE_START_COUNT = 5
GIANT_HEAD_TOUGH_START_COUNT = 4
GIANT_HEAD_MIN_COUNT = -6
GIANT_HEAD_GLARE_MOVE = "GLARE"
GIANT_HEAD_IT_IS_TIME_MOVE = "IT_IS_TIME"
GIANT_HEAD_COUNT_MOVE = "COUNT"
GIANT_HEAD_BRANCH = "GIANT_HEAD_BRANCH"


def create_giant_head(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GIANT_HEAD_TOUGH_HP, GIANT_HEAD_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=GIANT_HEAD_MONSTER_ID)
    creature.apply_power(PowerId.SLOW, 1, applier=creature)

    starting_count = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GIANT_HEAD_TOUGH_START_COUNT, GIANT_HEAD_BASE_START_COUNT)
    starting_death_dmg = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GIANT_HEAD_DEADLY_STARTING_DEATH_DMG, GIANT_HEAD_BASE_STARTING_DEATH_DMG)
    state = {"count": starting_count}

    def it_is_time_damage() -> int:
        return starting_death_dmg - state["count"] * GIANT_HEAD_DEATH_DMG_STEP

    def glare(combat: "CombatState") -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, 1, applier=creature)

    def it_is_time(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, it_is_time_damage())

    def count_move(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, GIANT_HEAD_COUNT_DAMAGE)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        if state["count"] <= 1:
            if state["count"] > GIANT_HEAD_MIN_COUNT:
                state["count"] -= 1
            return GIANT_HEAD_IT_IS_TIME_MOVE
        state["count"] -= 1
        r = rng_.next_float(100.0)
        if r < 50:
            return GIANT_HEAD_GLARE_MOVE if not last_two_is(GIANT_HEAD_GLARE_MOVE) else GIANT_HEAD_COUNT_MOVE
        return GIANT_HEAD_COUNT_MOVE if not last_two_is(GIANT_HEAD_COUNT_MOVE) else GIANT_HEAD_GLARE_MOVE

    states: dict[str, MonsterState] = {
        GIANT_HEAD_GLARE_MOVE: MoveState(GIANT_HEAD_GLARE_MOVE, glare, [debuff_intent()], follow_up_id=GIANT_HEAD_BRANCH),
        # Damage rises as ``count`` counts down past 0 toward -6; the
        # declared intent is a creation-time snapshot (see Hexaghost's
        # Divider placeholder-intent convention in exordium.py).
        GIANT_HEAD_IT_IS_TIME_MOVE: MoveState(GIANT_HEAD_IT_IS_TIME_MOVE, it_is_time, [attack_intent(it_is_time_damage())], follow_up_id=GIANT_HEAD_BRANCH),
        GIANT_HEAD_COUNT_MOVE: MoveState(GIANT_HEAD_COUNT_MOVE, count_move, [attack_intent(GIANT_HEAD_COUNT_DAMAGE), debuff_intent()], follow_up_id=GIANT_HEAD_BRANCH),
        GIANT_HEAD_BRANCH: BranchState(GIANT_HEAD_BRANCH, chooser),
    }
    return creature, MonsterAI(states, GIANT_HEAD_BRANCH, rng)


# ---- Nemesis (HP fixed 185 (asc<8) / 200 (asc>=8)) ----

NEMESIS_MONSTER_ID = "THEBEYOND_NEMESIS"
NEMESIS_BASE_HP = 185
NEMESIS_TOUGH_HP = 200
NEMESIS_BASE_FIRE_DAMAGE = 6
NEMESIS_DEADLY_FIRE_DAMAGE = 7
NEMESIS_TRI_ATTACK_HITS = 3
NEMESIS_SCYTHE_DAMAGE = 45  # flat
NEMESIS_SCYTHE_COOLDOWN = 2
NEMESIS_BURN_AMOUNT = 5
NEMESIS_TRI_ATTACK_MOVE = "TRI_ATTACK"
NEMESIS_SCYTHE_MOVE = "SCYTHE"
NEMESIS_TRI_BURN_MOVE = "TRI_BURN"
NEMESIS_BRANCH = "NEMESIS_BRANCH"
NEMESIS_FIRST_MOVE_BRANCH = "NEMESIS_FIRST_MOVE_BRANCH"


def create_nemesis(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, NEMESIS_TOUGH_HP, NEMESIS_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=NEMESIS_MONSTER_ID)
    creature.powers[PowerId.NEMESIS_FLICKER] = NemesisFlickerPowerCls(1)

    fire_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, NEMESIS_DEADLY_FIRE_DAMAGE, NEMESIS_BASE_FIRE_DAMAGE)
    state = {"scythe_cooldown": 0}

    def tri_attack(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, fire_damage, hits=NEMESIS_TRI_ATTACK_HITS)

    def scythe(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, NEMESIS_SCYTHE_DAMAGE)

    def tri_burn(combat: "CombatState") -> None:
        add_generated_cards_to_living_player_discards(combat, make_burn, NEMESIS_BURN_AMOUNT)

    def first_move_chooser(state_log: list[str], rng_: Rng) -> str:
        state["scythe_cooldown"] -= 1
        return NEMESIS_TRI_ATTACK_MOVE if rng_.next_float(100.0) < 50 else NEMESIS_TRI_BURN_MOVE

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        state["scythe_cooldown"] -= 1
        r = rng_.next_float(100.0)
        if r < 30:
            if not last_is(NEMESIS_SCYTHE_MOVE) and state["scythe_cooldown"] <= 0:
                state["scythe_cooldown"] = NEMESIS_SCYTHE_COOLDOWN
                return NEMESIS_SCYTHE_MOVE
            if rng_.next_float(1.0) < 0.5:
                return NEMESIS_TRI_ATTACK_MOVE if not last_two_is(NEMESIS_TRI_ATTACK_MOVE) else NEMESIS_TRI_BURN_MOVE
            return NEMESIS_TRI_BURN_MOVE if not last_is(NEMESIS_TRI_BURN_MOVE) else NEMESIS_TRI_ATTACK_MOVE
        if r < 65:
            if not last_two_is(NEMESIS_TRI_ATTACK_MOVE):
                return NEMESIS_TRI_ATTACK_MOVE
            if rng_.next_float(1.0) < 0.5:
                if state["scythe_cooldown"] <= 0:
                    state["scythe_cooldown"] = NEMESIS_SCYTHE_COOLDOWN
                    return NEMESIS_SCYTHE_MOVE
                return NEMESIS_TRI_BURN_MOVE
            return NEMESIS_TRI_BURN_MOVE
        if not last_is(NEMESIS_TRI_BURN_MOVE):
            return NEMESIS_TRI_BURN_MOVE
        if rng_.next_float(1.0) < 0.5 and state["scythe_cooldown"] <= 0:
            state["scythe_cooldown"] = NEMESIS_SCYTHE_COOLDOWN
            return NEMESIS_SCYTHE_MOVE
        return NEMESIS_TRI_ATTACK_MOVE

    states: dict[str, MonsterState] = {
        NEMESIS_TRI_ATTACK_MOVE: MoveState(NEMESIS_TRI_ATTACK_MOVE, tri_attack, [multi_attack_intent(fire_damage, NEMESIS_TRI_ATTACK_HITS)], follow_up_id=NEMESIS_BRANCH),
        NEMESIS_SCYTHE_MOVE: MoveState(NEMESIS_SCYTHE_MOVE, scythe, [attack_intent(NEMESIS_SCYTHE_DAMAGE)], follow_up_id=NEMESIS_BRANCH),
        NEMESIS_TRI_BURN_MOVE: MoveState(NEMESIS_TRI_BURN_MOVE, tri_burn, [status_intent()], follow_up_id=NEMESIS_BRANCH),
        NEMESIS_BRANCH: BranchState(NEMESIS_BRANCH, chooser),
        NEMESIS_FIRST_MOVE_BRANCH: BranchState(NEMESIS_FIRST_MOVE_BRANCH, first_move_chooser),
    }
    return creature, MonsterAI(states, NEMESIS_FIRST_MOVE_BRANCH, rng)


# ---- Reptomancer (Elite, HP 180-190 (asc<8) / 190-200 (asc>=8)) ----

REPTOMANCER_MONSTER_ID = "THEBEYOND_REPTOMANCER"
REPTOMANCER_BASE_MIN_HP = 180
REPTOMANCER_BASE_MAX_HP = 190
REPTOMANCER_TOUGH_MIN_HP = 190
REPTOMANCER_TOUGH_MAX_HP = 200
REPTOMANCER_BASE_SNAKE_STRIKE_DAMAGE = 13
REPTOMANCER_DEADLY_SNAKE_STRIKE_DAMAGE = 16
REPTOMANCER_SNAKE_STRIKE_HITS = 2
REPTOMANCER_BASE_BIG_BITE_DAMAGE = 30
REPTOMANCER_DEADLY_BIG_BITE_DAMAGE = 34
REPTOMANCER_MAX_ALIVE_DAGGERS = 4
REPTOMANCER_DAGGERS_PER_SPAWN = 2
REPTOMANCER_SNAKE_STRIKE_MOVE = "SNAKE_STRIKE"
REPTOMANCER_SPAWN_DAGGER_MOVE = "SPAWN_DAGGER"
REPTOMANCER_BIG_BITE_MOVE = "BIG_BITE"
REPTOMANCER_BRANCH = "REPTOMANCER_BRANCH"


def create_reptomancer(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_TOUGH_MIN_HP, REPTOMANCER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_TOUGH_MAX_HP, REPTOMANCER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=REPTOMANCER_MONSTER_ID)

    def num_alive_daggers(combat: "CombatState") -> int:
        return len([e for e in _other_living_enemies(combat, creature) if e.monster_id == SNAKE_DAGGER_MONSTER_ID])

    def can_spawn(combat: "CombatState") -> bool:
        return num_alive_daggers(combat) < REPTOMANCER_MAX_ALIVE_DAGGERS

    def spawn_dagger(combat: "CombatState") -> None:
        empty = REPTOMANCER_MAX_ALIVE_DAGGERS - num_alive_daggers(combat)
        spawned = 0
        while spawned < REPTOMANCER_DAGGERS_PER_SPAWN and spawned < max(0, empty):
            dagger, dagger_ai = create_snake_dagger(Rng(combat.rng.next_int(0, INT_MAX)))
            combat.add_enemy(dagger, dagger_ai)
            dagger.apply_power(PowerId.MINION, 1, applier=creature)
            spawned += 1

    def snake_strike(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_DEADLY_SNAKE_STRIKE_DAMAGE, REPTOMANCER_BASE_SNAKE_STRIKE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=REPTOMANCER_SNAKE_STRIKE_HITS)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, 1, applier=creature)

    def big_bite(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_DEADLY_BIG_BITE_DAMAGE, REPTOMANCER_BASE_BIG_BITE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def select(state_log: list[str], rng_: Rng, combat: "CombatState", lo_bound: float, hi_bound: float) -> str:
        r = lo_bound + rng_.next_float(hi_bound - lo_bound + 1.0)

        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        if r < 33:
            if not last_is(REPTOMANCER_SNAKE_STRIKE_MOVE):
                return REPTOMANCER_SNAKE_STRIKE_MOVE
            return select(state_log, rng_, combat, 33, 99)
        if r < 66:
            if not last_two_is(REPTOMANCER_SPAWN_DAGGER_MOVE) and can_spawn(combat):
                return REPTOMANCER_SPAWN_DAGGER_MOVE
            return REPTOMANCER_SNAKE_STRIKE_MOVE
        if not last_is(REPTOMANCER_BIG_BITE_MOVE):
            return REPTOMANCER_BIG_BITE_MOVE
        return select(state_log, rng_, combat, 0, 65)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        combat = creature.combat_state
        return select(state_log, rng_, combat, 0, 99)

    snake_strike_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_DEADLY_SNAKE_STRIKE_DAMAGE, REPTOMANCER_BASE_SNAKE_STRIKE_DAMAGE)
    big_bite_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, REPTOMANCER_DEADLY_BIG_BITE_DAMAGE, REPTOMANCER_BASE_BIG_BITE_DAMAGE)
    states: dict[str, MonsterState] = {
        REPTOMANCER_SPAWN_DAGGER_MOVE: MoveState(REPTOMANCER_SPAWN_DAGGER_MOVE, spawn_dagger, [Intent(IntentType.SUMMON)], follow_up_id=REPTOMANCER_BRANCH),
        REPTOMANCER_SNAKE_STRIKE_MOVE: MoveState(REPTOMANCER_SNAKE_STRIKE_MOVE, snake_strike, [multi_attack_intent(snake_strike_intent, REPTOMANCER_SNAKE_STRIKE_HITS), debuff_intent()], follow_up_id=REPTOMANCER_BRANCH),
        REPTOMANCER_BIG_BITE_MOVE: MoveState(REPTOMANCER_BIG_BITE_MOVE, big_bite, [attack_intent(big_bite_intent)], follow_up_id=REPTOMANCER_BRANCH),
        REPTOMANCER_BRANCH: BranchState(REPTOMANCER_BRANCH, chooser),
    }
    return creature, MonsterAI(states, REPTOMANCER_SPAWN_DAGGER_MOVE, rng)


# ---- SnakeDagger (Reptomancer's summon; HP fixed 20-25, no ascension) ----

SNAKE_DAGGER_MONSTER_ID = "THEBEYOND_SNAKE_DAGGER"
SNAKE_DAGGER_MIN_HP = 20
SNAKE_DAGGER_MAX_HP = 25
SNAKE_DAGGER_STAB_DAMAGE = 9  # flat
SNAKE_DAGGER_EXPLODE_DAMAGE = 25  # flat
SNAKE_DAGGER_WOUND_STAB_MOVE = "WOUND_STAB"
SNAKE_DAGGER_EXPLODE_MOVE = "EXPLODE"


def create_snake_dagger(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    del ascension_level  # not ascension-scaled per source (fixed HP 20-25)
    hp = rng.next_int(SNAKE_DAGGER_MIN_HP, SNAKE_DAGGER_MAX_HP)
    creature = Creature(max_hp=hp, monster_id=SNAKE_DAGGER_MONSTER_ID)

    def wound_stab(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, SNAKE_DAGGER_STAB_DAMAGE)
        add_generated_cards_to_living_player_discards(combat, make_wound, 1)

    def explode(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, SNAKE_DAGGER_EXPLODE_DAMAGE)
        combat.kill_creature(creature)

    states: dict[str, MonsterState] = {
        SNAKE_DAGGER_WOUND_STAB_MOVE: MoveState(SNAKE_DAGGER_WOUND_STAB_MOVE, wound_stab, [attack_intent(SNAKE_DAGGER_STAB_DAMAGE), status_intent()], follow_up_id=SNAKE_DAGGER_EXPLODE_MOVE),
        SNAKE_DAGGER_EXPLODE_MOVE: MoveState(SNAKE_DAGGER_EXPLODE_MOVE, explode, [Intent(IntentType.DEATH_BLOW, damage=SNAKE_DAGGER_EXPLODE_DAMAGE)], follow_up_id=SNAKE_DAGGER_EXPLODE_MOVE),
    }
    return creature, MonsterAI(states, SNAKE_DAGGER_WOUND_STAB_MOVE, rng)


# ========================================================================
# BOSS MONSTERS
# ========================================================================

# ---- AwakenedOne (HP fixed 300 / 320 asc; two-phase) ----

AWAKENED_ONE_MONSTER_ID = "THEBEYOND_AWAKENED_ONE"
AWAKENED_ONE_BASE_HP = 300
AWAKENED_ONE_TOUGH_HP = 320
AWAKENED_ONE_SLASH_DAMAGE = 20  # flat
AWAKENED_ONE_SOUL_STRIKE_DAMAGE = 6  # flat
AWAKENED_ONE_SOUL_STRIKE_HITS = 4
AWAKENED_ONE_DARK_ECHO_DAMAGE = 40  # flat
AWAKENED_ONE_SLUDGE_DAMAGE = 18  # flat
AWAKENED_ONE_TACKLE_DAMAGE = 10  # flat
AWAKENED_ONE_TACKLE_HITS = 3
AWAKENED_ONE_BASE_REGEN = 10
AWAKENED_ONE_DEADLY_REGEN = 15
AWAKENED_ONE_BASE_CURIOSITY = 1
AWAKENED_ONE_DEADLY_CURIOSITY = 2
AWAKENED_ONE_BASE_STARTING_STRENGTH = 0
AWAKENED_ONE_DEADLY_STARTING_STRENGTH = 2
AWAKENED_ONE_SLASH_MOVE = "SLASH"
AWAKENED_ONE_SOUL_STRIKE_MOVE = "SOUL_STRIKE"
AWAKENED_ONE_REBIRTH_MOVE = "REBIRTH"
AWAKENED_ONE_DARK_ECHO_MOVE = "DARK_ECHO"
AWAKENED_ONE_SLUDGE_MOVE = "SLUDGE"
AWAKENED_ONE_TACKLE_MOVE = "TACKLE"
AWAKENED_ONE_PHASE1_BRANCH = "AWAKENED_ONE_PHASE1_BRANCH"
AWAKENED_ONE_PHASE2_BRANCH = "AWAKENED_ONE_PHASE2_BRANCH"

CULTIST_MONSTER_ID_FOR_ESCAPE = "EXORDIUM_CULTIST"  # matches exordium.create_cultist's monster_id


def create_awakened_one(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, AWAKENED_ONE_TOUGH_HP, AWAKENED_ONE_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=AWAKENED_ONE_MONSTER_ID)

    # Phase 2 HP: C# ``Phase2Hp`` is the SAME ascension-scaled expression as
    # Min/MaxInitialHp, then ``scaledHp = Phase2Hp * Players.Count``; for
    # solo (Players.Count == 1) this is just the same base HP value again.
    phase2_hp = hp

    regen = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, AWAKENED_ONE_DEADLY_REGEN, AWAKENED_ONE_BASE_REGEN)
    curiosity = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, AWAKENED_ONE_DEADLY_CURIOSITY, AWAKENED_ONE_BASE_CURIOSITY)
    starting_strength = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, AWAKENED_ONE_DEADLY_STARTING_STRENGTH, AWAKENED_ONE_BASE_STARTING_STRENGTH)

    # RegenEnemyPower ("heal Amount every own turn end, never decays") is
    # mechanically identical to Act4Heart's existing RegenerateA4hPower --
    # reused directly rather than adding a third near-duplicate non-decaying
    # regen power to this simulator.
    creature.apply_power(PowerId.REGENERATE_A4H, regen, applier=creature)
    creature.powers[PowerId.CURIOSITY] = CuriosityPowerCls(curiosity)
    unawakened = UnawakenedPowerCls(1, AWAKENED_ONE_REBIRTH_MOVE, CULTIST_MONSTER_ID_FOR_ESCAPE)
    creature.powers[PowerId.UNAWAKENED] = unawakened
    if starting_strength > 0:
        creature.apply_power(PowerId.STRENGTH, starting_strength, applier=creature)

    def slash(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, AWAKENED_ONE_SLASH_DAMAGE)

    def soul_strike(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, AWAKENED_ONE_SOUL_STRIKE_DAMAGE, hits=AWAKENED_ONE_SOUL_STRIKE_HITS)

    def rebirth(combat: "CombatState") -> None:
        unawakened.do_revive()
        creature.max_hp = phase2_hp
        creature.current_hp = phase2_hp
        creature._death_processed = False  # noqa: SLF001
        # Strip Curiosity + all debuffs, matching AwakenedOne.RebirthMove's
        # ``Powers.Where(p => p.Type==Debuff || p is CuriosityPower ...)``.
        # Unawakened itself is deliberately kept alive here (see
        # UnawakenedPower's docstring for why) rather than also popped.
        creature.powers.pop(PowerId.CURIOSITY, None)
        for power_id, power in list(creature.powers.items()):
            if getattr(power, "power_type", None) is not None and power.power_type.name == "DEBUFF":
                creature.powers.pop(power_id, None)

    def dark_echo(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, AWAKENED_ONE_DARK_ECHO_DAMAGE)

    def sludge(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, AWAKENED_ONE_SLUDGE_DAMAGE)
        _add_generated_cards_to_living_player_draw(combat, make_void, 1)

    def tackle(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, AWAKENED_ONE_TACKLE_DAMAGE, hits=AWAKENED_ONE_TACKLE_HITS)

    def phase1_chooser(state_log: list[str], rng_: Rng) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        if rng_.next_float(100.0) < 25:
            return AWAKENED_ONE_SOUL_STRIKE_MOVE if not last_is(AWAKENED_ONE_SOUL_STRIKE_MOVE) else AWAKENED_ONE_SLASH_MOVE
        return AWAKENED_ONE_SLASH_MOVE if not last_two_is(AWAKENED_ONE_SLASH_MOVE) else AWAKENED_ONE_SOUL_STRIKE_MOVE

    def phase2_chooser(state_log: list[str], rng_: Rng) -> str:
        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        if rng_.next_float(100.0) < 50:
            return AWAKENED_ONE_SLUDGE_MOVE if not last_two_is(AWAKENED_ONE_SLUDGE_MOVE) else AWAKENED_ONE_TACKLE_MOVE
        return AWAKENED_ONE_TACKLE_MOVE if not last_two_is(AWAKENED_ONE_TACKLE_MOVE) else AWAKENED_ONE_SLUDGE_MOVE

    states: dict[str, MonsterState] = {
        AWAKENED_ONE_SLASH_MOVE: MoveState(AWAKENED_ONE_SLASH_MOVE, slash, [attack_intent(AWAKENED_ONE_SLASH_DAMAGE)], follow_up_id=AWAKENED_ONE_PHASE1_BRANCH),
        AWAKENED_ONE_SOUL_STRIKE_MOVE: MoveState(AWAKENED_ONE_SOUL_STRIKE_MOVE, soul_strike, [multi_attack_intent(AWAKENED_ONE_SOUL_STRIKE_DAMAGE, AWAKENED_ONE_SOUL_STRIKE_HITS)], follow_up_id=AWAKENED_ONE_PHASE1_BRANCH),
        AWAKENED_ONE_REBIRTH_MOVE: MoveState(
            AWAKENED_ONE_REBIRTH_MOVE, rebirth, [Intent(IntentType.HEAL), buff_intent()],
            follow_up_id=AWAKENED_ONE_DARK_ECHO_MOVE, must_perform_once=True,
        ),
        AWAKENED_ONE_DARK_ECHO_MOVE: MoveState(AWAKENED_ONE_DARK_ECHO_MOVE, dark_echo, [attack_intent(AWAKENED_ONE_DARK_ECHO_DAMAGE)], follow_up_id=AWAKENED_ONE_PHASE2_BRANCH),
        AWAKENED_ONE_SLUDGE_MOVE: MoveState(AWAKENED_ONE_SLUDGE_MOVE, sludge, [attack_intent(AWAKENED_ONE_SLUDGE_DAMAGE), status_intent()], follow_up_id=AWAKENED_ONE_PHASE2_BRANCH),
        AWAKENED_ONE_TACKLE_MOVE: MoveState(AWAKENED_ONE_TACKLE_MOVE, tackle, [multi_attack_intent(AWAKENED_ONE_TACKLE_DAMAGE, AWAKENED_ONE_TACKLE_HITS)], follow_up_id=AWAKENED_ONE_PHASE2_BRANCH),
        AWAKENED_ONE_PHASE1_BRANCH: BranchState(AWAKENED_ONE_PHASE1_BRANCH, phase1_chooser),
        AWAKENED_ONE_PHASE2_BRANCH: BranchState(AWAKENED_ONE_PHASE2_BRANCH, phase2_chooser),
    }
    return creature, MonsterAI(states, AWAKENED_ONE_SLASH_MOVE, rng)


# ---- Donu (Boss, paired with Deca; HP fixed 250 / 265 asc) ----

DONU_MONSTER_ID = "THEBEYOND_DONU"
DONU_BASE_HP = 250
DONU_TOUGH_HP = 265
DONU_BASE_BEAM_DAMAGE = 10
DONU_DEADLY_BEAM_DAMAGE = 12
DONU_BEAM_HITS = 2
DONU_BASE_CIRCLE_STRENGTH = 2
DONU_DEADLY_CIRCLE_STRENGTH = 3
DONU_BASE_ARTIFACT = 2
DONU_DEADLY_ARTIFACT = 3
DONU_CIRCLE_MOVE = "CIRCLE_OF_PROTECTION"
DONU_BEAM_MOVE = "BEAM"


def create_donu(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, DONU_TOUGH_HP, DONU_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=DONU_MONSTER_ID)
    artifact = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DONU_DEADLY_ARTIFACT, DONU_BASE_ARTIFACT)
    creature.apply_power(PowerId.ARTIFACT, artifact, applier=creature)

    def circle_of_protection(combat: "CombatState") -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, DONU_DEADLY_CIRCLE_STRENGTH, DONU_BASE_CIRCLE_STRENGTH)
        for ally in [creature] + _other_living_enemies(combat, creature):
            ally.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def beam(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, DONU_DEADLY_BEAM_DAMAGE, DONU_BASE_BEAM_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=DONU_BEAM_HITS)

    beam_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DONU_DEADLY_BEAM_DAMAGE, DONU_BASE_BEAM_DAMAGE)
    states: dict[str, MonsterState] = {
        DONU_CIRCLE_MOVE: MoveState(DONU_CIRCLE_MOVE, circle_of_protection, [buff_intent()], follow_up_id=DONU_BEAM_MOVE),
        DONU_BEAM_MOVE: MoveState(DONU_BEAM_MOVE, beam, [multi_attack_intent(beam_intent, DONU_BEAM_HITS)], follow_up_id=DONU_CIRCLE_MOVE),
    }
    return creature, MonsterAI(states, DONU_CIRCLE_MOVE, rng)


# ---- Deca (Boss, paired with Donu; HP fixed 250 / 265 asc) ----

DECA_MONSTER_ID = "THEBEYOND_DECA"
DECA_BASE_HP = 250
DECA_TOUGH_HP = 265
DECA_BASE_BEAM_DAMAGE = 10
DECA_DEADLY_BEAM_DAMAGE = 12
DECA_BEAM_HITS = 2
DECA_BEAM_DAZED_COUNT = 2
DECA_PROTECT_BLOCK = 16
DECA_PROTECT_PLATED_ARMOR = 3
DECA_BASE_ARTIFACT = 2
DECA_DEADLY_ARTIFACT = 3
DECA_BEAM_MOVE = "BEAM"
DECA_SQUARE_MOVE = "SQUARE_OF_PROTECTION"


def create_deca(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, DECA_TOUGH_HP, DECA_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=DECA_MONSTER_ID)
    artifact = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DECA_DEADLY_ARTIFACT, DECA_BASE_ARTIFACT)
    creature.apply_power(PowerId.ARTIFACT, artifact, applier=creature)

    def beam(combat: "CombatState") -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, DECA_DEADLY_BEAM_DAMAGE, DECA_BASE_BEAM_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=DECA_BEAM_HITS)
        add_generated_cards_to_living_player_discards(combat, make_dazed, DECA_BEAM_DAZED_COUNT)

    def square_of_protection(combat: "CombatState") -> None:
        for ally in [creature] + _other_living_enemies(combat, creature):
            _gain_block(ally, DECA_PROTECT_BLOCK, combat)
            existing = ally.powers.get(PowerId.PLATED_ARMOR)
            if existing is not None:
                ally.apply_power(PowerId.PLATED_ARMOR, DECA_PROTECT_PLATED_ARMOR, applier=creature)
            else:
                ally.powers[PowerId.PLATED_ARMOR] = PlatedArmorPower(DECA_PROTECT_PLATED_ARMOR)

    beam_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, DECA_DEADLY_BEAM_DAMAGE, DECA_BASE_BEAM_DAMAGE)
    states: dict[str, MonsterState] = {
        DECA_BEAM_MOVE: MoveState(DECA_BEAM_MOVE, beam, [multi_attack_intent(beam_intent, DECA_BEAM_HITS), status_intent()], follow_up_id=DECA_SQUARE_MOVE),
        DECA_SQUARE_MOVE: MoveState(DECA_SQUARE_MOVE, square_of_protection, [defend_intent(), buff_intent()], follow_up_id=DECA_BEAM_MOVE),
    }
    return creature, MonsterAI(states, DECA_BEAM_MOVE, rng)


# ---- TimeEater (HP fixed 456 / 480 asc) ----

TIME_EATER_MONSTER_ID = "THEBEYOND_TIME_EATER"
TIME_EATER_BASE_HP = 456
TIME_EATER_TOUGH_HP = 480
TIME_EATER_BASE_REVERB_DAMAGE = 7
TIME_EATER_DEADLY_REVERB_DAMAGE = 8
TIME_EATER_REVERB_HITS = 3
TIME_EATER_RIPPLE_BLOCK = 20
TIME_EATER_BASE_HEAD_SLAM_DAMAGE = 26
TIME_EATER_DEADLY_HEAD_SLAM_DAMAGE = 32
TIME_EATER_HEAD_SLAM_SLIMED_COUNT = 2
TIME_EATER_REVERBERATE_MOVE = "REVERBERATE"
TIME_EATER_RIPPLE_MOVE = "RIPPLE"
TIME_EATER_HEAD_SLAM_MOVE = "HEAD_SLAM"
TIME_EATER_HASTE_MOVE = "HASTE"
TIME_EATER_BRANCH = "TIME_EATER_BRANCH"


def create_time_eater(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, TIME_EATER_TOUGH_HP, TIME_EATER_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=TIME_EATER_MONSTER_ID)

    reverb_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, TIME_EATER_DEADLY_REVERB_DAMAGE, TIME_EATER_BASE_REVERB_DAMAGE)
    head_slam_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, TIME_EATER_DEADLY_HEAD_SLAM_DAMAGE, TIME_EATER_BASE_HEAD_SLAM_DAMAGE)
    state = {"used_haste": False}

    def apply_time_warp(combat: "CombatState") -> None:
        # TimeWarpPower is applied to each PLAYER (not TimeEater itself) --
        # see its docstring in powers/monster.py. Applied once, right after
        # this creature is added to combat, mirroring TimeEater's own
        # ``AfterAddedToRoom``.
        for target in living_player_targets(combat):
            existing = target.powers.get(PowerId.TIME_WARP)
            if existing is None:
                target.powers[PowerId.TIME_WARP] = TimeWarpPowerCls(1)
                target.powers[PowerId.TIME_WARP].applier = creature

    def reverberate(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, reverb_damage, hits=TIME_EATER_REVERB_HITS)

    def ripple(combat: "CombatState") -> None:
        _gain_block(creature, TIME_EATER_RIPPLE_BLOCK, combat)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, 1, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, 1, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, 1, applier=creature)

    def head_slam(combat: "CombatState") -> None:
        _deal_damage_to_player(combat, creature, head_slam_damage)
        for target in living_player_targets(combat):
            existing = target.powers.get(PowerId.DRAW_REDUCTION)
            if existing is not None:
                target.apply_power(PowerId.DRAW_REDUCTION, 1, applier=creature)
            else:
                target.powers[PowerId.DRAW_REDUCTION] = DrawReductionPowerCls(1)
                target.powers[PowerId.DRAW_REDUCTION].applier = creature
        add_generated_cards_to_living_player_discards(combat, make_slimed, TIME_EATER_HEAD_SLAM_SLIMED_COUNT)

    def haste(combat: "CombatState") -> None:
        for power_id, power in list(creature.powers.items()):
            if getattr(power, "power_type", None) is not None and power.power_type.name == "DEBUFF":
                creature.powers.pop(power_id, None)
        heal_amount = creature.max_hp // 2 - creature.current_hp
        if heal_amount > 0:
            creature.heal(heal_amount)
        _gain_block(creature, head_slam_damage, combat)

    def chooser(state_log: list[str], rng_: Rng) -> str:
        def last_is(move_id: str) -> bool:
            return bool(state_log) and state_log[-1] == move_id

        def last_two_is(move_id: str) -> bool:
            return len(state_log) >= 2 and state_log[-1] == move_id and state_log[-2] == move_id

        if creature.current_hp < creature.max_hp / 2 and not state["used_haste"]:
            state["used_haste"] = True
            return TIME_EATER_HASTE_MOVE

        r = rng_.next_float(100.0)
        if r < 45:
            if not last_two_is(TIME_EATER_REVERBERATE_MOVE):
                return TIME_EATER_REVERBERATE_MOVE
            r = 50.0 + rng_.next_float(50.0)
        if r < 80:
            if not last_is(TIME_EATER_HEAD_SLAM_MOVE):
                return TIME_EATER_HEAD_SLAM_MOVE
            return TIME_EATER_REVERBERATE_MOVE if rng_.next_float(1.0) < 0.66 else TIME_EATER_RIPPLE_MOVE
        if not last_is(TIME_EATER_RIPPLE_MOVE):
            return TIME_EATER_RIPPLE_MOVE
        r2 = rng_.next_float(75.0)
        if r2 < 45 and not last_two_is(TIME_EATER_REVERBERATE_MOVE):
            return TIME_EATER_REVERBERATE_MOVE
        if not last_is(TIME_EATER_HEAD_SLAM_MOVE):
            return TIME_EATER_HEAD_SLAM_MOVE
        return TIME_EATER_REVERBERATE_MOVE if rng_.next_float(1.0) < 0.66 else TIME_EATER_RIPPLE_MOVE

    states: dict[str, MonsterState] = {
        TIME_EATER_REVERBERATE_MOVE: MoveState(TIME_EATER_REVERBERATE_MOVE, reverberate, [multi_attack_intent(reverb_damage, TIME_EATER_REVERB_HITS)], follow_up_id=TIME_EATER_BRANCH),
        TIME_EATER_RIPPLE_MOVE: MoveState(TIME_EATER_RIPPLE_MOVE, ripple, [defend_intent(), debuff_intent()], follow_up_id=TIME_EATER_BRANCH),
        TIME_EATER_HEAD_SLAM_MOVE: MoveState(TIME_EATER_HEAD_SLAM_MOVE, head_slam, [attack_intent(head_slam_damage), debuff_intent(), status_intent()], follow_up_id=TIME_EATER_BRANCH),
        TIME_EATER_HASTE_MOVE: MoveState(TIME_EATER_HASTE_MOVE, haste, [buff_intent()], follow_up_id=TIME_EATER_BRANCH),
        TIME_EATER_BRANCH: BranchState(TIME_EATER_BRANCH, chooser),
    }
    ai = MonsterAI(states, TIME_EATER_BRANCH, rng)
    return creature, ai


def apply_time_eater_time_warp(combat: "CombatState", time_eater: Creature) -> None:
    """Call once right after adding TimeEater to combat (mirrors
    ``TimeEater.AfterAddedToRoom``) to apply ``TimeWarpPower`` to every
    living player. Kept as a separate function (rather than inline in
    ``create_time_eater``) since the creator function only receives ``rng``/
    ``ascension_level``, not a live ``CombatState`` -- see
    ``sts2_env/encounters/thebeyond.py``'s ``setup_time_eater_boss`` for the
    call site.
    """
    for target in living_player_targets(combat):
        existing = target.powers.get(PowerId.TIME_WARP)
        if existing is None:
            target.powers[PowerId.TIME_WARP] = TimeWarpPowerCls(1)
            target.powers[PowerId.TIME_WARP].applier = time_eater


# ========================================================================
# Power class imports (deferred to the bottom to avoid a circular import:
# ``sts2_env.powers.monster`` has no dependency on this module, so this is
# purely for readability -- keeps the big docstring/import block above
# focused on monster-building concerns).
# ========================================================================
from sts2_env.powers.monster import (  # noqa: E402
    ConstrictedPower as ConstrictedPowerCls,
    CuriosityPower as CuriosityPowerCls,
    DrawReductionPower as DrawReductionPowerCls,
    LifeLinkPower as LifeLinkPowerCls,
    NemesisFlickerPower as NemesisFlickerPowerCls,
    ReactivePower as ReactivePowerCls,
    ShiftingPower as ShiftingPowerCls,
    TimeWarpPower as TimeWarpPowerCls,
    UnawakenedPower as UnawakenedPowerCls,
)
