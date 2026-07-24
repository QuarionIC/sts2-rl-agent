"""Powers introduced by the Act4Heart mod (Corrupt Heart / Super Elites).

C# refs (decompiled_mods/Act4Heart/Act4Heart.Powers/):
    BeatOfDeathPower.cs, InvinciblePower.cs, RegeneratePowerA4h.cs

``MetallicizePowerA4h`` is intentionally NOT reimplemented here: its C#
source is behaviorally identical to the vanilla ``MetallicizePower`` already
implemented in ``sts2_env/powers/remaining_c.py`` (gain Amount block, no
decay, at the end of the owner's turn) so callers should just apply
``PowerId.METALLICIZE`` directly. ``RegeneratePowerA4h`` differs from the
vanilla ``PowerId.REGEN`` (which decays by 1 stack per use) in that it never
decays, so it gets its own PowerId/implementation below.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.enums import CombatSide, PowerId, PowerType, PowerStackType, ValueProp
from sts2_env.powers.base import PowerInstance

if TYPE_CHECKING:
    from sts2_env.core.creature import Creature
    from sts2_env.core.combat import CombatState


# ---------------------------------------------------------------------------
# BeatOfDeathPower
# ---------------------------------------------------------------------------
class BeatOfDeathPower(PowerInstance):
    """Whoever plays a card takes Amount damage, regardless of who owns it.

    C# ref: BeatOfDeathPower.cs
    - AfterCardPlayed: ``CreatureCmd.Damage(cardPlay.Card.Owner.Creature,
      Amount, ValueProp.Unpowered, Owner)`` -- i.e. the power punishes
      whoever PLAYED the card (the card's owner), not the power's owner.
      Damage is Unpowered (no Strength/Weak/Vulnerable) but still blockable.
    StackType.Counter.
    """

    # C# Type => (PowerType)1 == Buff: this power buffs its OWNER (the
    # Heart), even though its effect damages whoever plays a card.
    power_type = PowerType.BUFF
    stack_type = PowerStackType.COUNTER
    should_scale_in_multiplayer = False

    def __init__(self, amount: int):
        super().__init__(PowerId.BEAT_OF_DEATH, amount)

    def after_card_played(self, owner: Creature, card: object, combat: CombatState) -> None:
        if self.amount <= 0 or combat.is_over:
            return
        card_player = getattr(card, "owner", None)
        if card_player is None or not card_player.is_alive:
            return
        from sts2_env.core.damage import apply_damage

        apply_damage(card_player, int(self.amount), ValueProp.UNPOWERED, combat, owner)


# ---------------------------------------------------------------------------
# InvinciblePower
# ---------------------------------------------------------------------------
class InvinciblePower(PowerInstance):
    """Caps total HP loss this "turn" (since the owner's side last started
    its turn) to ``Amount``. Resets whenever the owner's side starts its
    turn.

    C# ref: InvinciblePower.cs
    - ModifyHpLostAfterOstyLate: ``min(amount, Amount - damage_taken_since_reset)``
    - AfterDamageReceived: accumulate unblocked damage taken.
    - BeforeSideTurnStart (side == Owner.Side): reset the accumulator.

    The C# implementation optionally tracks damage per-dealer for
    multiplayer ("split" pools); this simulator is single-player only so we
    track a single running total, matching the non-split (solo) behavior.
    """

    power_type = PowerType.BUFF
    stack_type = PowerStackType.COUNTER
    should_scale_in_multiplayer = False

    def __init__(self, amount: int):
        super().__init__(PowerId.INVINCIBLE, amount)
        self.damage_taken_this_cycle: int = 0

    @property
    def remaining_capacity(self) -> int:
        return max(0, int(self.amount) - self.damage_taken_this_cycle)

    def modify_hp_lost_late(
        self, owner: Creature, target: Creature, amount: float,
        dealer: Creature | None, props: ValueProp,
    ) -> float:
        if target is not owner or amount == 0:
            return amount
        return min(amount, self.remaining_capacity)

    def after_damage_received(
        self, owner: Creature, target: Creature, dealer: Creature | None,
        damage: int, props: ValueProp, combat: CombatState,
    ) -> None:
        if target is not owner:
            return
        self.damage_taken_this_cycle += damage

    def before_side_turn_start(self, owner: Creature, side: CombatSide, combat: CombatState) -> None:
        if side == owner.side:
            self.damage_taken_this_cycle = 0


# ---------------------------------------------------------------------------
# RegenerateA4hPower (Super Elite buff -- see Act4Heart.Keys/GreenKeyHooks.cs)
# ---------------------------------------------------------------------------
class RegenerateA4hPower(PowerInstance):
    """Heal Amount HP at end of owner's turn. Unlike vanilla Regen (PowerId
    .REGEN), this does NOT decay -- it heals the same amount every turn for
    the rest of combat.

    C# ref: RegeneratePowerA4h.cs -- AfterSideTurnEnd (side == Owner.Side,
    owner alive): ``CreatureCmd.Heal(Owner, Amount)``.
    """

    power_type = PowerType.BUFF
    stack_type = PowerStackType.COUNTER
    should_scale_in_multiplayer = True

    def __init__(self, amount: int):
        super().__init__(PowerId.REGENERATE_A4H, amount)

    def after_turn_end(self, owner: Creature, side: CombatSide, combat: CombatState) -> None:
        if side == owner.side and owner.is_alive:
            owner.heal(self.amount)


# ---------------------------------------------------------------------------
# Registration
# ---------------------------------------------------------------------------
from sts2_env.core.creature import register_power_class  # noqa: E402

register_power_class(PowerId.BEAT_OF_DEATH, BeatOfDeathPower)
register_power_class(PowerId.INVINCIBLE, InvinciblePower)
register_power_class(PowerId.REGENERATE_A4H, RegenerateA4hPower)
