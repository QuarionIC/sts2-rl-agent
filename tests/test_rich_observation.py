"""Tests for the rich observation encoder, reward config, and rich envs."""

from __future__ import annotations

import numpy as np
import pytest

from sts2_env.core.constants import ACTION_SPACE_SIZE
from sts2_env.gym_env import rich_observation as ro
from sts2_env.gym_env.reward_config import RewardConfig
from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv, resolve_encounter_pool
from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
from sts2_env.gym_env.run_env import TOTAL_ACTIONS


# ---------------------------------------------------------------------------
# Layout
# ---------------------------------------------------------------------------

class TestLayout:
    def test_segments_contiguous_and_cover_vector(self):
        table = ro.segment_table()
        # ID segments overlap the ID block by design; check the block first.
        assert table[0] == ("ids_hand", 0, ro.IDS_HAND_SIZE)
        offset = 0
        for name, off, size in table:
            assert off == offset, f"segment {name} not contiguous"
            offset += size
        assert offset == ro.RICH_OBS_SIZE

    def test_id_block_contiguous_at_start(self):
        assert ro.IDS_HAND_OFF == 0
        assert ro.IDS_POTION_OFF == ro.IDS_HAND_OFF + ro.IDS_HAND_SIZE
        assert ro.IDS_BOSS_OFF == ro.IDS_POTION_OFF + ro.IDS_POTION_SIZE
        assert ro.ID_BLOCK_SIZE == ro.IDS_BOSS_OFF + ro.IDS_BOSS_SIZE
        # first scalar segment starts right after the ID block
        assert ro.HAND_SCALARS_OFF == ro.ID_BLOCK_SIZE

    def test_sizes_derive_from_enums(self):
        from sts2_env.core.enums import CardId, PowerId
        from sts2_env.relics.base import RelicId

        assert ro.NUM_CARD_IDS == len(CardId)
        assert ro.NUM_POWER_IDS == len(PowerId)
        assert ro.NUM_RELIC_IDS == len(RelicId)
        assert ro.PILE_BAGS_SIZE == 3 * len(CardId)

    def test_boss_vocab_within_padded_size(self):
        assert 0 < len(ro.BOSS_NAME_TO_IDX) + 1 <= ro.BOSS_VOCAB_SIZE
        # vanilla bosses present
        assert "TheLich" in ro.BOSS_NAME_TO_IDX
        assert "CorruptHeart" in ro.BOSS_NAME_TO_IDX

    def test_potion_vocab_within_padded_size(self):
        assert 0 < ro.NUM_POTION_IDS + 1 <= ro.POTION_VOCAB_SIZE

    def test_deck_bag_segment_geometry(self):
        assert ro.DECK_BAG_OFF == ro.RUN_OFF + ro.RUN_BASE_SIZE
        assert ro.DECK_BAG_SIZE == ro.NUM_CARD_IDS
        assert ro.ARCH_SCALARS_OFF == ro.DECK_BAG_OFF + ro.DECK_BAG_SIZE
        assert ro.NUM_ARCH_SCALARS == 8
        assert ro.RICH_OBS_SIZE == ro.ARCH_SCALARS_OFF + ro.NUM_ARCH_SCALARS

    def test_archetype_sets_derived(self):
        from sts2_env.core.enums import CardId

        # Effect-registry-derived membership (spot checks vs known cards).
        assert CardId.BODYGUARD in ro.NECRO_SUMMON_CARD_IDS
        assert CardId.SEANCE in ro.NECRO_SOUL_GENERATOR_IDS
        assert CardId.SOUL_STORM in ro.NECRO_SOUL_PAYOFF_IDS
        assert CardId.SOUL not in ro.NECRO_SOUL_PAYOFF_IDS  # token, not payoff
        assert CardId.DEATHS_DOOR in ro.NECRO_DOOM_APPLIER_IDS
        assert len(ro.NECRO_SUMMON_CARD_IDS) >= 10
        assert len(ro.NECRO_DOOM_APPLIER_IDS) >= 8


# ---------------------------------------------------------------------------
# Reward config (PBRS)
# ---------------------------------------------------------------------------

class _StubPlayer:
    def __init__(self, hp: int, max_hp: int):
        self.current_hp = hp
        self.max_hp = max_hp


class _StubRunState:
    def __init__(self, act: int, act_floor: int, hp: int, max_hp: int):
        self.current_act_index = act
        self.act_floor = act_floor
        self.player = _StubPlayer(hp, max_hp)


class _StubMgr:
    """Hand-built out-of-combat state for potential() unit tests."""

    def __init__(self, act: int, act_floor: int, hp: int, max_hp: int):
        self.run_state = _StubRunState(act, act_floor, hp, max_hp)

    def get_combat_state(self):
        return None


class TestRewardConfig:
    def test_terminal_never_annealed(self):
        cfg = RewardConfig(shaping_scale=0.0)
        assert cfg.terminal_reward(True) == 1.0
        assert cfg.terminal_reward(False) == -1.0
        assert cfg.truncation == -1.0  # stalling is not safer than fighting

    def test_gamma_matches_training(self):
        assert RewardConfig().gamma_shape == pytest.approx(0.997)

    def test_potential_hand_built_states(self):
        cfg = RewardConfig()
        # Act 0, floor 0, full HP: Phi = 0.30 (effective_hp only).
        phi0 = cfg.potential(_StubMgr(0, 0, 80, 80))
        assert phi0 == pytest.approx(0.30)
        # Act 0, floor 8, full HP.
        phi8 = cfg.potential(_StubMgr(0, 8, 80, 80))
        assert phi8 == pytest.approx(0.45 * (8 / 17) / 4 + 0.30)
        # An act-1 death at floor 9 vs floor 8 differs by a visible amount.
        phi9 = cfg.potential(_StubMgr(0, 9, 80, 80))
        assert phi9 - phi8 == pytest.approx(0.45 / 17 / 4)
        assert phi9 - phi8 > 0.005
        # Act 2, floor 5, half HP, enemies half down.
        phi = cfg.potential(_StubMgr(2, 5, 40, 80), enemy_down=0.5)
        expected = 0.45 * (2 + 5 / 17) / 4 + 0.30 * 0.5 + 0.20 * 0.5
        assert phi == pytest.approx(expected)
        # act_floor clips at 17; enemy_down clips at 1.
        phi_cap = cfg.potential(_StubMgr(3, 99, 80, 80), enemy_down=7.0)
        assert phi_cap == pytest.approx(0.45 * (3 + 1) / 4 + 0.30 + 0.20)
        # Phi is bounded in [0, 1].
        assert 0.0 <= phi_cap <= 1.0

    def test_pbrs_telescoping_synthetic_trajectory(self):
        """sum_t gamma^t F_t == gamma^T Phi_T - Phi_0 (policy invariance)."""
        cfg = RewardConfig(shaping_scale=1.0)
        gamma = cfg.gamma_shape
        # Synthetic potential trajectory ending at a terminal state (Phi=0).
        phis = [0.31, 0.34, 0.33, 0.40, 0.52, 0.47, 0.61, 0.0]
        fs = [cfg.shaping_reward(phis[t], phis[t + 1]) for t in range(len(phis) - 1)]
        telescoped = sum(gamma ** t * f for t, f in enumerate(fs))
        T = len(fs)
        assert telescoped == pytest.approx(gamma ** T * phis[-1] - phis[0])
        assert telescoped == pytest.approx(-phis[0])

    def test_shaping_scale_multiplies_f(self):
        cfg = RewardConfig(shaping_scale=0.5)
        full = RewardConfig(shaping_scale=1.0)
        assert cfg.shaping_reward(0.3, 0.4) == pytest.approx(
            0.5 * full.shaping_reward(0.3, 0.4))
        off = RewardConfig(shaping_scale=0.0)
        assert off.shaping_reward(0.3, 0.4) == 0.0

    def test_clamp(self):
        cfg = RewardConfig(shaping_scale=3.0)
        cfg.clamp()
        assert cfg.shaping_scale == 1.0
        cfg.shaping_scale = -1.0
        cfg.clamp()
        assert cfg.shaping_scale == 0.0


# ---------------------------------------------------------------------------
# Combat env
# ---------------------------------------------------------------------------

class TestRichCombatEnv:
    def test_spaces(self):
        env = RichSTS2CombatEnv()
        assert env.observation_space.shape == (ro.RICH_OBS_SIZE,)
        assert env.action_space.n == ACTION_SPACE_SIZE

    def test_reset_deterministic(self):
        env1 = RichSTS2CombatEnv()
        env2 = RichSTS2CombatEnv()
        obs1, _ = env1.reset(seed=123)
        obs2, _ = env2.reset(seed=123)
        np.testing.assert_array_equal(obs1, obs2)

    def test_run_segment_zeroed(self):
        env = RichSTS2CombatEnv()
        obs, _ = env.reset(seed=0)
        assert not obs[ro.RUN_OFF:].any()

    def test_combat_segments_populated(self):
        env = RichSTS2CombatEnv()
        obs, info = env.reset(seed=0)
        # hand ids present (starter deck draws 5)
        assert (obs[ro.IDS_HAND_OFF:ro.IDS_HAND_OFF + ro.IDS_HAND_SIZE] > 0).sum() >= 5
        # ids are integer-valued
        ids = obs[:ro.ID_BLOCK_SIZE]
        np.testing.assert_array_equal(ids, np.round(ids))
        # enemy slot 0 alive
        assert obs[ro.ENEMIES_OFF] == 1.0
        # in_combat flag
        assert obs[ro.PLAYER_CORE_OFF + 7] == 1.0
        # relic vector has the starter relic
        assert obs[ro.RELICS_OFF:ro.RELICS_OFF + ro.NUM_RELIC_IDS].sum() >= 1
        assert info["action_mask"].sum() >= 1

    def test_full_episode_with_masks(self):
        env = RichSTS2CombatEnv()
        obs, info = env.reset(seed=7)
        rng = np.random.default_rng(7)
        done = False
        steps = 0
        while not done and steps < 1000:
            mask = env.action_masks()
            assert mask.sum() >= 1
            action = int(rng.choice(np.flatnonzero(mask)))
            obs, reward, terminated, truncated, info = env.step(action)
            assert obs.shape == (ro.RICH_OBS_SIZE,)
            done = terminated or truncated
            steps += 1
        assert done
        assert "won" in info
        assert reward in (-1.0, 1.0) or reward > 1.0 or reward < -0.9  # terminal +/- shaping

    def test_progressive_deck_sampler(self):
        env = RichSTS2CombatEnv(deck_sampler="progressive")
        for seed in range(5):
            obs, info = env.reset(seed=seed)
            assert env.combat is not None
            deck_size = len(env.combat.current_player_state.starting_deck)
            assert deck_size >= 10  # starter deck at minimum

    def test_mixed_pools_resolve(self):
        # thebeyond is import-guarded; missing module must not raise
        pool = resolve_encounter_pool(
            ["act1", "act2", "act3", "act4heart", "exordium", "thecity", "thebeyond"]
        )
        assert len(pool) > 20

    def test_unknown_pool_raises(self):
        with pytest.raises(ValueError):
            resolve_encounter_pool(["nope"])

    def test_shaping_scale_setter(self):
        env = RichSTS2CombatEnv()
        env.set_shaping_scale(0.25)
        assert env.reward_config.shaping_scale == 0.25
        env.set_shaping_scale(5.0)
        assert env.reward_config.shaping_scale == 1.0


# ---------------------------------------------------------------------------
# Run env
# ---------------------------------------------------------------------------

class TestRichRunEnv:
    def test_spaces(self):
        env = RichSTS2RunEnv()
        assert env.observation_space.shape == (ro.RICH_OBS_SIZE,)
        assert env.action_space.n == TOTAL_ACTIONS

    def test_invalid_act_count(self):
        with pytest.raises(ValueError):
            RichSTS2RunEnv(max_act_count=0)
        with pytest.raises(ValueError):
            RichSTS2RunEnv(max_act_count=5)

    def test_reset_deterministic(self):
        obs1, _ = RichSTS2RunEnv().reset(seed=42)
        obs2, _ = RichSTS2RunEnv().reset(seed=42)
        np.testing.assert_array_equal(obs1, obs2)

    def test_run_segment_populated_and_combat_zeroed_out_of_combat(self):
        env = RichSTS2RunEnv()
        obs, info = env.reset(seed=0)
        assert info["phase"] == "MAP_CHOICE"
        # combat segments zeroed
        assert not obs[ro.IDS_HAND_OFF:ro.IDS_HAND_OFF + ro.IDS_HAND_SIZE].any()
        assert not obs[ro.PILE_BAGS_OFF:ro.PILE_BAGS_OFF + ro.PILE_BAGS_SIZE].any()
        assert not obs[ro.ENEMIES_OFF:ro.ENEMIES_OFF + ro.ENEMIES_SIZE].any()
        assert obs[ro.PLAYER_CORE_OFF + 7] == 0.0  # in_combat flag
        # run segment populated: phase one-hot + deck aggregates + hp
        r = ro.RUN_OFF
        assert obs[r + ro.RUN_PHASE_OFF] == 1.0  # MAP_CHOICE
        assert obs[r + ro.RUN_HP_GOLD_OFF] == 1.0  # full hp
        assert obs[r + ro.RUN_DECK_OFF] > 0  # deck size
        # map lookahead sees at least one room
        look = obs[r + ro.RUN_LOOKAHEAD_OFF:
                   r + ro.RUN_LOOKAHEAD_OFF + ro.MAP_LOOKAHEAD_ROWS * ro.NUM_MAP_POINT_TYPES]
        assert look.sum() > 0
        # ascension encoded
        assert obs[r + ro.RUN_MISC_OFF] == pytest.approx(10 / 20.0)
        # boss id resolved
        assert obs[ro.IDS_BOSS_OFF] > 0

    def test_deck_bag_and_archetype_scalars(self):
        from collections import Counter

        from sts2_env.core.enums import CardId, CardTag

        env = RichSTS2RunEnv(ascension_level=0)
        obs, info = env.reset(seed=0)
        deck = env._mgr.run_state.player.deck
        assert len(deck) == 10  # Necrobinder starter (A0: no ascension curse)

        # Deck bag: exact per-CardId counts / BAG_COUNT_SCALE.
        bag = obs[ro.DECK_BAG_OFF: ro.DECK_BAG_OFF + ro.DECK_BAG_SIZE]
        assert bag.sum() == pytest.approx(len(deck) / ro.BAG_COUNT_SCALE)
        counts = Counter(ro.CARD_ID_TO_IDX[c.card_id] for c in deck)
        for ci, cnt in counts.items():
            assert bag[ci] == pytest.approx(cnt / ro.BAG_COUNT_SCALE)
        assert (bag > 0).sum() == len(counts)

        # Archetype scalars: starter deck has Bodyguard (summon) + Unleash
        # (Osty attack), nothing upgraded, no Souls/Doom/ethereal/zero-cost.
        a = ro.ARCH_SCALARS_OFF
        assert obs[a + 0] == pytest.approx(1 / ro.ARCH_COUNT_SCALE)  # summon
        assert obs[a + 1] == 0.0  # soul generators
        assert obs[a + 2] == 0.0  # soul payoffs
        assert obs[a + 3] == 0.0  # doom appliers
        assert obs[a + 4] == 0.0  # ethereal
        assert obs[a + 5] == pytest.approx(1 / ro.ARCH_COUNT_SCALE)  # osty attacks
        assert obs[a + 6] == 0.0  # zero-cost
        assert obs[a + 7] == 0.0  # upgraded fraction

        # Cross-check membership against the live deck instances.
        n_summon = sum(1 for c in deck if c.card_id in ro.NECRO_SUMMON_CARD_IDS)
        n_osty = sum(1 for c in deck if CardTag.OSTY_ATTACK in c.tags)
        assert n_summon == 1 and n_osty == 1
        assert any(c.card_id == CardId.BODYGUARD for c in deck)

    def test_combat_segments_appear_in_combat(self):
        env = RichSTS2RunEnv()
        obs, info = env.reset(seed=3)
        rng = np.random.default_rng(3)
        for _ in range(200):
            if info["phase"] == "COMBAT":
                break
            mask = env.action_masks()
            obs, _, term, trunc, info = env.step(int(rng.choice(np.flatnonzero(mask))))
            if term or trunc:
                obs, info = env.reset(seed=3)
        assert info["phase"] == "COMBAT"
        assert obs[ro.PLAYER_CORE_OFF + 7] == 1.0  # in_combat
        assert (obs[ro.IDS_HAND_OFF:ro.IDS_HAND_OFF + ro.IDS_HAND_SIZE] > 0).any()
        # run segment still populated during combat
        assert obs[ro.RUN_OFF + ro.RUN_PHASE_OFF + 1] == 1.0  # COMBAT phase one-hot

    def test_episode_with_masks_and_shaping(self):
        env = RichSTS2RunEnv(max_act_count=1, reward_config=RewardConfig(shaping_scale=1.0))
        obs, info = env.reset(seed=11)
        rng = np.random.default_rng(11)
        done = False
        steps = 0
        while not done and steps < 3000:
            mask = env.action_masks()
            assert mask.sum() >= 1
            obs, reward, terminated, truncated, info = env.step(
                int(rng.choice(np.flatnonzero(mask))))
            done = terminated or truncated
            if not done:
                # PBRS per-step term is bounded: |F| <= gamma*1 + 1 < 2.
                assert abs(reward) < 2.0
            steps += 1
        assert done
        assert "won" in info

    def test_pbrs_telescoping_real_episode(self):
        """Discounted sum of per-step F over a real episode == -Phi_0."""
        cfg = RewardConfig(shaping_scale=1.0)
        env = RichSTS2RunEnv(max_act_count=1, reward_config=cfg)
        obs, info = env.reset(seed=11)
        phi_0 = env._phi_prev
        assert 0.0 < phi_0 <= 1.0
        gamma = cfg.gamma_shape
        rng = np.random.default_rng(11)
        telescoped = 0.0
        t = 0
        done = False
        while not done and t < 3000:
            mask = env.action_masks()
            obs, reward, terminated, truncated, info = env.step(
                int(rng.choice(np.flatnonzero(mask))))
            done = terminated or truncated
            f = reward
            if done:
                # Subtract the terminal contribution to isolate F.
                if info.get("sim_error"):
                    f -= 0.0
                elif terminated:
                    f -= cfg.terminal_reward(bool(info.get("won", False)))
                else:
                    f -= cfg.truncation
            telescoped += gamma ** t * f
            t += 1
        assert done
        # Phi_T := 0 at terminal, so sum_t gamma^t F_t == -Phi_0.
        assert telescoped == pytest.approx(-phi_0, abs=1e-5)

    def test_pure_sparse_when_shaping_zero(self):
        env = RichSTS2RunEnv(max_act_count=1, reward_config=RewardConfig(shaping_scale=0.0))
        obs, info = env.reset(seed=5)
        rng = np.random.default_rng(5)
        done = False
        steps = 0
        while not done and steps < 3000:
            mask = env.action_masks()
            obs, reward, terminated, truncated, info = env.step(
                int(rng.choice(np.flatnonzero(mask))))
            done = terminated or truncated
            if not done:
                assert reward == 0.0
            steps += 1
        assert done
        assert reward in (1.0, -1.0)

    def test_obs_layout_identical_to_combat_env(self):
        """Combat and run envs must share the exact observation layout so
        policy weights transfer between curriculum stages."""
        combat_env = RichSTS2CombatEnv()
        run_env = RichSTS2RunEnv()
        assert combat_env.observation_space.shape == run_env.observation_space.shape
        assert combat_env.observation_space.dtype == run_env.observation_space.dtype
