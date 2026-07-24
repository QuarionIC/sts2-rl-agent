"""Act4Heart mod ("TheEnding") encounter definitions.

Act 4 is hand-authored (see sts2_env/map/generator.py:generate_act4_heart_map)
with exactly three possible encounters:
    - EmptyFightAct4Weak: zero possible monsters (RoomType.MONSTER, IsWeak).
      Unreachable via the hand-authored map (no Monster nodes exist on it)
      but kept so any code path that rolls a "weak"/"normal" encounter for
      Act 4 resolves to an instant, harmless empty win instead of erroring
      or picking a random Act 1-3 monster.
    - SpireShieldAndSpireSpearElite: the only Elite encounter.
    - CorruptHeartBoss: the only Boss encounter.

C# refs: decompiled_mods/Act4Heart/Act4Heart/{EmptyFightAct4Weak,
SpireShieldAndSpireSpearElite,CorruptHeartBoss}.cs
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.enums import PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.act4_heart import (
    create_corrupt_heart,
    create_spire_shield,
    create_spire_spear,
)
from sts2_env.monsters.targets import apply_power_to_living_player_targets

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState

EncounterSetup = Callable[..., None]


def setup_empty_fight_act4_weak(combat: "CombatState", rng: Rng) -> None:
    """No possible monsters -- adds nothing. Combat resolves as an instant
    win (see CombatState.start_combat's post-setup ``_check_combat_end``)."""
    return


def setup_corrupt_heart_boss(combat: "CombatState", rng: Rng) -> None:
    creature, ai = create_corrupt_heart(rng, ascension_level=combat.ascension_level)
    combat.add_enemy(creature, ai)


def setup_spire_shield_spire_spear_elite(combat: "CombatState", rng: Rng) -> None:
    shield, shield_ai = create_spire_shield(rng, ascension_level=combat.ascension_level)
    spear, spear_ai = create_spire_spear(rng, ascension_level=combat.ascension_level)
    combat.add_enemy(shield, shield_ai)
    combat.add_enemy(spear, spear_ai)
    # SpireSpear.AfterAddedToRoom: Surrounded(1) applied to all opponents
    # (the players), once combat/targets actually exist.
    apply_power_to_living_player_targets(combat, PowerId.SURROUNDED, 1, applier=spear)


WEAK_ENCOUNTERS: list[EncounterSetup] = [setup_empty_fight_act4_weak]
NORMAL_ENCOUNTERS: list[EncounterSetup] = []
ELITE_ENCOUNTERS: list[EncounterSetup] = [setup_spire_shield_spire_spear_elite]
BOSS_ENCOUNTERS: list[EncounterSetup] = [setup_corrupt_heart_boss]
