"""A combat win must be charged for the HP it cost.

The combat env's terminal reward was flat on a win, so a win at full HP and a
win at 1 HP paid identically and the agent had no reason to prefer the cheap
line. That is defensible for a single fight and wrong for a run: HP only
returns at rest sites, so a win bought at 30 HP loses the run two floors on.

Measured 2026-07-31 with scripts/rank_run_agents.py, 150 shared seeds per arm,
A0, max_act_count=1: the SAME run agent's decks converted to a 34.0% win rate
under the deterministic planner (which optimises win-then-HP) versus 8.7%
under the RL combat agent (two-proportion z=5.36, p=8.5e-08).

The safety property these tests exist to defend: charging for HP must never
make losing look better than winning.
"""

from __future__ import annotations

import pytest

from sts2_env.gym_env.reward_config import RewardConfig


def _cfg(**kw) -> RewardConfig:
    return RewardConfig(**kw)


def test_a_cheap_win_pays_more_than_an_expensive_one():
    cfg = _cfg()
    clean = cfg.combat_terminal_reward(True, 1.0, hp_cost=0.0)
    bloody = cfg.combat_terminal_reward(True, 1.0, hp_cost=0.5)
    assert clean > bloody, "HP loss must cost something on a win"
    assert clean - bloody == pytest.approx(cfg.w_combat_hp_retained * 0.5)


def test_the_worst_win_still_beats_the_best_loss():
    """The one invariant that must never break, whatever the weight.

    The headroom is no longer generous. At w=2.0 the worst win beat the best
    loss by 14 points; at the shipped 14.0 it is +2.00 vs -6.00, a margin of
    2. Still strictly ordered -- dying is never preferable to winning -- but
    close enough that any further increase needs this checked first, which is
    what worst_win_beats_best_loss() is for.
    """
    cfg = _cfg()
    assert cfg.worst_win_beats_best_loss()
    worst_win = cfg.combat_terminal_reward(True, 0.0, hp_cost=1.0)
    best_loss = cfg.combat_terminal_reward(False, 1.0, hp_cost=1.0)
    assert worst_win > best_loss
    # Pin the actual margin so a future weight change has to confront it
    # rather than quietly consume it.
    assert worst_win - best_loss == pytest.approx(2.0)


def test_losses_are_not_charged_for_hp():
    # A loss means 0 HP, so an HP term there is a near-constant penalty
    # scaled by the starting HP the sampler handed out -- it would charge the
    # combat agent for the run agent's earlier decisions.
    cfg = _cfg()
    for hp_cost in (0.0, 0.5, 1.0):
        assert cfg.combat_terminal_reward(False, 0.4, hp_cost) == \
            cfg.terminal_reward(False, 0.4)


def test_a_loss_is_still_graded_by_damage_dealt():
    cfg = _cfg()
    close = cfg.combat_terminal_reward(False, 0.9, hp_cost=1.0)
    hopeless = cfg.combat_terminal_reward(False, 0.1, hp_cost=1.0)
    assert close > hopeless


def test_healing_earns_but_is_capped():
    cfg = _cfg()
    flat = cfg.combat_terminal_reward(True, 1.0, hp_cost=0.0)
    healed = cfg.combat_terminal_reward(True, 1.0, hp_cost=-0.10)
    assert healed > flat, "a net heal is real value for the run"
    # ...but a stall loop cannot farm it without bound.
    huge_heal = cfg.combat_terminal_reward(True, 1.0, hp_cost=-99.0)
    capped = cfg.combat_terminal_reward(True, 1.0, cfg.combat_hp_cost_floor)
    assert huge_heal == capped


def test_a_catastrophic_fight_is_capped_too():
    cfg = _cfg()
    assert cfg.combat_terminal_reward(True, 1.0, hp_cost=99.0) == \
        cfg.combat_terminal_reward(True, 1.0, cfg.combat_hp_cost_ceiling)


def test_the_term_can_be_ablated_to_the_old_behaviour():
    off = _cfg(w_combat_hp_retained=0.0)
    for hp_cost in (0.0, 0.5, 1.0):
        assert off.combat_terminal_reward(True, 1.0, hp_cost) == \
            off.terminal_reward(True, 1.0)


def test_a_missing_hp_cost_falls_back_to_the_flat_win():
    cfg = _cfg()
    assert cfg.combat_terminal_reward(True, 1.0, None) == cfg.terminal_reward(True, 1.0)


def test_the_run_env_payout_is_untouched():
    # rich_run_env calls terminal_reward POSITIONALLY and its win is flat on
    # purpose: under PBRS the shaping telescopes, so a clean win and a bloody
    # one already total the same. If the HP term leaked into terminal_reward
    # it would double-count against the potential's own hp_damage term.
    cfg = _cfg()
    assert cfg.terminal_reward(True, 1.0) == cfg.win
    assert cfg.terminal_reward(True, 1.0, 2) == cfg.win + 2 * cfg.elite_bonus


def test_the_safety_invariant_fails_loudly_if_the_weight_is_raised_too_far():
    # A future edit that makes HP dominate must trip worst_win_beats_best_loss
    # rather than quietly teach the agent that dying is fine.
    reckless = _cfg(w_combat_hp_retained=20.0)
    assert not reckless.worst_win_beats_best_loss()


def _expected_value(cfg, win_prob, hp_cost):
    """EV of a line that wins with `win_prob` at `hp_cost`, else dies flat."""
    return (win_prob * cfg.combat_terminal_reward(True, 1.0, hp_cost)
            + (1 - win_prob) * cfg.combat_terminal_reward(False, 0.8))


def test_a_low_weight_does_not_invite_gambling_the_fight_to_save_hp():
    """The mechanism, pinned at a weight where it still holds.

    What binds this term is not "wins beat losses" but how much WIN
    PROBABILITY it invites trading for HP: giving up dp of win chance to save
    dc of HP pays iff ``w * dc > dp * (win - loss)``. At w=2.0 a 90%-win line
    costing 40 HP still outranks a 70%-win line costing 5.
    """
    gentle = _cfg(w_combat_hp_retained=2.0)
    assert _expected_value(gentle, 0.90, 40 / 66) > \
        _expected_value(gentle, 0.70, 5 / 66)


def test_the_shipped_weight_accepts_the_gamble_tradeoff():
    """The SHIPPED default inverts that preference, deliberately.

    Raised 2.0 -> 14.0 on 2026-07-31 by explicit direction, for a stronger
    signal: at 2.0 the term was worth a measured +0.95 HP per win (95% CI
    [+0.36, +1.53]) and had gone flat across 3M steps.

    The cost is asserted here rather than left to be discovered. At 14.0 a
    70%-win line costing 5 HP outranks a 90%-win line costing 40 -- the agent
    will accept a materially worse chance of winning in exchange for HP. If
    the live win rate falls, this is the explanation, and it was known in
    advance.

    Asserting the inversion rather than deploring it: a test that merely
    failed here would get "fixed" by being weakened. Lower the weight back
    and this fails, pointing at the sibling above.
    """
    cfg = _cfg()
    assert cfg.w_combat_hp_retained == 14.0
    assert _expected_value(cfg, 0.70, 5 / 66) > _expected_value(cfg, 0.90, 40 / 66)
    # The hard floor still holds: no HP saving makes dying acceptable.
    assert cfg.worst_win_beats_best_loss()


def test_hp_only_wins_when_it_is_nearly_free():
    # Between two lines that win equally often, the cheaper one must win.
    cfg = _cfg()
    assert _expected_value(cfg, 0.85, 0.10) > _expected_value(cfg, 0.85, 0.60)


class TestDamageShapingIsPolicyInvariant:
    """PBRS must change WHEN the HP signal arrives, never the objective.

    The terminal charge alone did not move behaviour: over 1.5M-step arms
    warm-started from one checkpoint, HP retained among wins went 0.500 (no
    term) -> 0.519 at w=2.0 and 0.508 at w=4.0 -- under a fifth of the 7.1 HP
    gap to the planner, and non-monotonic in the weight. A combat is ~30
    actions and the whole charge landed on the last one, so the fix is credit
    assignment, not a bigger number.

    Shaping is only SAFE to add because it telescopes: Phi(s0) = 0 (no damage
    yet) and Phi := 0 at terminal, so the per-episode sum is exactly zero and
    the optimal policy is untouched. That property is load-bearing and it is
    easy to break by accident -- assigning rather than accumulating the
    terminal reward silently drops the final cancelling term, turning the
    shaping into a second, double-counted damage penalty. These tests exist
    because that is precisely the bug that was written here first.
    """

    @staticmethod
    def _play(env, seed):
        import numpy as np

        env.reset(seed=seed)
        done = tr = False
        total = 0.0
        steps = 0
        while not (done or tr) and steps < 400:
            legal = np.flatnonzero(env.action_masks())
            action = int(legal[-1]) if legal.size > 1 else int(legal[0])
            _, reward, done, tr, _ = env.step(action)
            total += reward
            steps += 1
        return total

    def _env(self):
        import sts2_env.events  # noqa: F401
        from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

        return RichSTS2CombatEnv(character_id="Necrobinder", ascension_level=0)

    @pytest.mark.parametrize("seed", [0, 2, 4, 5, 9, 11])
    def test_episode_return_is_identical_with_and_without_shaping(self, seed):
        env = self._env()
        env.set_shaping_scale(1.0)
        shaped = self._play(env, seed)
        env.set_shaping_scale(0.0)
        sparse = self._play(env, seed)
        assert shaped == pytest.approx(sparse, abs=1e-6), (
            "shaping changed the episode return, so it is no longer a "
            "potential: the optimal policy has moved and every number "
            "measured before it is no longer comparable"
        )

    def test_shaping_actually_delivers_mid_episode_signal(self):
        # Invariance is worthless if the term does nothing; the whole point
        # is a non-zero reward on the steps where damage is taken.
        import numpy as np

        env = self._env()
        env.set_shaping_scale(1.0)
        env.reset(seed=0)
        done = tr = False
        mid_episode_nonzero = 0
        steps = 0
        while not (done or tr) and steps < 400:
            legal = np.flatnonzero(env.action_masks())
            action = int(legal[-1]) if legal.size > 1 else int(legal[0])
            _, reward, done, tr, _ = env.step(action)
            steps += 1
            if not (done or tr) and reward != 0.0:
                mid_episode_nonzero += 1
        assert mid_episode_nonzero > 0, (
            "no mid-episode reward: the shaping is inert and the HP charge "
            "still arrives only at the end"
        )


def test_the_shaping_weight_matches_the_terminal_charge():
    """They must agree, or the shaping only pre-pays a fraction of the cost.

    The shaping exists to deliver the terminal HP charge on the step the
    damage is taken. If w_combat_hp_shaping is smaller than
    w_combat_hp_retained, a point of damage produces only a fraction of the
    feedback it eventually costs -- which leaves most of the credit-assignment
    problem the shaping was added to solve.

    Correctness does not depend on this: PBRS telescopes to zero whatever the
    weight, so the optimum is identical either way (pinned by
    TestDamageShapingIsPolicyInvariant). Only the learning rate changes. This
    test exists because the two were left mismatched once, 2.0 against 14.0,
    and nothing failed -- the run would simply have learned more slowly for no
    stated reason.
    """
    cfg = _cfg()
    assert cfg.w_combat_hp_shaping == cfg.w_combat_hp_retained
