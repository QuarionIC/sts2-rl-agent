"""Exordium (Act-1-slot legacy act) monsters -- "Acts from the Past" mod.

Recreates Slay the Spire 1's Act 1 ("Exordium") monster roster as an
alternate for the vanilla Act-1 slot. This module is standalone content:
the run's act-slot-candidate extension point (which act-slot content gets
offered at map generation) had not landed in ``sts2_env`` as of this port,
so these monsters/encounters are NOT wired into the map generator yet --
see ``sts2_env/encounters/exordium.py`` for the encounter pools and the
scope note there.

All HP ranges, damage values, and state machines are ported from the
decompiled "Acts from the Past" mod source
(``decompiled_mods/ActsFromThePast/ActsFromThePast/*.cs``) and cross
checked against the task spec. Ascension convention (matching every other
act in this codebase): HP/toughness scales at Ascension 8, damage/debuff
amounts/status counts scale at Ascension 9.

Uses the existing ``MoveState``/``RandomBranchState``/``ConditionalBranchState``
state-machine framework from ``sts2_env/monsters/state_machine.py`` exactly.
A few monster AIs (JawWorm, AcidSlimeMedium/Large, SpikeSlimeMedium/Large,
SlaverBlue/Red, GremlinNob, Guardian, Hexaghost) use "reroll on repeat"
branching that doesn't fit ``RandomBranchState``'s weight/repeat-limit model
(it renormalizes weights among *all* remaining branches, not a custom
explicit fallback split) or ``ConditionalBranchState``'s condition-only
model (conditions don't receive move history). For those, ``BranchState``
below is a third small ``MonsterState`` implementation -- still just
``get_next_state(state_log, rng)`` like the other two -- that runs an
arbitrary chooser function with direct access to the AI's real move-history
log and the shared RNG, matching the decompiled C# monsters' own
``SelectNextMove(owner, rng, stateMachine)`` callback shape almost exactly.
"""

from __future__ import annotations

import math
from typing import Callable, TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import CardId, CombatSide, MoveRepeatType, PowerId, ValueProp
from sts2_env.core.damage import calculate_damage, apply_damage
from sts2_env.core.rng import INT_MAX, Rng
from sts2_env.cards.status import make_burn, make_dazed, make_slimed
from sts2_env.monsters.intents import (
    Intent, IntentType, attack_intent, multi_attack_intent,
    buff_intent, debuff_intent, status_intent, defend_intent, sleep_intent,
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
from sts2_env.powers.monster import AsleepPower, ThieveryPower

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ========================================================================
# Helpers (mirrors the per-act convention used by act1.py/act2.py/etc.)
# ========================================================================

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


def _deal_damage_to_targets(
    combat: CombatState, creature: Creature, targets: list[Creature], base_dmg: int, hits: int = 1,
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


def _gain_block(creature: Creature, amount: int, combat: CombatState) -> None:
    gain_move_block(creature, amount, combat)


def _other_living_enemies(combat: CombatState, creature: Creature) -> list[Creature]:
    return [e for e in combat.enemies if e.is_alive and e is not creature]


BURN_PLUS_DAMAGE = 4  # Burn+: 4 damage instead of 2


def _force_upgrade_burn(card: "object") -> None:
    """Mark a Burn card upgraded and double its damage.

    This simulator's card factory/``combat.upgrade_card`` treat Burn as
    non-upgradable (its decompiled reference metadata says "Cannot be
    upgraded" -- that's the vanilla-game restriction). The decompiled mod's
    own ``Hexaghost.UpgradeAllBurnsAndAddMore`` deliberately bypasses this
    restriction (``BurnUpgradePatch.AllowBurnUpgrade = true`` around a
    direct ``UpgradeInternal()`` call) specifically for this mechanic, so
    this mirrors that same bypass by mutating the card directly instead of
    going through the normal (restriction-respecting) upgrade path.
    """
    card.upgraded = True
    card.effect_vars = dict(card.effect_vars)
    card.effect_vars["damage"] = BURN_PLUS_DAMAGE


def _upgraded_burn() -> "object":
    card = make_burn()
    _force_upgrade_burn(card)
    return card


class BranchState(MonsterState):
    """A branch state whose next-move logic is a fully custom chooser
    function, for AI patterns that don't fit ``RandomBranchState``'s
    weight/repeat-limit model or ``ConditionalBranchState``'s pure-condition
    model (decompiled STS1/mod monster AIs frequently "roll r; if the
    resulting move is excluded by a repeat rule, roll again among a
    specific alternate split" -- a shape that needs direct move-history
    access, exactly like the C# monsters' own
    ``SelectNextMove(owner, rng, stateMachine)`` methods).

    Still just a ``MonsterState`` processed by the same ``MonsterAI``
    machinery as ``RandomBranchState``/``ConditionalBranchState`` -- not a
    parallel AI system.
    """

    def __init__(self, state_id: str, chooser: Callable[[list[str], Rng], str]):
        super().__init__(state_id)
        self.should_appear_in_logs = False
        self._chooser = chooser

    def get_next_state(self, state_log: list[str], rng: Rng) -> str:
        return self._chooser(state_log, rng)


# ========================================================================
# NORMAL / WEAK MONSTERS
# ========================================================================

# ---- Cultist (HP 48-54 / 50-56 asc) ----
# Fixed opener: INCANTATION (Ritual buff). Then loops DARK_STRIKE forever.

CULTIST_MONSTER_ID = "EXORDIUM_CULTIST"
CULTIST_BASE_MIN_HP = 48
CULTIST_BASE_MAX_HP = 54
CULTIST_TOUGH_MIN_HP = 50
CULTIST_TOUGH_MAX_HP = 56
CULTIST_BASE_RITUAL = 3
CULTIST_DEADLY_RITUAL = 5
CULTIST_DARK_STRIKE_DAMAGE = 6  # flat, not ascension-scaled
CULTIST_INCANTATION_MOVE = "INCANTATION"
CULTIST_DARK_STRIKE_MOVE = "DARK_STRIKE"


def create_cultist(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    min_hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CULTIST_TOUGH_MIN_HP, CULTIST_BASE_MIN_HP)
    max_hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, CULTIST_TOUGH_MAX_HP, CULTIST_BASE_MAX_HP)
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=CULTIST_MONSTER_ID)

    def incantation(combat: CombatState) -> None:
        ritual = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, CULTIST_DEADLY_RITUAL, CULTIST_BASE_RITUAL)
        creature.apply_power(PowerId.RITUAL, ritual, applier=creature)

    def dark_strike(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, CULTIST_DARK_STRIKE_DAMAGE)

    states: dict[str, MonsterState] = {
        CULTIST_INCANTATION_MOVE: MoveState(
            CULTIST_INCANTATION_MOVE, incantation, [buff_intent()], follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
        CULTIST_DARK_STRIKE_MOVE: MoveState(
            CULTIST_DARK_STRIKE_MOVE, dark_strike, [attack_intent(CULTIST_DARK_STRIKE_DAMAGE)],
            follow_up_id=CULTIST_DARK_STRIKE_MOVE,
        ),
    }
    return creature, MonsterAI(states, CULTIST_INCANTATION_MOVE)


# ---- JawWorm (HP 40-44 / 42-46 asc) ----

JAW_WORM_MONSTER_ID = "EXORDIUM_JAW_WORM"
JAW_WORM_BASE_MIN_HP = 40
JAW_WORM_BASE_MAX_HP = 44
JAW_WORM_TOUGH_MIN_HP = 42
JAW_WORM_TOUGH_MAX_HP = 46
JAW_WORM_BASE_CHOMP_DAMAGE = 11
JAW_WORM_DEADLY_CHOMP_DAMAGE = 12
JAW_WORM_BASE_BELLOW_STRENGTH = 3
JAW_WORM_DEADLY_BELLOW_STRENGTH = 5
JAW_WORM_BASE_BELLOW_BLOCK = 6
JAW_WORM_DEADLY_BELLOW_BLOCK = 9
JAW_WORM_THRASH_DAMAGE = 7  # flat
JAW_WORM_THRASH_BLOCK = 5  # flat
JAW_WORM_CHOMP_MOVE = "CHOMP"
JAW_WORM_BELLOW_MOVE = "BELLOW"
JAW_WORM_THRASH_MOVE = "THRASH"
JAW_WORM_BRANCH = "JAW_WORM_BRANCH"


def create_jaw_worm(rng: Rng, ascension_level: int = 0, hard_mode: bool = False) -> tuple[Creature, MonsterAI]:
    """``hard_mode`` is accepted for future reuse (unused by Exordium's own
    JawWormWeak, which always sets it False)."""
    del hard_mode
    min_hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, JAW_WORM_TOUGH_MIN_HP, JAW_WORM_BASE_MIN_HP)
    max_hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, JAW_WORM_TOUGH_MAX_HP, JAW_WORM_BASE_MAX_HP)
    hp = rng.next_int(min_hp, max_hp)
    creature = Creature(max_hp=hp, monster_id=JAW_WORM_MONSTER_ID)

    def chomp(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_CHOMP_DAMAGE, JAW_WORM_BASE_CHOMP_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def bellow(combat: CombatState) -> None:
        asc = _combat_ascension_level(combat)
        strength = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_BELLOW_STRENGTH, JAW_WORM_BASE_BELLOW_STRENGTH)
        block = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_BELLOW_BLOCK, JAW_WORM_BASE_BELLOW_BLOCK)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)
        _gain_block(creature, block, combat)

    def thrash(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, JAW_WORM_THRASH_DAMAGE)
        _gain_block(creature, JAW_WORM_THRASH_BLOCK, combat)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        last = state_log[-1] if state_log else None
        if r < 25:
            if last == JAW_WORM_CHOMP_MOVE:
                return JAW_WORM_BELLOW_MOVE if rng.next_float(100.0) < 56.25 else JAW_WORM_THRASH_MOVE
            return JAW_WORM_CHOMP_MOVE
        if r < 55:
            if len(state_log) >= 2 and state_log[-1] == JAW_WORM_THRASH_MOVE and state_log[-2] == JAW_WORM_THRASH_MOVE:
                return JAW_WORM_CHOMP_MOVE if rng.next_float(100.0) < 35.7 else JAW_WORM_BELLOW_MOVE
            return JAW_WORM_THRASH_MOVE
        if last == JAW_WORM_BELLOW_MOVE:
            return JAW_WORM_CHOMP_MOVE if rng.next_float(100.0) < 41.6 else JAW_WORM_THRASH_MOVE
        return JAW_WORM_BELLOW_MOVE

    chomp_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, JAW_WORM_DEADLY_CHOMP_DAMAGE, JAW_WORM_BASE_CHOMP_DAMAGE)

    states: dict[str, MonsterState] = {
        JAW_WORM_CHOMP_MOVE: MoveState(JAW_WORM_CHOMP_MOVE, chomp, [attack_intent(chomp_intent_damage)], follow_up_id=JAW_WORM_BRANCH),
        JAW_WORM_BELLOW_MOVE: MoveState(JAW_WORM_BELLOW_MOVE, bellow, [buff_intent(), defend_intent()], follow_up_id=JAW_WORM_BRANCH),
        JAW_WORM_THRASH_MOVE: MoveState(JAW_WORM_THRASH_MOVE, thrash, [attack_intent(JAW_WORM_THRASH_DAMAGE), defend_intent()], follow_up_id=JAW_WORM_BRANCH),
        JAW_WORM_BRANCH: BranchState(JAW_WORM_BRANCH, chooser),
    }
    return creature, MonsterAI(states, JAW_WORM_CHOMP_MOVE, rng)


# ---- LouseRed / LouseGreen (HP 10-15/11-16 and 11-17/12-18 asc) ----

LOUSE_RED_MONSTER_ID = "EXORDIUM_LOUSE_RED"
LOUSE_RED_BASE_MIN_HP = 10
LOUSE_RED_BASE_MAX_HP = 15
LOUSE_RED_TOUGH_MIN_HP = 11
LOUSE_RED_TOUGH_MAX_HP = 16
LOUSE_GREEN_MONSTER_ID = "EXORDIUM_LOUSE_GREEN"
LOUSE_GREEN_BASE_MIN_HP = 11
LOUSE_GREEN_BASE_MAX_HP = 17
LOUSE_GREEN_TOUGH_MIN_HP = 12
LOUSE_GREEN_TOUGH_MAX_HP = 18
LOUSE_CURL_UP_BASE_MIN = 3
LOUSE_CURL_UP_BASE_MAX = 8
LOUSE_CURL_UP_TOUGH_MIN = 9
LOUSE_CURL_UP_TOUGH_MAX = 13
LOUSE_BITE_BASE_MIN = 5
LOUSE_BITE_BASE_MAX = 8  # exclusive per spec [5,8)
LOUSE_BITE_DEADLY_MIN = 6
LOUSE_BITE_DEADLY_MAX = 9  # exclusive per spec [6,9)
LOUSE_GROW_BASE_STRENGTH = 3
LOUSE_GROW_DEADLY_STRENGTH = 4
LOUSE_BITE_MOVE = "BITE"
LOUSE_GROW_MOVE = "GROW"
LOUSE_WEB_MOVE = "SPIT_WEB"
LOUSE_RAND = "LOUSE_RAND"
LOUSE_WEAK_WEB = 2


def _create_louse(
    rng: Rng, monster_id: str, min_hp: int, max_hp: int, tough_min_hp: int, tough_max_hp: int,
    ascension_level: int, secondary_move_id: str,
    secondary_effect_factory: Callable[[Creature], Callable[[CombatState], None]],
    secondary_intents: list[Intent],
) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, tough_min_hp, min_hp)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, tough_max_hp, max_hp)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=monster_id)

    curl_lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LOUSE_CURL_UP_TOUGH_MIN, LOUSE_CURL_UP_BASE_MIN)
    curl_hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LOUSE_CURL_UP_TOUGH_MAX, LOUSE_CURL_UP_BASE_MAX)
    creature.apply_power(PowerId.CURL_UP, rng.next_int(curl_lo, curl_hi))

    bite_lo = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LOUSE_BITE_DEADLY_MIN, LOUSE_BITE_BASE_MIN)
    bite_hi_exclusive = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LOUSE_BITE_DEADLY_MAX, LOUSE_BITE_BASE_MAX)
    bite_damage = rng.next_int(bite_lo, bite_hi_exclusive - 1)

    def bite(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, bite_damage)

    def grow(combat: CombatState) -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, LOUSE_GROW_DEADLY_STRENGTH, LOUSE_GROW_BASE_STRENGTH)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)

    grow_max_times = 1 if ascension_level >= DEADLY_ENEMIES_ASCENSION_LEVEL else 2
    rand = RandomBranchState(LOUSE_RAND)
    rand.add_branch(LOUSE_GROW_MOVE, MoveRepeatType.CAN_REPEAT_X_TIMES, max_times=grow_max_times, weight=25.0)
    rand.add_branch(secondary_move_id, MoveRepeatType.CAN_REPEAT_X_TIMES, max_times=2, weight=75.0)

    states: dict[str, MonsterState] = {
        LOUSE_BITE_MOVE: MoveState(LOUSE_BITE_MOVE, bite, [attack_intent(bite_damage)], follow_up_id=LOUSE_RAND),
        LOUSE_GROW_MOVE: MoveState(LOUSE_GROW_MOVE, grow, [buff_intent()], follow_up_id=LOUSE_RAND),
        LOUSE_RAND: rand,
    }
    if secondary_move_id != LOUSE_BITE_MOVE:
        secondary_effect = secondary_effect_factory(creature)
        states[secondary_move_id] = MoveState(secondary_move_id, secondary_effect, secondary_intents, follow_up_id=LOUSE_RAND)

    # Real STS1 lice always open on Bite.
    return creature, MonsterAI(states, LOUSE_BITE_MOVE, rng)


def create_louse_red(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    def _noop_factory(creature: Creature) -> Callable[[CombatState], None]:
        def _noop(combat: CombatState) -> None:  # LouseRed has no secondary move besides Bite/Grow
            pass

        return _noop

    return _create_louse(
        rng, LOUSE_RED_MONSTER_ID, LOUSE_RED_BASE_MIN_HP, LOUSE_RED_BASE_MAX_HP,
        LOUSE_RED_TOUGH_MIN_HP, LOUSE_RED_TOUGH_MAX_HP, ascension_level,
        LOUSE_BITE_MOVE, _noop_factory, [attack_intent(0)],
    )


def create_louse_green(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    def _spit_web_factory(creature: Creature) -> Callable[[CombatState], None]:
        def spit_web(combat: CombatState) -> None:
            apply_power_to_living_player_targets(combat, PowerId.WEAK, LOUSE_WEAK_WEB, applier=creature)

        return spit_web

    return _create_louse(
        rng, LOUSE_GREEN_MONSTER_ID, LOUSE_GREEN_BASE_MIN_HP, LOUSE_GREEN_BASE_MAX_HP,
        LOUSE_GREEN_TOUGH_MIN_HP, LOUSE_GREEN_TOUGH_MAX_HP, ascension_level,
        LOUSE_WEB_MOVE, _spit_web_factory, [debuff_intent()],
    )


# ---- SpikeSlimeMedium (HP 28-32 / 29-34 asc) ----

SPIKE_SLIME_MEDIUM_MONSTER_ID = "EXORDIUM_SPIKE_SLIME_M"
SPIKE_SLIME_MEDIUM_BASE_MIN_HP = 28
SPIKE_SLIME_MEDIUM_BASE_MAX_HP = 32
SPIKE_SLIME_MEDIUM_TOUGH_MIN_HP = 29
SPIKE_SLIME_MEDIUM_TOUGH_MAX_HP = 34
SPIKE_SLIME_BASE_TACKLE_DAMAGE = 8
SPIKE_SLIME_DEADLY_TACKLE_DAMAGE = 10
SPIKE_SLIME_FRAIL_AMOUNT = 1
SPIKE_SLIME_FLAME_TACKLE_MOVE = "FLAME_TACKLE"
SPIKE_SLIME_LICK_MOVE = "LICK"
SPIKE_SLIME_BRANCH = "SPIKE_SLIME_BRANCH"


def create_spike_slime_medium(rng: Rng, ascension_level: int = 0, override_hp: int | None = None) -> tuple[Creature, MonsterAI]:
    if override_hp is not None:
        hp = override_hp
    else:
        lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_MEDIUM_TOUGH_MIN_HP, SPIKE_SLIME_MEDIUM_BASE_MIN_HP)
        hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_MEDIUM_TOUGH_MAX_HP, SPIKE_SLIME_MEDIUM_BASE_MAX_HP)
        hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SPIKE_SLIME_MEDIUM_MONSTER_ID)

    def flame_tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        add_generated_cards_to_living_player_discards(combat, make_slimed, 1)

    def lick(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, SPIKE_SLIME_FRAIL_AMOUNT, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        if r < 30:
            if len(state_log) >= 2 and state_log[-1] == SPIKE_SLIME_FLAME_TACKLE_MOVE and state_log[-2] == SPIKE_SLIME_FLAME_TACKLE_MOVE:
                return SPIKE_SLIME_LICK_MOVE
            return SPIKE_SLIME_FLAME_TACKLE_MOVE
        if state_log and state_log[-1] == SPIKE_SLIME_LICK_MOVE:
            return SPIKE_SLIME_FLAME_TACKLE_MOVE
        return SPIKE_SLIME_LICK_MOVE

    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        SPIKE_SLIME_FLAME_TACKLE_MOVE: MoveState(SPIKE_SLIME_FLAME_TACKLE_MOVE, flame_tackle, [attack_intent(tackle_intent_damage), status_intent()], follow_up_id=SPIKE_SLIME_BRANCH),
        SPIKE_SLIME_LICK_MOVE: MoveState(SPIKE_SLIME_LICK_MOVE, lick, [debuff_intent()], follow_up_id=SPIKE_SLIME_BRANCH),
        SPIKE_SLIME_BRANCH: BranchState(SPIKE_SLIME_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SPIKE_SLIME_FLAME_TACKLE_MOVE, rng)


# ---- AcidSlimeMedium (HP 28-32 / 29-34 asc) ----

ACID_SLIME_MEDIUM_MONSTER_ID = "EXORDIUM_ACID_SLIME_M"
ACID_SLIME_MEDIUM_BASE_MIN_HP = 28
ACID_SLIME_MEDIUM_BASE_MAX_HP = 32
ACID_SLIME_MEDIUM_TOUGH_MIN_HP = 29
ACID_SLIME_MEDIUM_TOUGH_MAX_HP = 34
ACID_SLIME_BASE_SPIT_DAMAGE = 7
ACID_SLIME_DEADLY_SPIT_DAMAGE = 8
ACID_SLIME_MEDIUM_BASE_TACKLE_DAMAGE = 10
ACID_SLIME_MEDIUM_DEADLY_TACKLE_DAMAGE = 12
ACID_SLIME_WEAK_AMOUNT = 1
ACID_SLIME_SPIT_MOVE = "CORROSIVE_SPIT"
ACID_SLIME_TACKLE_MOVE = "TACKLE"
ACID_SLIME_LICK_MOVE = "LICK"
ACID_SLIME_MEDIUM_BRANCH = "ACID_SLIME_MEDIUM_BRANCH"


def create_acid_slime_medium(rng: Rng, ascension_level: int = 0, override_hp: int | None = None) -> tuple[Creature, MonsterAI]:
    if override_hp is not None:
        hp = override_hp
    else:
        lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_MEDIUM_TOUGH_MIN_HP, ACID_SLIME_MEDIUM_BASE_MIN_HP)
        hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_MEDIUM_TOUGH_MAX_HP, ACID_SLIME_MEDIUM_BASE_MAX_HP)
        hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=ACID_SLIME_MEDIUM_MONSTER_ID)

    def spit(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_DEADLY_SPIT_DAMAGE, ACID_SLIME_BASE_SPIT_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        add_generated_cards_to_living_player_discards(combat, make_slimed, 1)

    def tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_MEDIUM_DEADLY_TACKLE_DAMAGE, ACID_SLIME_MEDIUM_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def lick(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, ACID_SLIME_WEAK_AMOUNT, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        last2_spit = len(state_log) >= 2 and state_log[-1] == ACID_SLIME_SPIT_MOVE and state_log[-2] == ACID_SLIME_SPIT_MOVE
        last2_tackle = len(state_log) >= 2 and state_log[-1] == ACID_SLIME_TACKLE_MOVE and state_log[-2] == ACID_SLIME_TACKLE_MOVE
        if r < 40:
            if last2_spit:
                return ACID_SLIME_TACKLE_MOVE if rng.next_float(100.0) < 50 else ACID_SLIME_LICK_MOVE
            return ACID_SLIME_SPIT_MOVE
        if r < 80:
            if last2_tackle:
                return ACID_SLIME_SPIT_MOVE if rng.next_float(100.0) < 50 else ACID_SLIME_LICK_MOVE
            return ACID_SLIME_TACKLE_MOVE
        if state_log and state_log[-1] == ACID_SLIME_LICK_MOVE:
            return ACID_SLIME_SPIT_MOVE if rng.next_float(100.0) < 40 else ACID_SLIME_TACKLE_MOVE
        return ACID_SLIME_LICK_MOVE

    spit_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_DEADLY_SPIT_DAMAGE, ACID_SLIME_BASE_SPIT_DAMAGE)
    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_MEDIUM_DEADLY_TACKLE_DAMAGE, ACID_SLIME_MEDIUM_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        ACID_SLIME_SPIT_MOVE: MoveState(ACID_SLIME_SPIT_MOVE, spit, [attack_intent(spit_intent_damage), status_intent()], follow_up_id=ACID_SLIME_MEDIUM_BRANCH),
        ACID_SLIME_TACKLE_MOVE: MoveState(ACID_SLIME_TACKLE_MOVE, tackle, [attack_intent(tackle_intent_damage)], follow_up_id=ACID_SLIME_MEDIUM_BRANCH),
        ACID_SLIME_LICK_MOVE: MoveState(ACID_SLIME_LICK_MOVE, lick, [debuff_intent()], follow_up_id=ACID_SLIME_MEDIUM_BRANCH),
        ACID_SLIME_MEDIUM_BRANCH: BranchState(ACID_SLIME_MEDIUM_BRANCH, chooser),
    }
    return creature, MonsterAI(states, ACID_SLIME_MEDIUM_BRANCH, rng)


# ---- SlaverBlue (HP 46-50 / 48-52 asc) ----

SLAVER_BLUE_MONSTER_ID = "EXORDIUM_SLAVER_BLUE"
SLAVER_BLUE_BASE_MIN_HP = 46
SLAVER_BLUE_BASE_MAX_HP = 50
SLAVER_BLUE_TOUGH_MIN_HP = 48
SLAVER_BLUE_TOUGH_MAX_HP = 52
SLAVER_BLUE_BASE_STAB_DAMAGE = 12
SLAVER_BLUE_DEADLY_STAB_DAMAGE = 13
SLAVER_BLUE_BASE_RAKE_DAMAGE = 7
SLAVER_BLUE_DEADLY_RAKE_DAMAGE = 8
SLAVER_BLUE_BASE_RAKE_WEAK = 1
SLAVER_BLUE_TOUGH_RAKE_WEAK = 2
SLAVER_BLUE_STAB_MOVE = "STAB"
SLAVER_BLUE_RAKE_MOVE = "RAKE"
SLAVER_BLUE_BRANCH = "SLAVER_BLUE_BRANCH"


def create_slaver_blue(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_TOUGH_MIN_HP, SLAVER_BLUE_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_TOUGH_MAX_HP, SLAVER_BLUE_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SLAVER_BLUE_MONSTER_ID)

    def stab(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_DEADLY_STAB_DAMAGE, SLAVER_BLUE_BASE_STAB_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def rake(combat: CombatState) -> None:
        asc = _combat_ascension_level(combat)
        dmg = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_DEADLY_RAKE_DAMAGE, SLAVER_BLUE_BASE_RAKE_DAMAGE)
        weak = _ascension_value(asc, TOUGH_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_TOUGH_RAKE_WEAK, SLAVER_BLUE_BASE_RAKE_WEAK)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, weak, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        last_two_stab = len(state_log) >= 2 and state_log[-1] == SLAVER_BLUE_STAB_MOVE and state_log[-2] == SLAVER_BLUE_STAB_MOVE
        if r >= 40 and not last_two_stab:
            return SLAVER_BLUE_STAB_MOVE
        if not state_log or state_log[-1] != SLAVER_BLUE_RAKE_MOVE:
            return SLAVER_BLUE_RAKE_MOVE
        return SLAVER_BLUE_STAB_MOVE

    stab_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_DEADLY_STAB_DAMAGE, SLAVER_BLUE_BASE_STAB_DAMAGE)
    rake_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_BLUE_DEADLY_RAKE_DAMAGE, SLAVER_BLUE_BASE_RAKE_DAMAGE)
    states: dict[str, MonsterState] = {
        SLAVER_BLUE_STAB_MOVE: MoveState(SLAVER_BLUE_STAB_MOVE, stab, [attack_intent(stab_intent_damage)], follow_up_id=SLAVER_BLUE_BRANCH),
        SLAVER_BLUE_RAKE_MOVE: MoveState(SLAVER_BLUE_RAKE_MOVE, rake, [attack_intent(rake_intent_damage), debuff_intent()], follow_up_id=SLAVER_BLUE_BRANCH),
        SLAVER_BLUE_BRANCH: BranchState(SLAVER_BLUE_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SLAVER_BLUE_BRANCH, rng)


# ---- SlaverRed (HP 46-50 / 48-52 asc) ----

SLAVER_RED_MONSTER_ID = "EXORDIUM_SLAVER_RED"
SLAVER_RED_BASE_MIN_HP = 46
SLAVER_RED_BASE_MAX_HP = 50
SLAVER_RED_TOUGH_MIN_HP = 48
SLAVER_RED_TOUGH_MAX_HP = 52
SLAVER_RED_BASE_STAB_DAMAGE = 13
SLAVER_RED_DEADLY_STAB_DAMAGE = 14
SLAVER_RED_BASE_SCRAPE_DAMAGE = 8
SLAVER_RED_DEADLY_SCRAPE_DAMAGE = 9
SLAVER_RED_BASE_SCRAPE_VULN = 1
SLAVER_RED_DEADLY_SCRAPE_VULN = 2
SLAVER_RED_STAB_MOVE = "STAB"
SLAVER_RED_SCRAPE_MOVE = "SCRAPE"
SLAVER_RED_ENTANGLE_MOVE = "ENTANGLE"
SLAVER_RED_BRANCH = "SLAVER_RED_BRANCH"


def create_slaver_red(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_TOUGH_MIN_HP, SLAVER_RED_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_TOUGH_MAX_HP, SLAVER_RED_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SLAVER_RED_MONSTER_ID)
    entangle_used = {"done": False}

    def stab(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_DEADLY_STAB_DAMAGE, SLAVER_RED_BASE_STAB_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def scrape(combat: CombatState) -> None:
        asc = _combat_ascension_level(combat)
        dmg = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_DEADLY_SCRAPE_DAMAGE, SLAVER_RED_BASE_SCRAPE_DAMAGE)
        vuln = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_DEADLY_SCRAPE_VULN, SLAVER_RED_BASE_SCRAPE_VULN)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, vuln, applier=creature)

    def entangle(combat: CombatState) -> None:
        # Reuses the simulator's existing ENTANGLED power (blocks Attack
        # cards for the rest of the turn) as the "Acts from the Past"
        # EntangledOriginal affliction equivalent -- see report for the
        # "N random cards" vs "all Attack cards this turn" simplification.
        apply_power_to_living_player_targets(combat, PowerId.ENTANGLED, 1, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        if r >= 75 and not entangle_used["done"]:
            entangle_used["done"] = True
            return SLAVER_RED_ENTANGLE_MOVE
        last_two_stab = len(state_log) >= 2 and state_log[-1] == SLAVER_RED_STAB_MOVE and state_log[-2] == SLAVER_RED_STAB_MOVE
        if r >= 55 and entangle_used["done"] and not last_two_stab:
            return SLAVER_RED_STAB_MOVE
        if not state_log or state_log[-1] != SLAVER_RED_SCRAPE_MOVE:
            return SLAVER_RED_SCRAPE_MOVE
        return SLAVER_RED_STAB_MOVE

    stab_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_DEADLY_STAB_DAMAGE, SLAVER_RED_BASE_STAB_DAMAGE)
    scrape_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SLAVER_RED_DEADLY_SCRAPE_DAMAGE, SLAVER_RED_BASE_SCRAPE_DAMAGE)
    states: dict[str, MonsterState] = {
        SLAVER_RED_STAB_MOVE: MoveState(SLAVER_RED_STAB_MOVE, stab, [attack_intent(stab_intent_damage)], follow_up_id=SLAVER_RED_BRANCH),
        SLAVER_RED_SCRAPE_MOVE: MoveState(SLAVER_RED_SCRAPE_MOVE, scrape, [attack_intent(scrape_intent_damage), debuff_intent()], follow_up_id=SLAVER_RED_BRANCH),
        SLAVER_RED_ENTANGLE_MOVE: MoveState(SLAVER_RED_ENTANGLE_MOVE, entangle, [Intent(IntentType.CARD_DEBUFF)], follow_up_id=SLAVER_RED_BRANCH),
        SLAVER_RED_BRANCH: BranchState(SLAVER_RED_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SLAVER_RED_BRANCH, rng)


# ---- Looter (HP 44-48 / 46-50 asc) ----

class _LooterThieveryPower(ThieveryPower):
    """Looter-specific ThieveryPower: if Looter dies before it escapes, any
    gold it already stole is refunded to the players it stole from (the
    classic STS1 "kill it before it flees" Looter bonus).

    Not from vanilla STS2 decompiled source (this simulator's shared
    ThieveryPower doesn't already grant a reward on owner death, unlike
    HeistPower which is what Looter's mod cousin, GremlinMerc's Surprise
    mechanic, transfers stolen gold into). Implemented as a Looter-local
    subclass rather than extending the shared ThieveryPower so
    GremlinMerc's existing Surprise/Heist transfer behavior is untouched.
    """

    def before_death(self, owner: Creature, creature: Creature, combat: "CombatState") -> None:
        if creature is not owner or not hasattr(combat.room, "add_extra_reward"):
            return
        from sts2_env.run.reward_objects import GoldReward

        for player, amount in self.gold_stolen_by_player.items():
            if amount <= 0:
                continue
            state = combat.combat_player_state_for(player)
            player_id = state.player_state.player_id if state is not None else combat.player_id
            combat.room.add_extra_reward(player_id, GoldReward(player_id, amount, amount))


LOOTER_MONSTER_ID = "EXORDIUM_LOOTER"
LOOTER_BASE_MIN_HP = 44
LOOTER_BASE_MAX_HP = 48
LOOTER_TOUGH_MIN_HP = 46
LOOTER_TOUGH_MAX_HP = 50
LOOTER_BASE_MUG_DAMAGE = 10
LOOTER_DEADLY_MUG_DAMAGE = 11
LOOTER_SMOKE_BOMB_BLOCK = 6
LOOTER_BASE_LUNGE_DAMAGE = 12
LOOTER_DEADLY_LUNGE_DAMAGE = 14
LOOTER_BASE_THIEVERY_GOLD = 15
LOOTER_DEADLY_THIEVERY_GOLD = 20
LOOTER_MUG_1_MOVE = "MUG_1"
LOOTER_MUG_2_MOVE = "MUG_2"
LOOTER_PATH_BRANCH = "LOOTER_PATH_BRANCH"
LOOTER_LUNGE_MOVE = "LUNGE"
LOOTER_SMOKE_BOMB_MOVE = "SMOKE_BOMB"
LOOTER_ESCAPE_MOVE = "ESCAPE"


def create_looter(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LOOTER_TOUGH_MIN_HP, LOOTER_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LOOTER_TOUGH_MAX_HP, LOOTER_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=LOOTER_MONSTER_ID)

    def mug(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, LOOTER_DEADLY_MUG_DAMAGE, LOOTER_BASE_MUG_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def smoke_bomb(combat: CombatState) -> None:
        _gain_block(creature, LOOTER_SMOKE_BOMB_BLOCK, combat)

    def escape(combat: CombatState) -> None:
        combat.escape_creature(creature)

    def lunge(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, LOOTER_DEADLY_LUNGE_DAMAGE, LOOTER_BASE_LUNGE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    mug_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LOOTER_DEADLY_MUG_DAMAGE, LOOTER_BASE_MUG_DAMAGE)
    lunge_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LOOTER_DEADLY_LUNGE_DAMAGE, LOOTER_BASE_LUNGE_DAMAGE)

    path_branch = RandomBranchState(LOOTER_PATH_BRANCH)
    path_branch.add_branch(LOOTER_SMOKE_BOMB_MOVE, weight=1.0)
    path_branch.add_branch(LOOTER_LUNGE_MOVE, weight=1.0)

    states: dict[str, MonsterState] = {
        LOOTER_MUG_1_MOVE: MoveState(LOOTER_MUG_1_MOVE, mug, [attack_intent(mug_intent_damage)], follow_up_id=LOOTER_MUG_2_MOVE),
        LOOTER_MUG_2_MOVE: MoveState(LOOTER_MUG_2_MOVE, mug, [attack_intent(mug_intent_damage)], follow_up_id=LOOTER_PATH_BRANCH),
        LOOTER_PATH_BRANCH: path_branch,
        LOOTER_LUNGE_MOVE: MoveState(LOOTER_LUNGE_MOVE, lunge, [attack_intent(lunge_intent_damage)], follow_up_id=LOOTER_SMOKE_BOMB_MOVE),
        LOOTER_SMOKE_BOMB_MOVE: MoveState(LOOTER_SMOKE_BOMB_MOVE, smoke_bomb, [defend_intent()], follow_up_id=LOOTER_ESCAPE_MOVE),
        LOOTER_ESCAPE_MOVE: MoveState(LOOTER_ESCAPE_MOVE, escape, [Intent(IntentType.ESCAPE)], follow_up_id=LOOTER_ESCAPE_MOVE),
    }

    gold_amount = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LOOTER_DEADLY_THIEVERY_GOLD, LOOTER_BASE_THIEVERY_GOLD)
    creature.powers[PowerId.THIEVERY] = _LooterThieveryPower(gold_amount)
    return creature, MonsterAI(states, LOOTER_MUG_1_MOVE, rng)


# ---- GremlinMad (HP 20-24 / 21-25 asc) ----

GREMLIN_MAD_MONSTER_ID = "EXORDIUM_GREMLIN_MAD"
GREMLIN_MAD_BASE_MIN_HP = 20
GREMLIN_MAD_BASE_MAX_HP = 24
GREMLIN_MAD_TOUGH_MIN_HP = 21
GREMLIN_MAD_TOUGH_MAX_HP = 25
GREMLIN_MAD_BASE_ANGRY = 1
GREMLIN_MAD_DEADLY_ANGRY = 2
GREMLIN_MAD_BASE_SCRATCH_DAMAGE = 4
GREMLIN_MAD_DEADLY_SCRATCH_DAMAGE = 5
GREMLIN_MAD_SCRATCH_MOVE = "SCRATCH"


def create_gremlin_mad(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_MAD_TOUGH_MIN_HP, GREMLIN_MAD_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_MAD_TOUGH_MAX_HP, GREMLIN_MAD_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_MAD_MONSTER_ID)

    def scratch(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_MAD_DEADLY_SCRATCH_DAMAGE, GREMLIN_MAD_BASE_SCRATCH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    angry = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_MAD_DEADLY_ANGRY, GREMLIN_MAD_BASE_ANGRY)
    creature.apply_power(PowerId.ANGRY, angry, applier=creature)

    scratch_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_MAD_DEADLY_SCRATCH_DAMAGE, GREMLIN_MAD_BASE_SCRATCH_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_MAD_SCRATCH_MOVE: MoveState(GREMLIN_MAD_SCRATCH_MOVE, scratch, [attack_intent(scratch_intent_damage)], follow_up_id=GREMLIN_MAD_SCRATCH_MOVE),
    }
    return creature, MonsterAI(states, GREMLIN_MAD_SCRATCH_MOVE)


# ---- GremlinSneaky (HP 10-14 / 11-15 asc) ----

GREMLIN_SNEAKY_MONSTER_ID = "EXORDIUM_GREMLIN_SNEAKY"
GREMLIN_SNEAKY_BASE_MIN_HP = 10
GREMLIN_SNEAKY_BASE_MAX_HP = 14
GREMLIN_SNEAKY_TOUGH_MIN_HP = 11
GREMLIN_SNEAKY_TOUGH_MAX_HP = 15
GREMLIN_SNEAKY_BASE_PUNCTURE_DAMAGE = 9
GREMLIN_SNEAKY_DEADLY_PUNCTURE_DAMAGE = 10
GREMLIN_SNEAKY_PUNCTURE_MOVE = "PUNCTURE"


def create_gremlin_sneaky(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_SNEAKY_TOUGH_MIN_HP, GREMLIN_SNEAKY_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_SNEAKY_TOUGH_MAX_HP, GREMLIN_SNEAKY_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_SNEAKY_MONSTER_ID)

    def puncture(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_SNEAKY_DEADLY_PUNCTURE_DAMAGE, GREMLIN_SNEAKY_BASE_PUNCTURE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    puncture_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_SNEAKY_DEADLY_PUNCTURE_DAMAGE, GREMLIN_SNEAKY_BASE_PUNCTURE_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_SNEAKY_PUNCTURE_MOVE: MoveState(GREMLIN_SNEAKY_PUNCTURE_MOVE, puncture, [attack_intent(puncture_intent_damage)], follow_up_id=GREMLIN_SNEAKY_PUNCTURE_MOVE),
    }
    return creature, MonsterAI(states, GREMLIN_SNEAKY_PUNCTURE_MOVE)


# ---- GremlinFat (HP 13-17 / 14-18 asc) ----

GREMLIN_FAT_MONSTER_ID = "EXORDIUM_GREMLIN_FAT"
GREMLIN_FAT_BASE_MIN_HP = 13
GREMLIN_FAT_BASE_MAX_HP = 17
GREMLIN_FAT_TOUGH_MIN_HP = 14
GREMLIN_FAT_TOUGH_MAX_HP = 18
GREMLIN_FAT_BASE_SMASH_DAMAGE = 4
GREMLIN_FAT_DEADLY_SMASH_DAMAGE = 5
GREMLIN_FAT_WEAK_AMOUNT = 1
GREMLIN_FAT_FRAIL_AMOUNT = 1
GREMLIN_FAT_SMASH_MOVE = "SMASH"


def create_gremlin_fat(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_FAT_TOUGH_MIN_HP, GREMLIN_FAT_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_FAT_TOUGH_MAX_HP, GREMLIN_FAT_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_FAT_MONSTER_ID)

    def smash(combat: CombatState) -> None:
        asc = _combat_ascension_level(combat)
        dmg = _ascension_value(asc, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_FAT_DEADLY_SMASH_DAMAGE, GREMLIN_FAT_BASE_SMASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.WEAK, GREMLIN_FAT_WEAK_AMOUNT, applier=creature)
        if asc >= DEADLY_ENEMIES_ASCENSION_LEVEL:
            apply_power_to_living_player_targets(combat, PowerId.FRAIL, GREMLIN_FAT_FRAIL_AMOUNT, applier=creature)

    smash_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_FAT_DEADLY_SMASH_DAMAGE, GREMLIN_FAT_BASE_SMASH_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_FAT_SMASH_MOVE: MoveState(GREMLIN_FAT_SMASH_MOVE, smash, [attack_intent(smash_intent_damage), debuff_intent()], follow_up_id=GREMLIN_FAT_SMASH_MOVE),
    }
    return creature, MonsterAI(states, GREMLIN_FAT_SMASH_MOVE)


# ---- GremlinShield (HP 12-15 / 13-17 asc) ----

GREMLIN_SHIELD_MONSTER_ID = "EXORDIUM_GREMLIN_SHIELD"
GREMLIN_SHIELD_BASE_MIN_HP = 12
GREMLIN_SHIELD_BASE_MAX_HP = 15
GREMLIN_SHIELD_TOUGH_MIN_HP = 13
GREMLIN_SHIELD_TOUGH_MAX_HP = 17
GREMLIN_SHIELD_BASE_PROTECT_BLOCK = 7
GREMLIN_SHIELD_TOUGH_PROTECT_BLOCK = 11
GREMLIN_SHIELD_BASE_BASH_DAMAGE = 6
GREMLIN_SHIELD_DEADLY_BASH_DAMAGE = 8
GREMLIN_SHIELD_PROTECT_MOVE = "PROTECT"
GREMLIN_SHIELD_BASH_MOVE = "SHIELD_BASH"
GREMLIN_SHIELD_CHECK = "GREMLIN_SHIELD_CHECK"


def create_gremlin_shield(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_SHIELD_TOUGH_MIN_HP, GREMLIN_SHIELD_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_SHIELD_TOUGH_MAX_HP, GREMLIN_SHIELD_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_SHIELD_MONSTER_ID)

    def protect(combat: CombatState) -> None:
        block = _ascension_value(_combat_ascension_level(combat), TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_SHIELD_TOUGH_PROTECT_BLOCK, GREMLIN_SHIELD_BASE_PROTECT_BLOCK)
        others = _other_living_enemies(combat, creature)
        target = combat.rng.choice(others) if others else creature
        _gain_block(target, block, combat)

    def shield_bash(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_SHIELD_DEADLY_BASH_DAMAGE, GREMLIN_SHIELD_BASE_BASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def _has_other_living_allies() -> bool:
        combat = creature.combat_state
        if combat is None:
            return False
        return len(_other_living_enemies(combat, creature)) > 0

    check = ConditionalBranchState(GREMLIN_SHIELD_CHECK)
    check.add_branch(_has_other_living_allies, GREMLIN_SHIELD_PROTECT_MOVE)
    check.add_branch(lambda: True, GREMLIN_SHIELD_BASH_MOVE)

    bash_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_SHIELD_DEADLY_BASH_DAMAGE, GREMLIN_SHIELD_BASE_BASH_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_SHIELD_PROTECT_MOVE: MoveState(GREMLIN_SHIELD_PROTECT_MOVE, protect, [buff_intent()], follow_up_id=GREMLIN_SHIELD_CHECK),
        GREMLIN_SHIELD_BASH_MOVE: MoveState(GREMLIN_SHIELD_BASH_MOVE, shield_bash, [attack_intent(bash_intent_damage)], follow_up_id=GREMLIN_SHIELD_BASH_MOVE),
        GREMLIN_SHIELD_CHECK: check,
    }
    return creature, MonsterAI(states, GREMLIN_SHIELD_PROTECT_MOVE)


# ---- GremlinWizard (HP 21-25 / 22-26 asc) ----

GREMLIN_WIZARD_MONSTER_ID = "EXORDIUM_GREMLIN_WIZARD"
GREMLIN_WIZARD_BASE_MIN_HP = 21
GREMLIN_WIZARD_BASE_MAX_HP = 25
GREMLIN_WIZARD_TOUGH_MIN_HP = 22
GREMLIN_WIZARD_TOUGH_MAX_HP = 26
GREMLIN_WIZARD_BASE_BLAST_DAMAGE = 25
GREMLIN_WIZARD_DEADLY_BLAST_DAMAGE = 30
GREMLIN_WIZARD_CHARGING_MOVE = "CHARGING"
GREMLIN_WIZARD_ULTIMATE_MOVE = "ULTIMATE_BLAST"
GREMLIN_WIZARD_BRANCH = "GREMLIN_WIZARD_BRANCH"


def create_gremlin_wizard(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_WIZARD_TOUGH_MIN_HP, GREMLIN_WIZARD_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_WIZARD_TOUGH_MAX_HP, GREMLIN_WIZARD_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_WIZARD_MONSTER_ID)
    charge = {"n": 1}

    def charging(combat: CombatState) -> None:
        charge["n"] += 1

    def ultimate_blast(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_WIZARD_DEADLY_BLAST_DAMAGE, GREMLIN_WIZARD_BASE_BLAST_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        charge["n"] = 0

    branch = ConditionalBranchState(GREMLIN_WIZARD_BRANCH)
    branch.add_branch(lambda: charge["n"] >= 3, GREMLIN_WIZARD_ULTIMATE_MOVE)
    branch.add_branch(lambda: True, GREMLIN_WIZARD_CHARGING_MOVE)

    blast_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_WIZARD_DEADLY_BLAST_DAMAGE, GREMLIN_WIZARD_BASE_BLAST_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_WIZARD_CHARGING_MOVE: MoveState(GREMLIN_WIZARD_CHARGING_MOVE, charging, [buff_intent()], follow_up_id=GREMLIN_WIZARD_BRANCH),
        GREMLIN_WIZARD_ULTIMATE_MOVE: MoveState(GREMLIN_WIZARD_ULTIMATE_MOVE, ultimate_blast, [attack_intent(blast_intent_damage)], follow_up_id=GREMLIN_WIZARD_ULTIMATE_MOVE),
        GREMLIN_WIZARD_BRANCH: branch,
    }
    return creature, MonsterAI(states, GREMLIN_WIZARD_CHARGING_MOVE)


# ---- GremlinNob (Elite, HP 82-86 / 85-90 asc) ----

GREMLIN_NOB_MONSTER_ID = "EXORDIUM_GREMLIN_NOB"
GREMLIN_NOB_BASE_MIN_HP = 82
GREMLIN_NOB_BASE_MAX_HP = 86
GREMLIN_NOB_TOUGH_MIN_HP = 85
GREMLIN_NOB_TOUGH_MAX_HP = 90
GREMLIN_NOB_BASE_ENRAGE = 2
GREMLIN_NOB_DEADLY_ENRAGE = 3
GREMLIN_NOB_BASE_RUSH_DAMAGE = 14
GREMLIN_NOB_DEADLY_RUSH_DAMAGE = 16
GREMLIN_NOB_BASE_SKULL_BASH_DAMAGE = 6
GREMLIN_NOB_DEADLY_SKULL_BASH_DAMAGE = 8
GREMLIN_NOB_SKULL_BASH_VULN = 2  # flat, not ascension-scaled
GREMLIN_NOB_BELLOW_MOVE = "BELLOW"
GREMLIN_NOB_RUSH_MOVE = "RUSH"
GREMLIN_NOB_SKULL_BASH_MOVE = "SKULL_BASH"
GREMLIN_NOB_BRANCH = "GREMLIN_NOB_BRANCH"


def create_gremlin_nob(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_TOUGH_MIN_HP, GREMLIN_NOB_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_TOUGH_MAX_HP, GREMLIN_NOB_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=GREMLIN_NOB_MONSTER_ID)

    def bellow(combat: CombatState) -> None:
        # Real STS1/mod GremlinNob Enrage triggers on Skill cards played
        # (see EnragePower docstring/report note), not literally "any card".
        amount = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_DEADLY_ENRAGE, GREMLIN_NOB_BASE_ENRAGE)
        creature.apply_power(PowerId.ENRAGE, amount, applier=creature)

    def rush(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_DEADLY_RUSH_DAMAGE, GREMLIN_NOB_BASE_RUSH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def skull_bash(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_DEADLY_SKULL_BASH_DAMAGE, GREMLIN_NOB_BASE_SKULL_BASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, GREMLIN_NOB_SKULL_BASH_VULN, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        if len(state_log) >= 2 and state_log[-1] != GREMLIN_NOB_SKULL_BASH_MOVE and state_log[-2] != GREMLIN_NOB_SKULL_BASH_MOVE:
            return GREMLIN_NOB_SKULL_BASH_MOVE
        if len(state_log) >= 2 and state_log[-1] == GREMLIN_NOB_RUSH_MOVE and state_log[-2] == GREMLIN_NOB_RUSH_MOVE:
            return GREMLIN_NOB_SKULL_BASH_MOVE
        return GREMLIN_NOB_RUSH_MOVE

    rush_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_DEADLY_RUSH_DAMAGE, GREMLIN_NOB_BASE_RUSH_DAMAGE)
    skull_bash_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GREMLIN_NOB_DEADLY_SKULL_BASH_DAMAGE, GREMLIN_NOB_BASE_SKULL_BASH_DAMAGE)
    states: dict[str, MonsterState] = {
        GREMLIN_NOB_BELLOW_MOVE: MoveState(GREMLIN_NOB_BELLOW_MOVE, bellow, [buff_intent()], follow_up_id=GREMLIN_NOB_BRANCH),
        GREMLIN_NOB_RUSH_MOVE: MoveState(GREMLIN_NOB_RUSH_MOVE, rush, [attack_intent(rush_intent_damage)], follow_up_id=GREMLIN_NOB_BRANCH),
        GREMLIN_NOB_SKULL_BASH_MOVE: MoveState(GREMLIN_NOB_SKULL_BASH_MOVE, skull_bash, [attack_intent(skull_bash_intent_damage), debuff_intent()], follow_up_id=GREMLIN_NOB_BRANCH),
        GREMLIN_NOB_BRANCH: BranchState(GREMLIN_NOB_BRANCH, chooser),
    }
    return creature, MonsterAI(states, GREMLIN_NOB_BELLOW_MOVE, rng)


# ---- AcidSlimeLarge (HP 65-69 / 68-72 asc) ----

ACID_SLIME_LARGE_MONSTER_ID = "EXORDIUM_ACID_SLIME_L"
ACID_SLIME_LARGE_BASE_MIN_HP = 65
ACID_SLIME_LARGE_BASE_MAX_HP = 69
ACID_SLIME_LARGE_TOUGH_MIN_HP = 68
ACID_SLIME_LARGE_TOUGH_MAX_HP = 72
ACID_SLIME_LARGE_BASE_SPIT_DAMAGE = 11
ACID_SLIME_LARGE_DEADLY_SPIT_DAMAGE = 12
ACID_SLIME_LARGE_SPIT_SLIMED_COUNT = 2
ACID_SLIME_LARGE_BASE_TACKLE_DAMAGE = 16
ACID_SLIME_LARGE_DEADLY_TACKLE_DAMAGE = 18
ACID_SLIME_LARGE_WEAK_AMOUNT = 2
ACID_SLIME_LARGE_SPIT_MOVE = "CORROSIVE_SPIT"
ACID_SLIME_LARGE_TACKLE_MOVE = "TACKLE"
ACID_SLIME_LARGE_LICK_MOVE = "LICK"
ACID_SLIME_LARGE_SPLIT_MOVE = "SPLIT"
ACID_SLIME_LARGE_BRANCH = "ACID_SLIME_LARGE_BRANCH"


def create_acid_slime_large(rng: Rng, ascension_level: int = 0, override_hp: int | None = None) -> tuple[Creature, MonsterAI]:
    if override_hp is not None:
        hp = override_hp
    else:
        lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_TOUGH_MIN_HP, ACID_SLIME_LARGE_BASE_MIN_HP)
        hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_TOUGH_MAX_HP, ACID_SLIME_LARGE_BASE_MAX_HP)
        hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=ACID_SLIME_LARGE_MONSTER_ID)
    creature.apply_power(PowerId.SPLIT, 1)

    def spit(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_DEADLY_SPIT_DAMAGE, ACID_SLIME_LARGE_BASE_SPIT_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        add_generated_cards_to_living_player_discards(combat, make_slimed, ACID_SLIME_LARGE_SPIT_SLIMED_COUNT)

    def tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_DEADLY_TACKLE_DAMAGE, ACID_SLIME_LARGE_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def lick(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, ACID_SLIME_LARGE_WEAK_AMOUNT, applier=creature)

    def split(combat: CombatState) -> None:
        hp_at_split = max(1, creature.current_hp)
        combat.kill_creature(creature)
        for _ in range(2):
            med, med_ai = create_acid_slime_medium(
                Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level, override_hp=hp_at_split,
            )
            combat.add_enemy(med, med_ai)

    def chooser(state_log: list[str], rng: Rng) -> str:
        split_power = creature.powers.get(PowerId.SPLIT)
        if split_power is not None and getattr(split_power, "triggered", False):
            return ACID_SLIME_LARGE_SPLIT_MOVE
        r = rng.next_float(100.0)
        last2_spit = len(state_log) >= 2 and state_log[-1] == ACID_SLIME_LARGE_SPIT_MOVE and state_log[-2] == ACID_SLIME_LARGE_SPIT_MOVE
        last2_tackle = len(state_log) >= 2 and state_log[-1] == ACID_SLIME_LARGE_TACKLE_MOVE and state_log[-2] == ACID_SLIME_LARGE_TACKLE_MOVE
        if r < 40:
            if last2_spit:
                return ACID_SLIME_LARGE_TACKLE_MOVE if rng.next_float(100.0) < 50 else ACID_SLIME_LARGE_LICK_MOVE
            return ACID_SLIME_LARGE_SPIT_MOVE
        if r < 70:
            if last2_tackle:
                return ACID_SLIME_LARGE_SPIT_MOVE if rng.next_float(100.0) < 50 else ACID_SLIME_LARGE_LICK_MOVE
            return ACID_SLIME_LARGE_TACKLE_MOVE
        if state_log and state_log[-1] == ACID_SLIME_LARGE_LICK_MOVE:
            return ACID_SLIME_LARGE_SPIT_MOVE if rng.next_float(100.0) < 40 else ACID_SLIME_LARGE_TACKLE_MOVE
        return ACID_SLIME_LARGE_LICK_MOVE

    spit_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_DEADLY_SPIT_DAMAGE, ACID_SLIME_LARGE_BASE_SPIT_DAMAGE)
    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_LARGE_DEADLY_TACKLE_DAMAGE, ACID_SLIME_LARGE_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        ACID_SLIME_LARGE_SPIT_MOVE: MoveState(ACID_SLIME_LARGE_SPIT_MOVE, spit, [attack_intent(spit_intent_damage), status_intent()], follow_up_id=ACID_SLIME_LARGE_BRANCH),
        ACID_SLIME_LARGE_TACKLE_MOVE: MoveState(ACID_SLIME_LARGE_TACKLE_MOVE, tackle, [attack_intent(tackle_intent_damage)], follow_up_id=ACID_SLIME_LARGE_BRANCH),
        ACID_SLIME_LARGE_LICK_MOVE: MoveState(ACID_SLIME_LARGE_LICK_MOVE, lick, [debuff_intent()], follow_up_id=ACID_SLIME_LARGE_BRANCH),
        ACID_SLIME_LARGE_SPLIT_MOVE: MoveState(ACID_SLIME_LARGE_SPLIT_MOVE, split, [Intent(IntentType.UNKNOWN)], follow_up_id=ACID_SLIME_LARGE_SPLIT_MOVE),
        ACID_SLIME_LARGE_BRANCH: BranchState(ACID_SLIME_LARGE_BRANCH, chooser),
    }
    return creature, MonsterAI(states, ACID_SLIME_LARGE_BRANCH, rng)


# ---- SpikeSlimeLarge (HP 64-70 / 67-73 asc) ----

SPIKE_SLIME_LARGE_MONSTER_ID = "EXORDIUM_SPIKE_SLIME_L"
SPIKE_SLIME_LARGE_BASE_MIN_HP = 64
SPIKE_SLIME_LARGE_BASE_MAX_HP = 70
SPIKE_SLIME_LARGE_TOUGH_MIN_HP = 67
SPIKE_SLIME_LARGE_TOUGH_MAX_HP = 73
SPIKE_SLIME_LARGE_BASE_TACKLE_DAMAGE = 16
SPIKE_SLIME_LARGE_DEADLY_TACKLE_DAMAGE = 18
SPIKE_SLIME_LARGE_TACKLE_SLIMED_COUNT = 2
SPIKE_SLIME_LARGE_BASE_FRAIL_TURNS = 2
SPIKE_SLIME_LARGE_DEADLY_FRAIL_TURNS = 3
SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE = "FLAME_TACKLE"
SPIKE_SLIME_LARGE_LICK_MOVE = "LICK"
SPIKE_SLIME_LARGE_SPLIT_MOVE = "SPLIT"
SPIKE_SLIME_LARGE_BRANCH = "SPIKE_SLIME_LARGE_BRANCH"


def create_spike_slime_large(rng: Rng, ascension_level: int = 0, override_hp: int | None = None) -> tuple[Creature, MonsterAI]:
    if override_hp is not None:
        hp = override_hp
    else:
        lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_LARGE_TOUGH_MIN_HP, SPIKE_SLIME_LARGE_BASE_MIN_HP)
        hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_LARGE_TOUGH_MAX_HP, SPIKE_SLIME_LARGE_BASE_MAX_HP)
        hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SPIKE_SLIME_LARGE_MONSTER_ID)
    creature.apply_power(PowerId.SPLIT, 1)

    def flame_tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_LARGE_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_LARGE_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        add_generated_cards_to_living_player_discards(combat, make_slimed, SPIKE_SLIME_LARGE_TACKLE_SLIMED_COUNT)

    def lick(combat: CombatState) -> None:
        turns = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_LARGE_DEADLY_FRAIL_TURNS, SPIKE_SLIME_LARGE_BASE_FRAIL_TURNS)
        apply_power_to_living_player_targets(combat, PowerId.FRAIL, turns, applier=creature)

    def split(combat: CombatState) -> None:
        hp_at_split = max(1, creature.current_hp)
        combat.kill_creature(creature)
        for _ in range(2):
            med, med_ai = create_spike_slime_medium(
                Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level, override_hp=hp_at_split,
            )
            combat.add_enemy(med, med_ai)

    def chooser(state_log: list[str], rng: Rng) -> str:
        split_power = creature.powers.get(PowerId.SPLIT)
        if split_power is not None and getattr(split_power, "triggered", False):
            return SPIKE_SLIME_LARGE_SPLIT_MOVE
        r = rng.next_float(100.0)
        if r < 30:
            if len(state_log) >= 2 and state_log[-1] == SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE and state_log[-2] == SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE:
                return SPIKE_SLIME_LARGE_LICK_MOVE
            return SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE
        if state_log and state_log[-1] == SPIKE_SLIME_LARGE_LICK_MOVE:
            return SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE
        return SPIKE_SLIME_LARGE_LICK_MOVE

    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_LARGE_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_LARGE_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE: MoveState(SPIKE_SLIME_LARGE_FLAME_TACKLE_MOVE, flame_tackle, [attack_intent(tackle_intent_damage), status_intent()], follow_up_id=SPIKE_SLIME_LARGE_BRANCH),
        SPIKE_SLIME_LARGE_LICK_MOVE: MoveState(SPIKE_SLIME_LARGE_LICK_MOVE, lick, [debuff_intent()], follow_up_id=SPIKE_SLIME_LARGE_BRANCH),
        SPIKE_SLIME_LARGE_SPLIT_MOVE: MoveState(SPIKE_SLIME_LARGE_SPLIT_MOVE, split, [Intent(IntentType.UNKNOWN)], follow_up_id=SPIKE_SLIME_LARGE_SPLIT_MOVE),
        SPIKE_SLIME_LARGE_BRANCH: BranchState(SPIKE_SLIME_LARGE_BRANCH, chooser),
    }
    return creature, MonsterAI(states, SPIKE_SLIME_LARGE_BRANCH, rng)


# ---- Lagavulin (Elite, HP 109-111 / 112-115 asc) ----

class _ExordiumLagavulinAsleepPower(AsleepPower):
    """Exordium Lagavulin's asleep/wake behavior: Metallicize (not Plating)
    is lost on waking, damage-wake stuns for that turn, natural wake (after
    3 turns) does not. Subclasses the shared ``AsleepPower`` since that
    class already special-cases ``LagavulinMatriarch`` by monster id in the
    same way -- this mirrors that pattern for our own Lagavulin id instead
    of touching the shared implementation.
    """

    def __init__(self, amount: int, awake_state_id: str):
        super().__init__(amount)
        self._awake_state_id = awake_state_id

    def after_damage_received(
        self, owner: Creature, target: Creature, dealer: Creature | None,
        damage: int, props: ValueProp, combat: "CombatState",
    ) -> None:
        if target is owner and damage > 0 and self.power_id in owner.powers:
            owner.powers.pop(PowerId.METALLICIZE, None)
            self.is_awake = True
            combat.stun_enemy(owner, self._awake_state_id)
            owner.powers.pop(self.power_id, None)

    def after_turn_end(self, owner: Creature, side: CombatSide, combat: "CombatState") -> None:
        if side == owner.side:
            self.amount -= 1
            if self.amount <= 0:
                owner.powers.pop(PowerId.METALLICIZE, None)
                self.is_awake = True
                owner.powers.pop(self.power_id, None)
                combat.set_enemy_state(owner, self._awake_state_id)
                # set_enemy_state (like the shared AsleepPower it mirrors)
                # only overwrites the AI's current state id -- it doesn't
                # walk through branch states like roll_move/_resolve_to_move
                # do. Our awake target is a BranchState (Attack/Debuff is a
                # real per-turn decision, unlike LagavulinMatriarch's fixed
                # "always SLASH_MOVE" natural-wake target), so resolve it
                # the rest of the way here.
                ai = combat.enemy_ais.get(owner.combat_id)
                if ai is not None:
                    ai._resolve_to_move(combat.monster_ai_rng)  # noqa: SLF001

    def before_turn_end_very_early(self, owner: Creature, side: CombatSide, combat: "CombatState") -> None:
        return  # Metallicize removal happens directly on wake, not pre-emptively.


LAGAVULIN_MONSTER_ID = "EXORDIUM_LAGAVULIN"
LAGAVULIN_BASE_MIN_HP = 109
LAGAVULIN_BASE_MAX_HP = 111
LAGAVULIN_TOUGH_MIN_HP = 112
LAGAVULIN_TOUGH_MAX_HP = 115
LAGAVULIN_METALLICIZE = 8
LAGAVULIN_ASLEEP_TURNS = 3
LAGAVULIN_BASE_ATTACK_DAMAGE = 18
LAGAVULIN_DEADLY_ATTACK_DAMAGE = 20
LAGAVULIN_BASE_DEBUFF_AMOUNT = -1
LAGAVULIN_DEADLY_DEBUFF_AMOUNT = -2
LAGAVULIN_SLEEP_MOVE = "SLEEP"
LAGAVULIN_ATTACK_MOVE = "ATTACK"
LAGAVULIN_DEBUFF_MOVE = "DEBUFF"
LAGAVULIN_SLEEP_BRANCH = "LAGAVULIN_SLEEP_BRANCH"
LAGAVULIN_AWAKE_BRANCH = "LAGAVULIN_AWAKE_BRANCH"


def create_lagavulin(rng: Rng, ascension_level: int = 0, starts_awake: bool = False) -> tuple[Creature, MonsterAI]:
    # starts_awake: DeadAdventurer event ambush variant (Lagavulin.cs
    # StartsAwake): no Metallicize, no Asleep power, and the first move is
    # DEBUFF (GetInitialStateId returns "DEBUFF" when StartsAwake and the
    # state log is empty).
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LAGAVULIN_TOUGH_MIN_HP, LAGAVULIN_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, LAGAVULIN_TOUGH_MAX_HP, LAGAVULIN_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=LAGAVULIN_MONSTER_ID)
    debuff_state = {"count": 0}

    def sleep_move(combat: CombatState) -> None:
        pass

    def attack_move(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, LAGAVULIN_DEADLY_ATTACK_DAMAGE, LAGAVULIN_BASE_ATTACK_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        debuff_state["count"] += 1

    def debuff_move(combat: CombatState) -> None:
        debuff_state["count"] = 0
        amount = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, LAGAVULIN_DEADLY_DEBUFF_AMOUNT, LAGAVULIN_BASE_DEBUFF_AMOUNT)
        apply_power_to_living_player_targets(combat, PowerId.DEXTERITY, amount, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.STRENGTH, amount, applier=creature)

    def awake_chooser(state_log: list[str], rng: Rng) -> str:
        if debuff_state["count"] >= 2:
            return LAGAVULIN_DEBUFF_MOVE
        if len(state_log) >= 2 and state_log[-1] == LAGAVULIN_ATTACK_MOVE and state_log[-2] == LAGAVULIN_ATTACK_MOVE:
            return LAGAVULIN_DEBUFF_MOVE
        return LAGAVULIN_ATTACK_MOVE

    sleep_branch = ConditionalBranchState(LAGAVULIN_SLEEP_BRANCH)
    sleep_branch.add_branch(lambda: creature.has_power(PowerId.ASLEEP), LAGAVULIN_SLEEP_MOVE)
    sleep_branch.add_branch(lambda: True, LAGAVULIN_AWAKE_BRANCH)

    attack_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, LAGAVULIN_DEADLY_ATTACK_DAMAGE, LAGAVULIN_BASE_ATTACK_DAMAGE)
    states: dict[str, MonsterState] = {
        LAGAVULIN_SLEEP_MOVE: MoveState(LAGAVULIN_SLEEP_MOVE, sleep_move, [sleep_intent()], follow_up_id=LAGAVULIN_SLEEP_BRANCH),
        LAGAVULIN_SLEEP_BRANCH: sleep_branch,
        LAGAVULIN_AWAKE_BRANCH: BranchState(LAGAVULIN_AWAKE_BRANCH, awake_chooser),
        LAGAVULIN_ATTACK_MOVE: MoveState(LAGAVULIN_ATTACK_MOVE, attack_move, [attack_intent(attack_intent_damage)], follow_up_id=LAGAVULIN_AWAKE_BRANCH),
        LAGAVULIN_DEBUFF_MOVE: MoveState(LAGAVULIN_DEBUFF_MOVE, debuff_move, [debuff_intent()], follow_up_id=LAGAVULIN_AWAKE_BRANCH),
    }

    if starts_awake:
        return creature, MonsterAI(states, LAGAVULIN_DEBUFF_MOVE)
    creature.apply_power(PowerId.METALLICIZE, LAGAVULIN_METALLICIZE)
    creature.powers[PowerId.ASLEEP] = _ExordiumLagavulinAsleepPower(LAGAVULIN_ASLEEP_TURNS, LAGAVULIN_AWAKE_BRANCH)
    return creature, MonsterAI(states, LAGAVULIN_SLEEP_MOVE)


# ---- Sentry (used in SentriesElite; HP 38-42 / 39-45 asc) ----

SENTRY_MONSTER_ID = "EXORDIUM_SENTRY"
SENTRY_BASE_MIN_HP = 38
SENTRY_BASE_MAX_HP = 42
SENTRY_TOUGH_MIN_HP = 39
SENTRY_TOUGH_MAX_HP = 45
SENTRY_ARTIFACT = 1
SENTRY_BASE_BOLT_COUNT = 2
SENTRY_DEADLY_BOLT_COUNT = 3
SENTRY_BASE_BEAM_DAMAGE = 9
SENTRY_DEADLY_BEAM_DAMAGE = 10
SENTRY_BOLT_MOVE = "BOLT"
SENTRY_BEAM_MOVE = "BEAM"


def create_sentry(rng: Rng, ascension_level: int = 0, bolt_first: bool = True) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SENTRY_TOUGH_MIN_HP, SENTRY_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SENTRY_TOUGH_MAX_HP, SENTRY_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SENTRY_MONSTER_ID)

    def bolt(combat: CombatState) -> None:
        count = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SENTRY_DEADLY_BOLT_COUNT, SENTRY_BASE_BOLT_COUNT)
        add_generated_cards_to_living_player_discards(combat, make_dazed, count)

    def beam(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SENTRY_DEADLY_BEAM_DAMAGE, SENTRY_BASE_BEAM_DAMAGE)
        targets = living_player_targets(combat)[:1]
        _deal_damage_to_targets(combat, creature, targets, dmg)

    beam_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SENTRY_DEADLY_BEAM_DAMAGE, SENTRY_BASE_BEAM_DAMAGE)
    states: dict[str, MonsterState] = {
        SENTRY_BOLT_MOVE: MoveState(SENTRY_BOLT_MOVE, bolt, [status_intent()], follow_up_id=SENTRY_BEAM_MOVE),
        SENTRY_BEAM_MOVE: MoveState(SENTRY_BEAM_MOVE, beam, [attack_intent(beam_intent_damage)], follow_up_id=SENTRY_BOLT_MOVE),
    }
    creature.apply_power(PowerId.ARTIFACT, SENTRY_ARTIFACT)
    initial = SENTRY_BOLT_MOVE if bolt_first else SENTRY_BEAM_MOVE
    return creature, MonsterAI(states, initial)


# ========================================================================
# BOSSES
# ========================================================================

# ---- SlimeBoss (HP fixed 140 / 150 asc, both min==max) ----

SLIME_BOSS_MONSTER_ID = "EXORDIUM_SLIME_BOSS"
SLIME_BOSS_BASE_HP = 140
SLIME_BOSS_TOUGH_HP = 150
SLIME_BOSS_BASE_SLIMED_COUNT = 3
SLIME_BOSS_DEADLY_SLIMED_COUNT = 5
SLIME_BOSS_BASE_SLAM_DAMAGE = 35
SLIME_BOSS_DEADLY_SLAM_DAMAGE = 38
SLIME_BOSS_GOOP_SPRAY_MOVE = "GOOP_SPRAY"
SLIME_BOSS_PREP_SLAM_MOVE = "PREP_SLAM"
SLIME_BOSS_SLAM_MOVE = "SLAM"
SLIME_BOSS_SPLIT_MOVE = "SPLIT"
SLIME_BOSS_BRANCH = "SLIME_BOSS_BRANCH"


def create_slime_boss(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SLIME_BOSS_TOUGH_HP, SLIME_BOSS_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=SLIME_BOSS_MONSTER_ID)
    creature.apply_power(PowerId.SPLIT, 1)

    def goop_spray(combat: CombatState) -> None:
        count = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SLIME_BOSS_DEADLY_SLIMED_COUNT, SLIME_BOSS_BASE_SLIMED_COUNT)
        add_generated_cards_to_living_player_discards(combat, make_slimed, count)

    def prep_slam(combat: CombatState) -> None:
        pass

    def slam(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SLIME_BOSS_DEADLY_SLAM_DAMAGE, SLIME_BOSS_BASE_SLAM_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def split(combat: CombatState) -> None:
        hp_at_split = max(1, creature.current_hp)
        combat.kill_creature(creature)
        spike, spike_ai = create_spike_slime_large(
            Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level, override_hp=hp_at_split,
        )
        combat.add_enemy(spike, spike_ai)
        acid, acid_ai = create_acid_slime_large(
            Rng(combat.rng.next_int(0, INT_MAX)), ascension_level=combat.ascension_level, override_hp=hp_at_split,
        )
        combat.add_enemy(acid, acid_ai)

    def branch_chooser(state_log: list[str], rng: Rng) -> str:
        split_power = creature.powers.get(PowerId.SPLIT)
        if split_power is not None and getattr(split_power, "triggered", False):
            return SLIME_BOSS_SPLIT_MOVE
        return SLIME_BOSS_GOOP_SPRAY_MOVE

    slam_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SLIME_BOSS_DEADLY_SLAM_DAMAGE, SLIME_BOSS_BASE_SLAM_DAMAGE)
    states: dict[str, MonsterState] = {
        SLIME_BOSS_GOOP_SPRAY_MOVE: MoveState(SLIME_BOSS_GOOP_SPRAY_MOVE, goop_spray, [status_intent()], follow_up_id=SLIME_BOSS_PREP_SLAM_MOVE),
        SLIME_BOSS_PREP_SLAM_MOVE: MoveState(SLIME_BOSS_PREP_SLAM_MOVE, prep_slam, [Intent(IntentType.UNKNOWN)], follow_up_id=SLIME_BOSS_SLAM_MOVE),
        SLIME_BOSS_SLAM_MOVE: MoveState(SLIME_BOSS_SLAM_MOVE, slam, [attack_intent(slam_intent_damage)], follow_up_id=SLIME_BOSS_BRANCH),
        SLIME_BOSS_SPLIT_MOVE: MoveState(SLIME_BOSS_SPLIT_MOVE, split, [Intent(IntentType.UNKNOWN)], follow_up_id=SLIME_BOSS_SPLIT_MOVE),
        SLIME_BOSS_BRANCH: BranchState(SLIME_BOSS_BRANCH, branch_chooser),
    }
    return creature, MonsterAI(states, SLIME_BOSS_GOOP_SPRAY_MOVE)


# ---- Guardian (HP fixed 240 / 250 asc, both min==max) ----

GUARDIAN_MONSTER_ID = "EXORDIUM_GUARDIAN"
GUARDIAN_BASE_HP = 240
GUARDIAN_TOUGH_HP = 250
GUARDIAN_CHARGE_UP_BLOCK = 9
GUARDIAN_BASE_FIERCE_BASH_DAMAGE = 32
GUARDIAN_DEADLY_FIERCE_BASH_DAMAGE = 36
GUARDIAN_VENT_DEBUFF_AMOUNT = 2
GUARDIAN_WHIRLWIND_DAMAGE = 5  # flat
GUARDIAN_WHIRLWIND_HITS = 4
GUARDIAN_BASE_SHARP_HIDE = 3
GUARDIAN_DEADLY_SHARP_HIDE = 4
GUARDIAN_BASE_ROLL_DAMAGE = 9
GUARDIAN_DEADLY_ROLL_DAMAGE = 10
GUARDIAN_TWIN_SLAM_DAMAGE = 8  # flat
GUARDIAN_TWIN_SLAM_HITS = 2
GUARDIAN_DEFENSIVE_BLOCK = 20
GUARDIAN_THRESHOLD_INCREASE = 10
GUARDIAN_BASE_THRESHOLD = 30
GUARDIAN_TOUGH_THRESHOLD = 40
GUARDIAN_CHARGE_UP_MOVE = "CHARGE_UP"
GUARDIAN_FIERCE_BASH_MOVE = "FIERCE_BASH"
GUARDIAN_VENT_STEAM_MOVE = "VENT_STEAM"
GUARDIAN_WHIRLWIND_MOVE = "WHIRLWIND"
GUARDIAN_CLOSE_UP_MOVE = "CLOSE_UP"
GUARDIAN_ROLL_ATTACK_MOVE = "ROLL_ATTACK"
GUARDIAN_TWIN_SLAM_MOVE = "TWIN_SLAM"
GUARDIAN_OFFENSIVE_BRANCH = "GUARDIAN_OFFENSIVE_BRANCH"

_GUARDIAN_OFFENSIVE_SEQUENCE = {
    GUARDIAN_CHARGE_UP_MOVE: GUARDIAN_FIERCE_BASH_MOVE,
    GUARDIAN_FIERCE_BASH_MOVE: GUARDIAN_VENT_STEAM_MOVE,
    GUARDIAN_VENT_STEAM_MOVE: GUARDIAN_WHIRLWIND_MOVE,
    GUARDIAN_TWIN_SLAM_MOVE: GUARDIAN_WHIRLWIND_MOVE,
    GUARDIAN_WHIRLWIND_MOVE: GUARDIAN_CHARGE_UP_MOVE,
}


def create_guardian(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GUARDIAN_TOUGH_HP, GUARDIAN_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=GUARDIAN_MONSTER_ID)

    def _transition_to_defensive(combat: CombatState, *, set_move: bool) -> None:
        # Shared defensive-mode transition (C# Guardian.TransitionToDefensiveMode):
        # close the shell, gain the defensive block, and bump the next
        # threshold. When ``set_move`` (the immediate player-turn case), also
        # force the CLOSE_UP intent right now -- the C# SetMoveImmediate(
        # _closeUpState, forceTransition: true). The deferred (enemy-turn)
        # case leaves the move alone; the offensive branch chooser then
        # routes to CLOSE_UP on the next roll because ``is_open`` is now False.
        mode = creature.powers.get(PowerId.MODE_SHIFT)
        if mode is None:
            return
        mode.pending_shift = False
        mode.is_open = False
        mode.base_threshold += GUARDIAN_THRESHOLD_INCREASE
        _gain_block(creature, GUARDIAN_DEFENSIVE_BLOCK, combat)
        if set_move:
            combat.set_enemy_state(creature, GUARDIAN_CLOSE_UP_MOVE)

    def _check_pending_mode_shift(combat: CombatState) -> None:
        mode = creature.powers.get(PowerId.MODE_SHIFT)
        if mode is not None and mode.pending_shift:
            _transition_to_defensive(combat, set_move=False)

    def charge_up(combat: CombatState) -> None:
        _gain_block(creature, GUARDIAN_CHARGE_UP_BLOCK, combat)
        _check_pending_mode_shift(combat)

    def fierce_bash(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GUARDIAN_DEADLY_FIERCE_BASH_DAMAGE, GUARDIAN_BASE_FIERCE_BASH_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)
        _check_pending_mode_shift(combat)

    def vent_steam(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, GUARDIAN_VENT_DEBUFF_AMOUNT, applier=creature)
        apply_power_to_living_player_targets(combat, PowerId.VULNERABLE, GUARDIAN_VENT_DEBUFF_AMOUNT, applier=creature)
        _check_pending_mode_shift(combat)

    def whirlwind(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, GUARDIAN_WHIRLWIND_DAMAGE, hits=GUARDIAN_WHIRLWIND_HITS)
        _check_pending_mode_shift(combat)

    def close_up(combat: CombatState) -> None:
        sharp_hide = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GUARDIAN_DEADLY_SHARP_HIDE, GUARDIAN_BASE_SHARP_HIDE)
        creature.apply_power(PowerId.THORNS, sharp_hide, applier=creature)

    def roll_attack(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, GUARDIAN_DEADLY_ROLL_DAMAGE, GUARDIAN_BASE_ROLL_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def twin_slam(combat: CombatState) -> None:
        mode = creature.powers.get(PowerId.MODE_SHIFT)
        if mode is None:
            creature.apply_power(PowerId.MODE_SHIFT, 1)
            mode = creature.powers[PowerId.MODE_SHIFT]
            mode.on_immediate_shift = lambda combat: _transition_to_defensive(combat, set_move=True)
        mode.start(mode.base_threshold)
        creature.block = 0
        _deal_damage_to_player(combat, creature, GUARDIAN_TWIN_SLAM_DAMAGE, hits=GUARDIAN_TWIN_SLAM_HITS)
        creature.powers.pop(PowerId.THORNS, None)
        _check_pending_mode_shift(combat)

    def offensive_branch_chooser(state_log: list[str], rng: Rng) -> str:
        mode = creature.powers.get(PowerId.MODE_SHIFT)
        if mode is not None and not mode.is_open:
            return GUARDIAN_CLOSE_UP_MOVE
        last_move = state_log[-1] if state_log else None
        return _GUARDIAN_OFFENSIVE_SEQUENCE.get(last_move, GUARDIAN_CHARGE_UP_MOVE)

    fierce_bash_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GUARDIAN_DEADLY_FIERCE_BASH_DAMAGE, GUARDIAN_BASE_FIERCE_BASH_DAMAGE)
    roll_intent = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, GUARDIAN_DEADLY_ROLL_DAMAGE, GUARDIAN_BASE_ROLL_DAMAGE)
    states: dict[str, MonsterState] = {
        GUARDIAN_CHARGE_UP_MOVE: MoveState(GUARDIAN_CHARGE_UP_MOVE, charge_up, [defend_intent()], follow_up_id=GUARDIAN_OFFENSIVE_BRANCH),
        GUARDIAN_FIERCE_BASH_MOVE: MoveState(GUARDIAN_FIERCE_BASH_MOVE, fierce_bash, [attack_intent(fierce_bash_intent)], follow_up_id=GUARDIAN_OFFENSIVE_BRANCH),
        GUARDIAN_VENT_STEAM_MOVE: MoveState(GUARDIAN_VENT_STEAM_MOVE, vent_steam, [debuff_intent()], follow_up_id=GUARDIAN_OFFENSIVE_BRANCH),
        GUARDIAN_WHIRLWIND_MOVE: MoveState(GUARDIAN_WHIRLWIND_MOVE, whirlwind, [multi_attack_intent(GUARDIAN_WHIRLWIND_DAMAGE, GUARDIAN_WHIRLWIND_HITS)], follow_up_id=GUARDIAN_OFFENSIVE_BRANCH),
        GUARDIAN_CLOSE_UP_MOVE: MoveState(GUARDIAN_CLOSE_UP_MOVE, close_up, [buff_intent()], follow_up_id=GUARDIAN_ROLL_ATTACK_MOVE),
        GUARDIAN_ROLL_ATTACK_MOVE: MoveState(GUARDIAN_ROLL_ATTACK_MOVE, roll_attack, [attack_intent(roll_intent)], follow_up_id=GUARDIAN_TWIN_SLAM_MOVE),
        GUARDIAN_TWIN_SLAM_MOVE: MoveState(GUARDIAN_TWIN_SLAM_MOVE, twin_slam, [multi_attack_intent(GUARDIAN_TWIN_SLAM_DAMAGE, GUARDIAN_TWIN_SLAM_HITS), buff_intent()], follow_up_id=GUARDIAN_OFFENSIVE_BRANCH),
        GUARDIAN_OFFENSIVE_BRANCH: BranchState(GUARDIAN_OFFENSIVE_BRANCH, offensive_branch_chooser),
    }

    base_threshold = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, GUARDIAN_TOUGH_THRESHOLD, GUARDIAN_BASE_THRESHOLD)
    creature.apply_power(PowerId.MODE_SHIFT, 1)
    mode_power = creature.powers[PowerId.MODE_SHIFT]
    mode_power.start(base_threshold)
    # Register the immediate defensive-mode transition used when the
    # threshold breaks on the player's turn (C# ModeShiftPower ->
    # TransitionToDefensiveMode when !IsExecutingMove).
    mode_power.on_immediate_shift = lambda combat: _transition_to_defensive(combat, set_move=True)
    return creature, MonsterAI(states, GUARDIAN_CHARGE_UP_MOVE)


# ---- Hexaghost (HP fixed 250 / 264 asc, both min==max) ----

HEXAGHOST_MONSTER_ID = "EXORDIUM_HEXAGHOST"
HEXAGHOST_BASE_HP = 250
HEXAGHOST_TOUGH_HP = 264
HEXAGHOST_SEAR_DAMAGE = 6  # flat
HEXAGHOST_BASE_SEAR_BURN_COUNT = 1
HEXAGHOST_DEADLY_SEAR_BURN_COUNT = 2
HEXAGHOST_BASE_TACKLE_DAMAGE = 5
HEXAGHOST_DEADLY_TACKLE_DAMAGE = 6
HEXAGHOST_TACKLE_HITS = 2
HEXAGHOST_INFLAME_BLOCK = 12
HEXAGHOST_BASE_INFLAME_STRENGTH = 2
HEXAGHOST_DEADLY_INFLAME_STRENGTH = 3
HEXAGHOST_BASE_INFERNO_DAMAGE = 2
HEXAGHOST_DEADLY_INFERNO_DAMAGE = 3
HEXAGHOST_INFERNO_HITS = 6
HEXAGHOST_INFERNO_BONUS_BURNS = 3
HEXAGHOST_ACTIVATE_MOVE = "ACTIVATE"
HEXAGHOST_DIVIDER_MOVE = "DIVIDER"
HEXAGHOST_SEAR_MOVE = "SEAR"
HEXAGHOST_TACKLE_MOVE = "TACKLE"
HEXAGHOST_INFLAME_MOVE = "INFLAME"
HEXAGHOST_INFERNO_MOVE = "INFERNO"
HEXAGHOST_BRANCH = "HEXAGHOST_BRANCH"

_HEXAGHOST_CYCLE = {
    0: HEXAGHOST_SEAR_MOVE,
    1: HEXAGHOST_TACKLE_MOVE,
    2: HEXAGHOST_SEAR_MOVE,
    3: HEXAGHOST_INFLAME_MOVE,
    4: HEXAGHOST_TACKLE_MOVE,
    5: HEXAGHOST_SEAR_MOVE,
    6: HEXAGHOST_INFERNO_MOVE,
}


def create_hexaghost(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    hp = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_TOUGH_HP, HEXAGHOST_BASE_HP)
    creature = Creature(max_hp=hp, monster_id=HEXAGHOST_MONSTER_ID)
    orb = {"count": 0, "divider_dmg": 1, "burns_upgraded": False}

    def activate(combat: CombatState) -> None:
        orb["count"] = 6
        targets = living_player_targets(combat)
        avg_hp = (sum(t.current_hp for t in targets) / len(targets)) if targets else 1.0
        orb["divider_dmg"] = math.floor(avg_hp / 12.0) + 1

    def divider(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, orb["divider_dmg"], hits=6)
        orb["count"] = 0

    def tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_TACKLE_DAMAGE, HEXAGHOST_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=HEXAGHOST_TACKLE_HITS)
        orb["count"] += 1

    def inflame(combat: CombatState) -> None:
        _gain_block(creature, HEXAGHOST_INFLAME_BLOCK, combat)
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_INFLAME_STRENGTH, HEXAGHOST_BASE_INFLAME_STRENGTH)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)
        orb["count"] += 1

    def sear(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, HEXAGHOST_SEAR_DAMAGE)
        count = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_SEAR_BURN_COUNT, HEXAGHOST_BASE_SEAR_BURN_COUNT)
        factory = _upgraded_burn if orb["burns_upgraded"] else make_burn
        add_generated_cards_to_living_player_discards(combat, factory, count)
        orb["count"] += 1

    def inferno(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_INFERNO_DAMAGE, HEXAGHOST_BASE_INFERNO_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg, hits=HEXAGHOST_INFERNO_HITS)
        for target in living_player_targets(combat):
            pstate = combat.combat_player_state_for(target)
            if pstate is None:
                continue
            for pile in (pstate.hand, pstate.draw, pstate.discard):
                for card in list(pile):
                    if getattr(card, "card_id", None) == CardId.BURN and not getattr(card, "upgraded", False):
                        _force_upgrade_burn(card)
        add_generated_cards_to_living_player_discards(combat, _upgraded_burn, HEXAGHOST_INFERNO_BONUS_BURNS)
        orb["burns_upgraded"] = True
        orb["count"] = 0

    def branch_chooser(state_log: list[str], rng: Rng) -> str:
        return _HEXAGHOST_CYCLE.get(orb["count"], HEXAGHOST_SEAR_MOVE)

    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_TACKLE_DAMAGE, HEXAGHOST_BASE_TACKLE_DAMAGE)
    inferno_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, HEXAGHOST_DEADLY_INFERNO_DAMAGE, HEXAGHOST_BASE_INFERNO_DAMAGE)
    states: dict[str, MonsterState] = {
        HEXAGHOST_ACTIVATE_MOVE: MoveState(HEXAGHOST_ACTIVATE_MOVE, activate, [Intent(IntentType.UNKNOWN)], follow_up_id=HEXAGHOST_DIVIDER_MOVE),
        # Divider's real damage is only known once ACTIVATE has run (it is
        # derived from live player HP); the declared intent below is a
        # placeholder shown before that first resolves.
        HEXAGHOST_DIVIDER_MOVE: MoveState(HEXAGHOST_DIVIDER_MOVE, divider, [multi_attack_intent(1, 6)], follow_up_id=HEXAGHOST_BRANCH),
        HEXAGHOST_TACKLE_MOVE: MoveState(HEXAGHOST_TACKLE_MOVE, tackle, [multi_attack_intent(tackle_intent_damage, HEXAGHOST_TACKLE_HITS)], follow_up_id=HEXAGHOST_BRANCH),
        HEXAGHOST_INFLAME_MOVE: MoveState(HEXAGHOST_INFLAME_MOVE, inflame, [defend_intent(), buff_intent()], follow_up_id=HEXAGHOST_BRANCH),
        HEXAGHOST_SEAR_MOVE: MoveState(HEXAGHOST_SEAR_MOVE, sear, [attack_intent(HEXAGHOST_SEAR_DAMAGE), status_intent()], follow_up_id=HEXAGHOST_BRANCH),
        HEXAGHOST_INFERNO_MOVE: MoveState(HEXAGHOST_INFERNO_MOVE, inferno, [multi_attack_intent(inferno_intent_damage, HEXAGHOST_INFERNO_HITS), debuff_intent()], follow_up_id=HEXAGHOST_BRANCH),
        HEXAGHOST_BRANCH: BranchState(HEXAGHOST_BRANCH, branch_chooser),
    }
    return creature, MonsterAI(states, HEXAGHOST_ACTIVATE_MOVE)


# ---- SpikeSlimeSmall / AcidSlimeSmall ----
# NOT in the task's monster spec list -- required by SmallSlimesWeak and
# LotsOfSlimesNormal (both of which name "SpikeSlimeSmall"/"AcidSlimeSmall"
# explicitly) but no move/HP spec was given for them. Ported directly from
# the decompiled mod source (SpikeSlimeSmall.cs / AcidSlimeSmall.cs).

SPIKE_SLIME_SMALL_MONSTER_ID = "EXORDIUM_SPIKE_SLIME_S"
SPIKE_SLIME_SMALL_BASE_MIN_HP = 10
SPIKE_SLIME_SMALL_BASE_MAX_HP = 14
SPIKE_SLIME_SMALL_TOUGH_MIN_HP = 11
SPIKE_SLIME_SMALL_TOUGH_MAX_HP = 15
SPIKE_SLIME_SMALL_BASE_TACKLE_DAMAGE = 5
SPIKE_SLIME_SMALL_DEADLY_TACKLE_DAMAGE = 6
SPIKE_SLIME_SMALL_TACKLE_MOVE = "TACKLE"


def create_spike_slime_small(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_SMALL_TOUGH_MIN_HP, SPIKE_SLIME_SMALL_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_SMALL_TOUGH_MAX_HP, SPIKE_SLIME_SMALL_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=SPIKE_SLIME_SMALL_MONSTER_ID)

    def tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_SMALL_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_SMALL_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, SPIKE_SLIME_SMALL_DEADLY_TACKLE_DAMAGE, SPIKE_SLIME_SMALL_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        SPIKE_SLIME_SMALL_TACKLE_MOVE: MoveState(SPIKE_SLIME_SMALL_TACKLE_MOVE, tackle, [attack_intent(tackle_intent_damage)], follow_up_id=SPIKE_SLIME_SMALL_TACKLE_MOVE),
    }
    return creature, MonsterAI(states, SPIKE_SLIME_SMALL_TACKLE_MOVE)


ACID_SLIME_SMALL_MONSTER_ID = "EXORDIUM_ACID_SLIME_S"
ACID_SLIME_SMALL_BASE_MIN_HP = 8
ACID_SLIME_SMALL_BASE_MAX_HP = 12
ACID_SLIME_SMALL_TOUGH_MIN_HP = 9
ACID_SLIME_SMALL_TOUGH_MAX_HP = 13
ACID_SLIME_SMALL_BASE_TACKLE_DAMAGE = 3
ACID_SLIME_SMALL_DEADLY_TACKLE_DAMAGE = 4
ACID_SLIME_SMALL_WEAK_AMOUNT = 1  # flat, not ascension-scaled
ACID_SLIME_SMALL_TACKLE_MOVE = "TACKLE"
ACID_SLIME_SMALL_LICK_MOVE = "LICK"


def create_acid_slime_small(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_SMALL_TOUGH_MIN_HP, ACID_SLIME_SMALL_BASE_MIN_HP)
    hi = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_SMALL_TOUGH_MAX_HP, ACID_SLIME_SMALL_BASE_MAX_HP)
    hp = rng.next_int(lo, hi)
    creature = Creature(max_hp=hp, monster_id=ACID_SLIME_SMALL_MONSTER_ID)

    def tackle(combat: CombatState) -> None:
        dmg = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_SMALL_DEADLY_TACKLE_DAMAGE, ACID_SLIME_SMALL_BASE_TACKLE_DAMAGE)
        _deal_damage_to_player(combat, creature, dmg)

    def lick(combat: CombatState) -> None:
        apply_power_to_living_player_targets(combat, PowerId.WEAK, ACID_SLIME_SMALL_WEAK_AMOUNT, applier=creature)

    tackle_intent_damage = _ascension_value(ascension_level, DEADLY_ENEMIES_ASCENSION_LEVEL, ACID_SLIME_SMALL_DEADLY_TACKLE_DAMAGE, ACID_SLIME_SMALL_BASE_TACKLE_DAMAGE)
    states: dict[str, MonsterState] = {
        ACID_SLIME_SMALL_TACKLE_MOVE: MoveState(ACID_SLIME_SMALL_TACKLE_MOVE, tackle, [attack_intent(tackle_intent_damage)], follow_up_id=ACID_SLIME_SMALL_LICK_MOVE),
        ACID_SLIME_SMALL_LICK_MOVE: MoveState(ACID_SLIME_SMALL_LICK_MOVE, lick, [debuff_intent()], follow_up_id=ACID_SLIME_SMALL_TACKLE_MOVE),
    }
    # Real STS1/mod AcidSlimeSmall opens on LICK, not TACKLE.
    return creature, MonsterAI(states, ACID_SLIME_SMALL_LICK_MOVE)


# ---- FungiBeast (HP 22-28 / 24-28 asc) ----
# NOT in the task's monster spec list -- required by ExordiumWildlifeNormal
# and TwoFungiBeastsNormal (both of which DO name it) but no move/HP/branch
# spec was given. Ported directly from the decompiled mod source
# (FungiBeast.cs) instead. GAP: FungiBeast.cs applies a "SporeCloudPower"
# to itself on spawn (2 stacks); that power's own .cs file was not present
# in the decompiled snapshot used for this port (only referenced from
# FungiBeast.cs) and isn't one of the powers this task's spec called out,
# so it is omitted here rather than guessed at -- see report.

FUNGI_BEAST_MONSTER_ID = "EXORDIUM_FUNGI_BEAST"
FUNGI_BEAST_BASE_MIN_HP = 22
FUNGI_BEAST_TOUGH_MIN_HP = 24
FUNGI_BEAST_MAX_HP = 28  # not ascension-scaled (C#: MaxInitialHp => 28 always)
FUNGI_BEAST_BITE_DAMAGE = 6  # flat, not ascension-scaled
FUNGI_BEAST_BASE_GROW_STRENGTH = 3
FUNGI_BEAST_DEADLY_GROW_STRENGTH = 5
FUNGI_BEAST_BITE_MOVE = "BITE"
FUNGI_BEAST_GROW_MOVE = "GROW"
FUNGI_BEAST_BRANCH = "FUNGI_BEAST_BRANCH"


def create_fungi_beast(rng: Rng, ascension_level: int = 0) -> tuple[Creature, MonsterAI]:
    lo = _ascension_value(ascension_level, TOUGH_ENEMIES_ASCENSION_LEVEL, FUNGI_BEAST_TOUGH_MIN_HP, FUNGI_BEAST_BASE_MIN_HP)
    hp = rng.next_int(lo, FUNGI_BEAST_MAX_HP)
    creature = Creature(max_hp=hp, monster_id=FUNGI_BEAST_MONSTER_ID)

    def bite(combat: CombatState) -> None:
        _deal_damage_to_player(combat, creature, FUNGI_BEAST_BITE_DAMAGE)

    def grow(combat: CombatState) -> None:
        strength = _ascension_value(_combat_ascension_level(combat), DEADLY_ENEMIES_ASCENSION_LEVEL, FUNGI_BEAST_DEADLY_GROW_STRENGTH, FUNGI_BEAST_BASE_GROW_STRENGTH)
        creature.apply_power(PowerId.STRENGTH, strength, applier=creature)

    def chooser(state_log: list[str], rng: Rng) -> str:
        r = rng.next_float(100.0)
        if r < 60:
            if len(state_log) >= 2 and state_log[-1] == FUNGI_BEAST_BITE_MOVE and state_log[-2] == FUNGI_BEAST_BITE_MOVE:
                return FUNGI_BEAST_GROW_MOVE
            return FUNGI_BEAST_BITE_MOVE
        if state_log and state_log[-1] == FUNGI_BEAST_GROW_MOVE:
            return FUNGI_BEAST_BITE_MOVE
        return FUNGI_BEAST_GROW_MOVE

    states: dict[str, MonsterState] = {
        FUNGI_BEAST_BITE_MOVE: MoveState(FUNGI_BEAST_BITE_MOVE, bite, [attack_intent(FUNGI_BEAST_BITE_DAMAGE)], follow_up_id=FUNGI_BEAST_BRANCH),
        FUNGI_BEAST_GROW_MOVE: MoveState(FUNGI_BEAST_GROW_MOVE, grow, [buff_intent()], follow_up_id=FUNGI_BEAST_BRANCH),
        FUNGI_BEAST_BRANCH: BranchState(FUNGI_BEAST_BRANCH, chooser),
    }
    return creature, MonsterAI(states, FUNGI_BEAST_BRANCH, rng)
