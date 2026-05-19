"""Target helpers for monster moves."""

from __future__ import annotations

from typing import TYPE_CHECKING

from sts2_env.core.creature import Creature

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState


def living_player_targets(combat: CombatState) -> list[Creature]:
    return [
        state.creature
        for state in combat.combat_player_states
        if state.creature.is_alive
    ]


def player_or_pet_owner(target: Creature) -> Creature:
    return getattr(target, "pet_owner", None) or target
