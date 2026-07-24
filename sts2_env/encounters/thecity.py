"""TheCity (Act-2-slot legacy act) encounter definitions -- "Acts from the
Past" mod: weak, normal, elite, boss.

Mirrors ``sts2_env/encounters/exordium.py``'s structure/pattern exactly
(same ``EncounterSetup`` callable shape, same ``WEAK_ENCOUNTERS`` /
``NORMAL_ENCOUNTERS`` / ``ELITE_ENCOUNTERS`` / ``BOSS_ENCOUNTERS`` /
``ALL_..._ENCOUNTERS`` list convention).

SCOPE NOTE: these pools are intentionally NOT wired into
``run_manager.py::_get_encounter_pools`` and TheCity is NOT registered into
the ``sts2_env/map/acts.py`` act-slot-candidate extension point yet --
that's a separate, later task once TheCity, TheBeyond, and Exordium's own
events are all done and tested together. This module is ready to be wired
in once that happens.
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.enums import PowerId
from sts2_env.core.rng import INT_MAX, Rng

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState

from sts2_env.monsters.exordium import (
    create_cultist,
    create_fungi_beast,
    create_looter,
    create_sentry,
    create_slaver_blue,
    create_slaver_red,
)
from sts2_env.monsters.thecity import (
    GREMLIN_LEADER_MAX_ALLIES,
    GREMLIN_LEADER_RALLY_POOL,
    create_book_of_stabbing,
    create_bronze_automaton,
    create_byrd,
    create_centurion,
    create_champ,
    create_chosen,
    create_collector,
    create_gremlin_leader,
    create_mugger,
    create_mystic,
    create_shelled_parasite,
    create_snake_plant,
    create_snecko,
    create_spheric_guardian,
    create_taskmaster,
)

EncounterSetup = Callable[..., None]


def _add(combat_state: "CombatState", rng: Rng, creator, **kwargs) -> None:
    creature, ai = creator(rng, ascension_level=combat_state.ascension_level, **kwargs)
    combat_state.add_enemy(creature, ai)


# ---- Weak Encounters (5) ----

def setup_chosen_weak(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_chosen)


def setup_shelled_parasite_weak(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_shelled_parasite)


def setup_spheric_guardian_weak(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_spheric_guardian)


def setup_three_byrds_weak(combat: "CombatState", rng: Rng) -> None:
    for _ in range(3):
        _add(combat, rng, create_byrd, combat=combat)


def setup_two_thieves_weak(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_looter)
    _add(combat, rng, create_mugger)


WEAK_ENCOUNTERS: list[EncounterSetup] = [
    setup_chosen_weak,
    setup_shelled_parasite_weak,
    setup_spheric_guardian_weak,
    setup_three_byrds_weak,
    setup_two_thieves_weak,
]


# ---- Normal Encounters (8) ----

def setup_byrd_and_chosen_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_byrd, combat=combat)
    _add(combat, rng, create_chosen)


def setup_centurion_and_mystic_normal(combat: "CombatState", rng: Rng) -> None:
    # Mystic added first so Centurion's very first move (resolved
    # immediately at creation, before it's added to combat) already sees a
    # living ally -- see ``create_centurion``'s docstring in
    # sts2_env/monsters/thecity.py.
    _add(combat, rng, create_mystic, combat=combat)
    _add(combat, rng, create_centurion, combat=combat)


def setup_city_protectors_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_sentry, bolt_first=True)
    _add(combat, rng, create_spheric_guardian)


def setup_cultist_and_chosen_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_cultist)
    _add(combat, rng, create_chosen)


def setup_pair_of_parasites_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_shelled_parasite)
    _add(combat, rng, create_fungi_beast)


def setup_snake_plant_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_snake_plant)


def setup_snecko_normal(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_snecko)


def setup_three_cultists_normal(combat: "CombatState", rng: Rng) -> None:
    for _ in range(3):
        _add(combat, rng, create_cultist)


NORMAL_ENCOUNTERS: list[EncounterSetup] = [
    setup_byrd_and_chosen_normal,
    setup_centurion_and_mystic_normal,
    setup_city_protectors_normal,
    setup_cultist_and_chosen_normal,
    setup_pair_of_parasites_normal,
    setup_snake_plant_normal,
    setup_snecko_normal,
    setup_three_cultists_normal,
]


# ---- Elite Encounters (3) ----

def setup_book_of_stabbing_elite(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_book_of_stabbing)


def setup_gremlin_leader_elite(combat: "CombatState", rng: Rng) -> None:
    # 2 DISTINCT random draws from the weighted gremlin pool (not 3 -- the
    # leader itself is the third slot), matching the task spec's explicit
    # simplification of the decompiled GremlinLeaderElite.cs (which reserves
    # a 3rd non-leader slot that starts empty and can only ever be filled by
    # RALLY -- collapsed here into "2 total non-leader companion slots" per
    # spec, see GREMLIN_LEADER_MAX_ALLIES in monsters/thecity.py).
    pool = list(GREMLIN_LEADER_RALLY_POOL)
    chosen_creators = rng.sample(pool, GREMLIN_LEADER_MAX_ALLIES)
    gremlins = []
    for creator in chosen_creators:
        creature, ai = creator(rng, ascension_level=combat.ascension_level)
        combat.add_enemy(creature, ai)
        gremlins.append(creature)

    leader, leader_ai = create_gremlin_leader(rng, ascension_level=combat.ascension_level, combat=combat)
    combat.add_enemy(leader, leader_ai)

    # Mirrors GremlinLeader.AfterAddedToRoom applying Minion to every other
    # already-living ally in the fight -- done here (once the whole
    # encounter is in the room) rather than inside create_gremlin_leader,
    # which doesn't have combat context yet when it runs.
    for gremlin in gremlins:
        gremlin.apply_power(PowerId.MINION, 1, applier=leader)


def setup_slavers_elite(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_slaver_blue)
    _add(combat, rng, create_taskmaster)
    _add(combat, rng, create_slaver_red)


ELITE_ENCOUNTERS: list[EncounterSetup] = [
    setup_book_of_stabbing_elite,
    setup_gremlin_leader_elite,
    setup_slavers_elite,
]


# ---- Boss Encounters (pool of 3; RunManager._roll_act_boss picks 1 via
# rng.choice(pool) at run start, same mechanism every other act uses) ----

def setup_champ_boss(combat: "CombatState", rng: Rng) -> None:
    _add(combat, rng, create_champ)


def setup_collector_boss(combat: "CombatState", rng: Rng) -> None:
    # TorchHead minions spawn dynamically when Collector's forced first
    # move (SPAWN) performs -- not placed here at encounter setup.
    _add(combat, rng, create_collector)


def setup_bronze_automaton_boss(combat: "CombatState", rng: Rng) -> None:
    # BronzeOrb minions spawn dynamically when BronzeAutomaton's forced
    # first move (SPAWN_ORBS) performs -- not placed here at encounter setup.
    _add(combat, rng, create_bronze_automaton)


BOSS_ENCOUNTERS: list[EncounterSetup] = [
    setup_champ_boss,
    setup_collector_boss,
    setup_bronze_automaton_boss,
]


ALL_THECITY_ENCOUNTERS: list[EncounterSetup] = (
    list(WEAK_ENCOUNTERS) +
    list(NORMAL_ENCOUNTERS) +
    list(ELITE_ENCOUNTERS) +
    list(BOSS_ENCOUNTERS)
)


# ---- Event Encounters (TheCity events: Colosseum, MaskedBandits) ----
#
# Triggered by events rather than appearing in the pools above -- same
# status as the base game's event encounters in sts2_env/encounters/
# events.py, and registered into that module's EVENT_ENCOUNTER_REGISTRY so
# RunManager._enter_event_combat can resolve them by id. NOT added to
# WEAK/NORMAL/ELITE/BOSS_ENCOUNTERS or ALL_THECITY_ENCOUNTERS.

from sts2_env.encounters.events import EVENT_ENCOUNTER_REGISTRY  # noqa: E402
from sts2_env.monsters.exordium import create_gremlin_nob  # noqa: E402
from sts2_env.monsters.thecity import (  # noqa: E402
    create_bear,
    create_pointy,
    create_romeo,
)


def setup_colosseum_slavers_event(combat: "CombatState", rng: Rng) -> None:
    """ColosseumFirstEncounter.cs: SlaverBlue ("blue") + SlaverRed ("red").

    ShouldGiveRewards => false in the decompiled encounter -- the Colosseum
    event enters this fight with an empty reward list and resumes the event
    afterwards.
    """
    _add(combat, rng, create_slaver_blue)
    _add(combat, rng, create_slaver_red)


def setup_colosseum_nobs_event(combat: "CombatState", rng: Rng) -> None:
    """ColosseumSecondEncounter.cs: Taskmaster + GremlinNob (RoomType 2 --
    elite-flavored, but entered as an event combat with explicit rewards).
    """
    _add(combat, rng, create_taskmaster)
    _add(combat, rng, create_gremlin_nob)


def setup_red_mask_bandits_event(combat: "CombatState", rng: Rng) -> None:
    """RedMaskBanditsEvent.cs: Pointy ("pointy") + Romeo ("romeo") +
    Bear ("bear").
    """
    _add(combat, rng, create_pointy)
    _add(combat, rng, create_romeo)
    _add(combat, rng, create_bear)


THECITY_EVENT_ENCOUNTERS: dict[str, EncounterSetup] = {
    "colosseum_slavers": setup_colosseum_slavers_event,
    "colosseum_nobs": setup_colosseum_nobs_event,
    "red_mask_bandits": setup_red_mask_bandits_event,
}

EVENT_ENCOUNTER_REGISTRY.update(THECITY_EVENT_ENCOUNTERS)
