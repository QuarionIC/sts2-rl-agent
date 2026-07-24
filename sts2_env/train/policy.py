"""Custom SB3 feature extractor for the rich observation (v1).

Architecture (docs/TRAINING_REDESIGN.md "Policy network"):

* A learned card-embedding table ``(NUM_CARD_IDS + 1) x card_embed_dim``
  (index 0 = empty) applied to the 10 hand-slot ID dims. Each hand slot's
  embedding is concatenated with its 12 scalar features and pushed through a
  small shared per-slot MLP, then mean-pooled.
* The 3 pile bag-of-cards count vectors are projected through the SAME
  embedding table (``bag @ E[1:]``), sharing card representations between
  hand and piles.
* Potion-slot ids and the boss id get their own small embedding tables
  (separate id spaces -- reusing the card table would alias card ids with
  potion/boss ids).
* Everything is concatenated with the remaining flat segments; the
  [1024, 1024, 512] torso is supplied via ``net_arch`` in ``policy_kwargs``
  (see :func:`rich_policy_kwargs`).

Weight transfer between curriculum stages (combat env Discrete(115) ->
run env Discrete(157)) is handled by :func:`transfer_weights`.
"""

from __future__ import annotations

import torch
import torch.nn as nn
from gymnasium import spaces
from stable_baselines3.common.torch_layers import BaseFeaturesExtractor

from sts2_env.gym_env.rich_observation import (
    BOSS_VOCAB_SIZE,
    HAND_FEATURES,
    HAND_SCALARS_OFF,
    HAND_SCALARS_SIZE,
    IDS_BOSS_OFF,
    IDS_HAND_OFF,
    IDS_POTION_OFF,
    NUM_CARD_IDS,
    NUM_HAND_SLOTS,
    NUM_PILES,
    NUM_POTION_SLOTS,
    PILE_BAGS_OFF,
    PILE_BAGS_SIZE,
    PILE_SIZES_OFF,
    POTION_VOCAB_SIZE,
    RICH_OBS_SIZE,
)


class RichFeaturesExtractor(BaseFeaturesExtractor):
    """Slices the rich flat obs, embeds the ID dims, passes the rest through."""

    def __init__(
        self,
        observation_space: spaces.Box,
        card_embed_dim: int = 64,
        small_embed_dim: int = 16,
        hand_hidden: int = 64,
    ):
        obs_size = int(observation_space.shape[0])
        assert obs_size == RICH_OBS_SIZE, (
            f"RichFeaturesExtractor expects obs size {RICH_OBS_SIZE}, got {obs_size}"
        )
        # Flat passthrough: everything from PILE_SIZES_OFF to the end
        # (pile sizes, player, powers, necro, enemies, relics, flags, run).
        flat_size = RICH_OBS_SIZE - PILE_SIZES_OFF
        features_dim = (
            hand_hidden                    # pooled hand
            + NUM_PILES * card_embed_dim   # pile bag projections
            + small_embed_dim              # pooled potion embeddings
            + small_embed_dim              # boss embedding
            + flat_size
        )
        super().__init__(observation_space, features_dim)

        self.card_embed_dim = card_embed_dim
        self.card_embedding = nn.Embedding(NUM_CARD_IDS + 1, card_embed_dim, padding_idx=0)
        self.potion_embedding = nn.Embedding(POTION_VOCAB_SIZE, small_embed_dim, padding_idx=0)
        self.boss_embedding = nn.Embedding(BOSS_VOCAB_SIZE, small_embed_dim)
        self.hand_mlp = nn.Sequential(
            nn.Linear(card_embed_dim + HAND_FEATURES, hand_hidden),
            nn.ReLU(),
        )

    def forward(self, observations: torch.Tensor) -> torch.Tensor:
        obs = observations
        batch = obs.shape[0]

        # --- hand: embeddings + scalar features -> shared slot MLP -> mean ---
        hand_ids = obs[:, IDS_HAND_OFF:IDS_HAND_OFF + NUM_HAND_SLOTS].long().clamp_(0, NUM_CARD_IDS)
        hand_emb = self.card_embedding(hand_ids)  # (B, 10, E)
        hand_scalars = obs[:, HAND_SCALARS_OFF:HAND_SCALARS_OFF + HAND_SCALARS_SIZE]
        hand_scalars = hand_scalars.view(batch, NUM_HAND_SLOTS, HAND_FEATURES)
        hand_x = self.hand_mlp(torch.cat([hand_emb, hand_scalars], dim=-1))  # (B, 10, H)
        hand_pooled = hand_x.mean(dim=1)  # (B, H)

        # --- pile bags projected through the SAME card embedding table ---
        bags = obs[:, PILE_BAGS_OFF:PILE_BAGS_OFF + PILE_BAGS_SIZE]
        bags = bags.view(batch, NUM_PILES, NUM_CARD_IDS)
        bag_feats = torch.matmul(bags, self.card_embedding.weight[1:])  # (B, 3, E)
        bag_feats = bag_feats.reshape(batch, NUM_PILES * self.card_embed_dim)

        # --- potion / boss ids ---
        potion_ids = obs[:, IDS_POTION_OFF:IDS_POTION_OFF + NUM_POTION_SLOTS].long()
        potion_ids = potion_ids.clamp_(0, POTION_VOCAB_SIZE - 1)
        potion_pooled = self.potion_embedding(potion_ids).mean(dim=1)  # (B, S)
        boss_ids = obs[:, IDS_BOSS_OFF].long().clamp_(0, BOSS_VOCAB_SIZE - 1)
        boss_emb = self.boss_embedding(boss_ids)  # (B, S)

        # --- flat passthrough ---
        flat = obs[:, PILE_SIZES_OFF:]

        return torch.cat([hand_pooled, bag_feats, potion_pooled, boss_emb, flat], dim=1)


def rich_policy_kwargs(
    card_embed_dim: int = 64,
    small_embed_dim: int = 16,
    hand_hidden: int = 64,
    torso: tuple[int, ...] = (1024, 1024, 512),
) -> dict:
    """policy_kwargs for MaskablePPO("MlpPolicy", ...) with the rich extractor."""
    return dict(
        features_extractor_class=RichFeaturesExtractor,
        features_extractor_kwargs=dict(
            card_embed_dim=card_embed_dim,
            small_embed_dim=small_embed_dim,
            hand_hidden=hand_hidden,
        ),
        net_arch=dict(pi=list(torso), vf=list(torso)),
    )


def transfer_weights(src_model, dst_model, verbose: bool = True) -> list[str]:
    """Warm-start ``dst_model`` from ``src_model`` (SB3 MaskablePPO models).

    Copies every policy parameter whose name and shape match. For the action
    head (``action_net``), where the source (combat, 115 actions) is a prefix
    of the destination (run, 157 actions) by construction, the leading rows
    are copied. Returns the list of transferred parameter names.
    """
    src_state = src_model.policy.state_dict()
    dst_state = dst_model.policy.state_dict()
    transferred: list[str] = []
    for name, dst_param in dst_state.items():
        src_param = src_state.get(name)
        if src_param is None:
            continue
        if src_param.shape == dst_param.shape:
            dst_state[name] = src_param.clone()
            transferred.append(name)
        elif (
            name.startswith("action_net")
            and src_param.shape[1:] == dst_param.shape[1:]
            and src_param.shape[0] <= dst_param.shape[0]
        ):
            new_param = dst_param.clone()
            new_param[: src_param.shape[0]] = src_param
            dst_state[name] = new_param
            transferred.append(f"{name} (prefix {src_param.shape[0]}/{dst_param.shape[0]})")
    dst_model.policy.load_state_dict(dst_state)
    if verbose:
        print(f"transfer_weights: {len(transferred)} tensors transferred")
    return transferred
