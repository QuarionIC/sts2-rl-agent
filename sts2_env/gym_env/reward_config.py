"""Reward configuration for the rich envs (docs/TRAINING_REVAMP_SPEC.json).

Terminal rewards (win / death / truncation) are fixed and never annealed.
All per-step shaping is a single potential-based term (PBRS):

    F(s, s') = shaping_scale * (gamma_shape * Phi(s') - Phi(s))

with ``Phi := 0`` at terminal states. PBRS telescopes to a policy-independent
constant per episode (Ng, Harada & Russell 1999), so it provably cannot
change the optimal policy and no reward-farming loop exists.

The potential is

    Phi(s) = w_progress * progress - w_hp_damage * hp_damage_cost
             + w_enemy_down * enemy_down + w_deck_quality * deck_quality

* ``progress = (current_act_index + clip(act_floor / 17, 0, 1)) / target_acts``
  -- monotone run progress; path-independent, so no take-more-rooms bias.
* ``hp_damage_cost = cumulative_damage_taken / max_hp`` -- a RATCHET, and the
  only term entering Phi with a minus sign. It counts HP actually lost and
  never decreases, so damage is penalised and healing is NOT rewarded.

  The previous form, ``+ w * clip(hp / max_hp, 0, 1)``, could not express
  that: any potential written as a function of CURRENT hp necessarily pays
  for healing exactly as much as it charges for damage, because it is the
  same term read in two directions. That made a heal option strictly
  dominate an upgrade option of equal survival value at the same room, which
  is wrong -- upgrading or a stronger event outcome is often the better pick.

  Ratcheting on cumulative damage rather than on min-HP-seen is deliberate.
  A min-HP ratchet makes every point of damage back down to a previous low
  free, so an agent that once dipped to 20 HP could take 40 more damage at
  no cost. Cumulative damage charges for every point, always.

  It is deliberately NOT clipped to 1. Clipping would make all damage free
  once a run had lost max_hp in total, reintroducing the same hole. Left
  uncapped, the per-floor guarantee still holds: surviving a floor means
  losing less than max_hp on it, so the worst per-floor charge is under
  w_hp_damage (0.30) and stays below the per-floor progress gain (0.353).

  Block and Osty HP were removed earlier and stay out: block was credited
  the instant it was gained with no reference to whether an attack was
  incoming, so the agent was paid for blocking a sleeping enemy. Block is a
  means, not an end -- used well it shows up here as damage never taken.
* ``enemy_down = 1 - sum(enemy_hp) / sum(enemy_max_hp)`` while in combat,
  carried at its last value between combats (1.0 after a cleared combat) --
  densifies long fights; only gained by actually reducing real enemy HP.
  Weighted 0 by default (moved to ``terminal_reward``).

Phi(s0) == 0
------------
At reset progress is 0 and cumulative damage is 0, so ``Phi(s0) = 0``. Since
PBRS shaping over a whole episode telescopes to ``-Phi(s0)``, the shaping
contributes EXACTLY ZERO to every episode's total return: a run is worth its
terminal reward and nothing else. That is what lets ``win`` be read directly
as the realised value of a win (see ``win`` below).

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
    win : terminal reward for winning the episode (never annealed). Because
        Phi(s0) == 0 the shaping cancels over a full episode, so this is
        literally the total return of a win; it is set above 1.0 so a win is
        always strictly greater than 1 after shaping.
    death : terminal reward for dying (never annealed).
    truncation : terminal reward when the episode is truncated (step-limit
        timeout). With the 30-turn combat cap scoring in-combat stalls as
        deaths, the only way to hit the episode step cap is a non-combat
        stall; stalling must not be "safer" than fighting, so it scores
        like a death (-1). (0.0 previously let the value function learn
        truncation(0) > death(-1), biasing eval-time argmax toward
        absorbing dither loops.)
    gamma_shape : discount inside the PBRS term; 1.0 (see the field comment
        -- the hierarchical env sums F across a combat undiscounted).
    w_progress / w_hp_damage / w_enemy_down / w_deck_quality : Phi component
        weights. w_hp_damage multiplies a COST and enters Phi NEGATIVELY.
        w_deck_quality defaults to 0.0 (off).
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

    #: Sized so the CUMULATIVE reward still RISES when the boss dies.
    #:
    #: Because Phi(s0) = 0, the running total after any floor n is exactly
    #: Phi(n), and the total after the win is exactly ``win`` -- the shaping
    #: telescopes away. So "beating the boss must beat the previous floor"
    #: is the condition ``win > Phi(last floor)``, and since Phi is capped by
    #: ``w_progress + w_deck_quality`` (progress <= 1, the hp term only
    #: subtracts) it is sufficient that ``win > w_progress + w_deck_quality``.
    #:
    #: At win=1.0 this failed badly: a hypothetical Act-1 win ran the
    #: cumulative up to +5.63 by floor 16, then the boss floor dropped it to
    #: +0.70 -- Phi collapsing to 0 at the terminal swamped the +1. The trace
    #: read as though winning were a setback.
    #:
    #: 10.0 clears the 6.0 cap with margin and also satisfies the weaker
    #: earlier requirement (a win is worth strictly more than 1 after
    #: shaping). Checked by :meth:`win_beats_last_floor`, asserted in tests.
    win: float = 10.0
    death: float = -10.0
    truncation: float = -10.0
    #: Paid at the terminal, once per elite defeated during the run, on ANY
    #: ending. Unlike everything else in this file it is NOT policy-invariant:
    #: it is a deliberate thumb on the scale toward fighting elites, so the
    #: agent trades HP for the relic/deck payoff instead of routing around
    #: them. It also grades losses -- dying having cleared 2 elites (-8.0)
    #: beats dying having cleared none (-10.0).
    #:
    #: Terminal rather than per-floor on purpose: an immediate bonus would be
    #: collectable and then thrown away, whereas paying at the end prices the
    #: elite by what the run actually did with it.
    elite_bonus: float = 1.0
    #: MUST be 1.0 here, not the PPO gamma, because the hierarchical env sums
    #: the parent's per-step F across an auto-played combat WITHOUT
    #: discounting the interior terms. That sum is
    #:     gamma_shape*Phi(end) - Phi(start) + (gamma_shape - 1)*sum(Phi_interior)
    #: so anything below 1.0 leaves a path-dependent residual proportional to
    #: how long Phi is held high -- i.e. a standing penalty for long fights and
    #: for surviving deep into the act, which is the opposite of the goal.
    #: Measured on 8 real episodes: at 0.997 with w_progress=6.0 the residual
    #: is -0.2099 (10.5% of the +1/-1 terminal gap); at 1.0 it is exactly
    #: 0.0000. The residual scales with Phi, so raising w_progress 0.45 -> 6.0
    #: had inflated it 5.7x (-0.0370 -> -0.2099) before this fix.
    gamma_shape: float = 1.0
    #: Sized so ADVANCING A FLOOR IS ALWAYS NET POSITIVE. A floor is worth
    #: w_progress / ACT_FLOOR_SCALE = 6.0/17 = 0.353 of potential, which
    #: exceeds the worst survivable HP charge (w_hp_damage * 1.0 = 0.30;
    #: surviving a floor means losing under max_hp on it).
    #: At the old 0.45 a floor was worth 0.027 and a 6 HP loss outweighed it,
    #: so climbing while hurt scored NEGATIVE -- measured -0.028 on floor 7
    #: of a real run.
    w_progress: float = 6.0
    #: Coefficient on the cumulative-damage COST. Enters Phi with a MINUS.
    #: Was ``w_effective_hp``, a coefficient on current hp/max_hp; renamed
    #: rather than reinterpreted because the sign and the semantics both
    #: changed and a silent swap would have been invisible at the call sites.
    w_hp_damage: float = 0.30
    #: Removed from the potential (0.0). Enemy depletion is now credited ONLY
    #: on a LOSS, via death_progress_credit below, so that losing having dealt
    #: 80% of enemy HP beats losing having dealt 60%. It cannot live in the
    #: potential: PBRS sets Phi := 0 at terminal states, so whatever depletion
    #: had been achieved is erased exactly when it should be scored.
    w_enemy_down: float = 0.0
    #: Scales the death penalty by damage dealt: death + credit * enemy_down,
    #: so a 100%-depleted loss scores -6.0 and a 0%-depleted loss the full
    #: -10.0. Never enough to make losing preferable to winning (+10.0).
    #:
    #: Held at 40% of |death| deliberately. When death moved -1.0 -> -10.0 this
    #: stayed at 0.4 for one revision, which silently crushed the grading it
    #: exists to provide: the whole 0%-to-100%-depleted spread was 0.4 out of a
    #: 20-point terminal range (2%), so "died having nearly killed it" and
    #: "died without scratching it" were all but the same number. Scaling with
    #: death keeps the ratio that made the signal legible.
    death_progress_credit: float = 4.0
    #: Upgrade-density term. Default 0.0 keeps every existing result
    #: reproducible; the hierarchical run agent opts in explicitly so
    #: the change can be A/B'd rather than silently altering Phi.
    w_deck_quality: float = 0.0
    #: COMBAT-ONLY: how much of a win's value HP loss can cost.
    #:
    #: The combat env's reward was terminal and FLAT on a win, so a win at
    #: full HP and a win at 1 HP paid identically -- the combat agent had no
    #: reason to prefer the cheap line. That is fine for one fight and wrong
    #: for a run: HP only comes back at rest sites, so a win bought at 30 HP
    #: loses the run two floors later. Measured 2026-07-31, the deterministic
    #: planner (which optimises win-then-HP) converted the SAME run agent's
    #: decks into a 34.0% run win rate against the RL combat agent's 8.7%
    #: (n=150 shared seeds each, two-proportion p=8.5e-08).
    #:
    #: 2.0 is 20% of ``win``. It spreads wins across a 2-point band while
    #: leaving 14 points between the worst win (+8.0, having lost a full
    #: max_hp) and the best possible loss (-6.0, having dealt 100% of enemy
    #: HP) -- so no amount of HP saved can ever make losing attractive.
    #: Rescale with ``win`` if ``win`` changes.
    #:
    #: Applies to WINS ONLY, via :meth:`combat_terminal_reward`. On a loss
    #: the player is at 0 HP, so an HP term there is a near-constant penalty
    #: scaled by whatever HP the sampler handed out -- it would punish the
    #: combat agent for the run agent's earlier decisions.
    #:
    #: THE SIZING CONSTRAINT THAT MATTERS is not "wins beat losses" (that is
    #: satisfied with enormous slack) but how much WIN PROBABILITY the term
    #: invites the agent to trade away for HP. Giving up dp of win chance to
    #: save dc of HP cost pays iff ``w * dc > dp * (win - loss)``. With
    #: win-loss ~= 18 and w = 2.0, even the full 0->1 HP swing only justifies
    #: dp < 2/18 ~= 11%, and a realistic dc of 0.3 justifies dp < 3.3%. That
    #: is the intended bargain: shave HP when it is nearly free, never gamble
    #: the fight for it. At w = 8.0 the arithmetic flips and a 70%-win line
    #: that costs 5 HP outranks a 90%-win line that costs 40 -- which is why
    #: this is 2.0 and not "as large as the loss bound allows".
    w_combat_hp_retained: float = 2.0

    #: COMBAT-ONLY PBRS on cumulative damage: Phi(s) = -w * damage/max_hp.
    #:
    #: The terminal charge alone did not move behaviour. Measured 2026-07-31
    #: over 1.5M-step arms warm-started from the same checkpoint, HP retained
    #: among wins went 0.500 (no term) -> 0.519 at w_combat_hp_retained=2.0
    #: and 0.508 at 4.0 -- under a fifth of the 7.1 HP gap to the planner, and
    #: NON-MONOTONIC in the weight, which is the signature of noise rather
    #: than a real effect.
    #:
    #: The problem is credit assignment, not sizing: a combat is ~30 actions
    #: and the entire HP charge arrived on the last one. This delivers it on
    #: the step the damage is taken. Being a potential with
    #: Phi(s0) = Phi(terminal) = 0, it telescopes to zero over the episode and
    #: cannot change the optimal policy -- it only makes the existing
    #: objective learnable. Eval zeroes shaping_scale, so reported numbers
    #: stay pure-sparse and comparable across every measurement.
    #:
    #: Sized to match the terminal charge so the two speak the same units.
    w_combat_hp_shaping: float = 2.0

    #: Bounds on the HP cost ratio. The floor is NEGATIVE because a net heal
    #: (Necrobinder heals mid-combat) is real value and should pay -- but
    #: only up to +0.5 reward, so a Regen stall cannot out-earn winning
    #: promptly. The ceiling stops a single brutal fight from dominating.
    combat_hp_cost_floor: float = -0.25
    combat_hp_cost_ceiling: float = 1.0

    #: Acts the episode is scored against; the progress denominator. 1 = the
    #: current goal (beat act 1). Must match the env's max_act_count or the
    #: potential saturates early or never reaches 1.0.
    target_acts: float = 1.0
    shaping_scale: float = 1.0
    legacy_shaping: bool = False
    act_completion: float = 0.25
    floor: float = 0.004
    combat_hp_retention: float = 0.05

    # ------------------------------------------------------------------

    def clamp(self) -> None:
        """Clamp shaping_scale into [0, 1]."""
        self.shaping_scale = min(1.0, max(0.0, self.shaping_scale))

    def terminal_reward(self, won: bool, enemy_down: float | None = None,
                        elites_beaten: int = 0) -> float:
        """Terminal payout. A LOSS is graded by how far the fight got.

        Wins are flat (+1.0): HP retained deliberately does not change a
        win's value.

        Losses are not flat. ``death + death_progress_credit * enemy_down``
        makes losing having dealt 80% of enemy HP (-0.68) rank above losing
        having dealt 60% (-0.76), which is the requested ordering. This has
        to be terminal: the potential is defined as 0 at terminal states, so
        an enemy_down term inside Phi is erased at exactly the moment it
        would matter.

        The credit is capped well below the win/loss gap, so no amount of
        damage dealt makes a loss preferable to a win (-0.60 worst case vs
        +1.00).

        An HP-retention bonus was tried here and removed by request. Note
        the consequence, because it is not obvious: with a flat terminal and
        potential-based shaping, the TOTAL return of a win does not depend
        on how much HP it cost. PBRS telescopes to
        ``gamma^T*Phi(terminal) - Phi(s0)`` with Phi := 0 at terminal --
        measured, a clean win totals -0.3027 of shaping and a bloody one
        -0.3016. So HP loss shapes intermediate credit and learning speed,
        but a 0-damage win and a 10-damage win are worth the same at the
        end of the episode.
        """
        elites = self.elite_bonus * max(0, int(elites_beaten))
        if won:
            return self.win + elites
        credit = 0.0
        if enemy_down is not None:
            credit = self.death_progress_credit * min(1.0, max(0.0, float(enemy_down)))
        return self.death + credit + elites

    def combat_terminal_reward(self, won: bool, enemy_down: float | None = None,
                               hp_cost: float | None = None) -> float:
        """Terminal payout for a STANDALONE COMBAT episode.

        Identical to :meth:`terminal_reward` except that a win is charged for
        the HP it cost. Deliberately a separate entry point rather than an
        extra argument on ``terminal_reward``: the run env calls that method
        positionally (rich_run_env.py) and its win is flat ON PURPOSE --
        under PBRS the shaping telescopes, so a clean win and a bloody one
        already total the same, and charging HP there would double-count
        against the potential's own hp_damage term.

        ``hp_cost`` is NET HP lost over the fight as a fraction of max_hp:
        ``(hp_start - hp_end) / max_hp``. Net, not gross -- healing back is
        genuine value for the run, so it earns rather than merely offsets.
        Normalised by max_hp rather than hp_start because absolute HP is the
        currency a run spends, and because hp_start is handed to the agent by
        the deck sampler; grading against it would charge the combat agent
        for a start it did not choose.
        """
        base = self.terminal_reward(won, enemy_down)
        if not won or hp_cost is None or self.w_combat_hp_retained == 0.0:
            return base
        cost = min(self.combat_hp_cost_ceiling,
                   max(self.combat_hp_cost_floor, float(hp_cost)))
        return base - self.w_combat_hp_retained * cost

    def worst_win_beats_best_loss(self) -> bool:
        """The invariant that keeps the HP term safe. Asserted in tests.

        No amount of HP saved may make losing preferable to winning. The
        worst win is a win that cost a full max_hp; the best loss is a death
        having depleted every enemy.
        """
        worst_win = self.win - self.w_combat_hp_retained * self.combat_hp_cost_ceiling
        best_loss = self.death + self.death_progress_credit
        return worst_win > best_loss

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

    def progress(self, mgr: RunManager) -> float:
        """Monotone progress in [0, 1], normalised to the TARGET act count.

        Dividing by a fixed 4 acts made each floor worth 1/(17*4) of the
        progress term -- 0.0066 of shaping after the 0.45 weight. With an
        act-1 goal that is 4x smaller than it should be, and per-floor
        progress is the densest signal this agent has. Normalising by
        ``target_acts`` makes a floor worth 1/17 of progress (0.0265), and
        reaching the goal put Phi's progress component at exactly 1.0.
        """
        rs = mgr.run_state
        act_frac = min(1.0, max(0.0, rs.act_floor / ACT_FLOOR_SCALE))
        denom = max(1.0, float(self.target_acts))
        return min(1.0, max(0.0, (rs.current_act_index + act_frac) / denom))

    @staticmethod
    def current_hp(mgr: RunManager) -> tuple[int, int]:
        """(hp, max_hp), read from combat when a fight is live.

        The env's damage ratchet and this module must agree on WHICH hp they
        are watching, or the ratchet double-counts at every combat boundary.
        Single source of truth.
        """
        combat = mgr.get_combat_state()
        player = (combat.primary_player if combat is not None
                  else mgr.run_state.player)
        return player.current_hp, player.max_hp

    @staticmethod
    def hp_damage_cost(mgr: RunManager, damage_taken: float) -> float:
        """Cumulative HP lost this episode, as a fraction of max_hp.

        A COST: :meth:`potential` subtracts it. ``damage_taken`` is the
        env-tracked ratchet -- it only ever increases, so healing cannot
        reduce it and therefore cannot be rewarded. Damage still is
        penalised, every point of it.

        Not clipped to 1: see the module docstring. Clipping would make
        damage free once a run had cumulatively lost max_hp.
        """
        _, max_hp = RewardConfig.current_hp(mgr)
        if max_hp <= 0:
            return 0.0
        return max(0.0, float(damage_taken) / max_hp)

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

    def potential(self, mgr: RunManager, enemy_down: float = 0.0,
                  damage_taken: float = 0.0) -> float:
        """Phi(s) for a live (non-terminal) state.

        ``enemy_down`` is the carried enemy-depletion value tracked by the
        env (recomputed while in combat, held between combats).
        ``damage_taken`` is the env's cumulative-damage ratchet, in HP.
        Callers must use Phi = 0 at terminal states.

        Note the sign on the HP term: it is SUBTRACTED. Phi is therefore not
        bounded below by 0, which is fine -- PBRS places no constraint on
        Phi's range, only that it is a function of state and that Phi := 0
        at terminal.
        """
        return (
            self.w_progress * self.progress(mgr)
            - self.w_hp_damage * self.hp_damage_cost(mgr, damage_taken)
            + self.w_enemy_down * min(1.0, max(0.0, enemy_down))
            + self.w_deck_quality * self.deck_quality(mgr)
        )

    def win_total_return(self, phi_start: float = 0.0) -> float:
        """Realised total return of a winning episode, shaping included.

        PBRS over a whole episode telescopes to ``-Phi(s0)`` (Phi := 0 at
        terminal), so the total is ``win - Phi(s0)``. With the damage-ratchet
        potential ``Phi(s0) = 0`` at reset -- floor 0, zero damage -- and this
        is just ``win``. Exposed so the "a win is always > 1 after shaping"
        requirement is a checkable property rather than a claim in a comment.
        """
        return self.win - phi_start

    def phi_max(self) -> float:
        """Upper bound on Phi over all non-terminal states.

        progress and deck_quality are both in [0, 1]; enemy_down is weighted
        0; the hp term only ever subtracts. So Phi can never exceed this.
        """
        return self.w_progress + self.w_deck_quality + self.w_enemy_down

    def win_beats_last_floor(self) -> bool:
        """Does the cumulative reward still RISE on the winning step?

        With Phi(s0) = 0 the running total after floor n is Phi(n) and the
        total after the win is ``win``, so this is exactly
        ``win > max Phi``. False means the reward trace shows beating the
        boss as a drop, which is what win=1.0 did.
        """
        return self.win_total_return(0.0) > self.phi_max()

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
