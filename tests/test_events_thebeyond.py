"""Tests for TheBeyond events ("Acts from the Past" mod, classic mode).

Covers sts2_env/events/thebeyond.py, the event-only encounters appended to
sts2_env/encounters/thebeyond.py, the MarkOfTheBloom relic's heal-to-zero
hook (sts2_env/relics/thebeyond.py + the Creature/PlayerState heal
pipelines), the Madness card and the mod-behavior Pain curse
(sts2_env/cards/status.py), and RunManager's SecretPortal boss skip.

Follows the conventions of tests/test_events_exordium.py (direct RunState +
EventModel exercise; the legacy acts aren't wired into a live act slot yet).
"""

from __future__ import annotations

import sts2_env.events  # noqa: F401  (registers all events)

from sts2_env.cards.factory import create_card
from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.cards.status import make_pain, make_regret
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, CardType, PowerId, RoomType
from sts2_env.core.rng import Rng
from sts2_env.encounters.events import get_event_encounter_setup
from sts2_env.events.thebeyond import (
    THEBEYOND_EVENT_IDS,
    Falling,
    MindBloom,
    MoaiHead,
    MysteriousSphere,
    SecretPortal,
    SensoryStone,
    TombOfLordRedMask,
    WindingHalls,
)
from sts2_env.map.acts import ActConfig
from sts2_env.relics.base import RelicId, RelicRarity
from sts2_env.relics.registry import create_relic_by_name
from sts2_env.run.events import event_allowed_in_act, get_event
from sts2_env.run.reward_objects import CardReward, GoldReward, PotionReward, RelicReward
from sts2_env.run.run_manager import RunManager
from sts2_env.run.run_state import RunState


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_run_state(seed: int = 42, gold: int = 200) -> RunState:
    run_state = RunState(seed=seed, character_id="Ironclad")
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    run_state.player.gold = gold
    return run_state


class _ScriptedRng:
    """Stand-in event RNG returning scripted rolls, with a no-op shuffle."""

    def __init__(self, rolls, floats=None, choices=None):
        self._rolls = list(rolls)
        self._floats = list(floats or [])
        self._choices = list(choices or [])

    def next_int_exclusive(self, low, high):
        return self._rolls.pop(0)

    def next_int(self, low, high):
        return self._rolls.pop(0)

    def next_float(self, upper=1.0):
        return self._floats.pop(0)

    def choice(self, lst):
        if self._choices:
            return lst[self._choices.pop(0)]
        return lst[0]

    def shuffle(self, lst):
        pass


def _resolve_single_card_choice(event, index: int = 0):
    assert event.pending_choice is not None
    return event.resolve_pending_choice(index)


# ---------------------------------------------------------------------------
# Registration / pool tagging
# ---------------------------------------------------------------------------

def test_all_thebeyond_events_are_registered():
    assert len(THEBEYOND_EVENT_IDS) == 8
    for event_id in THEBEYOND_EVENT_IDS:
        assert get_event(event_id) is not None, event_id


def test_all_thebeyond_events_are_tagged_legacy_exclusive():
    for event_id in THEBEYOND_EVENT_IDS:
        assert get_event(event_id).is_legacy_exclusive is True, event_id


def test_shared_thebeyond_events_blocked_outside_legacy_acts():
    legacy_act = ActConfig(act_index=2, num_rooms=1, is_legacy=True)
    vanilla_act = ActConfig(act_index=2, num_rooms=1, is_legacy=False)
    for event_id in ("MindBloom", "MysteriousSphere", "SecretPortal"):
        event = get_event(event_id)
        assert event.is_shared is True
        assert event_allowed_in_act(event, legacy_act) is True
        assert event_allowed_in_act(event, vanilla_act) is False


def test_secret_portal_is_a_one_time_shrine():
    event = get_event("SecretPortal")
    assert event.is_shrine is True
    assert event.is_one_time_event is True
    # The other TheBeyond events are NOT shrines.
    for event_id in THEBEYOND_EVENT_IDS:
        if event_id != "SecretPortal":
            assert get_event(event_id).is_shrine is False, event_id


def test_vanilla_act_event_pools_do_not_contain_thebeyond_events():
    from sts2_env.map.acts import ALL_ACTS

    for act in ALL_ACTS:
        for event_id in THEBEYOND_EVENT_IDS:
            assert event_id not in act.event_ids


def test_event_encounters_registered_but_not_in_random_pools():
    from sts2_env.encounters import thebeyond as enc

    for encounter_id in (
        "two_orb_walkers_event",
        "mind_bloom_guardian",
        "mind_bloom_hexaghost",
        "mind_bloom_slime_boss",
    ):
        setup = get_event_encounter_setup(encounter_id)
        assert setup is not None, encounter_id
        assert setup not in enc.WEAK_ENCOUNTERS
        assert setup not in enc.NORMAL_ENCOUNTERS
        assert setup not in enc.ELITE_ENCOUNTERS
        assert setup not in enc.BOSS_ENCOUNTERS


def test_event_encounter_compositions():
    def _spawn(encounter_id):
        combat = CombatState(
            player_hp=999,
            player_max_hp=999,
            deck=create_ironclad_starter_deck(),
            rng_seed=5,
            character_id="Ironclad",
        )
        get_event_encounter_setup(encounter_id)(combat, Rng(5))
        return combat

    orb_walkers = _spawn("two_orb_walkers_event").enemies
    assert len(orb_walkers) == 2

    assert len(_spawn("mind_bloom_guardian").enemies) == 1
    assert len(_spawn("mind_bloom_hexaghost").enemies) == 1
    assert len(_spawn("mind_bloom_slime_boss").enemies) == 1


# ---------------------------------------------------------------------------
# Falling
# ---------------------------------------------------------------------------

def test_falling_offers_one_random_card_per_category():
    run_state = _make_run_state()
    event = Falling()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert [o.option_id for o in options] == ["continue"]

    result = event.choose(run_state, "continue")
    assert not result.finished
    ids = [o.option_id for o in result.next_options]
    # Ironclad starter deck: skills (Defends) + attacks (Strikes/Bash), no
    # powers -> the power option is locked.
    assert "skill" in ids
    assert "attack" in ids
    assert "power_locked" in ids
    power_option = next(o for o in result.next_options if o.option_id == "power_locked")
    assert power_option.enabled is False


def test_falling_skill_removes_the_pre_rolled_skill():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    event = Falling()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    rolled = event._skill_card
    assert rolled is not None and rolled.card_type == CardType.SKILL

    event.choose(run_state, "continue")
    result = event.choose(run_state, "skill")
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1
    assert rolled not in run_state.player.deck


def test_falling_attack_removes_the_pre_rolled_attack():
    run_state = _make_run_state()
    event = Falling()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    rolled = event._attack_card
    assert rolled is not None and rolled.card_type == CardType.ATTACK

    event.choose(run_state, "continue")
    result = event.choose(run_state, "attack")
    assert result.finished
    assert rolled not in run_state.player.deck


# ---------------------------------------------------------------------------
# MindBloom
# ---------------------------------------------------------------------------

def test_mind_bloom_offers_gold_before_floor_41_and_heal_after():
    run_state = _make_run_state()
    run_state.total_floor = 40
    event = MindBloom()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert [o.option_id for o in options] == ["fight", "upgrade", "gold"]

    run_state2 = _make_run_state(seed=43)
    run_state2.total_floor = 41
    event2 = MindBloom()
    event2.reset_rng_for_run(run_state2)
    options2 = event2.generate_initial_options(run_state2)
    assert [o.option_id for o in options2] == ["fight", "upgrade", "heal"]


def test_mind_bloom_gold_gives_999_and_two_normality():
    run_state = _make_run_state(gold=0)
    run_state.total_floor = 10
    event = MindBloom()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "gold")
    assert result.finished
    assert run_state.player.gold == 999
    normality = [c for c in run_state.player.deck if c.card_id == CardId.NORMALITY]
    assert len(normality) == 2


def test_mind_bloom_heal_fully_heals_and_adds_doubt():
    run_state = _make_run_state()
    run_state.total_floor = 50
    run_state.player.current_hp = 10
    event = MindBloom()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "heal")
    assert result.finished
    assert run_state.player.current_hp == run_state.player.max_hp
    assert any(c.card_id == CardId.DOUBT for c in run_state.player.deck)


def test_mind_bloom_upgrade_upgrades_all_cards_and_grants_mark_of_the_bloom():
    run_state = _make_run_state()
    run_state.total_floor = 10
    event = MindBloom()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "upgrade")
    assert result.finished
    assert all(card.upgraded for card in run_state.player.deck)
    assert RelicId.MARK_OF_THE_BLOOM.name in run_state.player.relics
    # Mark of the Bloom: healing is reduced to zero for the rest of the run.
    run_state.player.current_hp = 1
    assert run_state.player.heal(50) == 0
    assert run_state.player.current_hp == 1


def test_mind_bloom_fight_rolls_a_boss_reskin_with_boss_tier_rewards():
    run_state = _make_run_state()
    run_state.total_floor = 10
    event = MindBloom()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "fight")
    assert result.finished
    assert result.event_combat_setup in (
        "mind_bloom_guardian", "mind_bloom_hexaghost", "mind_bloom_slime_boss",
    )
    rewards = result.rewards["reward_objects"]
    card_rewards = [r for r in rewards if isinstance(r, CardReward)]
    gold_rewards = [r for r in rewards if isinstance(r, GoldReward)]
    relic_rewards = [r for r in rewards if isinstance(r, RelicReward)]
    potion_rewards = [r for r in rewards if isinstance(r, PotionReward)]
    assert len(card_rewards) == 1
    assert card_rewards[0].context == "boss"
    assert [(g.min_gold, g.max_gold) for g in gold_rewards] == [(50, 50)]
    assert len(relic_rewards) == 1
    assert relic_rewards[0].rarity == RelicRarity.RARE
    # potion is an odds roll -- 0 or 1, never more
    assert len(potion_rewards) <= 1
    assert len(rewards) == 3 + len(potion_rewards)


# ---------------------------------------------------------------------------
# MarkOfTheBloom heal-to-zero (combat pipeline)
# ---------------------------------------------------------------------------

def test_mark_of_the_bloom_blocks_combat_healing():
    combat = CombatState(
        player_hp=20,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=7,
        character_id="Ironclad",
        relics=[RelicId.MARK_OF_THE_BLOOM.name],
    )
    assert combat.player.heal(30) == 0
    assert combat.player.current_hp == 20


def test_combat_healing_unaffected_without_the_mark():
    combat = CombatState(
        player_hp=20,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=7,
        character_id="Ironclad",
    )
    assert combat.player.heal(30) == 30


# ---------------------------------------------------------------------------
# MoaiHead
# ---------------------------------------------------------------------------

def test_moai_head_gate_requires_exordium_visit_or_half_hp():
    run_state = _make_run_state()
    event = MoaiHead()
    assert event.is_allowed(run_state) is False

    run_state.player.current_hp = run_state.player.max_hp // 2
    assert event.is_allowed(run_state) is True

    run_state2 = _make_run_state(seed=43)
    run_state2.acts[0].act_id = "Exordium"
    run_state2.current_act_index = 1
    assert event.is_allowed(run_state2) is True
    # Exordium must be BEFORE the current act.
    run_state2.current_act_index = 0
    assert event.is_allowed(run_state2) is False


def test_moai_head_pray_loses_18_percent_max_hp_then_fully_heals():
    run_state = _make_run_state()
    run_state.player.max_hp = 100
    run_state.player.current_hp = 30
    event = MoaiHead()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "pray")
    assert result.finished
    assert run_state.player.max_hp == 82  # 100 - round(100 * 0.18)
    assert run_state.player.current_hp == 82


def test_moai_head_offer_idol_locked_without_golden_idol():
    run_state = _make_run_state()
    run_state.player.current_hp = 10
    event = MoaiHead()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    offer = next(o for o in options if o.option_id == "offer_idol")
    assert offer.enabled is False


def test_moai_head_offer_idol_trades_relic_for_333_gold():
    run_state = _make_run_state(gold=0)
    run_state.player.current_hp = 10
    run_state.player.obtain_relic(RelicId.GOLDEN_IDOL.name)
    event = MoaiHead()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "offer_idol").enabled

    result = event.choose(run_state, "offer_idol")
    assert result.finished
    assert run_state.player.gold == 333
    assert RelicId.GOLDEN_IDOL.name not in run_state.player.relics


# ---------------------------------------------------------------------------
# MysteriousSphere
# ---------------------------------------------------------------------------

def test_mysterious_sphere_fight_pays_gold_and_rare_relic():
    run_state = _make_run_state()
    event = MysteriousSphere()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    opened = event.choose(run_state, "open")
    assert not opened.finished
    assert [o.option_id for o in opened.next_options] == ["fight"]

    fight = event.choose(run_state, "fight")
    assert fight.finished
    assert fight.event_combat_setup == "two_orb_walkers_event"
    rewards = fight.rewards["reward_objects"]
    gold_rewards = [r for r in rewards if isinstance(r, GoldReward)]
    relic_rewards = [r for r in rewards if isinstance(r, RelicReward)]
    assert len(rewards) == 2
    assert [(g.min_gold, g.max_gold) for g in gold_rewards] == [(45, 55)]
    assert relic_rewards[0].rarity == RelicRarity.RARE


# ---------------------------------------------------------------------------
# SecretPortal
# ---------------------------------------------------------------------------

def test_secret_portal_enter_signals_boss_jump():
    run_state = _make_run_state()
    event = SecretPortal()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    entered = event.choose(run_state, "enter")
    assert not entered.finished
    result = event.choose(run_state, "teleport")
    assert result.finished
    assert result.rewards.get("jump_to_boss") is True


def test_secret_portal_leave_does_nothing():
    run_state = _make_run_state()
    event = SecretPortal()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "leave")
    assert result.finished
    assert "jump_to_boss" not in result.rewards


def test_run_manager_jump_to_boss_room_enters_the_boss_combat():
    run = RunManager(seed=11, character_id="Ironclad")
    boss_coord = run.run_state.map.boss_point.coord
    assert boss_coord not in run.run_state.visited_map_coords

    run._jump_to_boss_room()
    assert run.phase == RunManager.PHASE_COMBAT
    assert boss_coord in run.run_state.visited_map_coords
    assert run._current_room_type == RoomType.BOSS


def test_secret_portal_event_flow_skips_to_boss_through_run_manager():
    run = RunManager(seed=11, character_id="Ironclad")
    event = SecretPortal()
    event.reset_rng_for_run(run.run_state)
    run._event_model = event
    run._event_options = event.generate_initial_options(run.run_state)
    run._event_started = True
    run._phase = RunManager.PHASE_EVENT

    result = run._do_event_choice({"option_id": "enter"})
    assert result["phase"] == RunManager.PHASE_EVENT
    result = run._do_event_choice({"option_id": "teleport"})
    assert result["phase"] == RunManager.PHASE_COMBAT
    assert run._current_room_type == RoomType.BOSS
    assert run.run_state.map.boss_point.coord in run.run_state.visited_map_coords


# ---------------------------------------------------------------------------
# SensoryStone
# ---------------------------------------------------------------------------

def _sensory_stone_choose(run_state, option_id):
    event = SensoryStone()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "continue")
    assert not result.finished
    assert [o.option_id for o in result.next_options] == ["memory_1", "memory_2", "memory_3"]
    return event.choose(run_state, option_id)


def test_sensory_stone_one_memory_is_free():
    run_state = _make_run_state()
    hp_before = run_state.player.current_hp
    result = _sensory_stone_choose(run_state, "memory_1")
    assert result.finished
    assert run_state.player.current_hp == hp_before
    rewards = result.rewards["reward_objects"]
    assert len(rewards) == 1
    assert all(isinstance(r, CardReward) for r in rewards)


def test_sensory_stone_three_memories_cost_10_hp_for_3_colorless_rewards():
    run_state = _make_run_state()
    hp_before = run_state.player.current_hp
    result = _sensory_stone_choose(run_state, "memory_3")
    assert result.finished
    assert run_state.player.current_hp == hp_before - 10
    rewards = result.rewards["reward_objects"]
    assert len(rewards) == 3
    for reward in rewards:
        assert isinstance(reward, CardReward)
        assert reward.include_colorless is True
        assert reward.use_default_character_pool is False
        assert reward.option_count == 3


def test_sensory_stone_two_memories_cost_5_hp():
    run_state = _make_run_state()
    hp_before = run_state.player.current_hp
    result = _sensory_stone_choose(run_state, "memory_2")
    assert run_state.player.current_hp == hp_before - 5
    assert len(result.rewards["reward_objects"]) == 2


# ---------------------------------------------------------------------------
# TombOfLordRedMask
# ---------------------------------------------------------------------------

def test_tomb_pay_respects_costs_all_gold_and_grants_red_mask():
    run_state = _make_run_state(gold=137)
    event = TombOfLordRedMask()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "wear_mask").enabled is False
    assert any(o.option_id == "pay_respects" for o in options)

    result = event.choose(run_state, "pay_respects")
    assert result.finished
    assert run_state.player.gold == 0
    assert RelicId.RED_MASK.name in run_state.player.relics


def test_tomb_wear_mask_with_red_mask_gains_222_gold():
    run_state = _make_run_state(gold=0)
    run_state.player.obtain_relic(RelicId.RED_MASK.name)
    event = TombOfLordRedMask()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "wear_mask").enabled is True
    assert not any(o.option_id == "pay_respects" for o in options)

    result = event.choose(run_state, "wear_mask")
    assert result.finished
    assert run_state.player.gold == 222


# ---------------------------------------------------------------------------
# WindingHalls
# ---------------------------------------------------------------------------

def _winding_halls_choose(run_state, option_id):
    event = WindingHalls()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "continue")
    assert not result.finished
    return event.choose(run_state, option_id)


def test_winding_halls_madness_damages_and_adds_two_madness_cards():
    run_state = _make_run_state()
    run_state.player.max_hp = 100
    run_state.player.current_hp = 100
    result = _winding_halls_choose(run_state, "madness")
    assert result.finished
    assert run_state.player.current_hp == 100 - 18  # round(100 * 0.18)
    madness = [c for c in run_state.player.deck if c.card_id == CardId.MADNESS]
    assert len(madness) == 2


def test_winding_halls_writhe_heals_20_percent_and_adds_writhe():
    run_state = _make_run_state()
    run_state.player.max_hp = 100
    run_state.player.current_hp = 50
    result = _winding_halls_choose(run_state, "writhe")
    assert result.finished
    assert run_state.player.current_hp == 70  # +round(100 * 0.20)
    assert any(c.card_id == CardId.WRITHE for c in run_state.player.deck)


def test_winding_halls_retreat_loses_5_percent_max_hp():
    run_state = _make_run_state()
    run_state.player.max_hp = 100
    run_state.player.current_hp = 100
    result = _winding_halls_choose(run_state, "retreat")
    assert result.finished
    assert run_state.player.max_hp == 95


# ---------------------------------------------------------------------------
# Madness card / Pain curse (mod behavior)
# ---------------------------------------------------------------------------

def test_madness_card_makes_a_random_hand_card_free_for_the_combat():
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=3,
        character_id="Ironclad",
    )
    combat.start_combat()
    from sts2_env.cards.status import madness_effect

    madness = create_card(CardId.MADNESS)
    madness.owner = combat.player
    combat.hand = list(combat.draw_pile[:5])
    costs_before = [card.cost for card in combat.hand]
    assert all(cost > 0 for cost in costs_before)  # starter cards cost 1-2

    madness_effect(madness, combat, None)
    zero_cost = [card for card in combat.hand if card.cost == 0]
    assert len(zero_cost) == 1


def test_madness_upgrade_costs_zero():
    assert create_card(CardId.MADNESS).cost == 1
    assert create_card(CardId.MADNESS, upgraded=True).cost == 0


def test_pain_in_hand_loses_1_hp_when_another_card_is_played():
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=3,
        character_id="Ironclad",
    )
    combat.start_combat()
    pain = make_pain()
    pain.owner = combat.player
    combat.hand = list(combat.draw_pile[:3]) + [pain]
    hp_before = combat.player.current_hp

    other = next(card for card in combat.hand if card is not pain)
    combat._apply_card_before_card_played(other, combat.player)
    assert combat.player.current_hp == hp_before - 1

    # Pain itself being played does not trigger it.
    combat._apply_card_before_card_played(pain, combat.player)
    assert combat.player.current_hp == hp_before - 1

    # Pain in the discard pile does not trigger it.
    combat.hand = [card for card in combat.hand if card is not pain]
    combat.discard_pile = combat.discard_pile + [pain]
    combat._apply_card_before_card_played(other, combat.player)
    assert combat.player.current_hp == hp_before - 1
