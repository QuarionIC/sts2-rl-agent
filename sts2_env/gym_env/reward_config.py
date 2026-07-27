"""Reward configuration for the rich envs (docs/TRAINING_REVAMP_SPEC.json).

Terminal rewards (win / death / truncation) are fixed and never annealed.
All per-step shaping is a single potential-based term (PBRS):

    F(s, s') = shaping_scale * (gamma_shape * Phi(s') - Phi(s))

with ``Phi := 0`` at terminal states. PBRS telescopes to a policy-independent
constant per episode (Ng, Harada & Russell 1999), so it provably cannot
change the optimal policy and no reward-farming loop exists.

The potential is

    Phi(s) = w_progress * progress + w_effective_hp * effective_hp
             + w_enemy_down * enemy_down + w_deck_quality * deck_quality

with every component in [0, 1]:

* ``progress = (current_act_index + clip(act_floor / 17, 0, 1)) / 4`` --
  monotone run progress; path-independent, so no take-more-rooms bias.
* ``effective_hp = clip((hp + 0.5*block + 0.3*osty_hp) / max_hp, 0, 1)`` --
  run-long HP economy (combat values while in combat, run values outside;
  spending HP/Osty for tempo is a transient dip refunded when it resolves).
* ``enemy_down = 1 - sum(enemy_hp) / sum(enemy_max_hp)`` while in combat,
  carried at its last value between combats (1.0 after a cleared combat) --
  densifies long fights; only gained by actually reducing real enemy HP.

``shaping_scale`` is a constant multiplier knob on F (1.0 during training,
0.0 for pure-sparse eval). It is never annealed.

Legacy shaping (ablation switch)
--------------------------------
``legacy_shaping=True`` restores the attempt-6-era event-based shaping that
PBRS replaced (floor +0.004, act completion +0.25, combat-win HP retention
+0.05 * hp_end/hp_start; all times ``shaping_scale``). The run env then
applies those terms INSTEAD of the PBRS term F. Terminal rewards (win +1 /
death -1 / truncation -1) are identical in both modes. Default is False --
PBRS -- so existing behavior is unchanged.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState
    from sts2_env.run.run_manager import RunManager

#: act_floor normalization inside ``progress`` (a full act is ~17 floors).
ACT_FLOOR_SCALE = 17.0
#: number of acts a full run spans (progress denominator).
NUM_ACTS = 4.0


@dataclass
class RewardConfig:
    """Reward terms consumed by RichSTS2RunEnv / RichSTS2CombatEnv.

    Attributes
    ----------
    win : terminal reward for winning the episode (never annealed).
    death : terminal reward for dying (never annealed).
    truncation : terminal reward when the episode is truncated (step-limit
        timeout). With the 30-turn combat cap scoring in-combat stalls as
        deaths, the only way to hit the episode step cap is a non-combat
        stall; stalling must not be "safer" than fighting, so it scores
        like a death (-1). (0.0 previously let the value function learn
        truncation(0) > death(-1), biasing eval-time argmax toward
        absorbing dither loops.)
    gamma_shape : discount used inside the PBRS term; MUST match the
        training gamma so the shaping stays policy-invariant.
    w_progress / w_effective_hp / w_enemy_down / w_deck_quality : Phi
        component weights. w_deck_quality defaults to 0.0 (off).
    shaping_scale : global multiplier in [0, 1] applied to the PBRS term F.
        1.0 = full shaping, 0.0 = pure sparse reward. Constant during
        training (no anneal -- PBRS is invariant and needs none).
    legacy_shaping : ablation switch (default False). True = the run env
        applies the attempt-6-era event shaping terms below INSTEAD of the
        PBRS term. Terminal rewards are identical in both modes.
    act_completion / floor / combat_hp_retention : legacy event shaping
        magnitudes (only consumed when ``legacy_shaping`` is True; all
        multiplied by ``shaping_scale``).
    """

    win: float = 1.0
    death: float = -1.0
    truncation: float = -1.0
    gamma_shape: float = 0.997
    w_progress: float = 0.45
    w_effective_hp: float = 0.30
    w_enemy_down: float = 0.20
    #: Upgrade-density term. Default 0.0 keeps every existing result
    #: reproducible; the hierarchical run agent opts in explicitly so
    #: the change can be A/B'd rather than silently altering Phi.
    w_deck_quality: float = 0.0
    shaping_scale: float = 1.0
    legacy_shaping: bool = False
    act_completion: float = 0.25
    floor: float = 0.004
    combat_hp_retention: float = 0.05

    # ------------------------------------------------------------------

    def clamp(self) -> None:
        """Clamp shaping_scale into [0, 1]."""
        self.shaping_scale = min(1.0, max(0.0, self.shaping_scale))

    def terminal_reward(self, won: bool) -> float:
        return self.win if won else self.death

    # ------------------------------------------------------------------
    # Legacy event shaping (attempt-6 era; only when legacy_shaping=True)
    # ------------------------------------------------------------------

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

    # ------------------------------------------------------------------
    # Potential Phi and its components
    # ------------------------------------------------------------------

    @staticmethod
    def progress(mgr: RunManager) -> float:
        """Monotone run progress in [0, 1]."""
        rs = mgr.run_state
        act_frac = min(1.0, max(0.0, rs.act_floor / ACT_FLOOR_SCALE))
        return min(1.0, max(0.0, (rs.current_act_index + act_frac) / NUM_ACTS))

    @staticmethod
    def effective_hp(mgr: RunManager) -> float:
        """clip((hp + 0.5*block + 0.3*osty_hp) / max_hp, 0, 1).

        Uses live combat values (block, Osty) while in combat; outside
        combat block/osty contribute 0 and run-state HP is used.
        """
        combat = mgr.get_combat_state()
        if combat is not None:
            player = combat.primary_player
            hp = player.current_hp
            max_hp = player.max_hp
            block = player.block
            osty = combat.get_osty(player)
            osty_hp = osty.current_hp if (osty is not None and osty.is_alive) else 0
        else:
            player = mgr.run_state.player
            hp = player.current_hp
            max_hp = player.max_hp
            block = 0
            osty_hp = 0
        if max_hp <= 0:
            return 0.0
        return min(1.0, max(0.0, (hp + 0.5 * block + 0.3 * osty_hp) / max_hp))

    @staticmethod
    def enemy_down(combat: CombatState) -> float:
        """In-combat enemy-HP depletion in [0, 1] (1 = all enemies dead)."""
        total_max = 0
        total_cur = 0
        for enemy in combat.enemies:
            total_max += max(0, enemy.max_hp)
            if enemy.is_alive:
                total_cur += max(0, enemy.current_hp)
        if total_max <= 0:
            return 0.0
        return min(1.0, max(0.0, 1.0 - total_cur / total_max))

    def potential(self, mgr: RunManager, enemy_down: float = 0.0) -> float:
        """Phi(s) for a live (non-terminal) state.

        ``enemy_down`` is the carried enemy-depletion value tracked by the
        env (recomputed while in combat, held between combats). Callers must
        use Phi = 0 at terminal states.
        """
        return (
            self.w_progress * self.progress(mgr)
            + self.w_effective_hp * self.effective_hp(mgr)
            + self.w_enemy_down * min(1.0, max(0.0, enemy_down))
            + self.w_deck_quality * self.deck_quality(mgr)
        )

    def deck_quality(self, mgr: RunManager) -> float:
        """Upgrade density of the owned deck, in [0, 1].

        Added because the hierarchical run agent exposed a gap the other three
        components cannot express. Once combats were delegated, the agent
        learned to TAKE cards (deck 10.3 -> 15.3) but not to take good ones:
        harvested decks averaged 13.1 cards carrying 0.57 upgrades, and were
        measurably harder to win with than synthetic decks of similar size.
        Nothing in Phi distinguished a strong pick from a weak one.

        Upgrade density is used rather than a hand-authored card-power table
        because it is unambiguous (an upgrade is never a downgrade), it is
        exactly the deficiency measured, and it is the one deck property the
        winnability probe tied to survival -- the single elite position where
        any line survived was the only deck with upgrades.

        Note this is deliberately a DENSITY, not a count: rewarding raw
        upgrade count would also reward deck bloat, which is the failure mode
        already observed. Density rises by upgrading and falls by adding
        unupgraded filler, which is the intended pressure.

        Being part of Phi keeps this policy-invariant: PBRS shapes the
        learning signal without changing the optimal policy, so a bad weight
        costs learning speed rather than correctness.
        """
        rs = getattr(mgr, "run_state", None)
        player = getattr(rs, "player", None) if rs is not None else None
        deck = getattr(player, "deck", None) if player is not None else None
        if not deck:
            return 0.0
        upgraded = sum(1 for card in deck if getattr(card, "upgraded", False))
        return min(1.0, max(0.0, upgraded / len(deck)))

    def shaping_reward(self, phi_prev: float, phi_next: float) -> float:
        """F = shaping_scale * (gamma_shape * Phi(s') - Phi(s))."""
        return self.shaping_scale * (self.gamma_shape * phi_next - phi_prev)
