""""Acts from the Past" mod -- shared shrine events.

Implemented against the decompiled mod source in
``decompiled_mods/ActsFromThePast/ActsFromThePast.SharedEvents/*.cs`` (plus
``ActsFromThePast.Minigames/{MatchAndKeepMinigame,WheelSpinMinigame}.cs``).

ALL 15 of the mod's SharedEvents implement IShrineEvent (there are no
"regular" shared events in this batch), so every event here carries
``is_shrine = True``; the one-time/repeatable split follows each class's
``IShrineEvent.IsOneTimeEvent`` (C# interface default: false == repeatable):

- one-time:   BonfireSpirits, Duplicator, FaceTrader, Lab, OminousForge,
              TheDivineFountain, TheWomanInBlue, WeMeetAgain, DesignerInSpire
- repeatable: GoldenShrine, MatchAndKeep, Purifier, Transmogrifier,
              UpgradeShrine, WheelOfChange

Repeatable shrines bypass the visited-events exclusion (see
run/events.py::_event_blocked_by_visited, mirroring the mod's
RepeatableShrineValidityPatch), and shrines are interleaved into legacy act
event pools at 25% per slot (run/events.py::build_legacy_event_pool,
mirroring ShrinePatches.EventPoolPatch).

CLASSIC MODE ONLY (``RebalancedMode=False``): every rebalanced branch is a
dead branch here (Duplicator's Kneel, Lab's Ransack, Purifier's Kneel,
Transmogrifier's Kneel, UpgradeShrine's Kneel, TheDivineFountain's Bathe,
GoldenShrine's no-leave, MatchAndKeep's Guilty pairs, the rebalanced
IsAllowed variants). ``AllowLegacySharedEventsInNonLegacyActs=False`` means
every event here is ``is_legacy_exclusive`` -- they never appear in vanilla
acts (enforced by run/events.py::event_allowed_in_act).

Act restrictions (IActRestricted, 1-based act numbers == slot index + 1):
FaceTrader -> acts 1-2 (Exordium/TheCity slots); DesignerInSpire -> acts
2-3 (TheCity/TheBeyond slots).

Number corrections vs. the planning spec (all read from source):

- BonfireSpirits: curse -> SpiritPoop relic; BASIC -> nothing; COMMON ->
  heal 5; UNCOMMON -> heal to FULL; RARE -> +10 Max HP then full heal
  (spec guessed "max HP?"); any other rarity (e.g. EVENT) -> heal 5.
- FaceTrader: Touch = max(1, MaxHp // 10) damage for 50 gold; Trade = a
  random face relic you don't own yet (Circlet if you own all five).
- GoldenShrine: Pray +50 gold free; Desecrate +275 gold + Regret curse.
- Lab: 2 potion rewards (not 3).
- TheDivineFountain: Drink removes ALL removable curses (no max-HP variant
  in classic); gated on having a removable non-Guilty curse.
- TheWomanInBlue: 1/2/3 potions at 20/30/40 gold; LEAVING costs
  ceil(5% Max HP) damage (she punches you). Gate: gold >= 50.
- WeMeetAgain: give a random-rolled 50-150 gold sum (capped by your gold;
  option locked under 50 gold), OR a random potion you hold, OR a random
  non-basic non-curse card -- each pays one normally-rolled relic. The
  fourth option (Attack) does nothing.
- WheelOfChange: equal 1/6 odds -- gold 100/200/300 by act slot / random
  relic / FULL heal / Decay curse (spec said "a curse") / remove a chosen
  card / damage = trunc(15% Max HP).
- DesignerInSpire: Adjustments 50g (coin-flip: upgrade 1 chosen OR 2
  random); Clean Up 75g (coin-flip: remove 1 chosen OR transform 2
  chosen); Full Service 110g (remove 1 chosen + upgrade 1 random); Punch
  (5 damage, free). Gate: gold >= 75.

MatchAndKeep modeling decision: implemented as the FAITHFUL interactive
memory game (12 face-down cards = 6 pairs: rare/uncommon/common from your
character pool, 2 random curses, 1 non-Strike/Defend basic; 5 attempts of 2
flips; a match adds ONE copy of the card to your deck and both attempts-
counters/board state track the C# screen logic exactly). The agent picks
which position to flip each step; option labels reveal the identity of
every card it has already seen (perfect memory), so the optimal policy is
learnable. Card identities are compared by card_id, like the C# ModelId
comparison (two identical rolled curses can cross-match).

Sim deviation (documented): the curse pairs draw from the vanilla
modifier-generatable curse pool; the mod would also include its own Pain /
Necronomicurse entries, but those are excluded from the sim's byte-for-byte
vanilla curse pool constants.
"""

from __future__ import annotations

import math
from typing import TYPE_CHECKING

from sts2_env.cards.factory import (
    create_card,
    eligible_character_cards,
    eligible_registered_cards,
)
from sts2_env.cards.status import make_decay, make_pain, make_regret
from sts2_env.core.card_pools import CardPoolId
from sts2_env.core.enums import CardId, CardRarity, CardTag, CardType
from sts2_env.events.shared import (
    _event_result_with_rewards,
    _remove_selected_cards,
    _should_defer_event_rewards,
    _transform_selected_cards,
    _upgrade_n_cards,
    _upgrade_selected_cards,
)
from sts2_env.relics.base import RelicId
from sts2_env.run.events import EventModel, EventOption, EventResult, register_event
from sts2_env.run.reward_objects import (
    AddCardsReward,
    DuplicateCardReward,
    PotionReward,
    RelicReward,
    RemoveCardReward,
    TransformCardsReward,
    UpgradeCardsReward,
)

if TYPE_CHECKING:
    from sts2_env.cards.base import CardInstance
    from sts2_env.run.run_state import RunState


class _ShrineEvent(EventModel):
    """Base for all "Acts from the Past" shared shrine events."""

    is_shared = True
    is_legacy_exclusive = True
    is_shrine = True


# ── BonfireSpirits ──────────────────────────────────────────────────

class BonfireSpirits(_ShrineEvent):
    """Offer a card to the spirits; the reward scales with its rarity:
    curse -> Spirit Poop relic; basic -> nothing; common -> heal 5;
    uncommon -> full heal; rare -> +10 Max HP and full heal.
    """

    event_id = "BonfireSpirits"
    is_one_time_event = True
    COMMON_HEAL = 5
    RARE_MAX_HP_GAIN = 10

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption("continue", "Approach", "Approach the bonfire")]

    def _resolve_offer(self, selected: list[CardInstance], run_state: RunState) -> EventResult:
        if not selected:
            return EventResult(finished=True, description="Offered nothing.")
        card = selected[0]
        rarity = card.rarity
        is_curse = card.card_type == CardType.CURSE
        _remove_selected_cards([card], run_state)
        player = run_state.player
        if is_curse:
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "The spirits reject the curse... and leave a gift.",
                    [RelicReward(player.player_id, relic_id=RelicId.SPIRIT_POOP.name)],
                )
            player.obtain_relic(RelicId.SPIRIT_POOP.name)
            return EventResult(
                finished=True,
                description="The spirits reject the curse... and leave a gift.",
            )
        if rarity == CardRarity.BASIC:
            return EventResult(finished=True, description="The spirits are unmoved.")
        if rarity == CardRarity.UNCOMMON:
            healed = player.heal(player.max_hp)
            return EventResult(finished=True, description=f"The spirits dance! Healed {healed} HP.")
        if rarity == CardRarity.RARE:
            player.gain_max_hp(self.RARE_MAX_HP_GAIN)
            healed = player.heal(player.max_hp)
            return EventResult(
                finished=True,
                description=f"The spirits rejoice! +{self.RARE_MAX_HP_GAIN} Max HP, healed {healed} HP.",
            )
        # COMMON and anything else (e.g. EVENT rarity): heal 5.
        healed = player.heal(self.COMMON_HEAL)
        return EventResult(finished=True, description=f"The spirits stir. Healed {healed} HP.")

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="The spirits circle the bonfire, expectant.",
                next_options=[EventOption("offer", "Offer", "Offer a card from your deck")],
            )
        if option_id == "offer":
            candidates = run_state.player.removable_deck_cards()
            return self.request_card_choice(
                prompt="Choose a card to offer",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: self._resolve_offer(selected, run_state),
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to offer.",
            )
        return EventResult(finished=True, description="Left the bonfire.")


register_event(BonfireSpirits())


# ── Duplicator ──────────────────────────────────────────────────────

class Duplicator(_ShrineEvent):
    """Pray: duplicate a card in your deck. Leave: nothing."""

    event_id = "Duplicator"
    is_one_time_event = True

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pray", "Pray", "Duplicate a card in your deck"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pray":
            player = run_state.player
            candidates = player.duplicable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Duplicated a card.",
                    [DuplicateCardReward(player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to duplicate",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    selected and player.add_card_instance_to_deck(
                        player.clone_card_for_deck(selected[0])
                    ),
                    EventResult(finished=True, description="Duplicated a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to duplicate.",
            )
        return EventResult(finished=True, description="Left the shrine.")


register_event(Duplicator())


# ── FaceTrader ──────────────────────────────────────────────────────

class FaceTrader(_ShrineEvent):
    """Touch: take 10% Max HP damage (min 1), gain 50 gold.
    Trade: gain a random face relic you don't own (Circlet if all owned).
    Leave: nothing. Only in act slots 1-2; single-player only.
    """

    event_id = "FaceTrader"
    is_one_time_event = True
    allowed_act_numbers = (1, 2)
    GOLD = 50
    _FACE_RELIC_IDS = (
        RelicId.CULTIST_HEADPIECE,
        RelicId.FACE_OF_CLERIC,
        RelicId.GREMLIN_VISAGE,
        RelicId.NLOTHS_HUNGRY_FACE,
        RelicId.SSSERPENT_HEAD,
    )

    def __init__(self) -> None:
        self._damage = 1

    def is_allowed(self, run_state: RunState) -> bool:
        # C#: never in multiplayer; classic mode has no further gate.
        return len(run_state.players) == 1

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: (int)(MaxHp / 10m), minimum 1.
        self._damage = max(1, int(run_state.player.max_hp / 10))

    def _roll_face_relic(self, run_state: RunState) -> str:
        owned = set(run_state.player.relics)
        pool = [relic_id.name for relic_id in self._FACE_RELIC_IDS if relic_id.name not in owned]
        if not pool:
            return RelicId.CIRCLET.name
        return self.get_rng(run_state).choice(pool)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("continue", "Approach", "A hooded figure beckons")]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="The Face Trader sizes you up.",
                next_options=[
                    EventOption("touch", "Touch",
                                f"Take {self._damage} damage, gain {self.GOLD} gold"),
                    EventOption("trade", "Trade", "Gain a random face relic"),
                    EventOption("leave", "Leave", "Nothing happens"),
                ],
            )
        if option_id == "touch":
            run_state.player.lose_hp(self._damage)
            run_state.player.gain_gold(self.GOLD)
            return EventResult(
                finished=True,
                description=f"Took {self._damage} damage, gained {self.GOLD} gold.",
            )
        if option_id == "trade":
            relic_id = self._roll_face_relic(run_state)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Traded faces: gained {relic_id}.",
                    [RelicReward(run_state.player.player_id, relic_id=relic_id)],
                )
            run_state.player.obtain_relic(relic_id)
            return EventResult(finished=True, description=f"Traded faces: gained {relic_id}.")
        return EventResult(finished=True, description="Kept your face.")


register_event(FaceTrader())


# ── GoldenShrine ────────────────────────────────────────────────────

class GoldenShrine(_ShrineEvent):
    """Pray: gain 50 gold. Desecrate: gain 275 gold, gain a Regret curse.
    Leave: nothing. (Repeatable shrine.)
    """

    event_id = "GoldenShrine"
    GOLD = 50
    CURSE_GOLD = 275

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pray", "Pray", f"Gain {self.GOLD} gold"),
            EventOption("desecrate", "Desecrate",
                        f"Gain {self.CURSE_GOLD} gold and a Regret curse"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pray":
            run_state.player.gain_gold(self.GOLD)
            return EventResult(finished=True, description=f"Gained {self.GOLD} gold.")
        if option_id == "desecrate":
            run_state.player.gain_gold(self.CURSE_GOLD)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Gained {self.CURSE_GOLD} gold and a Regret curse.",
                    [AddCardsReward(run_state.player.player_id, [make_regret()])],
                )
            run_state.player.add_card_instance_to_deck(make_regret())
            return EventResult(
                finished=True,
                description=f"Gained {self.CURSE_GOLD} gold and a Regret curse.",
            )
        return EventResult(finished=True, description="Left the shrine.")


register_event(GoldenShrine())


# ── Lab ─────────────────────────────────────────────────────────────

class Lab(_ShrineEvent):
    """Search: find 2 potions. (No other classic option.)"""

    event_id = "Lab"
    is_one_time_event = True
    POTION_COUNT = 2

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption("search", "Search", f"Find {self.POTION_COUNT} potions")]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "search":
            player_id = run_state.player.player_id
            return _event_result_with_rewards(
                f"Found {self.POTION_COUNT} potions.",
                [PotionReward(player_id) for _ in range(self.POTION_COUNT)],
            )
        return EventResult(finished=True, description="Left the lab.")


register_event(Lab())


# ── MatchAndKeep ────────────────────────────────────────────────────

class MatchAndKeep(_ShrineEvent):
    """Memory-match minigame: 12 face-down cards (6 pairs -- rare /
    uncommon / common from your pool, 2 random curses, 1 non-Strike/Defend
    basic). 5 attempts of 2 flips; each match adds one copy of the card to
    your deck. (Repeatable shrine.)
    """

    event_id = "MatchAndKeep"
    MAX_ATTEMPTS = 5
    PAIR_COUNT = 6

    def __init__(self) -> None:
        self._cards: list[CardInstance] = []
        self._matched: list[bool] = []
        self._known: set[int] = set()
        self._attempts_left = self.MAX_ATTEMPTS
        self._matches = 0
        self._first_flip: int | None = None

    # -- board construction (MatchAndKeepMinigame.GenerateCards) ------

    def _roll_canonicals(self, run_state: RunState) -> list[CardInstance]:
        rng = self.get_rng(run_state)
        character_id = run_state.player.character_id

        def _character_card(rarity: CardRarity) -> CardInstance:
            ids = eligible_character_cards(character_id, rarity=rarity, generation_context=None)
            if not ids:
                ids = eligible_character_cards(character_id, rarity=CardRarity.COMMON, generation_context=None)
            return create_card(rng.choice(ids))

        canonicals = [
            _character_card(CardRarity.RARE),
            _character_card(CardRarity.UNCOMMON),
            _character_card(CardRarity.COMMON),
        ]
        curse_ids = eligible_registered_cards(
            card_pool=CardPoolId.CURSE, generation_context="modifier"
        )
        for _ in range(2):
            canonicals.append(create_card(rng.choice(curse_ids)))
        # Basic pair: non-Strike/Defend basic from the character pool;
        # fallback to a common card (mirrors the C# starting-deck fallback).
        basic_ids = [
            card_id
            for card_id in eligible_character_cards(
                character_id, rarity=CardRarity.BASIC, generation_context=None
            )
        ]
        basic_cards = [create_card(card_id) for card_id in basic_ids]
        basic_cards = [
            card for card in basic_cards
            if CardTag.STRIKE not in card.tags and CardTag.DEFEND not in card.tags
        ]
        if basic_cards:
            canonicals.append(rng.choice(basic_cards))
        else:
            canonicals.append(_character_card(CardRarity.COMMON))
        return canonicals

    def calculate_vars(self, run_state: RunState) -> None:
        rng = self.get_rng(run_state)
        canonicals = self._roll_canonicals(run_state)
        cards: list[CardInstance] = []
        for canonical in canonicals:
            cards.append(create_card(canonical.card_id))
            cards.append(create_card(canonical.card_id))
        rng.shuffle(cards)
        self._cards = cards
        self._matched = [False] * len(cards)
        self._known = set()
        self._attempts_left = self.MAX_ATTEMPTS
        self._matches = 0
        self._first_flip = None

    # -- option pages --------------------------------------------------

    def _flip_options(self) -> list[EventOption]:
        options: list[EventOption] = []
        for index, card in enumerate(self._cards):
            if self._matched[index] or index == self._first_flip:
                continue
            if index in self._known:
                label = f"Flip {index + 1} ({card.card_id.name})"
            else:
                label = f"Flip {index + 1} (face down)"
            options.append(EventOption(f"flip_{index}", label, "Flip this card"))
        return options

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("play", "Play", f"{self.MAX_ATTEMPTS} attempts to match pairs")]

    def _board_state(self) -> str:
        return f"{self._attempts_left} attempts left, {self._matches} pairs matched."

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "play":
            return EventResult(
                finished=False,
                description=f"12 cards lie face down. {self._board_state()}",
                next_options=self._flip_options(),
            )
        if option_id.startswith("flip_"):
            index = int(option_id.split("_")[1])
            if index < 0 or index >= len(self._cards) or self._matched[index]:
                return EventResult(finished=True, description="Invalid flip.")
            self._known.add(index)
            card = self._cards[index]
            if self._first_flip is None:
                self._first_flip = index
                return EventResult(
                    finished=False,
                    description=f"Flipped {card.card_id.name}. Choose a second card.",
                    next_options=self._flip_options(),
                )
            first_index = self._first_flip
            first_card = self._cards[first_index]
            self._first_flip = None
            self._attempts_left -= 1
            if first_card.card_id == card.card_id:
                self._matched[first_index] = True
                self._matched[index] = True
                self._matches += 1
                kept = create_card(card.card_id)
                if _should_defer_event_rewards(run_state):
                    run_state.pending_rewards.append(
                        AddCardsReward(run_state.player.player_id, [kept])
                    )
                else:
                    run_state.player.add_card_instance_to_deck(kept)
                outcome = f"Matched {card.card_id.name}! Added a copy to your deck."
            else:
                outcome = f"{first_card.card_id.name} and {card.card_id.name} don't match."
            if self._matches >= self.PAIR_COUNT or self._attempts_left <= 0:
                return EventResult(
                    finished=True,
                    description=f"{outcome} Game over: {self._matches} pairs kept.",
                )
            return EventResult(
                finished=False,
                description=f"{outcome} {self._board_state()}",
                next_options=self._flip_options(),
            )
        return EventResult(finished=True, description="Walked away from the game.")


register_event(MatchAndKeep())


# ── OminousForge ────────────────────────────────────────────────────

class OminousForge(_ShrineEvent):
    """Forge: upgrade a card. Rummage: gain the Warped Tongs relic and a
    Pain curse. Leave: nothing.
    """

    event_id = "OminousForge"
    is_one_time_event = True

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.upgradable_deck_cards() for player in run_state.players)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("forge", "Forge", "Upgrade a card"),
            EventOption("rummage", "Rummage",
                        "Gain Warped Tongs and a Pain curse"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        player = run_state.player
        if option_id == "forge":
            candidates = player.upgradable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Upgraded a card.",
                    [UpgradeCardsReward(player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to upgrade",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _upgrade_selected_cards(selected, run_state),
                    EventResult(finished=True, description="Upgraded a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to upgrade.",
            )
        if option_id == "rummage":
            # C# order: Pain curse first, then Warped Tongs.
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Gained a Pain curse and the Warped Tongs.",
                    [
                        AddCardsReward(player.player_id, [make_pain()]),
                        RelicReward(player.player_id, relic_id=RelicId.WARPED_TONGS.name),
                    ],
                )
            player.add_card_instance_to_deck(make_pain())
            player.obtain_relic(RelicId.WARPED_TONGS.name)
            return EventResult(
                finished=True,
                description="Gained a Pain curse and the Warped Tongs.",
            )
        return EventResult(finished=True, description="Left the forge.")


register_event(OminousForge())


# ── Purifier ────────────────────────────────────────────────────────

class Purifier(_ShrineEvent):
    """Pray: remove a card from your deck. Leave: nothing.
    (Repeatable shrine.)
    """

    event_id = "Purifier"

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pray", "Pray", "Remove a card from your deck",
                        enabled=bool(run_state.player.removable_deck_cards())),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pray":
            candidates = run_state.player.removable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Removed a card.",
                    [RemoveCardReward(run_state.player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to remove",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _remove_selected_cards(selected, run_state),
                    EventResult(finished=True, description="Removed a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to remove.",
            )
        return EventResult(finished=True, description="Left the shrine.")


register_event(Purifier())


# ── TheDivineFountain ───────────────────────────────────────────────

class TheDivineFountain(_ShrineEvent):
    """Drink: remove ALL removable curses from your deck. Leave: nothing."""

    event_id = "TheDivineFountain"
    is_one_time_event = True

    def is_allowed(self, run_state: RunState) -> bool:
        # C#: every player has a removable curse that isn't Guilty.
        return all(
            any(
                card.card_type == CardType.CURSE
                and card.is_removable
                and card.card_id != CardId.GUILTY
                for card in player.deck
            )
            for player in run_state.players
        )

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("drink", "Drink", "Remove ALL curses from your deck"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "drink":
            curses = [
                card for card in run_state.player.deck
                if card.card_type == CardType.CURSE and card.is_removable
            ]
            removed = _remove_selected_cards(curses, run_state)
            return EventResult(
                finished=True,
                description=f"The water burns away {removed} curse(s).",
            )
        return EventResult(finished=True, description="Left the fountain.")


register_event(TheDivineFountain())


# ── TheWomanInBlue ──────────────────────────────────────────────────

class TheWomanInBlue(_ShrineEvent):
    """Buy 1/2/3 potions for 20/30/40 gold. Leaving without buying costs
    5% Max HP (rounded up) -- she punches you.
    """

    event_id = "TheWomanInBlue"
    is_one_time_event = True
    COST_1 = 20
    COST_2 = 30
    COST_3 = 40
    PUNCH_PERCENT = 0.05
    MIN_GOLD = 50

    def __init__(self) -> None:
        self._punch_damage = 1

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.gold >= self.MIN_GOLD for player in run_state.players)

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: (int)Math.Ceiling(MaxHp * 0.05m).
        self._punch_damage = math.ceil(run_state.player.max_hp * self.PUNCH_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        gold = run_state.player.gold
        return [
            EventOption("buy_1", f"Buy 1 Potion ({self.COST_1}g)", "Gain a potion",
                        enabled=gold >= self.COST_1),
            EventOption("buy_2", f"Buy 2 Potions ({self.COST_2}g)", "Gain 2 potions",
                        enabled=gold >= self.COST_2),
            EventOption("buy_3", f"Buy 3 Potions ({self.COST_3}g)", "Gain 3 potions",
                        enabled=gold >= self.COST_3),
            EventOption("leave", "Leave", f"Take {self._punch_damage} damage"),
        ]

    def _buy(self, run_state: RunState, count: int, cost: int) -> EventResult:
        run_state.player.lose_gold(cost)
        player_id = run_state.player.player_id
        return _event_result_with_rewards(
            f"Paid {cost} gold for {count} potion(s).",
            [PotionReward(player_id) for _ in range(count)],
        )

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "buy_1":
            return self._buy(run_state, 1, self.COST_1)
        if option_id == "buy_2":
            return self._buy(run_state, 2, self.COST_2)
        if option_id == "buy_3":
            return self._buy(run_state, 3, self.COST_3)
        # leave: she punches you.
        run_state.player.lose_hp(self._punch_damage)
        return EventResult(
            finished=True,
            description=f"She punches you for {self._punch_damage} damage.",
        )


register_event(TheWomanInBlue())


# ── Transmogrifier ──────────────────────────────────────────────────

class Transmogrifier(_ShrineEvent):
    """Pray: transform a card. Leave: nothing. (Repeatable shrine.)"""

    event_id = "Transmogrifier"

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pray", "Pray", "Transform a card",
                        enabled=bool(run_state.player.transformable_deck_cards())),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pray":
            candidates = run_state.player.transformable_deck_cards()
            if not candidates:
                return EventResult(finished=True, description="Transformed a card.")
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Transformed a card.",
                    [TransformCardsReward(run_state.player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to transform",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _transform_selected_cards(selected, run_state, rng=self.get_rng(run_state)),
                    EventResult(finished=True, description="Transformed a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to transform.",
            )
        return EventResult(finished=True, description="Left the shrine.")


register_event(Transmogrifier())


# ── UpgradeShrine ───────────────────────────────────────────────────

class UpgradeShrine(_ShrineEvent):
    """Pray: upgrade a card. Leave: nothing. (Repeatable shrine.)"""

    event_id = "UpgradeShrine"

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pray", "Pray", "Upgrade a card",
                        enabled=bool(run_state.player.upgradable_deck_cards())),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pray":
            candidates = run_state.player.upgradable_deck_cards()
            if not candidates:
                return EventResult(finished=True, description="Upgraded a card.")
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Upgraded a card.",
                    [UpgradeCardsReward(run_state.player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to upgrade",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _upgrade_selected_cards(selected, run_state),
                    EventResult(finished=True, description="Upgraded a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to upgrade.",
            )
        return EventResult(finished=True, description="Left the shrine.")


register_event(UpgradeShrine())


# ── WeMeetAgain ─────────────────────────────────────────────────────

class WeMeetAgain(_ShrineEvent):
    """Ranwid again! Give him a random potion / 50-150 gold / a random
    non-basic non-curse card; each pays out a (normally rolled) relic.
    Attack: nothing happens.
    """

    event_id = "WeMeetAgain"
    is_one_time_event = True
    MIN_GOLD = 50
    MAX_GOLD = 150

    def __init__(self) -> None:
        self._potion_index: int | None = None
        self._card: CardInstance | None = None
        self._gold_amount = 0

    def calculate_vars(self, run_state: RunState) -> None:
        rng = self.get_rng(run_state)
        player = run_state.player
        self._potion_index = (
            rng.next_int_exclusive(0, len(player.potions)) if player.potions else None
        )
        candidates = [
            card for card in player.deck
            if card.rarity != CardRarity.BASIC and card.card_type != CardType.CURSE
        ]
        self._card = rng.choice(candidates) if candidates else None
        if player.gold < self.MIN_GOLD:
            self._gold_amount = 0
        elif player.gold > self.MAX_GOLD:
            self._gold_amount = rng.next_int_exclusive(self.MIN_GOLD, self.MAX_GOLD + 1)
        else:
            self._gold_amount = rng.next_int_exclusive(self.MIN_GOLD, player.gold + 1)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        options: list[EventOption] = []
        if self._potion_index is not None and self._potion_index < len(player.potions):
            potion = player.potions[self._potion_index]
            options.append(EventOption(
                "give_potion", "Give Potion",
                f"Give {getattr(potion, 'potion_id', 'a potion')}, gain a relic",
            ))
        else:
            options.append(EventOption("give_potion", "Give Potion (Locked)",
                                       "No potion to give", enabled=False))
        if self._gold_amount > 0:
            options.append(EventOption("give_gold", f"Give Gold ({self._gold_amount}g)",
                                       f"Give {self._gold_amount} gold, gain a relic"))
        else:
            options.append(EventOption("give_gold", "Give Gold (Locked)",
                                       "Not enough gold", enabled=False))
        if self._card is not None:
            options.append(EventOption(
                "give_card", "Give Card",
                f"Give {self._card.card_id.name}, gain a relic",
            ))
        else:
            options.append(EventOption("give_card", "Give Card (Locked)",
                                       "No suitable card", enabled=False))
        options.append(EventOption("attack", "Attack", "Nothing happens"))
        return options

    def _relic_result(self, run_state: RunState, description: str) -> EventResult:
        if _should_defer_event_rewards(run_state):
            return _event_result_with_rewards(
                description, [RelicReward(run_state.player.player_id)]
            )
        from sts2_env.events.shared import _obtain_random_relics

        obtained = _obtain_random_relics(run_state, 1)
        relic_name = obtained[0] if obtained else "a relic"
        return EventResult(finished=True, description=f"{description} Gained {relic_name}.")

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        if option_id == "give_potion" and self._potion_index is not None:
            if self._potion_index < len(player.potions):
                player.potions.pop(self._potion_index)
            return self._relic_result(run_state, "Gave a potion.")
        if option_id == "give_gold" and self._gold_amount > 0:
            player.lose_gold(self._gold_amount)
            return self._relic_result(run_state, f"Gave {self._gold_amount} gold.")
        if option_id == "give_card" and self._card is not None:
            _remove_selected_cards([self._card], run_state)
            return self._relic_result(run_state, f"Gave {self._card.card_id.name}.")
        return EventResult(finished=True, description="Ranwid dodges and flees. Nothing happens.")


register_event(WeMeetAgain())


# ── WheelOfChange ───────────────────────────────────────────────────

class WheelOfChange(_ShrineEvent):
    """Spin the wheel -- equal 1/6 odds: gold (100/200/300 by act slot) /
    a random relic / full heal / a Decay curse / remove a card / take 15%
    Max HP damage. (Repeatable shrine.)
    """

    event_id = "WheelOfChange"
    HP_LOSS_PERCENT = 0.15
    _GOLD_BY_ACT = (100, 200, 300)
    _RESULT_NAMES = ("gold", "relic", "heal", "curse", "remove", "damage")

    def __init__(self) -> None:
        self._hp_loss = 0
        self._gold = 100
        self._result: int | None = None

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: (int)(MaxHp * 0.15m) -- truncation, not rounding.
        self._hp_loss = int(run_state.player.max_hp * self.HP_LOSS_PERCENT)
        act_index = run_state.current_act_index
        self._gold = self._GOLD_BY_ACT[min(act_index, len(self._GOLD_BY_ACT) - 1)]
        self._result = None

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("play", "Spin", "Spin the Wheel of Change")]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        if option_id == "play":
            self._result = self.get_rng(run_state).next_int_exclusive(0, 6)
            name = self._RESULT_NAMES[self._result]
            return EventResult(
                finished=False,
                description=f"The wheel slows... and lands on {name.upper()}!",
                next_options=[EventOption("claim", "Continue", f"Accept the {name} result")],
            )
        if option_id == "claim" and self._result is not None:
            result = self._result
            if result == 0:
                player.gain_gold(self._gold)
                return EventResult(finished=True, description=f"Gained {self._gold} gold.")
            if result == 1:
                return _event_result_with_rewards(
                    "The wheel grants a relic.",
                    [RelicReward(player.player_id)],
                )
            if result == 2:
                healed = player.heal(player.max_hp)
                return EventResult(finished=True, description=f"Healed {healed} HP.")
            if result == 3:
                if _should_defer_event_rewards(run_state):
                    return _event_result_with_rewards(
                        "Gained a Decay curse.",
                        [AddCardsReward(player.player_id, [make_decay()])],
                    )
                player.add_card_instance_to_deck(make_decay())
                return EventResult(finished=True, description="Gained a Decay curse.")
            if result == 4:
                candidates = player.removable_deck_cards()
                if _should_defer_event_rewards(run_state):
                    return _event_result_with_rewards(
                        "Removed a card.",
                        [RemoveCardReward(player.player_id, count=1, cards=candidates)],
                    )
                return self.request_card_choice(
                    prompt="Choose a card to remove",
                    cards=candidates,
                    source_pile="deck",
                    resolver=lambda selected: (
                        _remove_selected_cards(selected, run_state),
                        EventResult(finished=True, description="Removed a card."),
                    )[-1],
                    allow_skip=False,
                    min_count=1,
                    max_count=1,
                    description="Choose a card to remove.",
                )
            player.lose_hp(self._hp_loss)
            return EventResult(
                finished=True,
                description=f"The wheel's spikes hit for {self._hp_loss} damage.",
            )
        return EventResult(finished=True, description="Walked away from the wheel.")


register_event(WheelOfChange())


# ── DesignerInSpire ─────────────────────────────────────────────────

class DesignerInSpire(_ShrineEvent):
    """Pay the Designer for deck work (both service styles are coin-flipped
    when the event spawns): Adjustments 50g (upgrade 1 chosen OR 2 random),
    Clean Up 75g (remove 1 chosen OR transform 2 chosen), Full Service 110g
    (remove 1 chosen + upgrade 1 random), or Punch (5 damage, free).
    Only in act slots 2-3.
    """

    event_id = "DesignerInSpire"
    is_one_time_event = True
    allowed_act_numbers = (2, 3)
    ADJUST_COST = 50
    CLEANUP_COST = 75
    FULL_SERVICE_COST = 110
    PUNCH_DAMAGE = 5
    MIN_GOLD = 75

    def __init__(self) -> None:
        self._adjust_upgrades_one = True
        self._cleanup_removes = True

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.gold >= self.MIN_GOLD for player in run_state.players)

    def calculate_vars(self, run_state: RunState) -> None:
        rng = self.get_rng(run_state)
        self._adjust_upgrades_one = rng.next_int_exclusive(0, 2) == 0
        self._cleanup_removes = rng.next_int_exclusive(0, 2) == 0

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("continue", "Approach", "The Designer looks you over")]

    def _main_options(self, run_state: RunState) -> list[EventOption]:
        player = run_state.player
        gold = player.gold
        upgradable = bool(player.upgradable_deck_cards())
        removable = player.removable_deck_cards()
        adjust_label = (
            "Upgrade a card" if self._adjust_upgrades_one else "Upgrade 2 random cards"
        )
        cleanup_label = (
            "Remove a card" if self._cleanup_removes else "Transform 2 cards"
        )
        cleanup_enabled = (
            gold >= self.CLEANUP_COST
            and (bool(removable) if self._cleanup_removes else len(removable) >= 2)
        )
        return [
            EventOption("adjust", f"Adjustments ({self.ADJUST_COST}g)", adjust_label,
                        enabled=gold >= self.ADJUST_COST and upgradable),
            EventOption("cleanup", f"Clean Up ({self.CLEANUP_COST}g)", cleanup_label,
                        enabled=cleanup_enabled),
            EventOption("full_service", f"Full Service ({self.FULL_SERVICE_COST}g)",
                        "Remove a card and upgrade a random card",
                        enabled=gold >= self.FULL_SERVICE_COST and bool(removable)),
            EventOption("punch", "Punch", f"Take {self.PUNCH_DAMAGE} damage"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="\"Your deck could use some work,\" the Designer sniffs.",
                next_options=self._main_options(run_state),
            )
        if option_id == "adjust":
            player.lose_gold(self.ADJUST_COST)
            if self._adjust_upgrades_one:
                candidates = player.upgradable_deck_cards()
                if _should_defer_event_rewards(run_state):
                    return _event_result_with_rewards(
                        "Upgraded a card.",
                        [UpgradeCardsReward(player.player_id, count=1, cards=candidates)],
                    )
                return self.request_card_choice(
                    prompt="Choose a card to upgrade",
                    cards=candidates,
                    source_pile="deck",
                    resolver=lambda selected: (
                        _upgrade_selected_cards(selected, run_state),
                        EventResult(finished=True, description="Upgraded a card."),
                    )[-1],
                    allow_skip=False,
                    min_count=1,
                    max_count=1,
                    description="Choose a card to upgrade.",
                )
            upgraded = _upgrade_n_cards(run_state, 2, rng=run_state.rng.niche)
            return EventResult(finished=True, description=f"Upgraded {upgraded} random cards.")
        if option_id == "cleanup":
            player.lose_gold(self.CLEANUP_COST)
            if self._cleanup_removes:
                candidates = player.removable_deck_cards()
                if _should_defer_event_rewards(run_state):
                    return _event_result_with_rewards(
                        "Removed a card.",
                        [RemoveCardReward(player.player_id, count=1, cards=candidates)],
                    )
                return self.request_card_choice(
                    prompt="Choose a card to remove",
                    cards=candidates,
                    source_pile="deck",
                    resolver=lambda selected: (
                        _remove_selected_cards(selected, run_state),
                        EventResult(finished=True, description="Removed a card."),
                    )[-1],
                    allow_skip=False,
                    min_count=1,
                    max_count=1,
                    description="Choose a card to remove.",
                )
            candidates = player.transformable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Transformed 2 cards.",
                    [TransformCardsReward(player.player_id, count=2, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose 2 cards to transform",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _transform_selected_cards(selected, run_state, rng=self.get_rng(run_state)),
                    EventResult(finished=True, description="Transformed 2 cards."),
                )[-1],
                allow_skip=False,
                min_count=2,
                max_count=2,
                description="Choose 2 cards to transform.",
            )
        if option_id == "full_service":
            player.lose_gold(self.FULL_SERVICE_COST)
            candidates = player.removable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Removed a card and upgraded a random card.",
                    [
                        RemoveCardReward(
                            player.player_id, count=1, cards=candidates,
                            after_selected=lambda: _upgrade_n_cards(
                                run_state, 1, rng=run_state.rng.niche
                            ),
                        ),
                    ],
                )
            return self.request_card_choice(
                prompt="Choose a card to remove",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _remove_selected_cards(selected, run_state),
                    _upgrade_n_cards(run_state, 1, rng=run_state.rng.niche),
                    EventResult(
                        finished=True,
                        description="Removed a card and upgraded a random card.",
                    ),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to remove.",
            )
        if option_id == "punch":
            player.lose_hp(self.PUNCH_DAMAGE)
            return EventResult(
                finished=True,
                description=f"Punched the Designer, took {self.PUNCH_DAMAGE} damage in the scuffle.",
            )
        return EventResult(finished=True, description="Nothing happened.")


register_event(DesignerInSpire())


# ── Shared shrine event pool exports ────────────────────────────────
#
# All 15 of the mod's SharedEvents are shrines (IShrineEvent); the
# shrine/non-shrine split below is computed from the registered events so
# the legacy-act pool builder (run/events.py::build_legacy_event_pool) can
# consume it directly. These ids are candidates for ALL THREE legacy acts,
# subject to the per-event act-number restrictions (FaceTrader: acts 1-2,
# DesignerInSpire: acts 2-3) which build_legacy_event_pool enforces.

AFTP_SHARED_EVENT_IDS: list[str] = [
    "BonfireSpirits",
    "Duplicator",
    "FaceTrader",
    "GoldenShrine",
    "Lab",
    "MatchAndKeep",
    "OminousForge",
    "Purifier",
    "TheDivineFountain",
    "TheWomanInBlue",
    "Transmogrifier",
    "UpgradeShrine",
    "WeMeetAgain",
    "WheelOfChange",
    "DesignerInSpire",
]

from sts2_env.run.events import get_event as _get_event  # noqa: E402

AFTP_SHRINE_EVENT_IDS: list[str] = [
    event_id for event_id in AFTP_SHARED_EVENT_IDS
    if getattr(_get_event(event_id), "is_shrine", False)
]
AFTP_NON_SHRINE_SHARED_EVENT_IDS: list[str] = [
    event_id for event_id in AFTP_SHARED_EVENT_IDS
    if not getattr(_get_event(event_id), "is_shrine", False)
]
AFTP_ONE_TIME_SHRINE_EVENT_IDS: list[str] = [
    event_id for event_id in AFTP_SHRINE_EVENT_IDS
    if getattr(_get_event(event_id), "is_one_time_event", False)
]
AFTP_REPEATABLE_SHRINE_EVENT_IDS: list[str] = [
    event_id for event_id in AFTP_SHRINE_EVENT_IDS
    if not getattr(_get_event(event_id), "is_one_time_event", False)
]
