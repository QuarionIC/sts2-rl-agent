"""Tests for the per-slot RichFeaturesExtractor (docs/TRAINING_REVAMP_SPEC)."""

from __future__ import annotations

import numpy as np
import pytest

torch = pytest.importorskip("torch")

from gymnasium import spaces  # noqa: E402

from sts2_env.gym_env import rich_observation as ro  # noqa: E402
from sts2_env.train.policy import RichFeaturesExtractor, rich_policy_kwargs  # noqa: E402


def _make_space() -> spaces.Box:
    return spaces.Box(
        low=ro.RICH_OBS_LOW, high=ro.RICH_OBS_HIGH,
        shape=(ro.RICH_OBS_SIZE,), dtype=np.float32,
    )


class TestRichFeaturesExtractor:
    def test_features_dim_arithmetic(self):
        fe = RichFeaturesExtractor(_make_space())
        flat_size = ro.DECK_BAG_OFF - ro.PILE_SIZES_OFF
        expected = (
            ro.NUM_HAND_SLOTS * 96   # per-slot concat
            + 96                     # hand mean-pool global context
            + ro.NUM_PILES * 96      # pile bag projections
            + 96                     # deck bag projection
            + 16 + 16                # potion + boss embeddings
            + flat_size
            + ro.ARCH_SCALARS_SIZE
        )
        assert fe.features_dim == expected

    def test_forward_shapes_and_finite(self):
        fe = RichFeaturesExtractor(_make_space())
        x = torch.zeros(3, ro.RICH_OBS_SIZE)
        y = fe(x)
        assert y.shape == (3, fe.features_dim)
        assert torch.isfinite(y).all()

    def test_per_slot_concat_is_slot_sensitive(self):
        """Swapping two hand slots must CHANGE the features (the old
        mean-pool was permutation-invariant, hiding slot identity from the
        per-slot action head)."""
        fe = RichFeaturesExtractor(_make_space())
        a = torch.zeros(1, ro.RICH_OBS_SIZE)
        a[0, ro.IDS_HAND_OFF + 0] = 5.0   # card id 5 in slot 0
        a[0, ro.IDS_HAND_OFF + 1] = 9.0   # card id 9 in slot 1
        b = a.clone()
        b[0, ro.IDS_HAND_OFF + 0] = 9.0   # swapped
        b[0, ro.IDS_HAND_OFF + 1] = 5.0
        ya, yb = fe(a), fe(b)
        n_slots = ro.NUM_HAND_SLOTS * 96
        assert not torch.allclose(ya[:, :n_slots], yb[:, :n_slots])
        # ... while the mean-pooled global context stays permutation-invariant
        pooled_a = ya[:, n_slots:n_slots + 96]
        pooled_b = yb[:, n_slots:n_slots + 96]
        assert torch.allclose(pooled_a, pooled_b, atol=1e-6)

    def test_deck_bag_projection_uses_shared_embedding(self):
        fe = RichFeaturesExtractor(_make_space())
        x = torch.zeros(1, ro.RICH_OBS_SIZE)
        ci = 7
        x[0, ro.DECK_BAG_OFF + ci] = 2.0 / ro.BAG_COUNT_SCALE
        y = fe(x)
        base = ro.NUM_HAND_SLOTS * 96 + 96 + ro.NUM_PILES * 96
        deck_feats = y[0, base:base + 96]
        expected = (2.0 / ro.BAG_COUNT_SCALE) * fe.card_embedding.weight[1 + ci]
        assert torch.allclose(deck_feats, expected, atol=1e-6)

    def test_archetype_scalars_pass_through(self):
        fe = RichFeaturesExtractor(_make_space())
        x = torch.zeros(1, ro.RICH_OBS_SIZE)
        vals = torch.arange(1, ro.ARCH_SCALARS_SIZE + 1, dtype=torch.float32) / 10.0
        x[0, ro.ARCH_SCALARS_OFF:ro.ARCH_SCALARS_OFF + ro.ARCH_SCALARS_SIZE] = vals
        y = fe(x)
        assert torch.allclose(y[0, -ro.ARCH_SCALARS_SIZE:], vals)

    def test_policy_kwargs_defaults(self):
        kw = rich_policy_kwargs()
        assert kw["features_extractor_kwargs"]["card_embed_dim"] == 96
        assert kw["features_extractor_kwargs"]["hand_hidden"] == 96
        assert kw["net_arch"] == dict(pi=[1024, 1024, 512], vf=[1024, 1024, 512])
