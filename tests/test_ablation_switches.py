"""Tests for the ablation-study switches (scripts/ablation_study.py arms).

Covers, with DEFAULTS UNCHANGED as the invariant:

* ``RewardConfig.legacy_shaping`` -- attempt-6-era event shaping (floor
  +0.004, act +0.25, combat HP retention +0.05) applied by RichSTS2RunEnv
  INSTEAD of the PBRS term.
* ``RichSTS2RunEnv(include_deck_obs=False)`` -- deck-bag + archetype obs
  segment zeroed, layout unchanged.
* ``RichFeaturesExtractor(hand_encoding="meanpool")`` -- pre-per-slot
  permutation-invariant hand encoding.
"""

from __future__ import annotations

import numpy as np
import pytest

from sts2_env.gym_env import rich_observation as ro
from sts2_env.gym_env.reward_config import RewardConfig
from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv


# ---------------------------------------------------------------------------
# RewardConfig: legacy switch + legacy term math
# ---------------------------------------------------------------------------

class TestLegacyRewardConfig:
    def test_defaults_unchanged(self):
        cfg = RewardConfig()
        assert cfg.legacy_shaping is False
        assert cfg.win == 1.0 and cfg.death == -1.0 and cfg.truncation == -1.0
        assert cfg.gamma_shape == pytest.approx(0.997)
        assert cfg.shaping_scale == 1.0

    def test_legacy_magnitudes_match_attempt6(self):
        cfg = RewardConfig(legacy_shaping=True)
        assert cfg.floor == pytest.approx(0.004)
        assert cfg.act_completion == pytest.approx(0.25)
        assert cfg.combat_hp_retention == pytest.approx(0.05)

    def test_legacy_term_math(self):
        cfg = RewardConfig(legacy_shaping=True, shaping_scale=1.0)
        assert cfg.floor_reward(1) == pytest.approx(0.004)
        assert cfg.floor_reward(3) == pytest.approx(0.012)
        assert cfg.act_completion_reward(1) == pytest.approx(0.25)
        assert cfg.act_completion_reward(2) == pytest.approx(0.50)
        # HP retention: full retention 0.05, half retention 0.025.
        assert cfg.combat_win_reward(80, 80) == pytest.approx(0.05)
        assert cfg.combat_win_reward(80, 40) == pytest.approx(0.025)
        # Clipped: healing above start caps at 1.0; hp_start<=0 gives 0.
        assert cfg.combat_win_reward(40, 80) == pytest.approx(0.05)
        assert cfg.combat_win_reward(0, 10) == 0.0

    def test_legacy_terms_scale_with_shaping_scale(self):
        half = RewardConfig(legacy_shaping=True, shaping_scale=0.5)
        off = RewardConfig(legacy_shaping=True, shaping_scale=0.0)
        assert half.floor_reward(1) == pytest.approx(0.002)
        assert half.act_completion_reward(1) == pytest.approx(0.125)
        assert half.combat_win_reward(80, 80) == pytest.approx(0.025)
        assert off.floor_reward(5) == 0.0
        assert off.act_completion_reward(2) == 0.0
        assert off.combat_win_reward(80, 80) == 0.0


# ---------------------------------------------------------------------------
# Run env: legacy shaping path
# ---------------------------------------------------------------------------

class TestLegacyRunEnv:
    def test_legacy_nonterminal_rewards_are_nonnegative_bonuses(self):
        """Legacy shaping only ever ADDS bonuses per step (PBRS goes negative
        whenever Phi drops, e.g. on HP loss), so the reward stream is the
        cleanest behavioral fingerprint of the switch."""
        env = RichSTS2RunEnv(
            max_act_count=1,
            reward_config=RewardConfig(legacy_shaping=True, shaping_scale=1.0),
        )
        obs, info = env.reset(seed=11)
        rng = np.random.default_rng(11)
        positives = 0
        done = False
        steps = 0
        while not done and steps < 3000:
            mask = env.action_masks()
            obs, reward, terminated, truncated, info = env.step(
                int(rng.choice(np.flatnonzero(mask))))
            done = terminated or truncated
            if not done:
                assert reward >= 0.0
                # Largest possible per-step bonus: act 0.25 + floor terms +
                # retention 0.05 (well under 1).
                assert reward < 1.0
                if reward > 0:
                    positives += 1
            steps += 1
        assert done
        assert positives >= 1  # random play climbs at least one floor

    def test_legacy_pure_sparse_when_scale_zero(self):
        env = RichSTS2RunEnv(
            max_act_count=1,
            reward_config=RewardConfig(legacy_shaping=True, shaping_scale=0.0),
        )
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

    def test_legacy_floor_bonus_accounting(self):
        """Replaying the SAME seed+actions with scale 1 vs scale 0: every
        per-step difference must decompose into the legacy magnitudes."""
        actions_log: list[int] = []
        env0 = RichSTS2RunEnv(
            max_act_count=1,
            reward_config=RewardConfig(legacy_shaping=True, shaping_scale=0.0),
        )
        env0.reset(seed=23)
        rng = np.random.default_rng(23)
        rewards0 = []
        done = False
        while not done and len(actions_log) < 800:
            a = int(rng.choice(np.flatnonzero(env0.action_masks())))
            actions_log.append(a)
            _, r, term, trunc, _ = env0.step(a)
            rewards0.append(r)
            done = term or trunc

        env1 = RichSTS2RunEnv(
            max_act_count=1,
            reward_config=RewardConfig(legacy_shaping=True, shaping_scale=1.0),
        )
        env1.reset(seed=23)
        rewards1 = []
        for a in actions_log:
            _, r, term, trunc, _ = env1.step(a)
            rewards1.append(r)
            if term or trunc:
                break
        assert len(rewards1) == len(rewards0)  # same trajectory
        floor_bonuses = 0
        for r0, r1 in zip(rewards0, rewards1):
            diff = r1 - r0
            assert diff >= -1e-9
            if abs(diff - 0.004) < 1e-9:
                floor_bonuses += 1
        assert floor_bonuses >= 1  # bare floor climbs got exactly +0.004

    def test_default_env_still_pbrs(self):
        """Default RewardConfig keeps the PBRS path (regression guard)."""
        env = RichSTS2RunEnv(max_act_count=1)
        assert env.reward_config.legacy_shaping is False
        env.reset(seed=3)
        rng = np.random.default_rng(3)
        saw_negative = False
        for _ in range(300):
            _, r, term, trunc, _ = env.step(
                int(rng.choice(np.flatnonzero(env.action_masks()))))
            if term or trunc:
                break
            if r < 0:
                saw_negative = True
        # PBRS emits negative per-step terms (HP loss etc.) -- the legacy
        # path never does, so this distinguishes the two.
        assert saw_negative


# ---------------------------------------------------------------------------
# Run env: include_deck_obs switch
# ---------------------------------------------------------------------------

class TestNoDeckObs:
    def test_deck_segment_zeroed(self):
        env = RichSTS2RunEnv(max_act_count=1, include_deck_obs=False)
        obs, _ = env.reset(seed=7)
        seg = obs[ro.DECK_BAG_OFF:ro.ARCH_SCALARS_OFF + ro.ARCH_SCALARS_SIZE]
        assert not seg.any()
        # ... and stays zeroed after steps.
        rng = np.random.default_rng(7)
        for _ in range(50):
            obs, _, term, trunc, _ = env.step(
                int(rng.choice(np.flatnonzero(env.action_masks()))))
            if term or trunc:
                break
            seg = obs[ro.DECK_BAG_OFF:ro.ARCH_SCALARS_OFF + ro.ARCH_SCALARS_SIZE]
            assert not seg.any()

    def test_default_env_has_deck_segment(self):
        env = RichSTS2RunEnv(max_act_count=1)
        assert env.include_deck_obs is True
        obs, _ = env.reset(seed=7)
        deck_bag = obs[ro.DECK_BAG_OFF:ro.DECK_BAG_OFF + ro.DECK_BAG_SIZE]
        assert deck_bag.sum() > 0  # starter deck visible

    def test_rest_of_obs_identical(self):
        """The switch must ONLY affect the deck-bag+archetype segment."""
        a = RichSTS2RunEnv(max_act_count=1, include_deck_obs=True)
        b = RichSTS2RunEnv(max_act_count=1, include_deck_obs=False)
        obs_a, _ = a.reset(seed=13)
        obs_b, _ = b.reset(seed=13)
        np.testing.assert_array_equal(obs_a[:ro.DECK_BAG_OFF], obs_b[:ro.DECK_BAG_OFF])
        # RUN_DECK aggregates (deck size, cost, etc.) remain in both.
        agg = obs_b[ro.RUN_OFF + ro.RUN_DECK_OFF:ro.RUN_OFF + ro.RUN_DECK_OFF + ro.DECK_AGG_SIZE]
        assert agg.sum() > 0


# ---------------------------------------------------------------------------
# Policy: hand_encoding switch
# ---------------------------------------------------------------------------

class TestHandEncodingSwitch:
    def _space(self):
        from gymnasium import spaces
        return spaces.Box(
            low=ro.RICH_OBS_LOW, high=ro.RICH_OBS_HIGH,
            shape=(ro.RICH_OBS_SIZE,), dtype=np.float32,
        )

    def test_meanpool_features_dim(self):
        pytest.importorskip("torch")
        from sts2_env.train.policy import RichFeaturesExtractor
        fe = RichFeaturesExtractor(self._space(), hand_encoding="meanpool")
        flat_size = ro.DECK_BAG_OFF - ro.PILE_SIZES_OFF
        expected = (
            96                       # pooled hand only (no per-slot concat)
            + ro.NUM_PILES * 96      # pile bag projections
            + 96                     # deck bag projection
            + 16 + 16                # potion + boss embeddings
            + flat_size
            + ro.ARCH_SCALARS_SIZE
        )
        assert fe.features_dim == expected

    def test_meanpool_forward_and_permutation_invariance(self):
        torch = pytest.importorskip("torch")
        from sts2_env.train.policy import RichFeaturesExtractor
        fe = RichFeaturesExtractor(self._space(), hand_encoding="meanpool")
        a = torch.zeros(1, ro.RICH_OBS_SIZE)
        a[0, ro.IDS_HAND_OFF + 0] = 5.0
        a[0, ro.IDS_HAND_OFF + 1] = 9.0
        b = a.clone()
        b[0, ro.IDS_HAND_OFF + 0] = 9.0
        b[0, ro.IDS_HAND_OFF + 1] = 5.0
        ya, yb = fe(a), fe(b)
        assert ya.shape == (1, fe.features_dim)
        assert torch.isfinite(ya).all()
        # Mean-pool cannot distinguish slot order -- the whole output matches.
        assert torch.allclose(ya, yb, atol=1e-6)

    def test_perslot_default_is_slot_sensitive(self):
        torch = pytest.importorskip("torch")
        from sts2_env.train.policy import RichFeaturesExtractor
        fe = RichFeaturesExtractor(self._space())
        assert fe.hand_encoding == "perslot"
        a = torch.zeros(1, ro.RICH_OBS_SIZE)
        a[0, ro.IDS_HAND_OFF + 0] = 5.0
        a[0, ro.IDS_HAND_OFF + 1] = 9.0
        b = a.clone()
        b[0, ro.IDS_HAND_OFF + 0] = 9.0
        b[0, ro.IDS_HAND_OFF + 1] = 5.0
        assert not torch.allclose(fe(a), fe(b))

    def test_invalid_hand_encoding_raises(self):
        pytest.importorskip("torch")
        from sts2_env.train.policy import RichFeaturesExtractor
        with pytest.raises(ValueError):
            RichFeaturesExtractor(self._space(), hand_encoding="attention")

    def test_policy_kwargs_pass_through_and_default(self):
        pytest.importorskip("torch")
        from sts2_env.train.policy import rich_policy_kwargs
        kw = rich_policy_kwargs()
        assert kw["features_extractor_kwargs"]["hand_encoding"] == "perslot"
        kw = rich_policy_kwargs(hand_encoding="meanpool")
        assert kw["features_extractor_kwargs"]["hand_encoding"] == "meanpool"
