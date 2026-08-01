"""Behaviour tests for PowerId.EXPLOSIVE (ActsFromThePast ExplosivePower).

Ground truth is decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/
ExplosivePower.cs, whose entire body is:

    public override PowerType Type => (PowerType)1;                 // Buff
    public override PowerStackType StackType => (PowerStackType)1;  // Counter

    public override async Task BeforeSideTurnEndEarly(
        PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == ((PowerModel)this).Owner.Side
            && ((PowerModel)this).Amount > 1)
        {
            ((PowerModel)this).Flash();
            await PowerCmd.Decrement((PowerModel)(object)this);
        }
    }

So the three things that make it a distinct power rather than a duration
alias, and which these tests pin down:

  * ``Amount > 1`` -- the counter FLOORS at 1 and stays there forever. It
    never hits 0 and never removes itself.
  * ``BeforeSideTurnEndEarly`` -- a full sub-phase earlier than
    ``AfterSideTurnEnd``/``after_turn_end``.
  * ``side == Owner.Side`` and nothing else -- no ``participants.Contains``
    and, unlike RegenPower in the identical hook, no ``IsDead`` guard.

The power also does no damage: the Exploder's 30-damage DeathBlow and
self-kill live in the monster's EXPLODE move (Exploder.cs / create_exploder
in sts2_env/monsters/thebeyond.py), not here.
"""

from __future__ import annotations

import pytest

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.creature import Creature, get_power_class
from sts2_env.core.enums import CombatSide, PowerId, PowerStackType, PowerType
from sts2_env.core.hooks import fire_after_turn_end, fire_before_turn_end
from sts2_env.monsters.intents import attack_intent
from sts2_env.monsters.state_machine import MonsterAI, MoveState
from sts2_env.powers.base import PowerInstance
from sts2_env.powers.monster import ExplosivePower


PROBE_ENEMY_HP = 40
PLAYER_HP = 80
#: Exploder.ExplosiveCountdown in the mod source.
SOURCE_COUNTDOWN = 3


def _noop_move(combat: CombatState) -> None:
    pass


def _noop_monster_ai() -> MonsterAI:
    return MonsterAI(
        {"NOOP": MoveState("NOOP", _noop_move, [attack_intent(1)], follow_up_id="NOOP")},
        "NOOP",
    )


def _combat_with_enemy(amount: int | None = SOURCE_COUNTDOWN) -> tuple[CombatState, Creature]:
    """Combat vs a do-nothing enemy optionally carrying Explosive(amount)."""
    combat = CombatState(
        player_hp=PLAYER_HP,
        player_max_hp=PLAYER_HP,
        deck=create_ironclad_starter_deck(),
        rng_seed=42,
        character_id="Ironclad",
    )
    enemy = Creature(
        max_hp=PROBE_ENEMY_HP,
        current_hp=PROBE_ENEMY_HP,
        side=CombatSide.ENEMY,
        monster_id="EXPLOSIVE_PROBE",
    )
    combat.add_enemy(enemy, _noop_monster_ai())
    combat.start_combat()
    if amount is not None:
        enemy.powers[PowerId.EXPLOSIVE] = ExplosivePower(amount)
    return combat, enemy


class _TurnEndPhaseProbePower(PowerInstance):
    """Records the owner's Explosive amount at three distinct turn-end phases.

    ``fire_before_turn_end`` dispatches very_early -> early -> before_turn_end
    as three separate passes over every listener, so a probe that only reads
    (never writes) sees the pre-early value in ``before_turn_end_very_early``
    and the post-early value in ``before_turn_end`` regardless of the order
    powers happen to sit in the owner's dict.
    """

    def __init__(self) -> None:
        super().__init__(PowerId.ACCURACY, 1)
        self.very_early: int | None = None
        self.before_end: int | None = None
        self.after_end: int | None = None

    def before_turn_end_very_early(self, owner, side, combat) -> None:
        if side == owner.side:
            self.very_early = owner.get_power_amount(PowerId.EXPLOSIVE)

    def before_turn_end(self, owner, side, combat) -> None:
        if side == owner.side:
            self.before_end = owner.get_power_amount(PowerId.EXPLOSIVE)

    def after_turn_end(self, owner, side, combat) -> None:
        if side == owner.side:
            self.after_end = owner.get_power_amount(PowerId.EXPLOSIVE)


# ---------------------------------------------------------------------------
# Registration / metadata
# ---------------------------------------------------------------------------

def test_explosive_is_registered_and_constructible_the_way_reconstruction_calls_it():
    # combat_reconstruct builds enemy powers as cls(amount); a class that only
    # accepts (power_id, amount) silently degrades to a behaviourless
    # PowerInstance, which is exactly the failure this power set exists to fix.
    assert get_power_class(PowerId.EXPLOSIVE) is ExplosivePower
    instance = get_power_class(PowerId.EXPLOSIVE)(5)
    assert isinstance(instance, ExplosivePower)
    assert instance.power_id is PowerId.EXPLOSIVE
    assert instance.amount == 5


def test_explosive_metadata_matches_the_source_enums():
    # C#: Type => (PowerType)1 (Buff), StackType => (PowerStackType)1 (Counter).
    assert ExplosivePower.power_type is PowerType.BUFF
    assert ExplosivePower.stack_type is PowerStackType.COUNTER
    assert ExplosivePower.allow_negative is False


# ---------------------------------------------------------------------------
# Countdown behaviour
# ---------------------------------------------------------------------------

def test_explosive_ticks_down_exactly_one_per_owner_turn_through_real_rounds():
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)

    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 3
    combat.end_player_turn()  # player turn end + full enemy turn
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 2
    combat.end_player_turn()
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 1


def test_explosive_floors_at_one_and_is_never_removed():
    # C# guard is `Amount > 1`, not `> 0`: at 1 the power stops ticking and
    # stays on the creature for the rest of combat. A duration-style port
    # (tick to 0 then remove) would fail here.
    combat, enemy = _combat_with_enemy(2)

    combat.end_player_turn()
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 1

    for _ in range(5):
        combat.end_player_turn()
        assert PowerId.EXPLOSIVE in enemy.powers
        assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 1


def test_explosive_at_one_does_not_tick_at_all():
    combat, enemy = _combat_with_enemy(1)
    power = enemy.powers[PowerId.EXPLOSIVE]

    fire_before_turn_end(CombatSide.ENEMY, combat)

    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 1
    assert enemy.powers[PowerId.EXPLOSIVE] is power


# ---------------------------------------------------------------------------
# Side gating
# ---------------------------------------------------------------------------

def test_explosive_on_an_enemy_ignores_the_player_side_turn_end():
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)

    fire_before_turn_end(CombatSide.PLAYER, combat)
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 3

    fire_before_turn_end(CombatSide.ENEMY, combat)
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 2


def test_explosive_on_the_player_ticks_on_the_player_side_only():
    # The guard is `side == Owner.Side`, so the same power on a player-side
    # owner follows the player's turn, not the enemy's.
    combat, _enemy = _combat_with_enemy(amount=None)
    combat.player.powers[PowerId.EXPLOSIVE] = ExplosivePower(SOURCE_COUNTDOWN)

    fire_before_turn_end(CombatSide.ENEMY, combat)
    assert combat.player.get_power_amount(PowerId.EXPLOSIVE) == 3

    fire_before_turn_end(CombatSide.PLAYER, combat)
    assert combat.player.get_power_amount(PowerId.EXPLOSIVE) == 2


# ---------------------------------------------------------------------------
# Timing: BeforeSideTurnEndEarly, not AfterSideTurnEnd
# ---------------------------------------------------------------------------

def test_explosive_ticks_in_the_before_turn_end_early_phase():
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)
    probe = _TurnEndPhaseProbePower()
    enemy.powers[PowerId.ACCURACY] = probe

    fire_before_turn_end(CombatSide.ENEMY, combat)
    fire_after_turn_end(CombatSide.ENEMY, combat)

    # Still 3 in the very-early pass, already 2 by the later before_turn_end
    # pass => the decrement landed in the `early` pass, which is what
    # BeforeSideTurnEndEarly means. An after_turn_end port would read 3/3/2.
    assert probe.very_early == 3
    assert probe.before_end == 2
    assert probe.after_end == 2


def test_explosive_ticks_before_end_of_turn_damage_can_resolve():
    # PlatedArmor's comment in the source ("we do this in early so that it
    # triggers before end-of-turn damage effects") is the same slot; pin that
    # the tick is already visible to anything running in the ordinary
    # before_turn_end pass.
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)
    seen: list[int] = []

    class _LateReader(PowerInstance):
        def __init__(self) -> None:
            super().__init__(PowerId.ACCURACY, 1)

        def before_turn_end(self, owner, side, combat) -> None:
            if side == owner.side:
                seen.append(owner.get_power_amount(PowerId.EXPLOSIVE))

    enemy.powers[PowerId.ACCURACY] = _LateReader()
    fire_before_turn_end(CombatSide.ENEMY, combat)

    assert seen == [2]


# ---------------------------------------------------------------------------
# No IsDead guard (deliberate difference from RegenPower)
# ---------------------------------------------------------------------------

def test_explosive_still_ticks_for_a_dead_owner():
    # RegenPower.cs guards the identical hook with
    # `participants.Contains(Owner) && !Owner.IsDead`; ExplosivePower.cs has
    # neither guard, so the countdown is not suppressed by the owner's death.
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)
    enemy.current_hp = 0
    assert enemy.is_dead

    fire_before_turn_end(CombatSide.ENEMY, combat)

    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 2


# ---------------------------------------------------------------------------
# The power itself never detonates
# ---------------------------------------------------------------------------

def test_explosive_deals_no_damage_and_kills_nobody_by_itself():
    # The 30-damage DeathBlow + self-kill is Exploder's EXPLODE move, not this
    # power. Reading "Explosive" as "blows up at 0" would double the damage
    # and kill an enemy the real game leaves standing.
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)
    player_hp_before = combat.player.current_hp
    enemy_hp_before = enemy.current_hp

    for _ in range(6):
        combat.end_player_turn()

    assert combat.player.current_hp == player_hp_before
    assert enemy.current_hp == enemy_hp_before
    assert enemy.is_alive
    assert not combat.is_over
    assert enemy.get_power_amount(PowerId.EXPLOSIVE) == 1


def test_explosive_amount_survives_to_the_end_of_a_long_fight():
    combat, enemy = _combat_with_enemy(SOURCE_COUNTDOWN)

    for _ in range(10):
        combat.end_player_turn()

    assert enemy.powers[PowerId.EXPLOSIVE].amount == 1


# ---------------------------------------------------------------------------
# Wire resolution
# ---------------------------------------------------------------------------

@pytest.mark.parametrize(
    "wire_name",
    ["EXPLOSIVE", "EXPLOSIVE_POWER", "ACTSFROMTHEPAST-EXPLOSIVE_POWER"],
)
def test_explosive_resolves_from_the_shapes_the_wire_sends(wire_name: str):
    from sts2_env.bridge.combat_reconstruct import _to_power_id

    assert _to_power_id(wire_name) is PowerId.EXPLOSIVE
