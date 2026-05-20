"""Defect factory upgrade parity tests backed by decompiled card models."""

import sts2_env.powers  # noqa: F401

from sts2_env.cards.defect import (
    GENETIC_ALGORITHM_BLOCK,
    GENETIC_ALGORITHM_BLOCK_KEY,
    GENETIC_ALGORITHM_INCREASE_KEY,
    GENETIC_ALGORITHM_UPGRADED_INCREASE,
    HAILSTORM_UPGRADED_POWER,
    MACHINE_LEARNING_CARDS,
    OVERCLOCK_UPGRADED_CARDS,
    SKIM_UPGRADED_CARDS,
    STORM_UPGRADED_POWER,
    THUNDER_UPGRADED_POWER,
    create_defect_starter_deck,
    make_adaptive_strike,
    make_all_for_one,
    make_biased_cognition,
    make_boost_away,
    make_boot_sequence,
    make_bulk_up,
    make_chaos,
    make_chill,
    make_coolant,
    make_creative_ai,
    make_defragment,
    make_double_energy,
    make_energy_surge,
    make_feral,
    make_focused_strike,
    make_ftl,
    make_fusion,
    make_genetic_algorithm,
    make_glacier,
    make_glasswork,
    make_go_for_the_eyes,
    make_gunk_up,
    make_hailstorm,
    make_hotfix,
    make_leap,
    make_lightning_rod,
    make_machine_learning,
    make_momentum_strike,
    make_overclock,
    make_skim,
    make_storm,
    make_sweeping_beam,
    make_thunder,
    make_turbo,
    make_uproar,
)
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, OrbType, PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.act1_weak import create_shrinker_beetle, create_twig_slime_s
from sts2_env.run.run_state import PlayerState


TEST_PLAYER_HP = 75
TEST_RNG_SEED = 42
EXTRA_ENEMY_RNG_SEED = 43
DEFECT_CHARACTER_ID = "Defect"
HAND_CARD_INDEX = 0
FIRST_ENEMY_INDEX = 0
ZERO_COST = 0
ONE_ENERGY = 1
TWO_ENERGY = 2
BOOT_SEQUENCE_UPGRADED_BLOCK = 13
BOOST_AWAY_UPGRADED_BLOCK = 9
BULK_UP_STARTING_ORB_SLOTS = 3
BULK_UP_EXPECTED_ORB_SLOTS = 2
BULK_UP_UPGRADED_POWER = 3
CHAOS_UPGRADED_REPEAT = 2
ADAPTIVE_STRIKE_UPGRADED_DAMAGE = 23
ALL_FOR_ONE_UPGRADED_DAMAGE = 14
BIASED_COGNITION_UPGRADED_FOCUS = 5
BIASED_COGNITION_POWER = 1
FOCUSED_STRIKE_UPGRADED_DAMAGE = 11
FOCUSED_STRIKE_UPGRADED_POWER = 2
FTL_UPGRADED_DAMAGE = 6
FTL_UPGRADED_PLAY_MAX = 4
FTL_DRAW_COUNT = 1
ENERGY_SURGE_UPGRADED_ENERGY = 3
FERAL_UPGRADED_COST = 1
FERAL_POWER = 1
FUSION_UPGRADED_COST = 1
GLACIER_UPGRADED_BLOCK = 9
GLACIER_FROST_ORBS = 2
GLASSWORK_UPGRADED_BLOCK = 8
COOLANT_UPGRADED_POWER = 3
CREATIVE_AI_UPGRADED_COST = 2
CREATIVE_AI_POWER = 1
DEFRAGMENT_UPGRADED_FOCUS = 2
GO_FOR_THE_EYES_UPGRADED_DAMAGE = 4
GO_FOR_THE_EYES_UPGRADED_WEAK = 2
GUNK_UP_UPGRADED_DAMAGE = 5
GUNK_UP_HITS = 3
HOTFIX_UPGRADED_FOCUS = 3
LEAP_UPGRADED_BLOCK = 12
LIGHTNING_ROD_UPGRADED_BLOCK = 7
LIGHTNING_ROD_POWER = 2
MOMENTUM_STRIKE_UPGRADED_DAMAGE = 13
MOMENTUM_STRIKE_DAMAGE = 10
SWEEPING_BEAM_UPGRADED_DAMAGE = 9
SWEEPING_BEAM_DRAW_COUNT = 1
TURBO_UPGRADED_ENERGY = 3
UPROAR_UPGRADED_DAMAGE = 7
UPROAR_HITS = 2
ALLY_PLAYER_ID = 2
ALLY_PLAYER_HP = 70


def _make_combat(monster_factory=create_shrinker_beetle, *, extra_enemies: int = 0) -> CombatState:
    combat = CombatState(
        player_hp=TEST_PLAYER_HP,
        player_max_hp=TEST_PLAYER_HP,
        deck=create_defect_starter_deck(),
        rng_seed=TEST_RNG_SEED,
        character_id=DEFECT_CHARACTER_ID,
    )
    creature, ai = monster_factory(Rng(TEST_RNG_SEED))
    combat.add_enemy(creature, ai)
    for index in range(extra_enemies):
        extra_rng = Rng(EXTRA_ENEMY_RNG_SEED + index)
        extra_creature, extra_ai = create_shrinker_beetle(extra_rng)
        combat.add_enemy(extra_creature, extra_ai)
    combat.start_combat()
    return combat


def test_boost_away_factory_upgrade_increases_block_and_keeps_dazed_discard():
    combat = _make_combat()
    combat.hand = [make_boost_away(upgraded=True)]
    combat.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX)

    dazed = [card for card in combat.discard_pile if card.card_id == CardId.DAZED]
    assert combat.player.block == BOOST_AWAY_UPGRADED_BLOCK
    assert len(dazed) == 1
    assert dazed[0].owner is combat.player


def test_boot_sequence_factory_upgrade_increases_block_and_keeps_keywords():
    combat = _make_combat()
    card = make_boot_sequence(upgraded=True)
    combat.hand = [card]
    combat.energy = ZERO_COST

    assert card.upgraded is True
    assert card.is_innate is True
    assert card.exhausts is True
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == BOOT_SEQUENCE_UPGRADED_BLOCK
    assert card in combat.exhaust_pile


def test_bulk_up_factory_upgrade_increases_strength_and_dexterity_only():
    combat = _make_combat()
    combat.orb_queue.capacity = BULK_UP_STARTING_ORB_SLOTS
    combat.hand = [make_bulk_up(upgraded=True)]
    combat.energy = TWO_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.orb_queue.capacity == BULK_UP_EXPECTED_ORB_SLOTS
    assert combat.player.get_power_amount(PowerId.STRENGTH) == BULK_UP_UPGRADED_POWER
    assert combat.player.get_power_amount(PowerId.DEXTERITY) == BULK_UP_UPGRADED_POWER


def test_chaos_factory_upgrade_channels_two_random_orbs():
    combat = _make_combat()
    combat.hand = [make_chaos(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert len(combat.orb_queue.orbs) == CHAOS_UPGRADED_REPEAT
    assert {orb.orb_type for orb in combat.orb_queue.orbs}.issubset(
        {OrbType.LIGHTNING, OrbType.FROST, OrbType.DARK, OrbType.PLASMA, OrbType.GLASS}
    )


def test_chill_factory_upgrade_channels_frost_without_exhausting():
    combat = _make_combat(extra_enemies=1)
    card = make_chill(upgraded=True)
    combat.hand = [card]
    combat.energy = ZERO_COST

    assert card.upgraded is True
    assert card.exhausts is False
    assert combat.play_card(HAND_CARD_INDEX)

    assert [orb.orb_type for orb in combat.orb_queue.orbs] == [OrbType.FROST, OrbType.FROST]
    assert card in combat.discard_pile


def test_adaptive_strike_factory_upgrade_uses_upgraded_damage_and_zero_cost_copy():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    card = make_adaptive_strike(upgraded=True)
    combat.hand = [card]
    combat.energy = TWO_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    copies = [
        discarded
        for discarded in combat.discard_pile
        if discarded.card_id == CardId.ADAPTIVE_STRIKE and discarded is not card
    ]
    assert enemy.current_hp == starting_hp - ADAPTIVE_STRIKE_UPGRADED_DAMAGE
    assert len(copies) == 1
    assert copies[0].cost == ZERO_COST


def test_all_for_one_factory_upgrade_uses_upgraded_damage_and_returns_zero_cost_cards():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    returned = make_chill()
    retained = make_boot_sequence()
    retained.cost = ONE_ENERGY
    for card in [returned, retained]:
        card.owner = combat.player
    combat.hand = [make_all_for_one(upgraded=True)]
    combat.discard_pile = [returned, retained]
    combat.energy = TWO_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == starting_hp - ALL_FOR_ONE_UPGRADED_DAMAGE
    assert returned in combat.hand
    assert retained in combat.discard_pile


def test_biased_cognition_factory_upgrade_increases_focus_only():
    combat = _make_combat()
    combat.hand = [make_biased_cognition(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.FOCUS) == BIASED_COGNITION_UPGRADED_FOCUS
    assert combat.player.get_power_amount(PowerId.BIASED_COGNITION) == BIASED_COGNITION_POWER


def test_focused_strike_factory_upgrade_increases_damage_and_focus_power():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    combat.hand = [make_focused_strike(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == starting_hp - FOCUSED_STRIKE_UPGRADED_DAMAGE
    assert (
        combat.player.get_power_amount(PowerId.FOCUSED_STRIKE)
        == FOCUSED_STRIKE_UPGRADED_POWER
    )


def test_double_energy_factory_upgrade_costs_zero_and_keeps_exhaust():
    combat = _make_combat()
    card = make_double_energy(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert card.cost == ZERO_COST
    assert card.exhausts is True
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.energy == TWO_ENERGY
    assert card in combat.exhaust_pile


def test_energy_surge_factory_upgrade_gives_three_energy_to_player_ally():
    combat = _make_combat()
    ally_state = PlayerState(
        player_id=ALLY_PLAYER_ID,
        character_id=DEFECT_CHARACTER_ID,
        max_hp=ALLY_PLAYER_HP,
        current_hp=ALLY_PLAYER_HP,
    )
    ally = combat.add_ally_player(ally_state)
    ally_combat_state = combat.combat_player_state_for(ally)
    assert ally_combat_state is not None
    combat.hand = [make_energy_surge(upgraded=True)]
    combat.energy = ONE_ENERGY
    ally_combat_state.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.energy == ZERO_COST
    assert ally_combat_state.energy == ENERGY_SURGE_UPGRADED_ENERGY


def test_feral_factory_upgrade_costs_one_and_keeps_power_amount():
    combat = _make_combat()
    card = make_feral(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert card.cost == FERAL_UPGRADED_COST
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.FERAL) == FERAL_POWER


def test_ftl_factory_upgrade_increases_damage_and_four_play_draw_window():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    drawn = make_boot_sequence()
    combat.hand = [make_ftl(upgraded=True)]
    combat.draw_pile = [drawn]
    combat.energy = ZERO_COST
    combat.card_plays_this_turn = [make_chill(), make_chill(), make_chill()]

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == starting_hp - FTL_UPGRADED_DAMAGE
    assert combat.hand == [drawn]
    assert len(combat.hand) == FTL_DRAW_COUNT


def test_fusion_factory_upgrade_costs_one_and_channels_plasma():
    combat = _make_combat()
    card = make_fusion(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert card.cost == FUSION_UPGRADED_COST
    assert combat.play_card(HAND_CARD_INDEX)

    assert [orb.orb_type for orb in combat.orb_queue.orbs] == [OrbType.PLASMA]


def test_glacier_factory_upgrade_increases_block_and_keeps_two_frost_orbs():
    combat = _make_combat()
    combat.hand = [make_glacier(upgraded=True)]
    combat.energy = TWO_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == GLACIER_UPGRADED_BLOCK
    assert [orb.orb_type for orb in combat.orb_queue.orbs] == (
        [OrbType.FROST] * GLACIER_FROST_ORBS
    )


def test_glasswork_factory_upgrade_increases_block_and_keeps_glass_orb():
    combat = _make_combat()
    combat.hand = [make_glasswork(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == GLASSWORK_UPGRADED_BLOCK
    assert [orb.orb_type for orb in combat.orb_queue.orbs] == [OrbType.GLASS]


def test_coolant_factory_upgrade_increases_power_amount():
    combat = _make_combat()
    combat.hand = [make_coolant(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.COOLANT) == COOLANT_UPGRADED_POWER


def test_creative_ai_factory_upgrade_costs_two_and_keeps_power_amount():
    combat = _make_combat()
    card = make_creative_ai(upgraded=True)
    combat.hand = [card]
    combat.energy = TWO_ENERGY

    assert card.cost == CREATIVE_AI_UPGRADED_COST
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.CREATIVE_AI) == CREATIVE_AI_POWER


def test_defragment_factory_upgrade_increases_focus_amount():
    combat = _make_combat()
    combat.hand = [make_defragment(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.FOCUS) == DEFRAGMENT_UPGRADED_FOCUS


def test_genetic_algorithm_factory_upgrade_grows_by_four():
    combat = _make_combat()
    card = make_genetic_algorithm(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert card.upgraded is True
    assert card.base_block == GENETIC_ALGORITHM_BLOCK
    assert card.effect_vars[GENETIC_ALGORITHM_BLOCK_KEY] == GENETIC_ALGORITHM_BLOCK
    assert card.effect_vars[GENETIC_ALGORITHM_INCREASE_KEY] == GENETIC_ALGORITHM_UPGRADED_INCREASE
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == GENETIC_ALGORITHM_BLOCK
    assert card.base_block == GENETIC_ALGORITHM_BLOCK + GENETIC_ALGORITHM_UPGRADED_INCREASE
    assert card.effect_vars[GENETIC_ALGORITHM_BLOCK_KEY] == card.base_block


def test_hailstorm_factory_upgrade_increases_power_amount():
    combat = _make_combat()
    combat.hand = [make_hailstorm(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.HAILSTORM) == HAILSTORM_UPGRADED_POWER


def test_go_for_the_eyes_factory_upgrade_increases_damage_and_weak():
    combat = _make_combat(monster_factory=create_twig_slime_s)
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    combat.hand = [make_go_for_the_eyes(upgraded=True)]
    combat.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == starting_hp - GO_FOR_THE_EYES_UPGRADED_DAMAGE
    assert enemy.get_power_amount(PowerId.WEAK) == GO_FOR_THE_EYES_UPGRADED_WEAK


def test_gunk_up_factory_upgrade_increases_each_hit_damage_and_keeps_slimed():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    combat.hand = [make_gunk_up(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    slimed = [card for card in combat.discard_pile if card.card_id == CardId.SLIMED]
    assert enemy.current_hp == starting_hp - (GUNK_UP_UPGRADED_DAMAGE * GUNK_UP_HITS)
    assert len(slimed) == 1
    assert slimed[0].owner is combat.player


def test_hotfix_factory_upgrade_increases_temporary_focus():
    combat = _make_combat()
    combat.hand = [make_hotfix(upgraded=True)]
    combat.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.FOCUS) == HOTFIX_UPGRADED_FOCUS
    assert combat.player.get_power_amount(PowerId.HOTFIX) == HOTFIX_UPGRADED_FOCUS


def test_leap_factory_upgrade_increases_block():
    combat = _make_combat()
    combat.hand = [make_leap(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == LEAP_UPGRADED_BLOCK


def test_lightning_rod_factory_upgrade_increases_block_only():
    combat = _make_combat()
    combat.hand = [make_lightning_rod(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.block == LIGHTNING_ROD_UPGRADED_BLOCK
    assert combat.player.get_power_amount(PowerId.LIGHTNING_ROD) == LIGHTNING_ROD_POWER


def test_machine_learning_factory_upgrade_adds_innate_without_changing_power_amount():
    combat = _make_combat()
    card = make_machine_learning(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert card.is_innate is True
    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.MACHINE_LEARNING) == MACHINE_LEARNING_CARDS


def test_momentum_strike_factory_upgrade_increases_damage_and_sets_cost_zero():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    card = make_momentum_strike(upgraded=True)
    combat.hand = [card]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == starting_hp - MOMENTUM_STRIKE_UPGRADED_DAMAGE
    assert card.cost == ZERO_COST


def test_overclock_factory_upgrade_draws_three_and_adds_burn_to_discard():
    combat = _make_combat()
    drawn = [make_boot_sequence(), make_chill(), make_fusion()]
    combat.hand = [make_overclock(upgraded=True)]
    combat.draw_pile = list(drawn)
    combat.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.hand == drawn
    assert len(combat.hand) == OVERCLOCK_UPGRADED_CARDS
    burns = [card for card in combat.discard_pile if card.card_id == CardId.BURN]
    assert len(burns) == 1
    assert burns[0].owner is combat.player


def test_skim_factory_upgrade_draws_four_cards():
    combat = _make_combat()
    drawn = [make_boot_sequence(), make_chill(), make_fusion(), make_leap()]
    combat.hand = [make_skim(upgraded=True)]
    combat.draw_pile = list(drawn)
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.hand == drawn
    assert len(combat.hand) == SKIM_UPGRADED_CARDS


def test_storm_factory_upgrade_increases_power_amount_only():
    combat = _make_combat()
    combat.hand = [make_storm(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.STORM) == STORM_UPGRADED_POWER
    assert combat.orb_queue.orbs == []


def test_sweeping_beam_factory_upgrade_increases_all_enemy_damage_and_draws():
    combat = _make_combat(extra_enemies=1)
    enemies = list(combat.enemies)
    starting_hp = [enemy.current_hp for enemy in enemies]
    drawn = make_boot_sequence()
    combat.hand = [make_sweeping_beam(upgraded=True)]
    combat.draw_pile = [drawn]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert [enemy.current_hp for enemy in enemies] == [
        hp - SWEEPING_BEAM_UPGRADED_DAMAGE
        for hp in starting_hp
    ]
    assert combat.hand == [drawn]
    assert len(combat.hand) == SWEEPING_BEAM_DRAW_COUNT


def test_thunder_factory_upgrade_increases_power_amount_without_channeling():
    combat = _make_combat()
    combat.hand = [make_thunder(upgraded=True)]
    combat.energy = ONE_ENERGY

    assert combat.play_card(HAND_CARD_INDEX)

    assert combat.player.get_power_amount(PowerId.THUNDER) == THUNDER_UPGRADED_POWER
    assert combat.orb_queue.orbs == []


def test_turbo_factory_upgrade_gains_three_energy_and_keeps_void_discard():
    combat = _make_combat()
    combat.hand = [make_turbo(upgraded=True)]
    combat.energy = ZERO_COST

    assert combat.play_card(HAND_CARD_INDEX)

    voids = [card for card in combat.discard_pile if card.card_id == CardId.VOID]
    assert combat.energy == TURBO_UPGRADED_ENERGY
    assert len(voids) == 1
    assert voids[0].owner is combat.player


def test_uproar_factory_upgrade_increases_each_hit_damage_and_autoplays_attack():
    combat = _make_combat()
    enemy = combat.enemies[FIRST_ENEMY_INDEX]
    starting_hp = enemy.current_hp
    autoplayed = make_momentum_strike()
    combat.hand = [make_uproar(upgraded=True)]
    combat.draw_pile = [autoplayed]
    combat.energy = TWO_ENERGY

    assert combat.play_card(HAND_CARD_INDEX, FIRST_ENEMY_INDEX)

    assert enemy.current_hp == (
        starting_hp
        - (UPROAR_UPGRADED_DAMAGE * UPROAR_HITS)
        - MOMENTUM_STRIKE_DAMAGE
    )
    assert autoplayed in combat.discard_pile
