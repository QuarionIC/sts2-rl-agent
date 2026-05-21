"""Act 4 (Underdocks) monsters: weak, normal, elite, boss.

All HP ranges, damage values, and state machines verified against decompiled C# source.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import CombatSide, MoveRepeatType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import Rng
from sts2_env.cards.status import make_dazed, make_wound
from sts2_env.monsters.intents import (
    Intent, IntentType, attack_intent, multi_attack_intent,
    buff_intent, debuff_intent, strong_debuff_intent, status_intent,
    defend_intent, sleep_intent,
)
from sts2_env.monsters.state_machine import (
    ConditionalBranchState, MonsterAI, MonsterState, MoveState, RandomBranchState,
)
from sts2_env.monsters.block import gain_move_block
from sts2_env.monsters.targets import (
    add_generated_cards_to_living_player_discards,
    apply_power_to_living_player_targets,
    living_player_targets,
)

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ---- Helpers ----

TOUGH_ENEMIES_ASCENSION_LEVEL = 8
DEADLY_ENEMIES_ASCENSION_LEVEL = 9


def _ascension_value(ascension_level: int, threshold: int, ascension_value: int, base_value: int) -> int:
    return ascension_value if ascension_level >= threshold else base_value


def _combat_ascension_level(combat: CombatState) -> int:
    return combat.ascension_level


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


# ========================================================================
# WEAK ENCOUNTERS
# ========================================================================

# ---- CorpseSlug (HP 25-27 / 27-29 asc) ----

def create_corpse_slug(rng: Rng, starter_idx: int = 0) -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(25, 27)
    creature = Creature(max_hp=hp, monster_id="CORPSE_SLUG")
    whip_slap_dmg = 3
    glomp_dmg = 8
    goop_frail = 2

    def whip_slap(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, whip_slap_dmg, hits=2)

    def glomp(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, glomp_dmg)

    def goop(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, goop_frail, applier=creature)

    states: dict[str, MonsterState] = {
        "WHIP_SLAP_MOVE": MoveState(
            "WHIP_SLAP_MOVE",
            whip_slap,
            [multi_attack_intent(whip_slap_dmg, 2)],
            follow_up_id="GLOMP_MOVE",
        ),
        "GLOMP_MOVE": MoveState("GLOMP_MOVE", glomp, [attack_intent(glomp_dmg)], follow_up_id="GOOP_MOVE"),
        "GOOP_MOVE": MoveState("GOOP_MOVE", goop, [debuff_intent()], follow_up_id="WHIP_SLAP_MOVE"),
    }

    starter_map = {0: "WHIP_SLAP_MOVE", 1: "GLOMP_MOVE", 2: "GOOP_MOVE"}
    initial = starter_map.get(starter_idx, "WHIP_SLAP_MOVE")

    creature.apply_power(PowerId.RAVENOUS, 4)
    return creature, MonsterAI(states, initial, rng)


# ---- Seapunk (HP 44-46 / 47-49 asc) ----

SEAPUNK_MONSTER_ID = "SEAPUNK"
SEAPUNK_BASE_MIN_HP = 44
SEAPUNK_BASE_MAX_HP = 46
SEAPUNK_TOUGH_MIN_HP = 47
SEAPUNK_TOUGH_MAX_HP = 49
SEAPUNK_BASE_SEA_KICK_DAMAGE = 11
SEAPUNK_DEADLY_SEA_KICK_DAMAGE = 12
SEAPUNK_SPINNING_KICK_DAMAGE = 2
SEAPUNK_SPINNING_KICK_REPEAT = 4
SEAPUNK_BASE_BUBBLE_BLOCK = 7
SEAPUNK_TOUGH_BUBBLE_BLOCK = 8
SEAPUNK_BASE_BUBBLE_STRENGTH = 1
SEAPUNK_DEADLY_BUBBLE_STRENGTH = 2
SEAPUNK_SEA_KICK_MOVE = "SEA_KICK_MOVE"
SEAPUNK_SPINNING_KICK_MOVE = "SPINNING_KICK_MOVE"
SEAPUNK_BUBBLE_BURP_MOVE = "BUBBLE_BURP_MOVE"


def create_seapunk(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SEAPUNK_TOUGH_MIN_HP,
        SEAPUNK_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SEAPUNK_TOUGH_MAX_HP,
        SEAPUNK_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=SEAPUNK_MONSTER_ID)

    def sea_kick(combat: CombatState) -> None:
        sea_kick_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SEAPUNK_DEADLY_SEA_KICK_DAMAGE,
            SEAPUNK_BASE_SEA_KICK_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, sea_kick_dmg)

    def spinning_kick(combat: CombatState) -> None:
        _deal_damage_to_player(
            combat,
            creature,
            SEAPUNK_SPINNING_KICK_DAMAGE,
            hits=SEAPUNK_SPINNING_KICK_REPEAT,
        )

    def bubble_burp(combat: CombatState) -> None:
        bubble_block = _ascension_value(
            _combat_ascension_level(combat),
            TOUGH_ENEMIES_ASCENSION_LEVEL,
            SEAPUNK_TOUGH_BUBBLE_BLOCK,
            SEAPUNK_BASE_BUBBLE_BLOCK,
        )
        bubble_strength = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SEAPUNK_DEADLY_BUBBLE_STRENGTH,
            SEAPUNK_BASE_BUBBLE_STRENGTH,
        )
        _gain_block(creature, bubble_block, combat)
        creature.apply_power(PowerId.STRENGTH, bubble_strength, applier=creature)

    sea_kick_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SEAPUNK_DEADLY_SEA_KICK_DAMAGE,
        SEAPUNK_BASE_SEA_KICK_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        SEAPUNK_SEA_KICK_MOVE: MoveState(
            SEAPUNK_SEA_KICK_MOVE,
            sea_kick,
            [attack_intent(sea_kick_intent_damage)],
            follow_up_id=SEAPUNK_SPINNING_KICK_MOVE,
        ),
        SEAPUNK_SPINNING_KICK_MOVE: MoveState(
            SEAPUNK_SPINNING_KICK_MOVE,
            spinning_kick,
            [multi_attack_intent(SEAPUNK_SPINNING_KICK_DAMAGE, SEAPUNK_SPINNING_KICK_REPEAT)],
            follow_up_id=SEAPUNK_BUBBLE_BURP_MOVE,
        ),
        SEAPUNK_BUBBLE_BURP_MOVE: MoveState(
            SEAPUNK_BUBBLE_BURP_MOVE,
            bubble_burp,
            [buff_intent(), defend_intent()],
            follow_up_id=SEAPUNK_SEA_KICK_MOVE,
        ),
    }
    return creature, MonsterAI(states, SEAPUNK_SEA_KICK_MOVE)


# ---- SludgeSpinner (HP 37-39 / 41-42 asc) ----

SLUDGE_SPINNER_MONSTER_ID = "SLUDGE_SPINNER"
SLUDGE_SPINNER_BASE_MIN_HP = 37
SLUDGE_SPINNER_BASE_MAX_HP = 39
SLUDGE_SPINNER_TOUGH_MIN_HP = 41
SLUDGE_SPINNER_TOUGH_MAX_HP = 42
SLUDGE_SPINNER_BASE_OIL_SPRAY_DAMAGE = 8
SLUDGE_SPINNER_DEADLY_OIL_SPRAY_DAMAGE = 9
SLUDGE_SPINNER_OIL_SPRAY_WEAK = 1
SLUDGE_SPINNER_BASE_SLAM_DAMAGE = 11
SLUDGE_SPINNER_DEADLY_SLAM_DAMAGE = 12
SLUDGE_SPINNER_BASE_RAGE_DAMAGE = 6
SLUDGE_SPINNER_DEADLY_RAGE_DAMAGE = 7
SLUDGE_SPINNER_RAGE_STRENGTH = 3
SLUDGE_SPINNER_RANDOM_STATE = "RAND"
SLUDGE_SPINNER_OIL_SPRAY_MOVE = "OIL_SPRAY_MOVE"
SLUDGE_SPINNER_SLAM_MOVE = "SLAM_MOVE"
SLUDGE_SPINNER_RAGE_MOVE = "RAGE_MOVE"


def create_sludge_spinner(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SLUDGE_SPINNER_TOUGH_MIN_HP,
        SLUDGE_SPINNER_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SLUDGE_SPINNER_TOUGH_MAX_HP,
        SLUDGE_SPINNER_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=SLUDGE_SPINNER_MONSTER_ID)

    def oil_spray(combat: CombatState) -> None:
        oil_spray_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SLUDGE_SPINNER_DEADLY_OIL_SPRAY_DAMAGE,
            SLUDGE_SPINNER_BASE_OIL_SPRAY_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, oil_spray_dmg)
        apply_power_to_living_player_targets(
            combat,
            PowerId.WEAK,
            SLUDGE_SPINNER_OIL_SPRAY_WEAK,
            applier=creature,
        )

    def slam(combat: CombatState) -> None:
        slam_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SLUDGE_SPINNER_DEADLY_SLAM_DAMAGE,
            SLUDGE_SPINNER_BASE_SLAM_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, slam_dmg)

    def rage(combat: CombatState) -> None:
        rage_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SLUDGE_SPINNER_DEADLY_RAGE_DAMAGE,
            SLUDGE_SPINNER_BASE_RAGE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, rage_dmg)
        combat.apply_power_to(creature, PowerId.STRENGTH, SLUDGE_SPINNER_RAGE_STRENGTH, applier=creature)

    oil_spray_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SLUDGE_SPINNER_DEADLY_OIL_SPRAY_DAMAGE,
        SLUDGE_SPINNER_BASE_OIL_SPRAY_DAMAGE,
    )
    slam_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SLUDGE_SPINNER_DEADLY_SLAM_DAMAGE,
        SLUDGE_SPINNER_BASE_SLAM_DAMAGE,
    )
    rage_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SLUDGE_SPINNER_DEADLY_RAGE_DAMAGE,
        SLUDGE_SPINNER_BASE_RAGE_DAMAGE,
    )

    rand = RandomBranchState(SLUDGE_SPINNER_RANDOM_STATE)
    rand.add_branch(SLUDGE_SPINNER_OIL_SPRAY_MOVE, MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch(SLUDGE_SPINNER_SLAM_MOVE, MoveRepeatType.CANNOT_REPEAT)
    rand.add_branch(SLUDGE_SPINNER_RAGE_MOVE, MoveRepeatType.CANNOT_REPEAT)

    states: dict[str, MonsterState] = {
        SLUDGE_SPINNER_RANDOM_STATE: rand,
        SLUDGE_SPINNER_OIL_SPRAY_MOVE: MoveState(
            SLUDGE_SPINNER_OIL_SPRAY_MOVE,
            oil_spray,
            [attack_intent(oil_spray_intent_damage), debuff_intent()],
            follow_up_id=SLUDGE_SPINNER_RANDOM_STATE,
        ),
        SLUDGE_SPINNER_SLAM_MOVE: MoveState(
            SLUDGE_SPINNER_SLAM_MOVE,
            slam,
            [attack_intent(slam_intent_damage)],
            follow_up_id=SLUDGE_SPINNER_RANDOM_STATE,
        ),
        SLUDGE_SPINNER_RAGE_MOVE: MoveState(
            SLUDGE_SPINNER_RAGE_MOVE,
            rage,
            [attack_intent(rage_intent_damage), buff_intent()],
            follow_up_id=SLUDGE_SPINNER_RANDOM_STATE,
        ),
    }
    return creature, MonsterAI(states, SLUDGE_SPINNER_OIL_SPRAY_MOVE)


# ---- Toadpole (HP 21-25 / 22-26 asc) ----

def create_toadpole(rng: Rng, slot: str = "first") -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(21, 25)
    creature = Creature(max_hp=hp, monster_id="TOADPOLE")
    spike_spit_dmg = 3
    whirl_dmg = 7
    spiken_amount = 2

    def spike_spit(combat: CombatState) -> None:
        if creature.has_power(PowerId.THORNS):
            creature.apply_power(PowerId.THORNS, -spiken_amount, applier=creature)
        _deal_damage_to_player(combat, creature, spike_spit_dmg, hits=3)

    def whirl(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, whirl_dmg)

    def spiken(combat: CombatState) -> None:
        creature.apply_power(PowerId.THORNS, spiken_amount, applier=creature)

    is_front = slot in {"first", "front"}
    init = ConditionalBranchState("INIT_MOVE")
    init.add_branch(lambda: not is_front, "WHIRL_MOVE")
    init.add_branch(lambda: True, "SPIKEN_MOVE")

    states: dict[str, MonsterState] = {
        "INIT_MOVE": init,
        "SPIKE_SPIT_MOVE": MoveState(
            "SPIKE_SPIT_MOVE",
            spike_spit,
            [multi_attack_intent(spike_spit_dmg, 3)],
            follow_up_id="WHIRL_MOVE",
        ),
        "WHIRL_MOVE": MoveState("WHIRL_MOVE", whirl, [attack_intent(whirl_dmg)], follow_up_id="SPIKEN_MOVE"),
        "SPIKEN_MOVE": MoveState("SPIKEN_MOVE", spiken, [buff_intent()], follow_up_id="SPIKE_SPIT_MOVE"),
    }

    return creature, MonsterAI(states, "INIT_MOVE", rng)


# ========================================================================
# NORMAL ENCOUNTERS
# ========================================================================

# ---- CalcifiedCultist (HP 38-41 / 39-42 asc) ----

CULTIST_INCANTATION_MOVE = "INCANTATION_MOVE"
CULTIST_DARK_STRIKE_MOVE = "DARK_STRIKE_MOVE"
CALCIFIED_CULTIST_MONSTER_ID = "CALCIFIED_CULTIST"
CALCIFIED_CULTIST_BASE_MIN_HP = 38
CALCIFIED_CULTIST_BASE_MAX_HP = 41
CALCIFIED_CULTIST_TOUGH_MIN_HP = 39
CALCIFIED_CULTIST_TOUGH_MAX_HP = 42
CALCIFIED_CULTIST_BASE_DARK_STRIKE_DAMAGE = 9
CALCIFIED_CULTIST_DEADLY_DARK_STRIKE_DAMAGE = 11
CALCIFIED_CULTIST_INCANTATION_RITUAL = 2


def create_calcified_cultist(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        CALCIFIED_CULTIST_TOUGH_MIN_HP,
        CALCIFIED_CULTIST_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        CALCIFIED_CULTIST_TOUGH_MAX_HP,
        CALCIFIED_CULTIST_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=CALCIFIED_CULTIST_MONSTER_ID)

    def incantation(combat: CombatState) -> None:
        creature.apply_power(PowerId.RITUAL, CALCIFIED_CULTIST_INCANTATION_RITUAL)

    def dark_strike(combat: CombatState) -> None:
        dark_strike_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            CALCIFIED_CULTIST_DEADLY_DARK_STRIKE_DAMAGE,
            CALCIFIED_CULTIST_BASE_DARK_STRIKE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, dark_strike_dmg)

    dark_strike_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        CALCIFIED_CULTIST_DEADLY_DARK_STRIKE_DAMAGE,
        CALCIFIED_CULTIST_BASE_DARK_STRIKE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        CULTIST_INCANTATION_MOVE: MoveState(
            CULTIST_INCANTATION_MOVE,
            incantation,
            [buff_intent()],
            follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
        CULTIST_DARK_STRIKE_MOVE: MoveState(
            CULTIST_DARK_STRIKE_MOVE,
            dark_strike,
            [attack_intent(dark_strike_intent_damage)],
            follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
    }
    return creature, MonsterAI(states, CULTIST_INCANTATION_MOVE)


# ---- DampCultist (HP 51-53 / 52-54 asc) ----

DAMP_CULTIST_MONSTER_ID = "DAMP_CULTIST"
DAMP_CULTIST_BASE_MIN_HP = 51
DAMP_CULTIST_BASE_MAX_HP = 53
DAMP_CULTIST_TOUGH_MIN_HP = 52
DAMP_CULTIST_TOUGH_MAX_HP = 54
DAMP_CULTIST_BASE_DARK_STRIKE_DAMAGE = 1
DAMP_CULTIST_DEADLY_DARK_STRIKE_DAMAGE = 3
DAMP_CULTIST_BASE_INCANTATION_RITUAL = 5
DAMP_CULTIST_DEADLY_INCANTATION_RITUAL = 6


def create_damp_cultist(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        DAMP_CULTIST_TOUGH_MIN_HP,
        DAMP_CULTIST_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        DAMP_CULTIST_TOUGH_MAX_HP,
        DAMP_CULTIST_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=DAMP_CULTIST_MONSTER_ID)

    def incantation(combat: CombatState) -> None:
        ritual = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            DAMP_CULTIST_DEADLY_INCANTATION_RITUAL,
            DAMP_CULTIST_BASE_INCANTATION_RITUAL,
        )
        creature.apply_power(PowerId.RITUAL, ritual)

    def dark_strike(combat: CombatState) -> None:
        dark_strike_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            DAMP_CULTIST_DEADLY_DARK_STRIKE_DAMAGE,
            DAMP_CULTIST_BASE_DARK_STRIKE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, dark_strike_dmg)

    dark_strike_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        DAMP_CULTIST_DEADLY_DARK_STRIKE_DAMAGE,
        DAMP_CULTIST_BASE_DARK_STRIKE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        CULTIST_INCANTATION_MOVE: MoveState(
            CULTIST_INCANTATION_MOVE,
            incantation,
            [buff_intent()],
            follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
        CULTIST_DARK_STRIKE_MOVE: MoveState(
            CULTIST_DARK_STRIKE_MOVE,
            dark_strike,
            [attack_intent(dark_strike_intent_damage)],
            follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
    }
    return creature, MonsterAI(states, CULTIST_INCANTATION_MOVE)


# ---- FossilStalker (HP 51-53 / 54-56 asc) ----

FOSSIL_STALKER_MONSTER_ID = "FOSSIL_STALKER"
FOSSIL_STALKER_BASE_MIN_HP = 51
FOSSIL_STALKER_BASE_MAX_HP = 53
FOSSIL_STALKER_TOUGH_MIN_HP = 54
FOSSIL_STALKER_TOUGH_MAX_HP = 56
FOSSIL_STALKER_BASE_TACKLE_DAMAGE = 9
FOSSIL_STALKER_DEADLY_TACKLE_DAMAGE = 11
FOSSIL_STALKER_TACKLE_FRAIL = 1
FOSSIL_STALKER_BASE_LATCH_DAMAGE = 12
FOSSIL_STALKER_DEADLY_LATCH_DAMAGE = 14
FOSSIL_STALKER_BASE_LASH_DAMAGE = 3
FOSSIL_STALKER_DEADLY_LASH_DAMAGE = 4
FOSSIL_STALKER_LASH_REPEAT = 2
FOSSIL_STALKER_SUCK = 3
FOSSIL_STALKER_RANDOM_STATE = "RAND"
FOSSIL_STALKER_TACKLE_MOVE = "TACKLE_MOVE"
FOSSIL_STALKER_LATCH_MOVE = "LATCH_MOVE"
FOSSIL_STALKER_LASH_MOVE = "LASH_MOVE"
FOSSIL_STALKER_MOVE_WEIGHT = 2.0


def create_fossil_stalker(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FOSSIL_STALKER_TOUGH_MIN_HP,
        FOSSIL_STALKER_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FOSSIL_STALKER_TOUGH_MAX_HP,
        FOSSIL_STALKER_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=FOSSIL_STALKER_MONSTER_ID)

    def tackle(combat: CombatState) -> None:
        tackle_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FOSSIL_STALKER_DEADLY_TACKLE_DAMAGE,
            FOSSIL_STALKER_BASE_TACKLE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, tackle_dmg)
        apply_power_to_living_player_targets(
            combat,
            PowerId.FRAIL,
            FOSSIL_STALKER_TACKLE_FRAIL,
            applier=creature,
        )

    def latch(combat: CombatState) -> None:
        latch_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FOSSIL_STALKER_DEADLY_LATCH_DAMAGE,
            FOSSIL_STALKER_BASE_LATCH_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, latch_dmg)

    def lash(combat: CombatState) -> None:
        lash_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            FOSSIL_STALKER_DEADLY_LASH_DAMAGE,
            FOSSIL_STALKER_BASE_LASH_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, lash_dmg, hits=FOSSIL_STALKER_LASH_REPEAT)

    tackle_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FOSSIL_STALKER_DEADLY_TACKLE_DAMAGE,
        FOSSIL_STALKER_BASE_TACKLE_DAMAGE,
    )
    latch_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FOSSIL_STALKER_DEADLY_LATCH_DAMAGE,
        FOSSIL_STALKER_BASE_LATCH_DAMAGE,
    )
    lash_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        FOSSIL_STALKER_DEADLY_LASH_DAMAGE,
        FOSSIL_STALKER_BASE_LASH_DAMAGE,
    )

    rand = RandomBranchState(FOSSIL_STALKER_RANDOM_STATE)
    rand.add_branch(FOSSIL_STALKER_LATCH_MOVE, weight=FOSSIL_STALKER_MOVE_WEIGHT)
    rand.add_branch(FOSSIL_STALKER_TACKLE_MOVE, weight=FOSSIL_STALKER_MOVE_WEIGHT)
    rand.add_branch(FOSSIL_STALKER_LASH_MOVE, weight=FOSSIL_STALKER_MOVE_WEIGHT)

    states: dict[str, MonsterState] = {
        FOSSIL_STALKER_RANDOM_STATE: rand,
        FOSSIL_STALKER_TACKLE_MOVE: MoveState(
            FOSSIL_STALKER_TACKLE_MOVE,
            tackle,
            [attack_intent(tackle_intent_damage), debuff_intent()],
            follow_up_id=FOSSIL_STALKER_RANDOM_STATE,
        ),
        FOSSIL_STALKER_LATCH_MOVE: MoveState(
            FOSSIL_STALKER_LATCH_MOVE,
            latch,
            [attack_intent(latch_intent_damage)],
            follow_up_id=FOSSIL_STALKER_RANDOM_STATE,
        ),
        FOSSIL_STALKER_LASH_MOVE: MoveState(
            FOSSIL_STALKER_LASH_MOVE,
            lash,
            [multi_attack_intent(lash_intent_damage, FOSSIL_STALKER_LASH_REPEAT)],
            follow_up_id=FOSSIL_STALKER_RANDOM_STATE,
        ),
    }

    creature.apply_power(PowerId.SUCK, FOSSIL_STALKER_SUCK)
    return creature, MonsterAI(states, FOSSIL_STALKER_LATCH_MOVE)


# ---- GremlinMerc (HP 47-49 / 51-53 asc) + SneakyGremlin + FatGremlin ----

GREMLIN_SPAWNED_MOVE = "SPAWNED_MOVE"
GREMLIN_TACKLE_MOVE = "TACKLE_MOVE"
SNEAKY_GREMLIN_MONSTER_ID = "SNEAKY_GREMLIN"
SNEAKY_GREMLIN_BASE_MIN_HP = 10
SNEAKY_GREMLIN_BASE_MAX_HP = 14
SNEAKY_GREMLIN_TOUGH_MIN_HP = 11
SNEAKY_GREMLIN_TOUGH_MAX_HP = 15
SNEAKY_GREMLIN_BASE_TACKLE_DAMAGE = 9
SNEAKY_GREMLIN_DEADLY_TACKLE_DAMAGE = 10


def create_sneaky_gremlin(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SNEAKY_GREMLIN_TOUGH_MIN_HP,
        SNEAKY_GREMLIN_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SNEAKY_GREMLIN_TOUGH_MAX_HP,
        SNEAKY_GREMLIN_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=SNEAKY_GREMLIN_MONSTER_ID)

    def spawned(combat: CombatState) -> None:
        pass

    def tackle(combat: CombatState) -> None:
        tackle_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SNEAKY_GREMLIN_DEADLY_TACKLE_DAMAGE,
            SNEAKY_GREMLIN_BASE_TACKLE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, tackle_dmg)

    tackle_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SNEAKY_GREMLIN_DEADLY_TACKLE_DAMAGE,
        SNEAKY_GREMLIN_BASE_TACKLE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        GREMLIN_SPAWNED_MOVE: MoveState(
            GREMLIN_SPAWNED_MOVE,
            spawned,
            [Intent(IntentType.STUN)],
            follow_up_id=GREMLIN_TACKLE_MOVE,
        ),
        GREMLIN_TACKLE_MOVE: MoveState(
            GREMLIN_TACKLE_MOVE,
            tackle,
            [attack_intent(tackle_intent_damage)],
            follow_up_id=GREMLIN_TACKLE_MOVE,
        ),
    }
    return creature, MonsterAI(states, GREMLIN_SPAWNED_MOVE)


FAT_GREMLIN_MONSTER_ID = "FAT_GREMLIN"
FAT_GREMLIN_BASE_MIN_HP = 13
FAT_GREMLIN_BASE_MAX_HP = 17
FAT_GREMLIN_TOUGH_MIN_HP = 14
FAT_GREMLIN_TOUGH_MAX_HP = 18
FAT_GREMLIN_FLEE_MOVE = "FLEE_MOVE"


def create_fat_gremlin(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FAT_GREMLIN_TOUGH_MIN_HP,
        FAT_GREMLIN_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        FAT_GREMLIN_TOUGH_MAX_HP,
        FAT_GREMLIN_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=FAT_GREMLIN_MONSTER_ID)

    def spawned(combat: CombatState) -> None:
        pass

    def flee(combat: CombatState) -> None:
        combat.escape_creature(creature)

    states: dict[str, MonsterState] = {
        GREMLIN_SPAWNED_MOVE: MoveState(
            GREMLIN_SPAWNED_MOVE,
            spawned,
            [Intent(IntentType.STUN)],
            follow_up_id=FAT_GREMLIN_FLEE_MOVE,
        ),
        FAT_GREMLIN_FLEE_MOVE: MoveState(
            FAT_GREMLIN_FLEE_MOVE,
            flee,
            [Intent(IntentType.ESCAPE)],
            follow_up_id=FAT_GREMLIN_FLEE_MOVE,
        ),
    }
    return creature, MonsterAI(states, GREMLIN_SPAWNED_MOVE)


GREMLIN_MERC_MONSTER_ID = "GREMLIN_MERC"
GREMLIN_MERC_BASE_MIN_HP = 47
GREMLIN_MERC_BASE_MAX_HP = 49
GREMLIN_MERC_TOUGH_MIN_HP = 51
GREMLIN_MERC_TOUGH_MAX_HP = 53
GREMLIN_MERC_BASE_GIMME_DAMAGE = 7
GREMLIN_MERC_TOUGH_GIMME_DAMAGE = 8
GREMLIN_MERC_GIMME_REPEAT = 2
GREMLIN_MERC_BASE_DOUBLE_SMASH_DAMAGE = 6
GREMLIN_MERC_TOUGH_DOUBLE_SMASH_DAMAGE = 7
GREMLIN_MERC_DOUBLE_SMASH_REPEAT = 2
GREMLIN_MERC_DOUBLE_SMASH_WEAK = 2
GREMLIN_MERC_BASE_HEHE_DAMAGE = 8
GREMLIN_MERC_TOUGH_HEHE_DAMAGE = 9
GREMLIN_MERC_HEHE_STRENGTH = 2
GREMLIN_MERC_SURPRISE = 1
GREMLIN_MERC_THIEVERY = 20
GREMLIN_MERC_GIMME_MOVE = "GIMME_MOVE"
GREMLIN_MERC_DOUBLE_SMASH_MOVE = "DOUBLE_SMASH_MOVE"
GREMLIN_MERC_HEHE_MOVE = "HEHE_MOVE"


def create_gremlin_merc(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GREMLIN_MERC_TOUGH_MIN_HP,
        GREMLIN_MERC_BASE_MIN_HP,
    )
    max_hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GREMLIN_MERC_TOUGH_MAX_HP,
        GREMLIN_MERC_BASE_MAX_HP,
    )
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_MERC_MONSTER_ID)

    def gimme(combat: CombatState) -> None:
        gimme_dmg = _ascension_value(
            _combat_ascension_level(combat),
            TOUGH_ENEMIES_ASCENSION_LEVEL,
            GREMLIN_MERC_TOUGH_GIMME_DAMAGE,
            GREMLIN_MERC_BASE_GIMME_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, gimme_dmg, hits=GREMLIN_MERC_GIMME_REPEAT)

    def double_smash(combat: CombatState) -> None:
        double_smash_dmg = _ascension_value(
            _combat_ascension_level(combat),
            TOUGH_ENEMIES_ASCENSION_LEVEL,
            GREMLIN_MERC_TOUGH_DOUBLE_SMASH_DAMAGE,
            GREMLIN_MERC_BASE_DOUBLE_SMASH_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, double_smash_dmg, hits=GREMLIN_MERC_DOUBLE_SMASH_REPEAT)
        apply_power_to_living_player_targets(
            combat,
            PowerId.WEAK,
            GREMLIN_MERC_DOUBLE_SMASH_WEAK,
            applier=creature,
        )

    def hehe(combat: CombatState) -> None:
        hehe_dmg = _ascension_value(
            _combat_ascension_level(combat),
            TOUGH_ENEMIES_ASCENSION_LEVEL,
            GREMLIN_MERC_TOUGH_HEHE_DAMAGE,
            GREMLIN_MERC_BASE_HEHE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, hehe_dmg)
        combat.apply_power_to(creature, PowerId.STRENGTH, GREMLIN_MERC_HEHE_STRENGTH, applier=creature)

    gimme_intent_damage = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GREMLIN_MERC_TOUGH_GIMME_DAMAGE,
        GREMLIN_MERC_BASE_GIMME_DAMAGE,
    )
    double_smash_intent_damage = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GREMLIN_MERC_TOUGH_DOUBLE_SMASH_DAMAGE,
        GREMLIN_MERC_BASE_DOUBLE_SMASH_DAMAGE,
    )
    hehe_intent_damage = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GREMLIN_MERC_TOUGH_HEHE_DAMAGE,
        GREMLIN_MERC_BASE_HEHE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        GREMLIN_MERC_GIMME_MOVE: MoveState(
            GREMLIN_MERC_GIMME_MOVE,
            gimme,
            [multi_attack_intent(gimme_intent_damage, GREMLIN_MERC_GIMME_REPEAT)],
            follow_up_id=GREMLIN_MERC_DOUBLE_SMASH_MOVE,
        ),
        GREMLIN_MERC_DOUBLE_SMASH_MOVE: MoveState(
            GREMLIN_MERC_DOUBLE_SMASH_MOVE,
            double_smash,
            [multi_attack_intent(double_smash_intent_damage, GREMLIN_MERC_DOUBLE_SMASH_REPEAT), debuff_intent()],
            follow_up_id=GREMLIN_MERC_HEHE_MOVE,
        ),
        GREMLIN_MERC_HEHE_MOVE: MoveState(
            GREMLIN_MERC_HEHE_MOVE,
            hehe,
            [attack_intent(hehe_intent_damage), buff_intent()],
            follow_up_id=GREMLIN_MERC_GIMME_MOVE,
        ),
    }

    creature.apply_power(PowerId.SURPRISE, GREMLIN_MERC_SURPRISE)
    creature.apply_power(PowerId.THIEVERY, GREMLIN_MERC_THIEVERY)
    return creature, MonsterAI(states, GREMLIN_MERC_GIMME_MOVE)


# ---- HauntedShip (HP 63 / 67 asc) ----

HAUNTED_SHIP_MONSTER_ID = "HAUNTED_SHIP"
HAUNTED_SHIP_BASE_HP = 63
HAUNTED_SHIP_TOUGH_HP = 67
HAUNTED_SHIP_BASE_RAMMING_SPEED_DAMAGE = 10
HAUNTED_SHIP_DEADLY_RAMMING_SPEED_DAMAGE = 11
HAUNTED_SHIP_RAMMING_SPEED_STATUS_COUNT = 2
HAUNTED_SHIP_BASE_SWIPE_DAMAGE = 13
HAUNTED_SHIP_DEADLY_SWIPE_DAMAGE = 14
HAUNTED_SHIP_BASE_STOMP_DAMAGE = 4
HAUNTED_SHIP_DEADLY_STOMP_DAMAGE = 5
HAUNTED_SHIP_STOMP_REPEAT = 3
HAUNTED_SHIP_HAUNT_DEBUFF = 2
HAUNTED_SHIP_RANDOM_STATE = "RAND"
HAUNTED_SHIP_RAMMING_SPEED_MOVE = "RAMMING_SPEED_MOVE"
HAUNTED_SHIP_SWIPE_MOVE = "SWIPE_MOVE"
HAUNTED_SHIP_STOMP_MOVE = "STOMP_MOVE"
HAUNTED_SHIP_HAUNT_MOVE = "HAUNT_MOVE"


def create_haunted_ship(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        HAUNTED_SHIP_TOUGH_HP,
        HAUNTED_SHIP_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=HAUNTED_SHIP_MONSTER_ID)

    def _odd_round_weight() -> float:
        combat = creature.combat_state
        return 1.0 if combat is None or combat.round_number % 2 != 0 else 0.0

    def _even_round_weight() -> float:
        combat = creature.combat_state
        return 1.0 if combat is not None and combat.round_number % 2 == 0 else 0.0

    def ramming_speed(combat: CombatState) -> None:
        ramming_speed_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            HAUNTED_SHIP_DEADLY_RAMMING_SPEED_DAMAGE,
            HAUNTED_SHIP_BASE_RAMMING_SPEED_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, ramming_speed_dmg)
        if combat.is_over:
            return
        add_generated_cards_to_living_player_discards(
            combat,
            make_wound,
            HAUNTED_SHIP_RAMMING_SPEED_STATUS_COUNT,
        )

    def swipe(combat: CombatState) -> None:
        swipe_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            HAUNTED_SHIP_DEADLY_SWIPE_DAMAGE,
            HAUNTED_SHIP_BASE_SWIPE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, swipe_dmg)

    def stomp(combat: CombatState) -> None:
        stomp_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            HAUNTED_SHIP_DEADLY_STOMP_DAMAGE,
            HAUNTED_SHIP_BASE_STOMP_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, stomp_dmg, hits=HAUNTED_SHIP_STOMP_REPEAT)

    def haunt(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, HAUNTED_SHIP_HAUNT_DEBUFF, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, HAUNTED_SHIP_HAUNT_DEBUFF, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, HAUNTED_SHIP_HAUNT_DEBUFF, applier=creature)

    ramming_speed_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        HAUNTED_SHIP_DEADLY_RAMMING_SPEED_DAMAGE,
        HAUNTED_SHIP_BASE_RAMMING_SPEED_DAMAGE,
    )
    swipe_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        HAUNTED_SHIP_DEADLY_SWIPE_DAMAGE,
        HAUNTED_SHIP_BASE_SWIPE_DAMAGE,
    )
    stomp_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        HAUNTED_SHIP_DEADLY_STOMP_DAMAGE,
        HAUNTED_SHIP_BASE_STOMP_DAMAGE,
    )

    rand = RandomBranchState(HAUNTED_SHIP_RANDOM_STATE)
    rand.add_branch(HAUNTED_SHIP_RAMMING_SPEED_MOVE, MoveRepeatType.CANNOT_REPEAT, weight=_odd_round_weight)
    rand.add_branch(HAUNTED_SHIP_SWIPE_MOVE, MoveRepeatType.CANNOT_REPEAT, weight=_odd_round_weight)
    rand.add_branch(HAUNTED_SHIP_STOMP_MOVE, MoveRepeatType.CANNOT_REPEAT, weight=_odd_round_weight)
    rand.add_branch(HAUNTED_SHIP_HAUNT_MOVE, MoveRepeatType.USE_ONLY_ONCE, weight=_even_round_weight)

    states: dict[str, MonsterState] = {
        HAUNTED_SHIP_RANDOM_STATE: rand,
        HAUNTED_SHIP_RAMMING_SPEED_MOVE: MoveState(
            HAUNTED_SHIP_RAMMING_SPEED_MOVE,
            ramming_speed,
            [attack_intent(ramming_speed_intent_damage), status_intent()],
            follow_up_id=HAUNTED_SHIP_RANDOM_STATE,
        ),
        HAUNTED_SHIP_SWIPE_MOVE: MoveState(
            HAUNTED_SHIP_SWIPE_MOVE,
            swipe,
            [attack_intent(swipe_intent_damage)],
            follow_up_id=HAUNTED_SHIP_RANDOM_STATE,
        ),
        HAUNTED_SHIP_STOMP_MOVE: MoveState(
            HAUNTED_SHIP_STOMP_MOVE,
            stomp,
            [multi_attack_intent(stomp_intent_damage, HAUNTED_SHIP_STOMP_REPEAT)],
            follow_up_id=HAUNTED_SHIP_RANDOM_STATE,
        ),
        HAUNTED_SHIP_HAUNT_MOVE: MoveState(
            HAUNTED_SHIP_HAUNT_MOVE,
            haunt,
            [debuff_intent()],
            follow_up_id=HAUNTED_SHIP_RANDOM_STATE,
        ),
    }
    states[HAUNTED_SHIP_RAMMING_SPEED_MOVE].intents[1].hits = HAUNTED_SHIP_RAMMING_SPEED_STATUS_COUNT
    return creature, MonsterAI(states, HAUNTED_SHIP_RAMMING_SPEED_MOVE)


# ---- LivingFog (HP 80 / 82 asc) + GasBomb ----

GAS_BOMB_MONSTER_ID = "GAS_BOMB"
GAS_BOMB_BASE_HP = 10
GAS_BOMB_TOUGH_HP = 12
GAS_BOMB_BASE_EXPLODE_DAMAGE = 8
GAS_BOMB_DEADLY_EXPLODE_DAMAGE = 9
GAS_BOMB_EXPLODE_MOVE = "EXPLODE_MOVE"


def create_gas_bomb(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        GAS_BOMB_TOUGH_HP,
        GAS_BOMB_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=GAS_BOMB_MONSTER_ID)

    def explode(combat: CombatState) -> None:
        explode_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            GAS_BOMB_DEADLY_EXPLODE_DAMAGE,
            GAS_BOMB_BASE_EXPLODE_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, explode_dmg)
        combat.kill_creature(creature)

    explode_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        GAS_BOMB_DEADLY_EXPLODE_DAMAGE,
        GAS_BOMB_BASE_EXPLODE_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        GAS_BOMB_EXPLODE_MOVE: MoveState(
            GAS_BOMB_EXPLODE_MOVE,
            explode,
            [Intent(IntentType.DEATH_BLOW, damage=explode_intent_damage)],
            follow_up_id=GAS_BOMB_EXPLODE_MOVE,
        ),
    }
    return creature, MonsterAI(states, GAS_BOMB_EXPLODE_MOVE)


LIVING_FOG_MONSTER_ID = "LIVING_FOG"
LIVING_FOG_BASE_HP = 80
LIVING_FOG_TOUGH_HP = 82
LIVING_FOG_BASE_ADVANCED_GAS_DAMAGE = 8
LIVING_FOG_DEADLY_ADVANCED_GAS_DAMAGE = 9
LIVING_FOG_ADVANCED_GAS_SMOGGY = 1
LIVING_FOG_BASE_BLOAT_DAMAGE = 5
LIVING_FOG_DEADLY_BLOAT_DAMAGE = 6
LIVING_FOG_BASE_SUPER_GAS_BLAST_DAMAGE = 8
LIVING_FOG_DEADLY_SUPER_GAS_BLAST_DAMAGE = 9
LIVING_FOG_INITIAL_BLOAT_AMOUNT = 1
LIVING_FOG_MAX_BLOAT_AMOUNT = 5
LIVING_FOG_BLOAT_AMOUNT_KEY = "bloat_amount"
LIVING_FOG_ADVANCED_GAS_MOVE = "ADVANCED_GAS_MOVE"
LIVING_FOG_BLOAT_MOVE = "BLOAT_MOVE"
LIVING_FOG_SUPER_GAS_BLAST_MOVE = "SUPER_GAS_BLAST_MOVE"


def create_living_fog(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        LIVING_FOG_TOUGH_HP,
        LIVING_FOG_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=LIVING_FOG_MONSTER_ID)
    state = {LIVING_FOG_BLOAT_AMOUNT_KEY: LIVING_FOG_INITIAL_BLOAT_AMOUNT}

    def advanced_gas(combat: CombatState) -> None:
        advanced_gas_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            LIVING_FOG_DEADLY_ADVANCED_GAS_DAMAGE,
            LIVING_FOG_BASE_ADVANCED_GAS_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, advanced_gas_dmg)
        apply_power_to_living_player_targets(
            combat,
            PowerId.SMOGGY,
            LIVING_FOG_ADVANCED_GAS_SMOGGY,
            applier=creature,
        )

    def bloat(combat: CombatState) -> None:
        bloat_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            LIVING_FOG_DEADLY_BLOAT_DAMAGE,
            LIVING_FOG_BASE_BLOAT_DAMAGE,
        )
        for _ in range(state[LIVING_FOG_BLOAT_AMOUNT_KEY]):
            bomb, bomb_ai = create_gas_bomb(rng, ascension_level=_combat_ascension_level(combat))
            combat.add_enemy(bomb, bomb_ai)
        state[LIVING_FOG_BLOAT_AMOUNT_KEY] = min(
            state[LIVING_FOG_BLOAT_AMOUNT_KEY] + 1,
            LIVING_FOG_MAX_BLOAT_AMOUNT,
        )
        _deal_damage_to_player(combat, creature, bloat_dmg)

    def super_gas_blast(combat: CombatState) -> None:
        super_gas_blast_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            LIVING_FOG_DEADLY_SUPER_GAS_BLAST_DAMAGE,
            LIVING_FOG_BASE_SUPER_GAS_BLAST_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, super_gas_blast_dmg)

    advanced_gas_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        LIVING_FOG_DEADLY_ADVANCED_GAS_DAMAGE,
        LIVING_FOG_BASE_ADVANCED_GAS_DAMAGE,
    )
    bloat_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        LIVING_FOG_DEADLY_BLOAT_DAMAGE,
        LIVING_FOG_BASE_BLOAT_DAMAGE,
    )
    super_gas_blast_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        LIVING_FOG_DEADLY_SUPER_GAS_BLAST_DAMAGE,
        LIVING_FOG_BASE_SUPER_GAS_BLAST_DAMAGE,
    )

    states: dict[str, MonsterState] = {
        LIVING_FOG_ADVANCED_GAS_MOVE: MoveState(
            LIVING_FOG_ADVANCED_GAS_MOVE,
            advanced_gas,
            [attack_intent(advanced_gas_intent_damage), Intent(IntentType.CARD_DEBUFF)],
            follow_up_id=LIVING_FOG_BLOAT_MOVE,
        ),
        LIVING_FOG_BLOAT_MOVE: MoveState(
            LIVING_FOG_BLOAT_MOVE,
            bloat,
            [attack_intent(bloat_intent_damage), Intent(IntentType.SUMMON)],
            follow_up_id=LIVING_FOG_SUPER_GAS_BLAST_MOVE,
        ),
        LIVING_FOG_SUPER_GAS_BLAST_MOVE: MoveState(
            LIVING_FOG_SUPER_GAS_BLAST_MOVE,
            super_gas_blast,
            [attack_intent(super_gas_blast_intent_damage)],
            follow_up_id=LIVING_FOG_BLOAT_MOVE,
        ),
    }
    return creature, MonsterAI(states, LIVING_FOG_ADVANCED_GAS_MOVE)


# ---- PunchConstruct (HP 55 / 60 asc) ----

def create_punch_construct(
    rng: Rng,
    *,
    starts_with_strong_punch: bool = False,
    starting_hp_reduction: int = 0,
) -> tuple[Creature, MonsterAI]:
    hp = 55
    creature = Creature(max_hp=hp, monster_id="PUNCH_CONSTRUCT")
    strong_punch_dmg = 14
    fast_punch_dmg = 5
    fast_punch_weak = 1
    ready_block = 10

    if starting_hp_reduction > 0:
        creature.current_hp = max(1, creature.current_hp - starting_hp_reduction)
    creature.apply_power(PowerId.ARTIFACT, 1)

    def ready(combat: CombatState) -> None:
        _gain_block(creature, ready_block, combat)

    def strong_punch(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, strong_punch_dmg)

    def fast_punch(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, fast_punch_dmg, hits=2)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, fast_punch_weak, applier=creature)

    states: dict[str, MonsterState] = {
        "READY_MOVE": MoveState(
            "READY_MOVE",
            ready,
            [defend_intent()],
            follow_up_id="STRONG_PUNCH_MOVE",
        ),
        "STRONG_PUNCH_MOVE": MoveState(
            "STRONG_PUNCH_MOVE",
            strong_punch,
            [attack_intent(strong_punch_dmg)],
            follow_up_id="FAST_PUNCH_MOVE",
        ),
        "FAST_PUNCH_MOVE": MoveState(
            "FAST_PUNCH_MOVE",
            fast_punch,
            [multi_attack_intent(fast_punch_dmg, 2), debuff_intent()],
            follow_up_id="READY_MOVE",
        ),
    }
    initial = "STRONG_PUNCH_MOVE" if starts_with_strong_punch else "READY_MOVE"
    return creature, MonsterAI(states, initial)


# ---- SewerClam (HP 56 / 58 asc) ----

SEWER_CLAM_MONSTER_ID = "SEWER_CLAM"
SEWER_CLAM_BASE_HP = 56
SEWER_CLAM_TOUGH_HP = 58
SEWER_CLAM_BASE_JET_DAMAGE = 10
SEWER_CLAM_DEADLY_JET_DAMAGE = 11
SEWER_CLAM_PRESSURIZE_STRENGTH = 4
SEWER_CLAM_BASE_PLATING = 8
SEWER_CLAM_TOUGH_PLATING = 9
SEWER_CLAM_PRESSURIZE_MOVE = "PRESSURIZE_MOVE"
SEWER_CLAM_JET_MOVE = "JET_MOVE"


def create_sewer_clam(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SEWER_CLAM_TOUGH_HP,
        SEWER_CLAM_BASE_HP,
    )
    creature = Creature(max_hp=hp, monster_id=SEWER_CLAM_MONSTER_ID)

    def pressurize(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, SEWER_CLAM_PRESSURIZE_STRENGTH, applier=creature)

    def jet(combat: CombatState) -> None:
        jet_dmg = _ascension_value(
            _combat_ascension_level(combat),
            DEADLY_ENEMIES_ASCENSION_LEVEL,
            SEWER_CLAM_DEADLY_JET_DAMAGE,
            SEWER_CLAM_BASE_JET_DAMAGE,
        )
        _deal_damage_to_player(combat, creature, jet_dmg)

    jet_intent_damage = _ascension_value(
        ascension_level,
        DEADLY_ENEMIES_ASCENSION_LEVEL,
        SEWER_CLAM_DEADLY_JET_DAMAGE,
        SEWER_CLAM_BASE_JET_DAMAGE,
    )
    plating = _ascension_value(
        ascension_level,
        TOUGH_ENEMIES_ASCENSION_LEVEL,
        SEWER_CLAM_TOUGH_PLATING,
        SEWER_CLAM_BASE_PLATING,
    )

    states: dict[str, MonsterState] = {
        SEWER_CLAM_PRESSURIZE_MOVE: MoveState(
            SEWER_CLAM_PRESSURIZE_MOVE,
            pressurize,
            [buff_intent()],
            follow_up_id=SEWER_CLAM_JET_MOVE,
        ),
        SEWER_CLAM_JET_MOVE: MoveState(
            SEWER_CLAM_JET_MOVE,
            jet,
            [attack_intent(jet_intent_damage)],
            follow_up_id=SEWER_CLAM_PRESSURIZE_MOVE,
        ),
    }
    creature.apply_power(PowerId.PLATING, plating)
    return creature, MonsterAI(states, SEWER_CLAM_JET_MOVE)


# ---- TwoTailedRat (HP 17-21 / 18-22 asc) ----

def create_two_tailed_rat(rng: Rng, starter_move_idx: int = -1) -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(17, 21)
    creature = Creature(max_hp=hp, monster_id="TWO_TAILED_RAT")
    scratch_dmg = 8
    disease_bite_dmg = 6
    screech_frail = 1
    state = {
        "turns_until_summonable": 2,
        "call_for_backup_count": 0,
    }

    def can_summon(combat: CombatState | None = None) -> bool:
        combat = combat or creature.combat_state
        if combat is None:
            return False
        if state["turns_until_summonable"] > 0:
            return False
        if state["call_for_backup_count"] >= 3:
            return False
        alive_rats = [
            enemy
            for enemy in combat.enemies
            if enemy.monster_id == "TWO_TAILED_RAT" and enemy.is_alive
        ]
        if len(alive_rats) >= 5:
            return False
        for enemy in alive_rats:
            if enemy is creature:
                continue
            ai = combat.enemy_ais.get(enemy.combat_id)
            if ai is not None and ai.current_move.state_id == "CALL_FOR_BACKUP_MOVE":
                return False
        return True

    def _attack_performed() -> None:
        state["turns_until_summonable"] -= 1

    def scratch(combat: CombatState) -> None:
        _attack_performed()
        _deal_damage_to_player(combat, creature, scratch_dmg)

    def disease_bite(combat: CombatState) -> None:
        _attack_performed()
        _deal_damage_to_player(combat, creature, disease_bite_dmg)

    def screech(combat: CombatState) -> None:
        _attack_performed()
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, screech_frail, applier=creature)

    def call_for_backup(combat: CombatState) -> None:
        if can_summon(combat):
            backup, backup_ai = create_two_tailed_rat(rng)
            combat.add_enemy(backup, backup_ai)
        rat_ais = [
            combat.enemy_ais[enemy.combat_id]
            for enemy in combat.enemies
            if enemy.monster_id == "TWO_TAILED_RAT" and enemy.combat_id in combat.enemy_ais
        ]
        max_count = max(
            getattr(ai, "_two_tailed_rat_state", {}).get("call_for_backup_count", 0) + 1
            for ai in rat_ais
        )
        for ai in rat_ais:
            rat_state = getattr(ai, "_two_tailed_rat_state", None)
            if rat_state is not None:
                rat_state["call_for_backup_count"] = max_count

    rand = RandomBranchState("RAND")
    rand.add_branch(
        "SCRATCH_MOVE",
        MoveRepeatType.CANNOT_REPEAT,
        weight=lambda: 1.0 / 12.0 if can_summon() else 1.0,
    )
    rand.add_branch(
        "DISEASE_BITE_MOVE",
        MoveRepeatType.CANNOT_REPEAT,
        weight=lambda: 1.0 / 12.0 if can_summon() else 1.0,
    )
    rand.add_branch(
        "SCREECH_MOVE",
        MoveRepeatType.CANNOT_REPEAT,
        weight=lambda: 1.0 / 12.0 if can_summon() else 3.0,
    )
    rand.add_branch(
        "CALL_FOR_BACKUP_MOVE",
        MoveRepeatType.USE_ONLY_ONCE,
        weight=lambda: 0.75 if can_summon() else 0.0,
    )

    states: dict[str, MonsterState] = {
        "RAND": rand,
        "SCRATCH_MOVE": MoveState(
            "SCRATCH_MOVE",
            scratch,
            [attack_intent(scratch_dmg)],
            follow_up_id="RAND",
        ),
        "DISEASE_BITE_MOVE": MoveState(
            "DISEASE_BITE_MOVE",
            disease_bite,
            [attack_intent(disease_bite_dmg)],
            follow_up_id="RAND",
        ),
        "SCREECH_MOVE": MoveState(
            "SCREECH_MOVE",
            screech,
            [debuff_intent()],
            follow_up_id="RAND",
        ),
        "CALL_FOR_BACKUP_MOVE": MoveState(
            "CALL_FOR_BACKUP_MOVE",
            call_for_backup,
            [Intent(IntentType.SUMMON)],
            follow_up_id="RAND",
        ),
    }

    starter_map = {
        0: "SCRATCH_MOVE",
        1: "DISEASE_BITE_MOVE",
        2: "SCREECH_MOVE",
    }
    initial = starter_map.get(starter_move_idx, "RAND")
    ai = MonsterAI(states, initial, rng)
    ai._two_tailed_rat_state = state  # noqa: SLF001
    return creature, ai


# ========================================================================
# ELITE ENCOUNTERS
# ========================================================================

# ---- PhantasmalGardener (HP 28-32 / 29-33 asc) ----

def create_phantasmal_gardener(rng: Rng, slot: str = "first") -> tuple[Creature, MonsterAI]:
    hp = rng.next_int(28, 32)
    creature = Creature(max_hp=hp, monster_id="PHANTASMAL_GARDENER")
    bite_dmg = 5
    lash_dmg = 7
    flail_dmg = 1

    def bite(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, bite_dmg)

    def lash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, lash_dmg)

    def flail(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, flail_dmg, hits=3)

    def enlarge(combat: CombatState) -> None:
        creature.apply_power(PowerId.STRENGTH, 2, applier=creature)

    init = ConditionalBranchState("INIT_MOVE")
    init.add_branch(lambda: slot == "first", "FLAIL_MOVE")
    init.add_branch(lambda: slot == "second", "BITE_MOVE")
    init.add_branch(lambda: slot == "third", "LASH_MOVE")
    init.add_branch(lambda: slot == "fourth", "ENLARGE_MOVE")

    states: dict[str, MonsterState] = {
        "INIT_MOVE": init,
        "BITE_MOVE": MoveState("BITE_MOVE", bite, [attack_intent(bite_dmg)], follow_up_id="LASH_MOVE"),
        "LASH_MOVE": MoveState("LASH_MOVE", lash, [attack_intent(lash_dmg)], follow_up_id="FLAIL_MOVE"),
        "FLAIL_MOVE": MoveState(
            "FLAIL_MOVE",
            flail,
            [multi_attack_intent(flail_dmg, 3)],
            follow_up_id="ENLARGE_MOVE",
        ),
        "ENLARGE_MOVE": MoveState("ENLARGE_MOVE", enlarge, [buff_intent()], follow_up_id="BITE_MOVE"),
    }
    creature.apply_power(PowerId.SKITTISH, 6)
    return creature, MonsterAI(states, "INIT_MOVE", rng)


# ---- SkulkingColony (HP 79 / 84 asc) ----

def create_skulking_colony(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 79
    creature = Creature(max_hp=hp, monster_id="SKULKING_COLONY")
    super_crab_dmg = 6
    zoom_dmg = 16
    smash_dmg = 9
    smash_dazed = 4
    inertia_block = 10

    def inertia(combat: CombatState) -> None:
        _gain_block(creature, inertia_block, combat)
        creature.apply_power(PowerId.STRENGTH, 3, applier=creature)

    def zoom(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, zoom_dmg)

    def super_crab(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, super_crab_dmg, hits=2)

    def smash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, smash_dmg)
        if combat.is_over:
            return
        add_generated_cards_to_living_player_discards(combat, make_dazed, smash_dazed)

    states: dict[str, MonsterState] = {
        "INERTIA_MOVE": MoveState(
            "INERTIA_MOVE",
            inertia,
            [defend_intent(), buff_intent()],
            follow_up_id="SUPER_CRAB_MOVE",
        ),
        "ZOOM_MOVE": MoveState("ZOOM_MOVE", zoom, [attack_intent(zoom_dmg)], follow_up_id="INERTIA_MOVE"),
        "SUPER_CRAB_MOVE": MoveState(
            "SUPER_CRAB_MOVE",
            super_crab,
            [multi_attack_intent(super_crab_dmg, 2)],
            follow_up_id="SMASH_MOVE",
        ),
        "SMASH_MOVE": MoveState(
            "SMASH_MOVE",
            smash,
            [attack_intent(smash_dmg), status_intent()],
            follow_up_id="ZOOM_MOVE",
        ),
    }
    creature.apply_power(PowerId.HARDENED_SHELL, 20)
    return creature, MonsterAI(states, "SMASH_MOVE")


# ---- TerrorEel (HP 140 / 150 asc) ----

def create_terror_eel(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 140
    creature = Creature(max_hp=hp, monster_id="TERROR_EEL")
    crash_dmg = 17
    thrash_dmg = 3
    terror_vulnerable = 99

    def crash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, crash_dmg)

    def thrash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, thrash_dmg, hits=3)
        combat.apply_power_to(creature, PowerId.VIGOR, 7, applier=creature)

    def stun(combat: CombatState) -> None:
        pass

    def terror(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, terror_vulnerable, applier=creature)

    states: dict[str, MonsterState] = {
        "CRASH_MOVE": MoveState("CRASH_MOVE", crash, [attack_intent(crash_dmg)], follow_up_id="ThrashMove"),
        "ThrashMove": MoveState(
            "ThrashMove",
            thrash,
            [multi_attack_intent(thrash_dmg, 3), buff_intent()],
            follow_up_id="CRASH_MOVE",
        ),
        "STUN_MOVE": MoveState("STUN_MOVE", stun, [Intent(IntentType.STUN)], follow_up_id="TERROR_MOVE"),
        "TERROR_MOVE": MoveState("TERROR_MOVE", terror, [debuff_intent()], follow_up_id="CRASH_MOVE"),
    }
    creature.apply_power(PowerId.SHRIEK, 70)
    return creature, MonsterAI(states, "CRASH_MOVE")


# ========================================================================
# BOSS ENCOUNTERS
# ========================================================================

# ---- WaterfallGiant (HP 250 / 260 asc) ----

def create_waterfall_giant(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 250
    creature = Creature(max_hp=hp, monster_id="WATERFALL_GIANT")
    stomp_dmg = 15
    stomp_weak = 1
    ram_dmg = 10
    pressure_up_dmg = 13
    pressurize_amount = 15
    base_pressure_gun_dmg = 20
    pressure_gun_increase = 5
    pressure_buildup = 3
    siphon_heal = 15

    _state = {
        "current_pressure_gun_damage": base_pressure_gun_dmg,
        "steam_eruption_damage": 0,
    }

    def _gain_pressure(combat: CombatState, amount: int) -> None:
        combat.apply_power_to(creature, PowerId.STEAM_ERUPTION, amount, applier=creature)

    def pressurize(combat: CombatState) -> None:
        _gain_pressure(combat, pressurize_amount)

    def stomp(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, stomp_dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, stomp_weak, applier=creature)
        _gain_pressure(combat, pressure_buildup)

    def ram(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, ram_dmg)
        _gain_pressure(combat, pressure_buildup)

    def siphon(combat: CombatState) -> None:
        creature.heal(siphon_heal * len(combat.combat_player_states))
        _gain_pressure(combat, pressure_buildup)

    def pressure_gun(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, _state["current_pressure_gun_damage"])
        _state["current_pressure_gun_damage"] += pressure_gun_increase
        _gain_pressure(combat, pressure_buildup)

    def pressure_up(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, pressure_up_dmg)
        _gain_pressure(combat, pressure_buildup)

    def about_to_blow(combat: CombatState) -> None:
        _state["steam_eruption_damage"] = creature.get_power_amount(PowerId.STEAM_ERUPTION)
        creature.powers.pop(PowerId.STEAM_ERUPTION, None)

    def explode(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, _state["steam_eruption_damage"])
        combat.kill_creature(creature)

    states: dict[str, MonsterState] = {
        "PRESSURIZE_MOVE": MoveState("PRESSURIZE_MOVE", pressurize, [buff_intent()], follow_up_id="STOMP_MOVE"),
        "STOMP_MOVE": MoveState(
            "STOMP_MOVE",
            stomp,
            [attack_intent(stomp_dmg), debuff_intent(), buff_intent()],
            follow_up_id="RAM_MOVE",
        ),
        "RAM_MOVE": MoveState("RAM_MOVE", ram, [attack_intent(ram_dmg), buff_intent()], follow_up_id="SIPHON_MOVE"),
        "SIPHON_MOVE": MoveState(
            "SIPHON_MOVE",
            siphon,
            [Intent(IntentType.HEAL), buff_intent()],
            follow_up_id="PRESSURE_GUN_MOVE",
        ),
        "PRESSURE_GUN_MOVE": MoveState(
            "PRESSURE_GUN_MOVE",
            pressure_gun,
            [attack_intent(base_pressure_gun_dmg), buff_intent()],
            follow_up_id="PRESSURE_UP_MOVE",
        ),
        "PRESSURE_UP_MOVE": MoveState(
            "PRESSURE_UP_MOVE",
            pressure_up,
            [attack_intent(pressure_up_dmg), buff_intent()],
            follow_up_id="STOMP_MOVE",
        ),
        "ABOUT_TO_BLOW_MOVE": MoveState(
            "ABOUT_TO_BLOW_MOVE",
            about_to_blow,
            [Intent(IntentType.STUN)],
            follow_up_id="EXPLODE_MOVE",
            must_perform_once=True,
        ),
        "EXPLODE_MOVE": MoveState("EXPLODE_MOVE", explode, [Intent(IntentType.DEATH_BLOW)], follow_up_id="EXPLODE_MOVE"),
    }
    return creature, MonsterAI(states, "PRESSURIZE_MOVE")


# ---- SoulFysh (HP 211 / 221 asc) ----

def create_soul_fysh(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 211
    creature = Creature(max_hp=hp, monster_id="SOUL_FYSH")
    de_gas_dmg = 16
    scream_dmg = 11
    gaze_dmg = 7

    def beckon(combat: CombatState) -> None:
        from sts2_env.cards.status import make_beckon

        for target in living_player_targets(combat):
            combat.add_generated_card_to_creature_draw_pile(
                target,
                make_beckon(),
                added_by_player=False,
                random_position=True,
            )
            combat.add_generated_card_to_creature_discard(target, make_beckon(), added_by_player=False)

    def de_gas(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, de_gas_dmg)

    def gaze(combat: CombatState) -> None:
        from sts2_env.cards.status import make_beckon

        _deal_damage_to_player(combat, creature, gaze_dmg)
        if combat.is_over:
            return
        for target in living_player_targets(combat):
            combat.add_generated_card_to_creature_discard(target, make_beckon(), added_by_player=False)

    def fade(combat: CombatState) -> None:
        creature.apply_power(PowerId.INTANGIBLE, 2, applier=creature)

    def scream(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, scream_dmg)
        for target in living_player_targets(combat):
            combat.apply_power_to(target, PowerId.VULNERABLE, 3, applier=creature)

    states: dict[str, MonsterState] = {
        "BECKON_MOVE": MoveState("BECKON_MOVE", beckon, [status_intent()], follow_up_id="DE_GAS_MOVE"),
        "DE_GAS_MOVE": MoveState("DE_GAS_MOVE", de_gas, [attack_intent(de_gas_dmg)], follow_up_id="GAZE_MOVE"),
        "GAZE_MOVE": MoveState(
            "GAZE_MOVE",
            gaze,
            [attack_intent(gaze_dmg), status_intent()],
            follow_up_id="FADE_MOVE",
        ),
        "FADE_MOVE": MoveState("FADE_MOVE", fade, [buff_intent()], follow_up_id="SCREAM_MOVE"),
        "SCREAM_MOVE": MoveState(
            "SCREAM_MOVE",
            scream,
            [attack_intent(scream_dmg), debuff_intent()],
            follow_up_id="BECKON_MOVE",
        ),
    }
    return creature, MonsterAI(states, "BECKON_MOVE")


# ---- LagavulinMatriarch (HP 222 / 233 asc) ----
# C# cycle: SLEEP -> (branch: asleep->SLEEP, else->SLASH) ->
#   SLASH(19) -> DISEMBOWEL(9x2) -> SLASH2(12+12blk) -> SOUL_SIPHON(debuff+buff) -> SLASH...

def create_lagavulin_matriarch(rng: Rng) -> tuple[Creature, MonsterAI]:
    hp = 222
    creature = Creature(max_hp=hp, monster_id="LAGAVULIN_MATRIARCH")
    slash_dmg = 19
    disembowel_dmg = 9
    slash2_dmg = 12
    slash2_block = 12
    soul_siphon_debuff = -2
    soul_siphon_strength = 2

    def sleep_move(combat: CombatState) -> None:
        pass

    def slash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, slash_dmg)

    def disembowel(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, disembowel_dmg, hits=2)

    def slash2(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, slash2_dmg)
        _gain_block(creature, slash2_block, combat)

    def soul_siphon(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.STRENGTH, soul_siphon_debuff, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.DEXTERITY, soul_siphon_debuff, applier=creature)
        creature.apply_power(PowerId.STRENGTH, soul_siphon_strength, applier=creature)

    sleep_branch = ConditionalBranchState("SLEEP_BRANCH")
    sleep_branch.add_branch(lambda: creature.has_power(PowerId.ASLEEP), "SLEEP_MOVE")
    sleep_branch.add_branch(lambda: True, "SLASH_MOVE")

    states: dict[str, MonsterState] = {
        "SLEEP_MOVE": MoveState("SLEEP_MOVE", sleep_move, [sleep_intent()], follow_up_id="SLEEP_BRANCH"),
        "SLEEP_BRANCH": sleep_branch,
        "SLASH_MOVE": MoveState("SLASH_MOVE", slash, [attack_intent(slash_dmg)], follow_up_id="DISEMBOWEL_MOVE"),
        "DISEMBOWEL_MOVE": MoveState(
            "DISEMBOWEL_MOVE",
            disembowel,
            [multi_attack_intent(disembowel_dmg, 2)],
            follow_up_id="SLASH2_MOVE",
        ),
        "SLASH2_MOVE": MoveState(
            "SLASH2_MOVE",
            slash2,
            [attack_intent(slash2_dmg), defend_intent()],
            follow_up_id="SOUL_SIPHON_MOVE",
        ),
        "SOUL_SIPHON_MOVE": MoveState(
            "SOUL_SIPHON_MOVE",
            soul_siphon,
            [debuff_intent(), buff_intent()],
            follow_up_id="SLASH_MOVE",
        ),
    }

    creature.apply_power(PowerId.PLATING, 12)
    creature.apply_power(PowerId.ASLEEP, 3)
    return creature, MonsterAI(states, "SLEEP_MOVE")
