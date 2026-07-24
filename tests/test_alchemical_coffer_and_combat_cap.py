"""Regression tests for two training-run discoveries:

1. AlchemicalCoffer obtained at a rewards screen crashed with
   ``TypeError: unhashable type: 'PlayerState'`` -- the run-level PlayerState
   keeps a STALE combat_state reference after combat ends, sending
   after_obtained down the in-combat path, and PlayerState (a dataclass)
   is unhashable as a dict key in combat_player_state_for.

2. Combats exceeding 30 turns in RichSTS2RunEnv are scored as deaths
   (user-requested: stalling loses; run-level truncation stays only as a
   backstop for non-combat loops).
"""

import sts2_env.events  # noqa: F401

from sts2_env.core.combat import CombatState
from sts2_env.gym_env.rich_run_env import (
    DEFAULT_RICH_MAX_COMBAT_TURNS,
    RichSTS2RunEnv,
)
from sts2_env.run.run_state import RunState


def _finished_combat_for(player_state):
    combat = CombatState(
        player_hp=player_state.current_hp,
        player_max_hp=player_state.max_hp,
        deck=list(player_state.deck),
        rng_seed=5,
        character_id=player_state.character_id,
        player_state=player_state,
    )
    combat.is_over = True
    return combat


def test_alchemical_coffer_with_stale_combat_state_takes_run_path():
    run_state = RunState(seed=99, character_id="Necrobinder")
    run_state.initialize_run()
    player = run_state.player
    # Simulate the post-combat rewards screen: combat is over but the
    # PlayerState still points at it.
    player.combat_state = _finished_combat_for(player)

    slots_before = player.max_potion_slots
    assert player.obtain_relic("AlchemicalCoffer")

    assert player.max_potion_slots == slots_before + 4
    # Potions were granted into the new slots (roll can vary, but at least
    # one new potion should exist).
    assert any(p is not None for p in player.potions)


def test_combat_player_state_for_accepts_run_player_state():
    run_state = RunState(seed=100, character_id="Necrobinder")
    run_state.initialize_run()
    player = run_state.player
    combat = CombatState(
        player_hp=player.current_hp,
        player_max_hp=player.max_hp,
        deck=list(player.deck),
        rng_seed=6,
        character_id=player.character_id,
        player_state=player,
    )
    # Passing the unhashable PlayerState must resolve (not raise).
    state = combat.combat_player_state_for(player)
    assert state is not None
    assert state.creature is combat.primary_player


def test_rich_run_env_defaults_to_30_turn_combat_cap():
    env = RichSTS2RunEnv(character_id="Necrobinder", ascension_level=0, max_act_count=1)
    assert DEFAULT_RICH_MAX_COMBAT_TURNS == 30
    assert env.max_combat_turns == 30


def test_conqueror_power_survives_sourceless_enemy_damage():
    """Regression: ConquerorPower.modify_damage_multiplicative crashed with
    AttributeError when an ENEMY attacked its owner (active_card_source is
    None outside card plays); is_sovereign_blade must treat None as False."""
    import sts2_env.cards  # ensure registries loaded  # noqa: F401
    from sts2_env.cards.status import is_sovereign_blade
    from sts2_env.core.enums import PowerId, ValueProp
    from sts2_env.core.damage import calculate_damage
    from sts2_env.core.rng import Rng
    from sts2_env.monsters.act1 import create_nibbit

    assert is_sovereign_blade(None) is False

    combat = CombatState(
        player_hp=70, player_max_hp=70, deck=[], rng_seed=11,
        character_id="Necrobinder",
    )
    enemy, ai = create_nibbit(Rng(11))
    combat.add_enemy(enemy, ai)
    combat.apply_power_to(combat.primary_player, PowerId.CONQUEROR, 2)
    # Enemy attack: no active card source anywhere -- must not raise, and the
    # 2x Sovereign Blade multiplier must NOT apply.
    dmg = calculate_damage(10, enemy, combat.primary_player, ValueProp.MOVE, combat)
    assert dmg == 10
