"""Card effect registry and dispatch."""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.enums import CardId

if TYPE_CHECKING:
    from sts2_env.cards.base import CardInstance
    from sts2_env.core.combat import CombatState
    from sts2_env.core.creature import Creature
    from sts2_env.core.enums import RoomType, TargetType
    from sts2_env.map.generator import ActMap
    from sts2_env.run.events import EventModel
    from sts2_env.run.rest_site import RestSiteOption
    from sts2_env.run.run_state import PlayerState, RunState

# Type alias for card effect functions
CardEffect = Callable[["CardInstance", "CombatState", "Creature | None"], None]
CardLateEffect = Callable[["CardInstance", "CardInstance", "CombatState"], None]
CardChosenHook = Callable[
    ["CardInstance", "CombatState"],
    None,
]
CardPlayabilityHook = Callable[
    ["CardInstance", object, "CombatState", "Creature"],
    bool,
]
CardShouldPlayHook = Callable[
    ["CardInstance", "CardInstance", object, "CombatState", "Creature", bool],
    bool,
]
CardTargetTypeHook = Callable[
    ["CardInstance", "Creature", "TargetType"],
    "TargetType",
]
CardAfterCombatEndHook = Callable[
    ["CardInstance", "PlayerState"],
    bool,
]
CardNextEventHook = Callable[
    ["CardInstance", "RunState", "EventModel | None"],
    "EventModel | None",
]
CardUnknownRoomTypesHook = Callable[
    ["CardInstance", "RunState", set["RoomType"]],
    set["RoomType"],
]
CardGeneratedMapHook = Callable[
    ["CardInstance", "RunState", "ActMap", int],
    "ActMap",
]
CardAfterMapGeneratedHook = Callable[
    ["CardInstance", "RunState", "ActMap", int],
    None,
]
CardQuestCompleteHook = Callable[
    ["CardInstance", "RunState"],
    int,
]
CardRestSiteOptionsHook = Callable[
    ["CardInstance", "PlayerState", list["RestSiteOption"], "RunState | None"],
    list["RestSiteOption"],
]

_CARD_EFFECTS: dict[CardId, CardEffect] = {}
_CARD_LATE_EFFECTS: dict[CardId, CardLateEffect] = {}
_CARD_CHOSEN_HOOKS: dict[CardId, CardChosenHook] = {}
_CARD_PLAYABILITY_HOOKS: dict[CardId, CardPlayabilityHook] = {}
_CARD_SHOULD_PLAY_HOOKS: dict[CardId, CardShouldPlayHook] = {}
_CARD_TARGET_TYPE_HOOKS: dict[CardId, CardTargetTypeHook] = {}
_CARD_AFTER_COMBAT_END_HOOKS: dict[CardId, CardAfterCombatEndHook] = {}
_CARD_NEXT_EVENT_HOOKS: dict[CardId, CardNextEventHook] = {}
_CARD_UNKNOWN_ROOM_TYPES_HOOKS: dict[CardId, CardUnknownRoomTypesHook] = {}
_CARD_GENERATED_MAP_HOOKS: dict[CardId, CardGeneratedMapHook] = {}
_CARD_GENERATED_MAP_LATE_HOOKS: dict[CardId, CardGeneratedMapHook] = {}
_CARD_AFTER_MAP_GENERATED_HOOKS: dict[CardId, CardAfterMapGeneratedHook] = {}
_CARD_QUEST_COMPLETE_HOOKS: dict[CardId, CardQuestCompleteHook] = {}
_CARD_REST_SITE_OPTIONS_HOOKS: dict[CardId, CardRestSiteOptionsHook] = {}


def register_effect(card_id: CardId):
    """Decorator to register a card's play effect function."""
    def decorator(func: CardEffect) -> CardEffect:
        _CARD_EFFECTS[card_id] = func
        return func
    return decorator


def register_late_effect(card_id: CardId):
    """Decorator to register a card's post-play late effect function."""
    def decorator(func: CardLateEffect) -> CardLateEffect:
        _CARD_LATE_EFFECTS[card_id] = func
        return func
    return decorator


def register_chosen_hook(card_id: CardId):
    def decorator(func: CardChosenHook) -> CardChosenHook:
        _CARD_CHOSEN_HOOKS[card_id] = func
        return func
    return decorator


def register_playability_hook(card_id: CardId):
    def decorator(func: CardPlayabilityHook) -> CardPlayabilityHook:
        _CARD_PLAYABILITY_HOOKS[card_id] = func
        return func
    return decorator


def register_should_play_hook(card_id: CardId):
    def decorator(func: CardShouldPlayHook) -> CardShouldPlayHook:
        _CARD_SHOULD_PLAY_HOOKS[card_id] = func
        return func
    return decorator


def register_target_type_hook(card_id: CardId):
    def decorator(func: CardTargetTypeHook) -> CardTargetTypeHook:
        _CARD_TARGET_TYPE_HOOKS[card_id] = func
        return func
    return decorator


def register_after_combat_end_hook(card_id: CardId):
    def decorator(func: CardAfterCombatEndHook) -> CardAfterCombatEndHook:
        _CARD_AFTER_COMBAT_END_HOOKS[card_id] = func
        return func
    return decorator


def register_next_event_hook(card_id: CardId):
    def decorator(func: CardNextEventHook) -> CardNextEventHook:
        _CARD_NEXT_EVENT_HOOKS[card_id] = func
        return func
    return decorator


def register_unknown_room_types_hook(card_id: CardId):
    def decorator(func: CardUnknownRoomTypesHook) -> CardUnknownRoomTypesHook:
        _CARD_UNKNOWN_ROOM_TYPES_HOOKS[card_id] = func
        return func
    return decorator


def register_generated_map_hook(card_id: CardId):
    def decorator(func: CardGeneratedMapHook) -> CardGeneratedMapHook:
        _CARD_GENERATED_MAP_HOOKS[card_id] = func
        return func
    return decorator


def register_generated_map_late_hook(card_id: CardId):
    def decorator(func: CardGeneratedMapHook) -> CardGeneratedMapHook:
        _CARD_GENERATED_MAP_LATE_HOOKS[card_id] = func
        return func
    return decorator


def register_after_map_generated_hook(card_id: CardId):
    def decorator(func: CardAfterMapGeneratedHook) -> CardAfterMapGeneratedHook:
        _CARD_AFTER_MAP_GENERATED_HOOKS[card_id] = func
        return func
    return decorator


def register_quest_complete_hook(card_id: CardId):
    def decorator(func: CardQuestCompleteHook) -> CardQuestCompleteHook:
        _CARD_QUEST_COMPLETE_HOOKS[card_id] = func
        return func
    return decorator


def register_rest_site_options_hook(card_id: CardId):
    def decorator(func: CardRestSiteOptionsHook) -> CardRestSiteOptionsHook:
        _CARD_REST_SITE_OPTIONS_HOOKS[card_id] = func
        return func
    return decorator


def play_card_effect(
    card: "CardInstance",
    combat: "CombatState",
    target: "Creature | None",
) -> None:
    """Execute a card's effect. Raises KeyError if card has no registered effect."""
    effect = _CARD_EFFECTS.get(card.card_id)
    if effect is None:
        if card.is_unplayable or card.is_status:
            return  # Status/unplayable cards have no effect
        raise KeyError(f"No effect registered for {card.card_id}")
    try:
        effect(card, combat, target)
    finally:
        combat.flush_pending_attack_context()


def fire_card_late_effects(
    watched_card: "CardInstance",
    played_card: "CardInstance",
    combat: "CombatState",
) -> None:
    effect = _CARD_LATE_EFFECTS.get(watched_card.card_id)
    if effect is not None:
        effect(watched_card, played_card, combat)


def fire_card_chosen(
    card: "CardInstance",
    combat: "CombatState",
) -> None:
    hook = _CARD_CHOSEN_HOOKS.get(card.card_id)
    if hook is not None:
        hook(card, combat)


def is_card_playable(
    card: "CardInstance",
    owner_state: object,
    combat: "CombatState",
    owner: "Creature",
) -> bool:
    hook = _CARD_PLAYABILITY_HOOKS.get(card.card_id)
    if hook is None:
        return True
    return hook(card, owner_state, combat, owner)


def should_play_card_from_hand(
    listener_card: "CardInstance",
    played_card: "CardInstance",
    owner_state: object,
    combat: "CombatState",
    owner: "Creature",
    is_auto_play: bool,
) -> bool:
    hook = _CARD_SHOULD_PLAY_HOOKS.get(listener_card.card_id)
    if hook is None:
        return True
    return hook(listener_card, played_card, owner_state, combat, owner, is_auto_play)


def card_target_type_for(
    card: "CardInstance",
    owner: "Creature",
    target_type: "TargetType",
) -> "TargetType":
    hook = _CARD_TARGET_TYPE_HOOKS.get(card.card_id)
    if hook is None:
        return target_type
    return hook(card, owner, target_type)


def apply_card_after_combat_end(card: "CardInstance", owner: "PlayerState") -> bool:
    hook = _CARD_AFTER_COMBAT_END_HOOKS.get(card.card_id)
    if hook is None:
        return True
    return hook(card, owner)


def modify_card_next_event(
    card: "CardInstance",
    run_state: "RunState",
    event: "EventModel | None",
) -> "EventModel | None":
    hook = _CARD_NEXT_EVENT_HOOKS.get(card.card_id)
    if hook is None:
        return event
    return hook(card, run_state, event)


def modify_card_unknown_room_types(
    card: "CardInstance",
    run_state: "RunState",
    room_types: set["RoomType"],
) -> set["RoomType"]:
    hook = _CARD_UNKNOWN_ROOM_TYPES_HOOKS.get(card.card_id)
    if hook is None:
        return room_types
    return hook(card, run_state, room_types)


def modify_card_generated_map(
    card: "CardInstance",
    run_state: "RunState",
    act_map: "ActMap",
    act_index: int,
) -> "ActMap":
    hook = _CARD_GENERATED_MAP_HOOKS.get(card.card_id)
    if hook is None:
        return act_map
    return hook(card, run_state, act_map, act_index)


def modify_card_generated_map_late(
    card: "CardInstance",
    run_state: "RunState",
    act_map: "ActMap",
    act_index: int,
) -> "ActMap":
    hook = _CARD_GENERATED_MAP_LATE_HOOKS.get(card.card_id)
    if hook is None:
        return act_map
    return hook(card, run_state, act_map, act_index)


def fire_card_after_map_generated(
    card: "CardInstance",
    run_state: "RunState",
    act_map: "ActMap",
    act_index: int,
) -> None:
    hook = _CARD_AFTER_MAP_GENERATED_HOOKS.get(card.card_id)
    if hook is not None:
        hook(card, run_state, act_map, act_index)


def complete_card_quest(card: "CardInstance", run_state: "RunState") -> int:
    hook = _CARD_QUEST_COMPLETE_HOOKS.get(card.card_id)
    if hook is None:
        return 0
    return hook(card, run_state)


def modify_card_rest_site_options(
    card: "CardInstance",
    owner: "PlayerState",
    options: list["RestSiteOption"],
    run_state: "RunState | None",
) -> list["RestSiteOption"]:
    hook = _CARD_REST_SITE_OPTIONS_HOOKS.get(card.card_id)
    if hook is None:
        return options
    return hook(card, owner, options, run_state)
