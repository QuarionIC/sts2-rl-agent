"""Tests for TheCity (Act-2-slot legacy act, "Acts from the Past" mod)
events: sts2_env/events/thecity.py plus the supporting cards
(Jax/Bite/RitualDagger), relics (MutagenicStrength/BloodyIdol/Enchiridion/
NilrysCodex), event-only monsters (Pointy/Romeo/Bear) and the
event-embedded combats (Colosseum, MaskedBandits) with their
standard-reward suppression.

All expected numbers come from the decompiled mod source in
decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.TheCity.Events/*.cs
(classic mode -- RebalancedMode=False).
"""

from __future__ import annotations

import math

import sts2_env.events  # noqa: F401  (registers all events)

from sts2_env.cards.factory import card_metadata, create_card
from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.cards.registry import play_card_effect
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, CardRarity, CardTag, CardType, PowerId
from sts2_env.events.thecity import (
    THECITY_EVENT_IDS,
    AncientWriting,
    Augmenter,
    Colosseum,
    CouncilOfGhosts,
    CursedTome,
    ForgottenAltar,
    KnowingSkull,
    MaskedBandits,
    Nloth,
    OldBeggar,
    PleadingVagrant,
    TheJoust,
    TheLibrary,
    TheMausoleum,
    TheNest,
    Vampires,
)
from sts2_env.map.acts import ActConfig
from sts2_env.monsters.thecity import create_bear, create_pointy, create_romeo
from sts2_env.relics.base import RelicId
from sts2_env.run.events import event_allowed_in_act, get_event
from sts2_env.run.reward_objects import (
    AddCardsReward,
    CardReward,
    GoldReward,
    RelicReward,
    RemoveCardReward,
    TransformCardsReward,
)
from sts2_env.run.rooms import CombatRoom
from sts2_env.run.run_manager import RunManager
from sts2_env.run.run_state import PlayerState, RunState


IRONCLAD = "Ironclad"


def _make_run_state(seed: int = 4200) -> RunState:
    run_state = RunState(seed=seed, character_id=IRONCLAD)
    run_state.initialize_run()
    run_state.player.deck = create_ironclad_starter_deck()
    return run_state


def _resolve_single(event, index: int = 0):
    """Resolve a pending single-card event choice."""
    return event.resolve_pending_choice(index)


def _resolve_multi(event, indices):
    for index in indices:
        event.resolve_pending_choice(index)
    return event.resolve_pending_choice(None)


class _StubRng:
    """Deterministic rng stand-in for event rolls."""

    def __init__(self, floats=(), choices=None):
        self._floats = list(floats)
        self._choice_index = choices

    def next_float(self, _max=1.0):
        return self._floats.pop(0)

    def choice(self, seq):
        return seq[0 if self._choice_index is None else self._choice_index]

    def shuffle(self, seq):
        pass


# ---------------------------------------------------------------------------
# Registration / act scoping
# ---------------------------------------------------------------------------

def test_all_thecity_events_registered():
    for event_id in THECITY_EVENT_IDS:
        assert get_event(event_id) is not None, event_id
    assert len(THECITY_EVENT_IDS) == 16


def test_shared_flags_match_decompiled_source():
    # Only Colosseum and MaskedBandits are IsShared => true in the mod.
    for event_id in THECITY_EVENT_IDS:
        event = get_event(event_id)
        assert event.is_legacy_exclusive is True, event_id
        expected_shared = event_id in {"Colosseum", "MaskedBandits"}
        assert event.is_shared is expected_shared, event_id


def test_shared_thecity_events_blocked_in_vanilla_acts():
    vanilla_act = ActConfig(act_index=1, num_rooms=10)
    legacy_act = ActConfig(act_index=1, num_rooms=10, is_legacy=True, act_id="TheCity")
    for event_id in ("Colosseum", "MaskedBandits"):
        event = get_event(event_id)
        assert event_allowed_in_act(event, vanilla_act) is False
        assert event_allowed_in_act(event, legacy_act) is True


# ---------------------------------------------------------------------------
# AncientWriting
# ---------------------------------------------------------------------------

def test_ancient_writing_elegance_removes_one_card():
    run_state = _make_run_state(101)
    event = AncientWriting()
    deck_size = len(run_state.player.deck)

    result = event.choose(run_state, "elegance")
    assert not result.finished and event.pending_choice is not None
    result = _resolve_single(event)
    assert result.finished
    assert len(run_state.player.deck) == deck_size - 1


def test_ancient_writing_elegance_defers_to_remove_reward():
    run_state = _make_run_state(102)
    run_state.defer_followup_rewards = True
    event = AncientWriting()

    result = event.choose(run_state, "elegance")
    assert result.finished
    reward = result.rewards["reward_objects"][0]
    assert isinstance(reward, RemoveCardReward)
    assert reward.count == 1


def test_ancient_writing_simplicity_upgrades_all_basic_strikes_and_defends():
    run_state = _make_run_state(103)
    event = AncientWriting()
    basics = [
        card for card in run_state.player.deck
        if card.rarity == CardRarity.BASIC
        and (CardTag.STRIKE in card.tags or CardTag.DEFEND in card.tags)
    ]
    others = [card for card in run_state.player.deck if card not in basics]
    assert basics

    result = event.choose(run_state, "simplicity")
    assert result.finished
    assert all(card.upgraded for card in basics)
    assert all(not card.upgraded for card in others)


# ---------------------------------------------------------------------------
# Augmenter
# ---------------------------------------------------------------------------

def test_augmenter_requires_two_removable_cards():
    run_state = _make_run_state(111)
    event = Augmenter()
    assert event.is_allowed(run_state) is True
    run_state.player.deck = run_state.player.deck[:1]
    assert event.is_allowed(run_state) is False


def test_augmenter_jax_adds_jax_card():
    run_state = _make_run_state(112)
    event = Augmenter()
    result = event.choose(run_state, "jax")
    assert result.finished
    assert any(card.card_id == CardId.JAX for card in run_state.player.deck)


def test_augmenter_transform_defers_two_card_transform():
    run_state = _make_run_state(113)
    run_state.defer_followup_rewards = True
    event = Augmenter()
    result = event.choose(run_state, "transform")
    assert result.finished
    reward = result.rewards["reward_objects"][0]
    assert isinstance(reward, TransformCardsReward)
    assert reward.count == 2


def test_augmenter_mutagens_grants_mutagenic_strength():
    run_state = _make_run_state(114)
    event = Augmenter()
    result = event.choose(run_state, "mutagens")
    assert result.finished
    assert RelicId.MUTAGENIC_STRENGTH.name in run_state.player.relics


# ---------------------------------------------------------------------------
# Colosseum (event-embedded combats + reward suppression)
# ---------------------------------------------------------------------------

def test_colosseum_is_allowed_gating():
    run_state = _make_run_state(121)
    event = Colosseum()
    run_state.total_floor = 22
    assert event.is_allowed(run_state) is False
    run_state.total_floor = 23
    assert event.is_allowed(run_state) is True
    run_state.players.append(PlayerState(character_id=IRONCLAD, player_id=2))
    assert event.is_allowed(run_state) is False


def _win_current_combat(mgr: RunManager) -> dict:
    combat = mgr._combat
    assert combat is not None
    for enemy in combat.enemies:
        enemy.current_hp = 0
    combat._check_combat_end()
    assert combat.player_won
    return mgr._resolve_combat_end()


def _start_event(mgr: RunManager, event) -> None:
    event.reset_rng_for_run(mgr.run_state)
    mgr._phase = RunManager.PHASE_EVENT
    mgr._event_model = event
    mgr._event_started = True
    mgr._event_options = event.generate_initial_options(mgr.run_state)


def test_colosseum_full_flow_fight_again():
    mgr = RunManager(seed=1234, character_id=IRONCLAD)
    run_state = mgr.run_state
    run_state.total_floor = 25
    event = Colosseum()
    _start_event(mgr, event)

    result = mgr._do_event_choice({"option_id": "enter"})
    assert result["phase"] == RunManager.PHASE_EVENT
    assert [option.option_id for option in mgr._event_options] == ["fight"]

    result = mgr._do_event_choice({"option_id": "fight"})
    assert result["phase"] == RunManager.PHASE_COMBAT
    combat = mgr._combat
    assert sorted(enemy.monster_id for enemy in combat.enemies) == [
        "EXORDIUM_SLAVER_BLUE", "EXORDIUM_SLAVER_RED",
    ]
    # First fight: standard rewards suppressed AND no explicit rewards.
    room = mgr._current_room
    assert isinstance(room, CombatRoom)
    assert room.suppress_default_rewards is True
    assert room.extra_rewards.get(run_state.player.player_id, []) == []

    info = _win_current_combat(mgr)
    assert info["gold_earned"] == 0
    # The event resumes with the POST_SLAVERS choice.
    assert mgr.phase == RunManager.PHASE_EVENT
    assert mgr._event_model is event
    assert [option.option_id for option in mgr._event_options] == ["fight_again", "flee"]

    gold_before = run_state.player.gold
    relics_before = set(run_state.player.relics)
    result = mgr._do_event_choice({"option_id": "fight_again"})
    assert result["phase"] == RunManager.PHASE_COMBAT
    combat = mgr._combat
    assert sorted(enemy.monster_id for enemy in combat.enemies) == [
        "EXORDIUM_GREMLIN_NOB", "THECITY_TASKMASTER",
    ]
    room = mgr._current_room
    assert room.suppress_default_rewards is True
    extra = room.extra_rewards[run_state.player.player_id]
    assert sum(isinstance(reward, RelicReward) for reward in extra) == 2
    gold_rewards = [reward for reward in extra if isinstance(reward, GoldReward)]
    assert len(gold_rewards) == 1 and gold_rewards[0].amount == 100

    _win_current_combat(mgr)
    # Consume the reward chain (2 relics + 100 gold).
    steps = 0
    while mgr._current_reward is not None and steps < 10:
        mgr._current_reward.select(mgr)
        mgr._advance_post_combat_rewards()
        steps += 1
    assert run_state.player.gold - gold_before == 100
    assert len(set(run_state.player.relics) - relics_before) == 2


def test_colosseum_flee_finishes_event():
    run_state = _make_run_state(122)
    run_state.total_floor = 25
    event = Colosseum()
    result = event.choose(run_state, "flee")
    assert result.finished
    assert result.event_combat_setup is None


# ---------------------------------------------------------------------------
# CouncilOfGhosts
# ---------------------------------------------------------------------------

def test_council_of_ghosts_accept_costs_half_max_hp_and_gives_apparitions():
    run_state = _make_run_state(131)
    event = CouncilOfGhosts()
    max_hp = run_state.player.max_hp
    expected_loss = math.ceil(max_hp * 0.5)

    result = event.choose(run_state, "accept")
    assert result.finished
    assert run_state.player.max_hp == max_hp - expected_loss
    apparitions = [card for card in run_state.player.deck if card.card_id == CardId.APPARITION]
    assert len(apparitions) == 3


def test_council_of_ghosts_refuse_does_nothing():
    run_state = _make_run_state(132)
    event = CouncilOfGhosts()
    max_hp = run_state.player.max_hp
    deck_size = len(run_state.player.deck)
    result = event.choose(run_state, "refuse")
    assert result.finished
    assert run_state.player.max_hp == max_hp
    assert len(run_state.player.deck) == deck_size


# ---------------------------------------------------------------------------
# CursedTome
# ---------------------------------------------------------------------------

def test_cursed_tome_classic_damage_sequence_and_book():
    run_state = _make_run_state(141)
    event = CursedTome()
    start_hp = run_state.player.current_hp

    result = event.choose(run_state, "read")
    assert not result.finished
    assert run_state.player.current_hp == start_hp  # opening is free

    event.choose(run_state, "continue_1")
    assert run_state.player.current_hp == start_hp - 1
    event.choose(run_state, "continue_2")
    assert run_state.player.current_hp == start_hp - 1 - 2
    result = event.choose(run_state, "continue_3")
    assert run_state.player.current_hp == start_hp - 1 - 2 - 3
    assert {option.option_id for option in result.next_options} == {"obtain", "stop"}

    result = event.choose(run_state, "obtain")
    assert result.finished
    assert run_state.player.current_hp == start_hp - 1 - 2 - 3 - 15
    books = {RelicId.NECRONOMICON.name, RelicId.ENCHIRIDION.name, RelicId.NILRYS_CODEX.name}
    assert len(books & set(run_state.player.relics)) == 1


def test_cursed_tome_stop_costs_three():
    run_state = _make_run_state(142)
    event = CursedTome()
    event.choose(run_state, "read")
    event.choose(run_state, "continue_1")
    event.choose(run_state, "continue_2")
    event.choose(run_state, "continue_3")
    hp_before = run_state.player.current_hp
    result = event.choose(run_state, "stop")
    assert result.finished
    assert run_state.player.current_hp == hp_before - 3
    assert not any(
        relic in run_state.player.relics
        for relic in (RelicId.NECRONOMICON.name, RelicId.ENCHIRIDION.name, RelicId.NILRYS_CODEX.name)
    )


def test_cursed_tome_book_roll_excludes_owned_books():
    run_state = _make_run_state(143)
    event = CursedTome()
    run_state.player.obtain_relic(RelicId.NECRONOMICON.name)
    run_state.player.obtain_relic(RelicId.ENCHIRIDION.name)
    assert event._roll_book_relic_id(run_state) == RelicId.NILRYS_CODEX.name


# ---------------------------------------------------------------------------
# ForgottenAltar
# ---------------------------------------------------------------------------

def _visit_exordium_first(run_state: RunState) -> None:
    run_state.acts[0].act_id = "Exordium"
    run_state.current_act_index = 1


def test_forgotten_altar_requires_prior_exordium_act():
    run_state = _make_run_state(151)
    event = ForgottenAltar()
    run_state.current_act_index = 1
    assert event.is_allowed(run_state) is False
    _visit_exordium_first(run_state)
    assert event.is_allowed(run_state) is True
    # Being IN Exordium (index 0) doesn't count -- only earlier acts do.
    run_state.current_act_index = 0
    assert event.is_allowed(run_state) is False


def test_forgotten_altar_offer_idol_swaps_golden_for_bloody():
    run_state = _make_run_state(152)
    _visit_exordium_first(run_state)
    event = ForgottenAltar()
    options = event.generate_initial_options(run_state)
    assert options[0].enabled is False  # no Golden Idol yet

    run_state.player.obtain_relic(RelicId.GOLDEN_IDOL.name)
    event2 = ForgottenAltar()
    options = event2.generate_initial_options(run_state)
    assert options[0].enabled is True
    result = event2.choose(run_state, "offer_idol")
    assert result.finished
    assert RelicId.GOLDEN_IDOL.name not in run_state.player.relics
    assert RelicId.BLOODY_IDOL.name in run_state.player.relics


def test_forgotten_altar_sacrifice_numbers():
    run_state = _make_run_state(153)
    _visit_exordium_first(run_state)
    event = ForgottenAltar()
    max_hp = run_state.player.max_hp
    hp = run_state.player.current_hp
    expected_loss = int(round(max_hp * 0.35))

    result = event.choose(run_state, "sacrifice")
    assert result.finished
    assert run_state.player.max_hp == max_hp + 5
    # gain_max_hp(+5 hp) first, then the damage.
    assert run_state.player.current_hp == hp + 5 - expected_loss


def test_forgotten_altar_desecrate_gives_decay():
    run_state = _make_run_state(154)
    _visit_exordium_first(run_state)
    event = ForgottenAltar()
    result = event.choose(run_state, "desecrate")
    assert result.finished
    assert any(card.card_id == CardId.DECAY for card in run_state.player.deck)


# ---------------------------------------------------------------------------
# KnowingSkull
# ---------------------------------------------------------------------------

def test_knowing_skull_requires_13_hp():
    run_state = _make_run_state(161)
    event = KnowingSkull()
    assert event.is_allowed(run_state) is True
    run_state.player.current_hp = 12
    assert event.is_allowed(run_state) is False


def test_knowing_skull_gold_asks_escalate_independently():
    run_state = _make_run_state(162)
    event = KnowingSkull()
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    hp = run_state.player.current_hp
    gold = run_state.player.gold

    result = event.choose(run_state, "gold")
    assert not result.finished
    assert run_state.player.current_hp == hp - 6
    assert run_state.player.gold == gold + 90

    result = event.choose(run_state, "gold")
    assert run_state.player.current_hp == hp - 6 - 7  # escalated by 1
    assert run_state.player.gold == gold + 180
    # Other options are still at their base cost.
    assert "6 damage" in next(
        option.description for option in result.next_options if option.option_id == "potion"
    )


def test_knowing_skull_potion_ask_costs_6_and_offers_potion():
    from sts2_env.run.reward_objects import PotionReward

    run_state = _make_run_state(165)
    event = KnowingSkull()
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    hp = run_state.player.current_hp

    result = event.choose(run_state, "potion")
    assert not result.finished
    assert run_state.player.current_hp == hp - 6
    rewards = result.rewards["reward_objects"]
    assert len(rewards) == 1 and isinstance(rewards[0], PotionReward)
    assert rewards[0].potion_id is not None
    # Escalates independently: next potion ask costs 7.
    assert "7 damage" in next(
        option.description for option in result.next_options if option.option_id == "potion"
    )


def test_knowing_skull_card_gives_uncommon_colorless():
    run_state = _make_run_state(163)
    event = KnowingSkull()
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    deck_before = list(run_state.player.deck)

    result = event.choose(run_state, "card")
    assert not result.finished
    added = [card for card in run_state.player.deck if card not in deck_before]
    assert len(added) == 1
    metadata = card_metadata(added[0].card_id)
    assert metadata.rarity == CardRarity.UNCOMMON


def test_knowing_skull_leave_costs_six():
    run_state = _make_run_state(164)
    event = KnowingSkull()
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    hp = run_state.player.current_hp
    result = event.choose(run_state, "leave")
    assert result.finished
    assert run_state.player.current_hp == hp - 6


# ---------------------------------------------------------------------------
# MaskedBandits
# ---------------------------------------------------------------------------

def test_masked_bandits_is_allowed_gating():
    run_state = _make_run_state(171)
    event = MaskedBandits()
    run_state.total_floor = 22
    assert event.is_allowed(run_state) is False
    run_state.total_floor = 23
    assert event.is_allowed(run_state) is True
    run_state.player.obtain_relic(RelicId.RED_MASK.name)
    assert event.is_allowed(run_state) is False


def test_masked_bandits_pay_loses_all_gold():
    run_state = _make_run_state(172)
    run_state.total_floor = 25
    run_state.player.gold = 137
    event = MaskedBandits()
    result = event.choose(run_state, "pay")
    assert result.finished
    assert run_state.player.gold == 0


def test_masked_bandits_fight_combat_and_rewards():
    mgr = RunManager(seed=555, character_id=IRONCLAD)
    run_state = mgr.run_state
    run_state.total_floor = 25
    event = MaskedBandits()
    _start_event(mgr, event)

    result = mgr._do_event_choice({"option_id": "fight"})
    assert result["phase"] == RunManager.PHASE_COMBAT
    combat = mgr._combat
    assert sorted(enemy.monster_id for enemy in combat.enemies) == [
        "THECITY_BEAR", "THECITY_POINTY", "THECITY_ROMEO",
    ]
    room = mgr._current_room
    assert isinstance(room, CombatRoom)
    assert room.suppress_default_rewards is True
    extra = room.extra_rewards[run_state.player.player_id]
    gold_rewards = [reward for reward in extra if isinstance(reward, GoldReward)]
    assert len(gold_rewards) == 1
    assert (gold_rewards[0].min_gold, gold_rewards[0].max_gold) == (25, 35)
    relic_rewards = [reward for reward in extra if isinstance(reward, RelicReward)]
    assert len(relic_rewards) == 1 and relic_rewards[0].relic_id == RelicId.RED_MASK.name

    _win_current_combat(mgr)
    steps = 0
    while mgr._current_reward is not None and steps < 10:
        mgr._current_reward.select(mgr)
        mgr._advance_post_combat_rewards()
        steps += 1
    assert RelicId.RED_MASK.name in run_state.player.relics


# ---------------------------------------------------------------------------
# Nloth
# ---------------------------------------------------------------------------

def test_nloth_requires_two_tradable_relics():
    run_state = _make_run_state(181)
    event = Nloth()
    assert event.is_allowed(run_state) is False
    run_state.player.obtain_relic(RelicId.ANCHOR.name)
    run_state.player.obtain_relic(RelicId.VAJRA.name)
    assert event.is_allowed(run_state) is True


def test_nloth_trade_swaps_relic_for_nloths_gift():
    run_state = _make_run_state(182)
    run_state.player.obtain_relic(RelicId.ANCHOR.name)
    run_state.player.obtain_relic(RelicId.VAJRA.name)
    event = Nloth()
    options = event.generate_initial_options(run_state)
    assert [option.option_id for option in options] == ["trade_1", "trade_2", "leave"]
    traded_relic = event._choice_relics[0]

    result = event.choose(run_state, "trade_1")
    assert result.finished
    assert traded_relic not in run_state.player.relics
    assert RelicId.NLOTHS_GIFT.name in run_state.player.relics


def test_nloth_leave_keeps_relics():
    run_state = _make_run_state(183)
    run_state.player.obtain_relic(RelicId.ANCHOR.name)
    run_state.player.obtain_relic(RelicId.VAJRA.name)
    event = Nloth()
    event.generate_initial_options(run_state)
    relics_before = list(run_state.player.relics)
    result = event.choose(run_state, "leave")
    assert result.finished
    assert run_state.player.relics == relics_before


# ---------------------------------------------------------------------------
# OldBeggar
# ---------------------------------------------------------------------------

def test_old_beggar_pay_then_remove_card():
    run_state = _make_run_state(191)
    run_state.player.gold = 100
    event = OldBeggar()
    assert event.is_allowed(run_state) is True

    result = event.choose(run_state, "give_gold")
    assert not result.finished
    assert run_state.player.gold == 25
    assert [option.option_id for option in result.next_options] == ["remove_card"]

    deck_size = len(run_state.player.deck)
    result = event.choose(run_state, "remove_card")
    assert event.pending_choice is not None
    result = _resolve_single(event)
    assert result.finished
    assert len(run_state.player.deck) == deck_size - 1


def test_old_beggar_requires_75_gold():
    run_state = _make_run_state(192)
    run_state.player.gold = 74
    assert OldBeggar().is_allowed(run_state) is False


# ---------------------------------------------------------------------------
# PleadingVagrant
# ---------------------------------------------------------------------------

def test_pleading_vagrant_pay_option_locked_below_85():
    run_state = _make_run_state(201)
    run_state.player.gold = 84
    options = PleadingVagrant().generate_initial_options(run_state)
    assert options[0].enabled is False
    assert [option.option_id for option in options] == ["pay", "rob", "leave"]


def test_pleading_vagrant_pay_gives_relic():
    run_state = _make_run_state(202)
    run_state.player.gold = 100
    event = PleadingVagrant()
    relic_count = len(run_state.player.relics)
    result = event.choose(run_state, "pay")
    assert result.finished
    assert run_state.player.gold == 15
    assert len(run_state.player.relics) == relic_count + 1


def test_pleading_vagrant_rob_gives_shame_and_relic():
    run_state = _make_run_state(203)
    event = PleadingVagrant()
    relic_count = len(run_state.player.relics)
    gold = run_state.player.gold
    result = event.choose(run_state, "rob")
    assert result.finished
    assert run_state.player.gold == gold  # rob costs nothing
    assert len(run_state.player.relics) == relic_count + 1
    assert any(card.card_id == CardId.SHAME for card in run_state.player.deck)


# ---------------------------------------------------------------------------
# TheJoust
# ---------------------------------------------------------------------------

def test_the_joust_murderer_bet_win():
    run_state = _make_run_state(211)
    run_state.player.gold = 100
    event = TheJoust()
    event.rng = _StubRng(floats=[0.9])  # 0.9 >= 0.3 -> murderer wins

    result = event.choose(run_state, "continue")
    assert {option.option_id for option in result.next_options} == {"bet_murderer", "bet_owner"}
    event.choose(run_state, "bet_murderer")
    assert run_state.player.gold == 50
    result = event.choose(run_state, "watch")
    assert result.finished
    assert run_state.player.gold == 150  # +100 payout


def test_the_joust_owner_bet_outcomes():
    # Owner wins (roll < 0.3) with an owner bet -> +250.
    run_state = _make_run_state(212)
    run_state.player.gold = 100
    event = TheJoust()
    event.rng = _StubRng(floats=[0.1])
    event.choose(run_state, "continue")
    event.choose(run_state, "bet_owner")
    event.choose(run_state, "watch")
    assert run_state.player.gold == 300

    # Owner loses (roll >= 0.3) with an owner bet -> bet lost.
    run_state = _make_run_state(213)
    run_state.player.gold = 100
    event = TheJoust()
    event.rng = _StubRng(floats=[0.5])
    event.choose(run_state, "continue")
    event.choose(run_state, "bet_owner")
    event.choose(run_state, "watch")
    assert run_state.player.gold == 50


# ---------------------------------------------------------------------------
# TheLibrary
# ---------------------------------------------------------------------------

def test_the_library_read_offers_20_cards_pick_1_not_skippable():
    run_state = _make_run_state(221)
    event = TheLibrary()
    result = event.choose(run_state, "read")
    assert result.finished
    reward = result.rewards["reward_objects"][0]
    assert isinstance(reward, CardReward)
    assert reward.option_count == 20
    assert reward.cards_to_pick == 1
    assert reward.skippable is False


def test_the_library_sleep_heals_20_percent():
    run_state = _make_run_state(222)
    event = TheLibrary()
    run_state.player.current_hp = 10
    expected = int(round(run_state.player.max_hp * 0.2))
    result = event.choose(run_state, "sleep")
    assert result.finished
    assert run_state.player.current_hp == 10 + expected


# ---------------------------------------------------------------------------
# TheMausoleum
# ---------------------------------------------------------------------------

def test_the_mausoleum_open_gives_relic_and_writhe():
    run_state = _make_run_state(231)
    event = TheMausoleum()
    relic_count = len(run_state.player.relics)
    result = event.choose(run_state, "open")
    assert result.finished
    assert len(run_state.player.relics) == relic_count + 1
    assert any(card.card_id == CardId.WRITHE for card in run_state.player.deck)


def test_the_mausoleum_leave_does_nothing():
    run_state = _make_run_state(232)
    event = TheMausoleum()
    relic_count = len(run_state.player.relics)
    deck_size = len(run_state.player.deck)
    result = event.choose(run_state, "leave")
    assert result.finished
    assert len(run_state.player.relics) == relic_count
    assert len(run_state.player.deck) == deck_size


# ---------------------------------------------------------------------------
# TheNest
# ---------------------------------------------------------------------------

def test_the_nest_steal_gains_50_gold():
    run_state = _make_run_state(241)
    gold = run_state.player.gold
    event = TheNest()
    result = event.choose(run_state, "investigate")
    assert {option.option_id for option in result.next_options} == {"steal", "join"}
    result = event.choose(run_state, "steal")
    assert result.finished
    assert run_state.player.gold == gold + 50


def test_the_nest_join_costs_6_hp_and_gives_ritual_dagger():
    run_state = _make_run_state(242)
    hp = run_state.player.current_hp
    event = TheNest()
    event.choose(run_state, "investigate")
    result = event.choose(run_state, "join")
    assert result.finished
    assert run_state.player.current_hp == hp - 6
    assert any(card.card_id == CardId.RITUAL_DAGGER for card in run_state.player.deck)


# ---------------------------------------------------------------------------
# Vampires
# ---------------------------------------------------------------------------

def test_vampires_accept_replaces_strikes_with_five_bites():
    run_state = _make_run_state(251)
    event = Vampires()
    max_hp = run_state.player.max_hp
    expected_loss = math.ceil(max_hp * 0.3)
    non_strikes = [
        card for card in run_state.player.deck
        if not (card.rarity == CardRarity.BASIC and CardTag.STRIKE in card.tags)
    ]

    result = event.choose(run_state, "accept")
    assert result.finished
    assert run_state.player.max_hp == max_hp - expected_loss
    assert not any(
        card.rarity == CardRarity.BASIC and CardTag.STRIKE in card.tags
        for card in run_state.player.deck
    )
    bites = [card for card in run_state.player.deck if card.card_id == CardId.BITE]
    assert len(bites) == 5  # always exactly 5, regardless of strikes removed
    for card in non_strikes:
        assert card in run_state.player.deck


def test_vampires_vial_option_requires_and_consumes_blood_vial():
    run_state = _make_run_state(252)
    event = Vampires()
    options = event.generate_initial_options(run_state)
    assert "vial" not in {option.option_id for option in options}

    run_state.player.obtain_relic(RelicId.BLOOD_VIAL.name)
    event2 = Vampires()
    options = event2.generate_initial_options(run_state)
    assert "vial" in {option.option_id for option in options}

    max_hp = run_state.player.max_hp
    result = event2.choose(run_state, "vial")
    assert result.finished
    assert RelicId.BLOOD_VIAL.name not in run_state.player.relics
    assert run_state.player.max_hp == max_hp  # no max HP cost on the vial path
    assert len([card for card in run_state.player.deck if card.card_id == CardId.BITE]) == 5


def test_vampires_refuse_does_nothing():
    run_state = _make_run_state(253)
    event = Vampires()
    deck_before = list(run_state.player.deck)
    result = event.choose(run_state, "refuse")
    assert result.finished
    assert run_state.player.deck == deck_before


# ---------------------------------------------------------------------------
# Supporting cards: Jax, Bite, RitualDagger
# ---------------------------------------------------------------------------

def _make_combat(seed: int = 7, player_hp: int = 80) -> CombatState:
    return CombatState(
        player_hp=player_hp,
        player_max_hp=player_hp,
        deck=create_ironclad_starter_deck(),
        rng_seed=seed,
        character_id=IRONCLAD,
    )


def _add_enemy(combat: CombatState, hp: int = 30):
    from sts2_env.core.rng import Rng

    creature, ai = create_pointy(Rng(1))
    creature.max_hp = hp
    creature.current_hp = hp
    combat.add_enemy(creature, ai)
    return creature


def test_jax_loses_3_hp_and_gains_2_strength():
    combat = _make_combat()
    _add_enemy(combat)
    card = create_card(CardId.JAX)
    card.owner = combat.player
    hp = combat.player.current_hp
    play_card_effect(card, combat, None)
    assert combat.player.current_hp == hp - 3
    assert combat.player.powers[PowerId.STRENGTH].amount == 2

    upgraded = create_card(CardId.JAX, upgraded=True)
    assert upgraded.effect_vars["strength"] == 3
    assert upgraded.cost == 0 and upgraded.card_type == CardType.SKILL


def test_bite_deals_7_and_heals_2():
    combat = _make_combat(player_hp=80)
    enemy = _add_enemy(combat, hp=30)
    combat.player.current_hp = 50
    card = create_card(CardId.BITE)
    card.owner = combat.player
    play_card_effect(card, combat, enemy)
    assert enemy.current_hp == 23
    assert combat.player.current_hp == 52

    upgraded = create_card(CardId.BITE, upgraded=True)
    assert upgraded.base_damage == 8 and upgraded.effect_vars["heal"] == 3


def test_ritual_dagger_grows_3_on_kill_only():
    combat = _make_combat()
    survivor = _add_enemy(combat, hp=30)
    card = create_card(CardId.RITUAL_DAGGER)
    card.owner = combat.player
    assert "exhaust" in card.keywords

    # Non-lethal hit: no growth.
    play_card_effect(card, combat, survivor)
    assert survivor.current_hp == 15
    assert card.base_damage == 15

    # Lethal hit: permanent +3.
    victim = _add_enemy(combat, hp=10)
    play_card_effect(card, combat, victim)
    assert victim.is_dead
    assert card.base_damage == 18
    assert card.effect_vars["damage"] == 18


def test_ritual_dagger_growth_survives_upgrade_and_upgrade_boosts_increase():
    combat = _make_combat()
    victim = _add_enemy(combat, hp=5)
    card = create_card(CardId.RITUAL_DAGGER)
    card.owner = combat.player
    play_card_effect(card, combat, victim)
    assert card.base_damage == 18

    combat.upgrade_card(card)
    assert card.upgraded
    assert card.base_damage == 18  # self-mutating growth preserved
    assert card.effect_vars["increase"] == 5  # upgrade: +5 per kill


# ---------------------------------------------------------------------------
# Supporting relics: MutagenicStrength, BloodyIdol, Enchiridion, NilrysCodex
# ---------------------------------------------------------------------------

def test_mutagenic_strength_gives_3_temp_strength_at_combat_start():
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=3,
        relics=[RelicId.MUTAGENIC_STRENGTH.name],
        character_id=IRONCLAD,
    )
    _add_enemy(combat, hp=200)
    combat.start_combat()
    assert combat.player.powers[PowerId.MUTAGENIC_STRENGTH].amount == 3
    assert combat.player.powers[PowerId.STRENGTH].amount == 3

    combat.end_player_turn()
    # Temporary: strength reverted at end of the player's turn.
    strength = combat.player.powers.get(PowerId.STRENGTH)
    assert strength is None or strength.amount == 0


def test_bloody_idol_heals_5_on_gold_gain_out_of_combat():
    run_state = _make_run_state(261)
    run_state.player.obtain_relic(RelicId.BLOODY_IDOL.name)
    run_state.player.current_hp = 40
    run_state.player.gain_gold(20)
    assert run_state.player.current_hp == 45
    # No heal on gold loss.
    run_state.player.lose_gold(10)
    assert run_state.player.current_hp == 45


def test_enchiridion_adds_free_power_card_to_hand_on_turn_1():
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=5,
        relics=[RelicId.ENCHIRIDION.name],
        character_id=IRONCLAD,
    )
    _add_enemy(combat, hp=200)
    combat.start_combat()
    generated = [card for card in combat.hand if card.card_type == CardType.POWER]
    assert len(generated) == 1
    power_card = generated[0]
    assert combat.modified_card_cost(combat.player, power_card) == 0
    assert card_metadata(power_card.card_id).can_be_generated_in_combat


def test_nilrys_codex_offers_3_card_choice_at_turn_end_into_draw_pile():
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=9,
        relics=[RelicId.NILRYS_CODEX.name],
        character_id=IRONCLAD,
    )
    _add_enemy(combat, hp=500)
    combat.start_combat()
    combat.end_player_turn()

    choice = combat.pending_choice
    assert choice is not None
    assert len(choice.options) == 3
    assert choice.allow_skip is True
    chosen_id = choice.options[0].card.card_id

    assert combat.resolve_pending_choice(0) is True
    # Resolving the choice resumes the enemy turn and starts round 2, so the
    # generated card may already have been drawn -- check all piles.
    all_cards = [*combat.draw_pile, *combat.hand, *combat.discard_pile]
    assert any(card.card_id == chosen_id for card in all_cards)
    assert combat.pending_choice is None
    assert combat.round_number == 2  # combat flow resumed cleanly


def test_nilrys_codex_choice_can_be_skipped():
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(), rng_seed=11,
        relics=[RelicId.NILRYS_CODEX.name],
        character_id=IRONCLAD,
    )
    _add_enemy(combat, hp=500)
    combat.start_combat()
    draw_and_discard = len(combat.draw_pile) + len(combat.discard_pile) + len(combat.hand)
    combat.end_player_turn()
    assert combat.pending_choice is not None
    assert combat.resolve_pending_choice(None) is True
    assert len(combat.draw_pile) + len(combat.discard_pile) + len(combat.hand) == draw_and_discard


# ---------------------------------------------------------------------------
# Event-only monsters: Pointy, Romeo, Bear
# ---------------------------------------------------------------------------

def _perform_moves(combat: CombatState, ai, count: int) -> list[str]:
    moves = []
    for _ in range(count):
        move = ai.current_move
        moves.append(move.state_id)
        move.perform(combat)
        ai.on_move_performed()
        ai.roll_move(combat.monster_ai_rng)
    return moves


def test_pointy_hp_and_stab_damage():
    from sts2_env.core.rng import Rng

    creature, _ = create_pointy(Rng(3))
    assert creature.max_hp == 30
    tough, _ = create_pointy(Rng(3), ascension_level=8)
    assert tough.max_hp == 34

    combat = _make_combat(player_hp=100)
    creature, ai = create_pointy(Rng(3))
    combat.add_enemy(creature, ai)
    moves = _perform_moves(combat, ai, 2)
    assert moves == ["STAB", "STAB"]
    assert combat.player.current_hp == 100 - 2 * (2 * 5)


def test_romeo_pattern_and_weak():
    from sts2_env.core.rng import Rng

    combat = _make_combat(player_hp=300)
    creature, ai = create_romeo(Rng(4))
    assert 35 <= creature.max_hp <= 39
    combat.add_enemy(creature, ai)

    moves = _perform_moves(combat, ai, 6)
    # MOCK -> AGONIZING -> CROSS -> CROSS -> (two crosses) AGONIZING -> CROSS
    assert moves == [
        "MOCK", "AGONIZING_SLASH", "CROSS_SLASH", "CROSS_SLASH",
        "AGONIZING_SLASH", "CROSS_SLASH",
    ]
    # MOCK deals nothing; 2 agonizing (10) + 3 cross (15).
    assert combat.player.current_hp == 300 - 2 * 10 - 3 * 15
    assert combat.player.powers[PowerId.WEAK].amount > 0


def test_bear_pattern_dexterity_and_block():
    from sts2_env.core.rng import Rng

    combat = _make_combat(player_hp=300)
    creature, ai = create_bear(Rng(5))
    assert 38 <= creature.max_hp <= 42
    combat.add_enemy(creature, ai)

    moves = _perform_moves(combat, ai, 5)
    assert moves == ["BEAR_HUG", "LUNGE", "MAUL", "LUNGE", "MAUL"]
    assert combat.player.powers[PowerId.DEXTERITY].amount == -2
    assert combat.player.current_hp == 300 - 2 * 9 - 2 * 18
    assert creature.block > 0  # lunge grants 9 block


def test_event_monsters_ascension_9_damage_values():
    from sts2_env.core.rng import Rng

    combat = _make_combat(player_hp=300)
    combat.ascension_level = 9
    creature, ai = create_pointy(Rng(6), ascension_level=9)
    combat.add_enemy(creature, ai)
    _perform_moves(combat, ai, 1)
    assert combat.player.current_hp == 300 - 2 * 6

    combat2 = _make_combat(player_hp=300)
    combat2.ascension_level = 9
    bear, bear_ai = create_bear(Rng(6), ascension_level=9)
    combat2.add_enemy(bear, bear_ai)
    _perform_moves(combat2, bear_ai, 1)
    assert combat2.player.powers[PowerId.DEXTERITY].amount == -4
