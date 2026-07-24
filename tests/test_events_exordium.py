"""Tests for Exordium events ("Acts from the Past" mod, classic mode).

Covers sts2_env/events/exordium.py, the event-only encounters appended to
sts2_env/encounters/exordium.py, the GoldenIdol relic's combat-gold hook
(sts2_env/relics/exordium.py), and the Parasite curse's remove-from-deck
Max HP loss.

Follows the conventions of test_events_act1_act3_parity.py (direct
RunState + EventModel exercise; events aren't wired into a live act slot yet).
"""

from __future__ import annotations

import sts2_env.events  # noqa: F401  (registers all events)

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.cards.status import make_parasite, make_regret
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, PowerId, RoomType
from sts2_env.core.rng import Rng
from sts2_env.encounters.events import EVENT_ENCOUNTER_REGISTRY, get_event_encounter_setup
from sts2_env.events.exordium import (
    EXORDIUM_EVENT_IDS,
    BigFish,
    Cleric,
    DeadAdventurer,
    GoldenIdol,
    LivingWall,
    Mushrooms,
    ScrapOoze,
    ShiningLight,
    Sssserpent,
    WingStatue,
    WorldOfGoop,
)
from sts2_env.map.acts import ActConfig
from sts2_env.relics.base import RelicId
from sts2_env.relics.registry import create_relic_by_name
from sts2_env.run.events import event_allowed_in_act, get_event
from sts2_env.run.reward_objects import CardReward, GoldReward, RelicReward, RewardsSet
from sts2_env.run.rooms import CombatRoom
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

    def __init__(self, rolls):
        self._rolls = list(rolls)

    def next_int_exclusive(self, low, high):
        return self._rolls.pop(0)

    def next_int(self, low, high):
        return self._rolls.pop(0)

    def shuffle(self, lst):
        pass


def _resolve_single_card_choice(event, index: int = 0):
    assert event.pending_choice is not None
    return event.resolve_pending_choice(index)


# ---------------------------------------------------------------------------
# Registration / pool tagging
# ---------------------------------------------------------------------------

def test_all_exordium_events_are_registered():
    assert len(EXORDIUM_EVENT_IDS) == 11
    for event_id in EXORDIUM_EVENT_IDS:
        assert get_event(event_id) is not None, event_id


def test_all_exordium_events_are_tagged_legacy_exclusive():
    for event_id in EXORDIUM_EVENT_IDS:
        assert get_event(event_id).is_legacy_exclusive is True, event_id


def test_shared_exordium_events_blocked_outside_legacy_acts():
    legacy_act = ActConfig(act_index=0, num_rooms=1, is_legacy=True)
    vanilla_act = ActConfig(act_index=0, num_rooms=1, is_legacy=False)
    for event_id in ("DeadAdventurer", "Mushrooms"):
        event = get_event(event_id)
        assert event.is_shared is True
        assert event_allowed_in_act(event, legacy_act) is True
        assert event_allowed_in_act(event, vanilla_act) is False


def test_vanilla_act_event_pools_do_not_contain_exordium_events():
    from sts2_env.map.acts import ALL_ACTS

    for act in ALL_ACTS:
        for event_id in EXORDIUM_EVENT_IDS:
            assert event_id not in act.event_ids


def test_event_encounters_registered_but_not_in_random_pools():
    from sts2_env.encounters import exordium as enc

    for encounter_id in (
        "dead_adventurer_sentries",
        "dead_adventurer_gremlin_nob",
        "dead_adventurer_lagavulin",
        "three_fungi_beasts_event",
    ):
        setup = get_event_encounter_setup(encounter_id)
        assert setup is not None, encounter_id
        assert setup not in enc.WEAK_ENCOUNTERS
        assert setup not in enc.NORMAL_ENCOUNTERS
        assert setup not in enc.ELITE_ENCOUNTERS
        assert setup not in enc.BOSS_ENCOUNTERS


# ---------------------------------------------------------------------------
# BigFish
# ---------------------------------------------------------------------------

def test_big_fish_banana_heals_third_of_max_hp():
    run_state = _make_run_state()
    run_state.player.max_hp = 80
    run_state.player.current_hp = 30
    event = BigFish()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "banana")
    assert result.finished
    assert run_state.player.current_hp == 30 + 80 // 3  # 26 healed


def test_big_fish_donut_gains_five_max_hp():
    run_state = _make_run_state()
    before = run_state.player.max_hp
    event = BigFish()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "donut")
    assert result.finished
    assert run_state.player.max_hp == before + 5


def test_big_fish_box_grants_relic_and_regret():
    run_state = _make_run_state()
    relics_before = len(run_state.player.relics)
    event = BigFish()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "box")
    assert result.finished
    assert len(run_state.player.relics) == relics_before + 1
    assert any(card.card_id == CardId.REGRET for card in run_state.player.deck)


# ---------------------------------------------------------------------------
# Cleric
# ---------------------------------------------------------------------------

def test_cleric_is_allowed_requires_35_gold():
    run_state = _make_run_state(gold=35)
    assert Cleric().is_allowed(run_state) is True
    run_state.player.gold = 34
    assert Cleric().is_allowed(run_state) is False


def test_cleric_heal_costs_35_and_heals_quarter_max_hp():
    run_state = _make_run_state(gold=100)
    run_state.player.max_hp = 75
    run_state.player.current_hp = 20
    event = Cleric()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "heal")
    assert result.finished
    assert run_state.player.gold == 65
    assert run_state.player.current_hp == 20 + int(75 * 0.25)  # +18


def test_cleric_purify_costs_75_and_removes_a_card():
    run_state = _make_run_state(gold=100)
    deck_before = len(run_state.player.deck)
    event = Cleric()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "purify").enabled

    result = event.choose(run_state, "purify")
    assert not result.finished
    result = _resolve_single_card_choice(event)
    assert result.finished
    assert run_state.player.gold == 25
    assert len(run_state.player.deck) == deck_before - 1


def test_cleric_purify_locked_without_75_gold():
    run_state = _make_run_state(gold=50)
    event = Cleric()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "purify").enabled is False
    assert next(o for o in options if o.option_id == "leave").enabled is True


# ---------------------------------------------------------------------------
# DeadAdventurer
# ---------------------------------------------------------------------------

def test_dead_adventurer_requires_floor_7():
    run_state = _make_run_state()
    run_state.total_floor = 6
    assert DeadAdventurer().is_allowed(run_state) is False
    run_state.total_floor = 7
    assert DeadAdventurer().is_allowed(run_state) is True


def test_dead_adventurer_three_safe_searches_grant_all_rewards_and_finish():
    run_state = _make_run_state(gold=0)
    event = DeadAdventurer()
    # enemy roll 0 (Sentries); searches roll 99 (>= chance, never ambushed).
    event.rng = _ScriptedRng([0, 99, 99, 99])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)
    # No-op shuffle keeps the reward order [gold, nothing, relic].
    assert event._rewards == ["gold", "nothing", "relic"]
    relics_before = len(run_state.player.relics)

    first = event.choose(run_state, "search")
    assert not first.finished
    assert run_state.player.gold == 30
    assert event._encounter_chance == 60

    second = event.choose(run_state, "search")
    assert not second.finished
    assert "nothing" in second.description.lower()
    assert event._encounter_chance == 85

    third = event.choose(run_state, "search")
    assert third.finished  # 3 searches exhaust the event
    assert len(run_state.player.relics) == relics_before + 1


def test_dead_adventurer_ambush_enters_combat_with_exact_event_rewards():
    run_state = _make_run_state()
    event = DeadAdventurer()
    # enemy roll 2 (Lagavulin); first search rolls 10 < 35 -> ambush.
    event.rng = _ScriptedRng([2, 10])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)

    ambush = event.choose(run_state, "search")
    assert not ambush.finished
    assert [option.option_id for option in ambush.next_options] == ["fight"]

    fight = event.choose(run_state, "fight")
    assert fight.finished
    assert fight.event_combat_setup == "dead_adventurer_lagavulin"

    rewards = fight.rewards["reward_objects"]
    # Card reward (3 options, elite room context) + 25-35 gold + all three
    # unsearched rewards (30 gold, nothing, relic) -- nothing else.
    card_rewards = [r for r in rewards if isinstance(r, CardReward)]
    gold_rewards = [r for r in rewards if isinstance(r, GoldReward)]
    relic_rewards = [r for r in rewards if isinstance(r, RelicReward)]
    assert len(rewards) == 4
    assert len(card_rewards) == 1
    assert card_rewards[0].context == "elite"
    assert card_rewards[0].option_count == 3
    assert [(g.min_gold, g.max_gold) for g in gold_rewards] == [(25, 35), (30, 30)]
    assert len(relic_rewards) == 1


def test_dead_adventurer_gold_reward_reduced_after_partial_searches():
    run_state = _make_run_state(gold=0)
    event = DeadAdventurer()
    # enemy 1 (Gremlin Nob); search 1 safe (99), search 2 ambushed (0).
    event.rng = _ScriptedRng([1, 99, 0])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)

    event.choose(run_state, "search")  # claims the 30 gold
    ambush = event.choose(run_state, "search")
    assert not ambush.finished

    fight = event.choose(run_state, "fight")
    assert fight.event_combat_setup == "dead_adventurer_gremlin_nob"
    rewards = fight.rewards["reward_objects"]
    gold_rewards = [r for r in rewards if isinstance(r, GoldReward)]
    # Only the base 25-35 remains (the 30-gold search reward was claimed;
    # "nothing" and "relic" stay in the pot -> 1 relic reward).
    assert [(g.min_gold, g.max_gold) for g in gold_rewards] == [(25, 35)]
    assert len([r for r in rewards if isinstance(r, RelicReward)]) == 1


def test_event_combat_room_suppresses_default_rewards():
    """Event-embedded fights pay out exactly the event's explicit rewards."""
    run_state = _make_run_state()
    player_id = run_state.player.player_id
    room = CombatRoom(
        room_type=RoomType.MONSTER,
        encounter_id="three_fungi_beasts_event",
        suppress_default_rewards=True,
    )
    room.add_extra_reward(player_id, RelicReward(player_id, relic_id=RelicId.ODD_MUSHROOM.name))

    rewards = RewardsSet(player_id).with_rewards_from_room(room, run_state).generate_without_offering(run_state)

    assert len(rewards) == 1
    assert isinstance(rewards[0], RelicReward)
    assert rewards[0].relic_id == RelicId.ODD_MUSHROOM.name


def test_dead_adventurer_event_encounters_spawn_correct_monsters():
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

    sentries = _spawn("dead_adventurer_sentries").enemies
    assert len(sentries) == 3
    assert all(c.monster_id == "EXORDIUM_SENTRY" for c in sentries)

    nob = _spawn("dead_adventurer_gremlin_nob").enemies
    assert len(nob) == 1
    assert nob[0].monster_id == "EXORDIUM_GREMLIN_NOB"

    lagavulin = _spawn("dead_adventurer_lagavulin").enemies
    assert len(lagavulin) == 1
    assert lagavulin[0].monster_id == "EXORDIUM_LAGAVULIN"
    # StartsAwake: no Asleep power and no Metallicize.
    assert PowerId.ASLEEP not in lagavulin[0].powers
    assert PowerId.METALLICIZE not in lagavulin[0].powers

    fungi = _spawn("three_fungi_beasts_event").enemies
    assert len(fungi) == 3
    assert all(c.monster_id == "EXORDIUM_FUNGI_BEAST" for c in fungi)


def test_event_lagavulin_starts_awake_first_move_is_debuff():
    from sts2_env.monsters.exordium import create_lagavulin

    creature, ai = create_lagavulin(Rng(3), starts_awake=True)
    assert ai.current_move.state_id == "DEBUFF"

    asleep_creature, asleep_ai = create_lagavulin(Rng(3))
    assert asleep_ai.current_move.state_id == "SLEEP"
    assert PowerId.ASLEEP in asleep_creature.powers


# ---------------------------------------------------------------------------
# GoldenIdol (event + relic)
# ---------------------------------------------------------------------------

def test_golden_idol_take_grants_relic_and_forces_boulder_choice():
    run_state = _make_run_state()
    run_state.player.max_hp = 80
    run_state.player.current_hp = 80
    event = GoldenIdol()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "take")
    assert not result.finished
    assert {option.option_id for option in result.next_options} == {"outrun", "smash", "hide"}
    assert RelicId.GOLDEN_IDOL.name in run_state.player.relics


def test_golden_idol_outrun_gains_injury():
    run_state = _make_run_state()
    event = GoldenIdol()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "take")

    result = event.choose(run_state, "outrun")
    assert result.finished
    assert any(card.card_id == CardId.INJURY for card in run_state.player.deck)


def test_golden_idol_smash_takes_35_percent_damage():
    run_state = _make_run_state()
    run_state.player.max_hp = 81
    run_state.player.current_hp = 81
    event = GoldenIdol()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "take")

    result = event.choose(run_state, "smash")
    assert result.finished
    assert run_state.player.current_hp == 81 - int(81 * 0.35)  # -28


def test_golden_idol_hide_loses_10_percent_max_hp_min_1():
    run_state = _make_run_state()
    run_state.player.max_hp = 80
    run_state.player.current_hp = 80
    event = GoldenIdol()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)
    event.choose(run_state, "take")

    result = event.choose(run_state, "hide")
    assert result.finished
    assert run_state.player.max_hp == 72

    small = _make_run_state(seed=43)
    small.player.max_hp = 9
    small.player.current_hp = 9
    event2 = GoldenIdol()
    event2.reset_rng_for_run(small)
    event2.generate_initial_options(small)
    event2.choose(small, "take")
    event2.choose(small, "hide")
    assert small.player.max_hp == 8  # floor(0.9) = 0 -> min 1


def test_golden_idol_relic_boosts_combat_gold_rewards_by_25_percent():
    run_state = _make_run_state()
    player = run_state.player
    player.obtain_relic(RelicId.GOLDEN_IDOL.name)
    player_id = player.player_id

    room = CombatRoom(room_type=RoomType.MONSTER, encounter_id="whatever", suppress_default_rewards=True)
    room.add_extra_reward(player_id, GoldReward.fixed(player_id, 40))
    rewards = RewardsSet(player_id).with_rewards_from_room(room, run_state).generate_without_offering(run_state)
    gold = next(r for r in rewards if isinstance(r, GoldReward))
    assert gold.amount == 50  # 40 + int(40 * 0.25)


def test_golden_idol_relic_truncates_bonus_and_ignores_non_combat_rooms():
    run_state = _make_run_state()
    player = run_state.player
    relic = create_relic_by_name(RelicId.GOLDEN_IDOL.name)

    reward = GoldReward.fixed(player.player_id, 30)
    combat_room = CombatRoom(room_type=RoomType.ELITE, encounter_id="x")
    relic.modify_rewards(player, [reward], combat_room, run_state)
    assert reward.amount == 37  # 30 + int(7.5)

    unmodified = GoldReward.fixed(player.player_id, 30)
    relic.modify_rewards(player, [unmodified], None, run_state)
    assert unmodified.amount == 30


# ---------------------------------------------------------------------------
# LivingWall
# ---------------------------------------------------------------------------

def test_living_wall_forget_removes_a_card():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    event = LivingWall()
    assert event.is_allowed(run_state) is True

    result = event.choose(run_state, "forget")
    assert not result.finished
    result = _resolve_single_card_choice(event)
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1


def test_living_wall_change_transforms_a_card():
    run_state = _make_run_state()
    deck_before = len(run_state.player.deck)
    original_ids = [card.card_id for card in run_state.player.deck]
    event = LivingWall()

    result = event.choose(run_state, "change")
    assert not result.finished
    result = _resolve_single_card_choice(event)
    assert result.finished
    assert len(run_state.player.deck) == deck_before
    assert [card.card_id for card in run_state.player.deck] != original_ids


def test_living_wall_grow_upgrades_a_card_and_locks_without_upgradables():
    run_state = _make_run_state()
    event = LivingWall()
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "grow").enabled is True

    result = event.choose(run_state, "grow")
    assert not result.finished
    result = _resolve_single_card_choice(event)
    assert result.finished
    assert any(card.upgraded for card in run_state.player.deck)

    # All cards upgraded -> grow locked.
    no_upgrade = _make_run_state(seed=44)
    for card in list(no_upgrade.player.deck):
        no_upgrade.player.upgrade_card_instance(card)
    locked = LivingWall().generate_initial_options(no_upgrade)
    assert next(o for o in locked if o.option_id == "grow").enabled is False


# ---------------------------------------------------------------------------
# Mushrooms
# ---------------------------------------------------------------------------

def test_mushrooms_requires_floor_7():
    run_state = _make_run_state()
    run_state.total_floor = 6
    assert Mushrooms().is_allowed(run_state) is False
    run_state.total_floor = 7
    assert Mushrooms().is_allowed(run_state) is True


def test_mushrooms_eat_heals_quarter_max_hp_and_adds_parasite():
    run_state = _make_run_state()
    run_state.player.max_hp = 81
    run_state.player.current_hp = 40
    event = Mushrooms()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "eat")
    assert result.finished
    assert run_state.player.current_hp == 40 + int(81 * 0.25)  # +20
    assert any(card.card_id == CardId.PARASITE for card in run_state.player.deck)


def test_mushrooms_fight_pays_out_exactly_the_odd_mushroom_relic():
    run_state = _make_run_state()
    event = Mushrooms()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    fight = event.choose(run_state, "fight")
    assert not fight.finished
    assert [option.option_id for option in fight.next_options] == ["enter_combat"]

    combat = event.choose(run_state, "enter_combat")
    assert combat.finished
    assert combat.event_combat_setup == "three_fungi_beasts_event"
    rewards = combat.rewards["reward_objects"]
    assert len(rewards) == 1
    assert isinstance(rewards[0], RelicReward)
    assert rewards[0].relic_id == RelicId.ODD_MUSHROOM.name
    assert "three_fungi_beasts_event" in EVENT_ENCOUNTER_REGISTRY


# ---------------------------------------------------------------------------
# ScrapOoze
# ---------------------------------------------------------------------------

def test_scrap_ooze_damage_and_chance_escalate_until_relic_found():
    run_state = _make_run_state()
    run_state.player.max_hp = 100
    run_state.player.current_hp = 100
    event = ScrapOoze()
    # Rolls: 0 (fail, needs >= 75), 0 (fail, needs >= 65), 99 (success).
    event.rng = _ScriptedRng([0, 0, 99])
    event._vars_calculated_for_run = None
    event.generate_initial_options(run_state)
    relics_before = len(run_state.player.relics)

    first = event.choose(run_state, "reach")
    assert not first.finished
    assert run_state.player.current_hp == 95  # -5

    second = event.choose(run_state, "reach")
    assert not second.finished
    assert run_state.player.current_hp == 89  # -6

    third = event.choose(run_state, "reach")
    assert third.finished
    assert run_state.player.current_hp == 82  # -7
    assert len(run_state.player.relics) == relics_before + 1


def test_scrap_ooze_leave_is_free():
    run_state = _make_run_state()
    hp_before = run_state.player.current_hp
    event = ScrapOoze()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "leave")
    assert result.finished
    assert run_state.player.current_hp == hp_before


# ---------------------------------------------------------------------------
# ShiningLight
# ---------------------------------------------------------------------------

def test_shining_light_enter_damages_30_percent_and_upgrades_two_cards():
    run_state = _make_run_state()
    run_state.player.max_hp = 81
    run_state.player.current_hp = 81
    event = ShiningLight()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "enter").enabled is True

    result = event.choose(run_state, "enter")
    assert result.finished
    assert run_state.player.current_hp == 81 - int(81 * 0.30)  # -24
    assert sum(1 for card in run_state.player.deck if card.upgraded) == 2


def test_shining_light_enter_locked_without_upgradable_cards():
    run_state = _make_run_state()
    for card in list(run_state.player.deck):
        run_state.player.upgrade_card_instance(card)
    event = ShiningLight()
    event.reset_rng_for_run(run_state)
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "enter").enabled is False


# ---------------------------------------------------------------------------
# Sssserpent
# ---------------------------------------------------------------------------

def test_sssserpent_agree_gains_150_gold_and_doubt():
    run_state = _make_run_state(gold=0)
    event = Sssserpent()

    agree = event.choose(run_state, "agree")
    assert not agree.finished
    assert [option.option_id for option in agree.next_options] == ["take_gold"]

    result = event.choose(run_state, "take_gold")
    assert result.finished
    assert run_state.player.gold == 150
    assert any(card.card_id == CardId.DOUBT for card in run_state.player.deck)


def test_sssserpent_disagree_does_nothing():
    run_state = _make_run_state(gold=0)
    deck_before = len(run_state.player.deck)
    result = Sssserpent().choose(run_state, "disagree")
    assert result.finished
    assert run_state.player.gold == 0
    assert len(run_state.player.deck) == deck_before


# ---------------------------------------------------------------------------
# WingStatue
# ---------------------------------------------------------------------------

def test_wing_statue_agree_takes_7_damage_and_removes_a_card():
    run_state = _make_run_state()
    hp_before = run_state.player.current_hp
    deck_before = len(run_state.player.deck)
    event = WingStatue()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "agree")
    assert not result.finished
    assert run_state.player.current_hp == hp_before - 7
    result = _resolve_single_card_choice(event)
    assert result.finished
    assert len(run_state.player.deck) == deck_before - 1


def test_wing_statue_attack_requires_10_damage_attack_and_pays_50_to_80_gold():
    run_state = _make_run_state(gold=0)
    event = WingStatue()
    event.reset_rng_for_run(run_state)
    # Starter Ironclad deck: Strike 6 / Bash 8 -> locked.
    options = event.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "attack").enabled is False

    # Upgraded Bash deals 10 -> unlocked.
    bash = next(card for card in run_state.player.deck if card.card_id == CardId.BASH)
    run_state.player.upgrade_card_instance(bash)
    assert bash.base_damage >= 10
    event2 = WingStatue()
    event2.reset_rng_for_run(run_state)
    options = event2.generate_initial_options(run_state)
    assert next(o for o in options if o.option_id == "attack").enabled is True

    result = event2.choose(run_state, "attack")
    assert result.finished
    assert 50 <= run_state.player.gold <= 80


# ---------------------------------------------------------------------------
# WorldOfGoop
# ---------------------------------------------------------------------------

def test_world_of_goop_gather_takes_11_damage_for_75_gold():
    run_state = _make_run_state(gold=0)
    hp_before = run_state.player.current_hp
    event = WorldOfGoop()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "gather")
    assert result.finished
    assert run_state.player.current_hp == hp_before - 11
    assert run_state.player.gold == 75


def test_world_of_goop_leave_loses_35_to_75_gold():
    run_state = _make_run_state(gold=200)
    event = WorldOfGoop()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "leave")
    assert result.finished
    lost = 200 - run_state.player.gold
    assert 35 <= lost <= 75


def test_world_of_goop_leave_loss_capped_at_current_gold():
    run_state = _make_run_state(gold=10)
    event = WorldOfGoop()
    event.reset_rng_for_run(run_state)
    event.generate_initial_options(run_state)

    result = event.choose(run_state, "leave")
    assert result.finished
    assert run_state.player.gold == 0


# ---------------------------------------------------------------------------
# Parasite curse: remove-from-deck Max HP loss
# ---------------------------------------------------------------------------

def test_parasite_removal_from_deck_loses_3_max_hp():
    run_state = _make_run_state()
    player = run_state.player
    parasite = make_parasite()
    player.deck.append(parasite)
    max_hp_before = player.max_hp

    removed = player.remove_cards_from_deck(1, cards=[parasite])
    assert removed == 1
    assert player.max_hp == max_hp_before - 3
    assert all(card.card_id != CardId.PARASITE for card in player.deck)


def test_parasite_removal_via_event_card_choice_loses_3_max_hp():
    from sts2_env.events.shared import _remove_selected_cards

    run_state = _make_run_state()
    player = run_state.player
    parasite = make_parasite()
    player.deck.append(parasite)
    max_hp_before = player.max_hp

    removed = _remove_selected_cards([parasite], run_state)
    assert removed == 1
    assert player.max_hp == max_hp_before - 3


def test_removing_other_curses_does_not_lose_max_hp():
    run_state = _make_run_state()
    player = run_state.player
    regret = make_regret()
    player.deck.append(regret)
    max_hp_before = player.max_hp

    removed = player.remove_cards_from_deck(1, cards=[regret])
    assert removed == 1
    assert player.max_hp == max_hp_before
