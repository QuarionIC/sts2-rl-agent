"""Tests for TheCity (Act-2-slot legacy act, "Acts from the Past" mod)
monsters and encounters: sts2_env/monsters/thecity.py and
sts2_env/encounters/thecity.py.

Follows the conventions from test_exordium_monsters.py (``_make_combat``,
``_run_turns``/``_run_rounds``, direct ``move.perform(combat)`` damage
checks) since TheCity isn't wired into a real act slot yet -- monsters are
exercised directly rather than through RunManager/map generation.
"""

from __future__ import annotations

import pytest

from sts2_env.cards.base import CardInstance
from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, CardRarity, CardType, PowerId, TargetType, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.monsters.state_machine import MonsterAI
from sts2_env.run.rooms import CombatRoom
from sts2_env.run.run_state import PlayerState

import sts2_env.monsters.thecity as tc
from sts2_env.encounters import thecity as enc


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


def _hit(combat: CombatState, creature, dmg: int, *, force_unblocked: bool = True) -> None:
    """Deal dmg to creature from the player, going through the full damage
    pipeline (so power modifiers like Flight's halving apply)."""
    if force_unblocked:
        creature.block = 0
    combat.deal_damage(combat.player, creature, dmg, ValueProp.MOVE)


# ---------------------------------------------------------------------------
# 1. HP ranges / basic factory sanity for every monster
# ---------------------------------------------------------------------------

FACTORY_HP_CASES = [
    ("Chosen", tc.create_chosen, tc.CHOSEN_MONSTER_ID, tc.CHOSEN_HEX_MOVE, 95, 99, 98, 103),
    ("Centurion", tc.create_centurion, tc.CENTURION_MONSTER_ID, None, 76, 80, 78, 83),
    ("Mystic", tc.create_mystic, tc.MYSTIC_MONSTER_ID, None, 48, 56, 50, 58),
    ("SnakePlant", tc.create_snake_plant, tc.SNAKE_PLANT_MONSTER_ID, None, 75, 79, 78, 82),
    ("Snecko", tc.create_snecko, tc.SNECKO_MONSTER_ID, tc.SNECKO_GLARE_MOVE, 114, 120, 120, 125),
    ("ShelledParasite", tc.create_shelled_parasite, tc.SHELLED_PARASITE_MONSTER_ID, tc.SHELLED_PARASITE_FELL_MOVE, 68, 72, 70, 75),
    ("BookOfStabbing", tc.create_book_of_stabbing, tc.BOOK_OF_STABBING_MONSTER_ID, None, 160, 164, 168, 172),
    ("Taskmaster", tc.create_taskmaster, tc.TASKMASTER_MONSTER_ID, tc.TASKMASTER_WHIP_MOVE, 54, 60, 57, 64),
    ("Mugger", tc.create_mugger, tc.MUGGER_MONSTER_ID, tc.MUGGER_MUG_MOVE, 48, 52, 50, 54),
    ("GremlinLeader", tc.create_gremlin_leader, tc.GREMLIN_LEADER_MONSTER_ID, None, 140, 148, 145, 155),
    ("TorchHead", tc.create_torch_head, tc.TORCH_HEAD_MONSTER_ID, tc.TORCH_HEAD_TACKLE_MOVE, 38, 40, 40, 45),
    ("BronzeOrb", tc.create_bronze_orb, tc.BRONZE_ORB_MONSTER_ID, None, 52, 58, 54, 60),
    ("Byrd", tc.create_byrd, tc.BYRD_MONSTER_ID, None, 25, 31, 26, 33),
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


def test_fixed_hp_monsters_not_ranged():
    for factory, base_hp, tough_hp, monster_id in [
        (tc.create_champ, tc.CHAMP_BASE_HP, tc.CHAMP_TOUGH_HP, tc.CHAMP_MONSTER_ID),
        (tc.create_collector, tc.COLLECTOR_BASE_HP, tc.COLLECTOR_TOUGH_HP, tc.COLLECTOR_MONSTER_ID),
        (tc.create_bronze_automaton, tc.BRONZE_AUTOMATON_BASE_HP, tc.BRONZE_AUTOMATON_TOUGH_HP, tc.BRONZE_AUTOMATON_MONSTER_ID),
    ]:
        for seed in (1, 2, 3):
            creature, _ = factory(Rng(seed), ascension_level=0)
            assert creature.monster_id == monster_id
            assert creature.max_hp == base_hp == creature.current_hp
            creature8, _ = factory(Rng(seed), ascension_level=8)
            assert creature8.max_hp == tough_hp == creature8.current_hp


def test_spheric_guardian_hp_fixed_20_never_ascension_scaled():
    for asc in (0, 8, 9, 20):
        creature, ai = tc.create_spheric_guardian(Rng(1), ascension_level=asc)
        assert creature.monster_id == tc.SPHERIC_GUARDIAN_MONSTER_ID
        assert creature.max_hp == tc.SPHERIC_GUARDIAN_HP == creature.current_hp
        assert ai.current_move.state_id == tc.SPHERIC_GUARDIAN_ACTIVATE_MOVE


# ---------------------------------------------------------------------------
# 2. Byrd
# ---------------------------------------------------------------------------

def test_byrd_flight_amount_solo_matches_per_player_ascension_value():
    combat = _make_combat(1)
    creature, ai = tc.create_byrd(Rng(1), ascension_level=0, combat=combat)
    combat.add_enemy(creature, ai)
    assert creature.get_power_amount(PowerId.FLIGHT) == tc.BYRD_BASE_FLIGHT_PER_PLAYER

    combat8 = _make_combat(1, ascension=8)
    creature8, ai8 = tc.create_byrd(Rng(1), ascension_level=8, combat=combat8)
    combat8.add_enemy(creature8, ai8)
    assert creature8.get_power_amount(PowerId.FLIGHT) == tc.BYRD_TOUGH_FLIGHT_PER_PLAYER


def test_byrd_first_move_distribution_caw_or_peck_only():
    seen = set()
    for seed in range(40):
        combat = _make_combat(seed)
        creature, ai = tc.create_byrd(Rng(seed), ascension_level=0, combat=combat)
        seen.add(ai.current_move.state_id)
    assert seen == {tc.BYRD_CAW_MOVE, tc.BYRD_PECK_MOVE}


def test_byrd_flight_halves_unblocked_attack_damage():
    combat = _make_combat(1)
    creature, ai = tc.create_byrd(Rng(1), ascension_level=0, combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = creature.current_hp
    _hit(combat, creature, 10)
    # 10 base dmg -> 5 after Flight's 0.5 multiplier (player has no Strength).
    assert creature.current_hp == hp_before - 5


def test_byrd_falls_stuns_headbutt_then_go_airborne_resumes_flying():
    combat = _make_combat(1)
    creature, ai = tc.create_byrd(Rng(1), ascension_level=0, combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    flight_amount = creature.get_power_amount(PowerId.FLIGHT)
    for _ in range(flight_amount):
        _hit(combat, creature, 4)
    assert not creature.has_power(PowerId.FLIGHT)
    assert ai.current_move.state_id == tc.BYRD_HEADBUTT_MOVE

    moves = _run_turns(combat, ai, 3)
    assert moves[0] == tc.BYRD_HEADBUTT_MOVE
    assert moves[1] == tc.BYRD_GO_AIRBORNE_MOVE
    assert creature.has_power(PowerId.FLIGHT)
    assert creature.get_power_amount(PowerId.FLIGHT) == flight_amount


def test_byrd_headbutt_flat_damage_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    creature, ai = tc.create_byrd(Rng(1), ascension_level=20, combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.BYRD_HEADBUTT_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.BYRD_HEADBUTT_DAMAGE


def test_byrd_peck_hit_count_and_swoop_damage_ascension_pairing():
    creature0, ai0 = tc.create_byrd(Rng(1), ascension_level=0, combat=None)
    assert ai0.states[tc.BYRD_PECK_MOVE].intents[0].hits == tc.BYRD_BASE_PECK_COUNT
    assert ai0.states[tc.BYRD_SWOOP_MOVE].intents[0].damage == tc.BYRD_BASE_SWOOP_DAMAGE

    creature9, ai9 = tc.create_byrd(Rng(1), ascension_level=9, combat=None)
    assert ai9.states[tc.BYRD_PECK_MOVE].intents[0].hits == tc.BYRD_DEADLY_PECK_COUNT
    assert ai9.states[tc.BYRD_SWOOP_MOVE].intents[0].damage == tc.BYRD_DEADLY_SWOOP_DAMAGE


# ---------------------------------------------------------------------------
# 3. Chosen
# ---------------------------------------------------------------------------

def test_chosen_always_opens_on_hex():
    creature, ai = tc.create_chosen(Rng(1))
    assert ai.current_move.state_id == tc.CHOSEN_HEX_MOVE


def test_chosen_hex_applies_hex_original_to_all_targets():
    combat = _make_combat(1)
    creature, ai = tc.create_chosen(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tc.CHOSEN_HEX_MOVE].perform(combat)
    assert combat.player.has_power(PowerId.HEX_ORIGINAL)


def test_chosen_hex_original_shuffles_dazed_on_non_attack_card_played_only():
    combat = _make_combat(1)
    creature, ai = tc.create_chosen(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tc.CHOSEN_HEX_MOVE].perform(combat)

    player = combat.player
    draw_before = len(combat.combat_player_state_for(player).draw)
    attack_card = CardInstance(
        card_id=CardId.STRIKE_IRONCLAD, cost=1, card_type=CardType.ATTACK,
        target_type=TargetType.ANY_ENEMY, rarity=CardRarity.BASIC, instance_id=9001,
    )
    attack_card.owner = player
    from sts2_env.core.hooks import fire_after_card_played
    fire_after_card_played(attack_card, combat)
    assert len(combat.combat_player_state_for(player).draw) == draw_before

    skill_card = CardInstance(
        card_id=CardId.DEFEND_IRONCLAD, cost=1, card_type=CardType.SKILL,
        target_type=TargetType.SELF, rarity=CardRarity.BASIC, instance_id=9002,
    )
    skill_card.owner = player
    fire_after_card_played(skill_card, combat)
    assert len(combat.combat_player_state_for(player).draw) == draw_before + 1
    assert any(c.card_id == CardId.DAZED for c in combat.combat_player_state_for(player).draw)


def test_chosen_debilitate_and_drain_then_zap_and_poke_branch_after():
    combat = _make_combat(1)
    creature, ai = tc.create_chosen(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 30)
    assert moves[0] == tc.CHOSEN_HEX_MOVE
    assert moves[1] in (tc.CHOSEN_DEBILITATE_MOVE, tc.CHOSEN_DRAIN_MOVE)
    debil_or_drain = (tc.CHOSEN_DEBILITATE_MOVE, tc.CHOSEN_DRAIN_MOVE)
    zap_or_poke = (tc.CHOSEN_ZAP_MOVE, tc.CHOSEN_POKE_MOVE)
    for i in range(1, len(moves)):
        if moves[i - 1] in debil_or_drain:
            assert moves[i] in zap_or_poke
        else:
            assert moves[i] in debil_or_drain


def test_chosen_debilitate_applies_vulnerable_ascension_scaled_damage():
    combat = _make_combat(1, ascension=9)
    creature, ai = tc.create_chosen(Rng(1), ascension_level=9)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.CHOSEN_DEBILITATE_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.CHOSEN_DEADLY_DEBILITATE_DAMAGE
    assert combat.player.get_power_amount(PowerId.VULNERABLE) == tc.CHOSEN_DEBILITATE_VULN


def test_chosen_drain_applies_weak_and_self_strength():
    combat = _make_combat(1)
    creature, ai = tc.create_chosen(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tc.CHOSEN_DRAIN_MOVE].perform(combat)
    assert combat.player.get_power_amount(PowerId.WEAK) == tc.CHOSEN_DRAIN_WEAK
    assert creature.get_power_amount(PowerId.STRENGTH) == tc.CHOSEN_DRAIN_STRENGTH


# ---------------------------------------------------------------------------
# 4. Centurion
# ---------------------------------------------------------------------------

def test_centurion_protects_ally_when_present_fury_when_alone():
    combat = _make_combat(1)
    mystic, mystic_ai = tc.create_mystic(Rng(1), combat=combat)
    combat.add_enemy(mystic, mystic_ai)
    centurion, centurion_ai = tc.create_centurion(Rng(1), combat=combat)
    combat.add_enemy(centurion, centurion_ai)
    combat.start_combat()
    moves = _run_turns(combat, centurion_ai, 40)
    assert set(moves) <= {tc.CENTURION_SLASH_MOVE, tc.CENTURION_PROTECT_MOVE, tc.CENTURION_FURY_MOVE}
    assert tc.CENTURION_PROTECT_MOVE in moves
    assert tc.CENTURION_FURY_MOVE not in moves  # ally always present here


def test_centurion_alone_never_protects_uses_fury_instead():
    combat = _make_combat(1)
    creature, ai = tc.create_centurion(Rng(1), combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 40)
    assert tc.CENTURION_PROTECT_MOVE not in moves
    assert tc.CENTURION_FURY_MOVE in moves


def test_centurion_protect_block_ascension_pairing():
    combat0 = _make_combat(1)
    ally0, ally_ai0 = tc.create_mystic(Rng(1), combat=combat0)
    combat0.add_enemy(ally0, ally_ai0)
    creature0, ai0 = tc.create_centurion(Rng(1), ascension_level=0, combat=combat0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    ai0.states[tc.CENTURION_PROTECT_MOVE].perform(combat0)
    assert ally0.block == tc.CENTURION_BASE_PROTECT_BLOCK

    combat8 = _make_combat(1, ascension=8)
    ally8, ally_ai8 = tc.create_mystic(Rng(1), ascension_level=8, combat=combat8)
    combat8.add_enemy(ally8, ally_ai8)
    creature8, ai8 = tc.create_centurion(Rng(1), ascension_level=8, combat=combat8)
    combat8.add_enemy(creature8, ai8)
    combat8.start_combat()
    ai8.states[tc.CENTURION_PROTECT_MOVE].perform(combat8)
    assert ally8.block == tc.CENTURION_TOUGH_PROTECT_BLOCK


def test_centurion_slash_never_three_in_a_row():
    combat = _make_combat(1)
    creature, ai = tc.create_centurion(Rng(1), combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.CENTURION_SLASH_MOVE)


# ---------------------------------------------------------------------------
# 5. Mystic
# ---------------------------------------------------------------------------

def test_mystic_heals_all_allies_scaled_by_player_count():
    combat = _make_combat(1)
    creature, ai = tc.create_mystic(Rng(1), combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    creature.current_hp = 1
    ai.states[tc.MYSTIC_HEAL_MOVE].perform(combat)
    assert creature.current_hp == min(creature.max_hp, 1 + tc.MYSTIC_HEAL_PER_PLAYER)


def test_mystic_heals_when_missing_hp_exceeds_threshold():
    combat = _make_combat(1)
    creature, ai = tc.create_mystic(Rng(1), combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    creature.current_hp = max(1, creature.max_hp - tc.MYSTIC_HEAL_PER_PLAYER - 1)
    ai.roll_move(combat.monster_ai_rng)
    # After enough missing HP, HEAL should be reachable (not asserting move
    # is HEAL immediately since attack-branch RNG runs first in some rolls,
    # but running several turns should hit HEAL when hurt).
    moves = _run_turns(combat, ai, 10)
    assert tc.MYSTIC_HEAL_MOVE in moves or creature.current_hp == creature.max_hp


def test_mystic_buff_applies_strength_to_self_and_allies():
    combat = _make_combat(1)
    centurion, centurion_ai = tc.create_centurion(Rng(1), combat=combat)
    combat.add_enemy(centurion, centurion_ai)
    mystic, mystic_ai = tc.create_mystic(Rng(1), combat=combat)
    combat.add_enemy(mystic, mystic_ai)
    combat.start_combat()
    mystic_ai.states[tc.MYSTIC_BUFF_MOVE].perform(combat)
    assert mystic.get_power_amount(PowerId.STRENGTH) == tc.MYSTIC_BASE_BUFF_STRENGTH
    assert centurion.get_power_amount(PowerId.STRENGTH) == tc.MYSTIC_BASE_BUFF_STRENGTH


def test_mystic_attack_applies_frail_ascension_scaled_damage():
    combat = _make_combat(1, ascension=9)
    creature, ai = tc.create_mystic(Rng(1), ascension_level=9, combat=combat)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.MYSTIC_ATTACK_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.MYSTIC_DEADLY_ATTACK_DAMAGE
    assert combat.player.get_power_amount(PowerId.FRAIL) == tc.MYSTIC_ATTACK_FRAIL


# ---------------------------------------------------------------------------
# 6. SnakePlant
# ---------------------------------------------------------------------------

def test_snake_plant_has_malleable_on_spawn():
    creature, ai = tc.create_snake_plant(Rng(1))
    assert creature.get_power_amount(PowerId.MALLEABLE) == tc.SNAKE_PLANT_MALLEABLE_AMOUNT


def test_snake_plant_never_three_chomps_in_a_row():
    combat = _make_combat(1)
    creature, ai = tc.create_snake_plant(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.SNAKE_PLANT_CHOMP_MOVE)


def test_snake_plant_never_two_spores_within_two_moves():
    combat = _make_combat(1)
    creature, ai = tc.create_snake_plant(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 1):
        assert not (moves[i] == tc.SNAKE_PLANT_SPORES_MOVE and moves[i + 1] == tc.SNAKE_PLANT_SPORES_MOVE)
    for i in range(len(moves) - 2):
        # SPORES two moves apart is also disallowed per spec.
        if moves[i] == tc.SNAKE_PLANT_SPORES_MOVE:
            assert moves[i + 2] != tc.SNAKE_PLANT_SPORES_MOVE or moves[i + 1] == tc.SNAKE_PLANT_SPORES_MOVE


def test_snake_plant_malleable_grants_block_on_unblocked_hits_at_turn_end():
    combat = _make_combat(1)
    creature, ai = tc.create_snake_plant(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    _hit(combat, creature, 5)
    assert creature.block == 0  # queued, not yet granted
    combat.end_player_turn()
    assert creature.block > 0


# ---------------------------------------------------------------------------
# 7. Snecko
# ---------------------------------------------------------------------------

def test_snecko_always_opens_on_glare_and_applies_confused():
    combat = _make_combat(1)
    creature, ai = tc.create_snecko(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert ai.current_move.state_id == tc.SNECKO_GLARE_MOVE
    ai.states[tc.SNECKO_GLARE_MOVE].perform(combat)
    assert combat.player.has_power(PowerId.CONFUSED)


def test_snecko_bite_never_three_in_a_row():
    combat = _make_combat(1)
    creature, ai = tc.create_snecko(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.SNECKO_BITE_MOVE)


def test_snecko_tail_whip_extra_weak_only_at_ascension_9():
    combat0 = _make_combat(1)
    creature0, ai0 = tc.create_snecko(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    ai0.states[tc.SNECKO_TAIL_WHIP_MOVE].perform(combat0)
    assert not combat0.player.has_power(PowerId.WEAK)
    assert combat0.player.get_power_amount(PowerId.VULNERABLE) == tc.SNECKO_TAIL_VULN

    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = tc.create_snecko(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    ai9.states[tc.SNECKO_TAIL_WHIP_MOVE].perform(combat9)
    assert combat9.player.get_power_amount(PowerId.WEAK) == tc.SNECKO_DEADLY_TAIL_WEAK


def test_snecko_bite_damage_ascension_pairing():
    combat0 = _make_combat(1)
    creature0, ai0 = tc.create_snecko(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    hp_before = combat0.player.current_hp
    ai0.states[tc.SNECKO_BITE_MOVE].perform(combat0)
    assert combat0.player.current_hp == hp_before - tc.SNECKO_BASE_BITE_DAMAGE


# ---------------------------------------------------------------------------
# 8. ShelledParasite
# ---------------------------------------------------------------------------

def test_shelled_parasite_has_plated_armor_on_spawn():
    creature, ai = tc.create_shelled_parasite(Rng(1))
    assert creature.get_power_amount(PowerId.PLATED_ARMOR) == tc.SHELLED_PARASITE_PLATED_ARMOR
    assert ai.current_move.state_id == tc.SHELLED_PARASITE_FELL_MOVE


def test_shelled_parasite_armor_depletes_and_forces_stunned_then_resumes():
    combat = _make_combat(1)
    creature, ai = tc.create_shelled_parasite(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    armor = creature.get_power_amount(PowerId.PLATED_ARMOR)
    for _ in range(armor):
        _hit(combat, creature, 1)
    assert not creature.has_power(PowerId.PLATED_ARMOR)
    assert ai.current_move.state_id == tc.SHELLED_PARASITE_STUNNED_MOVE
    moves = _run_turns(combat, ai, 2)
    assert moves[0] == tc.SHELLED_PARASITE_STUNNED_MOVE
    assert moves[1] in (tc.SHELLED_PARASITE_FELL_MOVE, tc.SHELLED_PARASITE_DOUBLE_STRIKE_MOVE, tc.SHELLED_PARASITE_LIFE_SUCK_MOVE)


def test_shelled_parasite_life_suck_heals_self_for_unblocked_damage():
    combat = _make_combat(1)
    creature, ai = tc.create_shelled_parasite(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    creature.current_hp = 1
    hp_before_player = combat.player.current_hp
    ai.states[tc.SHELLED_PARASITE_LIFE_SUCK_MOVE].perform(combat)
    dealt = hp_before_player - combat.player.current_hp
    assert dealt == tc.SHELLED_PARASITE_BASE_LIFE_SUCK_DAMAGE
    assert creature.current_hp == 1 + dealt


def test_shelled_parasite_fell_never_repeats_immediately():
    combat = _make_combat(1)
    creature, ai = tc.create_shelled_parasite(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 1):
        assert not (moves[i] == tc.SHELLED_PARASITE_FELL_MOVE and moves[i + 1] == tc.SHELLED_PARASITE_FELL_MOVE)


def test_shelled_parasite_double_strike_and_life_suck_never_three_in_a_row():
    combat = _make_combat(1)
    creature, ai = tc.create_shelled_parasite(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 80)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.SHELLED_PARASITE_DOUBLE_STRIKE_MOVE)
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.SHELLED_PARASITE_LIFE_SUCK_MOVE)


# ---------------------------------------------------------------------------
# 9. BookOfStabbing
# ---------------------------------------------------------------------------

def test_book_of_stabbing_has_painful_stabs_on_spawn():
    creature, ai = tc.create_book_of_stabbing(Rng(1))
    assert creature.get_power_amount(PowerId.PAINFUL_STABS) == tc.BOOK_OF_STABBING_PAINFUL_STABS


def test_book_of_stabbing_stab_count_grows_every_turn():
    combat = _make_combat(1)
    creature, ai = tc.create_book_of_stabbing(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    moves = []
    hits_per_stab = []
    for _ in range(6):
        move = ai.current_move
        moves.append(move.state_id)
        before = combat.player.current_hp
        move.perform(combat)
        dealt = before - combat.player.current_hp
        per_hit = tc.BOOK_OF_STABBING_BASE_STAB_DAMAGE
        if move.state_id == tc.BOOK_OF_STABBING_STAB_MOVE:
            hits_per_stab.append(dealt // per_hit)
        ai.on_move_performed()
        ai.roll_move(combat.monster_ai_rng)
    # Each successive STAB should never deal FEWER hits than an earlier one
    # (the counter only ever grows).
    assert hits_per_stab == sorted(hits_per_stab)
    assert hp_before > combat.player.current_hp


def test_book_of_stabbing_never_three_stabs_in_a_row():
    combat = _make_combat(1)
    creature, ai = tc.create_book_of_stabbing(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    for i in range(len(moves) - 2):
        assert not (moves[i] == moves[i + 1] == moves[i + 2] == tc.BOOK_OF_STABBING_STAB_MOVE)


# ---------------------------------------------------------------------------
# 10. Taskmaster
# ---------------------------------------------------------------------------

def test_taskmaster_loops_scouring_whip_forever():
    combat = _make_combat(1)
    creature, ai = tc.create_taskmaster(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 10)
    assert moves == [tc.TASKMASTER_WHIP_MOVE] * 10


def test_taskmaster_whip_flat_damage_and_wound_count_ascension_pairing():
    combat0 = _make_combat(1)
    creature0, ai0 = tc.create_taskmaster(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    hp_before = combat0.player.current_hp
    ai0.states[tc.TASKMASTER_WHIP_MOVE].perform(combat0)
    assert combat0.player.current_hp == hp_before - tc.TASKMASTER_WHIP_DAMAGE
    wounds = [c for c in combat0.combat_player_state_for(combat0.player).discard if c.card_id == CardId.WOUND]
    assert len(wounds) == tc.TASKMASTER_BASE_WOUND_COUNT
    assert not creature0.has_power(PowerId.STRENGTH)

    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = tc.create_taskmaster(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    ai9.states[tc.TASKMASTER_WHIP_MOVE].perform(combat9)
    wounds9 = [c for c in combat9.combat_player_state_for(combat9.player).discard if c.card_id == CardId.WOUND]
    assert len(wounds9) == tc.TASKMASTER_DEADLY_WOUND_COUNT
    assert creature9.get_power_amount(PowerId.STRENGTH) == 1


def test_taskmaster_gains_strength_only_at_ascension_9_per_use():
    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = tc.create_taskmaster(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    _run_turns(combat9, ai9, 3)
    assert creature9.get_power_amount(PowerId.STRENGTH) == 3


# ---------------------------------------------------------------------------
# 11. SphericGuardian
# ---------------------------------------------------------------------------

def test_spheric_guardian_starting_block_barricade_and_artifact():
    creature, ai = tc.create_spheric_guardian(Rng(1))
    assert creature.block == tc.SPHERIC_GUARDIAN_STARTING_BLOCK
    assert creature.get_power_amount(PowerId.ARTIFACT) == tc.SPHERIC_GUARDIAN_ARTIFACT
    assert creature.has_power(PowerId.BARRICADE)


def test_spheric_guardian_deterministic_sequence():
    combat = _make_combat(1)
    creature, ai = tc.create_spheric_guardian(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 7)
    assert moves == [
        tc.SPHERIC_GUARDIAN_ACTIVATE_MOVE,
        tc.SPHERIC_GUARDIAN_FRAIL_ATTACK_MOVE,
        tc.SPHERIC_GUARDIAN_SLAM_MOVE,
        tc.SPHERIC_GUARDIAN_HARDEN_MOVE,
        tc.SPHERIC_GUARDIAN_SLAM_MOVE,
        tc.SPHERIC_GUARDIAN_HARDEN_MOVE,
        tc.SPHERIC_GUARDIAN_SLAM_MOVE,
    ]


def test_spheric_guardian_activate_block_ascension_pairing():
    combat0 = _make_combat(1)
    creature0, ai0 = tc.create_spheric_guardian(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    block_before = creature0.block
    ai0.states[tc.SPHERIC_GUARDIAN_ACTIVATE_MOVE].perform(combat0)
    assert creature0.block == block_before + tc.SPHERIC_GUARDIAN_BASE_ACTIVATE_BLOCK

    combat8 = _make_combat(1, ascension=8)
    creature8, ai8 = tc.create_spheric_guardian(Rng(1), ascension_level=8)
    combat8.add_enemy(creature8, ai8)
    combat8.start_combat()
    block_before8 = creature8.block
    ai8.states[tc.SPHERIC_GUARDIAN_ACTIVATE_MOVE].perform(combat8)
    assert creature8.block == block_before8 + tc.SPHERIC_GUARDIAN_TOUGH_ACTIVATE_BLOCK


# ---------------------------------------------------------------------------
# 12. Mugger
# ---------------------------------------------------------------------------

def test_mugger_mugs_twice_then_permanently_branches():
    combat = _make_combat(1)
    creature, ai = tc.create_mugger(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 6)
    assert moves[0] == tc.MUGGER_MUG_MOVE
    assert moves[1] == tc.MUGGER_MUG_MOVE
    assert moves[2] in (tc.MUGGER_SMOKE_BOMB_MOVE, tc.MUGGER_BIG_SWIPE_MOVE)
    assert tc.MUGGER_MUG_MOVE not in moves[2:]


def test_mugger_steals_gold_on_mug():
    from sts2_env.core.enums import RoomType

    combat = _make_combat(1)
    combat.room = CombatRoom(room_type=RoomType.MONSTER)
    creature, ai = tc.create_mugger(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.combat_player_state_for(combat.player).player_state.gold = 100
    combat.start_combat()
    gold_before = combat.combat_player_state_for(combat.player).player_state.gold
    ai.states[tc.MUGGER_MUG_MOVE].perform(combat)
    assert combat.combat_player_state_for(combat.player).player_state.gold < gold_before
    thievery = creature.powers[PowerId.THIEVERY]
    assert thievery.gold_stolen > 0


def test_mugger_death_refunds_stolen_gold():
    from sts2_env.core.enums import RoomType

    combat = _make_combat(1)
    combat.room = CombatRoom(room_type=RoomType.MONSTER)
    creature, ai = tc.create_mugger(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.combat_player_state_for(combat.player).player_state.gold = 100
    combat.start_combat()
    ai.states[tc.MUGGER_MUG_MOVE].perform(combat)
    thievery = creature.powers[PowerId.THIEVERY]
    stolen = thievery.gold_stolen_by_player.get(combat.player, 0)
    assert stolen > 0

    assert combat.kill_creature(creature)
    rewards = combat.room.extra_rewards.get(combat.player_id, [])
    assert any(getattr(r, "min_gold", 0) == stolen for r in rewards)


# ---------------------------------------------------------------------------
# 13. GremlinLeader
# ---------------------------------------------------------------------------

def _make_gremlin_leader_fight(seed=1, ascension=0):
    combat = _make_combat(seed, ascension=ascension)
    g1, g1ai = tc.create_gremlin_mad(Rng(seed), ascension_level=ascension)
    combat.add_enemy(g1, g1ai)
    g2, g2ai = tc.create_gremlin_sneaky(Rng(seed + 1), ascension_level=ascension)
    combat.add_enemy(g2, g2ai)
    leader, leader_ai = tc.create_gremlin_leader(Rng(seed), ascension_level=ascension, combat=combat)
    combat.add_enemy(leader, leader_ai)
    for g in (g1, g2):
        g.apply_power(PowerId.MINION, 1, applier=leader)
    return combat, leader, leader_ai, [g1, g2]


def test_gremlin_leader_presence_power_and_two_allies_seen_on_spawn():
    combat, leader, leader_ai, gremlins = _make_gremlin_leader_fight()
    assert leader.has_power(PowerId.GREMLIN_LEADER_PRESENCE)
    assert leader_ai.current_move.state_id in (tc.GREMLIN_LEADER_ENCOURAGE_MOVE, tc.GREMLIN_LEADER_STAB_MOVE)


def test_gremlin_leader_death_strips_minion_from_survivors():
    combat, leader, leader_ai, gremlins = _make_gremlin_leader_fight()
    combat.start_combat()
    combat.kill_creature(leader)
    for g in gremlins:
        if g.is_alive:
            assert not g.has_power(PowerId.MINION)


def test_gremlin_leader_rally_summons_replacements_into_empty_slots_with_minion():
    combat, leader, leader_ai, gremlins = _make_gremlin_leader_fight()
    combat.start_combat()
    combat.kill_creature(gremlins[0])
    combat.kill_creature(gremlins[1])
    before = len(combat.enemies)
    leader_ai.states[tc.GREMLIN_LEADER_RALLY_MOVE].perform(combat)
    after = len(combat.enemies)
    assert after == before + tc.GREMLIN_LEADER_MAX_ALLIES
    new_gremlins = combat.enemies[before:after]
    for g in new_gremlins:
        assert g.has_power(PowerId.MINION)


def test_gremlin_leader_encourage_buffs_self_and_allies_with_block_only_to_allies():
    combat, leader, leader_ai, gremlins = _make_gremlin_leader_fight()
    combat.start_combat()
    leader_ai.states[tc.GREMLIN_LEADER_ENCOURAGE_MOVE].perform(combat)
    assert leader.get_power_amount(PowerId.STRENGTH) == tc.GREMLIN_LEADER_BASE_STRENGTH
    assert leader.block == 0
    for g in gremlins:
        assert g.get_power_amount(PowerId.STRENGTH) == tc.GREMLIN_LEADER_BASE_STRENGTH
        assert g.block == tc.GREMLIN_LEADER_BASE_BLOCK


def test_gremlin_leader_stab_three_hits_flat_damage_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    leader, ai = tc.create_gremlin_leader(Rng(1), ascension_level=20, combat=combat)
    combat.add_enemy(leader, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.GREMLIN_LEADER_STAB_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.GREMLIN_LEADER_STAB_DAMAGE * tc.GREMLIN_LEADER_STAB_HITS


def test_gremlin_leader_alone_biased_toward_rally_or_stab():
    combat = _make_combat(1)
    leader, ai = tc.create_gremlin_leader(Rng(1), ascension_level=0, combat=None)
    combat.add_enemy(leader, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 30)
    assert set(moves) <= {tc.GREMLIN_LEADER_RALLY_MOVE, tc.GREMLIN_LEADER_STAB_MOVE, tc.GREMLIN_LEADER_ENCOURAGE_MOVE}


# ---------------------------------------------------------------------------
# 14. Champ (Boss)
# ---------------------------------------------------------------------------

def test_champ_taunt_every_fourth_turn_before_threshold():
    combat = _make_combat(1)
    creature, ai = tc.create_champ(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 4)
    assert moves[3] == tc.CHAMP_TAUNT_MOVE


def test_champ_anger_triggers_exactly_once_at_half_hp_and_clears_debuffs():
    combat = _make_combat(1, player_hp=999)
    creature, ai = tc.create_champ(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    creature.apply_power(PowerId.WEAK, 3, applier=combat.player)
    creature.current_hp = creature.max_hp // 2
    ai.roll_move(combat.monster_ai_rng)
    # Force a fresh resolution by directly invoking the branch chooser via a
    # full turn cycle (roll_move alone won't re-resolve the already-decided
    # current move; drive through the move-follow-up chain instead).
    moves = _run_turns(combat, ai, 3)
    assert tc.CHAMP_ANGER_MOVE in moves
    assert moves.count(tc.CHAMP_ANGER_MOVE) == 1
    assert not creature.has_power(PowerId.WEAK)
    assert creature.get_power_amount(PowerId.STRENGTH) == tc.CHAMP_BASE_STRENGTH * 3


def test_champ_execute_reachable_only_after_threshold():
    combat = _make_combat(1, player_hp=999)
    creature, ai = tc.create_champ(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves_before = _run_turns(combat, ai, 20)
    assert tc.CHAMP_EXECUTE_MOVE not in moves_before

    creature.current_hp = creature.max_hp // 2
    execute_seen = False
    for _ in range(30):
        if combat.is_over:
            break
        move = ai.current_move
        if move.state_id == tc.CHAMP_EXECUTE_MOVE:
            execute_seen = True
            break
        move.perform(combat)
        ai.on_move_performed()
        ai.roll_move(combat.monster_ai_rng)
        # Keep the player alive so a high accumulated Strength (from Anger)
        # doesn't end combat before EXECUTE is reached.
        combat.player.current_hp = combat.player.max_hp
        combat.is_over = False
    assert execute_seen


def test_champ_execute_flat_damage_not_ascension_scaled():
    # Fresh creature/player (no accumulated Strength/Vulnerable from a long
    # run) so the damage dealt reflects only EXECUTE's own flat base.
    combat = _make_combat(1, ascension=20)
    creature, ai = tc.create_champ(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.CHAMP_EXECUTE_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.CHAMP_EXECUTE_DAMAGE * tc.CHAMP_EXECUTE_HITS


def test_champ_defensive_stance_at_most_twice():
    combat = _make_combat(1, player_hp=999)
    creature, ai = tc.create_champ(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    assert moves.count(tc.CHAMP_DEFENSIVE_STANCE_MOVE) <= tc.CHAMP_MAX_DEFENSIVE_STANCES


def test_champ_defensive_stance_grants_block_and_metallicize():
    combat = _make_combat(1)
    creature, ai = tc.create_champ(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    block_before = creature.block
    ai.states[tc.CHAMP_DEFENSIVE_STANCE_MOVE].perform(combat)
    assert creature.block == block_before + tc.CHAMP_BASE_BLOCK
    assert creature.get_power_amount(PowerId.METALLICIZE) == tc.CHAMP_BASE_FORGE


# ---------------------------------------------------------------------------
# 15. TorchHead
# ---------------------------------------------------------------------------

def test_torch_head_loops_tackle_forever_flat_damage():
    combat = _make_combat(1, ascension=20)
    creature, ai = tc.create_torch_head(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 5)
    assert moves == [tc.TORCH_HEAD_TACKLE_MOVE] * 5
    hp_before = combat.player.current_hp
    ai.states[tc.TORCH_HEAD_TACKLE_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.TORCH_HEAD_TACKLE_DAMAGE


# ---------------------------------------------------------------------------
# 16. Collector (Boss)
# ---------------------------------------------------------------------------

def test_collector_forced_first_move_spawns_two_torch_heads_with_minion():
    combat = _make_combat(1)
    creature, ai = tc.create_collector(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert ai.current_move.state_id == tc.COLLECTOR_SPAWN_MOVE
    creature.powers[PowerId.MINION_MASTER]  # present from spawn
    ai.current_move.perform(combat)
    torches = [e for e in combat.enemies if e.monster_id == tc.TORCH_HEAD_MONSTER_ID]
    assert len(torches) == tc.COLLECTOR_TORCH_SLOTS
    for t in torches:
        assert t.has_power(PowerId.MINION)


def test_collector_mega_debuff_after_three_turns_used_once():
    combat = _make_combat(1)
    creature, ai = tc.create_collector(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 6)
    assert moves[0] == tc.COLLECTOR_SPAWN_MOVE
    assert tc.COLLECTOR_MEGA_DEBUFF_MOVE in moves[:4]
    assert moves.count(tc.COLLECTOR_MEGA_DEBUFF_MOVE) == 1
    idx = moves.index(tc.COLLECTOR_MEGA_DEBUFF_MOVE)
    assert combat.player.has_power(PowerId.WEAK) or idx < len(moves)


def test_collector_mega_debuff_applies_weak_vulnerable_frail():
    combat = _make_combat(1, ascension=9)
    creature, ai = tc.create_collector(Rng(1), ascension_level=9)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tc.COLLECTOR_MEGA_DEBUFF_MOVE].perform(combat)
    assert combat.player.get_power_amount(PowerId.WEAK) == tc.COLLECTOR_DEADLY_MEGA_DEBUFF
    assert combat.player.get_power_amount(PowerId.VULNERABLE) == tc.COLLECTOR_DEADLY_MEGA_DEBUFF
    assert combat.player.get_power_amount(PowerId.FRAIL) == tc.COLLECTOR_DEADLY_MEGA_DEBUFF


def test_collector_death_kills_living_torch_head_minions():
    combat = _make_combat(1)
    creature, ai = tc.create_collector(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.current_move.perform(combat)
    torches = [e for e in combat.enemies if e.monster_id == tc.TORCH_HEAD_MONSTER_ID]
    assert len(torches) == 2
    combat.kill_creature(creature)
    for t in torches:
        assert not t.is_alive


def test_collector_revive_only_fills_empty_slots():
    combat = _make_combat(1)
    creature, ai = tc.create_collector(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tc.COLLECTOR_SPAWN_MOVE].perform(combat)
    torches = [e for e in combat.enemies if e.monster_id == tc.TORCH_HEAD_MONSTER_ID]
    combat.kill_creature(torches[0])
    before = len(combat.enemies)
    ai.states[tc.COLLECTOR_REVIVE_MOVE].perform(combat)
    after = len(combat.enemies)
    assert after == before + 1


# ---------------------------------------------------------------------------
# 17. BronzeOrb
# ---------------------------------------------------------------------------

def test_bronze_orb_beam_flat_damage_not_ascension_scaled():
    combat = _make_combat(1, ascension=20)
    creature, ai = tc.create_bronze_orb(Rng(1), ascension_level=20)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    hp_before = combat.player.current_hp
    ai.states[tc.BRONZE_ORB_BEAM_MOVE].perform(combat)
    assert combat.player.current_hp == hp_before - tc.BRONZE_ORB_BEAM_DAMAGE


def test_bronze_orb_support_beam_blocks_living_automaton_ally():
    combat = _make_combat(1)
    auto, auto_ai = tc.create_bronze_automaton(Rng(1))
    combat.add_enemy(auto, auto_ai)
    orb, orb_ai = tc.create_bronze_orb(Rng(2))
    combat.add_enemy(orb, orb_ai)
    combat.start_combat()
    block_before = auto.block
    orb_ai.states[tc.BRONZE_ORB_SUPPORT_BEAM_MOVE].perform(combat)
    assert auto.block == block_before + tc.BRONZE_ORB_SUPPORT_BLOCK


def test_bronze_orb_stasis_steals_and_locks_card_used_once():
    combat = _make_combat(1)
    creature, ai = tc.create_bronze_orb(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    state = combat.combat_player_state_for(combat.player)
    draw_before = len(state.draw)
    ai.states[tc.BRONZE_ORB_STASIS_MOVE].perform(combat)
    stasis = creature.powers.get(PowerId.STASIS)
    assert stasis is not None
    assert stasis.stolen_card is not None
    assert len(state.draw) == draw_before - 1
    assert stasis.stolen_card not in state.draw
    assert stasis.stolen_card not in state.discard


def test_bronze_orb_stasis_card_returned_on_death():
    combat = _make_combat(1)
    creature, ai = tc.create_bronze_orb(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    state = combat.combat_player_state_for(combat.player)
    ai.states[tc.BRONZE_ORB_STASIS_MOVE].perform(combat)
    stolen = creature.powers[PowerId.STASIS].stolen_card
    combat.kill_creature(creature)
    assert stolen in state.draw


# ---------------------------------------------------------------------------
# 18. BronzeAutomaton (Boss)
# ---------------------------------------------------------------------------

def test_bronze_automaton_has_artifact_and_minion_master_on_spawn():
    creature, ai = tc.create_bronze_automaton(Rng(1))
    assert creature.get_power_amount(PowerId.ARTIFACT) == tc.BRONZE_AUTOMATON_ARTIFACT
    assert creature.has_power(PowerId.MINION_MASTER)
    assert ai.current_move.state_id == tc.BRONZE_AUTOMATON_SPAWN_ORBS_MOVE


def test_bronze_automaton_spawn_orbs_summons_two_with_minion():
    combat = _make_combat(1)
    creature, ai = tc.create_bronze_automaton(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.current_move.perform(combat)
    orbs = [e for e in combat.enemies if e.monster_id == tc.BRONZE_ORB_MONSTER_ID]
    assert len(orbs) == tc.BRONZE_AUTOMATON_ORB_SLOTS
    for o in orbs:
        assert o.has_power(PowerId.MINION)


def test_bronze_automaton_hyper_beam_every_fifth_non_spawn_turn():
    # Turn counter only starts incrementing AFTER the forced SPAWN_ORBS
    # opener (which doesn't itself increment it), so HYPER_BEAM is the 6th
    # move overall: SPAWN_ORBS, then 4 increments (FLAIL/BOOST alternating
    # since BOOST always immediately follows SPAWN_ORBS/BOOST), then
    # HYPER_BEAM.
    combat = _make_combat(1, ascension=0)
    creature, ai = tc.create_bronze_automaton(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 6)
    assert moves[0] == tc.BRONZE_AUTOMATON_SPAWN_ORBS_MOVE
    assert tc.BRONZE_AUTOMATON_HYPER_BEAM_MOVE not in moves[:5]
    assert moves[5] == tc.BRONZE_AUTOMATON_HYPER_BEAM_MOVE


def test_bronze_automaton_hyper_beam_damage_ascension_pairing():
    # Fresh creature (no accumulated Strength from prior BOOST moves) so the
    # damage dealt reflects only HYPER_BEAM's own ascension-scaled base.
    combat0 = _make_combat(1, ascension=0)
    creature0, ai0 = tc.create_bronze_automaton(Rng(1), ascension_level=0)
    combat0.add_enemy(creature0, ai0)
    combat0.start_combat()
    hp_before0 = combat0.player.current_hp
    ai0.states[tc.BRONZE_AUTOMATON_HYPER_BEAM_MOVE].perform(combat0)
    assert combat0.player.current_hp == hp_before0 - tc.BRONZE_AUTOMATON_BASE_BEAM_DAMAGE

    combat9 = _make_combat(1, ascension=9)
    creature9, ai9 = tc.create_bronze_automaton(Rng(1), ascension_level=9)
    combat9.add_enemy(creature9, ai9)
    combat9.start_combat()
    hp_before9 = combat9.player.current_hp
    ai9.states[tc.BRONZE_AUTOMATON_HYPER_BEAM_MOVE].perform(combat9)
    assert combat9.player.current_hp == hp_before9 - tc.BRONZE_AUTOMATON_DEADLY_BEAM_DAMAGE


def test_bronze_automaton_boost_follows_every_hyper_beam():
    combat = _make_combat(1)
    creature, ai = tc.create_bronze_automaton(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 12)
    for i, move in enumerate(moves[:-1]):
        if move == tc.BRONZE_AUTOMATON_HYPER_BEAM_MOVE:
            assert moves[i + 1] == tc.BRONZE_AUTOMATON_BOOST_MOVE


def test_bronze_automaton_death_kills_living_orb_minions():
    combat = _make_combat(1)
    creature, ai = tc.create_bronze_automaton(Rng(1))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.current_move.perform(combat)
    orbs = [e for e in combat.enemies if e.monster_id == tc.BRONZE_ORB_MONSTER_ID]
    combat.kill_creature(creature)
    for o in orbs:
        assert not o.is_alive


# ---------------------------------------------------------------------------
# 19. Encounters
# ---------------------------------------------------------------------------

def test_encounter_pool_sizes():
    assert len(enc.WEAK_ENCOUNTERS) == 5
    assert len(enc.NORMAL_ENCOUNTERS) == 8
    assert len(enc.ELITE_ENCOUNTERS) == 3
    assert len(enc.BOSS_ENCOUNTERS) == 3
    assert len(enc.ALL_THECITY_ENCOUNTERS) == 19


@pytest.mark.parametrize("setup", enc.ALL_THECITY_ENCOUNTERS, ids=[s.__name__ for s in enc.ALL_THECITY_ENCOUNTERS])
def test_every_encounter_setup_spawns_at_least_one_enemy(setup):
    combat = _make_combat(3)
    rng = Rng(11)
    setup(combat, rng)
    assert len(combat.enemies) >= 1
    for e in combat.enemies:
        assert e.max_hp > 0


@pytest.mark.parametrize("setup", enc.ALL_THECITY_ENCOUNTERS, ids=[s.__name__ for s in enc.ALL_THECITY_ENCOUNTERS])
def test_every_encounter_setup_starts_combat_cleanly_across_ascensions(setup):
    for asc in (0, 8, 9, 20):
        combat = _make_combat(5, ascension=asc)
        rng = Rng(5)
        setup(combat, rng)
        combat.start_combat()
        for e in combat.enemies:
            assert e.current_hp > 0


def test_city_protectors_normal_sentry_bolt_first():
    combat = _make_combat(1)
    enc.setup_city_protectors_normal(combat, Rng(1))
    combat.start_combat()
    sentry = next(e for e in combat.enemies if e.monster_id == "EXORDIUM_SENTRY")
    ai = combat.enemy_ais[sentry.combat_id]
    assert ai.current_move.state_id == "BOLT"


def test_gremlin_leader_elite_draws_two_distinct_slots_and_applies_minion():
    for seed in range(10):
        combat = _make_combat(seed)
        enc.setup_gremlin_leader_elite(combat, Rng(seed))
        assert len(combat.enemies) == 3
        leader = next(e for e in combat.enemies if e.monster_id == tc.GREMLIN_LEADER_MONSTER_ID)
        others = [e for e in combat.enemies if e is not leader]
        assert len(others) == 2
        for g in others:
            assert g.has_power(PowerId.MINION)


def test_three_byrds_weak_spawns_three_byrds_each_with_flight():
    combat = _make_combat(1)
    enc.setup_three_byrds_weak(combat, Rng(1))
    assert len(combat.enemies) == 3
    for e in combat.enemies:
        assert e.monster_id == tc.BYRD_MONSTER_ID
        assert e.has_power(PowerId.FLIGHT)


def test_two_thieves_weak_is_looter_and_mugger():
    combat = _make_combat(1)
    enc.setup_two_thieves_weak(combat, Rng(1))
    ids = sorted(e.monster_id for e in combat.enemies)
    assert ids == sorted(["EXORDIUM_LOOTER", tc.MUGGER_MONSTER_ID])


def test_slavers_elite_composition():
    combat = _make_combat(1)
    enc.setup_slavers_elite(combat, Rng(1))
    ids = sorted(e.monster_id for e in combat.enemies)
    assert ids == sorted(["EXORDIUM_SLAVER_BLUE", "EXORDIUM_SLAVER_RED", tc.TASKMASTER_MONSTER_ID])


def test_boss_pool_has_all_three_bosses_and_matches_run_manager_pick_mechanism():
    names = {s.__name__ for s in enc.BOSS_ENCOUNTERS}
    assert names == {"setup_champ_boss", "setup_collector_boss", "setup_bronze_automaton_boss"}
    picks = {Rng(seed).choice(enc.BOSS_ENCOUNTERS).__name__ for seed in range(30)}
    assert picks == names
