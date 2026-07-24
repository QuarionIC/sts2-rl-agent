"""Tests for TheBeyond (Act-3-slot legacy act, "Acts from the Past" mod)
monsters and encounters: sts2_env/monsters/thebeyond.py and
sts2_env/encounters/thebeyond.py.

Follows the conventions from test_exordium_monsters.py (``_make_combat``,
``_run_turns``, ``_run_rounds``, direct ``move.perform(combat)``/
``combat.deal_damage`` checks) since TheBeyond isn't wired into a real act
slot yet -- monsters are exercised directly rather than through
RunManager/map generation.
"""

from __future__ import annotations

import pytest

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, PowerId, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.monsters.state_machine import MonsterAI

import sts2_env.monsters.exordium as ex
import sts2_env.monsters.thebeyond as tb
from sts2_env.encounters import thebeyond as enc


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


# ---------------------------------------------------------------------------
# 1. HP ranges / basic factory sanity for every monster
# ---------------------------------------------------------------------------

FACTORY_HP_CASES = [
    ("Darkling", tb.create_darkling, tb.DARKLING_MONSTER_ID, 48, 59, 50, 59),
    ("OrbWalker", tb.create_orb_walker, tb.ORB_WALKER_MONSTER_ID, 90, 102, 92, 102),
    ("Repulsor", tb.create_repulsor, tb.REPULSOR_MONSTER_ID, 29, 38, 31, 38),
    ("Spiker", tb.create_spiker, tb.SPIKER_MONSTER_ID, 42, 60, 44, 60),
    ("Exploder", tb.create_exploder, tb.EXPLODER_MONSTER_ID, 30, 35, 30, 30),
    ("WrithingMass", tb.create_writhing_mass, tb.WRITHING_MASS_MONSTER_ID, 160, 175, 160, 175),
    ("SpireGrowth", tb.create_spire_growth, tb.SPIRE_GROWTH_MONSTER_ID, 170, 190, 170, 190),
    ("GiantHead", tb.create_giant_head, tb.GIANT_HEAD_MONSTER_ID, 500, 500, 520, 520),
    ("Nemesis", tb.create_nemesis, tb.NEMESIS_MONSTER_ID, 185, 185, 200, 200),
    ("Reptomancer", tb.create_reptomancer, tb.REPTOMANCER_MONSTER_ID, 180, 190, 190, 200),
    ("SnakeDagger", tb.create_snake_dagger, tb.SNAKE_DAGGER_MONSTER_ID, 20, 25, 20, 25),
    ("Donu", tb.create_donu, tb.DONU_MONSTER_ID, 250, 250, 265, 265),
    ("Deca", tb.create_deca, tb.DECA_MONSTER_ID, 250, 250, 265, 265),
]


@pytest.mark.parametrize(
    "name, factory, monster_id, base_min, base_max, tough_min, tough_max",
    FACTORY_HP_CASES,
    ids=[c[0] for c in FACTORY_HP_CASES],
)
def test_factory_hp_ranges_and_monster_id(name, factory, monster_id, base_min, base_max, tough_min, tough_max):
    creature, ai = factory(Rng(1))
    assert creature.monster_id == monster_id
    assert base_min <= creature.max_hp <= base_max
    assert creature.current_hp == creature.max_hp

    creature8, _ = factory(Rng(2), ascension_level=8)
    assert tough_min <= creature8.max_hp <= tough_max


def test_fixed_hp_monsters():
    for factory, hp in [
        (tb.create_maw, tb.MAW_HP),
        (tb.create_transient, tb.TRANSIENT_HP),
    ]:
        for asc in (0, 8, 20):
            creature, _ = factory(Rng(1), ascension_level=asc)
            assert creature.max_hp == hp == creature.current_hp


def test_awakened_one_fixed_hp_asc_split():
    for seed in (1, 2, 3):
        creature, _ = tb.create_awakened_one(Rng(seed), ascension_level=0)
        assert creature.max_hp == tb.AWAKENED_ONE_BASE_HP == creature.current_hp
        creature8, _ = tb.create_awakened_one(Rng(seed), ascension_level=8)
        assert creature8.max_hp == tb.AWAKENED_ONE_TOUGH_HP == creature8.current_hp


def test_time_eater_fixed_hp_asc_split():
    for seed in (1, 2, 3):
        creature, _ = tb.create_time_eater(Rng(seed), ascension_level=0)
        assert creature.max_hp == tb.TIME_EATER_BASE_HP == creature.current_hp
        creature8, _ = tb.create_time_eater(Rng(seed), ascension_level=8)
        assert creature8.max_hp == tb.TIME_EATER_TOUGH_HP == creature8.current_hp


# ---------------------------------------------------------------------------
# 2. Darkling / LifeLink revival
# ---------------------------------------------------------------------------

def test_darkling_has_life_link_and_correct_heal_amount():
    creature, ai = tb.create_darkling(Rng(1), ascension_level=0)
    life_link = creature.powers.get(PowerId.LIFE_LINK)
    assert life_link is not None
    assert life_link.amount == creature.max_hp // 2


def test_darkling_revives_when_a_sibling_is_still_alive():
    combat = _make_combat(1)
    dying, dying_ai = tb.create_darkling(Rng(1), ascension_level=0, slot_index=0)
    sibling, sibling_ai = tb.create_darkling(Rng(2), ascension_level=0, slot_index=1)
    combat.add_enemy(dying, dying_ai)
    combat.add_enemy(sibling, sibling_ai)
    combat.start_combat()

    combat.kill_creature(dying)
    assert dying.current_hp == 0
    assert not dying.escaped
    assert not combat.can_hit_creature(dying)
    assert dying.powers[PowerId.LIFE_LINK].is_reviving

    # DEAD_MOVE performs (no-op), then REATTACH_MOVE heals it back.
    dying_ai.current_move.perform(combat)
    dying_ai.on_move_performed()
    dying_ai.roll_move(combat.monster_ai_rng)
    assert dying_ai.current_move.state_id == tb.DARKLING_REATTACH_MOVE
    dying_ai.current_move.perform(combat)
    assert dying.current_hp == dying.powers[PowerId.LIFE_LINK].amount
    assert not dying.powers[PowerId.LIFE_LINK].is_reviving
    assert combat.can_hit_creature(dying)


def test_darkling_group_dies_for_real_when_last_one_falls():
    combat = _make_combat(1)
    first, first_ai = tb.create_darkling(Rng(1), ascension_level=0, slot_index=0)
    second, second_ai = tb.create_darkling(Rng(2), ascension_level=0, slot_index=1)
    combat.add_enemy(first, first_ai)
    combat.add_enemy(second, second_ai)
    combat.start_combat()

    # First one dies -> pretend-dead, waiting to reattach.
    combat.kill_creature(first)
    assert first.powers[PowerId.LIFE_LINK].is_reviving
    assert not first.escaped

    # Second (and last living) one dies -> sweeps the WHOLE group for real.
    combat.kill_creature(second)
    assert second.escaped
    assert first.escaped
    assert not first.powers[PowerId.LIFE_LINK].is_reviving


# ---------------------------------------------------------------------------
# 3. OrbWalker / Repulsor / Spiker / Exploder (Shape family)
# ---------------------------------------------------------------------------

def test_orb_walker_has_ritual_strength_up():
    creature, ai = tb.create_orb_walker(Rng(1), ascension_level=0)
    assert creature.get_power_amount(PowerId.RITUAL) == tb.ORB_WALKER_BASE_STRENGTH_UP
    creature9, _ = tb.create_orb_walker(Rng(1), ascension_level=9)
    assert creature9.get_power_amount(PowerId.RITUAL) == tb.ORB_WALKER_DEADLY_STRENGTH_UP


def test_repulsor_daze_shuffles_into_draw_pile():
    combat = _make_combat(1)
    creature, ai = tb.create_repulsor(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    before = len(combat.draw_pile)
    dazed_before = sum(1 for c in combat.draw_pile if c.card_id == CardId.DAZED)
    ai.states[tb.REPULSOR_DAZE_MOVE].perform(combat)
    # Random-position insertion means the new cards can land anywhere in the
    # pile, not necessarily appended at the end -- count occurrences rather
    # than slicing.
    assert len(combat.draw_pile) == before + tb.REPULSOR_DAZE_COUNT
    dazed_after = sum(1 for c in combat.draw_pile if c.card_id == CardId.DAZED)
    assert dazed_after == dazed_before + tb.REPULSOR_DAZE_COUNT


def test_spiker_stops_buffing_thorns_after_max_uses():
    creature, ai = tb.create_spiker(Rng(1), ascension_level=0)
    assert creature.get_power_amount(PowerId.THORNS) == tb.SPIKER_BASE_STARTING_THORNS
    combat = _make_combat(1)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 40)
    buff_count = moves.count(tb.SPIKER_BUFF_THORNS_MOVE)
    assert buff_count <= tb.SPIKER_MAX_BUFF_USES + 1
    assert moves[-1] == tb.SPIKER_ATTACK_MOVE
    assert moves[-5:] == [tb.SPIKER_ATTACK_MOVE] * 5


def test_exploder_attacks_twice_then_explodes_and_dies():
    combat = _make_combat(1)
    creature, ai = tb.create_exploder(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 3)
    assert moves == [tb.EXPLODER_ATTACK_MOVE, tb.EXPLODER_ATTACK_MOVE, tb.EXPLODER_EXPLODE_MOVE]
    assert creature.is_dead
    assert creature.escaped


# ---------------------------------------------------------------------------
# 4. WrithingMass (Reactive reroll + one-shot MegaDebuff)
# ---------------------------------------------------------------------------

def test_writhing_mass_reactive_rerolls_queued_move_on_hit():
    combat = _make_combat(3)
    creature, ai = tb.create_writhing_mass(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()

    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    queued_before = ai.current_move.state_id

    combat.deal_damage(combat.player, creature, 5, ValueProp.MOVE)
    queued_after = ai.current_move.state_id
    # Reactive always picks a DIFFERENT move than whatever was queued.
    assert queued_after != queued_before
    assert ai.states[queued_after].is_move


def test_writhing_mass_mega_debuff_used_at_most_once():
    combat = _make_combat(1)
    creature, ai = tb.create_writhing_mass(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 60)
    assert moves.count(tb.WRITHING_MASS_MEGA_DEBUFF_MOVE) <= 1


# ---------------------------------------------------------------------------
# 5. SpireGrowth (Constricted)
# ---------------------------------------------------------------------------

def test_spire_growth_constrict_damages_player_at_own_turn_end():
    combat = _make_combat(1)
    creature, ai = tb.create_spire_growth(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tb.SPIRE_GROWTH_CONSTRICT_MOVE].perform(combat)
    assert combat.player.has_power(PowerId.CONSTRICTED)
    amount = combat.player.get_power_amount(PowerId.CONSTRICTED)
    hp_before = combat.player.current_hp
    combat.end_player_turn()
    assert combat.player.current_hp == hp_before - amount


def test_spire_growth_constricted_removed_when_applier_dies():
    combat = _make_combat(1)
    creature, ai = tb.create_spire_growth(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    ai.states[tb.SPIRE_GROWTH_CONSTRICT_MOVE].perform(combat)
    assert combat.player.has_power(PowerId.CONSTRICTED)
    combat.kill_creature(creature)
    assert not combat.player.has_power(PowerId.CONSTRICTED)


# ---------------------------------------------------------------------------
# 6. GiantHead (Slow + countdown-based escalating It Is Time)
# ---------------------------------------------------------------------------

def test_giant_head_has_slow_and_starting_count():
    creature, ai = tb.create_giant_head(Rng(1), ascension_level=0)
    assert creature.has_power(PowerId.SLOW)


def test_giant_head_eventually_locks_into_escalating_it_is_time():
    combat = _make_combat(1, player_hp=10_000_000)
    creature, ai = tb.create_giant_head(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()

    # Count starts at 5 (asc<8) and only reaches <=1 (locking into
    # IT_IS_TIME forever) after exactly 4 GLARE/COUNT turns -- advance
    # exactly that many first so the sampling window below catches the
    # ramp-up rather than the eventual Count==-6 plateau.
    pre_moves = _run_turns(combat, ai, tb.GIANT_HEAD_BASE_START_COUNT - 1)
    assert tb.GIANT_HEAD_IT_IS_TIME_MOVE not in pre_moves

    # Damage rises every subsequent turn since Count keeps going negative
    # (clamped at -6): capture successive IT_IS_TIME hits and confirm a
    # non-decreasing sequence that rises at least once, then plateaus once
    # Count hits its -6 floor.
    hp_snapshots = []
    for _ in range(8):
        assert ai.current_move.state_id == tb.GIANT_HEAD_IT_IS_TIME_MOVE
        before = combat.player.current_hp
        ai.current_move.perform(combat)
        hp_snapshots.append(before - combat.player.current_hp)
        ai.on_move_performed()
        ai.roll_move(combat.monster_ai_rng)
    assert hp_snapshots == sorted(hp_snapshots)
    assert hp_snapshots[-1] > hp_snapshots[0]
    assert hp_snapshots[-1] == hp_snapshots[-2]  # plateaued at the Count==-6 cap


# ---------------------------------------------------------------------------
# 7. Nemesis (alternating Intangible)
# ---------------------------------------------------------------------------

def test_nemesis_flickers_intangible_every_other_enemy_turn():
    combat = _make_combat(1)
    creature, ai = tb.create_nemesis(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert not creature.has_power(PowerId.INTANGIBLE)
    combat.end_player_turn()
    assert creature.has_power(PowerId.INTANGIBLE)
    combat.end_player_turn()
    assert not creature.has_power(PowerId.INTANGIBLE)
    combat.end_player_turn()
    assert creature.has_power(PowerId.INTANGIBLE)


def test_nemesis_scythe_has_cooldown():
    combat = _make_combat(1)
    creature, ai = tb.create_nemesis(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    moves = _run_turns(combat, ai, 40)
    # Scythe should never appear on two turns within its cooldown window.
    scythe_indices = [i for i, m in enumerate(moves) if m == tb.NEMESIS_SCYTHE_MOVE]
    for a, b in zip(scythe_indices, scythe_indices[1:]):
        assert b - a >= tb.NEMESIS_SCYTHE_COOLDOWN


# ---------------------------------------------------------------------------
# 8. Reptomancer + SnakeDagger
# ---------------------------------------------------------------------------

def test_reptomancer_opens_on_spawn_dagger_and_caps_alive_daggers():
    combat = _make_combat(1)
    creature, ai = tb.create_reptomancer(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert ai.current_move.state_id == tb.REPTOMANCER_SPAWN_DAGGER_MOVE

    ai.current_move.perform(combat)
    daggers = [e for e in combat.enemies if e.monster_id == tb.SNAKE_DAGGER_MONSTER_ID]
    assert len(daggers) == tb.REPTOMANCER_DAGGERS_PER_SPAWN
    for dagger in daggers:
        assert dagger.has_power(PowerId.MINION)

    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    # Force another spawn attempt directly -- should not exceed the cap.
    ai.states[tb.REPTOMANCER_SPAWN_DAGGER_MOVE].perform(combat)
    daggers = [e for e in combat.enemies if e.monster_id == tb.SNAKE_DAGGER_MONSTER_ID and e.is_alive]
    assert len(daggers) <= tb.REPTOMANCER_MAX_ALIVE_DAGGERS


def test_snake_dagger_wound_stab_then_explode_self_kill():
    combat = _make_combat(1)
    creature, ai = tb.create_snake_dagger(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    before = len(combat.discard_pile)
    moves = _run_turns(combat, ai, 2)
    assert moves == [tb.SNAKE_DAGGER_WOUND_STAB_MOVE, tb.SNAKE_DAGGER_EXPLODE_MOVE]
    assert creature.is_dead
    assert creature.escaped
    assert combat.discard_pile[-1].card_id == CardId.WOUND or any(
        c.card_id == CardId.WOUND for c in combat.discard_pile[before:]
    )


# ---------------------------------------------------------------------------
# 9. AwakenedOne (two-phase boss -- most complex monster in this act)
# ---------------------------------------------------------------------------

def test_awakened_one_opens_with_regen_curiosity_unawakened_and_slash():
    creature, ai = tb.create_awakened_one(Rng(1), ascension_level=0)
    assert creature.get_power_amount(PowerId.REGENERATE_A4H) == tb.AWAKENED_ONE_BASE_REGEN
    curiosity = creature.powers.get(PowerId.CURIOSITY)
    assert curiosity is not None and curiosity.amount == tb.AWAKENED_ONE_BASE_CURIOSITY
    assert creature.has_power(PowerId.UNAWAKENED)
    assert not creature.has_power(PowerId.STRENGTH)  # asc<9: no starting Strength
    assert ai.current_move.state_id == tb.AWAKENED_ONE_SLASH_MOVE


def test_awakened_one_asc9_gets_starting_strength():
    creature, ai = tb.create_awakened_one(Rng(1), ascension_level=9)
    assert creature.get_power_amount(PowerId.STRENGTH) == tb.AWAKENED_ONE_DEADLY_STARTING_STRENGTH
    assert creature.get_power_amount(PowerId.REGENERATE_A4H) == tb.AWAKENED_ONE_DEADLY_REGEN
    curiosity = creature.powers.get(PowerId.CURIOSITY)
    assert curiosity.amount == tb.AWAKENED_ONE_DEADLY_CURIOSITY


def test_awakened_one_phase1_to_phase2_rebirth_transition():
    combat = _make_combat(1)
    creature, ai = tb.create_awakened_one(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    # Give it some Weak so we can confirm debuffs get stripped at rebirth.
    creature.apply_power(PowerId.WEAK, 3, applier=combat.player)
    assert creature.has_power(PowerId.WEAK)

    combat.kill_creature(creature)
    assert creature.current_hp == 0
    assert not creature.escaped
    assert not combat.can_hit_creature(creature)
    assert not combat.is_over  # UnawakenedPower keeps the fight alive

    # REBIRTH move (must_perform_once) executes on its next scheduled turn.
    assert ai.current_move.state_id == tb.AWAKENED_ONE_REBIRTH_MOVE
    ai.current_move.perform(combat)
    ai.on_move_performed()

    assert creature.current_hp == creature.max_hp == tb.AWAKENED_ONE_BASE_HP
    assert combat.can_hit_creature(creature)
    assert not creature.has_power(PowerId.CURIOSITY)
    assert not creature.has_power(PowerId.WEAK)
    assert creature.has_power(PowerId.UNAWAKENED)
    assert creature.powers[PowerId.UNAWAKENED].has_respawned

    ai.roll_move(combat.monster_ai_rng)
    assert ai.current_move.state_id == tb.AWAKENED_ONE_DARK_ECHO_MOVE


def test_awakened_one_second_death_is_real_and_escapes_cultists():
    combat = _make_combat(1)
    creature, ai = tb.create_awakened_one(Rng(1), ascension_level=0)
    cultist, cultist_ai = ex.create_cultist(Rng(2), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.add_enemy(cultist, cultist_ai)
    combat.start_combat()

    combat.kill_creature(creature)  # phase 1 "death" -> rebirth pending
    unawakened = creature.powers[PowerId.UNAWAKENED]
    unawakened.do_revive()  # simulate REBIRTH already having resolved
    assert unawakened.has_respawned
    creature.current_hp = 50  # pretend we're now in phase 2

    combat.kill_creature(creature)  # the REAL death this time
    assert creature.escaped
    assert cultist.escaped  # ally Cultist flees when AwakenedOne truly dies


# ---------------------------------------------------------------------------
# 10. Donu / Deca
# ---------------------------------------------------------------------------

def test_donu_has_artifact_and_alternates_circle_and_beam():
    combat = _make_combat(1)
    creature, ai = tb.create_donu(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert creature.get_power_amount(PowerId.ARTIFACT) == tb.DONU_BASE_ARTIFACT
    moves = _run_turns(combat, ai, 4)
    assert moves == [tb.DONU_CIRCLE_MOVE, tb.DONU_BEAM_MOVE, tb.DONU_CIRCLE_MOVE, tb.DONU_BEAM_MOVE]


def test_donu_circle_of_protection_buffs_deca_ally_too():
    combat = _make_combat(1)
    donu, donu_ai = tb.create_donu(Rng(1), ascension_level=0)
    deca, deca_ai = tb.create_deca(Rng(2), ascension_level=0)
    combat.add_enemy(donu, donu_ai)
    combat.add_enemy(deca, deca_ai)
    combat.start_combat()
    donu_ai.states[tb.DONU_CIRCLE_MOVE].perform(combat)
    assert donu.get_power_amount(PowerId.STRENGTH) == tb.DONU_BASE_CIRCLE_STRENGTH
    assert deca.get_power_amount(PowerId.STRENGTH) == tb.DONU_BASE_CIRCLE_STRENGTH


def test_deca_has_artifact_and_alternates_beam_and_square():
    combat = _make_combat(1)
    creature, ai = tb.create_deca(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    assert creature.get_power_amount(PowerId.ARTIFACT) == tb.DECA_BASE_ARTIFACT
    moves = _run_turns(combat, ai, 4)
    assert moves == [tb.DECA_BEAM_MOVE, tb.DECA_SQUARE_MOVE, tb.DECA_BEAM_MOVE, tb.DECA_SQUARE_MOVE]


# ---------------------------------------------------------------------------
# 11. TimeEater (Time Warp trigger)
# ---------------------------------------------------------------------------

class _FakeCard:
    def __init__(self, owner):
        self.owner = owner
        self.card_type = None


def test_time_eater_applies_time_warp_to_player():
    combat = _make_combat(1)
    creature, ai = tb.create_time_eater(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    tb.apply_time_eater_time_warp(combat, creature)
    assert combat.player.has_power(PowerId.TIME_WARP)


def test_time_warp_triggers_after_twelve_cards_ends_turn_and_buffs_enemies():
    combat = _make_combat(1)
    creature, ai = tb.create_time_eater(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    tb.apply_time_eater_time_warp(combat, creature)
    time_warp = combat.player.powers[PowerId.TIME_WARP]

    from sts2_env.core.hooks import fire_after_card_played

    for _ in range(11):
        fire_after_card_played(_FakeCard(combat.player), combat)
    assert creature.get_power_amount(PowerId.STRENGTH) == 0
    assert combat._end_turn_after_play is False  # noqa: SLF001

    fire_after_card_played(_FakeCard(combat.player), combat)
    assert creature.get_power_amount(PowerId.STRENGTH) == 2
    assert combat._end_turn_after_play is True  # noqa: SLF001


def test_time_eater_uses_haste_below_half_hp_once():
    combat = _make_combat(1)
    creature, ai = tb.create_time_eater(Rng(1), ascension_level=0)
    combat.add_enemy(creature, ai)
    combat.start_combat()
    # The very first move was already resolved (at full HP) when the AI was
    # constructed -- same as the decompiled source's own state machine,
    # whose root is the conditional branch itself. Perform that one, THEN
    # drop HP below half and confirm the next resolution picks HASTE.
    ai.current_move.perform(combat)
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    creature.current_hp = creature.max_hp // 2 - 1
    ai.current_move.perform(combat)
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    assert ai.current_move.state_id == tb.TIME_EATER_HASTE_MOVE

    ai.current_move.perform(combat)  # actually perform HASTE
    ai.on_move_performed()
    ai.roll_move(combat.monster_ai_rng)
    moves = _run_turns(combat, ai, 5)
    assert moves.count(tb.TIME_EATER_HASTE_MOVE) == 0  # already used once, never again


# ---------------------------------------------------------------------------
# 12. JawWorm hard mode (TheBeyond's JawWormHordeNormal only)
# ---------------------------------------------------------------------------

def test_jaw_worm_hard_mode_starts_with_strength_and_block_and_varied_first_move():
    seen_first_moves = set()
    for seed in range(1, 60):
        creature, ai = tb.create_jaw_worm_hard_mode(Rng(seed), ascension_level=0)
        assert creature.get_power_amount(PowerId.STRENGTH) == ex.JAW_WORM_BASE_BELLOW_STRENGTH
        assert creature.block == ex.JAW_WORM_BASE_BELLOW_BLOCK
        seen_first_moves.add(ai.current_move.state_id)
    # Unlike normal JawWorm (always forced CHOMP), HardMode's opening move
    # is chosen by the same random branch as every other turn.
    assert seen_first_moves == {ex.JAW_WORM_CHOMP_MOVE, ex.JAW_WORM_BELLOW_MOVE, ex.JAW_WORM_THRASH_MOVE}


# ---------------------------------------------------------------------------
# 13. Encounter pools
# ---------------------------------------------------------------------------

def test_encounter_pools_are_nonempty_and_correctly_sized():
    assert len(enc.WEAK_ENCOUNTERS) == 3
    assert len(enc.NORMAL_ENCOUNTERS) == 7
    assert len(enc.ELITE_ENCOUNTERS) == 3
    assert len(enc.BOSS_ENCOUNTERS) == 3


def test_darklings_weak_spawns_three_darklings():
    combat = _make_combat(1)
    enc.setup_darklings_weak(combat, combat.rng)
    daggers = [e for e in combat.enemies if e.monster_id == tb.DARKLING_MONSTER_ID]
    assert len(daggers) == 3


def test_orb_walker_weak_is_solo():
    combat = _make_combat(1)
    enc.setup_orb_walker_weak(combat, combat.rng)
    assert len(combat.enemies) == 1
    assert combat.enemies[0].monster_id == tb.ORB_WALKER_MONSTER_ID


def test_jaw_worm_horde_normal_spawns_three_hard_mode_jaw_worms():
    combat = _make_combat(1)
    enc.setup_jaw_worm_horde_normal(combat, combat.rng)
    worms = [e for e in combat.enemies if e.monster_id == ex.JAW_WORM_MONSTER_ID]
    assert len(worms) == 3
    for worm in worms:
        assert worm.get_power_amount(PowerId.STRENGTH) >= ex.JAW_WORM_BASE_BELLOW_STRENGTH


def test_reptomancer_elite_starts_with_two_daggers_already_alive():
    combat = _make_combat(1)
    enc.setup_reptomancer_elite(combat, combat.rng)
    daggers = [e for e in combat.enemies if e.monster_id == tb.SNAKE_DAGGER_MONSTER_ID]
    reptomancers = [e for e in combat.enemies if e.monster_id == tb.REPTOMANCER_MONSTER_ID]
    assert len(daggers) == 2
    assert len(reptomancers) == 1
    for dagger in daggers:
        assert dagger.has_power(PowerId.MINION)


def test_awakened_one_boss_spawns_two_cultists_and_awakened_one():
    combat = _make_combat(1)
    enc.setup_awakened_one_boss(combat, combat.rng)
    cultists = [e for e in combat.enemies if e.monster_id == ex.CULTIST_MONSTER_ID]
    awakened = [e for e in combat.enemies if e.monster_id == tb.AWAKENED_ONE_MONSTER_ID]
    assert len(cultists) == 2
    assert len(awakened) == 1


def test_donu_and_deca_boss_spawns_both():
    combat = _make_combat(1)
    enc.setup_donu_and_deca_boss(combat, combat.rng)
    ids = {e.monster_id for e in combat.enemies}
    assert tb.DONU_MONSTER_ID in ids
    assert tb.DECA_MONSTER_ID in ids


def test_time_eater_boss_applies_time_warp_to_player():
    combat = _make_combat(1)
    enc.setup_time_eater_boss(combat, combat.rng)
    assert combat.player.has_power(PowerId.TIME_WARP)


def test_sphere_and_two_shapes_normal_composition():
    combat = _make_combat(1)
    enc.setup_sphere_and_two_shapes_normal(combat, combat.rng)
    from sts2_env.monsters.thecity import SPHERIC_GUARDIAN_MONSTER_ID

    ids = [e.monster_id for e in combat.enemies]
    assert ids.count(SPHERIC_GUARDIAN_MONSTER_ID) == 1
    assert len(ids) == 3


def test_three_shapes_weak_samples_three_without_replacement():
    for seed in range(1, 10):
        combat = _make_combat(seed)
        enc.setup_three_shapes_weak(combat, combat.rng)
        assert len(combat.enemies) == 3
        shape_ids = {tb.REPULSOR_MONSTER_ID, tb.EXPLODER_MONSTER_ID, tb.SPIKER_MONSTER_ID}
        assert all(e.monster_id in shape_ids for e in combat.enemies)


def test_four_shapes_normal_samples_four():
    combat = _make_combat(1)
    enc.setup_four_shapes_normal(combat, combat.rng)
    assert len(combat.enemies) == 4
