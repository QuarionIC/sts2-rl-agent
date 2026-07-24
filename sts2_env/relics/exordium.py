""""Acts from the Past" mod -- Exordium event relics.

Decompiled reference: decompiled_mods/ActsFromThePast/ActsFromThePast.Relics/
GoldenIdol.cs. OddMushroom (the other Exordium-event relic) already lives in
sts2_env/relics/shop_event.py alongside the mod's other foundational relics.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.relics.base import RelicId, RelicPool, RelicInstance, RelicRarity
from sts2_env.relics.registry import register_relic

if TYPE_CHECKING:
    from sts2_env.run.run_state import RunState


@register_relic
class GoldenIdol(RelicInstance):
    """Combat gold rewards give 25% more gold (truncated).

    Decompiled reference: GoldenIdol.TryModifyRewards -- for every populated
    GoldReward in a combat room's reward list, Amount += (int)(Amount * 0.25m).
    Non-combat rooms (and other players' rewards) are untouched.
    """

    relic_id = RelicId.GOLDEN_IDOL
    rarity = RelicRarity.EVENT
    pool = RelicPool.EVENT
    GOLD_MULTIPLIER = 0.25

    def modify_rewards(
        self,
        owner: object,
        rewards: list[object],
        room: object | None,
        run_state: object,
    ) -> list[object]:
        from sts2_env.core.enums import RoomType
        from sts2_env.run.reward_objects import GoldReward

        room_type = getattr(room, "room_type", None)
        if room is None or room_type not in {RoomType.MONSTER, RoomType.ELITE, RoomType.BOSS}:
            return rewards
        modified = False
        for reward in rewards:
            if isinstance(reward, GoldReward) and reward.is_populated:
                bonus = int(reward.amount * self.GOLD_MULTIPLIER)
                reward.amount += bonus
                modified = True
        if modified:
            # Return a fresh list so RewardsSet fires after_modifying_rewards
            # (the relic "flash" hook stage), matching AmethystAubergine.
            return list(rewards)
        return rewards
