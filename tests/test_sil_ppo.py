"""Tests for Self-Imitation Learning (SILAnchoredMaskablePPO).

Covers: the SIL loss terms on hand-built batches (positive advantages only
contribute; the policy-term weight is detached), episodic discounted-return
computation, the transition ring buffer (episode bookkeeping + eviction),
rollout ingestion with synthetic episode boundaries (including episodes
spanning rollouts), and an end-to-end integration run on RichSTS2RunEnv
where train/sil_loss must be logged and finite.
"""

from __future__ import annotations

import numpy as np
import pytest

torch = pytest.importorskip("torch")
pytest.importorskip("sb3_contrib")

from sts2_env.train.anchored_ppo import (  # noqa: E402
    SILAnchoredMaskablePPO,
    SILReplayBuffer,
    discounted_returns,
    new_sil_pending,
    sil_ingest_rollout,
    sil_terms,
)


# ---------------------------------------------------------------------------
# discounted_returns
# ---------------------------------------------------------------------------

class TestDiscountedReturns:
    def test_known_sequence(self):
        r = np.array([1.0, 0.0, 2.0], dtype=np.float32)
        out = discounted_returns(r, gamma=0.5)
        assert out.dtype == np.float32
        # R2 = 2; R1 = 0 + .5*2 = 1; R0 = 1 + .5*1 = 1.5
        np.testing.assert_allclose(out, [1.5, 1.0, 2.0], rtol=1e-6)

    def test_single_step(self):
        np.testing.assert_allclose(
            discounted_returns(np.array([-1.0]), 0.997), [-1.0]
        )

    def test_gamma_one_is_reverse_cumsum(self):
        r = np.array([1.0, 2.0, 3.0, 4.0], dtype=np.float32)
        np.testing.assert_allclose(
            discounted_returns(r, 1.0), [10.0, 9.0, 7.0, 4.0], rtol=1e-6
        )


# ---------------------------------------------------------------------------
# sil_terms: hand-built batches
# ---------------------------------------------------------------------------

class TestSILTerms:
    def test_loss_only_from_positive_advantages(self):
        """Row 0 has adv=+2, row 1 has adv=0 (R < V clamps to 0): the policy
        term must come only from row 0 and the value term must equal
        mean(adv^2) over the batch."""
        values = torch.tensor([1.0, 2.0])
        returns = torch.tensor([3.0, 1.0])
        log_prob = torch.tensor([-0.5, -0.7])
        policy_term, value_term, adv = sil_terms(values, log_prob, returns)
        np.testing.assert_allclose(adv.numpy(), [2.0, 0.0])
        # -(2 * -0.5 + 0 * -0.7) / 2 = 0.5 -- row 1's log-prob is irrelevant.
        assert policy_term.item() == pytest.approx(0.5)
        assert value_term.item() == pytest.approx((4.0 + 0.0) / 2)

    def test_all_nonpositive_advantages_zero_loss(self):
        values = torch.tensor([5.0, 5.0], requires_grad=True)
        returns = torch.tensor([4.0, 5.0])
        log_prob = torch.tensor([-1.0, -2.0], requires_grad=True)
        policy_term, value_term, adv = sil_terms(values, log_prob, returns)
        assert policy_term.item() == 0.0
        assert value_term.item() == 0.0
        (policy_term + value_term).backward()
        # No gradient flows anywhere: the SIL filter rejects the whole batch.
        assert torch.count_nonzero(values.grad).item() == 0
        assert torch.count_nonzero(log_prob.grad).item() == 0

    def test_policy_term_weight_is_detached(self):
        """The advantage weight of the policy term must NOT backprop into V."""
        values = torch.tensor([1.0, 2.0], requires_grad=True)
        returns = torch.tensor([3.0, 1.0])
        log_prob = torch.tensor([-0.5, -0.7], requires_grad=True)
        policy_term, _, _ = sil_terms(values, log_prob, returns)
        policy_term.backward()
        # The graph must not even reach V through the policy term (grad None),
        # or if it does, contribute exactly nothing.
        assert values.grad is None or torch.count_nonzero(values.grad).item() == 0
        # d/dlp0 of -(2*lp0)/2 = -1; row 1 (adv 0) gets no gradient.
        np.testing.assert_allclose(log_prob.grad.numpy(), [-1.0, 0.0], rtol=1e-6)

    def test_value_term_pushes_v_toward_r_on_positive_rows_only(self):
        values = torch.tensor([1.0, 2.0], requires_grad=True)
        returns = torch.tensor([3.0, 1.0])
        log_prob = torch.tensor([-0.5, -0.7])
        _, value_term, _ = sil_terms(values, log_prob, returns)
        value_term.backward()
        # d/dV0 of mean((R-V)_+^2) = 2 * 2 * (-1) / 2 = -2 (V must go UP).
        np.testing.assert_allclose(values.grad.numpy(), [-2.0, 0.0], rtol=1e-6)


# ---------------------------------------------------------------------------
# SILReplayBuffer
# ---------------------------------------------------------------------------

def _ep(n: int, obs_dim: int = 3, n_actions: int = 4, base: float = 0.0):
    """Synthetic episode with recognizable per-transition values."""
    obs = np.arange(n * obs_dim, dtype=np.float32).reshape(n, obs_dim) + base
    actions = (np.arange(n) % n_actions).astype(np.int64)
    masks = np.ones((n, n_actions), dtype=bool)
    returns = np.full(n, base, dtype=np.float32) + np.arange(n)
    return obs, actions, masks, returns


class TestSILReplayBuffer:
    def test_add_and_len(self):
        buf = SILReplayBuffer(capacity=100)
        assert len(buf) == 0
        assert buf.num_episodes == 0
        buf.add_episode(*_ep(5))
        assert len(buf) == 5
        assert buf.num_episodes == 1
        buf.add_episode(*_ep(7))
        assert len(buf) == 12
        assert buf.num_episodes == 2

    def test_empty_episode_ignored(self):
        buf = SILReplayBuffer(capacity=10)
        buf.add_episode(*_ep(0))
        assert len(buf) == 0
        assert buf.num_episodes == 0

    def test_ring_eviction_and_episode_bookkeeping(self):
        buf = SILReplayBuffer(capacity=10)
        for k in range(3):
            buf.add_episode(*_ep(4, base=100.0 * k))
        # 12 transitions written into capacity 10: full, ep0 partially
        # overwritten but still resident (its last 2 transitions live).
        assert len(buf) == 10
        assert buf.full
        assert buf.num_episodes == 3
        buf.add_episode(*_ep(4, base=300.0))
        # 16 written, low watermark 6: ep0 (end 4) fully evicted.
        assert len(buf) == 10
        assert buf.num_episodes == 3

    def test_stored_values_roundtrip(self):
        buf = SILReplayBuffer(capacity=50)
        obs, actions, masks, returns = _ep(6, base=42.0)
        masks[2, 1] = False
        buf.add_episode(obs, actions, masks, returns)
        np.testing.assert_array_equal(buf.obs[:6], obs)
        np.testing.assert_array_equal(buf.actions[:6], actions)
        np.testing.assert_array_equal(buf.masks[:6], masks)
        np.testing.assert_array_equal(buf.returns[:6], returns)

    def test_sample_shapes_and_membership(self):
        buf = SILReplayBuffer(capacity=50)
        buf.add_episode(*_ep(8, base=7.0))
        rng = np.random.default_rng(0)
        obs, actions, masks, returns = buf.sample(16, rng)
        assert obs.shape == (16, 3) and obs.dtype == np.float32
        assert actions.shape == (16,) and actions.dtype == np.int64
        assert masks.shape == (16, 4) and masks.dtype == bool
        assert returns.shape == (16,) and returns.dtype == np.float32
        assert set(returns.tolist()) <= set((_ep(8, base=7.0)[3]).tolist())

    def test_sample_empty_raises(self):
        with pytest.raises(ValueError):
            SILReplayBuffer(capacity=10).sample(4, np.random.default_rng(0))

    def test_oversized_episode_keeps_tail(self):
        buf = SILReplayBuffer(capacity=4)
        obs, actions, masks, returns = _ep(6)
        buf.add_episode(obs, actions, masks, returns)
        assert len(buf) == 4
        np.testing.assert_array_equal(buf.returns[:4], returns[-4:])


# ---------------------------------------------------------------------------
# Rollout ingestion: episode boundaries from synthetic dones
# ---------------------------------------------------------------------------

def _rollout(n_steps, n_envs, starts, rewards, obs_dim=3, n_actions=4):
    """Synthetic rollout arrays in rollout-buffer layout. obs[t, i, 0] and
    actions[t, i] encode (t, i) so alignment is checkable after ingestion."""
    obs = np.zeros((n_steps, n_envs, obs_dim), dtype=np.float32)
    act = np.zeros((n_steps, n_envs, 1), dtype=np.float32)
    msk = np.ones((n_steps, n_envs, n_actions), dtype=np.float32)
    for t in range(n_steps):
        for i in range(n_envs):
            obs[t, i, 0] = 10.0 * t + i
            act[t, i, 0] = (t + i) % n_actions
    return (
        obs, act,
        np.asarray(rewards, dtype=np.float32).reshape(n_steps, n_envs),
        np.asarray(starts, dtype=np.float32).reshape(n_steps, n_envs),
        msk,
    )


class TestSILIngestion:
    GAMMA = 0.5

    def test_boundary_closes_episode_with_correct_returns(self):
        buf = SILReplayBuffer(capacity=100)
        pending = [new_sil_pending()]
        # starts[3]=1 <=> episode ended at t=2 with rewards [1, 2, 3].
        arrs = _rollout(6, 1, [1, 0, 0, 1, 0, 0], [1, 2, 3, 4, 5, 6])
        closed = sil_ingest_rollout(pending, *arrs, self.GAMMA, buf)
        assert closed == 1
        assert len(buf) == 3
        assert buf.num_episodes == 1
        # R = [1 + .5*(2 + .5*3), 2 + .5*3, 3] = [2.75, 3.5, 3]
        np.testing.assert_allclose(buf.returns[:3], [2.75, 3.5, 3.0])
        # Transition alignment: obs/action of steps 0..2, env 0.
        np.testing.assert_allclose(buf.obs[:3, 0], [0.0, 10.0, 20.0])
        np.testing.assert_array_equal(buf.actions[:3], [0, 1, 2])
        # Steps 3..5 stay pending (episode still open).
        assert len(pending[0]["rewards"]) == 1
        assert pending[0]["rewards"][0].shape == (3,)

    def test_episode_spanning_rollouts(self):
        """An episode whose tail arrives in the NEXT rollout must be closed
        there with returns over BOTH fragments."""
        buf = SILReplayBuffer(capacity=100)
        pending = [new_sil_pending()]
        sil_ingest_rollout(
            pending, *_rollout(6, 1, [1, 0, 0, 1, 0, 0], [1, 2, 3, 4, 5, 6]),
            self.GAMMA, buf,
        )
        # Next rollout: starts[0]=1 closes the pending [4, 5, 6] episode.
        closed = sil_ingest_rollout(
            pending, *_rollout(2, 1, [1, 0], [7, 8]), self.GAMMA, buf,
        )
        assert closed == 1
        assert len(buf) == 6
        assert buf.num_episodes == 2
        # R over [4, 5, 6]: [4 + .5*8, 5 + .5*6, 6] = [8, 8, 6]
        np.testing.assert_allclose(buf.returns[3:6], [8.0, 8.0, 6.0])
        np.testing.assert_allclose(buf.obs[3:6, 0], [30.0, 40.0, 50.0])

    def test_no_boundary_closes_nothing(self):
        buf = SILReplayBuffer(capacity=100)
        pending = [new_sil_pending()]
        closed = sil_ingest_rollout(
            pending, *_rollout(4, 1, [0, 0, 0, 0], [1, 1, 1, 1]),
            self.GAMMA, buf,
        )
        assert closed == 0
        assert len(buf) == 0

    def test_back_to_back_episodes_in_one_rollout(self):
        buf = SILReplayBuffer(capacity=100)
        pending = [new_sil_pending()]
        # Episodes: [t0], [t1], [t2, t3] (open tail).
        closed = sil_ingest_rollout(
            pending, *_rollout(4, 1, [1, 1, 1, 0], [5, 7, 1, 1]),
            self.GAMMA, buf,
        )
        assert closed == 2
        assert len(buf) == 2
        np.testing.assert_allclose(buf.returns[:2], [5.0, 7.0])

    def test_envs_are_independent(self):
        buf = SILReplayBuffer(capacity=100)
        pending = [new_sil_pending(), new_sil_pending()]
        starts = [[1, 1], [0, 0], [1, 0], [0, 0]]  # env0 ends at t1; env1 never
        rewards = [[1, 9], [2, 9], [3, 9], [4, 9]]
        closed = sil_ingest_rollout(
            pending, *_rollout(4, 2, starts, rewards), self.GAMMA, buf,
        )
        assert closed == 1
        assert len(buf) == 2  # env0's [t0, t1] episode only
        np.testing.assert_allclose(buf.returns[:2], [1 + 0.5 * 2, 2.0])
        np.testing.assert_allclose(buf.obs[:2, 0], [0.0, 10.0])  # env 0 column
        assert len(pending[1]["rewards"]) == 1  # env1 fully pending


# ---------------------------------------------------------------------------
# Integration: SIL model trains on the rich run env, sil_loss logged finite
# ---------------------------------------------------------------------------

def _make_run_env(max_steps: int = 25):
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    return RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=0,
        max_act_count=2,
        reward_config=RewardConfig(shaping_scale=1.0),
        max_steps=max_steps,
    )


@pytest.fixture(scope="module")
def sil_model():
    from sts2_env.train.policy import rich_policy_kwargs

    # max_steps=25 forces episode closes well inside n_steps=64 so the SIL
    # buffer fills within the first rollout; sil_batch_size shrunk to match.
    model = SILAnchoredMaskablePPO(
        "MlpPolicy",
        _make_run_env(max_steps=25),
        sil_coef=0.1,
        sil_updates=2,
        sil_batch_size=32,
        sil_buffer_capacity=5_000,
        n_steps=64,
        batch_size=64,
        n_epochs=1,
        gamma=0.997,
        policy_kwargs=rich_policy_kwargs(),
        device="cpu",
        verbose=0,
        seed=0,
    )
    model.learn(total_timesteps=128)  # n_steps=64, 1 env -> 2 updates
    return model


class TestSILIntegration:
    def test_two_updates_log_finite_sil_loss(self, sil_model):
        assert sil_model.num_timesteps >= 128
        logged = sil_model.logger.name_to_value
        assert "train/sil_loss" in logged
        assert np.isfinite(logged["train/sil_loss"])
        assert "train/sil_mean_adv" in logged
        assert np.isfinite(logged["train/sil_mean_adv"])
        assert logged["train/sil_mean_adv"] >= 0.0  # clamped at 0

    def test_buffer_ingested_closed_episodes(self, sil_model):
        # 128 steps at max_steps=25 must close several episodes.
        assert sil_model.sil_buffer.num_episodes >= 2
        assert len(sil_model.sil_buffer) >= sil_model.sil_batch_size
        logged = sil_model.logger.name_to_value
        assert logged["train/sil_buffer_eps"] == sil_model.sil_buffer.num_episodes

    def test_save_load_drops_buffer_keeps_config(self, sil_model, tmp_path):
        path = tmp_path / "sil_ckpt"
        sil_model.save(str(path))
        loaded = SILAnchoredMaskablePPO.load(str(path), device="cpu")
        # Runtime state is never checkpointed; config scalars survive.
        assert len(loaded.sil_buffer) == 0
        assert loaded._sil_pending is None
        assert loaded.anchor_policy is None
        assert loaded.sil_coef == pytest.approx(sil_model.sil_coef)
        assert loaded.sil_updates == sil_model.sil_updates
        assert loaded.sil_batch_size == sil_model.sil_batch_size

    def test_composes_with_anchor(self, sil_model, tmp_path):
        """SIL + BC anchor together: one PPO+SIL update with the anchor
        attached must log both anchor_kl and sil metrics."""
        ref_path = tmp_path / "ref"
        sil_model.save(str(ref_path))

        from sts2_env.train.policy import rich_policy_kwargs

        model = SILAnchoredMaskablePPO(
            "MlpPolicy",
            _make_run_env(max_steps=25),
            sil_coef=0.1,
            sil_updates=1,
            sil_batch_size=16,
            sil_buffer_capacity=1_000,
            anchor_coef=0.5,
            anchor_coef_final=0.02,
            anchor_decay_steps=10_000_000,
            n_steps=64,
            batch_size=64,
            n_epochs=1,
            gamma=0.997,
            policy_kwargs=rich_policy_kwargs(),
            device="cpu",
            verbose=0,
            seed=1,
        )
        model.set_anchor(str(ref_path) + ".zip")
        model.learn(total_timesteps=64)
        logged = model.logger.name_to_value
        assert "train/anchor_kl" in logged
        assert np.isfinite(logged["train/anchor_kl"])
        assert "train/sil_loss" in logged
        assert np.isfinite(logged["train/sil_loss"])
