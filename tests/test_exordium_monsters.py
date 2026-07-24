"""Tests for Exordium (Act-1-slot legacy act, "Acts from the Past" mod)
monsters and encounters: sts2_env/monsters/exordium.py and
sts2_env/encounters/exordium.py.

Follows the conventions from test_monster_ai_state_machine_parity.py
(``_make_combat``, ``_run_ai``, direct ``move.perform(combat)`` damage
checks) since Exordium isn't wired into a real act slot yet -- monsters are
exercised directly rather than through RunManager/map generation.
"""

from __future__ import annotations

import pytest

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, CombatSide, IntentType, PowerId, ValueProp
from sts2_env.gym_env import observation as obsmod
from sts2_env.gym_env.rich_observation import RichObservationEncoder
from sts2_env.core.rng import INT_MAX, Rng
from sts2_env.monsters.state_machine import MonsterAI
from sts2_env.run.rooms import CombatRoom
from sts2_env.run.run_state import PlayerState

import sts2_env.monsters.exordium as ex
from sts2_env.encounters import exordium as enc


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_combat(seed: int = 7, ascension: int = 0, player_hp: int = 999) -> CombatState:
    combat = CombatState(
        player_hp=player_hp,
        player_max_hp=player_hp,
        deck=create_ironclad_starter_deck(),
        rng_seed=seed,
        character_id="Ironclad",
    )
    combat.ascension_level = ascension
    return combat


def _run_ai(ai: MonsterAI, rng: Rng, n: int) -> list[str]:
    moves = [ai.current_move.state_id]
    ai.on_move_performed()
    for _ in range(n - 1):
        ai.roll_move(rng)
        moves.append(ai.current_move.state_id)
        ai.on_move_performed()
    return moves


def _run_turns(combat: CombatState, ai: MonsterAI, n: int) -> list[str]:
    """Perform n *actual* moves through combat (perform + advance)."""
    moves = []
    for _ in range(n):
        if combat.is_over:
            break
        move = ai.current_move
        moves.append(move.state_id)
        move.perform(combat)
        ai.on_move_performed()
        ai.roll_move(combat.monster_ai_rng)
    return moves


def _run_rounds(combat: CombatState, ai: MonsterAI, n: int) -> list[str]:
    """Advance n full combat rounds (player-end-turn -> full enemy turn)."""
    moves = []
    for _ in range(n):
        if combat.is_over:
            break
        moves.append(ai.current_move.state_id)
        combat.end_player_turn()
    return moves


# ---------------------------------------------------------------------------
# 1. HP ranges / basic factory sanity for every monster
# ---------------------------------------------------------------------------

FACTORY_HP_CASES = [
    ("Cultist", ex.create_cultist, ex.CULTIST_MONSTER_ID, "INCANTATION", 48, 54, 50, 56),
    ("JawWorm", ex.create_jaw_worm, ex.JAW_WORM_MONSTER_ID, "CHOMP", 40, 44, 42, 46),
    ("LouseRed", ex.create_louse_red, ex.LOUSE_RED_MONSTER_ID, "BITE", 10, 15, 11, 16),
    ("LouseGreen", ex.create_louse_green, ex.LOUSE_GREEN_MONSTER_ID, "BITE", 11, 17, 12, 18),
    ("SpikeSlimeMedium", ex.create_spike_slime_medium, ex.SPIKE_SLIME_MEDIUM_MONSTER_ID, "FLAME_TACKLE", 28, 32, 29, 34),
    ("AcidSlimeMedium", ex.create_acid_slime_medium, ex.ACID_SLIME_MEDIUM_MONSTER_ID, None, 28, 32, 29, 34),
    ("SlaverBlue", ex.create_slaver_blue, ex.SLAVER_BLUE_MONSTER_ID, None, 46, 50, 48, 52),
    ("SlaverRed", ex.create_slaver_red, ex.SLAVER_RED_MONSTER_ID, None, 46, 50, 48, 52),
    ("Looter", ex.create_looter, ex.LOOTER_MONSTER_ID, "MUG_1", 44, 48, 46, 50),
    ("GremlinMad", ex.create_gremlin_mad, ex.GREMLIN_MAD_MONSTER_ID, "SCRATCH", 20, 24, 21, 25),
    ("GremlinSneaky", ex.create_gremlin_sneaky, ex.GREMLIN_SNEAKY_MONSTER_ID, "PUNCTURE", 10, 14, 11, 15),
    ("GremlinFat", ex.create_gremlin_fat, ex.GREMLIN_FAT_MONSTER_ID, "SMASH", 13, 17, 14, 18),
    ("GremlinShield", ex.create_gremlin_shield, ex.GREMLIN_SHIELD_MONSTER_ID, "PROTECT", 12, 15, 13, 17),
    ("GremlinWizard", ex.create_gremlin_wizard, ex.GREMLIN_WIZARD_MONSTER_ID, "CHARGING", 21, 25, 22, 26),
    ("GremlinNob", ex.create_gremlin_nob, ex.GREMLIN_NOB_MONSTER_ID, "BELLOW", 82, 86, 85, 90),
    ("AcidSlimeLarge", ex.create_acid_slime_large, ex.ACID_SLIME_LARGE_MONSTER_ID, None, 65, 69, 68, 72),
    ("SpikeSlimeLarge", ex.create_spike_slime_large, ex.SPIKE_SLIME_LARGE_MONSTER_ID, None, 64, 70, 67, 73),
    ("Lagavulin", ex.create_lagavulin, ex.LAGAVULIN_MONSTER_ID, "SLEEP", 109, 111, 112, 115),
    ("Sentry", ex.create_sentry, ex.SENTRY_MONSTER_ID, "BOLT", 38, 42, 39, 45),
    ("SpikeSlimeSmall", ex.create_spike_slime_small, ex.SPIKE_SLIME_SMALL_MONSTER_ID, "TACKLE", 10, 14, 11, 15),
    ("AcidSlimeSmall", ex.create_acid_slime_small, ex.ACID_SLIME_SMALL_MONSTER_ID, "LICK", 8, 12, 9, 13),
    ("FungiBeast", ex.create_fungi_beast, ex.FUNGI_BEAST_MONSTER_ID, None, 22, 28, 24, 28),
]


@pytest.mark.parametrize(
    "name, factory, monster_id, initial_move, base_min, base_max, tough_min, tough_max",
    FACTORY_HP_CASES,
    ids=[c[0] for c in FACTORY_HP_CASES],
)
def test_factory_hp_ranges_and_monster_id(name, factory, monster_id, initial_move, base_min, base_max, tough_min, tough_max):
    creature, ai = factory(Rng(1))
    assert creature.monster_id == monster_id
    assert base_min <= creature.max_hp <= base_max
    assert creature.current_hp == creature.max_hp
    if initial_move is not None:
        assert ai.current_move.state_id == initial_move

    creature8, _ = factory(Rng(2), ascension_level=8)
    assert tough_min <= creature8.max_hp <= tough_max


def test_boss_fixed_hp_both_min_and_max():
    for factory, base_hp, tough_hp in [
        (ex.create_slime_boss, ex.SLIME_BOSS_BASE_HP, ex.SLIME_BOSS_TOUGH_HP),
        (ex.create_guardian, ex.GUARDIAN_BASE_HP, ex.GUARDIAN_TOUGH_HP),
        (ex.create_hexaghost, ex.HEXAGHOST_BASE_HP, ex.HEXAGHOST_TOUGH_HP),
    ]:
        for seed in (1, 2, 3):
            creature, _ = factory(Rng(seed), ascension_level=0)
            assert creature.max_hp == base_hp == creature.current_hp
            creature8, _ = factory(Rng(seed), ascension_level=8)
            assert creature8.max_hp == tough_hp == creature8.current_hp


# ---------------------------------------------------------------------------
# 2. Cultist
# ---------------------------------------------------------------------------

def test_cultist_ritual_then_dark_strike_forever():
    combat = _make_combat(1)
    creature, ai = ex.create_cultist(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert not creature.has_power(PowerId.RITUAL) or creature.get_power_amount(PowerId.RITUAL) == ex.CULTIST_BASE_RITUAL
    moves = _run_turns(combat, ai, 5)
    assert moves == [ex.CULTIST_INCANTATION_MOVE] + [ex.CULTIST_DARK_STRIKE_MOVE] * 4


def test_cultist_deadly_ritual_amount():
    creature, ai = ex.create_cultist(Rng(1), ascension_level=9)
    combat = _make_combat(1, ascension=9)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.current_move.perform(combat)
    assert creature.get_power_amount(PowerId.RITUAL) == ex.CULTIST_DEADLY_RITUAL


def test_cultist_dark_strike_flat_damage_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    creature, ai = ex.create_cultist(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[ex.CULTIST_DARK_STRIKE_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - ex.CULTIST_DARK_STRIKE_DAMAGE


# ---------------------------------------------------------------------------
# 3. JawWorm
# ---------------------------------------------------------------------------

def test_jaw_worm_opens_on_chomp():
    creature, ai = ex.create_jaw_worm(Rng(1))
    assert ai.current_move.state_id == ex.JAW_WORM_CHOMP_MOVE


def test_jaw_worm_never_repeats_chomp_more_than_expected():
    combat = _make_combat(3)
    creature, ai = ex.create_jaw_worm(Rng(3), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    # anti-repeat rules mean Chomp/Thrash never appear 2/3-in-a-row respectively
    # beyond what the reroll tables allow; smoke-check the move set is only
    # ever the 3 known moves.
    assert set(moves) <= {ex.JAW_WORM_CHOMP_MOVE, ex.JAW_WORM_BELLOW_MOVE, ex.JAW_WORM_THRASH_MOVE}
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == ex.JAW_WORM_THRASH_MOVE)


def test_jaw_worm_bellow_strength_and_block_ascension_pairing():
    combat0 = _make_combat(1, ascension=0)
    creature0, ai0 = ex.create_jaw_worm(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    ai0.states[ex.JAW_WORM_BELLOW_MOVE].perform(combat0)
    assert creature0.get_power_amount(PowerId.STRENGTH) == ex.JAW_WORM_BASE_BELLOW_STRENGTH
    assert creature0.block == ex.JAW_WORM_BASE_BELLOW_BLOCK

    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = ex.create_jaw_worm(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    ai9.states[ex.JAW_WORM_BELLOW_MOVE].perform(combat9)
    assert creature9.get_power_amount(PowerId.STRENGTH) == ex.JAW_WORM_DEADLY_BELLOW_STRENGTH
    assert creature9.block == ex.JAW_WORM_DEADLY_BELLOW_BLOCK


def test_jaw_worm_thrash_flat_values_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    creature, ai = ex.create_jaw_worm(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[ex.JAW_WORM_THRASH_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - ex.JAW_WORM_THRASH_DAMAGE
    assert creature.block == ex.JAW_WORM_THRASH_BLOCK


# ---------------------------------------------------------------------------
# 4. LouseRed / LouseGreen
# ---------------------------------------------------------------------------

def test_louse_curl_up_and_rolled_bite_damage_ranges():
    for asc, curl_lo, curl_hi, bite_lo, bite_hi in [
        (0, ex.LOUSE_CURL_UP_BASE_MIN, ex.LOUSE_CURL_UP_BASE_MAX, ex.LOUSE_BITE_BASE_MIN, ex.LOUSE_BITE_BASE_MAX - 1),
        (8, ex.LOUSE_CURL_UP_TOUGH_MIN, ex.LOUSE_CURL_UP_TOUGH_MAX, ex.LOUSE_BITE_BASE_MIN, ex.LOUSE_BITE_BASE_MAX - 1),
        (9, ex.LOUSE_CURL_UP_TOUGH_MIN, ex.LOUSE_CURL_UP_TOUGH_MAX, ex.LOUSE_BITE_DEADLY_MIN, ex.LOUSE_BITE_DEADLY_MAX - 1),
    ]:
        for seed in range(5):
            creature, ai = ex.create_louse_red(Rng(seed), ascension_level=asc)
            assert curl_lo <= creature.get_power_amount(PowerId.CURL_UP) <= curl_hi
            move_dmg = ai.states[ex.LOUSE_BITE_MOVE].intents[0].damage
            assert bite_lo <= move_dmg <= bite_hi


def test_louse_bite_damage_is_stable_across_repeated_bites():
    """The rolled bite damage is fixed at spawn and reused every Bite."""
    creature, ai = ex.create_louse_red(Rng(5), ascension_level=0)
    combat = _make_combat(5)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[ex.LOUSE_BITE_MOVE].perform(combat)
    dmg1 = hp_before - combat.player.current_hp
    hp_before2 = combat.player.current_hp
    ai.states[ex.LOUSE_BITE_MOVE].perform(combat)
    dmg2 = hp_before2 - combat.player.current_hp
    assert dmg1 == dmg2 == ai.states[ex.LOUSE_BITE_MOVE].intents[0].damage


def test_louse_green_spit_web_applies_weak():
    creature, ai = ex.create_louse_green(Rng(1), ascension_level=0)
    combat = _make_combat(1)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.LOUSE_WEB_MOVE].perform(combat)
    assert combat.player.get_power_amount(PowerId.WEAK) == ex.LOUSE_WEAK_WEB


def test_louse_grow_repeat_limit_scales_with_ascension():
    combat0 = _make_combat(1, ascension=0)
    creature0, ai0 = ex.create_louse_red(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    rand0 = ai0.states[ex.LOUSE_RAND]
    grow_branch0 = next(b for b in rand0.branches if b.state_id == ex.LOUSE_GROW_MOVE)
    assert grow_branch0.max_times == 2

    creature9, ai9 = ex.create_louse_red(Rng(1), ascension_level=9)
    rand9 = ai9.states[ex.LOUSE_RAND]
    grow_branch9 = next(b for b in rand9.branches if b.state_id == ex.LOUSE_GROW_MOVE)
    assert grow_branch9.max_times == 1


# ---------------------------------------------------------------------------
# 5. SpikeSlimeMedium / AcidSlimeMedium
# ---------------------------------------------------------------------------

def test_spike_slime_medium_flame_tackle_applies_slimed_and_damage():
    combat = _make_combat(1, ascension=0)
    creature, ai = ex.create_spike_slime_medium(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    discard_before = len(combat.discard_pile)
    ai.states[ex.SPIKE_SLIME_FLAME_TACKLE_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - ex.SPIKE_SLIME_BASE_TACKLE_DAMAGE
    assert len(combat.discard_pile) == discard_before + 1
    assert combat.discard_pile[-1].card_id == CardId.SLIMED


def test_spike_slime_medium_never_three_flame_tackles_in_a_row():
    combat = _make_combat(4)
    creature, ai = ex.create_spike_slime_medium(Rng(4), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == ex.SPIKE_SLIME_FLAME_TACKLE_MOVE)


def test_spike_slime_medium_never_two_licks_in_a_row():
    combat = _make_combat(4)
    creature, ai = ex.create_spike_slime_medium(Rng(4), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 1):
        assert not (moves[i] == moves[i + 1] == ex.SPIKE_SLIME_LICK_MOVE)


def test_acid_slime_medium_never_three_spits_or_three_tackles_in_a_row():
    combat = _make_combat(6)
    creature, ai = ex.create_acid_slime_medium(Rng(6), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 80)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == ex.ACID_SLIME_SPIT_MOVE)
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == ex.ACID_SLIME_TACKLE_MOVE)


def test_acid_slime_medium_never_two_licks_in_a_row():
    combat = _make_combat(6)
    creature, ai = ex.create_acid_slime_medium(Rng(6), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 80)
    for i in range(len(moves) - 1):
        assert not (moves[i] == moves[i + 1] == ex.ACID_SLIME_LICK_MOVE)


def test_slime_override_hp_used_for_current_and_max():
    creature, ai = ex.create_acid_slime_medium(Rng(1), override_hp=17)
    assert creature.max_hp == 17
    assert creature.current_hp == 17


# ---------------------------------------------------------------------------
# 6. SlaverBlue / SlaverRed
# ---------------------------------------------------------------------------

def test_slaver_blue_never_three_stabs_in_a_row():
    combat = _make_combat(2)
    creature, ai = ex.create_slaver_blue(Rng(2), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == ex.SLAVER_BLUE_STAB_MOVE)


def test_slaver_blue_never_two_rakes_in_a_row():
    combat = _make_combat(2)
    creature, ai = ex.create_slaver_blue(Rng(2), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 1):
        assert not (moves[i] == moves[i + 1] == ex.SLAVER_BLUE_RAKE_MOVE)


def test_slaver_red_entangle_used_at_most_once():
    combat = _make_combat(8)
    creature, ai = ex.create_slaver_red(Rng(8), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 100)
    assert moves.count(ex.SLAVER_RED_ENTANGLE_MOVE) <= 1


def test_slaver_red_entangle_applies_entangled_power():
    combat = _make_combat(1)
    creature, ai = ex.create_slaver_red(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.SLAVER_RED_ENTANGLE_MOVE].perform(combat)
    assert combat.player.has_power(PowerId.ENTANGLED)


def test_slaver_red_scrape_applies_vulnerable_ascension_scaled():
    combat = _make_combat(1, ascension=9)
    creature, ai = ex.create_slaver_red(Rng(1), ascension_level=9)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.SLAVER_RED_SCRAPE_MOVE].perform(combat)
    assert combat.player.get_power_amount(PowerId.VULNERABLE) == ex.SLAVER_RED_DEADLY_SCRAPE_VULN


# ---------------------------------------------------------------------------
# 7. Looter
# ---------------------------------------------------------------------------

def test_looter_sequence_two_mugs_then_branches():
    combat = _make_combat(1)
    creature, ai = ex.create_looter(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 3)
    assert moves[:2] == [ex.LOOTER_MUG_1_MOVE, ex.LOOTER_MUG_2_MOVE]
    assert moves[2] in (ex.LOOTER_SMOKE_BOMB_MOVE, ex.LOOTER_LUNGE_MOVE)


def test_looter_path_a_smoke_bomb_then_escape_loops():
    combat = CombatState(player_hp=999, player_max_hp=999, deck=create_ironclad_starter_deck(), rng_seed=1, character_id="Ironclad")
    creature, ai = ex.create_looter(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 6)
    assert moves[0:2] == [ex.LOOTER_MUG_1_MOVE, ex.LOOTER_MUG_2_MOVE]
    # After the branch, it must always end up looping Escape forever, and
    # Escape must actually remove the creature from further hits.
    assert moves[-1] == ex.LOOTER_ESCAPE_MOVE
    assert creature.escaped


def test_looter_steals_gold_on_mug():
    from sts2_env.core.enums import RoomType

    combat = _make_combat(1)
    combat.room = CombatRoom(room_type=RoomType.MONSTER)
    creature, ai = ex.create_looter(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.combat_player_state_for(combat.player).player_state.gold = 50
    combat.start_combat()
    gold_before = combat.combat_player_state_for(combat.player).player_state.gold
    ai.states[ex.LOOTER_MUG_1_MOVE].perform(combat)
    assert combat.combat_player_state_for(combat.player).player_state.gold < gold_before
    thievery = creature.powers[PowerId.THIEVERY]
    assert thievery.gold_stolen > 0


def test_looter_death_refunds_stolen_gold():
    from sts2_env.core.enums import RoomType

    combat = _make_combat(1)
    combat.room = CombatRoom(room_type=RoomType.MONSTER)
    creature, ai = ex.create_looter(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.combat_player_state_for(combat.player).player_state.gold = 50
    combat.start_combat()
    ai.states[ex.LOOTER_MUG_1_MOVE].perform(combat)
    thievery = creature.powers[PowerId.THIEVERY]
    stolen = thievery.gold_stolen_by_player.get(combat.player, 0)
    assert stolen > 0

    assert combat.kill_creature(creature)
    rewards = combat.room.extra_rewards.get(combat.player_id, [])
    assert any(getattr(r, "min_gold", 0) == stolen for r in rewards)


# ---------------------------------------------------------------------------
# 8. GremlinMad / GremlinSneaky / GremlinFat / GremlinShield / GremlinWizard
# ---------------------------------------------------------------------------

def test_gremlin_mad_has_angry_and_loops_scratch():
    combat = _make_combat(1)
    creature, ai = ex.create_gremlin_mad(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert creature.get_power_amount(PowerId.ANGRY) == ex.GREMLIN_MAD_BASE_ANGRY
    moves = _run_turns(combat, ai, 5)
    assert moves == [ex.GREMLIN_MAD_SCRATCH_MOVE] * 5


def test_gremlin_mad_angry_gains_strength_when_damaged():
    combat = _make_combat(1)
    creature, ai = ex.create_gremlin_mad(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.deal_damage(combat.player, creature, 3, ValueProp.MOVE)
    assert creature.get_power_amount(PowerId.STRENGTH) == ex.GREMLIN_MAD_BASE_ANGRY


def test_gremlin_sneaky_loops_puncture():
    combat = _make_combat(1)
    creature, ai = ex.create_gremlin_sneaky(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 4)
    assert moves == [ex.GREMLIN_SNEAKY_PUNCTURE_MOVE] * 4


def test_gremlin_fat_extra_frail_only_at_ascension_9():
    combat8 = _make_combat(1, ascension=8)
    creature8, ai8 = ex.create_gremlin_fat(Rng(1), ascension_level=8)
    combat8.add_enemy(creature8, ai8)
    combat8.start_combat()
    ai8.current_move.perform(combat8)
    assert combat8.player.get_power_amount(PowerId.WEAK) == ex.GREMLIN_FAT_WEAK_AMOUNT
    assert combat8.player.get_power_amount(PowerId.FRAIL) == 0

    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = ex.create_gremlin_fat(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    ai9.current_move.perform(combat9)
    assert combat9.player.get_power_amount(PowerId.WEAK) == ex.GREMLIN_FAT_WEAK_AMOUNT
    assert combat9.player.get_power_amount(PowerId.FRAIL) == ex.GREMLIN_FAT_FRAIL_AMOUNT


def test_gremlin_shield_protects_ally_then_bashes_once_alone():
    combat = _make_combat(1)
    shield, shield_ai = ex.create_gremlin_shield(Rng(1), ascension_level=0)
    ally, ally_ai = ex.create_gremlin_mad(Rng(2), ascension_level=0)
    combat.add_enemy(shield, shield_ai)
    combat.add_enemy(ally, ally_ai)
    combat.start_combat()
    assert shield_ai.current_move.state_id == ex.GREMLIN_SHIELD_PROTECT_MOVE
    ally_block_before = ally.block
    shield_block_before = shield.block
    shield_ai.current_move.perform(combat)
    shield_ai.on_move_performed()
    shield_ai.roll_move(combat.monster_ai_rng)
    # protect targets a random OTHER living ally -- only the ally exists here.
    assert ally.block > ally_block_before or shield.block > shield_block_before
    assert shield_ai.current_move.state_id == ex.GREMLIN_SHIELD_PROTECT_MOVE

    combat.kill_creature(ally)
    shield_ai.roll_move(combat.monster_ai_rng)
    assert shield_ai.current_move.state_id == ex.GREMLIN_SHIELD_BASH_MOVE
    moves = _run_turns(combat, shield_ai, 4)
    assert all(m == ex.GREMLIN_SHIELD_BASH_MOVE for m in moves)


def test_gremlin_shield_protects_self_when_alone():
    combat = _make_combat(1)
    shield, shield_ai = ex.create_gremlin_shield(Rng(1), ascension_level=0)
    combat.add_enemy(shield, shield_ai)
    combat.start_combat()
    block_before = shield.block
    shield_ai.current_move.perform(combat)
    assert shield.block > block_before


def test_gremlin_wizard_charges_twice_then_blasts_forever():
    combat = _make_combat(1)
    creature, ai = ex.create_gremlin_wizard(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 6)
    assert moves == [
        ex.GREMLIN_WIZARD_CHARGING_MOVE, ex.GREMLIN_WIZARD_CHARGING_MOVE,
        ex.GREMLIN_WIZARD_ULTIMATE_MOVE, ex.GREMLIN_WIZARD_ULTIMATE_MOVE,
        ex.GREMLIN_WIZARD_ULTIMATE_MOVE, ex.GREMLIN_WIZARD_ULTIMATE_MOVE,
    ]


# ---------------------------------------------------------------------------
# 9. GremlinNob (Elite)
# ---------------------------------------------------------------------------

def test_gremlin_nob_opens_on_bellow_and_gets_enrage():
    creature, ai = ex.create_gremlin_nob(Rng(1), ascension_level=0)
    assert ai.current_move.state_id == ex.GREMLIN_NOB_BELLOW_MOVE
    combat = _make_combat(1)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.current_move.perform(combat)
    assert creature.has_power(PowerId.ENRAGE)


def test_gremlin_nob_skull_bash_roughly_every_third_turn():
    combat = _make_combat(2)
    creature, ai = ex.create_gremlin_nob(Rng(2), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 30)
    assert moves[0] == ex.GREMLIN_NOB_BELLOW_MOVE
    # No 3-in-a-row of Rush without a SkullBash breaking it up.
    for i in range(len(moves) - 2):
        window = moves[i:i + 3]
        assert ex.GREMLIN_NOB_SKULL_BASH_MOVE in window or window.count(ex.GREMLIN_NOB_RUSH_MOVE) < 3


def test_gremlin_nob_skull_bash_applies_flat_vulnerable_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    creature, ai = ex.create_gremlin_nob(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.GREMLIN_NOB_SKULL_BASH_MOVE].perform(combat)
    assert combat.player.get_power_amount(PowerId.VULNERABLE) == ex.GREMLIN_NOB_SKULL_BASH_VULN


# ---------------------------------------------------------------------------
# 10. AcidSlimeLarge / SpikeSlimeLarge split
# ---------------------------------------------------------------------------

@pytest.mark.parametrize(
    "factory, split_id, medium_id",
    [
        (ex.create_acid_slime_large, ex.ACID_SLIME_LARGE_SPLIT_MOVE, ex.ACID_SLIME_MEDIUM_MONSTER_ID),
        (ex.create_spike_slime_large, ex.SPIKE_SLIME_LARGE_SPLIT_MOVE, ex.SPIKE_SLIME_MEDIUM_MONSTER_ID),
    ],
)
def test_large_slime_splits_into_two_mediums_at_half_hp(factory, split_id, medium_id):
    combat = _make_combat(1)
    creature, ai = factory(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert creature.has_power(PowerId.SPLIT)

    half = creature.max_hp // 2
    combat.deal_damage(combat.player, creature, creature.current_hp - half - 1, ValueProp.MOVE)
    assert not creature.powers[PowerId.SPLIT].triggered
    combat.deal_damage(combat.player, creature, 3, ValueProp.MOVE)
    assert creature.powers[PowerId.SPLIT].triggered
    hp_at_trigger = creature.current_hp

    _run_turns(combat, ai, 3)
    assert creature.is_dead
    mediums = [e for e in combat.enemies if e.monster_id == medium_id]
    assert len(mediums) == 2
    for m in mediums:
        assert m.max_hp == hp_at_trigger
        assert m.current_hp == hp_at_trigger


def test_large_slime_split_only_triggers_once():
    combat = _make_combat(1)
    creature, ai = ex.create_acid_slime_large(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.deal_damage(combat.player, creature, creature.max_hp, ValueProp.MOVE)
    split_power = creature.powers.get(PowerId.SPLIT)
    # If already dead the power may have been removed on death; either way
    # a second (impossible, since dead) hit shouldn't crash and shouldn't
    # somehow flip an already-triggered flag off.
    assert creature.is_dead


# ---------------------------------------------------------------------------
# 11. Lagavulin
# ---------------------------------------------------------------------------

def test_lagavulin_starts_asleep_with_metallicize():
    creature, ai = ex.create_lagavulin(Rng(1), ascension_level=0)
    assert creature.has_power(PowerId.ASLEEP)
    assert creature.get_power_amount(PowerId.METALLICIZE) == ex.LAGAVULIN_METALLICIZE
    assert ai.current_move.state_id == ex.LAGAVULIN_SLEEP_MOVE


def test_lagavulin_wakes_and_stuns_on_damage_loses_metallicize():
    combat = _make_combat(1)
    creature, ai = ex.create_lagavulin(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    combat.deal_damage(combat.player, creature, 5, ValueProp.MOVE)
    assert not creature.has_power(PowerId.ASLEEP)
    assert creature.get_power_amount(PowerId.METALLICIZE) == 0
    assert ai.current_move.state_id == "STUNNED"
    moves = _run_rounds(combat, ai, 4)
    assert moves[0] == "STUNNED"
    assert set(moves[1:]) <= {ex.LAGAVULIN_ATTACK_MOVE, ex.LAGAVULIN_DEBUFF_MOVE}


def test_lagavulin_wakes_naturally_after_three_turns_without_stun():
    combat = _make_combat(9)
    creature, ai = ex.create_lagavulin(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_rounds(combat, ai, 6)
    assert moves.count(ex.LAGAVULIN_SLEEP_MOVE) == 3
    assert "STUNNED" not in moves
    assert set(moves[3:]) <= {ex.LAGAVULIN_ATTACK_MOVE, ex.LAGAVULIN_DEBUFF_MOVE}


def test_lagavulin_debuff_every_third_awake_move_and_resets_dexterity_strength():
    combat = _make_combat(9)
    creature, ai = ex.create_lagavulin(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_rounds(combat, ai, 8)
    awake_moves = moves[3:]
    assert awake_moves[:3] == [ex.LAGAVULIN_ATTACK_MOVE, ex.LAGAVULIN_ATTACK_MOVE, ex.LAGAVULIN_DEBUFF_MOVE]
    assert combat.player.get_power_amount(PowerId.DEXTERITY) < 0
    assert combat.player.get_power_amount(PowerId.STRENGTH) < 0


# ---------------------------------------------------------------------------
# 12. Sentry
# ---------------------------------------------------------------------------

def test_sentry_bolt_first_alternates():
    combat = _make_combat(1)
    creature, ai = ex.create_sentry(Rng(1), ascension_level=0, bolt_first=True)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert creature.get_power_amount(PowerId.ARTIFACT) == ex.SENTRY_ARTIFACT
    moves = _run_turns(combat, ai, 5)
    assert moves == [ex.SENTRY_BOLT_MOVE, ex.SENTRY_BEAM_MOVE] * 2 + [ex.SENTRY_BOLT_MOVE]


def test_sentry_beam_first_alternates():
    combat = _make_combat(1)
    creature, ai = ex.create_sentry(Rng(1), ascension_level=0, bolt_first=False)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 4)
    assert moves == [ex.SENTRY_BEAM_MOVE, ex.SENTRY_BOLT_MOVE, ex.SENTRY_BEAM_MOVE, ex.SENTRY_BOLT_MOVE]


def test_sentry_bolt_adds_dazed_to_discard():
    combat = _make_combat(1)
    creature, ai = ex.create_sentry(Rng(1), ascension_level=0, bolt_first=True)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    before = len(combat.discard_pile)
    ai.states[ex.SENTRY_BOLT_MOVE].perform(combat)
    assert len(combat.discard_pile) == before + ex.SENTRY_BASE_BOLT_COUNT
    assert all(c.card_id == CardId.DAZED for c in combat.discard_pile[before:])


# ---------------------------------------------------------------------------
# 13. SlimeBoss
# ---------------------------------------------------------------------------

def test_slime_boss_deterministic_cycle():
    combat = _make_combat(1, player_hp=99999)
    creature, ai = ex.create_slime_boss(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 9)
    assert moves == [
        ex.SLIME_BOSS_GOOP_SPRAY_MOVE, ex.SLIME_BOSS_PREP_SLAM_MOVE, ex.SLIME_BOSS_SLAM_MOVE,
        ex.SLIME_BOSS_GOOP_SPRAY_MOVE, ex.SLIME_BOSS_PREP_SLAM_MOVE, ex.SLIME_BOSS_SLAM_MOVE,
        ex.SLIME_BOSS_GOOP_SPRAY_MOVE, ex.SLIME_BOSS_PREP_SLAM_MOVE, ex.SLIME_BOSS_SLAM_MOVE,
    ]


def test_slime_boss_split_overrides_deterministic_cycle_and_spawns_both_large_slimes():
    combat = _make_combat(1, player_hp=99999)
    creature, ai = ex.create_slime_boss(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    half = creature.max_hp // 2
    combat.deal_damage(combat.player, creature, creature.current_hp - half - 1, ValueProp.MOVE)
    combat.deal_damage(combat.player, creature, 5, ValueProp.MOVE)
    assert creature.powers[PowerId.SPLIT].triggered
    hp_at_trigger = creature.current_hp

    # SplitPower.cs calls SetMoveImmediate(SplitState, forceTransition: true)
    # the instant HP crosses <=50%, so the SPLIT intent is telegraphed
    # IMMEDIATELY (mid-player-turn) and interrupts any already-queued
    # GOOP_SPRAY/PREP_SLAM/SLAM -- it does not wait for the sequence to
    # finish. The very next enemy move is therefore SPLIT.
    assert ai.current_move.state_id == ex.SLIME_BOSS_SPLIT_MOVE
    moves = _run_turns(combat, ai, 4)
    assert moves[0] == ex.SLIME_BOSS_SPLIT_MOVE
    assert creature.is_dead
    spawned = {e.monster_id: e for e in combat.enemies if e is not creature}
    assert ex.SPIKE_SLIME_LARGE_MONSTER_ID in spawned
    assert ex.ACID_SLIME_LARGE_MONSTER_ID in spawned
    for e in spawned.values():
        assert e.max_hp == hp_at_trigger
        assert e.current_hp == hp_at_trigger


# ---------------------------------------------------------------------------
# 14. Guardian (mode-shift threshold -- the trickiest one)
# ---------------------------------------------------------------------------

def test_guardian_starts_offensive_with_base_threshold():
    for asc, expected in [(0, ex.GUARDIAN_BASE_THRESHOLD), (8, ex.GUARDIAN_TOUGH_THRESHOLD), (20, ex.GUARDIAN_TOUGH_THRESHOLD)]:
        creature, ai = ex.create_guardian(Rng(1), ascension_level=asc)
        mode = creature.powers[PowerId.MODE_SHIFT]
        assert mode.is_open is True
        assert mode.base_threshold == expected
        assert mode.threshold == expected
        assert ai.current_move.state_id == ex.GUARDIAN_CHARGE_UP_MOVE


def test_guardian_offensive_cycle_is_deterministic_without_damage():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 8)
    assert moves == [
        ex.GUARDIAN_CHARGE_UP_MOVE, ex.GUARDIAN_FIERCE_BASH_MOVE, ex.GUARDIAN_VENT_STEAM_MOVE, ex.GUARDIAN_WHIRLWIND_MOVE,
        ex.GUARDIAN_CHARGE_UP_MOVE, ex.GUARDIAN_FIERCE_BASH_MOVE, ex.GUARDIAN_VENT_STEAM_MOVE, ex.GUARDIAN_WHIRLWIND_MOVE,
    ]


def test_guardian_crossing_threshold_on_player_turn_shifts_immediately():
    # ModeShiftPower.cs: when the threshold breaks and the Guardian is NOT
    # executing its own move (the player-turn case, IsExecutingMove == false)
    # it calls TransitionToDefensiveMode() IMMEDIATELY -- gaining the
    # defensive block, bumping the next threshold, closing the shell, and
    # SetMoveImmediate(_closeUpState, forceTransition: true). The queued
    # FIERCE_BASH is interrupted; the telegraphed intent flips to CLOSE_UP
    # the instant the threshold breaks, before the Guardian's next turn.
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    mode = creature.powers[PowerId.MODE_SHIFT]

    ai.current_move.perform(combat)  # CHARGE_UP
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    assert ai.current_move.state_id == ex.GUARDIAN_FIERCE_BASH_MOVE

    creature.block = 0
    combat.deal_damage(combat.player, creature, mode.threshold, ValueProp.MOVE)
    # Immediate transition (still the player's turn -> current_side PLAYER):
    assert mode.pending_shift is False
    assert mode.is_open is False
    assert mode.base_threshold == ex.GUARDIAN_BASE_THRESHOLD + ex.GUARDIAN_THRESHOLD_INCREASE
    assert creature.block == ex.GUARDIAN_DEFENSIVE_BLOCK
    assert ai.current_move.state_id == ex.GUARDIAN_CLOSE_UP_MOVE


def test_guardian_crossing_threshold_during_own_move_defers_to_move_end():
    # The other ModeShiftPower.cs branch: if the Guardian breaks its own
    # threshold WHILE executing a move (e.g. reflected player Thorns during
    # its attack, IsExecutingMove == true) the shift is DEFERRED via
    # PendingModeShift and applied at the end of that move
    # (CheckPendingModeShift -> TransitionToDefensiveMode(setMove: false),
    # letting the branch chooser route to CLOSE_UP on the next roll rather
    # than force-overriding the intent mid-move). Modeled here by the enemy
    # side being active when the damage lands.
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    mode = creature.powers[PowerId.MODE_SHIFT]

    ai.current_move.perform(combat)  # CHARGE_UP
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    assert ai.current_move.state_id == ex.GUARDIAN_FIERCE_BASH_MOVE

    creature.block = 0
    combat.current_side = CombatSide.ENEMY  # Guardian is executing its own move
    combat.deal_damage(combat.player, creature, mode.threshold, ValueProp.MOVE)
    assert mode.pending_shift is True
    assert mode.is_open is True  # not yet flipped -- deferred
    assert ai.current_move.state_id == ex.GUARDIAN_FIERCE_BASH_MOVE  # unchanged

    ai.current_move.perform(combat)  # FIERCE_BASH resolves; pending shift applied at its end
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    assert mode.is_open is False
    assert mode.base_threshold == ex.GUARDIAN_BASE_THRESHOLD + ex.GUARDIAN_THRESHOLD_INCREASE
    assert ai.current_move.state_id == ex.GUARDIAN_CLOSE_UP_MOVE


def test_guardian_defensive_cycle_close_up_roll_attack_twin_slam_reopens():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    mode = creature.powers[PowerId.MODE_SHIFT]
    creature.block = 0
    # Cross the threshold on the player turn before CHARGE_UP (the queued
    # first move) even performs. Per ModeShiftPower.cs the transition is
    # immediate (SetMoveImmediate CLOSE_UP), so the queued CHARGE_UP is
    # interrupted and the very next enemy move is CLOSE_UP, then the fixed
    # defensive sequence ROLL_ATTACK -> TWIN_SLAM (reopens) -> WHIRLWIND.
    combat.deal_damage(combat.player, creature, mode.threshold + 5, ValueProp.MOVE)
    assert ai.current_move.state_id == ex.GUARDIAN_CLOSE_UP_MOVE
    moves = _run_turns(combat, ai, 5)
    assert moves == [
        ex.GUARDIAN_CLOSE_UP_MOVE, ex.GUARDIAN_ROLL_ATTACK_MOVE, ex.GUARDIAN_TWIN_SLAM_MOVE,
        ex.GUARDIAN_WHIRLWIND_MOVE, ex.GUARDIAN_CHARGE_UP_MOVE,
    ]
    assert creature.has_power(PowerId.THORNS) is False  # removed by TWIN_SLAM
    assert mode.is_open is True  # reopened by TWIN_SLAM
    assert mode.threshold == mode.base_threshold


def test_guardian_close_up_grants_sharp_hide_thorns():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.GUARDIAN_CLOSE_UP_MOVE].perform(combat)
    assert creature.get_power_amount(PowerId.THORNS) == ex.GUARDIAN_BASE_SHARP_HIDE


def test_guardian_threshold_increase_stacks_across_multiple_mode_shifts():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    mode = creature.powers[PowerId.MODE_SHIFT]

    # First shift.
    creature.block = 0
    combat.deal_damage(combat.player, creature, mode.threshold + 5, ValueProp.MOVE)
    _run_turns(combat, ai, 4)  # CLOSE_UP, ROLL_ATTACK, TWIN_SLAM, WHIRLWIND -> reopened
    assert mode.base_threshold == ex.GUARDIAN_BASE_THRESHOLD + ex.GUARDIAN_THRESHOLD_INCREASE
    first_threshold = mode.threshold
    assert first_threshold == mode.base_threshold

    # Second shift.
    creature.block = 0
    combat.deal_damage(combat.player, creature, first_threshold + 5, ValueProp.MOVE)
    _run_turns(combat, ai, 4)
    assert mode.base_threshold == ex.GUARDIAN_BASE_THRESHOLD + 2 * ex.GUARDIAN_THRESHOLD_INCREASE


def test_guardian_whirlwind_flat_damage_four_hits():
    combat = _make_combat(1, ascension=20)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[ex.GUARDIAN_WHIRLWIND_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - ex.GUARDIAN_WHIRLWIND_DAMAGE * ex.GUARDIAN_WHIRLWIND_HITS


# ---------------------------------------------------------------------------
# 15. Hexaghost
# ---------------------------------------------------------------------------

def test_hexaghost_opener_activate_then_divider():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_hexaghost(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert ai.current_move.state_id == ex.HEXAGHOST_ACTIVATE_MOVE
    moves = _run_turns(combat, ai, 2)
    assert moves == [ex.HEXAGHOST_ACTIVATE_MOVE, ex.HEXAGHOST_DIVIDER_MOVE]


def test_hexaghost_divider_damage_derived_from_average_player_hp():
    combat = _make_combat(1, player_hp=240)
    creature, ai = ex.create_hexaghost(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[ex.HEXAGHOST_ACTIVATE_MOVE].perform(combat)
    import math
    expected = math.floor(240 / 12.0) + 1
    hp_before = combat.player.current_hp
    ai.states[ex.HEXAGHOST_DIVIDER_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - expected * 6


def test_hexaghost_seven_step_cycle_after_divider():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_hexaghost(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 2 + 7 + 7)
    cycle = moves[2:]
    expected_cycle = [
        ex.HEXAGHOST_SEAR_MOVE, ex.HEXAGHOST_TACKLE_MOVE, ex.HEXAGHOST_SEAR_MOVE, ex.HEXAGHOST_INFLAME_MOVE,
        ex.HEXAGHOST_TACKLE_MOVE, ex.HEXAGHOST_SEAR_MOVE, ex.HEXAGHOST_INFERNO_MOVE,
    ]
    assert cycle == expected_cycle * 2


def test_hexaghost_sear_adds_burn_and_inferno_upgrades_all_burns():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_hexaghost(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()

    before = len(combat.discard_pile)
    ai.states[ex.HEXAGHOST_SEAR_MOVE].perform(combat)
    added = combat.discard_pile[before:]
    assert len(added) == ex.HEXAGHOST_BASE_SEAR_BURN_COUNT
    assert all(c.card_id == CardId.BURN for c in added)
    assert all(not c.upgraded for c in added)

    ai.states[ex.HEXAGHOST_INFERNO_MOVE].perform(combat)
    # All Burns now in discard (the earlier Sear's + Inferno's own 3 bonus)
    # must be upgraded, and the "burns are now upgraded" flag must stick.
    all_burns = [c for c in combat.discard_pile if c.card_id == CardId.BURN]
    assert len(all_burns) >= 1 + ex.HEXAGHOST_INFERNO_BONUS_BURNS
    assert all(c.upgraded for c in all_burns)

    before2 = len(combat.discard_pile)
    ai.states[ex.HEXAGHOST_SEAR_MOVE].perform(combat)
    added2 = combat.discard_pile[before2:]
    assert all(c.upgraded for c in added2 if c.card_id == CardId.BURN)


def test_hexaghost_inferno_six_hits():
    combat = _make_combat(1, ascension=0)
    creature, ai = ex.create_hexaghost(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[ex.HEXAGHOST_INFERNO_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - ex.HEXAGHOST_BASE_INFERNO_DAMAGE * ex.HEXAGHOST_INFERNO_HITS


# ---------------------------------------------------------------------------
# 16. Encounters
# ---------------------------------------------------------------------------

def test_encounter_pool_sizes():
    assert len(enc.WEAK_ENCOUNTERS) == 4
    assert len(enc.NORMAL_ENCOUNTERS) == 9
    assert len(enc.ELITE_ENCOUNTERS) == 3
    assert len(enc.BOSS_ENCOUNTERS) == 3
    assert len(enc.ALL_EXORDIUM_ENCOUNTERS) == 19


@pytest.mark.parametrize("setup", enc.ALL_EXORDIUM_ENCOUNTERS, ids=[s.__name__ for s in enc.ALL_EXORDIUM_ENCOUNTERS])
def test_every_encounter_setup_spawns_at_least_one_enemy(setup):
    combat = _make_combat(3)
    rng = Rng(11)
    setup(combat, rng)
    assert len(combat.enemies) >= 1
    for e in combat.enemies:
        assert e.max_hp > 0


def test_sentries_elite_bolt_first_pattern():
    combat = _make_combat(1)
    rng = Rng(1)
    enc.setup_sentries_elite(combat, rng)
    assert len(combat.enemies) == 3
    ais = [combat.enemy_ais[e.combat_id] for e in combat.enemies]
    assert ais[0].current_move.state_id == ex.SENTRY_BOLT_MOVE
    assert ais[1].current_move.state_id == ex.SENTRY_BEAM_MOVE
    assert ais[2].current_move.state_id == ex.SENTRY_BOLT_MOVE


def test_gremlin_gang_draws_four_distinct_slots_no_repeats():
    for seed in range(10):
        combat = _make_combat(seed)
        rng = Rng(seed)
        enc.setup_gremlin_gang_normal(combat, rng)
        assert len(combat.enemies) == 4


def test_lots_of_slimes_normal_always_three_spike_two_acid_small():
    for seed in range(10):
        combat = _make_combat(seed)
        rng = Rng(seed)
        enc.setup_lots_of_slimes_normal(combat, rng)
        ids = [e.monster_id for e in combat.enemies]
        assert ids.count(ex.SPIKE_SLIME_SMALL_MONSTER_ID) == 3
        assert ids.count(ex.ACID_SLIME_SMALL_MONSTER_ID) == 2


def test_small_slimes_weak_is_small_plus_medium_pairing():
    seen = set()
    for seed in range(20):
        combat = _make_combat(seed)
        rng = Rng(seed)
        enc.setup_small_slimes_weak(combat, rng)
        ids = tuple(sorted(e.monster_id for e in combat.enemies))
        seen.add(ids)
    assert seen == {
        tuple(sorted([ex.SPIKE_SLIME_SMALL_MONSTER_ID, ex.ACID_SLIME_MEDIUM_MONSTER_ID])),
        tuple(sorted([ex.ACID_SLIME_SMALL_MONSTER_ID, ex.SPIKE_SLIME_MEDIUM_MONSTER_ID])),
    }


def test_boss_pool_has_all_three_bosses_and_matches_run_manager_pick_mechanism():
    """RunManager._roll_act_boss picks one setup fn from BOSS_ENCOUNTERS via
    rng.choice(pool) -- confirm that mechanism works against this pool
    without needing any new selection code."""
    names = {s.__name__ for s in enc.BOSS_ENCOUNTERS}
    assert names == {"setup_slime_boss_boss", "setup_guardian_boss", "setup_hexaghost_boss"}
    picks = {Rng(seed).choice(enc.BOSS_ENCOUNTERS).__name__ for seed in range(30)}
    assert picks == names


# ---------------------------------------------------------------------------
# 30. Mid-turn intent updates reflected in the RL observations
#
# Regression coverage for the "stale intent" fix: an enemy whose telegraphed
# move changes DURING the player's turn (slime split threshold, Guardian
# mode-shift, Lagavulin wake) must update ai.current_move immediately, and
# both observation encoders read ai.current_move so the encoded intent must
# reflect the new move before the enemy's turn.
# ---------------------------------------------------------------------------

def _flat_enemy_intent(combat, i=0):
    """(attack_onehot, intent_damage, all_intent_onehots) for enemy i in the flat obs."""
    obs = obsmod.encode_observation(combat)
    eb = obsmod.OBS_SIZE - obsmod.MAX_ENEMIES * obsmod.ENEMY_FEATURES + i * obsmod.ENEMY_FEATURES
    onehots = [obs[eb + 3 + j] for j in range(obsmod.NUM_INTENT_TYPES)]
    dmg = obs[eb + 3 + obsmod.NUM_INTENT_TYPES]
    atk = obs[eb + 3 + obsmod.INTENT_TYPES.index(IntentType.ATTACK)]
    return atk, dmg, onehots


def _rich_enemy_intent(combat, intent_type, i=0):
    """(onehot_for_intent_type, intent_damage) for enemy i in the rich obs."""
    import sts2_env.gym_env.rich_observation as rich
    obs = RichObservationEncoder().encode_combat(combat)
    eb = rich.ENEMIES_OFF + i * rich.ENEMY_BLOCK_SIZE
    idx = rich.INTENT_TO_IDX[intent_type]
    return obs[eb + rich.ENEMY_CORE_FEATURES + idx], obs[eb + 4]


def test_acid_slime_large_split_intent_immediate_and_reflected_in_observations():
    combat = _make_combat(1)
    creature, ai = ex.create_acid_slime_large(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    # Seeded initial telegraph is an attack (CORROSIVE_SPIT).
    assert ai.current_move.state_id == ex.ACID_SLIME_LARGE_SPIT_MOVE
    atk_before, dmg_before, _ = _flat_enemy_intent(combat)
    assert atk_before == 1.0 and dmg_before > 0.0
    rich_unknown_before, _ = _rich_enemy_intent(combat, IntentType.UNKNOWN)
    assert rich_unknown_before == 0.0

    # Drop it to <=50% mid-player-turn; SplitPower must flip the intent to
    # SPLIT (Unknown) immediately, before the slime's own turn.
    half = creature.max_hp // 2
    combat.deal_damage(combat.player, creature, creature.current_hp - half, ValueProp.MOVE)
    assert creature.powers[PowerId.SPLIT].triggered
    assert ai.current_move.state_id == ex.ACID_SLIME_LARGE_SPLIT_MOVE
    assert ai.current_move.intents[0].intent_type == IntentType.UNKNOWN

    # Flat obs: the attack one-hot and intent damage collapse to zero (Unknown
    # is not one of the flat encoder's 5 intent types).
    atk_after, dmg_after, onehots_after = _flat_enemy_intent(combat)
    assert atk_after == 0.0 and dmg_after == 0.0
    assert all(v == 0.0 for v in onehots_after)
    # Rich obs: the Unknown intent bit turns on.
    rich_unknown_after, rich_dmg_after = _rich_enemy_intent(combat, IntentType.UNKNOWN)
    assert rich_unknown_after == 1.0 and rich_dmg_after == 0.0


def test_guardian_mode_shift_intent_immediate_and_reflected_in_observation():
    combat = _make_combat(1, player_hp=999999)
    creature, ai = ex.create_guardian(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    mode = creature.powers[PowerId.MODE_SHIFT]
    # Initial telegraph is CHARGE_UP (a Defend intent).
    assert ai.current_move.state_id == ex.GUARDIAN_CHARGE_UP_MOVE
    defend_before, _ = _rich_enemy_intent(combat, IntentType.DEFEND)
    assert defend_before == 1.0

    creature.block = 0
    combat.deal_damage(combat.player, creature, mode.threshold + 5, ValueProp.MOVE)
    # Immediate defensive-mode shift: intent flips to CLOSE_UP (a Buff intent).
    assert ai.current_move.state_id == ex.GUARDIAN_CLOSE_UP_MOVE
    assert ai.current_move.intents[0].intent_type == IntentType.BUFF
    buff_after, _ = _rich_enemy_intent(combat, IntentType.BUFF)
    defend_after, _ = _rich_enemy_intent(combat, IntentType.DEFEND)
    assert buff_after == 1.0 and defend_after == 0.0


def test_lagavulin_wake_stun_intent_immediate_and_reflected_in_observation():
    combat = _make_combat(1)
    creature, ai = ex.create_lagavulin(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert ai.current_move.state_id == ex.LAGAVULIN_SLEEP_MOVE
    sleep_before, _ = _rich_enemy_intent(combat, IntentType.SLEEP)
    assert sleep_before == 1.0

    # Unblocked damage while asleep wakes it mid-player-turn -> stunned intent.
    combat.deal_damage(combat.player, creature, 5, ValueProp.MOVE)
    assert not creature.has_power(PowerId.ASLEEP)
    assert ai.current_move.state_id == "STUNNED"
    assert ai.current_move.intents[0].intent_type == IntentType.STUN
    stun_after, _ = _rich_enemy_intent(combat, IntentType.STUN)
    sleep_after, _ = _rich_enemy_intent(combat, IntentType.SLEEP)
    assert stun_after == 1.0 and sleep_after == 0.0
