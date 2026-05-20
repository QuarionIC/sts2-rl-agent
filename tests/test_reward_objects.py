"""Tests for reward objects and rewards-set assembly."""

from sts2_env.cards.factory import create_card
from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.cards.status import make_curse_of_the_bell
from sts2_env.core.enums import CardId, CardRarity, CardType, RoomType
from sts2_env.run.reward_objects import (
    CardReward,
    ENCOUNTER_GOLD_REWARD_RANGES,
    GoldReward,
    PotionReward,
    RelicReward,
    RewardsSet,
)
from sts2_env.run.rooms import CombatRoom
from sts2_env.run.run_state import RunState


class _ExtraRewardCardRelic:
    def modify_card_reward_creation_options(self, player, options, reward, room, run_state):
        return options

    def modify_card_reward_options_late(self, player, cards, reward, room, run_state):
        return [*cards, create_card(CardId.SHRUG_IT_OFF)]

    def allow_card_reward_reroll(self, player, reward, room, run_state):
        return False


def test_rewards_set_merges_combat_room_extra_rewards_for_player():
    run_state = RunState(seed=42, character_id="Ironclad")
    room = CombatRoom(room_type=RoomType.MONSTER)
    extra = CardReward(run_state.player.player_id, context="regular")
    room.add_extra_reward(run_state.player.player_id, extra)

    rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(room, run_state)
    generated = rewards.generate_without_offering(run_state)

    assert any(reward is extra for reward in generated)


def test_combat_room_gold_ranges_match_encounter_model_defaults():
    assert ENCOUNTER_GOLD_REWARD_RANGES == {
        RoomType.MONSTER: (10, 20),
        RoomType.ELITE: (35, 45),
        RoomType.BOSS: (100, 100),
    }


def test_rewards_set_uses_encounter_gold_ranges_for_combat_rooms():
    run_state = RunState(seed=43, character_id="Ironclad")
    run_state.initialize_run()

    for room_type, expected_range in ENCOUNTER_GOLD_REWARD_RANGES.items():
        room = CombatRoom(room_type=room_type)
        rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(room, run_state)
        gold_reward = next(reward for reward in rewards.rewards if isinstance(reward, GoldReward))
        assert (gold_reward.min_gold, gold_reward.max_gold) == expected_range


def test_rewards_set_scales_monster_gold_by_gold_proportion_only():
    run_state = RunState(seed=44, character_id="Ironclad")
    run_state.initialize_run()

    monster_room = CombatRoom(room_type=RoomType.MONSTER, gold_proportion=0.5)
    monster_rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(monster_room, run_state)
    monster_gold = next(reward for reward in monster_rewards.rewards if isinstance(reward, GoldReward))

    elite_room = CombatRoom(room_type=RoomType.ELITE, gold_proportion=0.5)
    elite_rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(elite_room, run_state)
    elite_gold = next(reward for reward in elite_rewards.rewards if isinstance(reward, GoldReward))

    assert (monster_gold.min_gold, monster_gold.max_gold) == (5, 10)
    assert (elite_gold.min_gold, elite_gold.max_gold) == (35, 45)


def test_rewards_set_omits_monster_gold_when_gold_proportion_is_zero():
    run_state = RunState(seed=45, character_id="Ironclad")
    run_state.initialize_run()
    room = CombatRoom(room_type=RoomType.MONSTER, gold_proportion=0)

    rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(room, run_state)

    assert not any(isinstance(reward, GoldReward) for reward in rewards.rewards)


def test_rewards_set_applies_poverty_ascension_to_encounter_gold_ranges():
    run_state = RunState(seed=46, character_id="Ironclad", ascension_level=3)
    run_state.initialize_run()

    expected = {
        RoomType.MONSTER: (8, 15),
        RoomType.ELITE: (26, 34),
        RoomType.BOSS: (75, 75),
    }
    for room_type, expected_range in expected.items():
        room = CombatRoom(room_type=room_type)
        rewards = RewardsSet(run_state.player.player_id).with_rewards_from_room(room, run_state)
        gold_reward = next(reward for reward in rewards.rewards if isinstance(reward, GoldReward))
        assert (gold_reward.min_gold, gold_reward.max_gold) == expected_range


def test_cauldron_after_obtained_queues_five_potion_rewards():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()

    assert run_state.player.obtain_relic("CAULDRON")

    assert len(run_state.pending_rewards) == 5
    assert all(isinstance(reward, PotionReward) for reward in run_state.pending_rewards)


def test_orrery_after_obtained_queues_five_card_rewards():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()

    assert run_state.player.obtain_relic("ORRERY")

    assert len(run_state.pending_rewards) == 5
    assert all(isinstance(reward, CardReward) for reward in run_state.pending_rewards)


def test_card_reward_can_upgrade_cards_after_generation():
    run_state = RunState(seed=45, character_id="Ironclad")
    run_state.initialize_run()
    reward = CardReward(
        run_state.player.player_id,
        option_count=3,
        forced_rarities=(CardRarity.COMMON, CardRarity.COMMON, CardRarity.COMMON),
        generation_context=None,
        roll_upgrade=False,
        card_type=CardType.ATTACK,
        upgrade_after_generation=True,
    )

    reward.populate(run_state, None)

    assert len(reward.cards) == 3
    assert all(card.card_type == CardType.ATTACK for card in reward.cards)
    assert all(card.rarity == CardRarity.COMMON for card in reward.cards)
    assert all(card.upgraded for card in reward.cards)


def test_card_reward_upgrade_after_generation_runs_after_late_reward_modifiers():
    run_state = RunState(seed=46, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.get_relic_objects = lambda: [_ExtraRewardCardRelic()]
    reward = CardReward(
        run_state.player.player_id,
        option_count=1,
        forced_rarities=(CardRarity.COMMON,),
        generation_context=None,
        roll_upgrade=False,
        card_type=CardType.ATTACK,
        upgrade_after_generation=True,
    )

    reward.populate(run_state, None)

    added = next(card for card in reward.cards if card.card_id == CardId.SHRUG_IT_OFF)
    assert added.upgraded


def test_other_source_card_reward_uses_noncombat_base_odds_without_changing_pity():
    run_state = RunState(seed=2, character_id="Ironclad")
    run_state.initialize_run()
    run_state.card_rarity_odds.current_value = 0.4
    reward = CardReward(
        run_state.player.player_id,
        option_count=1,
        generation_context=None,
        card_creation_source="other",
    )

    reward.populate(run_state, None)

    assert len(reward.cards) == 1
    assert reward.cards[0].rarity == CardRarity.UNCOMMON
    assert run_state.card_rarity_odds.current_value == 0.4


def test_other_source_card_reward_filters_custom_pool_before_selection():
    run_state = RunState(seed=1, character_id="Ironclad")
    run_state.initialize_run()
    reward = CardReward(
        run_state.player.player_id,
        option_count=1,
        forced_rarities=(CardRarity.COMMON,),
        generation_context=None,
        card_creation_source="other",
        use_default_character_pool=False,
        has_custom_card_pool=True,
        custom_card_ids=(CardId.BASH, CardId.BLOODLETTING),
    )

    reward.populate(run_state, None)

    assert [card.card_id for card in reward.cards] == [CardId.BLOODLETTING]


def test_calling_bell_adds_curse_and_queues_three_relic_rewards():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()

    assert run_state.player.obtain_relic("CALLING_BELL")

    assert any(card.card_id == make_curse_of_the_bell().card_id for card in run_state.player.deck)
    assert len(run_state.pending_rewards) == 3
    assert all(isinstance(reward, RelicReward) for reward in run_state.pending_rewards)


def test_war_paint_upgrades_two_skills_on_obtain():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()

    assert run_state.player.obtain_relic("WAR_PAINT")

    upgraded_skills = [card for card in run_state.player.deck if card.upgraded and card.card_type.name == "SKILL"]
    assert len(upgraded_skills) >= 2


def test_whetstone_upgrades_two_attacks_on_obtain():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()

    assert run_state.player.obtain_relic("WHETSTONE")

    upgraded_attacks = [card for card in run_state.player.deck if card.upgraded and card.card_type.name == "ATTACK"]
    assert len(upgraded_attacks) >= 2


def test_archaic_tooth_transforms_a_basic_card():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    before = [card.card_id for card in run_state.player.deck]

    assert run_state.player.obtain_relic("ARCHAIC_TOOTH")

    after = [card.card_id for card in run_state.player.deck]
    assert before != after


def test_astrolabe_transforms_and_upgrades_three_cards():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    before = [card.card_id for card in run_state.player.deck[:3]]

    assert run_state.player.obtain_relic("ASTROLABE")

    after = [card.card_id for card in run_state.player.deck[:3]]
    assert before != after
    assert sum(1 for card in run_state.player.deck if card.upgraded) >= 3


def test_precise_scissors_removes_one_card_from_deck():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    starting_deck = len(run_state.player.deck)

    assert run_state.player.obtain_relic("PRECISE_SCISSORS")

    assert len(run_state.player.deck) == starting_deck - 1


def test_pandoras_box_only_transforms_basic_strike_defend_cards():
    run_state = RunState(seed=42, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    bash_count_before = sum(1 for card in run_state.player.deck if card.card_id == CardId.BASH)

    assert run_state.player.obtain_relic("PANDORAS_BOX")

    assert sum(1 for card in run_state.player.deck if card.card_id == CardId.BASH) == bash_count_before
    assert sum(
        1
        for card in run_state.player.deck
        if card.rarity.name == "BASIC" and ("STRIKE" in card.card_id.name or "DEFEND" in card.card_id.name)
    ) == 0


def test_duplicate_card_helper_excludes_quest_cards_from_candidates():
    run_state = RunState(seed=52, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    run_state.player.deck.append(create_card(CardId.BYRDONIS_EGG))

    assert run_state.player.duplicate_card_from_deck(cards=run_state.player.duplicable_deck_cards())

    assert sum(1 for card in run_state.player.deck if card.card_id == CardId.BYRDONIS_EGG) == 1
