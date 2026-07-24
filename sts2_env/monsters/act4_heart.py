"""Act4Heart mod ("TheEnding" / hand-authored Act 4) monsters.

The Corrupt Heart boss and the Spire Shield + Spire Spear duo elite.

C# refs (decompiled_mods/Act4Heart/Act4Heart/):
    CorruptHeart.cs, CorruptHeartBoss.cs, SpireShield.cs, SpireSpear.cs,
    SpireShieldAndSpireSpearElite.cs, EmptyFightAct4Weak.cs

HP values, damage values, and move-state-machine wiring verified against
the decompiled C# source.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import MoveRepeatType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import Rng
from sts2_env.cards.status import make_burn, make_dazed, make_slimed, make_void, make_wound
from sts2_env.monsters.intents import (
    attack_intent, buff_intent, debuff_intent, defend_intent, multi_attack_intent,
    status_intent, strong_debuff_intent,
)
from sts2_env.monsters.state_machine import (
    ConditionalBranchState, MonsterAI, MonsterState, MoveState, RandomBranchState,
)
from sts2_env.monsters.block import gain_move_block
from sts2_env.monsters.targets import (
    apply_power_to_living_player_targets,
    living_player_targets,
)

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ---- Helpers (matches convention used by monsters/act1.py..act4.py) ----

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


class _FightState:
    """Mutable per-fight AI state, closed over by the move callbacks.

    Mirrors the C# monster's private ``byte state`` bitfield (bit 1 = first
    alternating move used, bit 2 = second alternating move used) plus, for
    the Heart, a monotonic ``buff_counter`` that never resets.
    """

    __slots__ = ("bits", "buff_counter")

    def __init__(self, bits: int = 0) -> None:
        self.bits = bits
        self.buff_counter = 0


# ========================================================================
# CORRUPT HEART (Act4Heart boss)
# ========================================================================

CORRUPT_HEART_MONSTER_ID = "CORRUPT_HEART"
CORRUPT_HEART_BASE_HP = 750
CORRUPT_HEART_TOUGH_HP = 800
CORRUPT_HEART_BLOOD_SHOTS_DAMAGE = 2
CORRUPT_HEART_BLOOD_SHOTS_COUNT = 15
CORRUPT_HEART_ECHO_DAMAGE = 45
CORRUPT_HEART_DEBILITATE_VULNERABLE = 2
CORRUPT_HEART_DEBILITATE_WEAK = 2
CORRUPT_HEART_DEBILITATE_FRAIL = 2
CORRUPT_HEART_BASE_BEAT_OF_DEATH = 1
CORRUPT_HEART_DEADLY_BEAT_OF_DEATH = 2
CORRUPT_HEART_BASE_INVINCIBLE = 300
CORRUPT_HEART_DEADLY_INVINCIBLE = 200
CORRUPT_HEART_BUFF_ARTIFACT = 2
CORRUPT_HEART_BUFF_BEAT_OF_DEATH = 1
CORRUPT_HEART_BUFF_PAINFUL_STABS = 1
CORRUPT_HEART_BUFF_STRENGTH_4TH = 10
CORRUPT_HEART_BUFF_STRENGTH_5TH_PLUS = 50
CORRUPT_HEART_BUFF_STRENGTH = 2

CORRUPT_HEART_DEBILITATE_MOVE = "DEBILITATE_MOVE"
CORRUPT_HEART_BLOOD_SHOTS_MOVE = "BLOOD_SHOTS_MOVE"
CORRUPT_HEART_ECHO_MOVE = "ECHO_MOVE"
CORRUPT_HEART_BUFF_MOVE = "BUFF_MOVE"
CORRUPT_HEART_RANDOM_ATTACK_BRANCH = "RANDOM_ATTACK_BRANCH"
CORRUPT_HEART_POST_ATTACK_BRANCH = "POST_ATTACK_BRANCH"

_STATUS_CARD_FACTORIES = (make_dazed, make_slimed, make_wound, make_burn, make_void)


def create_corrupt_heart(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CORRUPT_HEART_TOUGH_HP, CORRUPT_HEART_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=CORRUPT_HEART_MONSTER_ID)

    state = _FightState(bits=0)

    def debilitate_move(combat: "CombatState") -> None:
        state.bits = 0
        targets = living_player_targets(combat)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, CORRUPT_HEART_DEBILITATE_VULNERABLE, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, CORRUPT_HEART_DEBILITATE_WEAK, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, CORRUPT_HEART_DEBILITATE_FRAIL, applier=creature)
        for target in targets:
            for factory in _STATUS_CARD_FACTORIES:
                combat.add_generated_card_to_creature_discard(target, factory(), added_by_player=False)

    def blood_shots_move(combat: "CombatState") -> None:
        state.bits |= 1
        _deal_damage_to_player(combat, creature, CORRUPT_HEART_BLOOD_SHOTS_DAMAGE, hits=CORRUPT_HEART_BLOOD_SHOTS_COUNT)

    def echo_move(combat: "CombatState") -> None:
        state.bits |= 2
        _deal_damage_to_player(combat, creature, CORRUPT_HEART_ECHO_DAMAGE)

    def buff_move(combat: "CombatState") -> None:
        state.bits = 0
        if creature.get_power_amount(PowerId.STRENGTH) < 0:
            combat.apply_power_to(creature, PowerId.STRENGTH, -creature.get_power_amount(PowerId.STRENGTH), applier=creature)
        combat.apply_power_to(creature, PowerId.STRENGTH, CORRUPT_HEART_BUFF_STRENGTH, applier=creature)
        state.buff_counter += 1
        counter = state.buff_counter
        if counter == 1:
            combat.apply_power_to(creature, PowerId.ARTIFACT, CORRUPT_HEART_BUFF_ARTIFACT, applier=creature)
        elif counter == 2:
            combat.apply_power_to(creature, PowerId.BEAT_OF_DEATH, CORRUPT_HEART_BUFF_BEAT_OF_DEATH, applier=creature)
        elif counter == 3:
            combat.apply_power_to(creature, PowerId.PAINFUL_STABS, CORRUPT_HEART_BUFF_PAINFUL_STABS, applier=creature)
        elif counter == 4:
            combat.apply_power_to(creature, PowerId.STRENGTH, CORRUPT_HEART_BUFF_STRENGTH_4TH, applier=creature)
        else:
            combat.apply_power_to(creature, PowerId.STRENGTH, CORRUPT_HEART_BUFF_STRENGTH_5TH_PLUS, applier=creature)

    debilitate = MoveState(
        CORRUPT_HEART_DEBILITATE_MOVE,
        debilitate_move,
        [strong_debuff_intent(), status_intent()],
        follow_up_id=CORRUPT_HEART_RANDOM_ATTACK_BRANCH,
    )
    blood_shots = MoveState(
        CORRUPT_HEART_BLOOD_SHOTS_MOVE,
        blood_shots_move,
        [multi_attack_intent(CORRUPT_HEART_BLOOD_SHOTS_DAMAGE, CORRUPT_HEART_BLOOD_SHOTS_COUNT)],
        follow_up_id=CORRUPT_HEART_POST_ATTACK_BRANCH,
    )
    echo = MoveState(
        CORRUPT_HEART_ECHO_MOVE,
        echo_move,
        [attack_intent(CORRUPT_HEART_ECHO_DAMAGE)],
        follow_up_id=CORRUPT_HEART_POST_ATTACK_BRANCH,
    )
    buff = MoveState(
        CORRUPT_HEART_BUFF_MOVE,
        buff_move,
        [buff_intent()],
        follow_up_id=CORRUPT_HEART_RANDOM_ATTACK_BRANCH,
    )

    random_branch = RandomBranchState(CORRUPT_HEART_RANDOM_ATTACK_BRANCH)
    random_branch.add_branch(CORRUPT_HEART_BLOOD_SHOTS_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)
    random_branch.add_branch(CORRUPT_HEART_ECHO_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)

    post_attack_branch = ConditionalBranchState(CORRUPT_HEART_POST_ATTACK_BRANCH)
    post_attack_branch.add_branch(lambda: (state.bits & 1) == 0, CORRUPT_HEART_BLOOD_SHOTS_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 2) == 0, CORRUPT_HEART_ECHO_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 3) == 3, CORRUPT_HEART_BUFF_MOVE)

    states: dict[str, object] = {
        CORRUPT_HEART_DEBILITATE_MOVE: debilitate,
        CORRUPT_HEART_BLOOD_SHOTS_MOVE: blood_shots,
        CORRUPT_HEART_ECHO_MOVE: echo,
        CORRUPT_HEART_BUFF_MOVE: buff,
        CORRUPT_HEART_RANDOM_ATTACK_BRANCH: random_branch,
        CORRUPT_HEART_POST_ATTACK_BRANCH: post_attack_branch,
    }

    # AfterAddedToRoom: apply Beat of Death + Invincible to self.
    beat_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CORRUPT_HEART_DEADLY_BEAT_OF_DEATH, CORRUPT_HEART_BASE_BEAT_OF_DEATH)
    invincible_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, CORRUPT_HEART_DEADLY_INVINCIBLE, CORRUPT_HEART_BASE_INVINCIBLE)
    creature.apply_power(PowerId.BEAT_OF_DEATH, beat_amount, applier=creature)
    creature.apply_power(PowerId.INVINCIBLE, invincible_amount, applier=creature)

    return creature, MonsterAI(states, CORRUPT_HEART_DEBILITATE_MOVE, rng)


# ========================================================================
# SPIRE SHIELD + SPIRE SPEAR (Act4Heart elite duo)
# ========================================================================

SPIRE_SHIELD_MONSTER_ID = "SPIRE_SHIELD"
SPIRE_SHIELD_BASE_HP = 110
SPIRE_SHIELD_TOUGH_HP = 125
SPIRE_SHIELD_BASE_ARTIFACT = 1
SPIRE_SHIELD_DEADLY_ARTIFACT = 2
SPIRE_SHIELD_BASH_DAMAGE = 14
SPIRE_SHIELD_FORTIFY_BLOCK = 30
SPIRE_SHIELD_SMASH_DAMAGE = 38
SPIRE_SHIELD_SMASH_FLAT_BLOCK_ASC9 = 99
SPIRE_SHIELD_ORBS_FOCUS_DOWN_ODDS = 0.5

SPIRE_SHIELD_BASH_MOVE = "BASH_MOVE"
SPIRE_SHIELD_FORTIFY_MOVE = "FORTIFY_MOVE"
SPIRE_SHIELD_SMASH_MOVE = "SMASH_MOVE"
SPIRE_SHIELD_RANDOM_BRANCH = "RANDOM_BRANCH"
SPIRE_SHIELD_POST_ATTACK_BRANCH = "POST_ATTACK_BRANCH"


def _spire_shield_bash_debuff_power(combat: "CombatState", target: Creature) -> PowerId:
    """C#: 50/50 (config-driven) toward Focus for orb-capable player targets,
    else Strength. Non-Defect characters have 0 orb capacity so this always
    resolves to Strength for them.
    """
    if not getattr(target, "is_player", False):
        return PowerId.STRENGTH
    player_state = combat.combat_player_state_for(target) if hasattr(combat, "combat_player_state_for") else None
    orb_capacity = getattr(getattr(player_state, "orb_queue", None), "capacity", 0)
    if orb_capacity <= 0:
        return PowerId.STRENGTH
    if combat.combat_targets_rng.next_float(1.0) < SPIRE_SHIELD_ORBS_FOCUS_DOWN_ODDS:
        return PowerId.FOCUS
    return PowerId.STRENGTH


def create_spire_shield(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIRE_SHIELD_TOUGH_HP, SPIRE_SHIELD_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=SPIRE_SHIELD_MONSTER_ID)

    state = _FightState(bits=0)

    def bash_move(combat: "CombatState") -> None:
        state.bits |= 1
        targets = living_player_targets(combat)
        _deal_damage_to_player(combat, creature, SPIRE_SHIELD_BASH_DAMAGE)
        if combat.is_over:
            return
        for target in targets:
            if not target.is_alive:
                continue
            power_id = _spire_shield_bash_debuff_power(combat, target)
            combat.apply_power_to(target, power_id, -1, applier=creature)

    def fortify_move(combat: "CombatState") -> None:
        state.bits |= 2
        gain_move_block(creature, SPIRE_SHIELD_FORTIFY_BLOCK, combat)
        for ally in combat.get_teammates_of(creature):
            gain_move_block(ally, SPIRE_SHIELD_FORTIFY_BLOCK, combat)

    def smash_move(combat: "CombatState") -> None:
        state.bits = 0
        total_dealt = 0
        for target in living_player_targets(combat):
            dmg = calculate_damage(SPIRE_SHIELD_SMASH_DAMAGE, creature, target, ValueProp.MOVE, combat)
            result = apply_damage(target, dmg, ValueProp.MOVE, combat, creature)
            total_dealt += result.total_damage
        combat._check_combat_end()  # noqa: SLF001
        if combat.is_over:
            return
        if ascension_level >= DEADLY_ENEMIES_ASCENSION_LEVEL:
            gain_move_block(creature, SPIRE_SHIELD_SMASH_FLAT_BLOCK_ASC9, combat)
        else:
            gain_move_block(creature, total_dealt, combat)

    bash = MoveState(
        SPIRE_SHIELD_BASH_MOVE, bash_move,
        [attack_intent(SPIRE_SHIELD_BASH_DAMAGE), debuff_intent()],
        follow_up_id=SPIRE_SHIELD_POST_ATTACK_BRANCH,
    )
    fortify = MoveState(SPIRE_SHIELD_FORTIFY_MOVE, fortify_move, [defend_intent()], follow_up_id=SPIRE_SHIELD_POST_ATTACK_BRANCH)
    smash = MoveState(
        SPIRE_SHIELD_SMASH_MOVE, smash_move,
        [attack_intent(SPIRE_SHIELD_SMASH_DAMAGE), defend_intent()],
        follow_up_id=SPIRE_SHIELD_RANDOM_BRANCH,
    )

    random_branch = RandomBranchState(SPIRE_SHIELD_RANDOM_BRANCH)
    random_branch.add_branch(SPIRE_SHIELD_BASH_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)
    random_branch.add_branch(SPIRE_SHIELD_FORTIFY_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)

    post_attack_branch = ConditionalBranchState(SPIRE_SHIELD_POST_ATTACK_BRANCH)
    post_attack_branch.add_branch(lambda: (state.bits & 1) == 0, SPIRE_SHIELD_BASH_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 2) == 0, SPIRE_SHIELD_FORTIFY_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 3) == 3, SPIRE_SHIELD_SMASH_MOVE)

    states: dict[str, object] = {
        SPIRE_SHIELD_BASH_MOVE: bash,
        SPIRE_SHIELD_FORTIFY_MOVE: fortify,
        SPIRE_SHIELD_SMASH_MOVE: smash,
        SPIRE_SHIELD_RANDOM_BRANCH: random_branch,
        SPIRE_SHIELD_POST_ATTACK_BRANCH: post_attack_branch,
    }

    # AfterAddedToRoom: BackAttackLeft(1) + Artifact.
    artifact_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_SHIELD_DEADLY_ARTIFACT, SPIRE_SHIELD_BASE_ARTIFACT)
    creature.apply_power(PowerId.BACK_ATTACK_LEFT, 1, applier=creature)
    creature.apply_power(PowerId.ARTIFACT, artifact_amount, applier=creature)

    return creature, MonsterAI(states, SPIRE_SHIELD_RANDOM_BRANCH, rng)


SPIRE_SPEAR_MONSTER_ID = "SPIRE_SPEAR"
SPIRE_SPEAR_BASE_HP = 160
SPIRE_SPEAR_TOUGH_HP = 180
SPIRE_SPEAR_BASE_ARTIFACT = 1
SPIRE_SPEAR_DEADLY_ARTIFACT = 2
SPIRE_SPEAR_BURN_STRIKE_DAMAGE = 6
SPIRE_SPEAR_BURN_STRIKE_COUNT = 2
SPIRE_SPEAR_BURN_STRIKE_CARD_COUNT = 2
SPIRE_SPEAR_SKEWER_DAMAGE = 10
SPIRE_SPEAR_SKEWER_COUNT = 4
SPIRE_SPEAR_PIERCER_AMOUNT = 2
SPIRE_SPEAR_SURROUNDED_AMOUNT = 1

SPIRE_SPEAR_BURN_STRIKE_MOVE = "BURN_STRIKE_MOVE"
SPIRE_SPEAR_SKEWER_MOVE = "SKEWER_MOVE"
SPIRE_SPEAR_PIERCER_MOVE = "PIERCER_MOVE"
SPIRE_SPEAR_RANDOM_BRANCH = "RANDOM_BRANCH"
SPIRE_SPEAR_POST_ATTACK_BRANCH = "POST_ATTACK_BRANCH"


def create_spire_spear(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIRE_SPEAR_TOUGH_HP, SPIRE_SPEAR_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=SPIRE_SPEAR_MONSTER_ID)

    # C# starts the bitfield at 2 (Piercer bit pre-set) so the very first
    # POST_ATTACK_BRANCH resolution after the forced opening Burn Strike
    # skips straight to Skewer instead of Piercer.
    state = _FightState(bits=2)

    def burn_strike_move(combat: "CombatState") -> None:
        state.bits |= 1
        _deal_damage_to_player(combat, creature, SPIRE_SPEAR_BURN_STRIKE_DAMAGE, hits=SPIRE_SPEAR_BURN_STRIKE_COUNT)
        if combat.is_over:
            return
        use_draw_pile = ascension_level >= DEADLY_ENEMIES_ASCENSION_LEVEL
        for target in living_player_targets(combat):
            for _ in range(SPIRE_SPEAR_BURN_STRIKE_CARD_COUNT):
                if use_draw_pile:
                    combat.add_generated_card_to_creature_draw_pile(target, make_burn(), added_by_player=False)
                else:
                    combat.add_generated_card_to_creature_discard(target, make_burn(), added_by_player=False)

    def skewer_move(combat: "CombatState") -> None:
        state.bits = 0
        _deal_damage_to_player(combat, creature, SPIRE_SPEAR_SKEWER_DAMAGE, hits=SPIRE_SPEAR_SKEWER_COUNT)

    def piercer_move(combat: "CombatState") -> None:
        state.bits |= 2
        combat.apply_power_to(creature, PowerId.STRENGTH, SPIRE_SPEAR_PIERCER_AMOUNT, applier=creature)
        for ally in combat.get_teammates_of(creature):
            combat.apply_power_to(ally, PowerId.STRENGTH, SPIRE_SPEAR_PIERCER_AMOUNT, applier=creature)

    burn_strike = MoveState(
        SPIRE_SPEAR_BURN_STRIKE_MOVE, burn_strike_move,
        [multi_attack_intent(SPIRE_SPEAR_BURN_STRIKE_DAMAGE, SPIRE_SPEAR_BURN_STRIKE_COUNT), status_intent()],
        follow_up_id=SPIRE_SPEAR_POST_ATTACK_BRANCH,
    )
    skewer = MoveState(
        SPIRE_SPEAR_SKEWER_MOVE, skewer_move,
        [multi_attack_intent(SPIRE_SPEAR_SKEWER_DAMAGE, SPIRE_SPEAR_SKEWER_COUNT)],
        follow_up_id=SPIRE_SPEAR_RANDOM_BRANCH,
    )
    piercer = MoveState(
        SPIRE_SPEAR_PIERCER_MOVE, piercer_move,
        [buff_intent()],
        follow_up_id=SPIRE_SPEAR_POST_ATTACK_BRANCH,
    )

    random_branch = RandomBranchState(SPIRE_SPEAR_RANDOM_BRANCH)
    random_branch.add_branch(SPIRE_SPEAR_BURN_STRIKE_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)
    random_branch.add_branch(SPIRE_SPEAR_PIERCER_MOVE, MoveRepeatType.CAN_REPEAT_FOREVER)

    post_attack_branch = ConditionalBranchState(SPIRE_SPEAR_POST_ATTACK_BRANCH)
    post_attack_branch.add_branch(lambda: (state.bits & 1) == 0, SPIRE_SPEAR_BURN_STRIKE_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 2) == 0, SPIRE_SPEAR_PIERCER_MOVE)
    post_attack_branch.add_branch(lambda: (state.bits & 3) == 3, SPIRE_SPEAR_SKEWER_MOVE)

    states: dict[str, object] = {
        SPIRE_SPEAR_BURN_STRIKE_MOVE: burn_strike,
        SPIRE_SPEAR_SKEWER_MOVE: skewer,
        SPIRE_SPEAR_PIERCER_MOVE: piercer,
        SPIRE_SPEAR_RANDOM_BRANCH: random_branch,
        SPIRE_SPEAR_POST_ATTACK_BRANCH: post_attack_branch,
    }

    # AfterAddedToRoom: BackAttackRight(1) + Artifact. (Surrounded is applied
    # to opponents by the encounter setup function, once combat/targets exist.)
    artifact_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIRE_SPEAR_DEADLY_ARTIFACT, SPIRE_SPEAR_BASE_ARTIFACT)
    creature.apply_power(PowerId.BACK_ATTACK_RIGHT, 1, applier=creature)
    creature.apply_power(PowerId.ARTIFACT, artifact_amount, applier=creature)

    return creature, MonsterAI(states, SPIRE_SPEAR_BURN_STRIKE_MOVE, rng)
