"""Tests for AnchoredMaskablePPO (KL-to-BC anchor in the PPO loss)."""

from __future__ import annotations

import math

import numpy as np
import pytest

torch = pytest.importorskip("torch")
pytest.importorskip("sb3_contrib")

from sb3_contrib import MaskablePPO  # noqa: E402
from sb3_contrib.common.maskable.distributions import MaskableCategorical  # noqa: E402

from sts2_env.train.anchored_ppo import (  # noqa: E402
    AnchoredMaskablePPO,
    anchor_coef_at,
    masked_kl,
)


# ---------------------------------------------------------------------------
# masked_kl: hand-built distributions with masks -> known KL
# ---------------------------------------------------------------------------

class TestMaskedKL:
    def test_known_kl_with_mask(self):
        """KL over the 3 valid actions of a 4-action space, computed by hand."""
        mask = np.array([[True, True, True, False]])
        p_probs = [0.7, 0.2, 0.1]
        q_probs = [0.5, 0.25, 0.25]
        # Raw logit of the masked slot is arbitrary: apply_masking floors it.
        p = MaskableCategorical(
            logits=torch.log(torch.tensor([[0.7, 0.2, 0.1, 123.0]])), masks=mask
        )
        q = MaskableCategorical(
            logits=torch.log(torch.tensor([[0.5, 0.25, 0.25, 456.0]])), masks=mask
        )
        expected = sum(
            pi * math.log(pi / qi) for pi, qi in zip(p_probs, q_probs)
        )
        kl = masked_kl(p, q)
        assert kl.shape == (1,)
        assert kl.item() == pytest.approx(expected, abs=1e-6)

    def test_identical_distributions_zero(self):
        mask = np.array([[True, False, True, True]])
        logits = torch.log(torch.tensor([[0.4, 0.9, 0.35, 0.25]]))
        p = MaskableCategorical(logits=logits.clone(), masks=mask)
        q = MaskableCategorical(logits=logits.clone(), masks=mask)
        assert masked_kl(p, q).item() == pytest.approx(0.0, abs=1e-7)

    def test_masked_slot_contributes_nothing(self):
        """Changing only the raw logit of a masked-out action changes nothing."""
        mask = np.array([[True, True, False]])
        p = MaskableCategorical(logits=torch.tensor([[1.0, 0.0, -2.0]]), masks=mask)
        q1 = MaskableCategorical(logits=torch.tensor([[0.5, 0.5, -2.0]]), masks=mask)
        q2 = MaskableCategorical(logits=torch.tensor([[0.5, 0.5, 99.0]]), masks=mask)
        # Equal up to float32 renormalization noise (Categorical normalizes
        # the raw logits once before masking, so a huge raw logit shifts all
        # slots by a constant that only cancels exactly in infinite precision).
        assert masked_kl(p, q1).item() == pytest.approx(masked_kl(p, q2).item(), abs=1e-5)

    def test_batch_rows_independent(self):
        mask = np.array([[True, True, True], [True, True, False]])
        p = MaskableCategorical(
            logits=torch.log(torch.tensor([[0.5, 0.3, 0.2], [0.6, 0.4, 0.31]])),
            masks=mask,
        )
        q = MaskableCategorical(
            logits=torch.log(torch.tensor([[0.2, 0.3, 0.5], [0.5, 0.5, 0.17]])),
            masks=mask,
        )
        kl = masked_kl(p, q)
        assert kl.shape == (2,)
        row0 = 0.5 * math.log(0.5 / 0.2) + 0.3 * math.log(0.3 / 0.3) + 0.2 * math.log(0.2 / 0.5)
        row1 = 0.6 * math.log(0.6 / 0.5) + 0.4 * math.log(0.4 / 0.5)
        assert kl[0].item() == pytest.approx(row0, abs=1e-6)
        assert kl[1].item() == pytest.approx(row1, abs=1e-6)

    def test_gradient_flows_through_p(self):
        """The anchor term must backprop into the current policy's logits."""
        mask = np.array([[True, True, False]])
        raw = torch.tensor([[1.0, -1.0, 0.0]], requires_grad=True)
        p = MaskableCategorical(logits=raw, masks=mask)
        q = MaskableCategorical(logits=torch.tensor([[0.0, 0.0, 0.0]]), masks=mask)
        masked_kl(p, q).mean().backward()
        assert raw.grad is not None
        assert torch.isfinite(raw.grad).all()
        assert raw.grad.abs().sum() > 0


# ---------------------------------------------------------------------------
# Coefficient schedule
# ---------------------------------------------------------------------------

class TestAnchorCoefSchedule:
    def test_endpoints(self):
        assert anchor_coef_at(0, 0.5, 0.02, 10_000_000) == pytest.approx(0.5)
        assert anchor_coef_at(10_000_000, 0.5, 0.02, 10_000_000) == pytest.approx(0.02)
        assert anchor_coef_at(25_000_000, 0.5, 0.02, 10_000_000) == pytest.approx(0.02)

    def test_midpoint_linear(self):
        assert anchor_coef_at(5_000_000, 0.5, 0.02, 10_000_000) == pytest.approx(0.26)

    def test_degenerate_decay_steps(self):
        assert anchor_coef_at(0, 0.5, 0.02, 0) == pytest.approx(0.02)

    def test_model_method_uses_num_timesteps(self, anchored_pair):
        model, _ = anchored_pair
        saved = model.num_timesteps
        try:
            model.num_timesteps = 0
            assert model.current_anchor_coef() == pytest.approx(model.anchor_coef)
            model.num_timesteps = model.anchor_decay_steps
            assert model.current_anchor_coef() == pytest.approx(model.anchor_coef_final)
        finally:
            model.num_timesteps = saved


# ---------------------------------------------------------------------------
# End-to-end: anchored model trains on the rich run env
# ---------------------------------------------------------------------------

def _make_run_env():
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    return RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=0,
        max_act_count=2,
        reward_config=RewardConfig(shaping_scale=1.0),
        max_steps=500,
    )


def _small_kwargs():
    from sts2_env.train.policy import rich_policy_kwargs

    return dict(
        n_steps=64,
        batch_size=64,
        n_epochs=1,
        gamma=0.997,
        policy_kwargs=rich_policy_kwargs(),
        device="cpu",
        verbose=0,
        seed=0,
    )


@pytest.fixture(scope="module")
def anchored_pair(tmp_path_factory):
    """(AnchoredMaskablePPO with a frozen reference attached, ref zip path)."""
    tmp = tmp_path_factory.mktemp("anchor")
    ref = MaskablePPO("MlpPolicy", _make_run_env(), **_small_kwargs())
    ref_path = tmp / "bc_ref"
    ref.save(str(ref_path))
    del ref

    model = AnchoredMaskablePPO(
        "MlpPolicy",
        _make_run_env(),
        anchor_coef=0.5,
        anchor_coef_final=0.02,
        anchor_decay_steps=10_000_000,
        **_small_kwargs(),
    )
    model.set_anchor(str(ref_path) + ".zip")
    return model, str(ref_path) + ".zip"


class TestAnchoredTraining:
    def test_anchor_is_frozen_and_eval(self, anchored_pair):
        model, _ = anchored_pair
        anchor = model.anchor_policy
        assert anchor is not None
        assert not anchor.training
        assert all(not p.requires_grad for p in anchor.parameters())

    def test_trains_two_updates_and_loss_includes_term(self, anchored_pair):
        model, _ = anchored_pair
        anchor_before = [p.clone() for p in model.anchor_policy.parameters()]
        model.learn(total_timesteps=128)  # n_steps=64, 1 env -> 2 updates
        assert model.num_timesteps >= 128

        logged = model.logger.name_to_value
        # The anchor KL term was computed and entered the loss ...
        assert "train/anchor_kl" in logged
        assert np.isfinite(logged["train/anchor_kl"])
        # ... and is genuinely positive: current policy != frozen reference.
        assert logged["train/anchor_kl"] > 0.0
        assert logged["train/anchor_coef"] == pytest.approx(
            anchor_coef_at(128, 0.5, 0.02, 10_000_000)  # coef of the 2nd update
        )
        # The frozen reference must not have moved.
        for before, after in zip(anchor_before, model.anchor_policy.parameters()):
            assert torch.equal(before, after)

    def test_save_load_roundtrip_drops_anchor_keeps_schedule(
        self, anchored_pair, tmp_path
    ):
        model, ref_zip = anchored_pair
        path = tmp_path / "anchored_ckpt"
        model.save(str(path))
        loaded = AnchoredMaskablePPO.load(str(path), device="cpu")
        # The frozen reference is a runtime attachment, never checkpointed.
        assert loaded.anchor_policy is None
        assert loaded.anchor_coef == pytest.approx(model.anchor_coef)
        assert loaded.anchor_coef_final == pytest.approx(model.anchor_coef_final)
        assert loaded.anchor_decay_steps == model.anchor_decay_steps
        # Re-attaching works on a loaded model (the resume path).
        loaded.set_anchor(ref_zip)
        assert loaded.anchor_policy is not None
