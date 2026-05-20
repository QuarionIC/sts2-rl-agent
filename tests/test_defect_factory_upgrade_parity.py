"""Defect factory upgrade parity tests backed by decompiled card models."""

import sts2_env.powers  # noqa: F401

from sts2_env.cards.defect import (
    create_defect_starter_deck,
    make_adaptive_strike,
    make_all_for_one,
    make_biased_cognition,
    make_boost_away,
    make_boot_sequence,
    make_bulk_up,
    make_chaos,
    make_chill,
)
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, OrbType, PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.act1_weak import create_shrinker_beetle


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


def _make_combat(*, extra_enemies: int = 0) -> CombatState:
    combat = CombatState(
        player_hp=TEST_PLAYER_HP,
        player_max_hp=TEST_PLAYER_HP,
        deck=create_defect_starter_deck(),
        rng_seed=TEST_RNG_SEED,
        character_id=DEFECT_CHARACTER_ID,
    )
    creature, ai = create_shrinker_beetle(Rng(TEST_RNG_SEED))
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
