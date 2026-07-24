"""Tests for the "Acts from the Past" shared shrine events (classic mode).

Covers sts2_env/events/aftp_shared.py (all 15 SharedEvents), the shrine
relics in sts2_env/relics/aftp_shared.py, the shrine-interleave pool
builder and repeatable-shrine semantics in sts2_env/run/events.py.

Follows the conventions of tests/test_events_exordium.py.
"""

from __future__ import annotations

import sts2_env.events  # noqa: F401  (registers all events)

from sts2_env.cards.factory import create_card, eligible_character_cards
from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.cards.status import make_decay, make_guilty, make_regret
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, CardRarity, CardTag, CardType, CombatSide, PowerId, RoomType
from sts2_env.events.aftp_shared import (
    AFTP_NON_SHRINE_SHARED_EVENT_IDS,
    AFTP_ONE_TIME_SHRINE_EVENT_IDS,
    AFTP_REPEATABLE_SHRINE_EVENT_IDS,
    AFTP_SHARED_EVENT_IDS,
    AFTP_SHRINE_EVENT_IDS,
    BonfireSpirits,
    DesignerInSpire,
    Duplicator,
    FaceTrader,
    GoldenShrine,
    Lab,
    MatchAndKeep,
    OminousForge,
    Purifier,
    TheDivineFountain,
    TheWomanInBlue,
    Transmogrifier,
    UpgradeShrine,
    WeMeetAgain,
    WheelOfChange,
)
from sts2_env.events.thebeyond import THEBEYOND_EVENT_IDS
from sts2_env.map.acts import ActConfig
from sts2_env.potions.base import create_potion
from sts2_env.relics.base import RelicId
from sts2_env.relics.registry import create_relic_by_name
from sts2_env.run.events import (
    build_legacy_event_pool,
    event_allowed_in_act,
    get_allowed_events,
    get_event,
)
from sts2_env.run.reward_objects import PotionReward, RelicReward
from sts2_env.run.rooms import RoomVisitContext
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

    def __init__(self, rolls=(), floats=(), choice_indices=()):
        self._rolls = list(rolls)
        self._floats = list(floats)
        self._choice_indices = list(choice_indices)

    def next_int_exclusive(self, low, high):
        return self._rolls.pop(0)

    def next_int(self, low, high):
        return self._rolls.pop(0)

    def next_float(self, upper=1.0):
        if len(self._floats) > 1:
            return self._floats.pop(0)
        return self._floats[0]

    def choice(self, lst):
        if self._choice_indices:
            return lst[self._choice_indices.pop(0)]
        return lst[0]

    def shuffle(self, lst):
        pass


def _choose_card(event, card):
    """Resolve a pending card choice by identity."""
    assert event.pending_choice is not None
    index = next(
        i for i, option in enumerate(event.pending_choice.options)
        if option.card is card
    )
    return event.resolve_pending_choice(index)


# ---------------------------------------------------------------------------
# Registration / classification
# ---------------------------------------------------------------------------

def test_all_15_shared_events_registered_and_tagged():
    assert len(AFTP_SHARED_EVENT_IDS) == 15
    for event_id in AFTP_SHARED_EVENT_IDS:
        event = get_event(event_id)
        assert event is not None, event_id
        assert event.is_shared is True, event_id
        assert event.is_legacy_exclusive is True, event_id


def test_all_15_shared_events_are_shrines():
    # Every one of the mod's SharedEvents implements IShrineEvent.
    assert AFTP_SHRINE_EVENT_IDS == AFTP_SHARED_EVENT_IDS
    assert AFTP_NON_SHRINE_SHARED_EVENT_IDS == []


def test_one_time_vs_repeatable_split_matches_source():
    assert sorted(AFTP_ONE_TIME_SHRINE_EVENT_IDS) == sorted([
        "BonfireSpirits", "Duplicator", "FaceTrader", "Lab", "OminousForge",
        "TheDivineFountain", "TheWomanInBlue", "WeMeetAgain", "DesignerInSpire",
    ])
    assert sorted(AFTP_REPEATABLE_SHRINE_EVENT_IDS) == sorted([
        "GoldenShrine", "MatchAndKeep", "Purifier", "Transmogrifier",
        "UpgradeShrine", "WheelOfChange",
    ])


def test_shared_shrines_blocked_outside_legacy_acts():
    legacy_act = ActConfig(act_index=0, num_rooms=1, is_legacy=True)
    vanilla_act = ActConfig(act_index=0, num_rooms=1, is_legacy=False)
    for event_id in AFTP_SHARED_EVENT_IDS:
        event = get_event(event_id)
        assert event_allowed_in_act(event, legacy_act) is True, event_id
        assert event_allowed_in_act(event, vanilla_act) is False, event_id


def test_act_restrictions():
    assert get_event("FaceTrader").allowed_act_numbers == (1, 2)
    assert get_event("DesignerInSpire").allowed_act_numbers == (2, 3)
    for event_id in AFTP_SHARED_EVENT_IDS:
        if event_id not in ("FaceTrader", "DesignerInSpire"):
            assert get_event(event_id).allowed_act_numbers is None, event_id


# ---------------------------------------------------------------------------
# Shrine interleave pool builder (ShrinePatches.EventPoolPatch)
# ---------------------------------------------------------------------------

def test_legacy_pool_low_rolls_front_load_shrines():
    act = ActConfig(act_index=2, num_rooms=1, is_legacy=True, act_id="TheBeyond")
    pool = build_legacy_event_pool(
        THEBEYOND_EVENT_IDS + AFTP_SHARED_EVENT_IDS, act, _ScriptedRng(floats=[0.1]),
    )
    # act 3 slot: FaceTrader (acts 1-2) is filtered out; everything else is in.
    assert "FaceTrader" not in pool
    assert "DesignerInSpire" in pool
    assert len(pool) == len(THEBEYOND_EVENT_IDS) + len(AFTP_SHARED_EVENT_IDS) - 1
    # With every roll < 0.25, all shrines come first (SecretPortal is
    # TheBeyond's own shrine, then the 14 eligible shared shrines).
    shrine_count = 15
    assert all(get_event(eid).is_shrine for eid in pool[:shrine_count])
    assert all(not get_event(eid).is_shrine for eid in pool[shrine_count:])


def test_legacy_pool_high_rolls_append_shrines_after_regulars():
    act = ActConfig(act_index=0, num_rooms=1, is_legacy=True, act_id="Exordium")
    pool = build_legacy_event_pool(
        THEBEYOND_EVENT_IDS + AFTP_SHARED_EVENT_IDS, act, _ScriptedRng(floats=[0.9]),
    )
    # act 1 slot: FaceTrader is in, DesignerInSpire (acts 2-3) is out.
    assert "FaceTrader" in pool
    assert "DesignerInSpire" not in pool
    regular_count = len([eid for eid in pool if not get_event(eid).is_shrine])
    assert all(not get_event(eid).is_shrine for eid in pool[:regular_count])
    assert all(get_event(eid).is_shrine for eid in pool[regular_count:])


def test_legacy_pool_interleaves_on_25_percent_rolls():
    act = ActConfig(act_index=2, num_rooms=1, is_legacy=True, act_id="TheBeyond")
    # First slot rolls a shrine (0.2 < 0.25), all later slots roll regular.
    floats = [0.2] + [0.9] * 40
    pool = build_legacy_event_pool(
        THEBEYOND_EVENT_IDS + AFTP_SHARED_EVENT_IDS, act, _ScriptedRng(floats=floats),
    )
    assert get_event(pool[0]).is_shrine is True
    assert get_event(pool[1]).is_shrine is False


def test_vanilla_act_pool_drops_all_legacy_shared_events():
    vanilla_act = ActConfig(act_index=2, num_rooms=1, is_legacy=False)
    pool = build_legacy_event_pool(
        THEBEYOND_EVENT_IDS + AFTP_SHARED_EVENT_IDS, vanilla_act, _ScriptedRng(floats=[0.9]),
    )
    for event_id in AFTP_SHARED_EVENT_IDS:
        assert event_id not in pool
    # SecretPortal / MindBloom / MysteriousSphere are shared -> also dropped.
    assert "SecretPortal" not in pool
    assert "MindBloom" not in pool


# ---------------------------------------------------------------------------
# Repeatable shrine semantics (RepeatableShrineValidityPatch)
# ---------------------------------------------------------------------------

def test_repeatable_shrines_bypass_visited_exclusion():
    run_state = _make_run_state()
    run_state.acts[0] = ActConfig(act_index=0, num_rooms=1, is_legacy=True, act_id="Exordium")
    run_state.visited_event_ids.add("UpgradeShrine")   # repeatable shrine
    run_state.visited_event_ids.add("Lab")             # one-time shrine

    allowed = get_allowed_events(run_state, pool=["UpgradeShrine", "Lab"])
    allowed_ids = [event.event_id for event in allowed]
    assert "UpgradeShrine" in allowed_ids
    assert "Lab" not in allowed_ids


def test_one_time_non_shrine_events_still_excluded_when_visited():
    run_state = _make_run_state()
    run_state.acts[0] = ActConfig(act_index=0, num_rooms=1, is_legacy=True, act_id="Exordium")
    run_state.visited_event_ids.add("MindBloom")
    allowed = get_allowed_events(run_state, pool=["MindBloom"])
    assert allowed == []


# ---------------------------------------------------------------------------
# BonfireSpirits
# ---------------------------------------------------------------------------

def _bonfire_offer(run_state, card):
    event = BonfireSpirits()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    result = event.choose(run_state, "offer")
    assert not result.finished
    return _choose_card(event, card)


def test_bonfire_spirits_curse_offering_grants_spirit_poop():
    run_state = _make_run_state()
    curse = make_regret()
    run_state.player.deck.append(curse)
    deck_before = len(run_state.player.deck)

    result = _bonfire_offer(run_state, curse)
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1
    assert RelicId.SPIRIT_POOP.name in run_state.player.relics


def test_bonfire_spirits_basic_offering_does_nothing():
    run_state = _make_run_state()
    run_state.player.current_hp = 10
    basic = next(c for c in run_state.player.deck if c.rarity == CardRarity.BASIC)
    result = _bonfire_offer(run_state, basic)
    assert result.finished
    assert run_state.player.current_hp == 10
    assert RelicId.SPIRIT_POOP.name not in run_state.player.relics


def test_bonfire_spirits_common_offering_heals_5():
    run_state = _make_run_state()
    run_state.player.current_hp = 10
    common_id = eligible_character_cards("Ironclad", rarity=CardRarity.COMMON)[0]
    common = create_card(common_id)
    run_state.player.deck.append(common)
    result = _bonfire_offer(run_state, common)
    assert result.finished
    assert run_state.player.current_hp == 15


def test_bonfire_spirits_uncommon_offering_fully_heals():
    run_state = _make_run_state()
    run_state.player.current_hp = 10
    uncommon_id = eligible_character_cards("Ironclad", rarity=CardRarity.UNCOMMON)[0]
    uncommon = create_card(uncommon_id)
    run_state.player.deck.append(uncommon)
    result = _bonfire_offer(run_state, uncommon)
    assert result.finished
    assert run_state.player.current_hp == run_state.player.max_hp


def test_bonfire_spirits_rare_offering_gains_10_max_hp_and_fully_heals():
    run_state = _make_run_state()
    max_before = run_state.player.max_hp
    run_state.player.current_hp = 10
    rare_id = eligible_character_cards("Ironclad", rarity=CardRarity.RARE)[0]
    rare = create_card(rare_id)
    run_state.player.deck.append(rare)
    result = _bonfire_offer(run_state, rare)
    assert result.finished
    assert run_state.player.max_hp == max_before + 10
    assert run_state.player.current_hp == run_state.player.max_hp


# ---------------------------------------------------------------------------
# Duplicator
# ---------------------------------------------------------------------------

def test_duplicator_pray_duplicates_the_chosen_card():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    target = run_state.player.deck[0]
    event = Duplicator()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "pray")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert len(run_state.player.deck) == deck_before + 1
    assert sum(1 for c in run_state.player.deck if c.card_id == target.card_id) >= 2


# ---------------------------------------------------------------------------
# FaceTrader
# ---------------------------------------------------------------------------

def test_face_trader_touch_damages_and_pays_50_gold():
    run_state = _make_run_state(gold=0)
    run_state.player.max_hp = 75
    run_state.player.current_hp = 75
    event = FaceTrader()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")

    result = event.choose(run_state, "touch")
    assert result.finished
    assert run_state.player.current_hp == 75 - 7  # max(1, 75 // 10)
    assert run_state.player.gold == 50


def test_face_trader_trade_grants_an_unowned_face_relic():
    run_state = _make_run_state()
    event = FaceTrader()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")

    result = event.choose(run_state, "trade")
    assert result.finished
    faces = {
        RelicId.CULTIST_HEADPIECE.name, RelicId.FACE_OF_CLERIC.name,
        RelicId.GREMLIN_VISAGE.name, RelicId.NLOTHS_HUNGRY_FACE.name,
        RelicId.SSSERPENT_HEAD.name,
    }
    assert faces & set(run_state.player.relics)


def test_face_trader_gives_circlet_when_all_faces_owned():
    run_state = _make_run_state()
    for relic_id in (
        RelicId.CULTIST_HEADPIECE, RelicId.FACE_OF_CLERIC, RelicId.GREMLIN_VISAGE,
        RelicId.NLOTHS_HUNGRY_FACE, RelicId.SSSERPENT_HEAD,
    ):
        run_state.player.obtain_relic(relic_id.name)
    event = FaceTrader()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "continue")
    result = event.choose(run_state, "trade")
    assert result.finished
    assert RelicId.CIRCLET.name in run_state.player.relics


# ---------------------------------------------------------------------------
# GoldenShrine
# ---------------------------------------------------------------------------

def test_golden_shrine_pray_and_desecrate():
    run_state = _make_run_state(gold=0)
    event = GoldenShrine()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "pray")
    assert result.finished
    assert run_state.player.gold == 50

    run_state2 = _make_run_state(seed=43, gold=0)
    event2 = GoldenShrine()
    event2.reset_rng_for_run(run_state2)
    event2.generate_initial_options(run_state2)
    result = event2.choose(run_state2, "desecrate")
    assert result.finished
    assert run_state2.player.gold == 275
    assert any(c.card_id == CardId.REGRET for c in run_state2.player.deck)


# ---------------------------------------------------------------------------
# Lab
# ---------------------------------------------------------------------------

def test_lab_search_grants_exactly_2_potion_rewards():
    run_state = _make_run_state()
    event = Lab()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "search")
    assert result.finished
    rewards = result.rewards["reward_objects"]
    assert len(rewards) == 2
    assert all(isinstance(r, PotionReward) for r in rewards)


# ---------------------------------------------------------------------------
# MatchAndKeep
# ---------------------------------------------------------------------------

def test_match_and_keep_board_composition():
    run_state = _make_run_state()
    event = MatchAndKeep()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    cards = event._cards
    assert len(cards) == 12
    # Every card id appears an even number of times (pairs).
    by_id = {}
    for card in cards:
        by_id[card.card_id] = by_id.get(card.card_id, 0) + 1
    assert all(count % 2 == 0 for count in by_id.values())
    # 2 curse pairs -> 4 curse-type cards.
    assert sum(1 for c in cards if c.card_type == CardType.CURSE) == 4
    # 1 rare / 1 uncommon / 1 common character pair.
    assert sum(1 for c in cards if c.rarity == CardRarity.RARE) == 2
    assert sum(1 for c in cards if c.rarity == CardRarity.UNCOMMON) == 2
    assert sum(1 for c in cards if c.rarity == CardRarity.COMMON) == 2
    # Basic pair excludes Strikes/Defends.
    basics = [c for c in cards if c.rarity == CardRarity.BASIC]
    assert len(basics) == 2
    for card in basics:
        assert CardTag.STRIKE not in card.tags
        assert CardTag.DEFEND not in card.tags


def test_match_and_keep_matching_pair_adds_card_and_uses_attempt():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    event = MatchAndKeep()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "play")

    # Find the rare pair (guaranteed exactly 2 copies of that id).
    rare_indices = [i for i, c in enumerate(event._cards) if c.rarity == CardRarity.RARE]
    assert len(rare_indices) == 2
    first, second = rare_indices

    mid = event.choose(run_state, f"flip_{first}")
    assert not mid.finished
    # The first flip stays face-up: it can't be flipped again this attempt.
    assert f"flip_{first}" not in [o.option_id for o in mid.next_options]

    result = event.choose(run_state, f"flip_{second}")
    assert not result.finished
    assert event._matches == 1
    assert event._attempts_left == 4
    assert len(run_state.player.deck) == deck_before + 1
    # Matched cards can no longer be flipped.
    option_ids = [o.option_id for o in result.next_options]
    assert f"flip_{first}" not in option_ids
    assert f"flip_{second}" not in option_ids


def test_match_and_keep_mismatch_uses_attempt_without_reward():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    event = MatchAndKeep()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "play")

    rare_index = next(i for i, c in enumerate(event._cards) if c.rarity == CardRarity.RARE)
    other_index = next(
        i for i, c in enumerate(event._cards)
        if c.card_id != event._cards[rare_index].card_id
    )
    event.choose(run_state, f"flip_{rare_index}")
    result = event.choose(run_state, f"flip_{other_index}")
    assert not result.finished
    assert event._matches == 0
    assert event._attempts_left == 4
    assert len(run_state.player.deck) == deck_before


def test_match_and_keep_ends_after_5_attempts():
    run_state = _make_run_state()
    event = MatchAndKeep()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "play")

    result = None
    for _ in range(5):
        # Always flip two mismatched cards (attempt burned, no match).
        options = event._flip_options()
        first_index = int(options[0].option_id.split("_")[1])
        second_index = next(
            int(o.option_id.split("_")[1]) for o in options[1:]
            if event._cards[int(o.option_id.split("_")[1])].card_id
            != event._cards[first_index].card_id
        )
        event.choose(run_state, f"flip_{first_index}")
        result = event.choose(run_state, f"flip_{second_index}")
    assert result is not None and result.finished
    assert event._attempts_left == 0


def test_match_and_keep_labels_reveal_previously_seen_cards():
    run_state = _make_run_state()
    event = MatchAndKeep()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "play")

    options = event._flip_options()
    assert all("face down" in o.label for o in options)
    first_index = int(options[0].option_id.split("_")[1])
    second_index = next(
        int(o.option_id.split("_")[1]) for o in options[1:]
        if event._cards[int(o.option_id.split("_")[1])].card_id
        != event._cards[first_index].card_id
    )
    event.choose(run_state, f"flip_{first_index}")
    event.choose(run_state, f"flip_{second_index}")
    labels = {o.option_id: o.label for o in event._flip_options()}
    assert event._cards[first_index].card_id.name in labels[f"flip_{first_index}"]
    assert event._cards[second_index].card_id.name in labels[f"flip_{second_index}"]


# ---------------------------------------------------------------------------
# OminousForge
# ---------------------------------------------------------------------------

def test_ominous_forge_forge_upgrades_the_chosen_card():
    run_state = _make_run_state()
    target = run_state.player.deck[0]
    event = OminousForge()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "forge")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert target.upgraded is True


def test_ominous_forge_rummage_grants_warped_tongs_and_pain():
    run_state = _make_run_state()
    event = OminousForge()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "rummage")
    assert result.finished
    assert RelicId.WARPED_TONGS.name in run_state.player.relics
    assert any(c.card_id == CardId.PAIN for c in run_state.player.deck)


# ---------------------------------------------------------------------------
# Purifier / Transmogrifier / UpgradeShrine
# ---------------------------------------------------------------------------

def test_purifier_pray_removes_the_chosen_card():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    target = run_state.player.removable_deck_cards()[0]
    event = Purifier()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "pray")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1


def test_transmogrifier_pray_transforms_the_chosen_card():
    run_state = _make_run_state()
    target = run_state.player.deck[0]
    original_id = target.card_id
    event = Transmogrifier()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "pray")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert target.card_id != original_id


def test_upgrade_shrine_pray_upgrades_the_chosen_card():
    run_state = _make_run_state()
    target = run_state.player.upgradable_deck_cards()[0]
    event = UpgradeShrine()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "pray")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert target.upgraded is True


# ---------------------------------------------------------------------------
# TheDivineFountain
# ---------------------------------------------------------------------------

def test_divine_fountain_gated_on_a_removable_non_guilty_curse():
    run_state = _make_run_state()
    event = TheDivineFountain()
    assert event.is_allowed(run_state) is False
    run_state.player.deck.append(make_guilty())
    assert event.is_allowed(run_state) is False  # Guilty alone doesn't count
    run_state.player.deck.append(make_decay())
    assert event.is_allowed(run_state) is True


def test_divine_fountain_drink_removes_all_removable_curses():
    run_state = _make_run_state()
    run_state.player.deck.append(make_decay())
    run_state.player.deck.append(make_regret())
    event = TheDivineFountain()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "drink")
    assert result.finished
    assert not any(c.card_type == CardType.CURSE for c in run_state.player.deck)


# ---------------------------------------------------------------------------
# TheWomanInBlue
# ---------------------------------------------------------------------------

def test_woman_in_blue_gated_on_50_gold():
    run_state = _make_run_state(gold=49)
    assert TheWomanInBlue().is_allowed(run_state) is False
    run_state.player.gold = 50
    assert TheWomanInBlue().is_allowed(run_state) is True


def test_woman_in_blue_buy_potions_at_escalating_cost():
    for option_id, cost, count in (("buy_1", 20, 1), ("buy_2", 30, 2), ("buy_3", 40, 3)):
        run_state = _make_run_state(gold=100)
        event = TheWomanInBlue()
        event.reset_rng_for_run(run_state)
        event.generate_initial_options(run_state)
        result = event.choose(run_state, option_id)
        assert result.finished
        assert run_state.player.gold == 100 - cost
        rewards = result.rewards["reward_objects"]
        assert len(rewards) == count
        assert all(isinstance(r, PotionReward) for r in rewards)


def test_woman_in_blue_leaving_costs_5_percent_max_hp_rounded_up():
    run_state = _make_run_state()
    run_state.player.max_hp = 81
    run_state.player.current_hp = 81
    event = TheWomanInBlue()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "leave")
    assert result.finished
    assert run_state.player.current_hp == 81 - 5  # ceil(81 * 0.05) == 5


# ---------------------------------------------------------------------------
# WeMeetAgain
# ---------------------------------------------------------------------------

def test_we_meet_again_gold_amount_rolls_between_50_and_150():
    run_state = _make_run_state(gold=1000)
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    assert 50 <= event._gold_amount <= 150


def test_we_meet_again_give_gold_pays_a_relic():
    run_state = _make_run_state(gold=100)
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    relics_before = len(run_state.player.relics)
    gold_given = event._gold_amount
    assert 50 <= gold_given <= 100

    result = event.choose(run_state, "give_gold")
    assert result.finished
    assert run_state.player.gold == 100 - gold_given
    assert len(run_state.player.relics) == relics_before + 1


def test_we_meet_again_gold_option_locked_below_50_gold():
    run_state = _make_run_state(gold=49)
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    gold_option = next(o for o in options if o.option_id == "give_gold")
    assert gold_option.enabled is False


def test_we_meet_again_give_card_removes_a_non_basic_card():
    run_state = _make_run_state()
    rare = create_card(eligible_character_cards("Ironclad", rarity=CardRarity.RARE)[0])
    run_state.player.deck.append(rare)
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    given = event._card
    assert given is rare  # only non-basic, non-curse card in the deck

    result = event.choose(run_state, "give_card")
    assert result.finished
    assert rare not in run_state.player.deck
    assert len(run_state.player.relics) == 1


def test_we_meet_again_give_potion_discards_the_potion_for_a_relic():
    run_state = _make_run_state()
    run_state.player.add_potion(create_potion("BlockPotion"))
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "give_potion").enabled

    result = event.choose(run_state, "give_potion")
    assert result.finished
    assert len(run_state.player.potions) == 0
    assert len(run_state.player.relics) == 1


def test_we_meet_again_attack_does_nothing():
    run_state = _make_run_state(gold=100)
    event = WeMeetAgain()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "attack")
    assert result.finished
    assert run_state.player.gold == 100
    assert run_state.player.relics == []


# ---------------------------------------------------------------------------
# WheelOfChange
# ---------------------------------------------------------------------------

def _spin_wheel(run_state, result_index):
    event = WheelOfChange()
    event.rng = _ScriptedRng(rolls=[result_index])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)
    spin = event.choose(run_state, "play")
    assert not spin.finished
    assert [o.option_id for o in spin.next_options] == ["claim"]
    return event, event.choose(run_state, "claim")


def test_wheel_of_change_gold_scales_with_act():
    run_state = _make_run_state(gold=0)
    _, result = _spin_wheel(run_state, 0)
    assert result.finished
    assert run_state.player.gold == 100  # act slot 0

    run_state2 = _make_run_state(seed=43, gold=0)
    run_state2.current_act_index = 2
    _, result2 = _spin_wheel(run_state2, 0)
    assert run_state2.player.gold == 300


def test_wheel_of_change_relic_result_offers_a_relic_reward():
    run_state = _make_run_state()
    _, result = _spin_wheel(run_state, 1)
    assert result.finished
    rewards = result.rewards["reward_objects"]
    assert len(rewards) == 1 and isinstance(rewards[0], RelicReward)


def test_wheel_of_change_heal_result_fully_heals():
    run_state = _make_run_state()
    run_state.player.current_hp = 5
    _, result = _spin_wheel(run_state, 2)
    assert run_state.player.current_hp == run_state.player.max_hp


def test_wheel_of_change_curse_result_adds_decay():
    run_state = _make_run_state()
    _, result = _spin_wheel(run_state, 3)
    assert any(c.card_id == CardId.DECAY for c in run_state.player.deck)


def test_wheel_of_change_remove_result_removes_a_chosen_card():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    event, result = _spin_wheel(run_state, 4)
    assert not result.finished
    target = event.pending_choice.options[0].card
    result = _choose_card(event, target)
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1


def test_wheel_of_change_damage_result_hits_for_15_percent_truncated():
    run_state = _make_run_state()
    run_state.player.max_hp = 77
    run_state.player.current_hp = 77
    _, result = _spin_wheel(run_state, 5)
    assert result.finished
    assert run_state.player.current_hp == 77 - int(77 * 0.15)  # -11


# ---------------------------------------------------------------------------
# DesignerInSpire
# ---------------------------------------------------------------------------

def test_designer_gated_on_75_gold():
    run_state = _make_run_state(gold=74)
    assert DesignerInSpire().is_allowed(run_state) is False
    run_state.player.gold = 75
    assert DesignerInSpire().is_allowed(run_state) is True


def _designer_with_flips(run_state, adjust_roll, cleanup_roll):
    event = DesignerInSpire()
    event.rng = _ScriptedRng(rolls=[adjust_roll, cleanup_roll])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)
    result = event.choose(run_state, "continue")
    assert not result.finished
    return event, result


def test_designer_adjust_upgrade_one_chosen_card():
    run_state = _make_run_state(gold=200)
    event, _ = _designer_with_flips(run_state, adjust_roll=0, cleanup_roll=0)
    target = run_state.player.upgradable_deck_cards()[0]
    result = event.choose(run_state, "adjust")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert run_state.player.gold == 150
    assert target.upgraded is True


def test_designer_adjust_upgrade_two_random_cards():
    run_state = _make_run_state(gold=200)
    event, _ = _designer_with_flips(run_state, adjust_roll=1, cleanup_roll=0)
    result = event.choose(run_state, "adjust")
    assert result.finished
    assert run_state.player.gold == 150
    assert sum(1 for c in run_state.player.deck if c.upgraded) == 2


def test_designer_cleanup_remove_one_chosen_card():
    run_state = _make_run_state(gold=200)
    deck_before = len(run_state.player.deck)
    event, _ = _designer_with_flips(run_state, adjust_roll=0, cleanup_roll=0)
    target = run_state.player.removable_deck_cards()[0]
    result = event.choose(run_state, "cleanup")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert run_state.player.gold == 125
    assert len(run_state.player.deck) == deck_before - 1


def test_designer_cleanup_transform_two_chosen_cards():
    run_state = _make_run_state(gold=200)
    event, _ = _designer_with_flips(run_state, adjust_roll=0, cleanup_roll=1)
    result = event.choose(run_state, "cleanup")
    assert not result.finished
    # Multi-select: toggle two cards, then confirm.
    assert event.pending_choice is not None
    original_ids = [option.card.card_id for option in event.pending_choice.options[:2]]
    event.resolve_pending_choice(0)
    event.resolve_pending_choice(1)
    result = event.resolve_pending_choice(None)
    assert result.finished
    assert run_state.player.gold == 125


def test_designer_full_service_removes_chosen_and_upgrades_random():
    run_state = _make_run_state(gold=200)
    deck_before = len(run_state.player.deck)
    event, _ = _designer_with_flips(run_state, adjust_roll=0, cleanup_roll=0)
    target = run_state.player.removable_deck_cards()[0]
    result = event.choose(run_state, "full_service")
    assert not result.finished
    result = _choose_card(event, target)
    assert result.finished
    assert run_state.player.gold == 90
    assert len(run_state.player.deck) == deck_before - 1
    assert sum(1 for c in run_state.player.deck if c.upgraded) == 1


def test_designer_punch_costs_5_hp():
    run_state = _make_run_state(gold=200)
    hp_before = run_state.player.current_hp
    event, _ = _designer_with_flips(run_state, adjust_roll=0, cleanup_roll=0)
    result = event.choose(run_state, "punch")
    assert result.finished
    assert run_state.player.current_hp == hp_before - 5
    assert run_state.player.gold == 200


# ---------------------------------------------------------------------------
# Shrine relic behaviors
# ---------------------------------------------------------------------------

def _make_combat(relics=None):
    return CombatState(
        player_hp=60,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=9,
        character_id="Ironclad",
        relics=relics or [],
    )


def test_warped_tongs_upgrades_a_random_hand_card_each_turn():
    from sts2_env.core.rng import Rng
    from sts2_env.encounters.events import get_event_encounter_setup

    combat = _make_combat(relics=[RelicId.WARPED_TONGS.name])
    get_event_encounter_setup("two_orb_walkers_event")(combat, Rng(9))
    combat.start_combat()
    combat.hand = list(combat.draw_pile[:5])
    relic = next(r for r in combat.relics if r.relic_id == RelicId.WARPED_TONGS)
    assert not any(card.upgraded for card in combat.hand)

    relic.after_side_turn_start(combat.player, CombatSide.PLAYER, combat)
    assert sum(1 for card in combat.hand if card.upgraded) == 1


def test_gremlin_visage_starts_combat_with_1_weak():
    combat = _make_combat(relics=[RelicId.GREMLIN_VISAGE.name])
    combat.start_combat()
    assert combat.player.get_power_amount(PowerId.WEAK) == 1


def test_face_of_cleric_gains_1_max_hp_after_combat():
    combat = _make_combat(relics=[RelicId.FACE_OF_CLERIC.name])
    relic = next(r for r in combat.relics if r.relic_id == RelicId.FACE_OF_CLERIC)
    max_before = combat.player.max_hp
    relic.after_combat_end(combat.player, combat)
    assert combat.player.max_hp == max_before + 1


def test_nloths_hungry_face_blanks_the_first_treasure_room():
    run_state = _make_run_state()
    relic = create_relic_by_name(RelicId.NLOTHS_HUNGRY_FACE.name)
    player = run_state.player
    context = RoomVisitContext(RoomType.TREASURE)

    relic.after_room_entered(player, context)
    assert relic.should_generate_treasure(player) is False
    assert relic.is_used_up is True

    relic.after_room_entered(player, context)
    assert relic.should_generate_treasure(player) is None


def test_ssserpent_head_pays_50_gold_on_unknown_room_entry():
    run_state = _make_run_state(gold=0)
    relic = create_relic_by_name(RelicId.SSSERPENT_HEAD.name)

    class _UnknownContext:
        is_unknown = True

    relic.after_room_entered(run_state.player, _UnknownContext())
    assert run_state.player.gold == 50

    class _CombatContext:
        is_unknown = False

    relic.after_room_entered(run_state.player, _CombatContext())
    assert run_state.player.gold == 50  # unchanged


def test_marker_relics_are_registered():
    assert create_relic_by_name(RelicId.SPIRIT_POOP.name) is not None
    assert create_relic_by_name(RelicId.CULTIST_HEADPIECE.name) is not None
