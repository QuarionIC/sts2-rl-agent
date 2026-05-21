"""Act 3 (Glory) monsters: weak, normal, elite, boss.

All HP ranges, damage values, and state machines verified against decompiled C# source.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import CombatSide, MoveRepeatType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import Rng
from sts2_env.monsters.intents import (
    Intent, IntentType, attack_intent, multi_attack_intent,
    buff_intent, debuff_intent, strong_debuff_intent, status_intent,
    defend_intent, sleep_intent,
)
from sts2_env.monsters.state_machine import (
    ConditionalBranchState, MonsterAI, MonsterState, MoveState, RandomBranchState,
)
from sts2_env.monsters.block import gain_move_block
from sts2_env.monsters.targets import apply_power_to_living_player_targets, living_player_targets
from sts2_env.cards.status import make_burn, make_dazed, make_slimed

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ---- Helpers ----

TOUGH_ENEMIES_ASCENSION_LEVEL = 8
DEADLY_ENEMIES_ASCENSION_LEVEL = 9


def _ascension_value(ascension_level: int, threshold: int, ascension_value: int, base_value: int) -> int:
    return ascension_value if ascension_level >= threshold else base_value


def _combat_ascension_level(combat: CombatState) -> int:
    return getattr(combat, "ascension_level", 0)


def _deal_damage_to_player(combat: CombatState, creature: Creature, base_dmg: int, hits: int = 1) -> None:
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


def _gain_block(creature: Creature, amount: int, combat: CombatState) -> None:
    gain_move_block(creature, amount, combat)


def _gain_unpowered_block(creature: Creature, amount: int, combat: CombatState) -> None:
    if combat.is_over:
        return
    before = creature.block
    creature.gain_block(amount, unpowered=True)
    gained = creature.block - before
    if gained > 0:
        from sts2_env.core.hooks import fire_after_block_gained

        fire_after_block_gained(creature, gained, combat, ValueProp.UNPOWERED, None)


# ========================================================================
# WEAK ENCOUNTERS
# ========================================================================

# ---- DevotedSculptor (HP 162 / 172 asc) ----

DEVOTED_SCULPTOR_MONSTER_ID = "DEVOTED_SCULPTOR"
DEVOTED_SCULPTOR_BASE_HP = 162
DEVOTED_SCULPTOR_TOUGH_HP = 172
DEVOTED_SCULPTOR_RITUAL_GAIN = 9
DEVOTED_SCULPTOR_BASE_SAVAGE_DAMAGE = 12
DEVOTED_SCULPTOR_DEADLY_SAVAGE_DAMAGE = 15
DEVOTED_SCULPTOR_FORBIDDEN_INCANTATION_MOVE = "FORBIDDEN_INCANTATION_MOVE"
DEVOTED_SCULPTOR_SAVAGE_MOVE = "SAVAGE_MOVE"


def create_devoted_sculptor(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        DEVOTED_SCULPTOR_TOUGH_HP,
        DEVOTED_SCULPTOR_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=DEVOTED_SCULPTOR_MONSTER_ID)

    def forbidden_incantation(combat: CombatState) -> None:
        creature.apply_power(PowerId.RITUAL, DEVOTED_SCULPTOR_RITUAL_GAIN)

    def savage(combat: CombatState) -> None:
        savage_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            DEVOTED_SCULPTOR_DEADLY_SAVAGE_DAMAGE,
            DEVOTED_SCULPTOR_BASE_SAVAGE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, savage_dmg)

    savage_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        DEVOTED_SCULPTOR_DEADLY_SAVAGE_DAMAGE,
        DEVOTED_SCULPTOR_BASE_SAVAGE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        DEVOTED_SCULPTOR_FORBIDDEN_INCANTATION_MOVE: MoveState(
            DEVOTED_SCULPTOR_FORBIDDEN_INCANTATION_MOVE,
            forbidden_incantation,
            [buff_intent()],
            follow_up_id=DEVOTED_SCULPTOR_SAVAGE_MOVE,
        ),
        DEVOTED_SCULPTOR_SAVAGE_MOVE: MoveState(
            DEVOTED_SCULPTOR_SAVAGE_MOVE,
            savage,
            [attack_intent(savage_intent_damage)],
            follow_up_id=DEVOTED_SCULPTOR_SAVAGE_MOVE,
        ),
    }
    return creature, MonsterAI(states, DEVOTED_SCULPTOR_FORBIDDEN_INCANTATION_MOVE)


# ---- ScrollOfBiting (HP 24-26 / 26-28 asc) ----

def create_scroll_of_biting(rng: Rng, starter_move_idx: int = 0) -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(31, 38)
    creature = Creature(max_hp=hp, monster_id="SCROLL_OF_BITING")
    chomp_dmg = 14
    chew_dmg = 5

    def chomp(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, chomp_dmg)

    def chew(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, chew_dmg, hits=2)

    def more_teeth(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, 2, applier=creature)

    rand = RandomBranchState("rand")
    rand.add_branch("CHOMP", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("CHEW", weight=2.0)

    states: dict[str, MonsterState] = {
        "CHOMP": MoveState("CHOMP", chomp, [attack_intent(chomp_dmg)], follow_up_id="MORE_TEETH"),
        "CHEW": MoveState("CHEW", chew, [multi_attack_intent(chew_dmg, 2)], follow_up_id="rand"),
        "MORE_TEETH": MoveState("MORE_TEETH", more_teeth, [buff_intent()], follow_up_id="CHEW"),
        "rand": rand,
    }
    initial = ("CHOMP", "CHEW", "MORE_TEETH")[starter_move_idx % 3]
    creature.apply_power(PowerId.PAPER_CUTS, 2)
    return creature, MonsterAI(states, initial, rng)


# ---- TurretOperator (HP 28-30 / 30-32 asc) ----

TURRET_OPERATOR_MONSTER_ID = "TURRET_OPERATOR"
TURRET_OPERATOR_BASE_HP = 41
TURRET_OPERATOR_TOUGH_HP = 51
TURRET_OPERATOR_BASE_FIRE_DAMAGE = 3
TURRET_OPERATOR_DEADLY_FIRE_DAMAGE = 4
TURRET_OPERATOR_FIRE_REPEAT = 5
TURRET_OPERATOR_RELOAD_STRENGTH = 1
TURRET_OPERATOR_UNLOAD_MOVE_1 = "UNLOAD_MOVE_1"
TURRET_OPERATOR_UNLOAD_MOVE_2 = "UNLOAD_MOVE_2"
TURRET_OPERATOR_RELOAD_MOVE = "RELOAD_MOVE"


def create_turret_operator(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        TURRET_OPERATOR_TOUGH_HP,
        TURRET_OPERATOR_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=TURRET_OPERATOR_MONSTER_ID)

    def unload(combat: CombatState) -> None:
        fire_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            TURRET_OPERATOR_DEADLY_FIRE_DAMAGE,
            TURRET_OPERATOR_BASE_FIRE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, fire_dmg, hits=TURRET_OPERATOR_FIRE_REPEAT)

    def reload(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, TURRET_OPERATOR_RELOAD_STRENGTH, applier=creature)

    fire_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        TURRET_OPERATOR_DEADLY_FIRE_DAMAGE,
        TURRET_OPERATOR_BASE_FIRE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        TURRET_OPERATOR_UNLOAD_MOVE_1: MoveState(
            TURRET_OPERATOR_UNLOAD_MOVE_1,
            unload,
            [multi_attack_intent(fire_intent_damage, TURRET_OPERATOR_FIRE_REPEAT)],
            follow_up_id=TURRET_OPERATOR_UNLOAD_MOVE_2,
        ),
        TURRET_OPERATOR_UNLOAD_MOVE_2: MoveState(
            TURRET_OPERATOR_UNLOAD_MOVE_2,
            unload,
            [multi_attack_intent(fire_intent_damage, TURRET_OPERATOR_FIRE_REPEAT)],
            follow_up_id=TURRET_OPERATOR_RELOAD_MOVE,
        ),
        TURRET_OPERATOR_RELOAD_MOVE: MoveState(
            TURRET_OPERATOR_RELOAD_MOVE,
            reload,
            [buff_intent()],
            follow_up_id=TURRET_OPERATOR_UNLOAD_MOVE_1,
        ),
    }
    return creature, MonsterAI(states, TURRET_OPERATOR_UNLOAD_MOVE_1)


# ========================================================================
# NORMAL ENCOUNTERS
# ========================================================================

# ---- Axebot (HP 40-44 / 42-46 asc) ----

def create_axebot(
    rng: Rng,
    start_with_boot_up: bool = False,
    stock_amount: int | None = None,
) -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(40, 44)
    creature = Creature(max_hp=hp, monster_id="AXEBOT")
    one_two_dmg = 5
    hammer_uppercut_dmg = 8
    hammer_uppercut_debuff = 1
    boot_up_block = 10
    boot_up_strength = 1
    sharpen_strength = 4

    def boot_up(combat: CombatState) -> None:
        _gain_block(creature, boot_up_block, combat)
        creature.apply_power(PowerId.STRENGTH, boot_up_strength)

    def one_two(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, one_two_dmg, hits=2)

    def sharpen(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, sharpen_strength)

    def hammer_uppercut(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, hammer_uppercut_dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, hammer_uppercut_debuff, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, hammer_uppercut_debuff, applier=creature)

    rand = RandomBranchState("RAND_MOVE")
    rand.add_branch("ONE_TWO_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)
    rand.add_branch("SHARPEN_MOVE", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("HAMMER_UPPERCUT_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)

    states: dict[str, MonsterState] = {
        "RAND_MOVE": rand,
        "BOOT_UP_MOVE": MoveState("BOOT_UP_MOVE", boot_up, [defend_intent(), buff_intent()], follow_up_id="RAND_MOVE"),
        "ONE_TWO_MOVE": MoveState("ONE_TWO_MOVE", one_two, [multi_attack_intent(one_two_dmg, 2)], follow_up_id="RAND_MOVE"),
        "SHARPEN_MOVE": MoveState("SHARPEN_MOVE", sharpen, [buff_intent()], follow_up_id="RAND_MOVE"),
        "HAMMER_UPPERCUT_MOVE": MoveState("HAMMER_UPPERCUT_MOVE", hammer_uppercut, [attack_intent(hammer_uppercut_dmg), debuff_intent()], follow_up_id="RAND_MOVE"),
    }

    if stock_amount is None:
        creature.apply_power(PowerId.STOCK, 2)
    elif stock_amount > 0:
        creature.apply_power(PowerId.STOCK, stock_amount)

    initial = "BOOT_UP_MOVE" if start_with_boot_up or stock_amount is not None else "RAND_MOVE"
    return creature, MonsterAI(states, initial, rng)


# ---- Fabricator (HP 150 / 155 asc) + bots ----

ZAPBOT_MONSTER_ID = "ZAPBOT"
ZAPBOT_BASE_MIN_HP = 23
ZAPBOT_BASE_MAX_HP = 28
ZAPBOT_TOUGH_MIN_HP = 24
ZAPBOT_TOUGH_MAX_HP = 29
ZAPBOT_BASE_ZAP_DAMAGE = 14
ZAPBOT_DEADLY_ZAP_DAMAGE = 15
ZAPBOT_HIGH_VOLTAGE_AMOUNT = 2
ZAPBOT_ZAP_MOVE = "ZAP"


def create_zapbot(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        ZAPBOT_TOUGH_MIN_HP,
        ZAPBOT_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        ZAPBOT_TOUGH_MAX_HP,
        ZAPBOT_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=ZAPBOT_MONSTER_ID)

    def zap(combat: CombatState) -> None:
        zap_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            ZAPBOT_DEADLY_ZAP_DAMAGE,
            ZAPBOT_BASE_ZAP_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, zap_dmg)

    zap_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        ZAPBOT_DEADLY_ZAP_DAMAGE,
        ZAPBOT_BASE_ZAP_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        ZAPBOT_ZAP_MOVE: MoveState(
            ZAPBOT_ZAP_MOVE,
            zap,
            [attack_intent(zap_intent_damage)],
            follow_up_id=ZAPBOT_ZAP_MOVE,
        ),
    }
    return creature, MonsterAI(states, ZAPBOT_ZAP_MOVE)


STABBOT_MONSTER_ID = "STABBOT"
STABBOT_BASE_MIN_HP = 23
STABBOT_BASE_MAX_HP = 28
STABBOT_TOUGH_MIN_HP = 24
STABBOT_TOUGH_MAX_HP = 29
STABBOT_BASE_STAB_DAMAGE = 11
STABBOT_DEADLY_STAB_DAMAGE = 12
STABBOT_STAB_FRAIL = 1
STABBOT_STAB_MOVE = "STAB_MOVE"


def create_stabbot(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        STABBOT_TOUGH_MIN_HP,
        STABBOT_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        STABBOT_TOUGH_MAX_HP,
        STABBOT_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=STABBOT_MONSTER_ID)

    def stab(combat: CombatState) -> None:
        stab_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            STABBOT_DEADLY_STAB_DAMAGE,
            STABBOT_BASE_STAB_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, stab_dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, STABBOT_STAB_FRAIL, applier=creature)

    stab_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        STABBOT_DEADLY_STAB_DAMAGE,
        STABBOT_BASE_STAB_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        STABBOT_STAB_MOVE: MoveState(
            STABBOT_STAB_MOVE,
            stab,
            [attack_intent(stab_intent_damage), debuff_intent()],
            follow_up_id=STABBOT_STAB_MOVE,
        ),
    }
    return creature, MonsterAI(states, STABBOT_STAB_MOVE)


GUARDBOT_MONSTER_ID = "GUARDBOT"
GUARDBOT_BASE_MIN_HP = 21
GUARDBOT_BASE_MAX_HP = 25
GUARDBOT_TOUGH_MIN_HP = 22
GUARDBOT_TOUGH_MAX_HP = 26
GUARDBOT_GUARD_BLOCK = 15
GUARDBOT_GUARD_MOVE = "GUARD_MOVE"


def create_guardbot(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GUARDBOT_TOUGH_MIN_HP,
        GUARDBOT_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GUARDBOT_TOUGH_MAX_HP,
        GUARDBOT_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=GUARDBOT_MONSTER_ID)

    def guard(combat: CombatState) -> None:
        for enemy in combat.alive_enemies:
            if enemy.monster_id == FABRICATOR_MONSTER_ID:
                _gain_unpowered_block(enemy, GUARDBOT_GUARD_BLOCK, combat)

    states: dict[str, MonsterState] = {
        GUARDBOT_GUARD_MOVE: MoveState(
            GUARDBOT_GUARD_MOVE,
            guard,
            [defend_intent()],
            follow_up_id=GUARDBOT_GUARD_MOVE,
        ),
    }
    return creature, MonsterAI(states, GUARDBOT_GUARD_MOVE)


NOISEBOT_MONSTER_ID = "NOISEBOT"
NOISEBOT_BASE_MIN_HP = 23
NOISEBOT_BASE_MAX_HP = 28
NOISEBOT_TOUGH_MIN_HP = 24
NOISEBOT_TOUGH_MAX_HP = 29
NOISEBOT_DAZED_TO_DISCARD = 1
NOISEBOT_DAZED_TO_DRAW = 1
NOISEBOT_NOISE_MOVE = "NOISE_MOVE"


def create_noisebot(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        NOISEBOT_TOUGH_MIN_HP,
        NOISEBOT_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        NOISEBOT_TOUGH_MAX_HP,
        NOISEBOT_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=NOISEBOT_MONSTER_ID)

    def noise(combat: CombatState) -> None:
        for target in living_player_targets(combat):
            for _ in range(NOISEBOT_DAZED_TO_DISCARD):
                combat.add_generated_card_to_creature_discard(
                    target,
                    make_dazed(),
                    added_by_player=False,
                )
            for _ in range(NOISEBOT_DAZED_TO_DRAW):
                combat.add_generated_card_to_creature_draw_pile(
                    target,
                    make_dazed(),
                    added_by_player=False,
                    random_position=True,
                )

    states: dict[str, MonsterState] = {
        NOISEBOT_NOISE_MOVE: MoveState(
            NOISEBOT_NOISE_MOVE,
            noise,
            [status_intent()],
            follow_up_id=NOISEBOT_NOISE_MOVE,
        ),
    }
    return creature, MonsterAI(states, NOISEBOT_NOISE_MOVE)


FABRICATOR_MONSTER_ID = "FABRICATOR"
FABRICATOR_BASE_HP = 150
FABRICATOR_TOUGH_HP = 155
FABRICATOR_BASE_FABRICATING_STRIKE_DAMAGE = 18
FABRICATOR_DEADLY_FABRICATING_STRIKE_DAMAGE = 21
FABRICATOR_BASE_DISINTEGRATE_DAMAGE = 11
FABRICATOR_DEADLY_DISINTEGRATE_DAMAGE = 13
FABRICATOR_MAX_LIVING_TEAMMATES_FOR_FABRICATE = 4
FABRICATOR_MINION_AMOUNT = 1
FABRICATOR_LAST_SPAWNED_CREATOR_KEY = "last_spawned_creator"
FABRICATOR_FABRICATE_BRANCH = "fabricateBranch"
FABRICATOR_RAND_MOVE = "RAND"
FABRICATOR_FABRICATE_MOVE = "FABRICATE_MOVE"
FABRICATOR_FABRICATING_STRIKE_MOVE = "FABRICATING_STRIKE_MOVE"
FABRICATOR_DISINTEGRATE_MOVE = "DISINTEGRATE_MOVE"


def create_fabricator(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FABRICATOR_TOUGH_HP,
        FABRICATOR_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=FABRICATOR_MONSTER_ID)

    _state = {FABRICATOR_LAST_SPAWNED_CREATOR_KEY: None}

    aggro_creators = [create_zapbot, create_stabbot]
    defense_creators = [create_guardbot, create_noisebot]

    def _spawn_bot(combat: CombatState, creators) -> None:
        options = [
            creator for creator in creators
            if creator is not _state[FABRICATOR_LAST_SPAWNED_CREATOR_KEY]
        ]
        creator = rng.choice(options)
        _state[FABRICATOR_LAST_SPAWNED_CREATOR_KEY] = creator
        bot, bot_ai = creator(rng, ascension_level=_combat_ascension_level(combat))
        combat.add_enemy(bot, bot_ai)
        combat.apply_power_to(bot, PowerId.MINION, FABRICATOR_MINION_AMOUNT, applier=creature)

    def _spawn_aggro(combat: CombatState) -> None:
        _spawn_bot(combat, aggro_creators)

    def _spawn_defense(combat: CombatState) -> None:
        _spawn_bot(combat, defense_creators)

    def fabricate(combat: CombatState) -> None:
        _spawn_defense(combat)
        _spawn_aggro(combat)

    def fabricating_strike(combat: CombatState) -> None:
        fabricating_strike_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FABRICATOR_DEADLY_FABRICATING_STRIKE_DAMAGE,
            FABRICATOR_BASE_FABRICATING_STRIKE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, fabricating_strike_dmg)
        _spawn_aggro(combat)

    def disintegrate(combat: CombatState) -> None:
        disintegrate_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FABRICATOR_DEADLY_DISINTEGRATE_DAMAGE,
            FABRICATOR_BASE_DISINTEGRATE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, disintegrate_dmg)

    def _can_fabricate() -> bool:
        combat = creature.combat_state
        if combat is None:
            return True
        alive_teammates = [
            enemy for enemy in combat.alive_enemies
            if enemy is not creature and enemy.side == creature.side
        ]
        return len(alive_teammates) < FABRICATOR_MAX_LIVING_TEAMMATES_FOR_FABRICATE

    fabricating_strike_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FABRICATOR_DEADLY_FABRICATING_STRIKE_DAMAGE,
        FABRICATOR_BASE_FABRICATING_STRIKE_DAMAGE,
    )
    disintegrate_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FABRICATOR_DEADLY_DISINTEGRATE_DAMAGE,
        FABRICATOR_BASE_DISINTEGRATE_DAMAGE,
    )

    fab_branch = ConditionalBranchState(FABRICATOR_FABRICATE_BRANCH)
    fab_branch.add_branch(_can_fabricate, FABRICATOR_RAND_MOVE)
    fab_branch.add_branch(lambda: True, FABRICATOR_DISINTEGRATE_MOVE)

    fab_rand = RandomBranchState(FABRICATOR_RAND_MOVE)
    fab_rand.add_branch(FABRICATOR_FABRICATE_MOVE)
    fab_rand.add_branch(FABRICATOR_FABRICATING_STRIKE_MOVE)

    states: dict[str, MonsterState] = {
        FABRICATOR_FABRICATE_BRANCH: fab_branch,
        FABRICATOR_RAND_MOVE: fab_rand,
        FABRICATOR_FABRICATE_MOVE: MoveState(
            FABRICATOR_FABRICATE_MOVE,
            fabricate,
            [Intent(IntentType.SUMMON)],
            follow_up_id=FABRICATOR_FABRICATE_BRANCH,
        ),
        FABRICATOR_FABRICATING_STRIKE_MOVE: MoveState(
            FABRICATOR_FABRICATING_STRIKE_MOVE,
            fabricating_strike,
            [attack_intent(fabricating_strike_intent_damage), Intent(IntentType.SUMMON)],
            follow_up_id=FABRICATOR_FABRICATE_BRANCH,
        ),
        FABRICATOR_DISINTEGRATE_MOVE: MoveState(
            FABRICATOR_DISINTEGRATE_MOVE,
            disintegrate,
            [attack_intent(disintegrate_intent_damage)],
            follow_up_id=FABRICATOR_FABRICATE_BRANCH,
        ),
    }
    return creature, MonsterAI(states, FABRICATOR_FABRICATE_BRANCH, rng)


# ---- FrogKnight (HP 191 / 199 asc) ----

FROG_KNIGHT_MONSTER_ID = "FROG_KNIGHT"
FROG_KNIGHT_BASE_HP = 191
FROG_KNIGHT_TOUGH_HP = 199
FROG_KNIGHT_BASE_STRIKE_DOWN_EVIL_DAMAGE = 21
FROG_KNIGHT_DEADLY_STRIKE_DOWN_EVIL_DAMAGE = 23
FROG_KNIGHT_BASE_TONGUE_LASH_DAMAGE = 13
FROG_KNIGHT_DEADLY_TONGUE_LASH_DAMAGE = 14
FROG_KNIGHT_TONGUE_LASH_FRAIL = 2
FROG_KNIGHT_BASE_BEETLE_CHARGE_DAMAGE = 35
FROG_KNIGHT_DEADLY_BEETLE_CHARGE_DAMAGE = 40
FROG_KNIGHT_FOR_THE_QUEEN_STRENGTH = 5
FROG_KNIGHT_BASE_PLATING = 15
FROG_KNIGHT_TOUGH_PLATING = 19
FROG_KNIGHT_BEETLE_CHARGED_KEY = "beetle_charged"
FROG_KNIGHT_TONGUE_LASH_MOVE = "TONGUE_LASH"
FROG_KNIGHT_STRIKE_DOWN_EVIL_MOVE = "STRIKE_DOWN_EVIL"
FROG_KNIGHT_FOR_THE_QUEEN_MOVE = "FOR_THE_QUEEN"
FROG_KNIGHT_HALF_HEALTH_BRANCH = "HALF_HEALTH"
FROG_KNIGHT_BEETLE_CHARGE_MOVE = "BEETLE_CHARGE"


def create_frog_knight(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FROG_KNIGHT_TOUGH_HP,
        FROG_KNIGHT_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=FROG_KNIGHT_MONSTER_ID)

    _state = {FROG_KNIGHT_BEETLE_CHARGED_KEY: False}

    def tongue_lash(combat: CombatState) -> None:
        tongue_lash_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FROG_KNIGHT_DEADLY_TONGUE_LASH_DAMAGE,
            FROG_KNIGHT_BASE_TONGUE_LASH_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, tongue_lash_dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, FROG_KNIGHT_TONGUE_LASH_FRAIL, applier=creature)

    def strike_down_evil(combat: CombatState) -> None:
        strike_down_evil_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FROG_KNIGHT_DEADLY_STRIKE_DOWN_EVIL_DAMAGE,
            FROG_KNIGHT_BASE_STRIKE_DOWN_EVIL_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, strike_down_evil_dmg)

    def for_the_queen(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, FROG_KNIGHT_FOR_THE_QUEEN_STRENGTH)

    def beetle_charge(combat: CombatState) -> None:
        beetle_charge_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FROG_KNIGHT_DEADLY_BEETLE_CHARGE_DAMAGE,
            FROG_KNIGHT_BASE_BEETLE_CHARGE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, beetle_charge_dmg)
        _state[FROG_KNIGHT_BEETLE_CHARGED_KEY] = True

    # After for_the_queen, check HP for beetle charge
    charge_check = ConditionalBranchState(FROG_KNIGHT_HALF_HEALTH_BRANCH)
    charge_check.add_branch(
        lambda: not _state[FROG_KNIGHT_BEETLE_CHARGED_KEY] and creature.current_hp < creature.max_hp // 2,
        FROG_KNIGHT_BEETLE_CHARGE_MOVE,
    )
    charge_check.add_branch(lambda: True, FROG_KNIGHT_TONGUE_LASH_MOVE)

    tongue_lash_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FROG_KNIGHT_DEADLY_TONGUE_LASH_DAMAGE,
        FROG_KNIGHT_BASE_TONGUE_LASH_DAMAGE,
    )
    strike_down_evil_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FROG_KNIGHT_DEADLY_STRIKE_DOWN_EVIL_DAMAGE,
        FROG_KNIGHT_BASE_STRIKE_DOWN_EVIL_DAMAGE,
    )
    beetle_charge_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FROG_KNIGHT_DEADLY_BEETLE_CHARGE_DAMAGE,
        FROG_KNIGHT_BASE_BEETLE_CHARGE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        FROG_KNIGHT_TONGUE_LASH_MOVE: MoveState(
            FROG_KNIGHT_TONGUE_LASH_MOVE,
            tongue_lash,
            [attack_intent(tongue_lash_intent_damage), debuff_intent()],
            follow_up_id=FROG_KNIGHT_STRIKE_DOWN_EVIL_MOVE,
        ),
        FROG_KNIGHT_STRIKE_DOWN_EVIL_MOVE: MoveState(
            FROG_KNIGHT_STRIKE_DOWN_EVIL_MOVE,
            strike_down_evil,
            [attack_intent(strike_down_evil_intent_damage)],
            follow_up_id=FROG_KNIGHT_FOR_THE_QUEEN_MOVE,
        ),
        FROG_KNIGHT_FOR_THE_QUEEN_MOVE: MoveState(
            FROG_KNIGHT_FOR_THE_QUEEN_MOVE,
            for_the_queen,
            [buff_intent()],
            follow_up_id=FROG_KNIGHT_HALF_HEALTH_BRANCH,
        ),
        FROG_KNIGHT_HALF_HEALTH_BRANCH: charge_check,
        FROG_KNIGHT_BEETLE_CHARGE_MOVE: MoveState(
            FROG_KNIGHT_BEETLE_CHARGE_MOVE,
            beetle_charge,
            [attack_intent(beetle_charge_intent_damage)],
            follow_up_id=FROG_KNIGHT_TONGUE_LASH_MOVE,
        ),
    }

    plating_amount = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FROG_KNIGHT_TOUGH_PLATING,
        FROG_KNIGHT_BASE_PLATING,
    )
    creature.apply_power(PowerId.PLATING, plating_amount)
    return creature, MonsterAI(states, FROG_KNIGHT_TONGUE_LASH_MOVE)


# ---- GlobeHead (HP 148 / 158 asc) ----

GLOBE_HEAD_MONSTER_ID = "GLOBE_HEAD"
GLOBE_HEAD_BASE_HP = 148
GLOBE_HEAD_TOUGH_HP = 158
GLOBE_HEAD_BASE_SHOCKING_SLAP_DAMAGE = 13
GLOBE_HEAD_DEADLY_SHOCKING_SLAP_DAMAGE = 14
GLOBE_HEAD_SHOCKING_SLAP_FRAIL = 2
GLOBE_HEAD_BASE_THUNDER_STRIKE_DAMAGE = 6
GLOBE_HEAD_DEADLY_THUNDER_STRIKE_DAMAGE = 7
GLOBE_HEAD_THUNDER_STRIKE_REPEAT = 3
GLOBE_HEAD_BASE_GALVANIC_BURST_DAMAGE = 16
GLOBE_HEAD_DEADLY_GALVANIC_BURST_DAMAGE = 17
GLOBE_HEAD_GALVANIC_BURST_STRENGTH = 2
GLOBE_HEAD_GALVANIC_AMOUNT = 6
GLOBE_HEAD_SHOCKING_SLAP_MOVE = "SHOCKING_SLAP"
GLOBE_HEAD_THUNDER_STRIKE_MOVE = "THUNDER_STRIKE"
GLOBE_HEAD_GALVANIC_BURST_MOVE = "GALVANIC_BURST"


def create_globe_head(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GLOBE_HEAD_TOUGH_HP,
        GLOBE_HEAD_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=GLOBE_HEAD_MONSTER_ID)

    def shocking_slap(combat: CombatState) -> None:
        shocking_slap_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            GLOBE_HEAD_DEADLY_SHOCKING_SLAP_DAMAGE,
            GLOBE_HEAD_BASE_SHOCKING_SLAP_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, shocking_slap_dmg)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, GLOBE_HEAD_SHOCKING_SLAP_FRAIL, applier=creature)

    def thunder_strike(combat: CombatState) -> None:
        thunder_strike_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            GLOBE_HEAD_DEADLY_THUNDER_STRIKE_DAMAGE,
            GLOBE_HEAD_BASE_THUNDER_STRIKE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, thunder_strike_dmg, hits=GLOBE_HEAD_THUNDER_STRIKE_REPEAT)

    def galvanic_burst(combat: CombatState) -> None:
        galvanic_burst_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            GLOBE_HEAD_DEADLY_GALVANIC_BURST_DAMAGE,
            GLOBE_HEAD_BASE_GALVANIC_BURST_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, galvanic_burst_dmg)
        combat.apply_power_to(creature, PowerId.STRENGTH, GLOBE_HEAD_GALVANIC_BURST_STRENGTH, applier=creature)

    shocking_slap_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        GLOBE_HEAD_DEADLY_SHOCKING_SLAP_DAMAGE,
        GLOBE_HEAD_BASE_SHOCKING_SLAP_DAMAGE,
    )
    thunder_strike_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        GLOBE_HEAD_DEADLY_THUNDER_STRIKE_DAMAGE,
        GLOBE_HEAD_BASE_THUNDER_STRIKE_DAMAGE,
    )
    galvanic_burst_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        GLOBE_HEAD_DEADLY_GALVANIC_BURST_DAMAGE,
        GLOBE_HEAD_BASE_GALVANIC_BURST_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        GLOBE_HEAD_SHOCKING_SLAP_MOVE: MoveState(
            GLOBE_HEAD_SHOCKING_SLAP_MOVE,
            shocking_slap,
            [attack_intent(shocking_slap_intent_damage), debuff_intent()],
            follow_up_id=GLOBE_HEAD_THUNDER_STRIKE_MOVE,
        ),
        GLOBE_HEAD_THUNDER_STRIKE_MOVE: MoveState(
            GLOBE_HEAD_THUNDER_STRIKE_MOVE,
            thunder_strike,
            [multi_attack_intent(thunder_strike_intent_damage, GLOBE_HEAD_THUNDER_STRIKE_REPEAT)],
            follow_up_id=GLOBE_HEAD_GALVANIC_BURST_MOVE,
        ),
        GLOBE_HEAD_GALVANIC_BURST_MOVE: MoveState(
            GLOBE_HEAD_GALVANIC_BURST_MOVE,
            galvanic_burst,
            [attack_intent(galvanic_burst_intent_damage), buff_intent()],
            follow_up_id=GLOBE_HEAD_SHOCKING_SLAP_MOVE,
        ),
    }

    creature.apply_power(PowerId.GALVANIC, GLOBE_HEAD_GALVANIC_AMOUNT)
    return creature, MonsterAI(states, GLOBE_HEAD_SHOCKING_SLAP_MOVE)


# ---- OwlMagistrate (HP 82 / 86 asc) ----

OWL_MAGISTRATE_MONSTER_ID = "OWL_MAGISTRATE"
OWL_MAGISTRATE_BASE_HP = 234
OWL_MAGISTRATE_TOUGH_HP = 243
OWL_MAGISTRATE_BASE_SCRUTINY_DAMAGE = 16
OWL_MAGISTRATE_DEADLY_SCRUTINY_DAMAGE = 17
OWL_MAGISTRATE_PECK_ASSAULT_DAMAGE = 4
OWL_MAGISTRATE_PECK_ASSAULT_REPEAT = 6
OWL_MAGISTRATE_BASE_VERDICT_DAMAGE = 33
OWL_MAGISTRATE_DEADLY_VERDICT_DAMAGE = 36
OWL_MAGISTRATE_JUDICIAL_FLIGHT_SOAR = 1
OWL_MAGISTRATE_VERDICT_VULNERABLE = 4
OWL_MAGISTRATE_SCRUTINY_MOVE = "MAGISTRATE_SCRUTINY"
OWL_MAGISTRATE_PECK_ASSAULT_MOVE = "PECK_ASSAULT"
OWL_MAGISTRATE_JUDICIAL_FLIGHT_MOVE = "JUDICIAL_FLIGHT"
OWL_MAGISTRATE_VERDICT_MOVE = "VERDICT"


def create_owl_magistrate(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        OWL_MAGISTRATE_TOUGH_HP,
        OWL_MAGISTRATE_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=OWL_MAGISTRATE_MONSTER_ID)

    def magistrate_scrutiny(combat: CombatState) -> None:
        scrutiny_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            OWL_MAGISTRATE_DEADLY_SCRUTINY_DAMAGE,
            OWL_MAGISTRATE_BASE_SCRUTINY_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, scrutiny_dmg)

    def peck_assault(combat: CombatState) -> None:
        _deal_damage_to_player(
            combat,
            creature,
            OWL_MAGISTRATE_PECK_ASSAULT_DAMAGE,
            hits=OWL_MAGISTRATE_PECK_ASSAULT_REPEAT,
        )

    def judicial_flight(combat: CombatState) -> None:
        creature.apply_power(PowerId.SOAR, OWL_MAGISTRATE_JUDICIAL_FLIGHT_SOAR, applier=creature)

    def verdict(combat: CombatState) -> None:
        verdict_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            OWL_MAGISTRATE_DEADLY_VERDICT_DAMAGE,
            OWL_MAGISTRATE_BASE_VERDICT_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, verdict_dmg)
        apply_power_to_living_player_targets(
            combat,
            PowerId.VULNERABLE,
            OWL_MAGISTRATE_VERDICT_VULNERABLE,
            applier=creature,
        )
        creature.powers.pop(PowerId.SOAR, None)

    scrutiny_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        OWL_MAGISTRATE_DEADLY_SCRUTINY_DAMAGE,
        OWL_MAGISTRATE_BASE_SCRUTINY_DAMAGE,
    )
    verdict_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        OWL_MAGISTRATE_DEADLY_VERDICT_DAMAGE,
        OWL_MAGISTRATE_BASE_VERDICT_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        OWL_MAGISTRATE_SCRUTINY_MOVE: MoveState(
            OWL_MAGISTRATE_SCRUTINY_MOVE,
            magistrate_scrutiny,
            [attack_intent(scrutiny_intent_damage)],
            follow_up_id=OWL_MAGISTRATE_PECK_ASSAULT_MOVE,
        ),
        OWL_MAGISTRATE_PECK_ASSAULT_MOVE: MoveState(
            OWL_MAGISTRATE_PECK_ASSAULT_MOVE,
            peck_assault,
            [multi_attack_intent(OWL_MAGISTRATE_PECK_ASSAULT_DAMAGE, OWL_MAGISTRATE_PECK_ASSAULT_REPEAT)],
            follow_up_id=OWL_MAGISTRATE_JUDICIAL_FLIGHT_MOVE,
        ),
        OWL_MAGISTRATE_JUDICIAL_FLIGHT_MOVE: MoveState(
            OWL_MAGISTRATE_JUDICIAL_FLIGHT_MOVE,
            judicial_flight,
            [buff_intent()],
            follow_up_id=OWL_MAGISTRATE_VERDICT_MOVE,
        ),
        OWL_MAGISTRATE_VERDICT_MOVE: MoveState(
            OWL_MAGISTRATE_VERDICT_MOVE,
            verdict,
            [attack_intent(verdict_intent_damage), debuff_intent()],
            follow_up_id=OWL_MAGISTRATE_SCRUTINY_MOVE,
        ),
    }
    return creature, MonsterAI(states, OWL_MAGISTRATE_SCRUTINY_MOVE)


# ---- SlimedBerserker (HP 60-65 / 64-69 asc) ----

SLIMED_BERSERKER_MONSTER_ID = "SLIMED_BERSERKER"
SLIMED_BERSERKER_BASE_HP = 266
SLIMED_BERSERKER_TOUGH_HP = 276
SLIMED_BERSERKER_BASE_PUMMELING_DAMAGE = 4
SLIMED_BERSERKER_DEADLY_PUMMELING_DAMAGE = 5
SLIMED_BERSERKER_PUMMELING_REPEAT = 4
SLIMED_BERSERKER_VOMIT_ICHOR_SLIMED = 10
SLIMED_BERSERKER_LEECHING_HUG_WEAK = 3
SLIMED_BERSERKER_LEECHING_HUG_STRENGTH = 3
SLIMED_BERSERKER_BASE_SMOTHER_DAMAGE = 30
SLIMED_BERSERKER_DEADLY_SMOTHER_DAMAGE = 33
SLIMED_BERSERKER_VOMIT_ICHOR_MOVE = "VOMIT_ICHOR_MOVE"
SLIMED_BERSERKER_FURIOUS_PUMMELING_MOVE = "FURIOUS_PUMMELING_MOVE"
SLIMED_BERSERKER_LEECHING_HUG_MOVE = "LEECHING_HUG_MOVE"
SLIMED_BERSERKER_SMOTHER_MOVE = "SMOTHER_MOVE"


def create_slimed_berserker(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SLIMED_BERSERKER_TOUGH_HP,
        SLIMED_BERSERKER_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=SLIMED_BERSERKER_MONSTER_ID)

    def vomit_ichor(combat: CombatState) -> None:
        for target in living_player_targets(combat):
            for _ in range(SLIMED_BERSERKER_VOMIT_ICHOR_SLIMED):
                combat.add_generated_card_to_creature_discard(
                    target,
                    make_slimed(),
                    added_by_player=False,
                )

    def furious_pummeling(combat: CombatState) -> None:
        pummeling_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SLIMED_BERSERKER_DEADLY_PUMMELING_DAMAGE,
            SLIMED_BERSERKER_BASE_PUMMELING_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, pummeling_dmg, hits=SLIMED_BERSERKER_PUMMELING_REPEAT)

    def leeching_hug(combat: CombatState) -> None:
        apply_power_to_living_player_targets(
            combat,
            PowerId.WEAK,
            SLIMED_BERSERKER_LEECHING_HUG_WEAK,
            applier=creature,
        )
        creature.apply_power(PowerId.STRENGTH, SLIMED_BERSERKER_LEECHING_HUG_STRENGTH)

    def smother(combat: CombatState) -> None:
        smother_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SLIMED_BERSERKER_DEADLY_SMOTHER_DAMAGE,
            SLIMED_BERSERKER_BASE_SMOTHER_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, smother_dmg)

    pummeling_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SLIMED_BERSERKER_DEADLY_PUMMELING_DAMAGE,
        SLIMED_BERSERKER_BASE_PUMMELING_DAMAGE,
    )
    smother_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SLIMED_BERSERKER_DEADLY_SMOTHER_DAMAGE,
        SLIMED_BERSERKER_BASE_SMOTHER_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        SLIMED_BERSERKER_VOMIT_ICHOR_MOVE: MoveState(
            SLIMED_BERSERKER_VOMIT_ICHOR_MOVE,
            vomit_ichor,
            [status_intent()],
            follow_up_id=SLIMED_BERSERKER_FURIOUS_PUMMELING_MOVE,
        ),
        SLIMED_BERSERKER_FURIOUS_PUMMELING_MOVE: MoveState(
            SLIMED_BERSERKER_FURIOUS_PUMMELING_MOVE,
            furious_pummeling,
            [multi_attack_intent(pummeling_intent_damage, SLIMED_BERSERKER_PUMMELING_REPEAT)],
            follow_up_id=SLIMED_BERSERKER_LEECHING_HUG_MOVE,
        ),
        SLIMED_BERSERKER_LEECHING_HUG_MOVE: MoveState(
            SLIMED_BERSERKER_LEECHING_HUG_MOVE,
            leeching_hug,
            [debuff_intent(), buff_intent()],
            follow_up_id=SLIMED_BERSERKER_SMOTHER_MOVE,
        ),
        SLIMED_BERSERKER_SMOTHER_MOVE: MoveState(
            SLIMED_BERSERKER_SMOTHER_MOVE,
            smother,
            [attack_intent(smother_intent_damage)],
            follow_up_id=SLIMED_BERSERKER_VOMIT_ICHOR_MOVE,
        ),
    }
    return creature, MonsterAI(states, SLIMED_BERSERKER_VOMIT_ICHOR_MOVE)


# ---- TheLost + TheForgotten ----

THE_LOST_MONSTER_ID = "THE_LOST"
THE_LOST_BASE_HP = 93
THE_LOST_TOUGH_HP = 99
THE_LOST_DEBILITATING_SMOG_STRENGTH = -2
THE_LOST_DEBILITATING_SMOG_SELF_STRENGTH = 2
THE_LOST_BASE_EYE_LASERS_DAMAGE = 4
THE_LOST_DEADLY_EYE_LASERS_DAMAGE = 5
THE_LOST_EYE_LASERS_REPEAT = 2
THE_LOST_POSSESS_STRENGTH = 1
THE_LOST_DEBILITATING_SMOG_MOVE = "DEBILITATING_SMOG"
THE_LOST_EYE_LASERS_MOVE = "EYE_LASERS"

THE_FORGOTTEN_MONSTER_ID = "THE_FORGOTTEN"
THE_FORGOTTEN_BASE_HP = 106
THE_FORGOTTEN_TOUGH_HP = 111
THE_FORGOTTEN_MIASMA_DEXTERITY = -2
THE_FORGOTTEN_MIASMA_BLOCK = 8
THE_FORGOTTEN_MIASMA_SELF_DEXTERITY = 2
THE_FORGOTTEN_BASE_DREAD_DAMAGE = 15
THE_FORGOTTEN_DEADLY_DREAD_DAMAGE = 17
THE_FORGOTTEN_POSSESS_SPEED = 1
THE_FORGOTTEN_MIASMA_MOVE = "MIASMA"
THE_FORGOTTEN_DREAD_MOVE = "DREAD"


def create_the_lost(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        THE_LOST_TOUGH_HP,
        THE_LOST_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=THE_LOST_MONSTER_ID)

    def debilitating_smog(combat: CombatState) -> None:
        apply_power_to_living_player_targets(
            combat,
            PowerId.STRENGTH,
            THE_LOST_DEBILITATING_SMOG_STRENGTH,
            applier=creature,
        )
        creature.apply_power(PowerId.STRENGTH, THE_LOST_DEBILITATING_SMOG_SELF_STRENGTH, applier=creature)

    def eye_lasers(combat: CombatState) -> None:
        eye_lasers_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            THE_LOST_DEADLY_EYE_LASERS_DAMAGE,
            THE_LOST_BASE_EYE_LASERS_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, eye_lasers_dmg, hits=THE_LOST_EYE_LASERS_REPEAT)

    eye_lasers_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        THE_LOST_DEADLY_EYE_LASERS_DAMAGE,
        THE_LOST_BASE_EYE_LASERS_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        THE_LOST_DEBILITATING_SMOG_MOVE: MoveState(
            THE_LOST_DEBILITATING_SMOG_MOVE,
            debilitating_smog,
            [debuff_intent(), buff_intent()],
            follow_up_id=THE_LOST_EYE_LASERS_MOVE,
        ),
        THE_LOST_EYE_LASERS_MOVE: MoveState(
            THE_LOST_EYE_LASERS_MOVE,
            eye_lasers,
            [multi_attack_intent(eye_lasers_intent_damage, THE_LOST_EYE_LASERS_REPEAT)],
            follow_up_id=THE_LOST_DEBILITATING_SMOG_MOVE,
        ),
    }
    creature.apply_power(PowerId.POSSESS_STRENGTH, THE_LOST_POSSESS_STRENGTH)
    return creature, MonsterAI(states, THE_LOST_DEBILITATING_SMOG_MOVE)


def create_the_forgotten(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        THE_FORGOTTEN_TOUGH_HP,
        THE_FORGOTTEN_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=THE_FORGOTTEN_MONSTER_ID)

    def miasma(combat: CombatState) -> None:
        apply_power_to_living_player_targets(
            combat,
            PowerId.DEXTERITY,
            THE_FORGOTTEN_MIASMA_DEXTERITY,
            applier=creature,
        )
        _gain_block(creature, THE_FORGOTTEN_MIASMA_BLOCK, combat)
        creature.apply_power(PowerId.DEXTERITY, THE_FORGOTTEN_MIASMA_SELF_DEXTERITY, applier=creature)

    def dread(combat: CombatState) -> None:
        dread_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            THE_FORGOTTEN_DEADLY_DREAD_DAMAGE,
            THE_FORGOTTEN_BASE_DREAD_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, dread_dmg)

    dread_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        THE_FORGOTTEN_DEADLY_DREAD_DAMAGE,
        THE_FORGOTTEN_BASE_DREAD_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        THE_FORGOTTEN_MIASMA_MOVE: MoveState(
            THE_FORGOTTEN_MIASMA_MOVE,
            miasma,
            [debuff_intent(), defend_intent(), buff_intent()],
            follow_up_id=THE_FORGOTTEN_DREAD_MOVE,
        ),
        THE_FORGOTTEN_DREAD_MOVE: MoveState(
            THE_FORGOTTEN_DREAD_MOVE,
            dread,
            [attack_intent(dread_intent_damage)],
            follow_up_id=THE_FORGOTTEN_MIASMA_MOVE,
        ),
    }
    creature.apply_power(PowerId.POSSESS_SPEED, THE_FORGOTTEN_POSSESS_SPEED)
    return creature, MonsterAI(states, THE_FORGOTTEN_MIASMA_MOVE)


# ---- ConstructMenagerie ----


# ========================================================================
# ELITE ENCOUNTERS
# ========================================================================

# ---- Knights (FlailKnight, MagiKnight, SpectralKnight) ----

def create_flail_knight(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 101
    creature = Creature(max_hp=hp, monster_id="FLAIL_KNIGHT")
    flail_dmg = 9
    ram_dmg = 15

    def war_chant(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, 3)

    def flail(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, flail_dmg, hits=2)

    def ram(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, ram_dmg)

    rand = RandomBranchState("RAND")
    rand.add_branch("WAR_CHANT", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("FLAIL_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)
    rand.add_branch("RAM_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)

    states: dict[str, MonsterState] = {
        "RAND": rand,
        "WAR_CHANT": MoveState("WAR_CHANT", war_chant, [buff_intent()], follow_up_id="RAND"),
        "FLAIL_MOVE": MoveState("FLAIL_MOVE", flail, [multi_attack_intent(flail_dmg, 2)], follow_up_id="RAND"),
        "RAM_MOVE": MoveState("RAM_MOVE", ram, [attack_intent(ram_dmg)], follow_up_id="RAND"),
    }
    return creature, MonsterAI(states, "RAM_MOVE")


# ---- MysteriousKnight (HP 101, event combat) ----
# Identical state machine to FlailKnight, but starts with Str+6 and Plating(6).

def create_mysterious_knight(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 101
    creature = Creature(max_hp=hp, monster_id="MYSTERIOUS_KNIGHT")
    flail_dmg = 9
    ram_dmg = 15

    def war_chant(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, 3)

    def flail(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, flail_dmg, hits=2)

    def ram(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, ram_dmg)

    rand = RandomBranchState("RAND")
    rand.add_branch("WAR_CHANT", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("FLAIL_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)
    rand.add_branch("RAM_MOVE", MoveRepeatType.CAN_REPEAT_FOREVER, weight=2.0)

    states: dict[str, MonsterState] = {
        "RAND": rand,
        "WAR_CHANT": MoveState("WAR_CHANT", war_chant, [buff_intent()], follow_up_id="RAND"),
        "FLAIL_MOVE": MoveState("FLAIL_MOVE", flail, [multi_attack_intent(flail_dmg, 2)], follow_up_id="RAND"),
        "RAM_MOVE": MoveState("RAM_MOVE", ram, [attack_intent(ram_dmg)], follow_up_id="RAND"),
    }

    # AfterAddedToRoom: Strength+6, Plating(6) on top of FlailKnight's base
    creature.apply_power(PowerId.STRENGTH, 6)
    creature.apply_power(PowerId.PLATING, 6)
    return creature, MonsterAI(states, "RAM_MOVE")


# ---- LivingShield (HP 55) ----
# Moves: SHIELD_SLAM(6) while allies alive, SMASH(16, Str+3) when alone.
# Conditional branch checks ally count.
# AfterAddedToRoom: Rampart(25)

def create_living_shield(rng: Rng, get_ally_count=None) -> tuple[Creature, MonsterAI]:
    hp = 55
    creature = Creature(max_hp=hp, monster_id="LIVING_SHIELD")
    shield_slam_dmg = 6
    smash_dmg = 16
    enrage_str = 3

    def shield_slam(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, shield_slam_dmg)

    def smash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, smash_dmg)
        combat.apply_power_to(creature, PowerId.STRENGTH, enrage_str, applier=creature)

    # Default ally count checker: count alive teammates excluding self
    def _default_ally_count() -> int:
        combat = creature.combat_state
        if combat is None:
            return 0
        return len(combat.get_teammates_of(creature))

    ally_count_fn = get_ally_count or _default_ally_count

    # Conditional: if allies alive -> SHIELD_SLAM, else -> SMASH (self loop)
    shield_slam_branch = ConditionalBranchState("SHIELD_SLAM_BRANCH")
    shield_slam_branch.add_branch(lambda: ally_count_fn() > 0, "SHIELD_SLAM_MOVE")
    shield_slam_branch.add_branch(lambda: ally_count_fn() == 0, "SMASH_MOVE")

    states: dict[str, MonsterState] = {
        "SHIELD_SLAM_BRANCH": shield_slam_branch,
        "SHIELD_SLAM_MOVE": MoveState("SHIELD_SLAM_MOVE", shield_slam, [attack_intent(shield_slam_dmg)], follow_up_id="SHIELD_SLAM_BRANCH"),
        "SMASH_MOVE": MoveState("SMASH_MOVE", smash, [attack_intent(smash_dmg), buff_intent()], follow_up_id="SMASH_MOVE"),
    }

    creature.apply_power(PowerId.RAMPART, 25)
    return creature, MonsterAI(states, "SHIELD_SLAM_MOVE")


def create_magi_knight(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 82
    creature = Creature(max_hp=hp, monster_id="MAGI_KNIGHT")
    power_shield_dmg = 6
    power_shield_block = 5
    dampen_amount = 1
    spear_dmg = 10
    bomb_dmg = 35

    def power_shield(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, power_shield_dmg)
        _gain_block(creature, power_shield_block, combat)

    def dampen(combat: CombatState) -> None:
        for target in living_player_targets(combat):
            power = target.powers.get(PowerId.DAMPEN)
            if power is not None:
                add_caster = getattr(power, "add_caster", None)
                if callable(add_caster):
                    add_caster(creature)
                continue
            combat.apply_power_to(target, PowerId.DAMPEN, dampen_amount, applier=creature)
            power = target.powers.get(PowerId.DAMPEN)
            if power is not None:
                add_caster = getattr(power, "add_caster", None)
                if callable(add_caster):
                    add_caster(creature)

    def spear(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, spear_dmg)

    def prep(combat: CombatState) -> None:
        _gain_block(creature, power_shield_block, combat)

    def magic_bomb(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, bomb_dmg)

    states: dict[str, MonsterState] = {
        "FIRST_POWER_SHIELD_MOVE": MoveState("FIRST_POWER_SHIELD_MOVE", power_shield, [attack_intent(power_shield_dmg), defend_intent()], follow_up_id="DAMPEN_MOVE"),
        "DAMPEN_MOVE": MoveState("DAMPEN_MOVE", dampen, [debuff_intent()], follow_up_id="RAM_MOVE"),
        "RAM_MOVE": MoveState("RAM_MOVE", spear, [attack_intent(spear_dmg)], follow_up_id="PREP_MOVE"),
        "PREP_MOVE": MoveState("PREP_MOVE", prep, [defend_intent()], follow_up_id="MAGIC_BOMB"),
        "MAGIC_BOMB": MoveState("MAGIC_BOMB", magic_bomb, [attack_intent(bomb_dmg)], follow_up_id="RAM_MOVE"),
    }
    return creature, MonsterAI(states, "FIRST_POWER_SHIELD_MOVE")


def create_spectral_knight(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 93
    creature = Creature(max_hp=hp, monster_id="SPECTRAL_KNIGHT")
    hex_amount = 2
    soul_slash_dmg = 15
    soul_flame_dmg = 3

    def hex_player(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.HEX, hex_amount, applier=creature)

    def soul_slash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, soul_slash_dmg)

    def soul_flame(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, soul_flame_dmg, hits=3)

    rand = RandomBranchState("RAND")
    rand.add_branch("SOUL_SLASH", weight=2.0)
    rand.add_branch("SOUL_FLAME", MoveRepeatType.CANNOT_REPEAT)

    states: dict[str, MonsterState] = {
        "HEX": MoveState("HEX", hex_player, [debuff_intent()], follow_up_id="SOUL_SLASH"),
        "RAND": rand,
        "SOUL_SLASH": MoveState("SOUL_SLASH", soul_slash, [attack_intent(soul_slash_dmg)], follow_up_id="RAND"),
        "SOUL_FLAME": MoveState("SOUL_FLAME", soul_flame, [multi_attack_intent(soul_flame_dmg, 3)], follow_up_id="RAND"),
    }
    return creature, MonsterAI(states, "HEX")


# ---- MechaKnight (HP 155 / 165 asc) ----

def create_mecha_knight(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 300
    creature = Creature(max_hp=hp, monster_id="MECHA_KNIGHT")
    charge_dmg = 25
    heavy_cleave_dmg = 35
    windup_block = 15
    flamethrower_burns = 4
    windup_strength = 5

    def charge(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, charge_dmg)

    def flamethrower(combat: CombatState) -> None:
        for target in living_player_targets(combat):
            for _ in range(flamethrower_burns):
                combat.add_generated_card_to_creature_hand(
                    target,
                    make_burn(),
                    added_by_player=False,
                )

    def windup(combat: CombatState) -> None:
        _gain_block(creature, windup_block, combat)
        creature.apply_power(PowerId.STRENGTH, windup_strength)

    def heavy_cleave(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, heavy_cleave_dmg)

    states: dict[str, MonsterState] = {
        "CHARGE_MOVE": MoveState("CHARGE_MOVE", charge, [attack_intent(charge_dmg)], follow_up_id="FLAMETHROWER_MOVE"),
        "FLAMETHROWER_MOVE": MoveState("FLAMETHROWER_MOVE", flamethrower, [status_intent()], follow_up_id="WINDUP_MOVE"),
        "WINDUP_MOVE": MoveState("WINDUP_MOVE", windup, [defend_intent(), buff_intent()], follow_up_id="HEAVY_CLEAVE_MOVE"),
        "HEAVY_CLEAVE_MOVE": MoveState("HEAVY_CLEAVE_MOVE", heavy_cleave, [attack_intent(heavy_cleave_dmg)], follow_up_id="FLAMETHROWER_MOVE"),
    }
    creature.apply_power(PowerId.ARTIFACT, 3)
    return creature, MonsterAI(states, "CHARGE_MOVE")


# ---- SoulNexus (HP 155 / 165 asc) + Osty ----

def create_osty(rng: Rng) -> tuple[Creature, MonsterAI]:
    creature = Creature(max_hp=1, monster_id="OSTY")

    def nothing(combat: CombatState) -> None:
        pass

    states: dict[str, MonsterState] = {
        "NOTHING_MOVE": MoveState("NOTHING_MOVE", nothing, [Intent(IntentType.UNKNOWN)], follow_up_id="NOTHING_MOVE"),
    }
    return creature, MonsterAI(states, "NOTHING_MOVE")


def create_soul_nexus(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 234
    creature = Creature(max_hp=hp, monster_id="SOUL_NEXUS")
    soul_burn_dmg = 29
    maelstrom_dmg = 6
    drain_life_dmg = 18
    drain_life_debuff = 2

    def soul_burn(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, soul_burn_dmg)

    def maelstrom(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, maelstrom_dmg, hits=4)

    def drain_life(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, drain_life_dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, drain_life_debuff, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, drain_life_debuff, applier=creature)

    rand = RandomBranchState("RAND")
    rand.add_branch("SOUL_BURN_MOVE", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("MAELSTROM_MOVE", MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch("DRAIN_LIFE_MOVE", MoveRepeatType.CANNOT_REPEAT)

    states: dict[str, MonsterState] = {
        "RAND": rand,
        "SOUL_BURN_MOVE": MoveState("SOUL_BURN_MOVE", soul_burn, [attack_intent(soul_burn_dmg)], follow_up_id="RAND"),
        "MAELSTROM_MOVE": MoveState("MAELSTROM_MOVE", maelstrom, [multi_attack_intent(maelstrom_dmg, 4)], follow_up_id="RAND"),
        "DRAIN_LIFE_MOVE": MoveState("DRAIN_LIFE_MOVE", drain_life, [attack_intent(drain_life_dmg), strong_debuff_intent()], follow_up_id="RAND"),
    }
    return creature, MonsterAI(states, "SOUL_BURN_MOVE")


# ========================================================================
# BOSS ENCOUNTERS
# ========================================================================

# ---- Door + Doormaker ----

def create_door(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 155
    creature = Creature(max_hp=hp, monster_id="DOOR")
    dramatic_open_dmg = 25
    enforce_dmg = 20
    door_slam_dmg = 15

    def dramatic_open(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, dramatic_open_dmg)

    def enforce(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, enforce_dmg)
        combat.apply_power_to(creature, PowerId.STRENGTH, 3, applier=creature)

    def door_slam(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, door_slam_dmg, hits=2)

    def dead_move(combat: CombatState) -> None:
        pass

    states: dict[str, MonsterState] = {
        "DRAMATIC_OPEN_MOVE": MoveState("DRAMATIC_OPEN_MOVE", dramatic_open, [attack_intent(dramatic_open_dmg)], follow_up_id="DOOR_SLAM_MOVE"),
        "DOOR_SLAM_MOVE": MoveState("DOOR_SLAM_MOVE", door_slam, [multi_attack_intent(door_slam_dmg, 2)], follow_up_id="ENFORCE_MOVE"),
        "ENFORCE_MOVE": MoveState("ENFORCE_MOVE", enforce, [attack_intent(enforce_dmg), buff_intent()], follow_up_id="DRAMATIC_OPEN_MOVE"),
        "DEAD_MOVE": MoveState("DEAD_MOVE", dead_move, [Intent(IntentType.UNKNOWN)], follow_up_id="DEAD_MOVE"),
    }

    creature.apply_power(PowerId.DOOR_REVIVAL, 1)
    return creature, MonsterAI(states, "DRAMATIC_OPEN_MOVE")


def create_doormaker(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 489
    creature = Creature(max_hp=hp, monster_id="DOORMAKER")
    laser_beam_dmg = 31
    get_back_in_dmg = 40

    def what_is_it(combat: CombatState) -> None:
        pass

    def beam(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, laser_beam_dmg)

    def get_back_in(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, get_back_in_dmg)
        if combat.is_over or creature.is_dead:
            return
        combat.apply_power_to(creature, PowerId.STRENGTH, 5, applier=creature)
        combat.revive_door()
        combat.escape_creature(creature)

    states: dict[str, MonsterState] = {
        "WHAT_IS_IT_MOVE": MoveState("WHAT_IS_IT_MOVE", what_is_it, [Intent(IntentType.STUN)], follow_up_id="BEAM_MOVE"),
        "BEAM_MOVE": MoveState("BEAM_MOVE", beam, [attack_intent(laser_beam_dmg)], follow_up_id="GET_BACK_IN_MOVE"),
        "GET_BACK_IN_MOVE": MoveState("GET_BACK_IN_MOVE", get_back_in, [attack_intent(get_back_in_dmg), buff_intent()], follow_up_id="GET_BACK_IN_MOVE"),
    }
    return creature, MonsterAI(states, "WHAT_IS_IT_MOVE")


# ---- Queen (HP 302 / 322 asc) ----

def create_royal_guard(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 40
    creature = Creature(max_hp=hp, monster_id="ROYAL_GUARD")
    strike_dmg = 15

    def guard_strike(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, strike_dmg)

    states: dict[str, MonsterState] = {
        "STRIKE": MoveState("STRIKE", guard_strike, [attack_intent(strike_dmg)], follow_up_id="STRIKE"),
    }
    creature.apply_power(PowerId.MINION, 1)
    return creature, MonsterAI(states, "STRIKE")


def create_queen(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 400
    creature = Creature(max_hp=hp, monster_id="QUEEN")
    off_with_your_head_dmg = 3
    execution_dmg = 15
    chains_of_binding = 3
    youre_mine_debuff = 99
    burn_bright_strength = 1
    burn_bright_block = 20
    enrage_strength = 2

    def _has_amalgam_alive() -> bool:
        combat = creature.combat_state
        if combat is None:
            return True
        return any(enemy.monster_id == "TORCH_HEAD_AMALGAM" and enemy.is_alive for enemy in combat.enemies)

    def puppet_strings(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.CHAINS_OF_BINDING, chains_of_binding, applier=creature)

    def youre_mine(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, youre_mine_debuff, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, youre_mine_debuff, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, youre_mine_debuff, applier=creature)

    def burn_bright_for_me(combat: CombatState) -> None:
        for enemy in combat.alive_enemies:
            if enemy is not creature and enemy.side == creature.side:
                enemy.apply_power(PowerId.STRENGTH, burn_bright_strength, applier=creature)
        _gain_block(creature, burn_bright_block, combat)

    def off_with_your_head(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, off_with_your_head_dmg, hits=5)

    def execution(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, execution_dmg)

    def enrage(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, enrage_strength, applier=creature)

    youre_mine_now_branch = ConditionalBranchState("YOURE_MINE_NOW_BRANCH")
    youre_mine_now_branch.add_branch(_has_amalgam_alive, "BURN_BRIGHT_FOR_ME_MOVE")
    youre_mine_now_branch.add_branch(lambda: True, "OFF_WITH_YOUR_HEAD_MOVE")

    burn_bright_branch = ConditionalBranchState("BURN_BRIGHT_FOR_ME_BRANCH")
    burn_bright_branch.add_branch(_has_amalgam_alive, "BURN_BRIGHT_FOR_ME_MOVE")
    burn_bright_branch.add_branch(lambda: True, "OFF_WITH_YOUR_HEAD_MOVE")

    states: dict[str, MonsterState] = {
        "PUPPET_STRINGS_MOVE": MoveState("PUPPET_STRINGS_MOVE", puppet_strings, [strong_debuff_intent()], follow_up_id="YOUR_MINE_MOVE"),
        "YOUR_MINE_MOVE": MoveState("YOUR_MINE_MOVE", youre_mine, [debuff_intent()], follow_up_id="YOURE_MINE_NOW_BRANCH"),
        "YOURE_MINE_NOW_BRANCH": youre_mine_now_branch,
        "BURN_BRIGHT_FOR_ME_MOVE": MoveState("BURN_BRIGHT_FOR_ME_MOVE", burn_bright_for_me, [buff_intent(), defend_intent()], follow_up_id="BURN_BRIGHT_FOR_ME_BRANCH"),
        "BURN_BRIGHT_FOR_ME_BRANCH": burn_bright_branch,
        "OFF_WITH_YOUR_HEAD_MOVE": MoveState("OFF_WITH_YOUR_HEAD_MOVE", off_with_your_head, [multi_attack_intent(off_with_your_head_dmg, 5)], follow_up_id="EXECUTION_MOVE"),
        "EXECUTION_MOVE": MoveState("EXECUTION_MOVE", execution, [attack_intent(execution_dmg)], follow_up_id="ENRAGE_MOVE"),
        "ENRAGE_MOVE": MoveState("ENRAGE_MOVE", enrage, [buff_intent()], follow_up_id="OFF_WITH_YOUR_HEAD_MOVE"),
    }
    return creature, MonsterAI(states, "PUPPET_STRINGS_MOVE")


# ---- TestSubject (HP 255 / 270 asc) ----

def create_test_subject(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 100
    creature = Creature(max_hp=hp, monster_id="TEST_SUBJECT")
    bite_dmg = 20
    skull_bash_dmg = 14
    skull_bash_vulnerable = 1
    pounce_dmg = 30
    multi_claw_dmg = 10
    big_pounce_dmg = 45
    burning_growl_burns = 3
    burning_growl_strength = 2
    enrage_amount = 2
    second_form_hp = 200
    third_form_hp = 300

    _state = {
        "respawns": 0,
        "extra_multi_claw_count": 0,
    }

    def _multi_claw_total_count() -> int:
        return 3 + _state["extra_multi_claw_count"]

    def respawn(combat: CombatState) -> None:
        _state["respawns"] += 1
        adaptable = creature.powers.get(PowerId.ADAPTABLE)
        do_revive = getattr(adaptable, "do_revive", None)
        if callable(do_revive):
            do_revive()
        scaled_hp = (second_form_hp if _state["respawns"] == 1 else third_form_hp) * len(combat.combat_player_states)
        creature.max_hp = scaled_hp
        creature.current_hp = scaled_hp
        if _state["respawns"] == 1:
            creature.apply_power(PowerId.PAINFUL_STABS, 1)
        else:
            creature.apply_power(PowerId.NEMESIS, 1)
            creature.powers.pop(PowerId.ADAPTABLE, None)
            creature.powers.pop(PowerId.PAINFUL_STABS, None)

    def bite(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, bite_dmg)

    def skull_bash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, skull_bash_dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, skull_bash_vulnerable, applier=creature)

    def pounce(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, pounce_dmg)

    def multi_claw(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, multi_claw_dmg, hits=_multi_claw_total_count())
        _state["extra_multi_claw_count"] += 1
        states["MULTI_CLAW_MOVE"].intents = [multi_attack_intent(multi_claw_dmg, _multi_claw_total_count())]

    def phase3_lacerate(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, multi_claw_dmg, hits=3)

    def big_pounce(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, big_pounce_dmg)

    def burning_growl(combat: CombatState) -> None:
        for target in living_player_targets(combat):
            combat.add_status_cards_to_discard(target, "BURN", burning_growl_burns)
        creature.apply_power(PowerId.STRENGTH, burning_growl_strength)

    revive_branch = ConditionalBranchState("REVIVE_BRANCH")
    revive_branch.add_branch(lambda: _state["respawns"] < 2, "MULTI_CLAW_MOVE")
    revive_branch.add_branch(lambda: True, "PHASE3_LACERATE_MOVE")

    states: dict[str, MonsterState] = {
        "RESPAWN_MOVE": MoveState("RESPAWN_MOVE", respawn, [Intent(IntentType.HEAL), buff_intent()], follow_up_id="REVIVE_BRANCH", must_perform_once=True),
        "REVIVE_BRANCH": revive_branch,
        "BITE_MOVE": MoveState("BITE_MOVE", bite, [attack_intent(bite_dmg)], follow_up_id="SKULL_BASH_MOVE"),
        "SKULL_BASH_MOVE": MoveState("SKULL_BASH_MOVE", skull_bash, [attack_intent(skull_bash_dmg), debuff_intent()], follow_up_id="BITE_MOVE"),
        "POUNCE_MOVE": MoveState("POUNCE_MOVE", pounce, [attack_intent(pounce_dmg)], follow_up_id="MULTI_CLAW_MOVE"),
        "MULTI_CLAW_MOVE": MoveState("MULTI_CLAW_MOVE", multi_claw, [multi_attack_intent(multi_claw_dmg, _multi_claw_total_count())], follow_up_id="POUNCE_MOVE"),
        "PHASE3_LACERATE_MOVE": MoveState("PHASE3_LACERATE_MOVE", phase3_lacerate, [multi_attack_intent(multi_claw_dmg, 3)], follow_up_id="BIG_POUNCE_MOVE"),
        "BIG_POUNCE_MOVE": MoveState("BIG_POUNCE_MOVE", big_pounce, [attack_intent(big_pounce_dmg)], follow_up_id="BURNING_GROWL_MOVE"),
        "BURNING_GROWL_MOVE": MoveState("BURNING_GROWL_MOVE", burning_growl, [status_intent(), buff_intent()], follow_up_id="PHASE3_LACERATE_MOVE"),
    }
    creature.apply_power(PowerId.ADAPTABLE, 1)
    creature.apply_power(PowerId.ENRAGE, enrage_amount)
    return creature, MonsterAI(states, "BITE_MOVE")
