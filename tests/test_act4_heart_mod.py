"""Tests for the Act4Heart mod (Corrupt Heart, Spire Shield/Spear, keys).

Follows the conventions of test_monster_ai_state_machine_parity.py: build a
CombatState directly (no start_combat()), drive MoveState.perform(combat)
directly or via a small _drive_ai helper, and assert HP/power/state-id
transitions.
"""

from __future__ import annotations

# Ensure power registration happens (mirrors tests/conftest.py convention).
import sts2_env.powers  # noqa: F401

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CombatSide, MapPointType, PowerId, ValueProp
from sts2_env.core.rng import Rng
from sts2_env.map.acts import ALL_ACTS
from sts2_env.map.generator import generate_act4_heart_map
from sts2_env.monsters.act4_heart import (
    CORRUPT_HEART_BASE_BEAT_OF_DEATH,
    CORRUPT_HEART_BASE_HP,
    CORRUPT_HEART_BASE_INVINCIBLE,
    CORRUPT_HEART_BLOOD_SHOTS_MOVE,
    CORRUPT_HEART_BUFF_MOVE,
    CORRUPT_HEART_DEADLY_BEAT_OF_DEATH,
    CORRUPT_HEART_DEADLY_INVINCIBLE,
    CORRUPT_HEART_DEBILITATE_MOVE,
    CORRUPT_HEART_ECHO_MOVE,
    CORRUPT_HEART_TOUGH_HP,
    SPIRE_SHIELD_BASE_HP,
    SPIRE_SHIELD_BASH_MOVE,
    SPIRE_SHIELD_FORTIFY_MOVE,
    SPIRE_SHIELD_SMASH_MOVE,
    SPIRE_SHIELD_TOUGH_HP,
    SPIRE_SPEAR_BASE_HP,
    SPIRE_SPEAR_BURN_STRIKE_MOVE,
    SPIRE_SPEAR_PIERCER_MOVE,
    SPIRE_SPEAR_SKEWER_MOVE,
    SPIRE_SPEAR_TOUGH_HP,
    create_corrupt_heart,
    create_spire_shield,
    create_spire_spear,
)
from sts2_env.powers.act4_heart import BeatOfDeathPower


def _make_combat(seed: int = 1, ascension_level: int = 0, player_hp: int = 9999) -> CombatState:
    return CombatState(
        player_hp=player_hp,
        player_max_hp=player_hp,
        deck=create_ironclad_starter_deck(),
        rng_seed=seed,
        character_id="Ironclad",
        ascension_level=ascension_level,
    )


def _drive_ai(ai, combat: CombatState, rng: Rng, n: int) -> list[str]:
    """Perform n moves (state transitions + effects) and return state ids."""
    moves: list[str] = []
    for _ in range(n):
        move = ai.current_move
        moves.append(move.state_id)
        move.perform(combat)
        ai.on_move_performed()
        if combat.is_over:
            break
        ai.roll_move(rng)
    return moves


# ---------------------------------------------------------------------------
# Corrupt Heart
# ---------------------------------------------------------------------------

class TestCorruptHeart:
    def test_hp_ascension_scaling(self):
        creature, _ = create_corrupt_heart(Rng(1), ascension_level=0)
        assert creature.max_hp == CORRUPT_HEART_BASE_HP == 750
        creature8, _ = create_corrupt_heart(Rng(1), ascension_level=8)
        assert creature8.max_hp == CORRUPT_HEART_TOUGH_HP == 800

    def test_spawn_powers_beat_of_death_and_invincible(self):
        creature, _ = create_corrupt_heart(Rng(1), ascension_level=0)
        assert creature.get_power_amount(PowerId.BEAT_OF_DEATH) == CORRUPT_HEART_BASE_BEAT_OF_DEATH == 1
        assert creature.get_power_amount(PowerId.INVINCIBLE) == CORRUPT_HEART_BASE_INVINCIBLE == 300

    def test_spawn_powers_deadly_ascension_scaling(self):
        creature, _ = create_corrupt_heart(Rng(1), ascension_level=9)
        assert creature.get_power_amount(PowerId.BEAT_OF_DEATH) == CORRUPT_HEART_DEADLY_BEAT_OF_DEATH == 2
        # Invincible is LOWER at Ascension 9+ per the decompiled source
        # (GetValueIfAscension(9, 200, 300)).
        assert creature.get_power_amount(PowerId.INVINCIBLE) == CORRUPT_HEART_DEADLY_INVINCIBLE == 200

    def test_move_sequence_debilitate_then_alternating_then_buff(self):
        combat = _make_combat(seed=7)
        creature, ai = create_corrupt_heart(Rng(7), ascension_level=0)
        combat.add_enemy(creature, ai)

        moves = _drive_ai(ai, combat, Rng(123), 7)

        assert moves[0] == CORRUPT_HEART_DEBILITATE_MOVE
        # Both alternating attacks fire exactly once (order random) before Buff.
        assert set(moves[1:3]) == {CORRUPT_HEART_BLOOD_SHOTS_MOVE, CORRUPT_HEART_ECHO_MOVE}
        assert moves[1] != moves[2]
        assert moves[3] == CORRUPT_HEART_BUFF_MOVE
        # Cycle repeats: attack, attack, buff, ...
        assert set(moves[4:6]) == {CORRUPT_HEART_BLOOD_SHOTS_MOVE, CORRUPT_HEART_ECHO_MOVE}
        assert moves[6] == CORRUPT_HEART_BUFF_MOVE

    def test_debilitate_applies_debuffs_and_five_status_cards(self):
        combat = _make_combat(seed=3)
        creature, ai = create_corrupt_heart(Rng(3), ascension_level=0)
        combat.add_enemy(creature, ai)
        player = combat.primary_player

        ai.current_move.perform(combat)

        assert player.get_power_amount(PowerId.VULNERABLE) == 2
        assert player.get_power_amount(PowerId.WEAK) == 2
        assert player.get_power_amount(PowerId.FRAIL) == 2
        state = combat.combat_player_state_for(player)
        assert len(state.discard) == 5
        card_ids = sorted(card.card_id.name for card in state.discard)
        assert card_ids == ["BURN", "DAZED", "SLIMED", "VOID", "WOUND"]

    def test_blood_shots_and_echo_damage(self):
        combat = _make_combat(seed=5)
        creature, ai = create_corrupt_heart(Rng(5), ascension_level=0)
        combat.add_enemy(creature, ai)
        player = combat.primary_player

        blood_shots = ai.states[CORRUPT_HEART_BLOOD_SHOTS_MOVE]
        hp_before = player.current_hp
        blood_shots.perform(combat)
        assert hp_before - player.current_hp == 15 * 2

        echo = ai.states[CORRUPT_HEART_ECHO_MOVE]
        hp_before = player.current_hp
        echo.perform(combat)
        assert hp_before - player.current_hp == 45

    def test_buff_cycle_effects_by_counter(self):
        combat = _make_combat(seed=9)
        creature, ai = create_corrupt_heart(Rng(9), ascension_level=0)
        combat.add_enemy(creature, ai)
        buff = ai.states[CORRUPT_HEART_BUFF_MOVE]

        buff.perform(combat)
        assert creature.get_power_amount(PowerId.STRENGTH) == 2
        assert creature.get_power_amount(PowerId.ARTIFACT) == 2

        buff.perform(combat)
        assert creature.get_power_amount(PowerId.STRENGTH) == 4
        assert creature.get_power_amount(PowerId.BEAT_OF_DEATH) == CORRUPT_HEART_BASE_BEAT_OF_DEATH + 1

        buff.perform(combat)
        assert creature.get_power_amount(PowerId.STRENGTH) == 6
        assert creature.get_power_amount(PowerId.PAINFUL_STABS) == 1

        buff.perform(combat)
        assert creature.get_power_amount(PowerId.STRENGTH) == 6 + 2 + 10

        strength_before = creature.get_power_amount(PowerId.STRENGTH)
        buff.perform(combat)
        assert creature.get_power_amount(PowerId.STRENGTH) == strength_before + 2 + 50

    def test_buff_strips_negative_strength_before_adding(self):
        combat = _make_combat(seed=11)
        creature, ai = create_corrupt_heart(Rng(11), ascension_level=0)
        combat.add_enemy(creature, ai)
        combat.apply_power_to(creature, PowerId.STRENGTH, -5, applier=creature)
        assert creature.get_power_amount(PowerId.STRENGTH) == -5

        buff = ai.states[CORRUPT_HEART_BUFF_MOVE]
        buff.perform(combat)

        # Negative strength is fully stripped, then +2 applied -- not -5+2=-3.
        assert creature.get_power_amount(PowerId.STRENGTH) == 2


# ---------------------------------------------------------------------------
# Beat of Death power
# ---------------------------------------------------------------------------

class TestBeatOfDeathPower:
    def test_damages_the_card_player_not_the_power_owner(self):
        combat = _make_combat(seed=13)
        heart, heart_ai = create_corrupt_heart(Rng(13), ascension_level=0)
        combat.add_enemy(heart, heart_ai)
        combat.start_combat()
        player = combat.primary_player
        # Heart already has Beat of Death from spawn (amount 1); bump it up
        # for a clearer assertion.
        combat.apply_power_to(heart, PowerId.BEAT_OF_DEATH, 4, applier=heart)
        total_amount = heart.get_power_amount(PowerId.BEAT_OF_DEATH)

        heart_hp_before = heart.current_hp
        player_hp_before = player.current_hp

        # Use Strike (deals damage, grants no block of its own) so the
        # Beat of Death damage isn't incidentally absorbed by fresh block.
        state = combat.combat_player_state_for(player)
        hand_index = next(
            i for i, card in enumerate(state.hand) if card.card_id.name == "STRIKE_IRONCLAD"
        )
        assert combat.play_card_from_creature(player, hand_index, target_index=0)

        # The Strike itself damages the Heart (it was the played card's
        # target); Beat of Death damage lands on the PLAYER (who played the
        # card) on top of that -- it does not further damage the Heart.
        strike_damage = heart_hp_before - heart.current_hp
        assert strike_damage > 0
        assert player_hp_before - player.current_hp == total_amount

    def test_beat_of_death_ignores_strength_but_is_blockable(self):
        combat = _make_combat(seed=17)
        power = BeatOfDeathPower(5)
        # Attach directly to a creature to test the hook in isolation.
        dummy_owner = combat.primary_player
        dummy_owner.powers[PowerId.BEAT_OF_DEATH] = power

        target = combat.primary_player
        target.block = 3
        hp_before = target.current_hp

        class _FakeCard:
            owner = target

        power.after_card_played(dummy_owner, _FakeCard(), combat)

        # 3 blocked, 2 unblocked -> 2 HP lost.
        assert hp_before - target.current_hp == 2


# ---------------------------------------------------------------------------
# Invincible power
# ---------------------------------------------------------------------------

class TestInvinciblePower:
    def test_caps_hp_loss_per_turn_and_resets_on_turn_start(self):
        combat = _make_combat(seed=19, player_hp=999)
        creature, _ = create_corrupt_heart(Rng(19), ascension_level=0)
        combat.add_enemy(creature, create_corrupt_heart(Rng(19))[1])
        combat.apply_power_to(creature, PowerId.INVINCIBLE, 0, applier=creature)  # no-op, ensure exists
        # Reset to a small, easy-to-test cap.
        power = creature.powers[PowerId.INVINCIBLE]
        power.amount = 10
        power.damage_taken_this_cycle = 0

        from sts2_env.core.damage import apply_damage

        apply_damage(creature, 7, ValueProp.MOVE, combat, combat.primary_player)
        assert creature.max_hp - creature.current_hp == 7

        # A second hit of 7 should be capped: only 3 more HP lost (10 total).
        apply_damage(creature, 7, ValueProp.MOVE, combat, combat.primary_player)
        assert creature.max_hp - creature.current_hp == 10

        # Reset at the start of the owner's (enemy) side turn.
        from sts2_env.core.hooks import fire_before_side_turn_start

        fire_before_side_turn_start(CombatSide.ENEMY, combat)
        assert power.damage_taken_this_cycle == 0

        apply_damage(creature, 7, ValueProp.MOVE, combat, combat.primary_player)
        assert creature.max_hp - creature.current_hp == 17

    def test_reset_only_fires_for_owners_own_side(self):
        combat = _make_combat(seed=23)
        creature, _ = create_corrupt_heart(Rng(23), ascension_level=0)
        combat.add_enemy(creature, create_corrupt_heart(Rng(23))[1])
        power = creature.powers[PowerId.INVINCIBLE]
        power.damage_taken_this_cycle = 42

        from sts2_env.core.hooks import fire_before_side_turn_start

        fire_before_side_turn_start(CombatSide.PLAYER, combat)
        assert power.damage_taken_this_cycle == 42

        fire_before_side_turn_start(CombatSide.ENEMY, combat)
        assert power.damage_taken_this_cycle == 0


# ---------------------------------------------------------------------------
# Spire Shield
# ---------------------------------------------------------------------------

class TestSpireShield:
    def test_hp_and_artifact_ascension_scaling(self):
        creature, _ = create_spire_shield(Rng(1), ascension_level=0)
        assert creature.max_hp == SPIRE_SHIELD_BASE_HP == 110
        assert creature.get_power_amount(PowerId.ARTIFACT) == 1
        assert creature.get_power_amount(PowerId.BACK_ATTACK_LEFT) == 1

        creature8, _ = create_spire_shield(Rng(1), ascension_level=8)
        assert creature8.max_hp == SPIRE_SHIELD_TOUGH_HP == 125

        creature9, _ = create_spire_shield(Rng(1), ascension_level=9)
        assert creature9.get_power_amount(PowerId.ARTIFACT) == 2

    def test_move_sequence_bash_and_fortify_once_each_then_smash(self):
        combat = _make_combat(seed=29)
        creature, ai = create_spire_shield(Rng(29), ascension_level=0)
        combat.add_enemy(creature, ai)

        moves = _drive_ai(ai, combat, Rng(41), 6)

        assert set(moves[0:2]) == {SPIRE_SHIELD_BASH_MOVE, SPIRE_SHIELD_FORTIFY_MOVE}
        assert moves[0] != moves[1]
        assert moves[2] == SPIRE_SHIELD_SMASH_MOVE
        assert set(moves[3:5]) == {SPIRE_SHIELD_BASH_MOVE, SPIRE_SHIELD_FORTIFY_MOVE}
        assert moves[5] == SPIRE_SHIELD_SMASH_MOVE

    def test_smash_gains_block_equal_to_damage_dealt(self):
        combat = _make_combat(seed=31)
        creature, ai = create_spire_shield(Rng(31), ascension_level=0)
        combat.add_enemy(creature, ai)
        smash = ai.states[SPIRE_SHIELD_SMASH_MOVE]

        smash.perform(combat)
        assert creature.block == 38

    def test_smash_grants_flat_99_block_at_ascension_9(self):
        combat = _make_combat(seed=33, ascension_level=9)
        creature, ai = create_spire_shield(Rng(33), ascension_level=9)
        combat.add_enemy(creature, ai)
        smash = ai.states[SPIRE_SHIELD_SMASH_MOVE]

        smash.perform(combat)
        assert creature.block == 99

    def test_fortify_grants_block_to_self_and_allies(self):
        combat = _make_combat(seed=37)
        shield, shield_ai = create_spire_shield(Rng(37), ascension_level=0)
        spear, spear_ai = create_spire_spear(Rng(37), ascension_level=0)
        combat.add_enemy(shield, shield_ai)
        combat.add_enemy(spear, spear_ai)

        fortify = shield_ai.states[SPIRE_SHIELD_FORTIFY_MOVE]
        fortify.perform(combat)

        assert shield.block == 30
        assert spear.block == 30

    def test_bash_applies_minus_one_strength_to_target_without_orbs(self):
        combat = _make_combat(seed=39)
        creature, ai = create_spire_shield(Rng(39), ascension_level=0)
        combat.add_enemy(creature, ai)
        player = combat.primary_player

        bash = ai.states[SPIRE_SHIELD_BASH_MOVE]
        hp_before = player.current_hp
        bash.perform(combat)

        assert hp_before - player.current_hp == 14
        assert player.get_power_amount(PowerId.STRENGTH) == -1


# ---------------------------------------------------------------------------
# Spire Spear
# ---------------------------------------------------------------------------

class TestSpireSpear:
    def test_hp_and_artifact_ascension_scaling(self):
        creature, _ = create_spire_spear(Rng(1), ascension_level=0)
        assert creature.max_hp == SPIRE_SPEAR_BASE_HP == 160
        assert creature.get_power_amount(PowerId.ARTIFACT) == 1
        assert creature.get_power_amount(PowerId.BACK_ATTACK_RIGHT) == 1

        creature8, _ = create_spire_spear(Rng(1), ascension_level=8)
        assert creature8.max_hp == SPIRE_SPEAR_TOUGH_HP == 180

        creature9, _ = create_spire_spear(Rng(1), ascension_level=9)
        assert creature9.get_power_amount(PowerId.ARTIFACT) == 2

    def test_move_sequence_burn_strike_first_then_skewer_then_random_pair(self):
        combat = _make_combat(seed=43)
        creature, ai = create_spire_spear(Rng(43), ascension_level=0)
        combat.add_enemy(creature, ai)

        moves = _drive_ai(ai, combat, Rng(53), 7)

        assert moves[0] == SPIRE_SPEAR_BURN_STRIKE_MOVE  # forced opening move
        assert moves[1] == SPIRE_SPEAR_SKEWER_MOVE
        assert set(moves[2:4]) == {SPIRE_SPEAR_BURN_STRIKE_MOVE, SPIRE_SPEAR_PIERCER_MOVE}
        assert moves[2] != moves[3]
        assert moves[4] == SPIRE_SPEAR_SKEWER_MOVE
        assert set(moves[5:7]) == {SPIRE_SPEAR_BURN_STRIKE_MOVE, SPIRE_SPEAR_PIERCER_MOVE}

    def test_burn_strike_inserts_burns_into_discard_by_default(self):
        combat = _make_combat(seed=59)
        creature, ai = create_spire_spear(Rng(59), ascension_level=0)
        combat.add_enemy(creature, ai)
        player = combat.primary_player
        state = combat.combat_player_state_for(player)

        burn_strike = ai.states[SPIRE_SPEAR_BURN_STRIKE_MOVE]
        hp_before = player.current_hp
        burn_strike.perform(combat)

        assert hp_before - player.current_hp == 2 * 6
        assert len(state.discard) == 2
        assert all(card.card_id.name == "BURN" for card in state.discard)

    def test_burn_strike_inserts_burns_into_draw_pile_at_ascension_9(self):
        combat = _make_combat(seed=61, ascension_level=9)
        creature, ai = create_spire_spear(Rng(61), ascension_level=9)
        combat.add_enemy(creature, ai)
        player = combat.primary_player
        state = combat.combat_player_state_for(player)
        draw_before = len(state.draw)

        burn_strike = ai.states[SPIRE_SPEAR_BURN_STRIKE_MOVE]
        burn_strike.perform(combat)

        assert len(state.discard) == 0
        burns_in_draw = [c for c in state.draw if c.card_id.name == "BURN"]
        assert len(burns_in_draw) == 2
        assert len(state.draw) == draw_before + 2

    def test_piercer_buffs_self_and_allies(self):
        combat = _make_combat(seed=67)
        shield, shield_ai = create_spire_shield(Rng(67), ascension_level=0)
        spear, spear_ai = create_spire_spear(Rng(67), ascension_level=0)
        combat.add_enemy(shield, shield_ai)
        combat.add_enemy(spear, spear_ai)

        piercer = spear_ai.states[SPIRE_SPEAR_PIERCER_MOVE]
        piercer.perform(combat)

        assert spear.get_power_amount(PowerId.STRENGTH) == 2
        assert shield.get_power_amount(PowerId.STRENGTH) == 2


# ---------------------------------------------------------------------------
# Act 4 map generation
# ---------------------------------------------------------------------------

class TestAct4HeartMap:
    def test_four_nodes_start_rest_elite_boss(self):
        act_map = generate_act4_heart_map()
        points = sorted(act_map.all_points(), key=lambda p: p.row)
        assert [p.point_type for p in points] == [
            MapPointType.UNASSIGNED,
            MapPointType.REST_SITE,
            MapPointType.ELITE,
            MapPointType.BOSS,
        ]
        assert act_map.start_point is points[0]
        assert act_map.boss_point is points[3]
        # Strictly linear.
        assert [c.coord for c in points[0].children] == [points[1].coord]
        assert [c.coord for c in points[1].children] == [points[2].coord]
        assert [c.coord for c in points[2].children] == [points[3].coord]
        # No shop/treasure/unknown/event/monster nodes.
        assert act_map.room_points()[0].point_type != MapPointType.SHOP

    def test_all_acts_includes_act4_heart_as_fourth_act(self):
        assert len(ALL_ACTS) == 4
        assert ALL_ACTS[3].act_index == 3
        assert ALL_ACTS[3].boss_ids == ["CorruptHeart"]


# ---------------------------------------------------------------------------
# Run-level integration: heal on entry, keys, final boss patch
# ---------------------------------------------------------------------------

class TestAct4HeartRunIntegration:
    def _new_manager(self, seed=100, ascension_level=0):
        from sts2_env.run.run_manager import RunManager

        return RunManager(seed=seed, character_id="Ironclad", ascension_level=ascension_level)

    def test_heal_on_act4_entry_full_heal_below_ascension_2(self):
        mgr = self._new_manager(ascension_level=0)
        rs = mgr._run_state
        rs.player.current_hp = max(1, rs.player.max_hp - 30)
        missing_before = rs.player.max_hp - rs.player.current_hp
        rs.enter_act(3)
        assert rs.player.current_hp == rs.player.max_hp
        assert missing_before == 30

    def test_heal_on_act4_entry_75_percent_at_ascension_2(self):
        mgr = self._new_manager(ascension_level=2)
        rs = mgr._run_state
        rs.player.current_hp = max(1, rs.player.max_hp - 40)
        rs.enter_act(3)
        # floor(40 * 0.75) = 30 healed -> 10 missing remains.
        assert rs.player.max_hp - rs.player.current_hp == 10

    def test_act4_map_is_generated_on_entry(self):
        mgr = self._new_manager()
        rs = mgr._run_state
        rs.enter_act(3)
        assert [p.point_type for p in sorted(rs.map.all_points(), key=lambda p: p.row)] == [
            MapPointType.UNASSIGNED,
            MapPointType.REST_SITE,
            MapPointType.ELITE,
            MapPointType.BOSS,
        ]

    def test_recall_rest_site_option_grants_ruby_key(self):
        from sts2_env.relics.base import RelicId

        mgr = self._new_manager(seed=101)
        rs = mgr._run_state
        rs.enter_act(3)
        mgr._enter_map_choice()
        mgr.take_action({"action": "move", "coord": (3, 1)})
        assert mgr.phase == "REST_SITE"
        options = mgr.get_available_actions()
        assert any(opt.get("option_id") == "RECALL" for opt in options)
        assert RelicId.RUBY_KEY.name not in rs.player.relics

        mgr.take_action({"action": "rest_option", "option_id": "RECALL"})
        assert RelicId.RUBY_KEY.name in rs.player.relics

    def test_sapphire_key_granted_on_treasure_skip(self):
        from sts2_env.relics.base import RelicId
        from sts2_env.run.reward_objects import RelicReward

        mgr = self._new_manager(seed=103)
        rs = mgr._run_state
        assert RelicId.SAPPHIRE_KEY.name not in rs.player.relics

        mgr._current_reward = RelicReward(rs.player.player_id, relic_id="ANCHOR")
        mgr._phase = mgr.PHASE_TREASURE
        actions = mgr._actions_treasure()
        assert any(a["action"] == "skip" for a in actions)

        result = mgr._do_treasure_skip()
        assert RelicId.SAPPHIRE_KEY.name in rs.player.relics
        assert "Sapphire Key" in result["description"]

    def test_treasure_skip_is_noop_without_a_skippable_reward(self):
        mgr = self._new_manager(seed=105)
        mgr._current_reward = None
        mgr._phase = mgr.PHASE_TREASURE
        result = mgr._do_treasure_skip()
        assert result["description"] == "Nothing to skip."

    def test_emerald_key_marks_one_elite_and_awards_on_kill(self):
        from sts2_env.relics.base import RelicId
        from sts2_env.run.modifiers import SuperEliteMarker

        mgr = self._new_manager(seed=107)
        rs = mgr._run_state
        elite_points = [p for p in rs.map.room_points() if p.point_type == MapPointType.ELITE]
        marked = [p for p in elite_points if any(isinstance(q, SuperEliteMarker) for q in p.quests)]
        assert len(marked) == 1

    def test_final_boss_act_index_is_act3_not_act4(self):
        mgr = self._new_manager(seed=109)
        rs = mgr._run_state
        assert rs.final_boss_act_index == 2


# ---------------------------------------------------------------------------
# Empty weak encounter (defensive instant-win)
# ---------------------------------------------------------------------------

def test_empty_fight_act4_weak_resolves_instantly():
    from sts2_env.encounters.act4_heart import setup_empty_fight_act4_weak

    combat = _make_combat(seed=111)
    setup_empty_fight_act4_weak(combat, Rng(111))
    combat.start_combat()
    assert combat.is_over
    assert combat.player_won is True
