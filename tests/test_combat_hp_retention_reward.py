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
    cfg = _cfg()
    assert cfg.worst_win_beats_best_loss()
    worst_win = cfg.combat_terminal_reward(True, 0.0, hp_cost=1.0)
    best_loss = cfg.combat_terminal_reward(False, 1.0, hp_cost=1.0)
    assert worst_win > best_loss
    # And with real headroom, not by a hair.
    assert worst_win - best_loss > 10.0


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


def test_the_term_does_not_invite_gambling_the_fight_to_save_hp():
    """The constraint that actually sizes the weight.

    "Wins beat losses" is satisfied with enormous slack and is not the
    binding limit. What binds is how much WIN PROBABILITY the term invites
    the agent to trade for HP. A 90%-win line costing 40 HP must stay ahead
    of a 70%-win line costing 5 -- otherwise "preserve HP" quietly becomes
    "avoid risk", which loses runs.
    """
    cfg = _cfg()
    safe_but_flaky = _expected_value(cfg, 0.70, 5 / 70)
    reliable_but_costly = _expected_value(cfg, 0.90, 40 / 70)
    assert reliable_but_costly > safe_but_flaky

    # ...and the same comparison inverts once the weight is far too large,
    # which is exactly the failure this sizing avoids.
    reckless = _cfg(w_combat_hp_retained=8.0)
    assert _expected_value(reckless, 0.70, 5 / 70) > \
        _expected_value(reckless, 0.90, 40 / 70)


def test_hp_only_wins_when_it_is_nearly_free():
    # Between two lines that win equally often, the cheaper one must win.
    cfg = _cfg()
    assert _expected_value(cfg, 0.85, 0.10) > _expected_value(cfg, 0.85, 0.60)
