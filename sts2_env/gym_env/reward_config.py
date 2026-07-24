"""Reward configuration for the rich envs (docs/TRAINING_REVAMP_SPEC.json).

Terminal rewards (win / death / truncation) are fixed and never annealed.
All shaping terms are multiplied by a single ``shaping_scale`` in [0, 1].
It is a constant knob (1.0 during training, 0.0 for pure-sparse eval); the
old trainer-side win-rate anneal has been removed.
"""

from __future__ import annotations

from dataclasses import dataclass


@dataclass
class RewardConfig:
    """Reward terms consumed by RichSTS2RunEnv / RichSTS2CombatEnv.

    Attributes
    ----------
    win : terminal reward for winning the episode (never annealed).
    death : terminal reward for dying (never annealed).
    truncation : terminal reward when the episode is truncated (step-limit
        timeout). 0.0 so a slow-but-alive run is NOT scored as a death;
        the value function bootstraps instead.
    act_completion : shaping bonus per act boss killed (x shaping_scale).
    floor : shaping bonus per floor climbed (x shaping_scale).
    combat_hp_retention : shaping bonus per combat win, multiplied by
        ``hp_end / hp_start`` for that combat (x shaping_scale).
    shaping_scale : global multiplier in [0, 1] applied to every shaping
        term. 1.0 = full shaping, 0.0 = pure sparse reward. Constant
        during training (no anneal).
    """

    win: float = 1.0
    death: float = -1.0
    truncation: float = 0.0
    act_completion: float = 0.25
    floor: float = 0.004
    combat_hp_retention: float = 0.05
    shaping_scale: float = 1.0

    # ------------------------------------------------------------------

    def clamp(self) -> None:
        """Clamp shaping_scale into [0, 1]."""
        self.shaping_scale = min(1.0, max(0.0, self.shaping_scale))

    def terminal_reward(self, won: bool) -> float:
        return self.win if won else self.death

    def act_completion_reward(self, acts_completed: int = 1) -> float:
        return self.shaping_scale * self.act_completion * acts_completed

    def floor_reward(self, floors_climbed: int = 1) -> float:
        return self.shaping_scale * self.floor * floors_climbed

    def combat_win_reward(self, hp_start: int, hp_end: int) -> float:
        """HP-retention bonus for a combat win."""
        if hp_start <= 0:
            return 0.0
        ratio = max(0.0, min(1.0, hp_end / hp_start))
        return self.shaping_scale * self.combat_hp_retention * ratio
