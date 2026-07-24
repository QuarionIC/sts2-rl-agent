"""Event engine base classes.

Provides the EventModel base, EventOption, and event registry,
matching MegaCrit.Sts2.Core.Models.Events/EventModel.cs patterns.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import TYPE_CHECKING, Any, Callable

from sts2_env.core.constants import (
    ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS,
    ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS,
)
from sts2_env.core.selection import CardChoiceOption, PendingCardChoice
from sts2_env.core.rng import Rng, deterministic_hash_code

if TYPE_CHECKING:
    from sts2_env.cards.base import CardInstance
    from sts2_env.run.run_state import RunState


@dataclass
class EventOption:
    """A single choice in an event."""

    option_id: str
    label: str
    description: str = ""
    enabled: bool = True

    def __repr__(self) -> str:
        return f"EventOption({self.option_id}: {self.label})"


class EventModel:
    """Base class for all events.

    Subclasses implement is_allowed(), generate_initial_options(),
    and choose() to define event behavior.
    """

    event_id: str = ""
    is_shared: bool = False
    # "Acts from the Past" mod: True for the mod's own SharedEvents (e.g.
    # BonfireSpirits, Duplicator). Only meaningful when is_shared is True --
    # see `event_allowed_in_act` below. Always False for base-game events.
    is_legacy_exclusive: bool = False
    # "Acts from the Past" mod: True for events implementing the mod's
    # IShrineEvent interface. Shrines are interleaved into LEGACY act event
    # pools at a 25% draw chance per slot (see `interleave_shrine_events`
    # below, mirroring ShrinePatches.EventPoolPatch), and non-one-time
    # shrines may be re-offered within the same run (they bypass the
    # visited-events exclusion, mirroring RepeatableShrineValidityPatch).
    is_shrine: bool = False
    # IShrineEvent.IsOneTimeEvent (C# default false). Only meaningful when
    # is_shrine is True.
    is_one_time_event: bool = False
    # "Acts from the Past" mod IActRestricted.AllowedActIndices: 1-based act
    # numbers (act slot index + 1) this event may appear in, or None for no
    # restriction (e.g. FaceTrader => (1, 2), DesignerInSpire => (2, 3)).
    allowed_act_numbers: tuple[int, ...] | None = None

    @property
    def pending_choice(self) -> PendingCardChoice | None:
        return getattr(self, "_pending_choice", None)

    @pending_choice.setter
    def pending_choice(self, value: PendingCardChoice | None) -> None:
        self._pending_choice = value

    def is_allowed(self, run_state: RunState) -> bool:
        """Whether this event can appear given current run state.

        Default = True (always allowed). Override for conditional events.
        """
        return True

    @property
    def rng(self) -> Rng | None:
        return getattr(self, "_rng", None)

    @rng.setter
    def rng(self, value: Rng | None) -> None:
        self._rng = value

    def event_entry(self) -> str:
        source = self.event_id or self.__class__.__name__
        chars: list[str] = []
        for index, char in enumerate(source.strip()):
            if char.isalnum():
                if index > 0 and char.isupper() and source[index - 1].isalnum() and not source[index - 1].isupper():
                    chars.append("_")
                chars.append(char.upper())
            elif chars and chars[-1] != "_":
                chars.append("_")
        return "".join(chars).strip("_")

    def create_event_rng(self, run_state: RunState) -> Rng:
        player_offset = 0 if self.is_shared else getattr(run_state.player, "player_id", 1)
        return Rng(run_state.rng.seed + player_offset + deterministic_hash_code(self.event_entry()))

    def get_rng(self, run_state: RunState) -> Rng:
        if self.rng is None:
            self.rng = self.create_event_rng(run_state)
        return self.rng

    def reset_rng_for_run(self, run_state: RunState) -> None:
        self.rng = self.create_event_rng(run_state)
        self._vars_calculated_for_run = None

    def ensure_vars_calculated(self, run_state: RunState) -> None:
        run_key = id(run_state)
        if getattr(self, "_vars_calculated_for_run", None) == run_key:
            return
        self.calculate_vars(run_state)
        self._vars_calculated_for_run = run_key

    def calculate_vars(self, run_state: RunState) -> None:
        """Randomize dynamic variables (damage, gold, etc.) before display."""
        pass

    def before_event_started(self, run_state: RunState) -> None:
        pass

    def on_event_finished(self, run_state: RunState) -> None:
        pass

    def generate_initial_options(self, run_state: RunState) -> list[EventOption]:
        """Return the initial set of choices for this event."""
        return []

    def choose(self, run_state: RunState, option_id: str) -> EventResult:
        """Execute a choice and return the result.

        May return a finished result or a new set of options (multi-page).
        """
        return EventResult(finished=True, description="Nothing happened.")

    def request_card_choice(
        self,
        *,
        prompt: str,
        cards: list[CardInstance],
        source_pile: str,
        resolver: Callable[[list[CardInstance]], EventResult | None],
        allow_skip: bool = False,
        min_count: int = 1,
        max_count: int = 1,
        description: str = "",
    ) -> EventResult:
        if not cards or max_count <= 0:
            return EventResult(finished=True, description=description or prompt)
        self.pending_choice = PendingCardChoice(
            prompt=prompt,
            options=[CardChoiceOption(card=card, source_pile=source_pile) for card in cards],
            resolver=resolver,
            allow_skip=allow_skip,
            min_choices=min_count,
            max_choices=max_count,
        )
        return EventResult(finished=False, description=description or prompt)

    def request_multi_card_choice(
        self,
        *,
        prompt: str,
        cards: list[CardInstance],
        source_pile: str,
        resolver: Callable[[list[CardInstance]], EventResult | None],
        allow_skip: bool = False,
        min_count: int = 1,
        max_count: int | None = None,
        description: str = "",
    ) -> EventResult:
        if max_count is None:
            max_count = len(cards)
        if not cards or max_count <= 0:
            return EventResult(finished=True, description=description or prompt)
        self.pending_choice = PendingCardChoice(
            prompt=prompt,
            options=[CardChoiceOption(card=card, source_pile=source_pile) for card in cards],
            resolver=resolver,
            allow_skip=allow_skip,
            min_choices=min_count,
            max_choices=max_count,
        )
        return EventResult(finished=False, description=description or prompt)

    def resolve_pending_choice(self, choice_index: int | None) -> EventResult:
        choice = self.pending_choice
        if choice is None:
            return EventResult(finished=False, description="No pending event choice.")

        if choice.is_multi:
            if choice_index is None:
                if not choice.can_confirm():
                    return EventResult(finished=False, description="Cannot confirm event choice.")
                selected_cards = choice.selected_cards
                self.pending_choice = None
                result = choice.resolver(selected_cards)
                return result if isinstance(result, EventResult) else EventResult(finished=True, description="Resolved event choice.")
            if not choice.toggle(choice_index):
                return EventResult(finished=False, description="Invalid event choice.")
            return EventResult(finished=False, description=choice.prompt)

        selected_cards: list[CardInstance] = []
        if choice_index is None:
            if not choice.allow_skip:
                return EventResult(finished=False, description="Cannot skip event choice.")
        else:
            if choice_index < 0 or choice_index >= len(choice.options):
                return EventResult(finished=False, description="Invalid event choice.")
            selected_cards = [choice.options[choice_index].card]
        self.pending_choice = None
        result = choice.resolver(selected_cards)
        return result if isinstance(result, EventResult) else EventResult(finished=True, description="Resolved event choice.")


@dataclass
class EventResult:
    """Result of choosing an event option."""

    finished: bool = True
    description: str = ""
    next_options: list[EventOption] = field(default_factory=list)
    rewards: dict[str, Any] = field(default_factory=dict)
    event_combat_setup: str | None = None
    post_combat_phase: str | None = None
    preserve_reward_order: bool = False


# ── Event Registry ────────────────────────────────────────────────────

_EVENT_REGISTRY: dict[str, EventModel] = {}


def register_event(event: EventModel) -> EventModel:
    _EVENT_REGISTRY[event.event_id] = event
    return event


def get_event(event_id: str) -> EventModel | None:
    return _EVENT_REGISTRY.get(event_id)


def all_events() -> list[EventModel]:
    return list(_EVENT_REGISTRY.values())


def event_allowed_in_act(event: EventModel, act: object) -> bool:
    """"Acts from the Past" mod SharedEvents pool filter.

    Only SharedEvents are gated (act-exclusive event pools are untouched
    either way -- non-shared events return True unconditionally). Decompiled
    reference: ActsFromThePast.Patches.Events.ShrinePatches (Postfix on
    ActModel's room-set patch), which removes base-game SharedEvents from
    legacy acts unless AllowNonLegacySharedEventsInLegacyActs, and removes
    the mod's own SharedEvents from non-legacy acts unless
    AllowLegacySharedEventsInNonLegacyActs.
    """
    if not getattr(event, "is_shared", False):
        return True
    is_legacy_act = getattr(act, "is_legacy", False)
    is_legacy_event = getattr(event, "is_legacy_exclusive", False)
    if is_legacy_act and not is_legacy_event and not ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS:
        return False
    if not is_legacy_act and is_legacy_event and not ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS:
        return False
    return True


def _event_blocked_by_visited(event: EventModel, run_state: RunState) -> bool:
    """Visited-events exclusion, with the "Acts from the Past" mod's
    RepeatableShrineValidityPatch semantics: a non-one-time shrine event may
    be re-offered even after it was visited this run (the patch temporarily
    removes such events' ids from RunState's visited set while the next
    event is validated)."""
    if event.event_id not in run_state.visited_event_ids:
        return False
    if getattr(event, "is_shrine", False) and not getattr(event, "is_one_time_event", False):
        return False
    return True


def get_allowed_events(run_state: RunState, pool: list[str] | None = None) -> list[EventModel]:
    """Return events from pool that pass is_allowed and haven't been visited."""
    candidates = all_events() if pool is None else [
        _EVENT_REGISTRY[eid] for eid in pool if eid in _EVENT_REGISTRY
    ]
    return [
        e for e in candidates
        if not _event_blocked_by_visited(e, run_state)
        and e.is_allowed(run_state)
        and event_allowed_in_act(e, run_state.current_act)
    ]


def pick_event(run_state: RunState, pool: list[str] | None = None) -> EventModel | None:
    """Pick the next allowed event from the current act event order."""
    act = run_state.current_act
    if pool is None:
        pool = act.event_ids
    uses_act_event_order = pool is act.event_ids
    if not pool:
        return None

    event = None
    event_index = act.events_visited if uses_act_event_order else 0
    for _ in range(len(pool)):
        event_id = pool[event_index % len(pool)]
        candidate = _EVENT_REGISTRY.get(event_id)
        if (
            candidate is not None
            and not _event_blocked_by_visited(candidate, run_state)
            and candidate.is_allowed(run_state)
            and event_allowed_in_act(candidate, act)
        ):
            event = candidate
            break
        event_index += 1
    if event is None and uses_act_event_order:
        event = _EVENT_REGISTRY.get(pool[event_index % len(pool)])

    for player in run_state.players:
        for card in player.deck:
            event = card.modify_next_event(run_state, event)
    if event is not None:
        run_state.visited_event_ids.add(event.event_id)
        event_index += 1
    if uses_act_event_order:
        act.events_visited = event_index
    return event


# ── "Acts from the Past" shrine pool logic ───────────────────────────────
#
# Decompiled reference: ActsFromThePast.Patches.Events.ShrinePatches
# .EventPoolPatch (Postfix on ActModel.GenerateRooms). After the config
# gates above (event_allowed_in_act) and IActRestricted filtering, a LEGACY
# act's event pool is rebuilt by interleaving shrine events (IShrineEvent)
# with the non-shrine events: for each output slot, a shrine is drawn with
# 25% probability (rng.NextFloat(1f) < 0.25f) while shrines remain;
# otherwise the next non-shrine event is taken; once non-shrines run out
# the remaining shrines are appended.

SHRINE_DRAW_CHANCE = 0.25


def build_legacy_event_pool(event_ids: list[str], act: object, rng: Rng) -> list[str]:
    """Build a LEGACY act's ordered event pool (ShrinePatches.EventPoolPatch).

    ``event_ids`` is the candidate pool (the act's own event ids plus any
    shared event ids); events are filtered through ``event_allowed_in_act``
    and the mod's IActRestricted act-number gate (1-based act number ==
    act.act_index + 1), then shrine events are interleaved with non-shrine
    events at a 25% draw chance per slot. Intended to be called only for
    acts flagged ``is_legacy`` (vanilla acts keep their static event_ids
    lists); the relative order of ``event_ids`` is preserved within each of
    the shrine/non-shrine halves, matching the C# patch.
    """
    act_number = getattr(act, "act_index", 0) + 1
    filtered: list[EventModel] = []
    for event_id in event_ids:
        event = _EVENT_REGISTRY.get(event_id)
        if event is None:
            continue
        if not event_allowed_in_act(event, act):
            continue
        allowed_acts = getattr(event, "allowed_act_numbers", None)
        if allowed_acts is not None and act_number not in allowed_acts:
            continue
        filtered.append(event)

    shrines = [event for event in filtered if getattr(event, "is_shrine", False)]
    if not shrines:
        return [event.event_id for event in filtered]
    regulars = [event for event in filtered if not getattr(event, "is_shrine", False)]

    interleaved: list[EventModel] = []
    shrine_index = 0
    regular_index = 0
    for _ in range(len(shrines) + len(regulars)):
        if rng.next_float(1.0) < SHRINE_DRAW_CHANCE and shrine_index < len(shrines):
            interleaved.append(shrines[shrine_index])
            shrine_index += 1
        elif regular_index < len(regulars):
            interleaved.append(regulars[regular_index])
            regular_index += 1
        else:
            interleaved.append(shrines[shrine_index])
            shrine_index += 1
    return [event.event_id for event in interleaved]
