"""Tests for the combat MCTS (sts2_env/search/combat_mcts.py) and the ExIt
distillation math (sts2_env/search/distill.py).

Combat construction follows tests/test_exordium_monsters.py conventions:
hand-built CombatState + a real Act-1 monster, exercised directly (no
RunManager)."""

from __future__ import annotations

import copy

import numpy as np
import pytest

from sts2_env.cards.factory import create_card
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId
from sts2_env.core.rng import Rng
from sts2_env.gym_env.action_space import action_to_card_and_target, get_action_mask
from sts2_env.monsters.act1_weak import create_shrinker_beetle
from sts2_env.search.combat_mcts import (
    COMBAT_ACTIONS,
    CombatMCTS,
    MCTSConfig,
    UniformEvaluator,
    apply_combat_action,
    determinize,
    make_bare_obs_builder,
    mcts_action_distribution,
)


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_combat(seed: int = 42, player_hp: int = 60, enemy_hp: int | None = None) -> CombatState:
    deck = [create_card(CardId.STRIKE_NECROBINDER) for _ in range(5)]
    deck += [create_card(CardId.DEFEND_NECROBINDER) for _ in range(5)]
    combat = CombatState(
        player_hp=player_hp,
        player_max_hp=player_hp,
        deck=deck,
        rng_seed=seed,
        character_id="Necrobinder",
    )
    creature, ai = create_shrinker_beetle(Rng(seed))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    if enemy_hp is not None:
        combat.enemies[0].current_hp = enemy_hp
    return combat


def _kill_actions(combat: CombatState) -> list[int]:
    """Legal actions that play a Strike at enemy 0 (lethal at 1 HP)."""
    kills = []
    for a in np.flatnonzero(get_action_mask(combat)):
        hand_idx, target_idx = action_to_card_and_target(int(a))
        if hand_idx is None or target_idx != 0 or hand_idx >= len(combat.hand):
            continue
        if combat.hand[hand_idx].card_id == CardId.STRIKE_NECROBINDER:
            kills.append(int(a))
    return kills


def _cfg(**overrides) -> MCTSConfig:
    base = dict(
        n_simulations=64,
        n_determinizations=4,
        win_value_from_net=False,  # exact +1 win detection for tests
        seed=1,
    )
    base.update(overrides)
    return MCTSConfig(**base)


# ---------------------------------------------------------------------------
# deepcopy safety
# ---------------------------------------------------------------------------

class TestDeepcopySafety:
    def test_clone_diverges_original_untouched(self):
        combat = _make_combat()
        hp0 = combat.player.current_hp
        enemy_hp0 = [e.current_hp for e in combat.enemies]
        hand0 = [c.card_id for c in combat.hand]
        draw0 = [id(c) for c in combat.draw_pile]

        clone = copy.deepcopy(combat)
        clone.end_player_turn()  # full enemy turn on the clone

        assert combat.player.current_hp == hp0
        assert [e.current_hp for e in combat.enemies] == enemy_hp0
        assert [c.card_id for c in combat.hand] == hand0
        assert [id(c) for c in combat.draw_pile] == draw0
        # ... while the clone actually moved
        assert clone.round_number != combat.round_number or clone.hand != combat.hand

    def test_apply_combat_action_on_clone_only(self):
        combat = _make_combat(enemy_hp=1)
        kills = _kill_actions(combat)
        assert kills, "expected at least one Strike targeting the enemy"
        clone = copy.deepcopy(combat)
        apply_combat_action(clone, kills[0])
        assert clone.enemies[0].current_hp <= 0
        assert clone.is_over and clone.player_won
        assert combat.enemies[0].current_hp == 1
        assert not combat.is_over


# ---------------------------------------------------------------------------
# determinization
# ---------------------------------------------------------------------------

class TestDeterminize:
    def test_same_seed_same_world(self):
        combat = _make_combat()
        c1, c2 = copy.deepcopy(combat), copy.deepcopy(combat)
        determinize(c1, 12345)
        determinize(c2, 12345)
        # Identical draw order (instance identity via deepcopy'd uuids) and
        # identical future rng draws.
        assert [c.instance_id for c in c1.draw_pile] == [c.instance_id for c in c2.draw_pile]
        assert [c1.rng.next_int(0, 10**6) for _ in range(5)] == [
            c2.rng.next_int(0, 10**6) for _ in range(5)
        ]

    def test_different_seed_different_world(self):
        combat = _make_combat()
        c1, c3 = copy.deepcopy(combat), copy.deepcopy(combat)
        determinize(c1, 12345)
        determinize(c3, 54321)
        order_differs = [c.instance_id for c in c1.draw_pile] != [c.instance_id for c in c3.draw_pile]
        draws_differ = [c1.rng.next_int(0, 10**6) for _ in range(5)] != [
            c3.rng.next_int(0, 10**6) for _ in range(5)
        ]
        assert order_differs or draws_differ

    def test_original_rng_untouched(self):
        combat = _make_combat()
        counter0 = combat.rng.counter
        clone = copy.deepcopy(combat)
        determinize(clone, 999)
        assert combat.rng.counter == counter0
        assert clone.rng is not combat.rng


# ---------------------------------------------------------------------------
# MCTS core (uniform priors -> behavior driven purely by search)
# ---------------------------------------------------------------------------

class TestCombatMCTS:
    def test_visits_respect_mask_and_sum_to_one(self):
        combat = _make_combat()
        mask = get_action_mask(combat).astype(bool)
        mcts = CombatMCTS(UniformEvaluator(), make_bare_obs_builder(), _cfg())
        visits, _ = mcts.run(combat)
        assert visits.shape == (COMBAT_ACTIONS,)
        assert visits[~mask].sum() == 0.0
        assert visits.sum() == pytest.approx(1.0)
        assert mcts.sim_errors == 0

    def test_lethal_in_one_concentrates_visits(self):
        combat = _make_combat(enemy_hp=1)
        kills = _kill_actions(combat)
        assert kills
        mcts = CombatMCTS(UniformEvaluator(), make_bare_obs_builder(), _cfg())
        visits, root_value = mcts.run(combat)
        assert int(np.argmax(visits)) in kills
        assert visits[kills].sum() > 0.6
        assert root_value > 0.5  # search sees the win

    def test_root_mask_restriction(self):
        combat = _make_combat()
        mask = get_action_mask(combat).astype(bool)
        legal = np.flatnonzero(mask)
        assert len(legal) >= 2
        allowed = np.zeros(COMBAT_ACTIONS, dtype=bool)
        allowed[legal[0]] = True
        allowed[legal[1]] = True
        mcts = CombatMCTS(UniformEvaluator(), make_bare_obs_builder(), _cfg(n_simulations=24))
        visits, _ = mcts.run(combat, root_mask115=allowed)
        assert visits[~allowed].sum() == 0.0
        assert visits.sum() == pytest.approx(1.0)

    def test_forced_move_fast_path(self):
        combat = _make_combat()
        only_end_turn = np.zeros(COMBAT_ACTIONS, dtype=bool)
        only_end_turn[0] = True
        mcts = CombatMCTS(UniformEvaluator(), make_bare_obs_builder(), _cfg())
        visits, _ = mcts.run(combat, root_mask115=only_end_turn)
        assert visits[0] == 1.0
        assert visits.sum() == 1.0

    def test_search_does_not_mutate_root(self):
        combat = _make_combat(enemy_hp=1)
        hp0 = combat.player.current_hp
        hand0 = [c.instance_id for c in combat.hand]
        rng_counter0 = combat.rng.counter
        mcts = CombatMCTS(UniformEvaluator(), make_bare_obs_builder(), _cfg())
        mcts.run(combat)
        assert combat.player.current_hp == hp0
        assert [c.instance_id for c in combat.hand] == hand0
        assert combat.rng.counter == rng_counter0
        assert not combat.is_over


# ---------------------------------------------------------------------------
# SB3 policy evaluator + entry point (torch / sb3-contrib required)
# ---------------------------------------------------------------------------

def _small_policy():
    torch = pytest.importorskip("torch")  # noqa: F841
    pytest.importorskip("sb3_contrib")
    from gymnasium import spaces
    from sb3_contrib.common.maskable.policies import MaskableActorCriticPolicy

    from sts2_env.gym_env.rich_observation import (
        RICH_OBS_HIGH, RICH_OBS_LOW, RICH_OBS_SIZE,
    )
    from sts2_env.gym_env.run_env import TOTAL_ACTIONS
    from sts2_env.train.policy import rich_policy_kwargs

    obs_space = spaces.Box(
        low=RICH_OBS_LOW, high=RICH_OBS_HIGH, shape=(RICH_OBS_SIZE,), dtype=np.float32
    )
    kwargs = rich_policy_kwargs(
        card_embed_dim=16, small_embed_dim=8, hand_hidden=16, torso=(64,)
    )
    return MaskableActorCriticPolicy(
        obs_space, spaces.Discrete(TOTAL_ACTIONS), lambda _: 3.0e-4, **kwargs
    )


class TestSB3PolicyEvaluator:
    def test_masked_probs_and_cache(self):
        from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE
        from sts2_env.search.combat_mcts import SB3PolicyEvaluator

        policy = _small_policy()
        ev = SB3PolicyEvaluator(policy)
        obs = np.zeros(RICH_OBS_SIZE, dtype=np.float32)
        mask = np.zeros(ev.action_dim, dtype=bool)
        mask[[0, 5, 16, 120]] = True

        probs, value = ev.evaluate(obs, mask)
        assert probs.shape == (ev.action_dim,)
        assert probs[~mask].sum() == 0.0
        assert probs[mask].sum() == pytest.approx(1.0)
        assert np.isfinite(value)
        assert (ev.cache_hits, ev.cache_misses) == (0, 1)

        probs2, value2 = ev.evaluate(obs, mask)
        assert ev.cache_hits == 1
        assert np.array_equal(probs, probs2) and value == value2

    def test_mcts_action_distribution_with_policy(self):
        policy = _small_policy()
        combat = _make_combat(enemy_hp=1)
        mask = get_action_mask(combat).astype(bool)
        visits, root_value = mcts_action_distribution(
            combat, policy, n_sims=16,
            config=MCTSConfig(n_determinizations=4, seed=2),
        )
        assert visits.shape == (COMBAT_ACTIONS,)
        assert visits[~mask].sum() == 0.0
        assert visits.sum() == pytest.approx(1.0)
        assert np.isfinite(root_value)


# ---------------------------------------------------------------------------
# Distillation loss math
# ---------------------------------------------------------------------------

class TestDistillLosses:
    def test_synthetic_batch_matches_manual_math(self):
        torch = pytest.importorskip("torch")
        from sts2_env.search.distill import distill_losses

        logits = torch.tensor([
            [1.0, 0.5, -1.0, float("-inf")],
            [0.0, 0.0, 0.0, 0.0],
        ])
        log_probs = torch.log_softmax(logits, dim=-1)
        targets = torch.tensor([
            [0.75, 0.25, 0.0, 0.0],   # zero target where logp = -inf
            [0.0, 0.0, 1.0, 0.0],
        ])
        values = torch.tensor([0.5, -0.5])
        target_values = torch.tensor([1.0, -1.0])

        policy_loss, value_loss = distill_losses(log_probs, values, targets, target_values)

        expected_ce = -(
            0.75 * log_probs[0, 0] + 0.25 * log_probs[0, 1] + 1.0 * log_probs[1, 2]
        ) / 2.0
        expected_mse = ((0.5 - 1.0) ** 2 + (-0.5 - -1.0) ** 2) / 2.0
        assert policy_loss.item() == pytest.approx(expected_ce.item(), rel=1e-6)
        assert value_loss.item() == pytest.approx(expected_mse, rel=1e-6)
        assert torch.isfinite(policy_loss)  # the 0 * -inf guard held

    def test_distill_reduces_ce_on_synthetic_batch(self):
        torch = pytest.importorskip("torch")  # noqa: F841
        from types import SimpleNamespace

        from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE
        from sts2_env.search.distill import distill

        policy = _small_policy()
        model = SimpleNamespace(policy=policy)
        rng = np.random.default_rng(0)
        n = 32
        obs = rng.random((n, RICH_OBS_SIZE), dtype=np.float32)
        masks = np.zeros((n, int(policy.action_space.n)), dtype=bool)
        visits = np.zeros((n, COMBAT_ACTIONS), dtype=np.float32)
        for i in range(n):
            legal = rng.choice(COMBAT_ACTIONS, size=5, replace=False)
            masks[i, legal] = True
            w = rng.random(5).astype(np.float32)
            visits[i, legal] = w / w.sum()
        values = rng.standard_normal(n).astype(np.float32)

        stats = distill(
            model, obs, masks, visits, values,
            epochs=3, lr=1.0e-3, batch_size=n, verbose=False,
        )
        assert stats.policy_loss_last < stats.policy_loss_first
        assert np.isfinite(stats.value_loss_last)
