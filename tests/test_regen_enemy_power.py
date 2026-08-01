"""ActsFromThePast ``RegenEnemyPower`` -- heals every turn, forever.

Ground truth: decompiled_mods/ActsFromThePast/ActsFromThePast.Powers/
RegenEnemyPower.cs, whose entire behaviour is one hook::

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext,
            CombatSide side, IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && !Owner.IsDead)
        {
            Flash();
            await CreatureCmd.Heal(Owner, (decimal)Amount, true);
        }
    }

The point of these tests is the two things that make it a distinct power
rather than an alias of ``PowerId.REGEN``:

  * there is no ``Amount--`` anywhere in the class, so the heal is the same
    size on turn 10 as on turn 1 (REGEN shrinks by 1 per tick), and
  * it fires at AfterSideTurnEnd of the OWNER's side -- after the enemy has
    already acted, not at turn start.

Both are asserted against a real combat turn, not just by calling the hook.
"""

from __future__ import annotations

import pytest

# Ensure power registration happens (mirrors tests/conftest.py convention).
import sts2_env.powers  # noqa: F401

from sts2_env.core.combat import CombatState
from sts2_env.core.creature import Creature, get_power_class
from sts2_env.core.enums import CombatSide, PowerId, PowerStackType, PowerType
from sts2_env.core.hooks import fire_after_side_turn_start, fire_after_turn_end
from sts2_env.powers.base import PowerInstance
from sts2_env.powers.turn_effects import RegenEnemyPower


REGEN_AMOUNT = 5
ENEMY_MAX_HP = 60
ENEMY_WOUNDED_HP = 20
TURNS_OBSERVED = 4


def _enemy(combat: CombatState) -> Creature:
    return combat.enemies[0]


def _wound(creature: Creature, hp: int = ENEMY_WOUNDED_HP) -> int:
    creature.max_hp = max(creature.max_hp, ENEMY_MAX_HP)
    creature.current_hp = hp
    return hp


class _HpProbe(PowerInstance):
    """Records the owner's HP at each turn-lifecycle point it can see.

    Used to pin down WHEN the regen fires within a turn: a turn-start regen
    and a turn-end regen produce the same HP after a full round, so the
    ordering has to be observed from inside the round.
    """

    # PowerId.ACCURACY is an arbitrary inert id to hang the probe on (the
    # instance overrides all behaviour); follows the convention in
    # tests/test_power_lifecycle_and_modifier_hooks.py.
    def __init__(self):
        super().__init__(PowerId.ACCURACY, 1)
        self.hp_at_side_turn_start: list[int] = []
        self.hp_at_before_turn_end: list[int] = []

    def after_side_turn_start(self, owner, side, combat) -> None:
        if side == owner.side:
            self.hp_at_side_turn_start.append(owner.current_hp)

    def before_turn_end(self, owner, side, combat) -> None:
        if side == owner.side:
            self.hp_at_before_turn_end.append(owner.current_hp)


# ---------------------------------------------------------------------------
# Registration / metadata
# ---------------------------------------------------------------------------
def test_regen_enemy_is_registered_as_its_own_power_class():
    assert get_power_class(PowerId.REGEN_ENEMY) is RegenEnemyPower
    # It must NOT collapse onto either of the two powers it resembles.
    assert get_power_class(PowerId.REGEN) is not RegenEnemyPower
    assert get_power_class(PowerId.REGENERATE_A4H) is not RegenEnemyPower


def test_regen_enemy_metadata_matches_the_cs_declarations():
    # C#: Type => (PowerType)1, StackType => (PowerStackType)1,
    # ShouldScaleInMultiplayer => true. The game enums are
    # PowerType{None,Buff,Debuff} and PowerStackType{None,Counter,Single}.
    assert RegenEnemyPower.power_type is PowerType.BUFF
    assert RegenEnemyPower.stack_type is PowerStackType.COUNTER
    assert RegenEnemyPower.should_scale_in_multiplayer is True


# ---------------------------------------------------------------------------
# Timing: AfterSideTurnEnd of the owner's own side
# ---------------------------------------------------------------------------
def test_heals_at_the_end_of_the_owners_own_side_turn(simple_combat):
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    fire_after_turn_end(CombatSide.PLAYER, simple_combat)
    assert enemy.current_hp == hp, "must not heal on the other side's turn end"

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)
    assert enemy.current_hp == hp + REGEN_AMOUNT


def test_does_not_heal_at_turn_start(simple_combat):
    """The distinguishing detail vs a turn-START regen such as RITUAL-style
    growth: nothing happens when the owner's side turn begins."""
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    fire_after_side_turn_start(CombatSide.ENEMY, simple_combat)
    fire_after_side_turn_start(CombatSide.PLAYER, simple_combat)

    assert enemy.current_hp == hp


def test_in_a_real_round_the_heal_lands_after_the_enemy_has_acted(simple_combat):
    """Drive a whole round through ``end_player_turn`` and observe ordering.

    The probe sees the enemy's HP at AfterSideTurnStart and at
    BeforeTurnEnd -- both strictly before AfterSideTurnEnd -- so if the heal
    were mis-hooked to turn start, the probe would see the healed value.
    """
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    probe = _HpProbe()
    enemy.powers[PowerId.ACCURACY] = probe
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    simple_combat.end_player_turn()

    assert probe.hp_at_side_turn_start == [hp], "healed too early (turn start)"
    assert probe.hp_at_before_turn_end == [hp], "healed too early (before turn end)"
    assert enemy.current_hp == hp + REGEN_AMOUNT, "did not heal at turn end"


# ---------------------------------------------------------------------------
# No decay -- the whole reason this is not PowerId.REGEN
# ---------------------------------------------------------------------------
def test_amount_never_decays_and_heals_the_same_every_turn(simple_combat):
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    healed_per_turn = []
    for _ in range(TURNS_OBSERVED):
        before = enemy.current_hp
        fire_after_turn_end(CombatSide.ENEMY, simple_combat)
        healed_per_turn.append(enemy.current_hp - before)
        assert enemy.get_power_amount(PowerId.REGEN_ENEMY) == REGEN_AMOUNT

    assert healed_per_turn == [REGEN_AMOUNT] * TURNS_OBSERVED
    assert enemy.current_hp == hp + REGEN_AMOUNT * TURNS_OBSERVED


def test_it_outheals_vanilla_regen_which_is_why_aliasing_would_be_wrong(simple_combat):
    """Side-by-side against ``PowerId.REGEN`` on an identical creature.

    REGEN heals 5, 4, 3 (and its stack shrinks); RegenEnemy heals 5, 5, 5.
    Aliasing REGEN_ENEMY onto REGEN would have the planner simulate an enemy
    healing 12 over three turns when it really heals 15.
    """
    decaying = _enemy(simple_combat)
    steady = Creature(max_hp=ENEMY_MAX_HP, current_hp=ENEMY_MAX_HP, side=CombatSide.ENEMY, monster_id="STEADY")
    simple_combat.add_enemy(steady, simple_combat.enemy_ais[decaying.combat_id])

    decaying_hp = _wound(decaying)
    steady_hp = _wound(steady)
    simple_combat.apply_power_to(decaying, PowerId.REGEN, REGEN_AMOUNT, applier=decaying)
    simple_combat.apply_power_to(steady, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=steady)

    for _ in range(3):
        fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert decaying.current_hp == decaying_hp + (5 + 4 + 3)
    assert decaying.get_power_amount(PowerId.REGEN) == REGEN_AMOUNT - 3
    assert steady.current_hp == steady_hp + (5 + 5 + 5)
    assert steady.get_power_amount(PowerId.REGEN_ENEMY) == REGEN_AMOUNT


def test_power_is_not_removed_after_ticking(simple_combat):
    enemy = _enemy(simple_combat)
    _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    for _ in range(TURNS_OBSERVED):
        fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert PowerId.REGEN_ENEMY in enemy.powers


# ---------------------------------------------------------------------------
# Stacking (StackType.Counter)
# ---------------------------------------------------------------------------
def test_stacks_add_and_heal_the_summed_amount(simple_combat):
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, 2, applier=enemy)

    assert enemy.get_power_amount(PowerId.REGEN_ENEMY) == REGEN_AMOUNT + 2

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.current_hp == hp + REGEN_AMOUNT + 2


# ---------------------------------------------------------------------------
# Guards: !Owner.IsDead, and the max-HP cap inside CreatureCmd.Heal
# ---------------------------------------------------------------------------
def test_a_dead_owner_does_not_heal_or_revive(simple_combat):
    enemy = _enemy(simple_combat)
    _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)
    enemy.current_hp = 0

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.current_hp == 0
    assert enemy.is_dead


def test_an_escaped_owner_does_not_keep_healing(simple_combat):
    enemy = _enemy(simple_combat)
    hp = _wound(enemy)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)
    enemy.escaped = True

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.current_hp == hp


def test_healing_is_capped_at_max_hp(simple_combat):
    enemy = _enemy(simple_combat)
    _wound(enemy, ENEMY_MAX_HP - 2)
    simple_combat.apply_power_to(enemy, PowerId.REGEN_ENEMY, REGEN_AMOUNT, applier=enemy)

    fire_after_turn_end(CombatSide.ENEMY, simple_combat)

    assert enemy.current_hp == enemy.max_hp


# ---------------------------------------------------------------------------
# The wire name the live game sends must land on this power
# ---------------------------------------------------------------------------
@pytest.mark.parametrize(
    "wire_name",
    [
        "REGEN_ENEMY_POWER",
        "ACTSFROMTHEPAST-REGEN_ENEMY_POWER",
        "RegenEnemyPower",
    ],
)
def test_the_wire_power_id_resolves_to_regen_enemy(wire_name):
    from sts2_env.bridge.combat_reconstruct import _to_power_id

    assert _to_power_id(wire_name) is PowerId.REGEN_ENEMY
