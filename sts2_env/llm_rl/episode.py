"""Roll out one run with the LLM deciding, recording what RL needs.

An episode here is a full STS2 run. The LLM is asked at every out-of-combat
decision; combat is consumed by the deterministic planner and never reaches
the model. Each ask produces a :class:`DecisionRecord` holding the prompt,
the legal options, which one was chosen, and the state before/after -- which
is everything the trainer needs to recompute logprobs and advantages.

Two design points worth stating, because both were learned the hard way in
this project:

* **Actions are option INDICES over the simulator's own legal list.** The
  policy cannot produce an illegal move, only a differently-ranked legal
  one. That is why no output-constraining machinery is needed.
* **Unparseable replies are recorded, not silently coerced.** A run where
  the model failed to answer half the time is not a run that scored what it
  scored; ``parse_ok`` marks those so the trainer can drop or down-weight
  them instead of learning from the fallback policy's choices.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

import numpy as np


@dataclass
class DecisionRecord:
    """One out-of-combat decision the LLM was asked to make."""

    phase: str
    prompt: str
    #: Human-readable option list, index-aligned with the env's legal actions.
    options: list[str]
    #: Index the model chose, or None when its reply could not be parsed.
    chosen: int | None
    #: Raw model reply, kept for offline inspection of failures.
    reply: str
    #: Env action actually taken (the fallback's, when parsing failed).
    env_action: int
    #: Potential Phi(s) before and after, for potential-based credit.
    phi_before: float
    phi_after: float
    floor: float
    parse_ok: bool = True

    @property
    def shaped_reward(self) -> float:
        """Potential difference at this decision.

        Using the SAME potential the rest of the project uses keeps this
        comparable to every prior result and, being potential-based, keeps
        it policy-invariant: it changes how fast credit propagates, never
        which policy is optimal.
        """
        return self.phi_after - self.phi_before


@dataclass
class EpisodeRollout:
    """A full run: the decisions taken and how the run ended."""

    decisions: list[DecisionRecord] = field(default_factory=list)
    final_floor: int = 0
    final_act: int = 0
    won: bool = False
    truncated: bool = False
    deck_size: int = 0
    upgrades: int = 0

    @property
    def parse_rate(self) -> float:
        if not self.decisions:
            return 0.0
        return sum(d.parse_ok for d in self.decisions) / len(self.decisions)

    def terminal_reward(self, win_bonus: float = 10.0) -> float:
        """Run depth is the objective; a win is worth a large bonus.

        Depth rather than win-rate because at current strength wins are far
        too rare to give gradient -- every measured configuration in this
        project sits at 0% wins over acts 1-2, so a win-only reward would be
        all zeros.
        """
        return float(self.final_floor) + (win_bonus if self.won else 0.0)


def collect_episode(
    env: Any,
    ask_fn: Any,
    seed: int,
    max_steps: int = 2000,
) -> EpisodeRollout:
    """Play one run, asking ``ask_fn`` at each out-of-combat decision.

    ``ask_fn(prompt, options) -> (chosen_index_or_None, raw_reply)`` is the
    only coupling to the model, so the same collector serves the local GGUF
    runner, an HF model under training, or a scripted policy used as a
    control arm.
    """
    from sts2_env.llm.state_text import render_decision
    from sts2_env.run.run_manager import RunManager

    rollout = EpisodeRollout()
    obs, info = env.reset(seed=seed)
    mgr = env._mgr
    cfg = env.reward_config
    done = trunc = False
    steps = 0

    while not (done or trunc) and steps < max_steps:
        decision = render_decision(mgr)
        mask = np.asarray(env.action_masks(), dtype=bool)
        legal = np.flatnonzero(mask)
        if not legal.size:
            break

        if decision is None:
            # A phase the model is not asked about (pending choices etc.).
            action = int(legal[0])
            obs, r, done, trunc, info = env.step(action)
            steps += 1
            continue

        # Read the env's damage ratchet, not a fresh 0: Phi's HP term is now
        # cumulative damage taken, so passing 0.0 would price every state as
        # undamaged and silently drop the HP signal from these rollouts.
        phi_before = float(
            cfg.potential(mgr, 0.0, getattr(env, "_damage_taken", 0.0)))
        chosen, reply = ask_fn(decision.prompt, decision.options)
        parse_ok = chosen is not None

        action = None
        if parse_ok:
            action = _resolve(env, mgr, decision.options, chosen, mask)
        if action is None:
            parse_ok = False
            action = int(legal[0])

        obs, r, done, trunc, info = env.step(int(action))
        steps += 1
        phi_after = 0.0 if (done or trunc) else float(
            cfg.potential(mgr, 0.0, getattr(env, "_damage_taken", 0.0)))

        rollout.decisions.append(DecisionRecord(
            phase=decision.phase,
            prompt=decision.prompt,
            options=[str(o) for o in decision.options],
            chosen=chosen,
            reply=reply,
            env_action=int(action),
            phi_before=phi_before,
            phi_after=phi_after,
            floor=float(info.get("floor", 0)),
            parse_ok=parse_ok,
        ))

    rollout.final_floor = int(info.get("floor", 0))
    rollout.final_act = int(info.get("act", 0))
    rollout.won = bool(info.get("won", False))
    rollout.truncated = bool(trunc)
    rs = mgr.run_state if mgr is not None else None
    if rs is not None:
        rollout.deck_size = len(rs.player.deck)
        rollout.upgrades = sum(1 for c in rs.player.deck if c.upgraded)
    return rollout


def _resolve(env: Any, mgr: Any, options: list, chosen: int,
             mask: np.ndarray) -> int | None:
    """Option index -> env action index, verified legal.

    Resolution goes through the phase's action slice and is checked against
    the mask, so a model choice can never become an illegal env action.
    """
    from sts2_env.gym_env.run_env import _LAYOUT
    from sts2_env.run.run_manager import RunManager

    starts = {
        RunManager.PHASE_MAP_CHOICE: _LAYOUT.map_start,
        RunManager.PHASE_CARD_REWARD: _LAYOUT.card_reward_start,
        RunManager.PHASE_SHOP: _LAYOUT.shop_start,
        RunManager.PHASE_REST_SITE: _LAYOUT.rest_start,
        RunManager.PHASE_EVENT: _LAYOUT.event_start,
        RunManager.PHASE_TREASURE: _LAYOUT.treasure_start,
        RunManager.PHASE_BOSS_RELIC: _LAYOUT.boss_relic_start,
    }
    base = starts.get(mgr.phase)
    if base is None or not (0 <= chosen < len(options)):
        return None
    idx = base + chosen
    if 0 <= idx < mask.size and mask[idx]:
        return int(idx)
    return None
