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


# ---------------------------------------------------------------------------
# Reward config
# ---------------------------------------------------------------------------

class TestRewardConfig:
    def test_terminal_never_annealed(self):
        cfg = RewardConfig(shaping_scale=0.0)
        assert cfg.terminal_reward(True) == 1.0
        assert cfg.terminal_reward(False) == -1.0

    def test_shaping_scales(self):
        cfg = RewardConfig(shaping_scale=0.5)
        assert cfg.act_completion_reward() == pytest.approx(0.5 * 0.25)
        assert cfg.floor_reward(2) == pytest.approx(0.5 * 0.004 * 2)
        assert cfg.combat_win_reward(100, 80) == pytest.approx(0.5 * 0.05 * 0.8)

    def test_combat_win_reward_clamped(self):
        cfg = RewardConfig(shaping_scale=1.0)
        assert cfg.combat_win_reward(50, 80) == pytest.approx(0.05)  # ratio capped at 1
        assert cfg.combat_win_reward(0, 10) == 0.0

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
        total_shaping = 0.0
        done = False
        steps = 0
        while not done and steps < 3000:
            mask = env.action_masks()
            assert mask.sum() >= 1
            obs, reward, terminated, truncated, info = env.step(
                int(rng.choice(np.flatnonzero(mask))))
            done = terminated or truncated
            if not done:
                total_shaping += reward
            steps += 1
        assert done
        assert "won" in info
        # a dead-at-floor-N run must still have accumulated floor shaping
        if info.get("floor", 0) > 0:
            assert total_shaping > 0.0

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
