"""Transient fades via PowerId.FADING, not a hidden closure counter.

Why this test exists
--------------------
Transient's countdown used to be a plain ``state["turns_left"]`` closure
variable decremented inside its attack move. That was wrong in two ways beyond
tidiness:

* INVISIBLE. Enemy powers are one-hot in the observation; a closure variable is
  not. "How many turns until this fades" is the entire tactical question a
  Transient poses -- kill it, or outlive it -- and the agent could not see the
  answer. It had to learn a 999-HP enemy sometimes vanishes, with no clue when.
* WRONG PLACE. The C# ticks at ``BeforeSideTurnEndEarly`` on the owner's own
  side (FadingPower.cs). Ticking inside the attack move means a Transient
  prevented from attacking never fades, and the kill lands earlier in the turn
  than the game's.

The live wire also sends ``ACTSFROMTHEPAST-FADING_POWER`` on every Transient,
so the simulator modelling the same countdown as private state guaranteed a
sim/live mismatch on an enemy whose whole threat is a timer.
"""

from __future__ import annotations

import pytest

from sts2_env.core.enums import PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.thebeyond import (
    TRANSIENT_BASE_FADING_TURNS,
    TRANSIENT_TOUGH_FADING_TURNS,
    create_transient,
)


@pytest.mark.parametrize("ascension,expected", [
    (0, TRANSIENT_BASE_FADING_TURNS),
    (7, TRANSIENT_BASE_FADING_TURNS),
    (8, TRANSIENT_TOUGH_FADING_TURNS),
    (10, TRANSIENT_TOUGH_FADING_TURNS),
])
def test_transient_spawns_with_fading(ascension: int, expected: int):
    """Transient.cs:58 -- AfterAddedToRoom applies A8 ? 6 : 5."""
    creature, _ai = create_transient(Rng(1), ascension_level=ascension)
    power = creature.powers.get(PowerId.FADING)
    assert power is not None, (
        "Transient has no FADING power; the countdown is invisible to the "
        "agent and cannot match the wire"
    )
    assert power.amount == expected


def test_the_countdown_is_not_a_hidden_closure():
    """Regression guard on the specific shape that was replaced."""
    import inspect

    from sts2_env.monsters import thebeyond

    src = inspect.getsource(thebeyond.create_transient)
    assert "turns_left" not in src, (
        "the closure countdown is back; it is invisible in the observation "
        "and ticks in the attack move rather than at turn end"
    )
    assert "PowerId.FADING" in src


def test_fading_is_in_the_observation_vocabulary():
    """A power the observation cannot encode is still invisible."""
    from sts2_env.gym_env.rich_observation import POWER_IDS

    assert PowerId.FADING in POWER_IDS


def _combat_with_transient(ascension: int = 0):
    """A real CombatState containing a real Transient.

    Built through ``CombatState``/``add_enemy`` rather than a hand-rolled stub,
    following tests/test_mod_power_explosive.py. A stub would only prove that
    my stub calls the power; the point is that the ENGINE's turn-end dispatch
    reaches it.
    """
    from sts2_env.cards.ironclad import create_ironclad_starter_deck
    from sts2_env.core.combat import CombatState

    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=42, character_id="Ironclad",
    )
    creature, ai = create_transient(Rng(1), ascension_level=ascension)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    return combat, creature


class TestFadingTicksAndKills:
    """Behaviour through the engine's real dispatch."""

    def test_amount_counts_down_on_the_owners_turn_end(self):
        from sts2_env.core.enums import CombatSide
        from sts2_env.core.hooks import fire_before_turn_end

        combat, creature = _combat_with_transient()
        start = creature.powers[PowerId.FADING].amount

        fire_before_turn_end(CombatSide.ENEMY, combat)

        assert creature.powers[PowerId.FADING].amount == start - 1, (
            "FADING did not tick on the owner's own turn end"
        )
        assert creature.is_alive

    def test_it_does_not_tick_on_the_players_turn_end(self):
        """``side != Owner.Side`` returns early in the C#."""
        from sts2_env.core.enums import CombatSide
        from sts2_env.core.hooks import fire_before_turn_end

        combat, creature = _combat_with_transient()
        start = creature.powers[PowerId.FADING].amount

        fire_before_turn_end(CombatSide.PLAYER, combat)

        assert creature.powers[PowerId.FADING].amount == start

    def test_the_owner_dies_on_the_tick_that_would_run_it_out(self):
        from sts2_env.core.enums import CombatSide
        from sts2_env.core.hooks import fire_before_turn_end

        combat, creature = _combat_with_transient()
        turns = creature.powers[PowerId.FADING].amount

        for i in range(turns):
            assert creature.is_alive, f"died early, after {i} of its own turns"
            fire_before_turn_end(CombatSide.ENEMY, combat)

        assert not creature.is_alive, (
            f"Transient survived {turns} of its own turn ends; FADING never "
            f"killed it"
        )

    def test_it_never_reaches_zero_alive(self):
        """The C# kills at Amount <= 1 rather than ticking to 0."""
        from sts2_env.core.enums import CombatSide
        from sts2_env.core.hooks import fire_before_turn_end

        combat, creature = _combat_with_transient()
        for _ in range(creature.powers[PowerId.FADING].amount):
            if not creature.is_alive:
                break
            fire_before_turn_end(CombatSide.ENEMY, combat)
            if creature.is_alive:
                assert creature.powers[PowerId.FADING].amount >= 1
