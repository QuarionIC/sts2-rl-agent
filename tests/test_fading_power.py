"""FadingPower (ActsFromThePast): the owner dies at the end of its own Nth turn.

Ground truth:
  decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/FadingPower.cs

    public override PowerType Type => (PowerType)2;                  // Debuff
    public override PowerStackType StackType => (PowerStackType)1;   // Counter

    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != ((PowerModel)this).Owner.Side) { return; }
        if (((PowerModel)this).Amount <= 1)
        {
            if (((PowerModel)this).Owner.IsDead) { return; }
            ((PowerModel)this).Flash();
            ... NFireSmokePuffVfx ...
            await Cmd.Wait(0.1f, false);
            await CreatureCmd.Kill(((PowerModel)this).Owner, false);
        }
        else
        {
            ((PowerModel)this).Flash();
            await PowerCmd.Decrement((PowerModel)(object)this);
        }
    }

Everything below asserts a timing/branch decision from that method rather than
that the class can be constructed. The four that make Fading its own power
instead of a duration alias:

  * ``BeforeSideTurnEndEarly``, not ``AfterSideTurnEnd`` -- it resolves in the
    middle sub-phase of the before-turn-end dispatch, so the owner is already
    dead before the end-of-turn tick-down pass runs at all.
  * ``side != Owner.Side`` returns -- N Fading is N of the OWNER's turns.
  * ``PowerCmd.Decrement`` is a bare ``ModifyAmount(-1)``; unlike
    ``PowerCmd.TickDownDuration`` it never consults ``SkipNextDurationTick``.
  * At ``Amount <= 1`` the branch is kill, not decrement -- the counter is left
    sitting at 1 and never reaches 0 on a living creature.
"""

from __future__ import annotations

import pytest

from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.creature import Creature, get_power_class
from sts2_env.core.enums import CombatSide, PowerId, PowerStackType, PowerType
from sts2_env.core.hooks import fire_after_turn_end, fire_before_turn_end
from sts2_env.monsters.intents import attack_intent
from sts2_env.monsters.state_machine import MonsterAI, MoveState
from sts2_env.powers.base import PowerInstance
from sts2_env.powers.monster import FadingPower


#: Transient.cs ``AfterAddedToRoom``: ``PowerCmd.Apply<FadingPower>(...,
#: AscensionHelper.GetValueIfAscension((AscensionLevel)8, 6, 5), ...)``.
TRANSIENT_FADING_AMOUNT = 5
TRANSIENT_FADING_AMOUNT_ASCENSION_8 = 6

#: Transient.cs ``MinInitialHp``/``MaxInitialHp`` -- HP is irrelevant to the
#: fight, the timer is what kills it, so the dummy uses the same value.
FADING_ENEMY_HP = 999

#: An inert power slot to hang a hook-ordering probe on (same trick the
#: existing power-lifecycle tests use for their block probe).
PROBE_POWER_ID = PowerId.JUGGERNAUT

NOOP_MOVE_ID = "NOOP"
NOOP_INTENT_DAMAGE = 1


def _noop_move(combat: CombatState) -> None:
    """The enemy does nothing on its turn -- only the Fading timer matters."""


def _noop_ai() -> MonsterAI:
    return MonsterAI(
        {
            NOOP_MOVE_ID: MoveState(
                NOOP_MOVE_ID,
                _noop_move,
                [attack_intent(NOOP_INTENT_DAMAGE)],
                follow_up_id=NOOP_MOVE_ID,
            )
        },
        NOOP_MOVE_ID,
    )


def _make_combat() -> tuple[CombatState, Creature]:
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=42,
    )
    enemy = Creature(
        max_hp=FADING_ENEMY_HP,
        current_hp=FADING_ENEMY_HP,
        side=CombatSide.ENEMY,
        monster_id="FADING_DUMMY",
    )
    combat.add_enemy(enemy, _noop_ai())
    combat.start_combat()
    return combat, enemy


@pytest.fixture
def fading_combat() -> tuple[CombatState, Creature]:
    return _make_combat()


def _end_enemy_turn(combat: CombatState) -> None:
    """The two hook phases a real enemy turn end fires, in order."""
    fire_before_turn_end(CombatSide.ENEMY, combat)
    fire_after_turn_end(CombatSide.ENEMY, combat)


def _end_player_turn_hooks(combat: CombatState) -> None:
    fire_before_turn_end(CombatSide.PLAYER, combat)
    fire_after_turn_end(CombatSide.PLAYER, combat)


class TestDeclaration:
    def test_registered_under_its_own_power_id(self):
        assert get_power_class(PowerId.FADING) is FadingPower

    def test_is_a_counter_debuff(self):
        # (PowerType)2 == Debuff, (PowerStackType)1 == Counter in
        # MegaCrit.Sts2.Core.Entities.Powers.
        assert FadingPower.power_type is PowerType.DEBUFF
        assert FadingPower.stack_type is PowerStackType.COUNTER

    def test_being_a_debuff_means_artifact_blocks_it(self, fading_combat):
        combat, enemy = fading_combat
        enemy.apply_power(PowerId.ARTIFACT, 1)

        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        assert PowerId.FADING not in enemy.powers
        assert PowerId.ARTIFACT not in enemy.powers  # the charge was spent

    def test_the_counter_is_not_scaled_by_player_count(self):
        # FadingPower.cs does not override ShouldScaleInMultiplayer, and
        # PowerModel's default is false -- a 5-turn fuse stays 5 turns.
        assert FadingPower.should_scale_in_multiplayer is False


class TestCountdownTiming:
    def test_ticks_at_the_end_of_the_owners_own_turn(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        _end_enemy_turn(combat)

        assert enemy.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT - 1
        assert enemy.is_alive

    def test_the_players_turn_end_does_not_tick_it(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        _end_player_turn_hooks(combat)

        # ``if (side != Owner.Side) return;`` -- an enemy's Fading is deaf to
        # the player's turn ending, so N Fading is N ENEMY turns, not N rounds.
        assert enemy.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT
        assert enemy.is_alive

    def test_a_full_round_advances_the_counter_exactly_once(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        _end_player_turn_hooks(combat)
        _end_enemy_turn(combat)

        assert enemy.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT - 1

    def test_owner_survives_n_minus_one_own_turns_then_dies_on_the_nth(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        for turn in range(1, TRANSIENT_FADING_AMOUNT):
            _end_enemy_turn(combat)
            assert enemy.is_alive, f"died early on own turn {turn}"
            assert enemy.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT - turn

        fire_before_turn_end(CombatSide.ENEMY, combat)

        assert enemy.is_dead
        assert enemy.current_hp == 0

    def test_ascension_eight_amount_buys_exactly_one_more_turn(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT_ASCENSION_8)

        for _ in range(TRANSIENT_FADING_AMOUNT):
            _end_enemy_turn(combat)

        # Still standing on the turn the base-ascension Transient dies on.
        assert enemy.is_alive
        assert enemy.get_power_amount(PowerId.FADING) == 1

        fire_before_turn_end(CombatSide.ENEMY, combat)

        assert enemy.is_dead


class TestFinalTurnBranch:
    def test_the_last_turn_kills_instead_of_decrementing(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 1)
        power = enemy.powers[PowerId.FADING]

        fire_before_turn_end(CombatSide.ENEMY, combat)

        # ``if (Amount <= 1)`` takes the kill branch -- Decrement is never
        # reached, so the counter is still sitting at 1.
        assert power.amount == 1
        assert enemy.is_dead
        # ...and the instance is gone only because the owner died, via the
        # normal death cleanup, not because it ticked itself to zero.
        assert PowerId.FADING not in enemy.powers

    def test_death_by_fading_ends_the_fight(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 1)

        fire_before_turn_end(CombatSide.ENEMY, combat)

        assert combat.is_over
        assert combat.player_won

    def test_an_already_dead_owner_is_not_killed_again(self, fading_combat, monkeypatch):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 1)
        power = enemy.powers[PowerId.FADING]
        enemy.current_hp = 0  # IsDead == CurrentHp <= 0

        kills: list[Creature] = []
        monkeypatch.setattr(combat, "kill_creature", lambda creature: kills.append(creature))

        fire_before_turn_end(CombatSide.ENEMY, combat)

        # ``if (Owner.IsDead) return;`` -- no second kill, and the early return
        # is inside the Amount <= 1 branch so nothing decrements either.
        assert kills == []
        assert power.amount == 1


class TestHookPhase:
    def test_resolves_during_before_turn_end_not_after_turn_end(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 1)

        fire_before_turn_end(CombatSide.ENEMY, combat)

        # AfterSideTurnEnd has not run at all yet. A power that killed on
        # AfterSideTurnEnd would still have a living owner here.
        assert enemy.is_dead

    def test_fires_in_the_early_phase_between_very_early_and_late(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 1)
        seen: dict[str, bool] = {}

        class _PhaseProbe(PowerInstance):
            def __init__(self) -> None:
                super().__init__(PROBE_POWER_ID, 1)

            def before_turn_end_very_early(self, owner, side, combat):
                seen["very_early"] = enemy.is_dead

            def before_turn_end(self, owner, side, combat):
                seen["late"] = enemy.is_dead

        combat.player.powers[PROBE_POWER_ID] = _PhaseProbe()

        fire_before_turn_end(CombatSide.ENEMY, combat)

        # BeforeSideTurnEndEarly sits strictly between the very-early and the
        # plain before-turn-end sub-phases.
        assert seen == {"very_early": False, "late": True}


class TestDecrementSemantics:
    def test_skip_next_tick_does_not_stall_the_countdown(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)
        power = enemy.powers[PowerId.FADING]
        power.skip_next_tick = True

        fire_before_turn_end(CombatSide.ENEMY, combat)

        # PowerCmd.Decrement, not PowerCmd.TickDownDuration: Vulnerable/Weak
        # would have burned the flag and stayed put. Fading does not look at it.
        assert power.amount == TRANSIENT_FADING_AMOUNT - 1
        assert power.skip_next_tick is True

    def test_stacks_additively_like_a_counter(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, TRANSIENT_FADING_AMOUNT)
        combat.apply_power_to(enemy, PowerId.FADING, 2)

        assert enemy.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT + 2

        for _ in range(TRANSIENT_FADING_AMOUNT + 1):
            _end_enemy_turn(combat)

        assert enemy.is_alive
        assert enemy.get_power_amount(PowerId.FADING) == 1

    def test_a_second_fading_owner_is_untouched_by_the_first_ones_countdown(self):
        combat, first = _make_combat()
        second = Creature(
            max_hp=FADING_ENEMY_HP,
            current_hp=FADING_ENEMY_HP,
            side=CombatSide.ENEMY,
            monster_id="FADING_DUMMY_2",
        )
        combat.add_enemy(second, _noop_ai())
        combat.apply_power_to(first, PowerId.FADING, 1)
        combat.apply_power_to(second, PowerId.FADING, TRANSIENT_FADING_AMOUNT)

        fire_before_turn_end(CombatSide.ENEMY, combat)

        assert first.is_dead
        assert second.is_alive
        assert second.get_power_amount(PowerId.FADING) == TRANSIENT_FADING_AMOUNT - 1
        assert not combat.is_over


class TestThroughTheRealTurnLoop:
    def test_end_player_turn_loop_kills_the_owner_and_ends_the_fight(self, fading_combat):
        combat, enemy = fading_combat
        combat.apply_power_to(enemy, PowerId.FADING, 2)

        combat.end_player_turn()

        assert enemy.is_alive
        assert enemy.get_power_amount(PowerId.FADING) == 1

        combat.end_player_turn()

        assert enemy.is_dead
        assert combat.is_over
        assert combat.player_won
