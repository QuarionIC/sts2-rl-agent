"""Behaviour tests for PowerId.STRENGTH_UP (ActsFromThePast StrengthUpPower).

Ground truth: decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/
StrengthUpPower.cs, whose entire behaviour is one hook::

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext,
            CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == ((PowerModel)this).Owner.Side)
        {
            ((PowerModel)this).Flash();
            await PowerCmd.Apply<StrengthPower>(..., Owner, (decimal)Amount, Owner, null, false);
        }
    }

plus ``Type => (PowerType)1`` (Buff) and ``StackType => (PowerStackType)1``
(Counter). Applied by OrbWalker.AfterAddedToRoom at 3, or 5 from Ascension 9.

What these tests pin down, in the order it matters:

* the TIMING -- end of the owner's own side turn, not turn start, and not the
  other side's turn end;
* that it fires on the very FIRST own-side turn end, which is the whole reason
  this is not an alias for RITUAL (RitualPower skips its first tick when an
  enemy applied it);
* that the counter never decays, so Strength grows linearly forever;
* that it stacks additively and pays out at the stacked amount.
"""

from __future__ import annotations

import pytest

import sts2_env.powers  # noqa: F401  (registration side effect)
from sts2_env.core.creature import get_power_class
from sts2_env.core.enums import CombatSide, PowerId, PowerStackType, PowerType
from sts2_env.core.hooks import fire_after_side_turn_start, fire_after_turn_end
from sts2_env.powers.monster import StrengthUpPower


STRENGTH_UP_AMOUNT = 3
#: OrbWalker's Ascension-9 value; used to check the payout tracks Amount.
STRENGTH_UP_DEADLY_AMOUNT = 5
TURNS_TO_CHECK_FOR_DECAY = 5

#: A dummy enemy that does exactly one thing every turn, so a damage delta
#: across turns can only come from Strength.
FIXED_ATTACK_DAMAGE = 6
FIXED_ATTACKER_HP = 200
FIXED_ATTACKER_PLAYER_HP = 200


def _enemy(combat):
    return combat.enemies[0]


def _combat_with_fixed_attacker():
    """Combat whose only enemy attacks for FIXED_ATTACK_DAMAGE every turn."""
    from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
    from sts2_env.core.combat import CombatState
    from sts2_env.core.creature import Creature
    from sts2_env.core.damage import apply_damage, calculate_damage
    from sts2_env.core.enums import ValueProp
    from sts2_env.monsters.intents import attack_intent
    from sts2_env.monsters.state_machine import MonsterAI, MoveState

    combat = CombatState(
        player_hp=FIXED_ATTACKER_PLAYER_HP,
        player_max_hp=FIXED_ATTACKER_PLAYER_HP,
        deck=create_ironclad_starter_deck(),
        rng_seed=42,
    )
    enemy = Creature(max_hp=FIXED_ATTACKER_HP, monster_id="STRENGTH_UP_TEST_DUMMY")

    def strike(active_combat) -> None:
        target = active_combat.player
        dmg = calculate_damage(
            FIXED_ATTACK_DAMAGE, enemy, target, ValueProp.MOVE, active_combat
        )
        apply_damage(target, dmg, ValueProp.MOVE, active_combat, enemy)

    ai = MonsterAI(
        {
            "STRIKE": MoveState(
                "STRIKE",
                strike,
                [attack_intent(FIXED_ATTACK_DAMAGE)],
                follow_up_id="STRIKE",
            )
        },
        "STRIKE",
    )
    combat.add_enemy(enemy, ai)
    combat.start_combat()
    return combat, enemy


# ---------------------------------------------------------------------------
# Registration / metadata
# ---------------------------------------------------------------------------

def test_strength_up_is_registered_with_buff_counter_metadata():
    """(PowerType)1 == Buff, (PowerStackType)1 == Counter in the game enums."""
    assert get_power_class(PowerId.STRENGTH_UP) is StrengthUpPower
    assert StrengthUpPower.power_type is PowerType.BUFF
    assert StrengthUpPower.stack_type is PowerStackType.COUNTER
    assert StrengthUpPower(STRENGTH_UP_AMOUNT).power_id is PowerId.STRENGTH_UP


def test_live_wire_power_id_resolves_to_the_implemented_power():
    """The reconstruct path must land on the class, not just the enum member."""
    from sts2_env.bridge.combat_reconstruct import _to_power_id

    resolved = _to_power_id("ACTSFROMTHEPAST-STRENGTH_UP_POWER")
    assert resolved is PowerId.STRENGTH_UP
    assert get_power_class(resolved) is StrengthUpPower


# ---------------------------------------------------------------------------
# Timing: AfterSideTurnEnd, gated on side == Owner.Side
# ---------------------------------------------------------------------------

def test_strength_is_not_granted_at_turn_start(simple_combat):
    """The hook is AfterSideTurnEnd. Turn START must do nothing at all."""
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    fire_after_side_turn_start(CombatSide.PLAYER, simple_combat)
    fire_after_side_turn_start(CombatSide.ENEMY, simple_combat)

    assert enemy.get_power_amount(PowerId.STRENGTH) == 0


def test_enemy_gains_strength_only_at_the_end_of_the_enemy_turn(simple_combat):
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    # side != Owner.Side -> the C# `if` does not run.
    fire_after_turn_end(CombatSide.PLAYER, simple_combat)
    assert enemy.get_power_amount(PowerId.STRENGTH) == 0

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)
    assert enemy.get_power_amount(PowerId.STRENGTH) == STRENGTH_UP_AMOUNT


def test_player_owned_strength_up_fires_on_the_player_side_only(simple_combat):
    """`side == Owner.Side` is symmetric -- it is not hardcoded to ENEMY."""
    player = simple_combat.player
    simple_combat.apply_power_to(player, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)
    assert player.get_power_amount(PowerId.STRENGTH) == 0

    fire_after_turn_end(CombatSide.PLAYER, simple_combat)
    assert player.get_power_amount(PowerId.STRENGTH) == STRENGTH_UP_AMOUNT


# ---------------------------------------------------------------------------
# The alias-distinguishing behaviour: no skipped first tick
# ---------------------------------------------------------------------------

def test_first_enemy_turn_end_already_pays_out_unlike_ritual(simple_combat):
    """This is why STRENGTH_UP is not aliased onto RITUAL.

    RitualPower sets skip_next_tick when an ENEMY applies it
    (C# RitualPower.WasJustAppliedByEnemy), so enemy Ritual pays nothing on the
    first turn end. StrengthUpPower has no such flag: reading the .cs, the only
    guard is `side == Owner.Side`. An enemy aliased onto RITUAL would therefore
    be a full Amount of Strength behind for the entire fight, and the planner
    would under-estimate every incoming hit.
    """
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT, applier=enemy)
    simple_combat.apply_power_to(enemy, PowerId.RITUAL, STRENGTH_UP_AMOUNT, applier=enemy)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    # Ritual skipped this tick; StrengthUp did not.
    assert enemy.get_power_amount(PowerId.STRENGTH) == STRENGTH_UP_AMOUNT

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    # Second tick: both paid, so the gap stays exactly one Amount forever.
    assert enemy.get_power_amount(PowerId.STRENGTH) == 3 * STRENGTH_UP_AMOUNT


# ---------------------------------------------------------------------------
# No decay, and stacking
# ---------------------------------------------------------------------------

def test_amount_never_decays_and_strength_grows_linearly(simple_combat):
    """Nothing in the .cs touches Amount, so the payout is the same every turn."""
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    for turn in range(1, TURNS_TO_CHECK_FOR_DECAY + 1):
        fire_after_turn_end(CombatSide.PLAYER, simple_combat)
        fire_after_turn_end(CombatSide.ENEMY, simple_combat)
        assert enemy.get_power_amount(PowerId.STRENGTH_UP) == STRENGTH_UP_AMOUNT
        assert enemy.get_power_amount(PowerId.STRENGTH) == turn * STRENGTH_UP_AMOUNT

    assert PowerId.STRENGTH_UP in enemy.powers


def test_stacking_is_additive_and_pays_out_at_the_stacked_amount(simple_combat):
    """StackType Counter: a second application adds to Amount."""
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_DEADLY_AMOUNT)

    stacked = STRENGTH_UP_AMOUNT + STRENGTH_UP_DEADLY_AMOUNT
    assert enemy.get_power_amount(PowerId.STRENGTH_UP) == stacked

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)
    assert enemy.get_power_amount(PowerId.STRENGTH) == stacked


@pytest.mark.parametrize("amount", [STRENGTH_UP_AMOUNT, STRENGTH_UP_DEADLY_AMOUNT])
def test_payout_equals_amount_for_both_orb_walker_values(simple_combat, amount):
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, amount)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.get_power_amount(PowerId.STRENGTH) == amount


# ---------------------------------------------------------------------------
# End-to-end through the real turn loop
# ---------------------------------------------------------------------------

def test_gained_strength_reaches_the_damage_pipeline_through_a_real_turn():
    """Drive the actual combat loop, not just the hook, and check it bites.

    Strength is granted at the END of the enemy turn, so the enemy's attack on
    turn 1 is UNBUFFED and its attack on turn 2 is buffed by Amount. That
    ordering is the whole point of the hook choice: a turn-start
    implementation would buff the turn-1 attack too, which is exactly the
    mis-plan the planner would make against a mis-modelled OrbWalker.
    """
    combat, enemy = _combat_with_fixed_attacker()
    combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    hp_before_turn_1 = combat.player.current_hp
    combat.player.block = 0
    combat.end_player_turn()
    turn_1_damage = hp_before_turn_1 - combat.player.current_hp

    assert turn_1_damage == FIXED_ATTACK_DAMAGE  # the first hit is unbuffed
    assert enemy.get_power_amount(PowerId.STRENGTH) == STRENGTH_UP_AMOUNT

    hp_before_turn_2 = combat.player.current_hp
    combat.player.block = 0
    combat.end_player_turn()
    turn_2_damage = hp_before_turn_2 - combat.player.current_hp

    assert enemy.get_power_amount(PowerId.STRENGTH) == 2 * STRENGTH_UP_AMOUNT
    # Same monster, same move, one Amount of Strength apart.
    assert turn_2_damage == FIXED_ATTACK_DAMAGE + STRENGTH_UP_AMOUNT


def test_dead_owner_stops_contributing_strength(simple_combat):
    """A killed enemy leaves the hook listener set, so the payout stops.

    Note the .cs has NO `!Owner.IsDead` guard (its sibling RegenEnemyPower
    does), because the game removes a dead creature's powers for it. This
    pins that the simulator's equivalent removal actually covers the gap.
    """
    enemy = _enemy(simple_combat)
    simple_combat.apply_power_to(enemy, PowerId.STRENGTH_UP, STRENGTH_UP_AMOUNT)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)
    assert enemy.get_power_amount(PowerId.STRENGTH) == STRENGTH_UP_AMOUNT

    simple_combat.kill_creature(enemy)
    strength_at_death = enemy.get_power_amount(PowerId.STRENGTH)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.get_power_amount(PowerId.STRENGTH) == strength_at_death
