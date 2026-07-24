"""Exordium (Act-1-slot legacy act) events -- "Acts from the Past" mod.

Implemented against the decompiled mod source in
``decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.Exordium.Events/*.cs``
(plus the event-encounter classes DeadAdventurerSentries/-GremlinNob/
-Lagavulin and ThreeFungiBeastsEvent in ``.../ActsFromThePast/``).

CLASSIC MODE ONLY: the user's mod config has ``RebalancedMode=False``, so
every ``ActsFromThePastConfig.RebalancedMode`` branch in the source is a dead
branch here and is intentionally NOT implemented (BigFish's TheBox card,
Cleric's no-leave variant, DeadAdventurer's WalkAway, GoldenIdol's
Switcheroo/Jam, Mushrooms' SporeMind, ScrapOoze's Bloat, ShiningLight's
BurnBright Bask, Sssserpent's 250g/potion variant, WingStatue's 60-95g roll).
``LegacyEnemiesGiveClassicSlimed=False`` likewise means no classic-Slimed
tagging anywhere.

Event pool: these events all declare ``Acts => [ExordiumAct]`` in the source,
so they form Exordium's act-exclusive event pool (``EXORDIUM_EVENT_IDS``
below, analogous to the vanilla acts' ``event_ids`` lists in
``sts2_env/map/acts.py``). The Exordium act config isn't registered as a map
candidate yet; when it is, its ``event_ids`` should be this list.
DeadAdventurer and Mushrooms are additionally ``IsShared => true`` events in
the source, so they carry ``is_shared``/``is_legacy_exclusive`` and are also
blocked from non-legacy acts by ``event_allowed_in_act``.

Event-embedded combats use RunManager's ``_enter_event_combat`` mechanism
(``EventResult.event_combat_setup`` + explicit ``reward_objects``), which
creates the CombatRoom with ``suppress_default_rewards=True``: the fight pays
out EXACTLY the event's reward list, never the standard room-type reward roll.
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.cards.status import make_doubt, make_injury, make_parasite, make_regret
from sts2_env.core.enums import CardType
from sts2_env.events.shared import (
    _event_result_with_rewards,
    _obtain_random_relics,
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
    CardReward,
    GoldReward,
    RelicReward,
    RemoveCardReward,
    TransformCardsReward,
    UpgradeCardsReward,
)

# Importing the encounter module registers the event-only encounters
# (dead_adventurer_* / three_fungi_beasts_event) in EVENT_ENCOUNTER_REGISTRY.
import sts2_env.encounters.exordium  # noqa: F401

if TYPE_CHECKING:
    from sts2_env.run.run_state import RunState


# ── BigFish ──────────────────────────────────────────────────────────

class BigFish(EventModel):
    """Banana: heal Max HP / 3. Donut: +5 Max HP.
    Box: gain a relic and the Regret curse.
    """

    event_id = "BigFish"
    is_legacy_exclusive = True
    MAX_HP_GAIN = 5

    def __init__(self) -> None:
        self._heal = 0

    def calculate_vars(self, run_state: RunState) -> None:
        # BigFish.CalculateVars: Heal.BaseValue = MaxHp / 3 (decimal; the
        # heal command truncates to int).
        self._heal = run_state.player.max_hp // 3

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("banana", "Banana", f"Heal {self._heal} HP"),
            EventOption("donut", "Donut", f"+{self.MAX_HP_GAIN} Max HP"),
            EventOption("box", "Box", "Gain a relic and the Regret curse"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "banana":
            healed = run_state.player.heal(self._heal)
            return EventResult(finished=True, description=f"Healed {healed} HP.")
        if option_id == "donut":
            run_state.player.gain_max_hp(self.MAX_HP_GAIN)
            return EventResult(finished=True, description=f"Gained {self.MAX_HP_GAIN} Max HP.")
        # box: Regret curse first, then a relic (BigFish.Box order).
        if _should_defer_event_rewards(run_state):
            return _event_result_with_rewards(
                "Gained a relic and the Regret curse.",
                [
                    AddCardsReward(run_state.player.player_id, [make_regret()]),
                    RelicReward(run_state.player.player_id),
                ],
            )
        run_state.player.add_card_instance_to_deck(make_regret())
        _obtain_random_relics(run_state, 1)
        return EventResult(finished=True, description="Gained a relic and the Regret curse.")


register_event(BigFish())


# ── Cleric ───────────────────────────────────────────────────────────

class Cleric(EventModel):
    """Heal: pay 35 gold, heal 25% Max HP. Purify: pay 75 gold, remove a card.
    Leave: nothing.
    """

    event_id = "Cleric"
    is_legacy_exclusive = True
    HEAL_COST = 35
    PURIFY_COST = 75
    HEAL_PERCENT = 0.25

    def __init__(self) -> None:
        self._heal = 0

    def is_allowed(self, run_state: RunState) -> bool:
        # Classic-mode Cleric.IsAllowed: all players have >= 35 gold.
        return all(player.gold >= self.HEAL_COST for player in run_state.players)

    def calculate_vars(self, run_state: RunState) -> None:
        self._heal = int(run_state.player.max_hp * self.HEAL_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        return [
            EventOption(
                "heal", f"Heal ({self.HEAL_COST}g)",
                f"Pay {self.HEAL_COST} gold, heal {self._heal} HP",
                enabled=player.gold >= self.HEAL_COST,
            ),
            EventOption(
                "purify", f"Purify ({self.PURIFY_COST}g)",
                f"Pay {self.PURIFY_COST} gold, remove a card",
                enabled=player.gold >= self.PURIFY_COST and bool(player.removable_deck_cards()),
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "heal":
            run_state.player.lose_gold(self.HEAL_COST)
            healed = run_state.player.heal(self._heal)
            return EventResult(
                finished=True,
                description=f"Paid {self.HEAL_COST} gold, healed {healed} HP.",
            )
        if option_id == "purify":
            run_state.player.lose_gold(self.PURIFY_COST)
            candidates = run_state.player.removable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Paid {self.PURIFY_COST} gold, removed a card.",
                    [RemoveCardReward(run_state.player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to remove",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _remove_selected_cards(selected, run_state),
                    EventResult(
                        finished=True,
                        description=f"Paid {self.PURIFY_COST} gold, removed a card.",
                    ),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to remove.",
            )
        return EventResult(finished=True, description="Left the Cleric.")


register_event(Cleric())


# ── DeadAdventurer ──────────────────────────────────────────────────

class DeadAdventurer(EventModel):
    """Repeatable Search with escalating ambush chance (35% +25% per search).

    Each successful search grants one of a shuffled [30 gold, nothing, relic]
    list. A failed roll reveals the ambush (Sentries / Gremlin Nob / awake
    Lagavulin, rolled once at event start); fighting pays out a 3-card elite
    reward, 25-35 gold, and every unsearched reward -- and nothing else
    (default combat rewards suppressed).
    """

    event_id = "DeadAdventurer"
    is_shared = True
    is_legacy_exclusive = True
    GOLD_REWARD = 30
    ENCOUNTER_CHANCE_START = 35
    ENCOUNTER_CHANCE_RAMP = 25
    COMBAT_GOLD_MIN = 25
    COMBAT_GOLD_MAX = 35
    COMBAT_CARD_OPTIONS = 3
    MAX_SEARCHES = 3
    MIN_TOTAL_FLOOR = 7
    _ENCOUNTER_IDS = (
        "dead_adventurer_sentries",
        "dead_adventurer_gremlin_nob",
        "dead_adventurer_lagavulin",
    )
    _ENEMY_NAMES = ("Sentries", "Gremlin Nob", "Lagavulin")

    def __init__(self) -> None:
        self._encounter_chance = self.ENCOUNTER_CHANCE_START
        self._num_searches = 0
        self._enemy_type = 0
        self._rewards: list[str] = []

    def is_allowed(self, run_state: RunState) -> bool:
        return run_state.total_floor >= self.MIN_TOTAL_FLOOR

    def calculate_vars(self, run_state: RunState) -> None:
        rng = self.get_rng(run_state)
        self._encounter_chance = self.ENCOUNTER_CHANCE_START
        self._num_searches = 0
        self._enemy_type = rng.next_int_exclusive(0, 3)
        self._rewards = ["gold", "nothing", "relic"]
        rng.shuffle(self._rewards)

    @property
    def encounter_id(self) -> str:
        return self._ENCOUNTER_IDS[self._enemy_type]

    def _search_options(self) -> list[EventOption]:
        return [
            EventOption(
                "search", "Search",
                f"{self._encounter_chance}% chance of an ambush; otherwise find something",
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return self._search_options()

    def _combat_rewards(self, run_state: RunState) -> list[object]:
        # DeadAdventurer.EnterCombat: 3-card reward (elite room context),
        # 25-35 gold, plus every not-yet-claimed search reward.
        player_id = run_state.player.player_id
        rewards: list[object] = [
            CardReward(player_id, "elite", self.COMBAT_CARD_OPTIONS),
            GoldReward(player_id, self.COMBAT_GOLD_MIN, self.COMBAT_GOLD_MAX),
        ]
        for reward in self._rewards:
            if reward == "gold":
                rewards.append(GoldReward(player_id, self.GOLD_REWARD, self.GOLD_REWARD))
            elif reward == "relic":
                rewards.append(RelicReward(player_id))
        return rewards

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "search":
            rng = self.get_rng(run_state)
            if rng.next_int_exclusive(0, 100) < self._encounter_chance:
                enemy = self._ENEMY_NAMES[self._enemy_type]
                return EventResult(
                    finished=False,
                    description=f"An ambush! {enemy} attack{'s' if self._enemy_type == 1 else ''}!",
                    next_options=[EventOption("fight", "Fight", "Fight the ambush")],
                )
            return self._grant_reward(run_state)
        if option_id == "fight":
            return EventResult(
                finished=True,
                description="Entered combat with the ambushers.",
                rewards={"reward_objects": self._combat_rewards(run_state)},
                event_combat_setup=self.encounter_id,
            )
        return EventResult(finished=True, description="Left the dead adventurer.")

    def _grant_reward(self, run_state: RunState) -> EventResult:
        self._num_searches += 1
        self._encounter_chance += self.ENCOUNTER_CHANCE_RAMP
        reward = self._rewards.pop(0)
        was_last_search = self._num_searches >= self.MAX_SEARCHES
        if reward == "gold":
            run_state.player.gain_gold(self.GOLD_REWARD)
            description = f"Found {self.GOLD_REWARD} gold."
        elif reward == "relic":
            obtained = _obtain_random_relics(run_state, 1)
            relic_name = obtained[0] if obtained else "a relic"
            description = f"Found a relic ({relic_name})."
        else:
            description = "Found nothing."
        if was_last_search:
            return EventResult(
                finished=True,
                description=f"{description} Nothing left to search.",
            )
        return EventResult(
            finished=False,
            description=description,
            next_options=self._search_options(),
        )


register_event(DeadAdventurer())


# ── GoldenIdol ──────────────────────────────────────────────────────

class GoldenIdol(EventModel):
    """Take the Golden Idol relic, then face the boulder:
    Outrun (gain Injury curse) / Smash (take 35% Max HP damage) /
    Hide (lose 10% Max HP, min 1). Or just leave.
    """

    event_id = "GoldenIdol"
    is_legacy_exclusive = True
    HP_LOSS_PERCENT = 0.35
    MAX_HP_LOSS_PERCENT = 0.10

    def __init__(self) -> None:
        self._damage = 0
        self._max_hp_loss = 1

    def calculate_vars(self, run_state: RunState) -> None:
        max_hp = run_state.player.max_hp
        self._damage = int(max_hp * self.HP_LOSS_PERCENT)
        self._max_hp_loss = max(1, int(max_hp * self.MAX_HP_LOSS_PERCENT))

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("take", "Take", "Gain the Golden Idol relic (a boulder gives chase)"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def _boulder_options(self) -> list[EventOption]:
        return [
            EventOption("outrun", "Outrun", "Gain an Injury curse"),
            EventOption("smash", "Smash", f"Take {self._damage} damage"),
            EventOption("hide", "Hide", f"Lose {self._max_hp_loss} Max HP"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "take":
            description = "Took the Golden Idol. A boulder gives chase!"
            if _should_defer_event_rewards(run_state):
                return EventResult(
                    finished=False,
                    description=description,
                    next_options=self._boulder_options(),
                    rewards={
                        "reward_objects": [
                            RelicReward(run_state.player.player_id, relic_id=RelicId.GOLDEN_IDOL.name)
                        ]
                    },
                )
            run_state.player.obtain_relic(RelicId.GOLDEN_IDOL.name)
            return EventResult(
                finished=False,
                description=description,
                next_options=self._boulder_options(),
            )
        if option_id == "outrun":
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    "Outran the boulder, gained an Injury curse.",
                    [AddCardsReward(run_state.player.player_id, [make_injury()])],
                )
            run_state.player.add_card_instance_to_deck(make_injury())
            return EventResult(finished=True, description="Outran the boulder, gained an Injury curse.")
        if option_id == "smash":
            run_state.player.lose_hp(self._damage)
            return EventResult(finished=True, description=f"Smashed through, took {self._damage} damage.")
        if option_id == "hide":
            run_state.player.lose_max_hp(self._max_hp_loss)
            return EventResult(finished=True, description=f"Hid from the boulder, lost {self._max_hp_loss} Max HP.")
        return EventResult(finished=True, description="Left the idol alone.")


register_event(GoldenIdol())


# ── LivingWall ──────────────────────────────────────────────────────

class LivingWall(EventModel):
    """Forget: remove a card. Change: transform a card.
    Grow: upgrade a card (only if an upgradable card exists).
    """

    event_id = "LivingWall"
    is_legacy_exclusive = True

    def is_allowed(self, run_state: RunState) -> bool:
        return all(player.removable_deck_cards() for player in run_state.players)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("forget", "Forget", "Remove a card"),
            EventOption("change", "Change", "Transform a card"),
            EventOption(
                "grow", "Grow", "Upgrade a card",
                enabled=bool(run_state.player.upgradable_deck_cards()),
            ),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "forget":
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
        if option_id == "change":
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
                    _transform_selected_cards(selected, run_state, rng=run_state.rng.niche),
                    EventResult(finished=True, description="Transformed a card."),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to transform.",
            )
        if option_id == "grow":
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
        return EventResult(finished=True, description="Nothing happened.")


register_event(LivingWall())


# ── Mushrooms ───────────────────────────────────────────────────────

class Mushrooms(EventModel):
    """Fight: 3 Fungi Beasts; winning grants exactly the Odd Mushroom relic.
    Eat: heal 25% Max HP, gain the Parasite curse.
    """

    event_id = "Mushrooms"
    is_shared = True
    is_legacy_exclusive = True
    HEAL_PERCENT = 0.25
    MIN_TOTAL_FLOOR = 7
    ENCOUNTER_ID = "three_fungi_beasts_event"

    def __init__(self) -> None:
        self._heal = 0

    def is_allowed(self, run_state: RunState) -> bool:
        return run_state.total_floor >= self.MIN_TOTAL_FLOOR

    def calculate_vars(self, run_state: RunState) -> None:
        self._heal = int(run_state.player.max_hp * self.HEAL_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("fight", "Fight", "Fight 3 Fungi Beasts; win the Odd Mushroom relic"),
            EventOption("eat", "Eat", f"Heal {self._heal} HP, gain the Parasite curse"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "fight":
            return EventResult(
                finished=False,
                description="The mushrooms rise up to fight!",
                next_options=[EventOption("enter_combat", "Fight", "Fight the Fungi Beasts")],
            )
        if option_id == "enter_combat":
            return EventResult(
                finished=True,
                description="Entered combat with the Fungi Beasts.",
                rewards={
                    "reward_objects": [
                        RelicReward(run_state.player.player_id, relic_id=RelicId.ODD_MUSHROOM.name)
                    ]
                },
                event_combat_setup=self.ENCOUNTER_ID,
            )
        # eat
        healed = run_state.player.heal(self._heal)
        if _should_defer_event_rewards(run_state):
            return _event_result_with_rewards(
                f"Healed {healed} HP, gained the Parasite curse.",
                [AddCardsReward(run_state.player.player_id, [make_parasite()])],
            )
        run_state.player.add_card_instance_to_deck(make_parasite())
        return EventResult(
            finished=True,
            description=f"Healed {healed} HP, gained the Parasite curse.",
        )


register_event(Mushrooms())


# ── ScrapOoze ───────────────────────────────────────────────────────

class ScrapOoze(EventModel):
    """Reach In: take 5 damage (+1 per attempt) for a 25% (+10% per attempt)
    chance of a relic. Leave: nothing.
    """

    event_id = "ScrapOoze"
    is_legacy_exclusive = True
    BASE_DAMAGE = 5
    DAMAGE_INCREASE_PER_ATTEMPT = 1
    BASE_RELIC_CHANCE = 25
    CHANCE_INCREASE_PER_ATTEMPT = 10

    def __init__(self) -> None:
        self._attempts = 0

    @property
    def current_damage(self) -> int:
        return self.BASE_DAMAGE + self._attempts * self.DAMAGE_INCREASE_PER_ATTEMPT

    @property
    def current_relic_chance(self) -> int:
        return self.BASE_RELIC_CHANCE + self._attempts * self.CHANCE_INCREASE_PER_ATTEMPT

    def calculate_vars(self, run_state: RunState) -> None:
        self._attempts = 0

    def _options(self) -> list[EventOption]:
        return [
            EventOption(
                "reach", "Reach In",
                f"Take {self.current_damage} damage; {self.current_relic_chance}% chance of a relic",
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return self._options()

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "reach":
            damage = self.current_damage
            run_state.player.lose_hp(damage)
            # ScrapOoze.Reach: roll = Rng.NextInt(100); success iff
            # roll >= 100 - CurrentRelicChance.
            roll = self.get_rng(run_state).next_int_exclusive(0, 100)
            if roll >= 100 - self.current_relic_chance:
                obtained = _obtain_random_relics(run_state, 1)
                relic_name = obtained[0] if obtained else "a relic"
                return EventResult(
                    finished=True,
                    description=f"Took {damage} damage and found a relic ({relic_name})!",
                )
            self._attempts += 1
            return EventResult(
                finished=False,
                description=f"Took {damage} damage and found nothing.",
                next_options=self._options(),
            )
        return EventResult(finished=True, description="Left the ooze alone.")


register_event(ScrapOoze())


# ── ShiningLight ────────────────────────────────────────────────────

class ShiningLight(EventModel):
    """Enter: take 30% Max HP damage, upgrade 2 random upgradable cards.
    Leave: nothing.
    """

    event_id = "ShiningLight"
    is_legacy_exclusive = True
    HP_LOSS_PERCENT = 0.30
    UPGRADE_COUNT = 2

    def __init__(self) -> None:
        self._damage = 0

    def calculate_vars(self, run_state: RunState) -> None:
        self._damage = int(run_state.player.max_hp * self.HP_LOSS_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption(
                "enter", "Enter",
                f"Take {self._damage} damage, upgrade {self.UPGRADE_COUNT} random cards",
                enabled=bool(run_state.player.upgradable_deck_cards()),
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "enter":
            run_state.player.lose_hp(self._damage)
            upgraded = _upgrade_n_cards(run_state, self.UPGRADE_COUNT, rng=run_state.rng.niche)
            return EventResult(
                finished=True,
                description=f"Took {self._damage} damage, upgraded {upgraded} cards.",
            )
        return EventResult(finished=True, description="Left the light alone.")


register_event(ShiningLight())


# ── Sssserpent ──────────────────────────────────────────────────────

class Sssserpent(EventModel):
    """Agree: gain the Doubt curse and 150 gold. Disagree: nothing."""

    event_id = "Sssserpent"
    is_legacy_exclusive = True
    GOLD_REWARD = 150

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("agree", "Agree", f"Gain {self.GOLD_REWARD} gold and the Doubt curse"),
            EventOption("disagree", "Disagree", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "agree":
            return EventResult(
                finished=False,
                description="The serpent offers you riches.",
                next_options=[
                    EventOption(
                        "take_gold", "Take the Gold",
                        f"Gain {self.GOLD_REWARD} gold and the Doubt curse",
                    )
                ],
            )
        if option_id == "take_gold":
            # Sssserpent.TakeGold order: Doubt curse first, then the gold.
            run_state.player.gain_gold(self.GOLD_REWARD)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Gained {self.GOLD_REWARD} gold and the Doubt curse.",
                    [AddCardsReward(run_state.player.player_id, [make_doubt()])],
                )
            run_state.player.add_card_instance_to_deck(make_doubt())
            return EventResult(
                finished=True,
                description=f"Gained {self.GOLD_REWARD} gold and the Doubt curse.",
            )
        return EventResult(finished=True, description="Refused the serpent.")


register_event(Sssserpent())


# ── WingStatue ──────────────────────────────────────────────────────

class WingStatue(EventModel):
    """Pray: take 7 damage, remove a card.
    Destroy: gain 50-80 gold (requires an Attack dealing >= 10 damage).
    Leave: nothing.
    """

    event_id = "WingStatue"
    is_legacy_exclusive = True
    DAMAGE = 7
    REQUIRED_DAMAGE = 10
    MIN_GOLD = 50
    MAX_GOLD = 80

    def __init__(self) -> None:
        self._gold = self.MIN_GOLD

    def calculate_vars(self, run_state: RunState) -> None:
        # WingStatue.CalculateVars (classic): 50 + Rng.NextInt(31) -> 50-80.
        self._gold = self.MIN_GOLD + self.get_rng(run_state).next_int_exclusive(
            0, self.MAX_GOLD - self.MIN_GOLD + 1
        )

    def _can_attack(self, run_state: RunState) -> bool:
        # WingStatue.CanAttack: any Attack card whose Damage var is >= 10.
        return any(
            card.card_type == CardType.ATTACK and card.base_damage >= self.REQUIRED_DAMAGE
            for card in run_state.player.deck
        )

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("agree", "Pray", f"Take {self.DAMAGE} damage, remove a card"),
            EventOption(
                "attack", "Destroy",
                "Gain 50-80 gold (requires an Attack dealing 10+ damage)",
                enabled=self._can_attack(run_state),
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "agree":
            run_state.player.lose_hp(self.DAMAGE)
            candidates = run_state.player.removable_deck_cards()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Took {self.DAMAGE} damage, removed a card.",
                    [RemoveCardReward(run_state.player.player_id, count=1, cards=candidates)],
                )
            return self.request_card_choice(
                prompt="Choose a card to remove",
                cards=candidates,
                source_pile="deck",
                resolver=lambda selected: (
                    _remove_selected_cards(selected, run_state),
                    EventResult(
                        finished=True,
                        description=f"Took {self.DAMAGE} damage, removed a card.",
                    ),
                )[-1],
                allow_skip=False,
                min_count=1,
                max_count=1,
                description="Choose a card to remove.",
            )
        if option_id == "attack":
            run_state.player.gain_gold(self._gold)
            return EventResult(finished=True, description=f"Destroyed the statue, gained {self._gold} gold.")
        return EventResult(finished=True, description="Left the statue alone.")


register_event(WingStatue())


# ── WorldOfGoop ─────────────────────────────────────────────────────

class WorldOfGoop(EventModel):
    """Gather Gold: take 11 damage, gain 75 gold.
    Leave: lose 35-75 gold (dropped into the goop, capped at current gold).
    """

    event_id = "WorldOfGoop"
    is_legacy_exclusive = True
    DAMAGE = 11
    GOLD = 75
    MIN_GOLD_LOSS = 35
    MAX_GOLD_LOSS = 75

    def __init__(self) -> None:
        self._gold_loss = 0

    def calculate_vars(self, run_state: RunState) -> None:
        # WorldOfGoop.CalculateVars: 35 + Rng.NextInt(41), capped at the
        # player's current gold.
        loss = self.MIN_GOLD_LOSS + self.get_rng(run_state).next_int_exclusive(
            0, self.MAX_GOLD_LOSS - self.MIN_GOLD_LOSS + 1
        )
        self._gold_loss = min(loss, run_state.player.gold)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [
            EventOption("gather", "Gather Gold", f"Take {self.DAMAGE} damage, gain {self.GOLD} gold"),
            EventOption("leave", "Leave", f"Lose {self._gold_loss} gold"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "gather":
            run_state.player.lose_hp(self.DAMAGE)
            run_state.player.gain_gold(self.GOLD)
            return EventResult(
                finished=True,
                description=f"Took {self.DAMAGE} damage, gained {self.GOLD} gold.",
            )
        run_state.player.lose_gold(self._gold_loss)
        return EventResult(finished=True, description=f"Lost {self._gold_loss} gold to the goop.")


register_event(WorldOfGoop())


# ── Exordium event pool ─────────────────────────────────────────────
#
# The act-exclusive event pool for the Exordium ActConfig (analogous to the
# vanilla acts' event_ids lists in sts2_env/map/acts.py). The Exordium act
# isn't registered as a map-slot candidate yet; when it is, use this list as
# its event_ids.

EXORDIUM_EVENT_IDS: list[str] = [
    "BigFish",
    "Cleric",
    "DeadAdventurer",
    "GoldenIdol",
    "LivingWall",
    "Mushrooms",
    "ScrapOoze",
    "ShiningLight",
    "Sssserpent",
    "WingStatue",
    "WorldOfGoop",
]
