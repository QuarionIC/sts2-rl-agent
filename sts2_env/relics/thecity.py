""""Acts from the Past" mod -- TheCity legacy-act event relics.

Decompiled references: decompiled_mods/ActsFromThePast/ActsFromThePast.Relics/
{MutagenicStrength,BloodyIdol,Enchiridion,NilrysCodex}.cs (all
[Pool(EventRelicPool)], C# RelicRarity 6 == Event) plus
ActsFromThePast.Powers/MutagenicStrengthPower.cs. Granted only by TheCity
events (Augmenter, ForgottenAltar, CursedTome) -- never rolled from the
normal relic grab bag (RelicPool.EVENT is excluded by
PlayerState.populate_relic_grab_bag). Mirrors sts2_env/relics/exordium.py's
per-legacy-act relic module convention.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature, register_power_class
from sts2_env.core.enums import PowerId
from sts2_env.powers.base import PowerInstance
from sts2_env.powers.turn_effects import TemporaryStrengthPower
from sts2_env.relics.base import RelicId, RelicInstance, RelicPool, RelicRarity
from sts2_env.relics.registry import register_relic

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


# ---------------------------------------------------------------------------
# MutagenicStrengthPower
# ---------------------------------------------------------------------------
class MutagenicStrengthPower(TemporaryStrengthPower):
    """Temporary Strength granted by the MutagenicStrength relic.

    C# ref: ActsFromThePast.Powers.MutagenicStrengthPower extends the
    base-game TemporaryStrengthPower unchanged (only OriginModel differs).
    Own PowerId follows the same convention as ReptileTrinketPower.
    """

    def __init__(self, amount: int):
        # Call PowerInstance.__init__ directly to set the correct PowerId
        # (same pattern as FlexPotionPower in powers/turn_effects.py).
        PowerInstance.__init__(self, PowerId.MUTAGENIC_STRENGTH, amount)


register_power_class(PowerId.MUTAGENIC_STRENGTH, MutagenicStrengthPower)


@register_relic
class MutagenicStrength(RelicInstance):
    """At the start of each combat, gain 3 temporary Strength (lost at the
    end of your first turn).

    C# ref: MutagenicStrength.AfterRoomEntered(room is CombatRoom) applies
    MutagenicStrengthPower(3) to the owner's creature.
    """

    relic_id = RelicId.MUTAGENIC_STRENGTH
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    STRENGTH = 3

    def before_combat_start(self, owner: Creature, combat: CombatState) -> None:
        owner.apply_power(PowerId.MUTAGENIC_STRENGTH, self.STRENGTH)


@register_relic
class BloodyIdol(RelicInstance):
    """Whenever you gain gold, heal 5 HP.

    C# ref: BloodyIdol.AfterGoldGained(player == Owner) heals 5. The
    `on_gold_gained` duck-typed hook here fires from both
    RunState/PlayerState.gain_gold (out of combat -- owner is the
    PlayerState) and CombatState.gain_gold (in combat -- owner is the
    Creature); both expose `.heal()` (same dual-context pattern as
    DragonFruit in relics/shop_event.py).
    """

    relic_id = RelicId.BLOODY_IDOL
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    HEAL = 5

    def on_gold_gained(self, owner: object, amount: int) -> None:
        if amount > 0:
            heal = getattr(owner, "heal", None)
            if callable(heal):
                heal(self.HEAL)


@register_relic
class Enchiridion(RelicInstance):
    """At the start of your first turn each combat, add a random Power card
    from your character's pool to your hand. It costs 0 that turn.

    C# ref: Enchiridion.AfterPlayerTurnStart (RoundNumber == 1) creates 1
    distinct Power card from the owner's character card pool
    (CardFactory.GetDistinctForCombat, Rng.CombatCardGeneration), calls
    SetToFreeThisTurn(), and adds it to the Hand pile.
    """

    relic_id = RelicId.ENCHIRIDION
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT

    def after_player_turn_start(self, owner: Creature, combat: CombatState) -> None:
        from sts2_env.cards.factory import create_distinct_character_cards
        from sts2_env.core.enums import CardType

        if combat.round_number != 1:
            return
        state = combat.combat_player_state_for(owner)
        if state is None:
            return
        cards = create_distinct_character_cards(
            state.character_id,
            combat.combat_card_generation_rng,
            1,
            card_type=CardType.POWER,
            generation_context="combat",
            is_multiplayer=combat.is_multiplayer,
        )
        if not cards:
            return
        card = cards[0]
        card.owner = owner
        card.set_temporary_free_this_turn()
        combat.add_generated_card_to_creature_hand(owner, card)


@register_relic
class NilrysCodex(RelicInstance):
    """At the end of each of your turns, choose 1 of 3 random cards from
    your character's pool to shuffle into your draw pile (skippable).

    C# ref: NilrysCodex.BeforeFlushLate (combat in progress) creates 3
    distinct cards from the owner's character card pool
    (CardFactory.GetDistinctForCombat, Rng.CombatCardGeneration), offers a
    cancelable choose-a-card screen, and adds the pick to the Draw pile at
    a Random position.
    """

    relic_id = RelicId.NILRYS_CODEX
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    CHOICES = 3

    def before_flush_late(self, owner: Creature, flushing_owner: Creature, combat: CombatState) -> None:
        from sts2_env.cards.factory import create_distinct_character_cards

        if flushing_owner is not owner or combat.is_over or combat.pending_choice is not None:
            return
        state = combat.combat_player_state_for(owner)
        if state is None:
            return
        cards = create_distinct_character_cards(
            state.character_id,
            combat.combat_card_generation_rng,
            self.CHOICES,
            generation_context="combat",
            is_multiplayer=combat.is_multiplayer,
        )
        if not cards:
            return

        def _resolver(card):
            if card is not None:
                combat.add_generated_card_to_creature_draw_pile(
                    owner, card, random_position=True,
                )

        combat.request_card_choice(
            prompt="Choose a card to shuffle into your draw pile.",
            cards=cards,
            source_pile="generated",
            resolver=_resolver,
            allow_skip=True,
            owner=owner,
        )
