"""Turn an episode into per-decision learning signal.

The reward design reuses this project's existing potential-based shaping
rather than inventing a per-choice score, for one reason: the goal is to
LEARN what a good card pick is. Hand-authoring per-choice rewards would bake
in the very judgement the training is supposed to discover -- and the
knowledge-policy work already showed those hand-authored priors do not
reliably beat random on this agent's runs.

Potential-based shaping (Ng, Harada & Russell 1999) is safe here in the
strict sense: F = gamma*Phi(s') - Phi(s) telescopes to a policy-independent
constant per episode, so it changes how fast credit propagates without
changing which policy is optimal. That property is why the same potential
is used across the RL, planner and LLM work in this repo -- results stay
comparable.
"""

from __future__ import annotations

import numpy as np


def shaped_decision_rewards(rollout, gamma: float = 0.99,
                            terminal_weight: float = 1.0) -> np.ndarray:
    """Per-decision rewards: PBRS difference, plus terminal depth at the end.

    Decisions whose reply failed to parse get 0 shaped reward. Their env
    action came from the fallback policy, so crediting the model for it
    would teach it that failing to answer is fine.
    """
    n = len(rollout.decisions)
    if n == 0:
        return np.zeros(0, dtype=np.float64)
    r = np.zeros(n, dtype=np.float64)
    for i, d in enumerate(rollout.decisions):
        r[i] = d.shaped_reward if d.parse_ok else 0.0
    r[-1] += terminal_weight * rollout.terminal_reward()
    return r


def compute_returns(rewards: np.ndarray, gamma: float = 0.99) -> np.ndarray:
    """Discounted return-to-go."""
    out = np.zeros_like(rewards)
    running = 0.0
    for i in range(len(rewards) - 1, -1, -1):
        running = rewards[i] + gamma * running
        out[i] = running
    return out


def normalize_advantages(adv: np.ndarray, eps: float = 1e-8) -> np.ndarray:
    """Zero-mean unit-variance advantages.

    Run depth varies by roughly a full floor between identical
    configurations (measured: the same random baseline scored 8.50, 8.87 and
    10.27 across three runs of the same protocol), so unnormalised
    advantages are dominated by which runs happened to go well.
    """
    if adv.size == 0:
        return adv
    std = adv.std()
    if std < eps:
        return adv - adv.mean()
    return (adv - adv.mean()) / (std + eps)


def batch_statistics(rollouts) -> dict:
    """Aggregates worth logging every iteration.

    Deck size and upgrades are first-class because they diagnose the
    specific failure this whole architecture exists to fix: agents that
    reach elites carrying a near-starter deck.
    """
    if not rollouts:
        return {}
    floors = np.array([r.final_floor for r in rollouts], dtype=float)
    return {
        "episodes": len(rollouts),
        "mean_floors": float(floors.mean()),
        "se_floors": float(floors.std(ddof=1) / np.sqrt(len(floors)))
        if len(floors) > 1 else 0.0,
        "max_floor": float(floors.max()),
        "win_rate": float(np.mean([r.won for r in rollouts])),
        "mean_deck": float(np.mean([r.deck_size for r in rollouts])),
        "mean_upgrades": float(np.mean([r.upgrades for r in rollouts])),
        "mean_decisions": float(np.mean([len(r.decisions) for r in rollouts])),
        "parse_rate": float(np.mean([r.parse_rate for r in rollouts])),
    }
