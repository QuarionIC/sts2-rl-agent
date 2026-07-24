"""Exordium (Act-1-slot legacy act) encounter definitions -- "Acts from the
Past" mod: weak, normal, elite, boss.

Mirrors ``sts2_env/encounters/act1.py``'s structure/pattern exactly (same
``EncounterSetup`` callable shape, same ``WEAK_ENCOUNTERS`` /
``NORMAL_ENCOUNTERS`` / ``ELITE_ENCOUNTERS`` / ``BOSS_ENCOUNTERS`` /
``ALL_..._ENCOUNTERS`` list convention consumed by
``sts2_env/run/run_manager.py::_get_encounter_pools`` and
``RunManager._roll_act_boss`` -- picking one boss setup function from
``BOSS_ENCOUNTERS`` via ``rng.choice(pool)`` is exactly how the boss-pool-of-N
mechanism already works for every other act, so no new mechanism was needed
for "pick 1 of 3 bosses at run start").

SCOPE NOTE: these pools are intentionally NOT wired into
``run_manager.py::_get_encounter_pools`` (which currently only dispatches on
``act_index`` to the four vanilla/mod act encounter modules). Flipping
Exordium on as a selectable Act-1-slot alternative is a separate task's
decision (once TheCity, TheBeyond, and Exordium's own events are all done).
This module is ready to be wired in -- just add an ``act_index``/act-slot
branch that imports from here once the act-slot-candidate extension point
lands (see the module docstring in ``sts2_env/monsters/exordium.py``).
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.rng import Rng

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState

from sts2_env.monsters.exordium import (
    create_acid_slime_large,
    create_acid_slime_medium,
    create_acid_slime_small,
    create_cultist,
    create_fungi_beast,
    create_gremlin_fat,
    create_gremlin_mad,
    create_gremlin_nob,
    create_gremlin_shield,
    create_gremlin_sneaky,
    create_gremlin_wizard,
    create_guardian,
    create_hexaghost,
    create_jaw_worm,
    create_lagavulin,
    create_looter,
    create_louse_green,
    create_louse_red,
    create_sentry,
    create_slaver_blue,
    create_slaver_red,
    create_slime_boss,
    create_spike_slime_large,
    create_spike_slime_medium,
    create_spike_slime_small,
)

EncounterSetup = Callable[..., None]


def _add(combat: CombatState, rng: Rng, creator, **kwargs) -> None:
    creature, ai = creator(rng, ascension_level=combat.ascension_level, **kwargs)
    combat.add_enemy(creature, ai)


# ---- Weak Encounters (4) ----

def setup_cultist_weak(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_cultist)


def setup_jaw_worm_weak(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_jaw_worm, hard_mode=False)


def setup_lice_weak(combat: CombatState, rng: Rng) -> None:
    creators = [create_louse_red, create_louse_green]
    for _ in range(2):
        creator = rng.choice(creators)
        _add(combat, rng, creator)


def setup_small_slimes_weak(combat: CombatState, rng: Rng) -> None:
    # Per decompiled SmallSlimesWeak.cs: 50/50 {SpikeSlimeSmall+AcidSlimeMedium}
    # or {AcidSlimeSmall+SpikeSlimeMedium} (small+medium, not small+small).
    if rng.next_bool():
        _add(combat, rng, create_spike_slime_small)
        _add(combat, rng, create_acid_slime_medium)
    else:
        _add(combat, rng, create_acid_slime_small)
        _add(combat, rng, create_spike_slime_medium)


WEAK_ENCOUNTERS: list[EncounterSetup] = [
    setup_cultist_weak,
    setup_jaw_worm_weak,
    setup_lice_weak,
    setup_small_slimes_weak,
]


# ---- Normal Encounters (9) ----

def setup_exordium_wildlife_normal(combat: CombatState, rng: Rng) -> None:
    # FungiBeast: implemented directly against the decompiled mod source
    # (FungiBeast.cs) since it wasn't included in the task's monster spec
    # list, only referenced by this encounter and TwoFungiBeastsNormal.
    strong_creators = [create_fungi_beast, create_jaw_worm]
    weak_creators = [create_louse_red, create_louse_green, create_spike_slime_medium, create_acid_slime_medium]
    _add(combat, rng, rng.choice(strong_creators))
    _add(combat, rng, rng.choice(weak_creators))


def setup_exordium_thugs_normal(combat: CombatState, rng: Rng) -> None:
    front_creators = [create_louse_red, create_louse_green, create_spike_slime_medium, create_acid_slime_medium]
    back_creators = [create_cultist, create_slaver_blue, create_slaver_red, create_looter]
    _add(combat, rng, rng.choice(front_creators))
    _add(combat, rng, rng.choice(back_creators))


def setup_gremlin_gang_normal(combat: CombatState, rng: Rng) -> None:
    pool = (
        [create_gremlin_mad] * 2
        + [create_gremlin_sneaky] * 2
        + [create_gremlin_fat] * 2
        + [create_gremlin_shield]
        + [create_gremlin_wizard]
    )
    chosen = rng.sample(pool, 4)
    for creator in chosen:
        _add(combat, rng, creator)


def setup_large_slime_normal(combat: CombatState, rng: Rng) -> None:
    creator = rng.choice([create_acid_slime_large, create_spike_slime_large])
    _add(combat, rng, creator)


def setup_lice_normal(combat: CombatState, rng: Rng) -> None:
    creators = [create_louse_red, create_louse_green]
    for _ in range(3):
        _add(combat, rng, rng.choice(creators))


def setup_looter_normal(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_looter)


def setup_lots_of_slimes_normal(combat: CombatState, rng: Rng) -> None:
    pool = [create_spike_slime_small] * 3 + [create_acid_slime_small] * 2
    chosen = rng.sample(pool, 5)
    for creator in chosen:
        _add(combat, rng, creator)


def setup_slaver_normal(combat: CombatState, rng: Rng) -> None:
    creator = rng.choice([create_slaver_red, create_slaver_blue])
    _add(combat, rng, creator)


def setup_two_fungi_beasts_normal(combat: CombatState, rng: Rng) -> None:
    for _ in range(2):
        _add(combat, rng, create_fungi_beast)


NORMAL_ENCOUNTERS: list[EncounterSetup] = [
    setup_exordium_wildlife_normal,
    setup_exordium_thugs_normal,
    setup_gremlin_gang_normal,
    setup_large_slime_normal,
    setup_lice_normal,
    setup_looter_normal,
    setup_lots_of_slimes_normal,
    setup_slaver_normal,
    setup_two_fungi_beasts_normal,
]


# ---- Elite Encounters (3) ----

def setup_gremlin_nob_elite(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_gremlin_nob)


def setup_lagavulin_elite(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_lagavulin)


def setup_sentries_elite(combat: CombatState, rng: Rng) -> None:
    for bolt_first in (True, False, True):
        _add(combat, rng, create_sentry, bolt_first=bolt_first)


ELITE_ENCOUNTERS: list[EncounterSetup] = [
    setup_gremlin_nob_elite,
    setup_lagavulin_elite,
    setup_sentries_elite,
]


# ---- Boss Encounters (pool of 3; RunManager._roll_act_boss picks 1 via
# rng.choice(pool) at run start, same mechanism every other act uses) ----

def setup_slime_boss_boss(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_slime_boss)


def setup_guardian_boss(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_guardian)


def setup_hexaghost_boss(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_hexaghost)


BOSS_ENCOUNTERS: list[EncounterSetup] = [
    setup_slime_boss_boss,
    setup_guardian_boss,
    setup_hexaghost_boss,
]


ALL_EXORDIUM_ENCOUNTERS: list[EncounterSetup] = (
    list(WEAK_ENCOUNTERS) +
    list(NORMAL_ENCOUNTERS) +
    list(ELITE_ENCOUNTERS) +
    list(BOSS_ENCOUNTERS)
)


# ---- Event-only encounters ----
#
# Triggered by Exordium events (sts2_env/events/exordium.py), never part of
# the weak/normal/elite/boss pools above (mirroring the decompiled event
# encounter classes, whose IsValidForAct always returns false). They are
# registered in EVENT_ENCOUNTER_REGISTRY -- the registry RunManager's
# _enter_event_combat consults -- exactly like the vanilla event fights in
# sts2_env/encounters/events.py, which also gives them the reward-suppression
# behavior (CombatRoom.suppress_default_rewards=True: the combat pays out
# exactly the event's explicit reward list).

def setup_dead_adventurer_sentries(combat: CombatState, rng: Rng) -> None:
    # DeadAdventurerSentries.cs: 3 Sentries, BoltFirst = true/false/true.
    for bolt_first in (True, False, True):
        _add(combat, rng, create_sentry, bolt_first=bolt_first)


def setup_dead_adventurer_gremlin_nob(combat: CombatState, rng: Rng) -> None:
    # DeadAdventurerGremlinNob.cs: a single GremlinNob.
    _add(combat, rng, create_gremlin_nob)


def setup_dead_adventurer_lagavulin(combat: CombatState, rng: Rng) -> None:
    # DeadAdventurerLagavulin.cs: a single Lagavulin with StartsAwake = true.
    _add(combat, rng, create_lagavulin, starts_awake=True)


def setup_three_fungi_beasts_event(combat: CombatState, rng: Rng) -> None:
    # ThreeFungiBeastsEvent.cs: 3 FungiBeasts (Mushrooms event fight).
    for _ in range(3):
        _add(combat, rng, create_fungi_beast)


EXORDIUM_EVENT_ENCOUNTER_REGISTRY: dict[str, EncounterSetup] = {
    "dead_adventurer_sentries": setup_dead_adventurer_sentries,
    "dead_adventurer_gremlin_nob": setup_dead_adventurer_gremlin_nob,
    "dead_adventurer_lagavulin": setup_dead_adventurer_lagavulin,
    "three_fungi_beasts_event": setup_three_fungi_beasts_event,
}

# Additive registration into the shared event-encounter registry.
from sts2_env.encounters.events import EVENT_ENCOUNTER_REGISTRY as _EVENT_ENCOUNTER_REGISTRY  # noqa: E402

_EVENT_ENCOUNTER_REGISTRY.update(EXORDIUM_EVENT_ENCOUNTER_REGISTRY)
