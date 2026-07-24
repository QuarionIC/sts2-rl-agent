"""TheBeyond (Act-3-slot legacy act) events -- "Acts from the Past" mod.

Implemented against the decompiled mod source in
``decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.TheBeyond.Events/*.cs``
(plus the event-encounter classes TwoOrbWalkersEvent / MindBloomGuardian /
MindBloomHexaghost / MindBloomSlimeBoss in ``.../Encounters/``).

CLASSIC MODE ONLY: the user's mod config has ``RebalancedMode=False``, so
every ``ActsFromThePastConfig.RebalancedMode`` branch in the source is a dead
branch here and is intentionally NOT implemented (MoaiHead's Harvest potion,
MysteriousSphere's Distract, SecretPortal's map-builder minigame + ReachIn,
SensoryStone's 10/20 damage numbers, TombOfLordRedMask's Fearful-enchant Run,
WindingHalls' 1-Madness variant, Falling is unchanged).

Number corrections vs. the planning spec (all read from source):

- MindBloom "I am War": the fight pays a BOSS-tier card reward + exactly 50
  gold + 1 RARE relic (MindBloomPatches.RewardsPatch strips the default
  gold/relic and MindBloomEncounterRoomTypePatch reports RoomType 3 == Boss
  while the fight is active, so the default boss card reward/potion roll
  survives). The 999-gold option adds exactly 2 Normality curses and is
  gated on ``TotalFloor < 41`` (38 in multiplayer), not literally on the
  treasure room; the alternative is full heal + 1 Doubt.
- MoaiHead's Pray loses 18% *Max HP* (LoseMaxHp, Math.Round), then fully
  heals. Gate: visited an Exordium legacy act earlier OR any player at
  <= 50% HP (not strictly below).
- MysteriousSphere's fight pays exactly 45-55 gold + 1 RARE relic (no card
  reward -- default rewards stay suppressed for its RoomType).
- SecretPortal is an IShrineEvent (one-time). Its C# gate is wall-clock run
  time > 800s, which has no headless equivalent; the sim leaves it always
  allowed (documented proxy decision).
- SensoryStone: 1/2/3 colorless card rewards (3 options each) at 0/5/10
  self-damage (classic numbers confirmed).
- WindingHalls Madness: take 18% Max HP damage and gain 2 Madness cards;
  Writhe: heal 20% Max HP + Writhe curse; Retreat: lose 5% *Max HP*
  (LoseMaxHp, not current HP). All three percents use Math.Round.
- Falling removes a RANDOM eligible card per category (rolled once with the
  event rng when the event is entered), not a chosen one; the three options
  only pick WHICH category.

Event-embedded combats use RunManager's ``_enter_event_combat`` mechanism
(``EventResult.event_combat_setup`` + explicit ``reward_objects``), which
creates the CombatRoom with ``suppress_default_rewards=True``: the fight
pays out EXACTLY the event's reward list (MindBloom's boss-tier card reward
and potion roll are therefore enumerated explicitly).
"""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.cards.factory import create_card
from sts2_env.cards.status import make_doubt, make_normality, make_writhe
from sts2_env.core.enums import CardId, CardType
from sts2_env.events.shared import (
    _event_result_with_rewards,
    _should_defer_event_rewards,
    _upgrade_selected_cards,
)
from sts2_env.relics.base import RelicId, RelicRarity
from sts2_env.run.events import EventModel, EventOption, EventResult, register_event
from sts2_env.run.reward_objects import (
    AddCardsReward,
    CardReward,
    GoldReward,
    PotionReward,
    RelicReward,
)

# Importing the encounter module registers the event-only encounters
# (two_orb_walkers_event / mind_bloom_*) in EVENT_ENCOUNTER_REGISTRY.
import sts2_env.encounters.thebeyond  # noqa: F401

if TYPE_CHECKING:
    from sts2_env.cards.base import CardInstance
    from sts2_env.run.run_state import RunState


def _has_visited_exordium(run_state: RunState) -> bool:
    """MoaiHead.HasVisitedExordium: any act BEFORE the current one is an
    ExordiumAct (identified here by ActConfig.act_id == "Exordium")."""
    for act in run_state.acts[: run_state.current_act_index]:
        if getattr(act, "act_id", "") == "Exordium":
            return True
    return False


# ── Falling ─────────────────────────────────────────────────────────

class Falling(EventModel):
    """Choose which of a random Skill / Power / Attack (rolled from your
    deck when the event begins) falls out of your deck. No leaving.
    """

    event_id = "Falling"
    is_legacy_exclusive = True

    def __init__(self) -> None:
        self._skill_card: CardInstance | None = None
        self._power_card: CardInstance | None = None
        self._attack_card: CardInstance | None = None

    def calculate_vars(self, run_state: RunState) -> None:
        # Falling.SetCards: one random removable card per category, rolled
        # with the event rng (Rng.NextItem).
        rng = self.get_rng(run_state)
        deck = run_state.player.deck
        skills = [c for c in deck if c.card_type == CardType.SKILL and c.is_removable]
        powers = [c for c in deck if c.card_type == CardType.POWER and c.is_removable]
        attacks = [c for c in deck if c.card_type == CardType.ATTACK and c.is_removable]
        self._skill_card = rng.choice(skills) if skills else None
        self._power_card = rng.choice(powers) if powers else None
        self._attack_card = rng.choice(attacks) if attacks else None

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("continue", "Continue", "Choose what to lose")]

    def _choice_options(self) -> list[EventOption]:
        def _label(kind: str, card: CardInstance | None) -> EventOption:
            if card is None:
                return EventOption(
                    f"{kind}_locked", f"{kind.title()} (Locked)",
                    f"No {kind} card to lose", enabled=False,
                )
            return EventOption(kind, kind.title(), f"Lose {card.card_id.name}")

        options = [
            _label("skill", self._skill_card),
            _label("power", self._power_card),
            _label("attack", self._attack_card),
        ]
        # Sim-only safety valve: with NO removable Skill/Power/Attack at all
        # the C# event would soft-lock the run; offer a no-op exit instead.
        if all(not option.enabled for option in options):
            options.append(EventOption("leave", "Land", "Nothing to lose"))
        return options

    def _remove(self, run_state: RunState, card: CardInstance | None, kind: str) -> EventResult:
        from sts2_env.events.shared import _remove_selected_cards

        if card is not None:
            _remove_selected_cards([card], run_state)
            return EventResult(
                finished=True,
                description=f"Lost {card.card_id.name} ({kind}).",
            )
        return EventResult(finished=True, description="Nothing was lost.")

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="Something must be left behind.",
                next_options=self._choice_options(),
            )
        if option_id == "skill":
            return self._remove(run_state, self._skill_card, "Skill")
        if option_id == "power":
            return self._remove(run_state, self._power_card, "Power")
        if option_id == "attack":
            return self._remove(run_state, self._attack_card, "Attack")
        return EventResult(finished=True, description="Landed.")


register_event(Falling())


# ── MindBloom ───────────────────────────────────────────────────────

class MindBloom(EventModel):
    """I am War: fight a random Exordium boss reskin (boss-tier card reward
    + 50 gold + a RARE relic). I am Awake: upgrade ALL cards, gain Mark of
    the Bloom (no more healing). Before floor 41: I am Rich (999 gold + 2
    Normality); after: I am Healthy (full heal + Doubt).
    """

    event_id = "MindBloom"
    is_shared = True
    is_legacy_exclusive = True
    FIGHT_GOLD = 50
    GOLD_REWARD = 999
    NORMALITY_COUNT = 2
    BEFORE_TREASURE_FLOOR_1P = 41
    BEFORE_TREASURE_FLOOR_MP = 38
    _ENCOUNTER_IDS = (
        "mind_bloom_guardian",
        "mind_bloom_hexaghost",
        "mind_bloom_slime_boss",
    )

    def __init__(self) -> None:
        self._is_before_treasure = True

    def calculate_vars(self, run_state: RunState) -> None:
        threshold = (
            self.BEFORE_TREASURE_FLOOR_MP
            if len(run_state.players) > 1
            else self.BEFORE_TREASURE_FLOOR_1P
        )
        self._is_before_treasure = run_state.total_floor < threshold

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        options = [
            EventOption("fight", "I am War",
                        "Fight an old boss; win a boss card reward, 50 gold and a rare relic"),
            EventOption("upgrade", "I am Awake",
                        "Upgrade ALL cards; gain Mark of the Bloom (you can no longer heal)"),
        ]
        if self._is_before_treasure:
            options.append(EventOption("gold", "I am Rich",
                                       "Gain 999 gold and 2 Normality curses"))
        else:
            options.append(EventOption("heal", "I am Healthy",
                                       "Heal to full, gain a Doubt curse"))
        return options

    def _fight_rewards(self, run_state: RunState) -> list[object]:
        # MindBloomPatches: RoomType 3 (Boss) while the fight is active, so
        # the default boss-room reward roll happens (card reward + potion
        # odds), then RewardsPatch strips the default gold/relic in favor of
        # the event's exact 50 gold + RARE relic.
        player_id = run_state.player.player_id
        rewards: list[object] = [CardReward(player_id, context="boss")]
        if run_state.potion_reward_odds.roll(run_state.rng.rewards, is_elite=False):
            rewards.append(PotionReward(player_id))
        rewards.append(GoldReward(player_id, self.FIGHT_GOLD, self.FIGHT_GOLD))
        rewards.append(RelicReward(player_id, rarity=RelicRarity.RARE))
        return rewards

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        if option_id == "fight":
            encounter_id = self.get_rng(run_state).choice(list(self._ENCOUNTER_IDS))
            return EventResult(
                finished=True,
                description="The bloom shows you war.",
                rewards={"reward_objects": self._fight_rewards(run_state)},
                event_combat_setup=encounter_id,
            )
        if option_id == "upgrade":
            candidates = run_state.player.upgradable_deck_cards()
            upgraded = _upgrade_selected_cards(candidates, run_state)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Upgraded {upgraded} cards, gained Mark of the Bloom.",
                    [RelicReward(run_state.player.player_id,
                                 relic_id=RelicId.MARK_OF_THE_BLOOM.name)],
                )
            run_state.player.obtain_relic(RelicId.MARK_OF_THE_BLOOM.name)
            return EventResult(
                finished=True,
                description=f"Upgraded {upgraded} cards, gained Mark of the Bloom.",
            )
        if option_id == "gold":
            run_state.player.gain_gold(self.GOLD_REWARD)
            curses = [make_normality() for _ in range(self.NORMALITY_COUNT)]
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Gained {self.GOLD_REWARD} gold and {self.NORMALITY_COUNT} Normality curses.",
                    [AddCardsReward(run_state.player.player_id, curses)],
                )
            for curse in curses:
                run_state.player.add_card_instance_to_deck(curse)
            return EventResult(
                finished=True,
                description=f"Gained {self.GOLD_REWARD} gold and {self.NORMALITY_COUNT} Normality curses.",
            )
        if option_id == "heal":
            healed = run_state.player.heal(run_state.player.max_hp)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Healed {healed} HP, gained a Doubt curse.",
                    [AddCardsReward(run_state.player.player_id, [make_doubt()])],
                )
            run_state.player.add_card_instance_to_deck(make_doubt())
            return EventResult(
                finished=True,
                description=f"Healed {healed} HP, gained a Doubt curse.",
            )
        return EventResult(finished=True, description="Nothing happened.")


register_event(MindBloom())


# ── MoaiHead ────────────────────────────────────────────────────────

class MoaiHead(EventModel):
    """Pray: lose 18% Max HP, heal to full. Offer Gold Idol: trade the
    Golden Idol relic for 333 gold (only if held). Leave: nothing.
    """

    event_id = "MoaiHead"
    is_legacy_exclusive = True
    HP_LOSS_PERCENT = 0.18
    GOLD = 333
    HP_GATE_PERCENT = 0.5

    def __init__(self) -> None:
        self._max_hp_loss = 0

    def is_allowed(self, run_state: RunState) -> bool:
        # MoaiHead.IsAllowed: visited an Exordium legacy act earlier this
        # run, OR any player at <= 50% HP.
        if _has_visited_exordium(run_state):
            return True
        return any(
            player.current_hp / player.max_hp <= self.HP_GATE_PERCENT
            for player in run_state.players
        )

    def calculate_vars(self, run_state: RunState) -> None:
        # C#: Math.Round(MaxHp * 0.18m) -- LoseMaxHp, then full heal.
        self._max_hp_loss = round(run_state.player.max_hp * self.HP_LOSS_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        has_idol = RelicId.GOLDEN_IDOL.name in run_state.player.relics
        return [
            EventOption("pray", "Pray",
                        f"Lose {self._max_hp_loss} Max HP, heal to full"),
            EventOption(
                "offer_idol", "Offer Gold Idol",
                f"Lose the Golden Idol, gain {self.GOLD} gold",
                enabled=has_idol,
            ),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        if option_id == "pray":
            player.lose_max_hp(self._max_hp_loss)
            healed = player.heal(player.max_hp)
            return EventResult(
                finished=True,
                description=f"Lost {self._max_hp_loss} Max HP, healed {healed} HP.",
            )
        if option_id == "offer_idol":
            relic_name = RelicId.GOLDEN_IDOL.name
            if relic_name in player.relics:
                relic_index = player.relics.index(relic_name)
                player.relics.pop(relic_index)
                if relic_index < len(player.relic_objects):
                    player.relic_objects.pop(relic_index)
            player.gain_gold(self.GOLD)
            return EventResult(
                finished=True,
                description=f"Offered the Golden Idol, gained {self.GOLD} gold.",
            )
        return EventResult(finished=True, description="Left the head alone.")


register_event(MoaiHead())


# ── MysteriousSphere ────────────────────────────────────────────────

class MysteriousSphere(EventModel):
    """Open: fight 2 Orb Walkers; win 45-55 gold and a RARE relic.
    Leave: nothing.
    """

    event_id = "MysteriousSphere"
    is_shared = True
    is_legacy_exclusive = True
    GOLD_MIN = 45
    GOLD_MAX = 55
    ENCOUNTER_ID = "two_orb_walkers_event"

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("open", "Open", "Open the sphere (something stirs inside)"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "open":
            return EventResult(
                finished=False,
                description="Two Orb Walkers emerge from the sphere!",
                next_options=[EventOption("fight", "Fight", "Fight the Orb Walkers")],
            )
        if option_id == "fight":
            player_id = run_state.player.player_id
            return EventResult(
                finished=True,
                description="Entered combat with the Orb Walkers.",
                rewards={
                    "reward_objects": [
                        GoldReward(player_id, self.GOLD_MIN, self.GOLD_MAX),
                        RelicReward(player_id, rarity=RelicRarity.RARE),
                    ]
                },
                event_combat_setup=self.ENCOUNTER_ID,
            )
        return EventResult(finished=True, description="Left the sphere alone.")


register_event(MysteriousSphere())


# ── SecretPortal ────────────────────────────────────────────────────

class SecretPortal(EventModel):
    """Enter: teleport directly to this act's boss. Leave: nothing.

    One-time IShrineEvent. C# IsAllowed gates on RunManager.RunTime > 800
    wall-clock seconds; the headless simulator has no wall clock, so the
    event is always allowed here (documented proxy decision -- there is no
    defensible floor/turn analogue for real-time played).
    """

    event_id = "SecretPortal"
    is_shared = True
    is_legacy_exclusive = True
    is_shrine = True
    is_one_time_event = True

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [
            EventOption("enter", "Enter the Portal", "Skip directly to the boss"),
            EventOption("leave", "Leave", "Nothing happens"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "enter":
            return EventResult(
                finished=False,
                description="The portal crackles. The boss lies beyond.",
                next_options=[EventOption("teleport", "Continue", "Face the boss now")],
            )
        if option_id == "teleport":
            return EventResult(
                finished=True,
                description="Stepped through the portal to the boss.",
                rewards={"jump_to_boss": True},
            )
        return EventResult(finished=True, description="Left the portal alone.")


register_event(SecretPortal())


# ── SensoryStone ────────────────────────────────────────────────────

class SensoryStone(EventModel):
    """Recall 1/2/3 memories: gain that many colorless card rewards for
    0/5/10 self-damage (classic numbers).
    """

    event_id = "SensoryStone"
    is_legacy_exclusive = True
    DAMAGE_2 = 5
    DAMAGE_3 = 10
    CARD_OPTIONS_PER_REWARD = 3

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        return [EventOption("continue", "Touch", "Relive memories")]

    def _memory_options(self) -> list[EventOption]:
        return [
            EventOption("memory_1", "Recall 1 Memory", "Gain a colorless card reward"),
            EventOption("memory_2", "Recall 2 Memories",
                        f"Take {self.DAMAGE_2} damage, gain 2 colorless card rewards"),
            EventOption("memory_3", "Recall 3 Memories",
                        f"Take {self.DAMAGE_3} damage, gain 3 colorless card rewards"),
        ]

    def _colorless_reward(self, run_state: RunState) -> CardReward:
        # SensoryStone.Memory: CardReward over the ColorlessCardPool with
        # default non-combat odds (same shape as BrainLeech in act1.py).
        return CardReward(
            run_state.player.player_id,
            option_count=self.CARD_OPTIONS_PER_REWARD,
            character_ids=(),
            include_colorless=True,
            generation_context=None,
            roll_upgrade=False,
            use_default_character_pool=False,
        )

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="The stone hums with old memories.",
                next_options=self._memory_options(),
            )
        counts = {"memory_1": (1, 0), "memory_2": (2, self.DAMAGE_2), "memory_3": (3, self.DAMAGE_3)}
        if option_id in counts:
            count, damage = counts[option_id]
            if damage:
                run_state.player.lose_hp(damage)
            rewards = [self._colorless_reward(run_state) for _ in range(count)]
            description = (
                f"Relived {count} memor{'y' if count == 1 else 'ies'}"
                + (f", took {damage} damage." if damage else ".")
            )
            return _event_result_with_rewards(description, rewards)
        return EventResult(finished=True, description="Nothing happened.")


register_event(SensoryStone())


# ── TombOfLordRedMask ───────────────────────────────────────────────

class TombOfLordRedMask(EventModel):
    """Wear the mask (Red Mask held): gain 222 gold. Pay respects (no Red
    Mask): lose ALL gold, gain the Red Mask. Leave: nothing.
    """

    event_id = "TombOfLordRedMask"
    is_legacy_exclusive = True
    GOLD = 222

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        has_mask = RelicId.RED_MASK.name in run_state.player.relics
        options: list[EventOption] = []
        if has_mask:
            options.append(EventOption("wear_mask", "Wear the Mask",
                                       f"Gain {self.GOLD} gold"))
        else:
            options.append(EventOption("wear_mask", "Wear the Mask (Locked)",
                                       "Requires the Red Mask", enabled=False))
            options.append(EventOption("pay_respects", "Pay Respects",
                                       "Lose ALL gold, gain the Red Mask"))
        options.append(EventOption("leave", "Leave", "Nothing happens"))
        return options

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        player = run_state.player
        if option_id == "wear_mask" and RelicId.RED_MASK.name in player.relics:
            player.gain_gold(self.GOLD)
            return EventResult(
                finished=True,
                description=f"Lord Red Mask approves. Gained {self.GOLD} gold.",
            )
        if option_id == "pay_respects" and RelicId.RED_MASK.name not in player.relics:
            lost = player.lose_all_gold()
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Paid {lost} gold in respects, gained the Red Mask.",
                    [RelicReward(player.player_id, relic_id=RelicId.RED_MASK.name)],
                )
            player.obtain_relic(RelicId.RED_MASK.name)
            return EventResult(
                finished=True,
                description=f"Paid {lost} gold in respects, gained the Red Mask.",
            )
        return EventResult(finished=True, description="Left the tomb.")


register_event(TombOfLordRedMask())


# ── WindingHalls ────────────────────────────────────────────────────

class WindingHalls(EventModel):
    """Embrace Madness: take 18% Max HP damage, add 2 Madness cards.
    Focus on Pain: heal 20% Max HP, gain a Writhe curse.
    Retreat: lose 5% Max HP.
    """

    event_id = "WindingHalls"
    is_legacy_exclusive = True
    HP_LOSS_PERCENT = 0.18
    HEAL_PERCENT = 0.20
    MAX_HP_LOSS_PERCENT = 0.05
    MADNESS_COUNT = 2

    def __init__(self) -> None:
        self._hp_loss = 0
        self._heal = 0
        self._max_hp_loss = 0

    def calculate_vars(self, run_state: RunState) -> None:
        max_hp = run_state.player.max_hp
        # C# uses Math.Round on all three.
        self._hp_loss = round(max_hp * self.HP_LOSS_PERCENT)
        self._heal = round(max_hp * self.HEAL_PERCENT)
        self._max_hp_loss = round(max_hp * self.MAX_HP_LOSS_PERCENT)

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        self.ensure_vars_calculated(run_state)
        return [EventOption("continue", "Continue", "Wander the endless halls")]

    def _choice_options(self) -> list[EventOption]:
        return [
            EventOption("madness", "Embrace Madness",
                        f"Take {self._hp_loss} damage, add {self.MADNESS_COUNT} Madness cards"),
            EventOption("writhe", "Focus on Pain",
                        f"Heal {self._heal} HP, gain a Writhe curse"),
            EventOption("retreat", "Retreat", f"Lose {self._max_hp_loss} Max HP"),
        ]

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        self.ensure_vars_calculated(run_state)
        player = run_state.player
        if option_id == "continue":
            return EventResult(
                finished=False,
                description="The halls twist back on themselves.",
                next_options=self._choice_options(),
            )
        if option_id == "madness":
            player.lose_hp(self._hp_loss)
            cards = [create_card(CardId.MADNESS) for _ in range(self.MADNESS_COUNT)]
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Took {self._hp_loss} damage, gained {self.MADNESS_COUNT} Madness cards.",
                    [AddCardsReward(player.player_id, cards)],
                )
            for card in cards:
                player.add_card_instance_to_deck(card)
            return EventResult(
                finished=True,
                description=f"Took {self._hp_loss} damage, gained {self.MADNESS_COUNT} Madness cards.",
            )
        if option_id == "writhe":
            healed = player.heal(self._heal)
            if _should_defer_event_rewards(run_state):
                return _event_result_with_rewards(
                    f"Healed {healed} HP, gained a Writhe curse.",
                    [AddCardsReward(player.player_id, [make_writhe()])],
                )
            player.add_card_instance_to_deck(make_writhe())
            return EventResult(
                finished=True,
                description=f"Healed {healed} HP, gained a Writhe curse.",
            )
        if option_id == "retreat":
            player.lose_max_hp(self._max_hp_loss)
            return EventResult(
                finished=True,
                description=f"Retreated, lost {self._max_hp_loss} Max HP.",
            )
        return EventResult(finished=True, description="Nothing happened.")


register_event(WindingHalls())


# ── TheBeyond event pool ────────────────────────────────────────────
#
# The act-exclusive event pool for the TheBeyond ActConfig (analogous to
# EXORDIUM_EVENT_IDS / THECITY_EVENT_IDS). The TheBeyond act isn't
# registered as a map-slot candidate yet; when it is, feed this list (plus
# AFTP_SHARED_EVENT_IDS from events/aftp_shared.py) through
# run/events.py::build_legacy_event_pool as its event_ids.

THEBEYOND_EVENT_IDS: list[str] = [
    "Falling",
    "MindBloom",
    "MoaiHead",
    "MysteriousSphere",
    "SecretPortal",
    "SensoryStone",
    "TombOfLordRedMask",
    "WindingHalls",
]
