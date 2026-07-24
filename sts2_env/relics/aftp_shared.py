""""Acts from the Past" mod -- shared shrine-event relics.

Decompiled references: decompiled_mods/ActsFromThePast/ActsFromThePast.Relics/
{SpiritPoop,WarpedTongs,CultistHeadpiece,FaceOfCleric,GremlinVisage,
NlothsHungryFace,SsserpentHead}.cs (all [Pool(EventRelicPool)], C#
RelicRarity 6 == Event). Granted only by the mod's shared shrine events
(sts2_env/events/aftp_shared.py: BonfireSpirits gives SpiritPoop,
OminousForge's Rummage gives WarpedTongs, FaceTrader's Trade gives one of
the five face relics) -- never rolled from the normal relic grab bag
(RelicPool.EVENT is excluded by PlayerState.populate_relic_grab_bag).
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature
from sts2_env.core.enums import MapPointType, PowerId, RoomType
from sts2_env.relics.base import RelicId, RelicInstance, RelicPool, RelicRarity
from sts2_env.relics.registry import register_relic

if TYPE_CHECKING:
    from sts2_env.cards.base import CardInstance
    from sts2_env.core.combat import CombatState


def _card_is_upgradable(card: CardInstance) -> bool:
    """Same upgradability test as PlayerState.upgradable_deck_cards."""
    from sts2_env.cards.factory import create_card

    if card.upgraded:
        return False
    try:
        upgraded = create_card(card.card_id, upgraded=True)
    except KeyError:
        return False
    return upgraded.upgraded


@register_relic
class SpiritPoop(RelicInstance):
    """It's incredibly smelly! (No mechanics -- marker relic.)

    C# ref: SpiritPoop.cs has no behavior overrides at all.
    """

    relic_id = RelicId.SPIRIT_POOP
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT


@register_relic
class WarpedTongs(RelicInstance):
    """At the start of your turn, upgrade a random upgradable card in your
    hand for the rest of the combat.

    C# ref: WarpedTongs.AfterSideTurnStart(side == Player): pick a random
    IsUpgradable card in hand with Rng.CombatCardSelection, CardCmd.Upgrade.
    """

    relic_id = RelicId.WARPED_TONGS
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT

    def after_side_turn_start(self, owner: Creature, side: object, combat: CombatState) -> None:
        from sts2_env.core.enums import CombatSide

        if side != CombatSide.PLAYER:
            return
        state = combat.combat_player_state_for(owner)
        if state is None:
            return
        upgradable = [card for card in state.hand if _card_is_upgradable(card)]
        if not upgradable:
            return
        card = combat.combat_card_selection_rng.choice(upgradable)
        combat.upgrade_card(card)


@register_relic
class CultistHeadpiece(RelicInstance):
    """Caw! Caaaw! (No mechanics -- cosmetic banter only in C#.)

    C# ref: CultistHeadpiece.AfterPlayerTurnStart(round 1) only plays a
    speech bubble + sfx; no gameplay effect.
    """

    relic_id = RelicId.CULTIST_HEADPIECE
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT


@register_relic
class FaceOfCleric(RelicInstance):
    """Raise your Max HP by 1 after each combat.

    C# ref: FaceOfCleric.AfterCombatEnd gains MaxHpVar(1). The combat
    creature's max_hp is synced back to the run player after combat by
    RunManager (player.max_hp = combat.player.max_hp).
    """

    relic_id = RelicId.FACE_OF_CLERIC
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    MAX_HP = 1

    def after_combat_end(self, owner: Creature, combat: CombatState) -> None:
        owner.gain_max_hp(self.MAX_HP)


@register_relic
class GremlinVisage(RelicInstance):
    """Start each combat with 1 Weak.

    C# ref: GremlinVisage.AfterRoomEntered(room is CombatRoom) applies
    WeakPower(1) to the owner (SkipNextDurationTick = false, so it ticks
    down at the end of the first turn as usual for pre-combat debuffs).
    """

    relic_id = RelicId.GREMLIN_VISAGE
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    WEAK = 1

    def before_combat_start(self, owner: Creature, combat: CombatState) -> None:
        owner.apply_power(PowerId.WEAK, self.WEAK)


@register_relic
class NlothsHungryFace(RelicInstance):
    """The next non-boss chest you open contains nothing.

    C# ref: NlothsHungryFace counts TreasureRooms entered
    (AfterRoomEntered), IsUsedUp once >= 1, and ShouldGenerateTreasure
    returns false for the first treasure room. RunManager fires
    after_room_entered BEFORE consulting should_generate_treasure, matching
    the C# hook order. (The C# SilverCrucible interplay is skipped -- that
    relic doesn't exist in this simulator.)
    """

    relic_id = RelicId.NLOTHS_HUNGRY_FACE
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT

    _treasure_rooms_entered: int = 0

    @property
    def is_used_up(self) -> bool:
        return self._treasure_rooms_entered >= 1

    def after_room_entered(self, owner: object, room_type: object) -> None:
        resolved = getattr(room_type, "room_type", room_type)
        if resolved == RoomType.TREASURE:
            self._treasure_rooms_entered += 1

    def should_generate_treasure(self, owner: object) -> bool | None:
        if self._treasure_rooms_entered > 1:
            return None
        return False


@register_relic
class SsserpentHead(RelicInstance):
    """Whenever you enter a "?" (unknown) map node, gain 50 gold.

    C# ref: SsserpentHead.AfterRoomEntered: if the current map point's
    PointType is 1 (Unknown) and the owner isn't dead, gain 50 gold.
    Follows the Planisphere pattern (relics/uncommon.py) for detecting
    unknown-node entry.
    """

    relic_id = RelicId.SSSERPENT_HEAD
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    GOLD = 50

    def after_room_entered(self, owner: object, room_type: object) -> None:
        if getattr(owner, "current_hp", 1) <= 0:
            return
        if hasattr(room_type, "is_unknown") and getattr(room_type, "is_unknown"):
            owner.gain_gold(self.GOLD)
            return
        run_state = getattr(owner, "run_state", None)
        visited = getattr(run_state, "visited_map_coords", None)
        act_map = getattr(run_state, "map", None)
        if act_map is None or not visited:
            return
        current = act_map.get_point(visited[-1])
        if current is not None and getattr(current, "point_type", None) == MapPointType.UNKNOWN:
            owner.gain_gold(self.GOLD)
