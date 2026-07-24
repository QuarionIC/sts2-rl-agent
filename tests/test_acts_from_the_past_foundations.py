"""Unit tests for the "Acts from the Past" mod's foundational/global pieces.

Covers only the pieces implemented in this task (see the module docstrings
on the relics/cards themselves for decompiled-source references):
  1. Odd Mushroom, Necronomicon, Necronomicurse (real global mechanics)
  2. NlothsGift's card-rarity-odds hook
  3. The act-slot-candidates extension point
  4. SharedEvents legacy-act filtering config flags + filter function

Does NOT cover: the legacy acts' own monsters/events/content (future task).
"""

from __future__ import annotations

import pytest

import sts2_env.powers  # noqa: F401  (ensure power registration happens)

import sts2_env.map.acts as acts_module
import sts2_env.relics.shop_event as shop_event_module
import sts2_env.run.events as events_module

from sts2_env.cards.factory import create_card, create_transform_card
from sts2_env.cards.ironclad import make_perfected_strike as _make_perfected_strike_card
from sts2_env.cards.ironclad_basic import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.constants import (
    ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS,
    ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS,
)
from sts2_env.core.enums import CardId, CardRarity, PowerId, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.map.acts import (
    ACT_0,
    ACT_1,
    ACT_2,
    ACT_3,
    NUM_ACT_SLOTS,
    ActConfig,
    act_candidates_for_slot,
    register_act_candidate,
    select_act_for_slot,
)
from sts2_env.monsters.act1_weak import create_shrinker_beetle
from sts2_env.relics.base import RelicId
from sts2_env.relics.registry import create_relic_by_name
from sts2_env.run.events import EventModel, all_events, event_allowed_in_act
from sts2_env.run.odds import CardRarityOdds
from sts2_env.run.run_state import RunState


def _make_ironclad_combat(relics: list[str] | None = None, *, seed: int = 5001) -> CombatState:
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=seed,
        character_id="Ironclad",
        relics=relics or [],
    )
    creature, ai = create_shrinker_beetle(Rng(seed))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    return combat


def _clean_perfected_strike():
    """Perfected Strike with the Strike tag stripped so its own damage
    formula (6 + 2*strike_count) stays a flat 6 regardless of how many
    copies are in hand/discard -- keeps Necronomicon's double-play math
    simple and independent of card-pool composition."""
    card = _make_perfected_strike_card()
    card.tags = frozenset()
    return card


class _FixedFloatRng:
    """Minimal Rng stand-in exposing only `next_float()`."""

    def __init__(self, value: float):
        self._value = value

    def next_float(self, upper: float = 1.0) -> float:
        return self._value * upper


# ===========================================================================
# 1a. Odd Mushroom
# ===========================================================================

class TestOddMushroom:
    def test_reduces_vulnerable_multiplier_by_flat_quarter(self):
        combat = _make_ironclad_combat(["OddMushroom"], seed=5101)
        player = combat.player
        enemy = combat.enemies[0]
        player.max_hp = player.current_hp = 500
        player.apply_power(PowerId.VULNERABLE, 1)

        hp_before = player.current_hp
        combat.deal_damage(dealer=enemy, target=player, amount=100, props=ValueProp.MOVE)

        assert hp_before - player.current_hp == 125  # floor(100 * 1.25)

    def test_vanilla_vulnerable_without_relic_is_unaffected(self):
        combat = _make_ironclad_combat([], seed=5101)
        player = combat.player
        enemy = combat.enemies[0]
        player.max_hp = player.current_hp = 500
        player.apply_power(PowerId.VULNERABLE, 1)

        hp_before = player.current_hp
        combat.deal_damage(dealer=enemy, target=player, amount=100, props=ValueProp.MOVE)

        assert hp_before - player.current_hp == 150  # floor(100 * 1.5), vanilla

    def test_no_reduction_when_target_is_not_vulnerable(self):
        combat = _make_ironclad_combat(["OddMushroom"], seed=5101)
        player = combat.player
        enemy = combat.enemies[0]
        player.max_hp = player.current_hp = 500

        hp_before = player.current_hp
        combat.deal_damage(dealer=enemy, target=player, amount=100, props=ValueProp.MOVE)

        assert hp_before - player.current_hp == 100  # unmodified

    def test_reduction_never_drops_the_multiplier_below_one(self, monkeypatch):
        """Vanilla Vulnerable is always >= x1.5, so exercise the floor directly
        by shrinking the constant the relic reads its multiplier from."""
        monkeypatch.setattr(shop_event_module, "VULNERABLE_MULTIPLIER", 1.1)
        relic = shop_event_module.OddMushroom(RelicId.ODD_MUSHROOM)
        combat = _make_ironclad_combat([], seed=5102)
        player = combat.player
        player.apply_power(PowerId.VULNERABLE, 1)

        factor = relic.modify_damage_multiplicative(
            player, dealer=None, target=player, props=ValueProp.MOVE,
        )

        # 1.1 - 0.25 = 0.85 -> floored to 1.0. Returned factor is relative to
        # the (patched) base multiplier: 1.0 / 1.1.
        assert factor == pytest.approx(1.0 / 1.1)
        assert 1.1 * factor == pytest.approx(1.0)


# ===========================================================================
# 1b. Necronomicon + Necronomicurse
# ===========================================================================

class TestNecronomicon:
    def test_adds_necronomicurse_to_deck_on_pickup(self):
        run_state = RunState(seed=5201, character_id="Ironclad")
        before = sum(1 for card in run_state.player.deck if card.card_id == CardId.NECRONOMICURSE)

        assert run_state.player.obtain_relic("Necronomicon")

        after = sum(1 for card in run_state.player.deck if card.card_id == CardId.NECRONOMICURSE)
        assert after == before + 1

    def test_doubles_first_qualifying_attack_each_turn_only(self):
        # Perfected Strike (cost 2, flat 6 damage with no Strike cards
        # anywhere in piles) avoids side effects like Bash/Uppercut's own
        # Vulnerable application, which would otherwise compound across the
        # replay and make the expected damage math depend on card ordering.
        combat = _make_ironclad_combat(["Necronomicon"], seed=5202)
        enemy = combat.enemies[0]
        enemy.max_hp = enemy.current_hp = 500
        combat.draw_pile = []
        combat.discard_pile = []
        combat.exhaust_pile = []
        combat.hand = [_clean_perfected_strike(), _clean_perfected_strike()]
        for card in combat.hand:
            card.owner = combat.player
        combat.energy = 4  # 2x Perfected Strike @ cost 2

        hp_before = enemy.current_hp
        assert combat.play_card(0, 0)
        first_play_damage = hp_before - enemy.current_hp
        assert first_play_damage == 12  # Perfected Strike (6 dmg) played twice

        hp_before = enemy.current_hp
        assert combat.play_card(0, 0)
        second_play_damage = hp_before - enemy.current_hp
        assert second_play_damage == 6  # only the first qualifying Attack replays

    def test_does_not_double_attacks_costing_less_than_two_energy(self):
        from sts2_env.cards.ironclad_basic import make_strike_ironclad

        combat = _make_ironclad_combat(["Necronomicon"], seed=5203)
        enemy = combat.enemies[0]
        enemy.max_hp = enemy.current_hp = 500
        combat.hand = [make_strike_ironclad()]
        combat.hand[0].owner = combat.player
        combat.energy = 1  # Strike costs 1

        hp_before = enemy.current_hp
        assert combat.play_card(0, 0)
        assert hp_before - enemy.current_hp == 6  # single Strike, not doubled

    def test_reactivates_next_turn(self):
        combat = _make_ironclad_combat(["Necronomicon"], seed=5204)
        enemy = combat.enemies[0]
        enemy.max_hp = enemy.current_hp = 999

        def _play_one_clean_perfected_strike() -> int:
            combat.draw_pile = []
            combat.discard_pile = []
            combat.exhaust_pile = []
            combat.hand = [_clean_perfected_strike()]
            combat.hand[0].owner = combat.player
            combat.energy = 2
            enemy.block = 0  # ignore whatever block the enemy's own move granted
            combat.player.powers.clear()  # ignore Weak/etc. picked up during the enemy's turn
            hp_before = enemy.current_hp
            assert combat.play_card(0, 0)
            return hp_before - enemy.current_hp

        assert _play_one_clean_perfected_strike() == 12  # doubled on turn 1
        hp_after_first_turn = enemy.current_hp
        assert hp_after_first_turn == 999 - 12

        combat.end_player_turn()

        assert _play_one_clean_perfected_strike() == 12  # doubled again on turn 2


class TestNecronomicurse:
    def test_is_unplayable_curse(self):
        card = create_card(CardId.NECRONOMICURSE)
        assert card.is_curse
        assert card.is_unplayable
        assert card.cost == -1

    def test_exhausting_it_returns_it_to_hand_instead_of_exhaust_pile(self):
        combat = _make_ironclad_combat([], seed=5301)
        card = create_card(CardId.NECRONOMICURSE)
        card.owner = combat.player

        combat.exhaust_card(card)

        assert card in combat.hand
        assert card not in combat.exhaust_pile

    def test_normal_curses_still_go_to_the_exhaust_pile(self):
        from sts2_env.cards.status import make_regret

        combat = _make_ironclad_combat([], seed=5301)
        card = make_regret()
        card.owner = combat.player

        combat.exhaust_card(card)

        assert card in combat.exhaust_pile
        assert card not in combat.hand

    def test_is_immune_to_transform_effects(self):
        card = create_card(CardId.NECRONOMICURSE)
        rng = Rng(5302)
        for _ in range(10):
            replacement = create_transform_card(
                card, character_id="Ironclad", rng=rng, generation_context="combat",
            )
            assert replacement.card_id == CardId.NECRONOMICURSE

    def test_normal_curses_are_not_forced_to_stay_the_same_card(self):
        from sts2_env.cards.status import make_regret

        card = make_regret()
        rng = Rng(5303)
        replacements = {
            create_transform_card(card, character_id="Ironclad", rng=rng, generation_context="combat").card_id
            for _ in range(20)
        }
        assert replacements != {CardId.REGRET}


# ===========================================================================
# 2. NlothsGift rarity-odds hook
# ===========================================================================

class TestNlothsGift:
    def test_offset_formula_matches_decompiled_arithmetic(self):
        relic = create_relic_by_name("NlothsGift")
        base_rare_odds = 0.03
        existing_offset = 0.02

        result = relic.modify_rare_card_odds_offset(
            owner=None, offset=existing_offset, base_rare_odds=base_rare_odds, context="regular",
        )

        original_threshold = base_rare_odds + existing_offset
        expected = original_threshold * 3.0 - base_rare_odds
        assert result == pytest.approx(expected)
        assert result == pytest.approx(0.12)

    def test_offset_formula_with_zero_pity_triples_base_odds_minus_base(self):
        relic = create_relic_by_name("NlothsGift")
        base_rare_odds = 0.03

        result = relic.modify_rare_card_odds_offset(
            owner=None, offset=0.0, base_rare_odds=base_rare_odds, context="regular",
        )

        assert result == pytest.approx(base_rare_odds * 3 - base_rare_odds)
        assert result == pytest.approx(0.06)

    def test_boosts_regular_reward_rolls_into_rare(self):
        run_state = RunState(seed=5401, character_id="Ironclad")
        odds = run_state.card_rarity_odds
        odds.current_value = 0.0
        base_rare = odds.regular_odds["rare"]
        roll_val = base_rare + 0.005  # not rare under base odds

        odds.current_value = 0.0
        without_relic = odds.roll(_FixedFloatRng(roll_val), context="regular", run_state=run_state)
        assert without_relic != CardRarity.RARE

        assert run_state.player.obtain_relic("NlothsGift")
        odds.current_value = 0.0
        with_relic = odds.roll(_FixedFloatRng(roll_val), context="regular", run_state=run_state)
        assert with_relic == CardRarity.RARE

    def test_boosts_elite_reward_rolls_into_rare(self):
        run_state = RunState(seed=5402, character_id="Ironclad")
        assert run_state.player.obtain_relic("NlothsGift")
        odds = run_state.card_rarity_odds
        base_rare = odds.elite_odds["rare"]
        roll_val = base_rare + 0.01

        odds.current_value = 0.0
        result = odds.roll(_FixedFloatRng(roll_val), context="elite", run_state=run_state)

        assert result == CardRarity.RARE

    def test_does_not_affect_shop_rolls(self):
        run_state = RunState(seed=5403, character_id="Ironclad")
        assert run_state.player.obtain_relic("NlothsGift")
        odds = run_state.card_rarity_odds
        base_rare = odds.shop_odds["rare"]
        # Within the tripled range but outside the untouched base range.
        roll_val = base_rare + 0.01

        result = odds.roll(_FixedFloatRng(roll_val), context="shop", run_state=run_state)

        assert result != CardRarity.RARE

    def test_does_not_affect_boss_rolls(self):
        run_state = RunState(seed=5404, character_id="Ironclad")
        assert run_state.player.obtain_relic("NlothsGift")
        odds = run_state.card_rarity_odds

        result = odds.roll(_FixedFloatRng(0.999), context="boss", run_state=run_state)

        assert result == CardRarity.RARE  # boss odds are always 100% rare regardless


# ===========================================================================
# 3. Act-slot-candidates extension point
# ===========================================================================

class TestActSlotCandidates:
    def test_slots_expose_their_installed_act_candidates(self):
        # The user's install (v0.109.0 + Acts-from-the-Past) offers per-slot
        # alternates, all wired in and rolled uniformly.
        assert [c.act_id for c in act_candidates_for_slot(0)] == [
            "Overgrowth", "Underdocks", "Exordium",
        ]
        assert [c.act_id for c in act_candidates_for_slot(1)] == ["Hive", "TheCity"]
        assert [c.act_id for c in act_candidates_for_slot(2)] == ["Glory", "TheBeyond"]

    def test_selection_returns_a_registered_candidate_and_is_seed_deterministic(self):
        from sts2_env.core.rng import Rng

        for slot in range(NUM_ACT_SLOTS):
            candidates = act_candidates_for_slot(slot)
            picked = select_act_for_slot(slot, Rng(777, "act_selection"))
            assert picked in candidates
            # Same seed -> same pick.
            again = select_act_for_slot(slot, Rng(777, "act_selection"))
            assert again is picked

    def test_single_candidate_selection_never_touches_rng(self, monkeypatch):
        # The singleton fast path still exists: a slot with exactly one
        # candidate resolves without ever calling rng.choice (passing None
        # would raise if it tried).
        monkeypatch.setattr(acts_module, "_ACT_SLOT_CANDIDATES", {0: [ACT_0]})
        assert select_act_for_slot(0, rng=None) is ACT_0

    def test_invalid_slot_raises(self):
        with pytest.raises(ValueError):
            select_act_for_slot(NUM_ACT_SLOTS, rng=None)
        with pytest.raises(ValueError):
            register_act_candidate(NUM_ACT_SLOTS, ACT_0)

    def test_run_state_acts_resolve_per_slot_from_registered_candidates(self):
        """RunState.acts must be 4 acts: one candidate per slot (0/1/2) picked
        via the act_selection RNG stream, plus the fixed Act4Heart ending."""
        run_state = RunState(seed=5501, character_id="Ironclad")

        assert [act.act_index for act in run_state.acts] == [0, 1, 2, 3]
        for slot in range(NUM_ACT_SLOTS):
            picked_ids = {c.act_id for c in act_candidates_for_slot(slot)}
            assert run_state.acts[slot].act_id in picked_ids
        # Act 4 is always the Act4Heart ending.
        assert run_state.acts[3].boss_ids == ACT_3.boss_ids == ["CorruptHeart"]
        # Multi-candidate slots now consume the dedicated act_selection stream.
        assert run_state.rng.act_selection.counter > 0

    def test_registering_a_second_candidate_makes_both_reachable_by_rng(self, monkeypatch):
        monkeypatch.setattr(
            acts_module,
            "_ACT_SLOT_CANDIDATES",
            {0: [ACT_0], 1: [ACT_1], 2: [ACT_2]},
        )
        fake_legacy_act = ACT_0.to_mutable()
        fake_legacy_act.boss_ids = ["FakeLegacyBoss"]
        fake_legacy_act.is_legacy = True

        register_act_candidate(0, fake_legacy_act)

        assert act_candidates_for_slot(0) == [ACT_0, fake_legacy_act]

        seen_boss_ids = {
            select_act_for_slot(0, Rng(seed)).boss_ids[0]
            for seed in range(30)
        }
        assert "TheLich" in seen_boss_ids  # ACT_0's real boss
        assert "FakeLegacyBoss" in seen_boss_ids

    def test_registering_a_candidate_for_slot_1_or_2_does_not_affect_other_slots(self, monkeypatch):
        monkeypatch.setattr(
            acts_module,
            "_ACT_SLOT_CANDIDATES",
            {0: [ACT_0], 1: [ACT_1], 2: [ACT_2]},
        )
        fake_act = ACT_1.to_mutable()
        fake_act.boss_ids = ["FakeSlotOneBoss"]
        register_act_candidate(1, fake_act)

        assert act_candidates_for_slot(0) == [ACT_0]
        assert act_candidates_for_slot(2) == [ACT_2]
        assert act_candidates_for_slot(1) == [ACT_1, fake_act]


# ===========================================================================
# 4. Legacy SharedEvents pool filtering
# ===========================================================================

def _fake_event(*, is_shared: bool, is_legacy_exclusive: bool) -> EventModel:
    event = EventModel()
    event.is_shared = is_shared
    event.is_legacy_exclusive = is_legacy_exclusive
    return event


def _fake_act(*, is_legacy: bool) -> ActConfig:
    return ActConfig(act_index=0, num_rooms=1, is_legacy=is_legacy)


class TestLegacySharedEventFiltering:
    def test_config_flag_defaults_match_the_mod(self):
        assert ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS is True
        assert ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS is False

    def test_non_shared_events_are_never_filtered(self):
        legacy_act = _fake_act(is_legacy=True)
        non_legacy_act = _fake_act(is_legacy=False)
        act_exclusive_event = _fake_event(is_shared=False, is_legacy_exclusive=True)

        assert event_allowed_in_act(act_exclusive_event, legacy_act) is True
        assert event_allowed_in_act(act_exclusive_event, non_legacy_act) is True

    def test_base_game_shared_event_allowed_in_legacy_act_by_default(self):
        base_game_event = _fake_event(is_shared=True, is_legacy_exclusive=False)
        legacy_act = _fake_act(is_legacy=True)

        assert event_allowed_in_act(base_game_event, legacy_act) is True

    def test_mod_shared_event_blocked_in_non_legacy_act_by_default(self):
        mod_event = _fake_event(is_shared=True, is_legacy_exclusive=True)
        non_legacy_act = _fake_act(is_legacy=False)

        assert event_allowed_in_act(mod_event, non_legacy_act) is False

    def test_matching_pairs_are_always_allowed_regardless_of_flags(self, monkeypatch):
        monkeypatch.setattr(events_module, "ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS", False)
        monkeypatch.setattr(events_module, "ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS", False)

        legacy_event_in_legacy_act = _fake_event(is_shared=True, is_legacy_exclusive=True)
        base_event_in_base_act = _fake_event(is_shared=True, is_legacy_exclusive=False)

        assert event_allowed_in_act(legacy_event_in_legacy_act, _fake_act(is_legacy=True)) is True
        assert event_allowed_in_act(base_event_in_base_act, _fake_act(is_legacy=False)) is True

    def test_flags_gate_the_filter_when_flipped(self, monkeypatch):
        monkeypatch.setattr(events_module, "ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS", False)
        base_game_event = _fake_event(is_shared=True, is_legacy_exclusive=False)
        assert event_allowed_in_act(base_game_event, _fake_act(is_legacy=True)) is False

        monkeypatch.setattr(events_module, "ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS", True)
        mod_event = _fake_event(is_shared=True, is_legacy_exclusive=True)
        assert event_allowed_in_act(mod_event, _fake_act(is_legacy=False)) is True

    def test_all_real_registered_events_pass_the_filter_for_every_vanilla_act_today(self):
        """Only the mod's legacy-exclusive SharedEvents (e.g. Exordium's
        DeadAdventurer/Mushrooms) are filtered out of the vanilla acts; every
        other registered event passes untouched."""
        for act in (ACT_0, ACT_1, ACT_2, ACT_3):
            for event in all_events():
                expected = not (event.is_shared and event.is_legacy_exclusive)
                assert event_allowed_in_act(event, act) is expected
