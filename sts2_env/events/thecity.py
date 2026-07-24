"""TheCity (Act-2-slot legacy act) events -- "Acts from the Past" mod.

Recreates Slay the Spire 1's Act 2 ("The City") event roster. Every number
and branch is ported from the decompiled mod source
(``decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.TheCity.Events/*.cs``
plus the event-encounter classes ``ColosseumFirstEncounter.cs`` /
``ColosseumSecondEncounter.cs`` / ``RedMaskBanditsEvent.cs``).

CLASSIC MODE ONLY: this simulator runs with ``RebalancedMode=False``, so
every ``ActsFromThePastConfig.RebalancedMode`` branch in the decompiled
source is intentionally NOT implemented (e.g. CursedTome's flat-6-damage
pages with PullAway, MaskedBandits' BrandishMask, Vampires' BloodBank
option, OldBeggar's Swift/Clumsy option, TheMausoleum's PayRespects,
CouncilOfGhosts' Haunted refusal, Nloth's SearchWithNloth).

Registration: all events here are exclusive to TheCityAct (C#
``Acts => [TheCityAct]``). ``Colosseum`` and ``MaskedBandits`` are
additionally ``IsShared => true`` in the source, so they carry
``is_shared=True`` + ``is_legacy_exclusive=True`` and are filtered by
``run/events.py::event_allowed_in_act`` exactly like the mod's own
SharedEvents. TheCity is not registered as an act-slot candidate yet
(see sts2_env/map/acts.py); ``THECITY_EVENT_IDS`` below is the event pool a
future TheCityAct ActConfig should use.
"""

from __future__ import annotations

import math
from typing import TYPE_CHECKING

# Importing the encounter module registers the Colosseum/MaskedBandits event
# encounters ("colosseum_slavers", "colosseum_nobs", "red_mask_bandits")
# into encounters/events.py's EVENT_ENCOUNTER_REGISTRY.
import sts2_env.encounters.thecity  # noqa: F401
from sts2_env.cards.factory import create_card, create_cards_from_ids, eligible_registered_cards
from sts2_env.core.card_pools import CardPoolId
from sts2_env.core.enums import CardId, CardRarity, CardTag, RelicRarity
from sts2_env.events.shared import (
    _event_result_with_rewards,
    _obtain_random_relics,
    _remove_selected_cards,
    _roll_event_potion_id,
    _roll_random_relic_rewards,
    _should_defer_event_rewards,
    _transform_selected_cards,
    _upgrade_selected_cards,
)
from sts2_env.relics.base import RelicId
from sts2_env.run.events import EventModel, EventOption, EventResult, register_event
from sts2_env.run.reward_objects import (
    AddCardsReward,
    CardReward,
    GoldReward,
    PotionReward,
    RelicReward,
    RemoveCardReward,
    TransformCardsReward,
    UpgradeCardsReward,
)

if TYPE_CHECKING:
    from sts2_env.run.run_state import RunState


# ── Constants (verified against the decompiled .cs sources) ──────────────

AUGMENTER_TRANSFORM_COUNT = 2
COLOSSEUM_MIN_TOTAL_FLOOR = 23
COLOSSEUM_FIGHT_AGAIN_GOLD = 100
COUNCIL_OF_GHOSTS_APPARITION_COUNT = 3
COUNCIL_OF_GHOSTS_MAX_HP_LOSS_PERCENT = 0.5
CURSED_TOME_DMG_PAGE_1 = 1
CURSED_TOME_DMG_PAGE_2 = 2
CURSED_TOME_DMG_PAGE_3 = 3
CURSED_TOME_DMG_STOP = 3
CURSED_TOME_DMG_OBTAIN = 15
FORGOTTEN_ALTAR_HP_LOSS_PERCENT = 0.35
FORGOTTEN_ALTAR_MAX_HP_GAIN = 5
KNOWING_SKULL_MIN_HP = 13
KNOWING_SKULL_BASE_COST = 6
KNOWING_SKULL_GOLD_REWARD = 90
MASKED_BANDITS_MIN_TOTAL_FLOOR = 23
MASKED_BANDITS_MIN_GOLD_REWARD = 25
MASKED_BANDITS_MAX_GOLD_REWARD = 35
NLOTH_REQUIRED_TRADABLE_RELICS = 2
OLD_BEGGAR_GOLD_COST = 75
PLEADING_VAGRANT_GOLD_COST = 85
THE_JOUST_BET_AMOUNT = 50
THE_JOUST_WIN_MURDERER = 100
THE_JOUST_WIN_OWNER = 250
THE_JOUST_OWNER_WIN_CHANCE = 0.3
THE_LIBRARY_CARD_CHOICE_COUNT = 20
THE_LIBRARY_HEAL_PERCENT = 0.2
THE_NEST_HP_LOSS = 6
THE_NEST_GOLD_GAIN = 50
VAMPIRES_MAX_HP_LOSS_PERCENT = 0.3
VAMPIRES_BITE_COUNT = 5


def _has_visited_exordium(run_state: RunState) -> bool:
    """ForgottenAltar.HasVisitedExordium: any act BEFORE the current one is
    an ExordiumAct (identified here by ActConfig.act_id == "Exordium")."""
    for act in run_state.acts[: run_state.current_act_index]:
        if getattr(act, "act_id", "") == "Exordium":
            return True
    return False


# ── AncientWriting ────────────────────────────────────────────────────

class AncientWriting(EventModel):
    """Elegance: remove 1 card from your deck.
    Simplicity: upgrade ALL basic Strike and Defend cards in your deck.
    """

    event_id = "AncientWriting"
    is_legacy_exclusive = True

    @staticmethod
    def _simplicity_candidates(run_state: RunState) -> list:
        # C# filter: Rarity == Basic && (Strike tag || Defend tag) &&
        # IsUpgradable (no removability requirement).
        return [
            card
            for card in run_state.player.deck
            if card.rarity == CardRarity.BASIC
            and (CardTag.STRIKE in card.tags or CardTag.DEFEND in card.tags)
            and not card.upgraded
        ]

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("elegance", "Elegance", "Remove a card from your deck"),
            EventOption("simplicity", "Simplicity",
                         "Upgrade all Strike and Defend cards"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "elegance":
            candidates = run_state.player.removable_deck_cards()
            if not candidates:
                return EventResult(finished=True, description="No removable cards.")
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
        if option_id == "simplicity":
            candidates = self._simplicity_candidates(run_state)
            if not candidates:
                return EventResult(finished=True,
                                   description="No Strikes or Defends to upgrade.")
            upgraded = _upgrade_selected_cards(candidates, run_state)
            return EventResult(
                finished=True,
                description=f"Upgraded {upgraded} Strikes and Defends.",
            )
        return EventResult(finished=True, description="Nothing happened.")


register_event(AncientWriting())


# ── Augmenter ─────────────────────────────────────────────────────────

class Augmenter(EventModel):
    """Test J.A.X.: gain a Jax card.
    Become Test Subject: transform 2 cards.
    Ingest Mutagens: gain the Mutagenic Strength relic.
    """

    event_id = "Augmenter"
    is_legacy_exclusive = True

    def is_allowed(self, run_state: RunState) -> bool:
        # C#: all players have at least 2 removable cards.
        return all(
            sum(1 for card in player.deck if card.is_removable) >= AUGMENTER_TRANSFORM_COUNT
            for player in run_state.players
        )

    @staticmethod
    def _transform_candidates(run_state: RunState) -> list:
        return [card for card in run_state.player.deck if card.is_removable]

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        can_transform = len(self._transform_candidates(run_state)) >= AUGMENTER_TRANSFORM_COUNT
        return [
            EventOption("jax", "Test J.A.X.", "Gain a Jax card"),
            EventOption("transform", "Become Test Subject", "Transform 2 cards",
                         enabled=can_transform),
            EventOption("mutagens", "Ingest Mutagens",
                         "Gain the Mutagenic Strength relic"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "jax":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Gained a Jax card.",
                    [AddCardsReward(run_state.player.player_id, [create_card(CardId.JAX)])],
                )
            run_state.player.add_card_instance_to_deck(create_card(CardId.JAX))
            return EventResult(finished=True, description="Gained a Jax card.")
        if option_id == "transform":
            candidates = self._transform_candidates(run_state)
            if len(candidates) < AUGMENTER_TRANSFORM_COUNT:
                return EventResult(finished=True, description="Not enough cards to transform.")
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Transformed 2 cards.",
                    [TransformCardsReward(run_state.player.player_id,
                                          count=AUGMENTER_TRANSFORM_COUNT, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose 2 cards to transform",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _transform_selected_cards(selected, run_state),
                    EventResult(finished=True, description="Transformed 2 cards."),
                )[-1],
                allow_skip=False,
                min_count=AUGMENTER_TRANSFORM_COUNT,
                max_count=AUGMENTER_TRANSFORM_COUNT,
                description="Choose 2 cards to transform.",
            )
        if option_id == "mutagens":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Gained Mutagenic Strength relic.",
                    [RelicReward(run_state.player.player_id, relic_id=RelicId.MUTAGENIC_STRENGTH.name)],
                )
            run_state.player.obtain_relic(RelicId.MUTAGENIC_STRENGTH.name)
            return EventResult(finished=True, description="Gained Mutagenic Strength relic.")
        return EventResult(finished=True, description="Nothing happened.")


register_event(Augmenter())


# ── Colosseum ─────────────────────────────────────────────────────────

class Colosseum(EventModel):
    """Forced first fight vs Slavers (no rewards), then choose to fight the
    Taskmaster + Gremlin Nob (rare relic + uncommon relic + 100 gold) or
    flee (classic: nothing).
    """

    event_id = "Colosseum"
    is_shared = True
    is_legacy_exclusive = True

    OPTION_ENTER = "enter"
    OPTION_FIGHT = "fight"
    OPTION_FIGHT_AGAIN = "fight_again"
    OPTION_FLEE = "flee"

    def is_allowed(self, run_state: RunState) -> bool:
        # C#: TotalFloor >= 23 and singleplayer only.
        return (
            run_state.total_floor >= COLOSSEUM_MIN_TOTAL_FLOOR
            and len(run_state.players) == 1
        )

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption(self.OPTION_ENTER, "Enter", "Step into the arena")]

    def _post_slavers_options(self) -> list[EventOption]:
        return [
            EventOption(self.OPTION_FIGHT_AGAIN, "Fight the Champions",
                         "Win: a rare relic, an uncommon relic, and 100 gold"),
            EventOption(self.OPTION_FLEE, "Flee", "Escape the Colosseum"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == self.OPTION_ENTER:
            return EventResult(
                finished=False,
                description="The crowd roars for a fight.",
                next_options=[EventOption(self.OPTION_FIGHT, "Fight", "Face the Slavers")],
            )
        if option_id == self.OPTION_FIGHT:
            # ColosseumFirstEncounter: SlaverBlue + SlaverRed with NO rewards
            # (ShouldGiveRewards => false / empty reward list); the event
            # resumes afterwards with the POST_SLAVERS choice.
            return EventResult(
                finished=False,
                description="The Slavers enter the arena.",
                next_options=self._post_slavers_options(),
                event_combat_setup="colosseum_slavers",
            )
        if option_id == self.OPTION_FIGHT_AGAIN:
            # C# FightAgain: RelicFactory.PullNextRelicFromFront(Rare) +
            # PullNextRelicFromFront(Uncommon) + GoldReward(100), then the
            # ColosseumSecondEncounter (Taskmaster + GremlinNob).
            rare_relic_id = run_state.player.pull_next_relic_reward_id(rarity=RelicRarity.RARE)
            uncommon_relic_id = run_state.player.pull_next_relic_reward_id(rarity=RelicRarity.UNCOMMON)
            rewards: list = []
            if rare_relic_id is not None:
                rewards.append(RelicReward(run_state.player.player_id, relic_id=rare_relic_id))
            if uncommon_relic_id is not None:
                rewards.append(RelicReward(run_state.player.player_id, relic_id=uncommon_relic_id))
            rewards.append(GoldReward.fixed(run_state.player.player_id, COLOSSEUM_FIGHT_AGAIN_GOLD))
            return EventResult(
                finished=True,
                description="The champions Taskmaster and Gremlin Nob appear.",
                rewards={"reward_objects": rewards},
                event_combat_setup="colosseum_nobs",
            )
        return EventResult(finished=True, description="Fled the Colosseum.")


register_event(Colosseum())


# ── CouncilOfGhosts ───────────────────────────────────────────────────

class CouncilOfGhosts(EventModel):
    """Accept: lose 50% of your max HP, gain 3 Apparition cards.
    Refuse: nothing (classic).
    """

    event_id = "CouncilOfGhosts"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._max_hp_loss = 0

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: ceil(MaxHp * 0.5), clamped to MaxHp - 1.
        loss = math.ceil(run_state.player.max_hp * COUNCIL_OF_GHOSTS_MAX_HP_LOSS_PERCENT)
        if loss >= run_state.player.max_hp:
            loss = run_state.player.max_hp - 1
        self._max_hp_loss = loss

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("accept", "Accept",
                         f"Lose {self._max_hp_loss} Max HP, gain 3 Apparitions"),
            EventOption("refuse", "Refuse", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "accept":
            self.ensure_vars_calculated(run_state)
            run_state.player.lose_max_hp(self._max_hp_loss)
            apparitions = [create_card(CardId.APPARITION) for _ in range(COUNCIL_OF_GHOSTS_APPARITION_COUNT)]
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Lost {self._max_hp_loss} Max HP, gained 3 Apparitions.",
                    [AddCardsReward(run_state.player.player_id, apparitions)],
                )
            for card in apparitions:
                run_state.player.add_card_instance_to_deck(card)
            return EventResult(
                finished=True,
                description=f"Lost {self._max_hp_loss} Max HP, gained 3 Apparitions.",
            )
        return EventResult(finished=True, description="Refused the ghosts.")


register_event(CouncilOfGhosts())


# ── CursedTome ────────────────────────────────────────────────────────

class CursedTome(EventModel):
    """Read the tome page by page (1 / 2 / 3 damage), then either take 15
    more damage to obtain one of the three book relics (Necronomicon /
    Enchiridion / Nilry's Codex -- one you don't already own) or stop for
    3 damage.
    """

    event_id = "CursedTome"
    is_legacy_exclusive = True

    _BOOK_RELIC_IDS = (RelicId.NECRONOMICON, RelicId.ENCHIRIDION, RelicId.NILRYS_CODEX)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("read", "Read", "Open the tome"),
            EventOption("leave", "Leave", "Walk away"),
        ]

    def _page_options(self, page: int) -> list[EventOption]:
        if page == 1:
            return [EventOption("continue_1", "Continue", "Take 1 damage")]
        if page == 2:
            return [EventOption("continue_2", "Continue", "Take 2 damage")]
        if page == 3:
            return [EventOption("continue_3", "Continue", "Take 3 damage")]
        return [
            EventOption("obtain", "Take the Book", "Take 15 damage, gain a book relic"),
            EventOption("stop", "Stop", "Take 3 damage"),
        ]

    def _roll_book_relic_id(self, run_state: RunState) -> str | None:
        candidates = [
            relic_id.name
            for relic_id in self._BOOK_RELIC_IDS
            if relic_id.name not in run_state.player.relics
        ]
        if not candidates:
            # C#: if all three books are owned, fall back to
            # PullNextRelicFromFront (a random grab-bag relic).
            return run_state.player.pull_next_relic_reward_id()
        return self.get_rng(run_state).choice(candidates)

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "read":
            return EventResult(finished=False, description="You open the tome.",
                               next_options=self._page_options(1))
        if option_id == "continue_1":
            run_state.player.lose_hp(CURSED_TOME_DMG_PAGE_1)
            return EventResult(finished=False, description="Took 1 damage.",
                               next_options=self._page_options(2))
        if option_id == "continue_2":
            run_state.player.lose_hp(CURSED_TOME_DMG_PAGE_2)
            return EventResult(finished=False, description="Took 2 damage.",
                               next_options=self._page_options(3))
        if option_id == "continue_3":
            run_state.player.lose_hp(CURSED_TOME_DMG_PAGE_3)
            return EventResult(finished=False, description="Took 3 damage.",
                               next_options=self._page_options(4))
        if option_id == "obtain":
            run_state.player.lose_hp(CURSED_TOME_DMG_OBTAIN)
            relic_id = self._roll_book_relic_id(run_state)
            if relic_id is None:
                return EventResult(finished=True, description="Took 15 damage.")
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Took 15 damage, gained {relic_id}.",
                    [RelicReward(run_state.player.player_id, relic_id=relic_id)],
                )
            run_state.player.obtain_relic(relic_id)
            return EventResult(finished=True,
                               description=f"Took 15 damage, gained {relic_id}.")
        if option_id == "stop":
            run_state.player.lose_hp(CURSED_TOME_DMG_STOP)
            return EventResult(finished=True, description="Took 3 damage and stopped.")
        return EventResult(finished=True, description="Left the tome alone.")


register_event(CursedTome())


# ── ForgottenAltar ────────────────────────────────────────────────────

class ForgottenAltar(EventModel):
    """Offer Golden Idol: swap it for the Bloody Idol (locked without it).
    Sacrifice: gain 5 Max HP, take 35%-of-max-HP damage.
    Desecrate: gain a Decay curse.

    Only appears if the run visited the Exordium legacy act earlier.
    """

    event_id = "ForgottenAltar"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._hp_loss = 0

    def is_allowed(self, run_state: RunState) -> bool:
        return _has_visited_exordium(run_state)

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: (int)Math.Round(MaxHp * 0.35f).
        self._hp_loss = int(round(run_state.player.max_hp * FORGOTTEN_ALTAR_HP_LOSS_PERCENT))

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        has_golden_idol = RelicId.GOLDEN_IDOL.name in run_state.player.relics
        return [
            EventOption("offer_idol", "Offer the Golden Idol",
                         "Swap the Golden Idol for the Bloody Idol",
                         enabled=has_golden_idol),
            EventOption("sacrifice", "Sacrifice",
                         f"Gain 5 Max HP, take {self._hp_loss} damage"),
            EventOption("desecrate", "Desecrate", "Gain a Decay curse"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "offer_idol":
            if RelicId.GOLDEN_IDOL.name not in run_state.player.relics:
                return EventResult(finished=True, description="You have no Golden Idol.")
            run_state.player.transform_relic(RelicId.GOLDEN_IDOL.name, RelicId.BLOODY_IDOL)
            return EventResult(finished=True,
                               description="Swapped the Golden Idol for the Bloody Idol.")
        if option_id == "sacrifice":
            self.ensure_vars_calculated(run_state)
            # C# order: GainMaxHp(5) first, then the damage.
            run_state.player.gain_max_hp(FORGOTTEN_ALTAR_MAX_HP_GAIN)
            run_state.player.lose_hp(self._hp_loss)
            return EventResult(
                finished=True,
                description=f"Gained 5 Max HP, took {self._hp_loss} damage.",
            )
        if option_id == "desecrate":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Gained a Decay curse.",
                    [AddCardsReward(run_state.player.player_id, [create_card(CardId.DECAY)])],
                )
            run_state.player.add_card_instance_to_deck(create_card(CardId.DECAY))
            return EventResult(finished=True, description="Gained a Decay curse.")
        return EventResult(finished=True, description="Nothing happened.")


register_event(ForgottenAltar())


# ── KnowingSkull ──────────────────────────────────────────────────────

class KnowingSkull(EventModel):
    """Repeatable menu: a potion / 90 gold / an uncommon colorless card,
    each costing HP that starts at 6 and increases by 1 per use of that
    option. Leaving always costs 6 HP.
    """

    event_id = "KnowingSkull"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._potion_cost = KNOWING_SKULL_BASE_COST
        self._gold_cost = KNOWING_SKULL_BASE_COST
        self._card_cost = KNOWING_SKULL_BASE_COST
        self._leave_cost = KNOWING_SKULL_BASE_COST

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.current_hp >= KNOWING_SKULL_MIN_HP for player in run_state.players)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self._potion_cost = KNOWING_SKULL_BASE_COST
        self._gold_cost = KNOWING_SKULL_BASE_COST
        self._card_cost = KNOWING_SKULL_BASE_COST
        self._leave_cost = KNOWING_SKULL_BASE_COST
        return [EventOption("continue", "Approach", "Speak with the skull")]

    def _ask_options(self) -> list[EventOption]:
        return [
            EventOption("potion", "A potion?",
                         f"Take {self._potion_cost} damage, gain a potion"),
            EventOption("gold", "Riches?",
                         f"Take {self._gold_cost} damage, gain {KNOWING_SKULL_GOLD_REWARD} gold"),
            EventOption("card", "A pick me up?",
                         f"Take {self._card_cost} damage, gain an uncommon colorless card"),
            EventOption("leave", "Leave",
                         f"Take {self._leave_cost} damage and leave"),
        ]

    def _roll_uncommon_colorless_card(self, run_state: RunState):
        # C#: random UNCOMMON card from the ColorlessCardPool (C# CardRarity
        # 3 == Uncommon), rolled with the Niche rng.
        card_ids = eligible_registered_cards(
            card_pool=CardPoolId.COLORLESS,
            rarity=CardRarity.UNCOMMON,
            generation_context=None,
            is_multiplayer=len(run_state.players) > 1,
        )
        cards = create_cards_from_ids(card_ids, run_state.rng.niche, 1, distinct=False)
        return cards[0] if cards else None

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "continue":
            return EventResult(finished=False, description="What do you seek?",
                               next_options=self._ask_options())
        if option_id == "potion":
            run_state.player.lose_hp(self._potion_cost)
            self._potion_cost += 1
            potion_id = _roll_event_potion_id(run_state)
            rewards = [PotionReward(run_state.player.player_id, potion_id=potion_id)] if potion_id else []
            return EventResult(
                finished=False,
                description="The skull conjures a potion.",
                next_options=self._ask_options(),
                rewards={"reward_objects": rewards},
            )
        if option_id == "gold":
            run_state.player.lose_hp(self._gold_cost)
            self._gold_cost += 1
            run_state.player.gain_gold(KNOWING_SKULL_GOLD_REWARD)
            return EventResult(
                finished=False,
                description=f"Gained {KNOWING_SKULL_GOLD_REWARD} gold.",
                next_options=self._ask_options(),
            )
        if option_id == "card":
            run_state.player.lose_hp(self._card_cost)
            self._card_cost += 1
            card = self._roll_uncommon_colorless_card(run_state)
            if card is None:
                return EventResult(finished=False, description="The skull offers nothing.",
                                   next_options=self._ask_options())
            if _should_defer_event_rewards(run_state):
                return EventResult(
                    finished=False,
                    description=f"Gained {card.card_id.name}.",
                    next_options=self._ask_options(),
                    rewards={"reward_objects": [AddCardsReward(run_state.player.player_id, [card])]},
                )
            run_state.player.add_card_instance_to_deck(card)
            return EventResult(finished=False,
                               description=f"Gained {card.card_id.name}.",
                               next_options=self._ask_options())
        # leave
        run_state.player.lose_hp(self._leave_cost)
        return EventResult(finished=True,
                           description=f"Took {self._leave_cost} damage and left.")


register_event(KnowingSkull())


# ── MaskedBandits ─────────────────────────────────────────────────────

class MaskedBandits(EventModel):
    """Pay: lose ALL of your gold.
    Fight: battle Pointy, Romeo, and Bear; win 25-35 gold and the Red Mask.
    """

    event_id = "MaskedBandits"
    is_shared = True
    is_legacy_exclusive = True

    def is_allowed(self, run_state: RunState) -> bool:
        # C# (classic): TotalFloor >= 23 and no player already has RedMask.
        return run_state.total_floor >= MASKED_BANDITS_MIN_TOTAL_FLOOR and not any(
            RelicId.RED_MASK.name in player.relics for player in run_state.players
        )

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("pay", "Pay", "Lose ALL your gold"),
            EventOption("fight", "Fight",
                         "Fight the bandits; win 25-35 gold and the Red Mask"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pay":
            lost = run_state.player.lose_all_gold()
            return EventResult(finished=True,
                               description=f"Paid {lost} gold to the bandits.")
        if option_id == "fight":
            return EventResult(
                finished=True,
                description="The Red Mask Bandits attack!",
                rewards={
                    "reward_objects": [
                        GoldReward(run_state.player.player_id,
                                   MASKED_BANDITS_MIN_GOLD_REWARD,
                                   MASKED_BANDITS_MAX_GOLD_REWARD),
                        RelicReward(run_state.player.player_id, relic_id=RelicId.RED_MASK.name),
                    ]
                },
                event_combat_setup="red_mask_bandits",
            )
        return EventResult(finished=True, description="Nothing happened.")


register_event(MaskedBandits())


# ── Nloth ─────────────────────────────────────────────────────────────

class Nloth(EventModel):
    """Trade one of two randomly-picked tradable relics for N'loth's Gift;
    or leave.
    """

    event_id = "Nloth"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._choice_relics: list[str] = []

    def is_allowed(self, run_state: RunState) -> bool:
        return all(
            len(player.tradable_relics()) >= NLOTH_REQUIRED_TRADABLE_RELICS
            for player in run_state.players
        )

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        # C#: StableShuffle the tradable relics with the event rng, take 2.
        tradable = run_state.player.tradable_relics()
        self.get_rng(run_state).shuffle(tradable)
        self._choice_relics = tradable[:2]
        options = [
            EventOption(f"trade_{index + 1}", f"Trade {relic_id}",
                         f"Lose {relic_id}, gain N'loth's Gift")
            for index, relic_id in enumerate(self._choice_relics)
        ]
        options.append(EventOption("leave", "Leave", "Keep your relics"))
        return options

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id.startswith("trade_"):
            index = int(option_id.split("_")[1]) - 1
            if 0 <= index < len(self._choice_relics):
                relic_id = self._choice_relics[index]
                if relic_id in run_state.player.relics:
                    relic_index = run_state.player.relics.index(relic_id)
                    run_state.player.relics.pop(relic_index)
                    if relic_index < len(run_state.player.relic_objects):
                        run_state.player.relic_objects.pop(relic_index)
                if _should_defer_event_rewards(run_state):
                    return _event_result_with_rewards(
                        f"Traded {relic_id} for N'loth's Gift.",
                        [RelicReward(run_state.player.player_id, relic_id=RelicId.NLOTHS_GIFT.name)],
                    )
                run_state.player.obtain_relic(RelicId.NLOTHS_GIFT.name)
                return EventResult(finished=True,
                                   description=f"Traded {relic_id} for N'loth's Gift.")
        return EventResult(finished=True, description="Left N'loth alone.")


register_event(Nloth())


# ── OldBeggar ─────────────────────────────────────────────────────────

class OldBeggar(EventModel):
    """Offer Gold: pay 75 gold, then remove a card from your deck.
    Leave: nothing (classic).
    """

    event_id = "OldBeggar"
    is_legacy_exclusive = True

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.gold >= OLD_BEGGAR_GOLD_COST for player in run_state.players)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("give_gold", f"Offer Gold ({OLD_BEGGAR_GOLD_COST}g)",
                         "Pay 75 gold, remove a card"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "give_gold":
            run_state.player.lose_gold(OLD_BEGGAR_GOLD_COST)
            return EventResult(
                finished=False,
                description="The beggar reveals themselves as a cleric.",
                next_options=[EventOption("remove_card", "Remove a Card",
                                           "Remove a card from your deck")],
            )
        if option_id == "remove_card":
            candidates = run_state.player.removable_deck_cards()
            if not candidates:
                return EventResult(finished=True, description="No removable cards.")
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
        return EventResult(finished=True, description="Left the beggar.")


register_event(OldBeggar())


# ── PleadingVagrant ───────────────────────────────────────────────────

class PleadingVagrant(EventModel):
    """Offer Gold: pay 85 gold, gain a random relic (locked below 85g).
    Rob: gain a Shame curse and the relic anyway.
    Leave: nothing.
    """

    event_id = "PleadingVagrant"
    is_legacy_exclusive = True

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        can_afford = run_state.player.gold >= PLEADING_VAGRANT_GOLD_COST
        return [
            EventOption("pay", f"Offer Gold ({PLEADING_VAGRANT_GOLD_COST}g)",
                         "Pay 85 gold, gain a relic", enabled=can_afford),
            EventOption("rob", "Rob", "Gain a Shame curse and a relic"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "pay":
            run_state.player.lose_gold(PLEADING_VAGRANT_GOLD_COST)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Paid 85 gold, gained a relic.",
                    _roll_random_relic_rewards(run_state, 1),
                )
            _obtain_random_relics(run_state, 1)
            return EventResult(finished=True, description="Paid 85 gold, gained a relic.")
        if option_id == "rob":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Robbed the vagrant: gained a Shame curse and a relic.",
                    [
                        AddCardsReward(run_state.player.player_id, [create_card(CardId.SHAME)]),
                        *_roll_random_relic_rewards(run_state, 1),
                    ],
                )
            run_state.player.add_card_instance_to_deck(create_card(CardId.SHAME))
            _obtain_random_relics(run_state, 1)
            return EventResult(finished=True,
                               description="Robbed the vagrant: gained a Shame curse and a relic.")
        return EventResult(finished=True, description="Left the vagrant.")


register_event(PleadingVagrant())


# ── TheJoust ──────────────────────────────────────────────────────────

class TheJoust(EventModel):
    """Bet 50 gold on the Murderer (~70% to win 100) or on the Owner
    (~30% to win 250). There is no option to abstain in the source.
    """

    event_id = "TheJoust"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._bet_for_owner = False

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption("continue", "Continue", "Approach the arena")]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="Place your bet (50 gold).",
                next_options=[
                    EventOption("bet_murderer", "Bet on the Murderer",
                                 f"Bet {THE_JOUST_BET_AMOUNT}g; win {THE_JOUST_WIN_MURDERER}g (70%)"),
                    EventOption("bet_owner", "Bet on the Owner",
                                 f"Bet {THE_JOUST_BET_AMOUNT}g; win {THE_JOUST_WIN_OWNER}g (30%)"),
                ],
            )
        if option_id in ("bet_murderer", "bet_owner"):
            self._bet_for_owner = option_id == "bet_owner"
            run_state.player.lose_gold(THE_JOUST_BET_AMOUNT)
            return EventResult(
                finished=False,
                description="The joust begins.",
                next_options=[EventOption("watch", "Watch", "See who wins")],
            )
        if option_id == "watch":
            # C#: ownerWins = Rng.NextFloat(1f) < 0.3f.
            owner_wins = self.get_rng(run_state).next_float(1.0) < THE_JOUST_OWNER_WIN_CHANCE
            if owner_wins and self._bet_for_owner:
                run_state.player.gain_gold(THE_JOUST_WIN_OWNER)
                return EventResult(finished=True,
                                   description=f"The Owner wins! Won {THE_JOUST_WIN_OWNER} gold.")
            if owner_wins:
                return EventResult(finished=True,
                                   description="The Owner wins. You lost your bet.")
            if self._bet_for_owner:
                return EventResult(finished=True,
                                   description="The Murderer wins. You lost your bet.")
            run_state.player.gain_gold(THE_JOUST_WIN_MURDERER)
            return EventResult(finished=True,
                               description=f"The Murderer wins! Won {THE_JOUST_WIN_MURDERER} gold.")
        return EventResult(finished=True, description="Nothing happened.")


register_event(TheJoust())


# ── TheLibrary ────────────────────────────────────────────────────────

class TheLibrary(EventModel):
    """Read: pick 1 of 20 cards from your character's pool (not skippable).
    Sleep: heal 20% of your max HP.
    """

    event_id = "TheLibrary"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._heal_amount = 0

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: Math.Round(MaxHp * 0.2m).
        self._heal_amount = int(round(run_state.player.max_hp * THE_LIBRARY_HEAL_PERCENT))

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("read", "Read", "Pick 1 of 20 cards"),
            EventOption("sleep", "Sleep", f"Heal {self._heal_amount} HP"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "read":
            # C#: CardFactory.CreateForReward(owner, 20,
            # ForNonCombatWithDefaultOdds(character pool)), not cancelable.
            return EventResult(
                finished=True,
                description="Picked a card from the library.",
                rewards={
                    "reward_objects": [
                        CardReward(
                            run_state.player.player_id,
                            option_count=THE_LIBRARY_CARD_CHOICE_COUNT,
                            cards_to_pick=1,
                            skippable=False,
                            generation_context=None,
                            roll_upgrade=False,
                        )
                    ]
                },
            )
        self.ensure_vars_calculated(run_state)
        healed = run_state.player.heal(self._heal_amount)
        return EventResult(finished=True, description=f"Slept and healed {healed} HP.")


register_event(TheLibrary())


# ── TheMausoleum ──────────────────────────────────────────────────────

class TheMausoleum(EventModel):
    """Open Coffin: gain a random relic AND a Writhe curse (always cursed
    in the mod's classic mode -- no 50% roll).
    Leave: nothing.
    """

    event_id = "TheMausoleum"
    is_legacy_exclusive = True

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("open", "Open Coffin", "Gain a relic and a Writhe curse"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "open":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Gained a relic and a Writhe curse.",
                    [
                        *_roll_random_relic_rewards(run_state, 1),
                        AddCardsReward(run_state.player.player_id, [create_card(CardId.WRITHE)]),
                    ],
                )
            _obtain_random_relics(run_state, 1)
            run_state.player.add_card_instance_to_deck(create_card(CardId.WRITHE))
            return EventResult(finished=True,
                               description="Gained a relic and a Writhe curse.")
        return EventResult(finished=True, description="Left the mausoleum.")


register_event(TheMausoleum())


# ── TheNest ───────────────────────────────────────────────────────────

class TheNest(EventModel):
    """Investigate, then: Steal (gain 50 gold) or Join (take 6 damage, gain
    a Ritual Dagger).
    """

    event_id = "TheNest"
    is_legacy_exclusive = True

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption("investigate", "Investigate", "Look around the nest")]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "investigate":
            return EventResult(
                finished=False,
                description="Cultists mill about, leaving a stash unattended.",
                next_options=[
                    EventOption("steal", "Steal", f"Gain {THE_NEST_GOLD_GAIN} gold"),
                    EventOption("join", "Join",
                                 f"Take {THE_NEST_HP_LOSS} damage, gain a Ritual Dagger"),
                ],
            )
        if option_id == "steal":
            run_state.player.gain_gold(THE_NEST_GOLD_GAIN)
            return EventResult(finished=True,
                               description=f"Stole {THE_NEST_GOLD_GAIN} gold.")
        if option_id == "join":
            run_state.player.lose_hp(THE_NEST_HP_LOSS)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Took 6 damage, gained a Ritual Dagger.",
                    [AddCardsReward(run_state.player.player_id, [create_card(CardId.RITUAL_DAGGER)])],
                )
            run_state.player.add_card_instance_to_deck(create_card(CardId.RITUAL_DAGGER))
            return EventResult(finished=True,
                               description="Took 6 damage, gained a Ritual Dagger.")
        return EventResult(finished=True, description="Nothing happened.")


register_event(TheNest())


# ── Vampires ──────────────────────────────────────────────────────────

class Vampires(EventModel):
    """Accept: lose 30% of your max HP, replace all basic Strikes in your
    deck with 5 Bites.
    Offer Blood Vial (only if held): lose the Blood Vial instead of max HP.
    Refuse: nothing.
    """

    event_id = "Vampires"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._max_hp_loss = 0

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: ceil(MaxHp * 0.3), clamped to MaxHp - 1.
        loss = math.ceil(run_state.player.max_hp * VAMPIRES_MAX_HP_LOSS_PERCENT)
        if loss >= run_state.player.max_hp:
            loss = run_state.player.max_hp - 1
        self._max_hp_loss = loss

    @staticmethod
    def _basic_strikes(run_state: RunState) -> list:
        # C# filter: Rarity == Basic && Strike tag (no removability check).
        return [
            card
            for card in run_state.player.deck
            if card.rarity == CardRarity.BASIC and CardTag.STRIKE in card.tags
        ]

    def _replace_strikes_with_bites(self, run_state: RunState) -> EventResult | None:
        for strike in self._basic_strikes(run_state):
            run_state.player.deck.remove(strike)
        bites = [create_card(CardId.BITE) for _ in range(VAMPIRES_BITE_COUNT)]
        if _should_defer_event_rewards(run_state):
            return _event_result_with_rewards(
                "Replaced your Strikes with 5 Bites.",
                [AddCardsReward(run_state.player.player_id, bites)],
            )
        for bite in bites:
            run_state.player.add_card_instance_to_deck(bite)
        return None

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        options = [
            EventOption("accept", "Accept",
                         f"Lose {self._max_hp_loss} Max HP; replace Strikes with 5 Bites"),
        ]
        if RelicId.BLOOD_VIAL.name in run_state.player.relics:
            options.append(EventOption("vial", "Offer Blood Vial",
                                        "Lose the Blood Vial; replace Strikes with 5 Bites"))
        options.append(EventOption("refuse", "Refuse", "Nothing happens"))
        return options

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "accept":
            self.ensure_vars_calculated(run_state)
            run_state.player.lose_max_hp(self._max_hp_loss)
            deferred = self._replace_strikes_with_bites(run_state)
            if deferred is not None:
                deferred.description = (
                    f"Lost {self._max_hp_loss} Max HP; replaced Strikes with 5 Bites."
                )
                return deferred
            return EventResult(
                finished=True,
                description=f"Lost {self._max_hp_loss} Max HP; replaced Strikes with 5 Bites.",
            )
        if option_id == "vial" and RelicId.BLOOD_VIAL.name in run_state.player.relics:
            index = run_state.player.relics.index(RelicId.BLOOD_VIAL.name)
            run_state.player.relics.pop(index)
            if index < len(run_state.player.relic_objects):
                run_state.player.relic_objects.pop(index)
            deferred = self._replace_strikes_with_bites(run_state)
            if deferred is not None:
                deferred.description = "Gave the Blood Vial; replaced Strikes with 5 Bites."
                return deferred
            return EventResult(finished=True,
                               description="Gave the Blood Vial; replaced Strikes with 5 Bites.")
        return EventResult(finished=True, description="Refused the vampires.")


register_event(Vampires())


# ── Act event pool ────────────────────────────────────────────────────
# The event pool for a future TheCityAct ActConfig (act_id="TheCity",
# is_legacy=True). Colosseum and MaskedBandits are SharedEvents in the mod
# but still only valid in TheCity via their Acts override, so they belong in
# this act pool too (event_allowed_in_act additionally keeps them out of
# non-legacy acts).

THECITY_EVENT_IDS: list[str] = [
    "AncientWriting",
    "Augmenter",
    "Colosseum",
    "CouncilOfGhosts",
    "CursedTome",
    "ForgottenAltar",
    "KnowingSkull",
    "MaskedBandits",
    "Nloth",
    "OldBeggar",
    "PleadingVagrant",
    "TheJoust",
    "TheLibrary",
    "TheMausoleum",
    "TheNest",
    "Vampires",
]
