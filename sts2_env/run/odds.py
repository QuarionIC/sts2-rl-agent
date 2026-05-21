"""Odds systems: Unknown room, card rarity, potion reward.

Implements the rolling/pity counter mechanics from:
- MegaCrit.Sts2.Core.Odds/UnknownMapPointOdds.cs
- MegaCrit.Sts2.Core.Odds/CardRarityOdds.cs
- MegaCrit.Sts2.Core.Odds/PotionRewardOdds.cs
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.enums import RoomType, CardRarity, MapPointType
from sts2_env.core.rng import Rng

if TYPE_CHECKING:
    from sts2_env.run.run_state import RunState


# ── Unknown Map Point Odds ────────────────────────────────────────────

FIRST_RUN_UNKNOWN_EVENT_HISTORY_LIMIT = 2
FIRST_RUN_UNKNOWN_MONSTER_HISTORY_COUNT = 2

C_SHARP_ROOM_TYPE_ORDER = {
    RoomType.MONSTER: 1,
    RoomType.ELITE: 2,
    RoomType.BOSS: 3,
    RoomType.TREASURE: 4,
    RoomType.SHOP: 5,
    RoomType.EVENT: 6,
    RoomType.REST_SITE: 7,
}


class UnknownMapPointOdds:
    """Rolling odds for Unknown ("?") rooms.

    Each non-event result that is NOT rolled gets its odds increased by
    its base value. When it IS rolled, it resets to base.
    """

    BASE_MONSTER = 0.10
    BASE_ELITE = -1.00   # negative = impossible until boosted
    BASE_TREASURE = 0.02
    BASE_SHOP = 0.03

    def __init__(self) -> None:
        self.reset_to_base()

    def reset_to_base(self) -> None:
        self._current: dict[RoomType, float] = {
            RoomType.MONSTER: self.BASE_MONSTER,
            RoomType.ELITE: self.BASE_ELITE,
            RoomType.TREASURE: self.BASE_TREASURE,
            RoomType.SHOP: self.BASE_SHOP,
        }
        self._base: dict[RoomType, float] = {
            RoomType.MONSTER: self.BASE_MONSTER,
            RoomType.ELITE: self.BASE_ELITE,
            RoomType.TREASURE: self.BASE_TREASURE,
            RoomType.SHOP: self.BASE_SHOP,
        }

    def roll(self, rng: Rng, run_state: RunState, blacklist: set[RoomType] | None = None) -> RoomType:
        from sts2_env.run.run_state import FIRST_RUN_COUNT, UNLOCK_STATE_NUMBER_OF_RUNS_KEY

        if blacklist is None:
            blacklist = set()

        if run_state.player.unlock_state.get(UNLOCK_STATE_NUMBER_OF_RUNS_KEY, FIRST_RUN_COUNT) == FIRST_RUN_COUNT:
            unknown_history_count = run_state.count_map_point_history_entries(map_point_type=MapPointType.UNKNOWN)
            if unknown_history_count < FIRST_RUN_UNKNOWN_EVENT_HISTORY_LIMIT:
                return RoomType.EVENT
            if unknown_history_count == FIRST_RUN_UNKNOWN_MONSTER_HISTORY_COUNT:
                return RoomType.MONSTER

        room_types = (set(self._current.keys()) | {RoomType.EVENT}) - blacklist
        for player in run_state.players:
            for card in player.deck:
                room_types = card.modify_unknown_map_point_room_types(run_state, room_types)
        for player in run_state.players:
            for relic in player.get_relic_objects():
                room_types = relic.modify_unknown_map_point_room_types(player, room_types)

        if RoomType.EVENT in room_types:
            result = RoomType.EVENT
        else:
            result = min(room_types, key=C_SHARP_ROOM_TYPE_ORDER.__getitem__)

        roll_val = rng.next_float()
        cumulative = 0.0

        for room_type, odds in self._current.items():
            if room_type not in room_types or odds < 0:
                continue
            cumulative += odds
            if roll_val <= cumulative:
                result = room_type
                break

        # Update odds for next roll
        for room_type, base_odds in self._base.items():
            if room_type == result:
                self._current[room_type] = base_odds  # reset rolled type
            elif room_type in room_types:
                odds_increase = base_odds
                for modifier in run_state.modifiers:
                    odds_increase = modifier.modify_odds_increase_for_unrolled_room_type(room_type, odds_increase)
                self._current[room_type] += odds_increase  # increase un-rolled

        return result


# ── Card Rarity Odds ──────────────────────────────────────────────────

class CardRarityOdds:
    """Card rarity pity counter system.

    Maintains a CurrentValue offset that shifts rare odds over time.
    Boss encounters ignore pity. Shop rolls don't change pity.
    """

    def __init__(self, ascension_level: int = 0) -> None:
        self.ascension_level = ascension_level
        self.current_value: float = -0.05
        a7 = ascension_level >= 7

        # Base odds tables
        self.regular_odds = {
            "common": 0.615 if a7 else 0.60,
            "uncommon": 0.37,
            "rare": 0.0149 if a7 else 0.03,
        }
        self.elite_odds = {
            "common": 0.549 if a7 else 0.50,
            "uncommon": 0.40,
            "rare": 0.05 if a7 else 0.10,
        }
        self.boss_odds = {
            "common": 0.00,
            "uncommon": 0.00,
            "rare": 1.00,
        }
        self.shop_odds = {
            "common": 0.585 if a7 else 0.54,
            "uncommon": 0.37,
            "rare": 0.045 if a7 else 0.09,
        }

        self.rarity_growth = 0.005 if a7 else 0.01

    def _get_odds(self, context: str) -> dict[str, float]:
        if context == "boss":
            return self.boss_odds
        if context == "elite":
            return self.elite_odds
        if context == "shop":
            return self.shop_odds
        return self.regular_odds

    def roll(self, rng: Rng, context: str = "regular") -> CardRarity:
        """Roll for a card rarity.

        Args:
            rng: RNG stream.
            context: "regular", "elite", "boss", or "shop".

        Returns:
            CardRarity.COMMON, UNCOMMON, or RARE.
        """
        odds = self._get_odds(context)
        offset = 0.0 if context == "boss" else self.current_value
        roll_val = rng.next_float()

        rare_threshold = odds["rare"] + offset

        if roll_val < rare_threshold:
            result = CardRarity.RARE
            if context != "shop":
                self.current_value = -0.05  # reset on rare hit
        elif roll_val < odds["uncommon"] + rare_threshold:
            result = CardRarity.UNCOMMON
            if context != "shop":
                self.current_value = min(self.current_value + self.rarity_growth, 0.4)
        else:
            result = CardRarity.COMMON
            if context != "shop":
                self.current_value = min(self.current_value + self.rarity_growth, 0.4)

        return result

    def roll_without_changing_odds(self, rng: Rng, context: str = "shop") -> CardRarity:
        """Roll rarity for merchant cards -- consumes RNG but no pity change.

        Matches C# RollWithoutChangingFutureOdds: still applies current
        pity offset to the rare threshold.
        """
        odds = self._get_odds(context)
        roll_val = rng.next_float()
        rare_threshold = odds["rare"] + self.current_value
        if roll_val < rare_threshold:
            return CardRarity.RARE
        if roll_val < odds["uncommon"] + rare_threshold:
            return CardRarity.UNCOMMON
        return CardRarity.COMMON

    def roll_with_base_odds(self, rng: Rng, context: str = "regular") -> CardRarity:
        odds = self._get_odds(context)
        roll_val = rng.next_float()
        if roll_val < odds["rare"]:
            return CardRarity.RARE
        if roll_val < odds["uncommon"]:
            return CardRarity.UNCOMMON
        return CardRarity.COMMON


# ── Potion Reward Odds ────────────────────────────────────────────────

class PotionRewardOdds:
    """Potion drop odds oscillating around 40-50%.

    Each combat: if potion drops, reduce future odds by 10%.
    If no drop, increase by 10%. Elite fights get +12.5% bonus.
    """

    INITIAL_VALUE = 0.40
    TARGET = 0.50
    ELITE_BONUS = 0.25
    SWING = 0.10

    def __init__(self) -> None:
        self.current_value: float = self.INITIAL_VALUE

    def roll(self, rng: Rng, is_elite: bool = False, force: bool = False) -> bool:
        """Roll whether a potion drops. Returns True if yes.

        Matches C# PotionRewardOdds.Roll: odds update uses base value
        (without elite bonus), but drop check includes elite bonus.
        """
        saved_value = self.current_value
        roll_val = rng.next_float()

        # Update odds based on raw roll vs base value (no elite bonus)
        if roll_val < saved_value or force:
            self.current_value -= self.SWING
        else:
            self.current_value += self.SWING

        # Return check includes elite bonus
        elite_bonus = self.ELITE_BONUS if is_elite else 0.0
        return force or roll_val < saved_value + elite_bonus * 0.50
