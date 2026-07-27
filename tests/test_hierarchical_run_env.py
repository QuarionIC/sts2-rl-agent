"""Contract tests for the hierarchical run env.

The whole point of the split is that the run agent never sees a combat
decision and that combats still resolve normally. Both are easy to break
silently (a mask change, a phase-dispatch change), so they are pinned here.
"""

from __future__ import annotations

import numpy as np
import pytest

import sts2_env.events  # noqa: F401  (registry side effects)
from sts2_env.gym_env.hierarchical_run_env import (
    HierarchicalRunEnv,
    RandomCombatController,
)
from sts2_env.run.run_manager import RunManager


def make_env(seed: int = 0, **kw) -> HierarchicalRunEnv:
    return HierarchicalRunEnv(
        character_id="Necrobinder",
        ascension_level=0,
        max_act_count=2,
        combat_controller=RandomCombatController(seed=seed),
        **kw,
    )


def rollout(env, seed: int, rng: np.random.Generator, max_steps: int = 500):
    obs, info = env.reset(seed=seed)
    done = trunc = False
    n = 0
    while not (done or trunc) and n < max_steps:
        mask = np.asarray(env.action_masks(), dtype=bool)
        legal = np.flatnonzero(mask)
        assert legal.size, "no legal action at a run decision point"
        obs, r, done, trunc, info = env.step(int(rng.choice(legal)))
        n += 1
    return info, n


def test_run_agent_is_never_asked_to_act_in_combat():
    """The defining invariant of the architecture."""
    env = make_env(seed=1)
    rng = np.random.default_rng(0)
    for ep in range(5):
        obs, info = env.reset(seed=70_000_000 + ep)
        done = trunc = False
        n = 0
        while not (done or trunc) and n < 500:
            assert env._mgr.phase != RunManager.PHASE_COMBAT, (
                f"run agent asked to act during COMBAT at step {n}"
            )
            mask = np.asarray(env.action_masks(), dtype=bool)
            legal = np.flatnonzero(mask)
            obs, r, done, trunc, info = env.step(int(rng.choice(legal)))
            n += 1


def test_combats_are_actually_played():
    """Delegation must resolve real fights, not skip them."""
    env = make_env(seed=2)
    rng = np.random.default_rng(1)
    total_combats = 0
    for ep in range(5):
        info, _ = rollout(env, 70_100_000 + ep, rng)
        total_combats += info["combats_played"]
        assert info["combats_played"] >= 1, "a full run resolved zero combats"
    assert total_combats >= 10


def test_episode_is_far_shorter_than_flat_env():
    """The credit-assignment win must be real and measurable."""
    env = make_env(seed=3)
    rng = np.random.default_rng(2)
    decisions = []
    for ep in range(6):
        info, n = rollout(env, 70_200_000 + ep, rng)
        decisions.append(info["run_decisions"])
    mean = float(np.mean(decisions))
    # The flat env averages ~250 steps/episode; anything near that means
    # combats are leaking back into the run agent's decision stream.
    assert mean < 80, f"run-agent episode too long ({mean:.1f} decisions)"


def test_deck_can_grow_through_the_run_agent():
    """Card rewards must remain reachable -- this env exists to fix deck
    stagnation, so a plumbing bug here would defeat the purpose."""
    env = make_env(seed=4)
    rng = np.random.default_rng(3)
    grew = False
    for ep in range(8):
        obs, info = env.reset(seed=70_300_000 + ep)
        start = len(env._mgr.run_state.player.deck)
        done = trunc = False
        n = 0
        while not (done or trunc) and n < 500:
            mask = np.asarray(env.action_masks(), dtype=bool)
            obs, r, done, trunc, info = env.step(
                int(rng.choice(np.flatnonzero(mask)))
            )
            n += 1
        if len(env._mgr.run_state.player.deck) > start:
            grew = True
            break
    assert grew, "no random rollout ever added a card to the deck"


def test_info_contract():
    env = make_env(seed=5)
    rng = np.random.default_rng(4)
    info, _ = rollout(env, 70_400_000, rng)
    for key in ("combats_played", "combats_won", "combat_hp_lost",
                "run_decisions", "combat_stuck_count", "player_select_fallbacks"):
        assert key in info, f"missing info key {key!r}"
    assert info["combats_won"] <= info["combats_played"]


def test_stuck_controller_truncates_instead_of_hanging():
    """A cycling combat controller must be surfaced, not spun on forever."""

    class StuckController:
        """Always picks a legal action that cannot end the combat."""

        def act(self, obs, mask):
            legal = np.flatnonzero(np.asarray(mask, dtype=bool))
            # Avoid index 0 (END TURN) where possible so the fight never
            # progresses -- the pathology the cap exists to catch.
            nonzero = legal[legal != 0]
            return int(nonzero[0]) if nonzero.size else int(legal[0])

    env = HierarchicalRunEnv(
        character_id="Necrobinder",
        ascension_level=0,
        max_act_count=2,
        combat_controller=StuckController(),
        max_combat_steps=40,
    )
    rng = np.random.default_rng(5)
    saw_stuck = False
    for ep in range(6):
        obs, info = env.reset(seed=70_500_000 + ep)
        done = trunc = False
        n = 0
        while not (done or trunc) and n < 300:
            mask = np.asarray(env.action_masks(), dtype=bool)
            obs, r, done, trunc, info = env.step(
                int(rng.choice(np.flatnonzero(mask)))
            )
            n += 1
        if info.get("combat_stuck_count", 0) or info.get("combat_stuck"):
            saw_stuck = True
            assert trunc, "stuck combat did not truncate the episode"
            break
    assert saw_stuck, "stuck controller never tripped the combat step cap"


@pytest.mark.parametrize("seed", [11, 12, 13])
def test_deterministic_given_seed(seed):
    """Same seed + same controller seed => same episode."""

    def run_once():
        env = make_env(seed=seed)
        rng = np.random.default_rng(seed)
        info, n = rollout(env, 70_600_000 + seed, rng)
        return info["floor"], info["combats_played"], n

    assert run_once() == run_once()
