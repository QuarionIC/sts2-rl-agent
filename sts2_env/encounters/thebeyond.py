"""TheBeyond (Act-3-slot legacy act) encounter definitions -- "Acts from the
Past" mod: weak, normal, elite, boss.

Mirrors ``sts2_env/encounters/exordium.py``'s structure/pattern exactly (same
``EncounterSetup`` callable shape, same ``WEAK_ENCOUNTERS`` /
``NORMAL_ENCOUNTERS`` / ``ELITE_ENCOUNTERS`` / ``BOSS_ENCOUNTERS`` /
``ALL_..._ENCOUNTERS`` list convention consumed by
``sts2_env/run/run_manager.py::_get_encounter_pools`` and
``RunManager._roll_act_boss``).

SCOPE NOTE: these pools are intentionally NOT wired into
``run_manager.py::_get_encounter_pools`` -- see the module docstring in
``sts2_env/monsters/thebeyond.py`` and Exordium's own encounter module for
the same standing scope note (flipping TheBeyond on as a selectable Act-3-
slot alternative is a separate task's decision).

Pool composition below is read directly from the decompiled encounter
sources (``decompiled_mods/ActsFromThePast/ActsFromThePast.Acts.TheBeyond.
Encounters/*.cs`` and ``.../Encounters.Elite/*.cs``), not the prior research
summary -- two compositions turned out to differ from what that summary
guessed:

- ``OrbWalkerWeak`` is JUST 1x OrbWalker (no companion Shape-type monsters,
  despite the research report's "1 or 2 companions" guess).
- ``ReptomancerElite`` opens with 2 SnakeDagger already alive alongside
  Reptomancer (not spawned mid-fight from an empty start).

Also, the "11 total" Normal/Weak count in the originating task spec doesn't
match either the decompiled source directory listing or this file's own
final tally: it's 3 Weak (``DarklingsWeak``, ``OrbWalkerWeak``,
``ThreeShapesWeak``) + 7 Normal (``FourShapesNormal``, ``JawWormHordeNormal``,
``MawNormal``, ``SphereAndTwoShapesNormal``, ``SpireGrowthNormal``,
``TransientNormal``, ``WrithingMassNormal``) = 10, treated as a miscount in
that summary and resolved in favor of the direct source read.

``SphereAndTwoShapesNormal`` reuses TheCity's ``SphericGuardian`` (the
decompiled mod's own ``SphereAndTwoShapesNormal.cs``, despite living in the
``ActsFromThePast.Acts.TheBeyond.Encounters`` namespace, references the
exact same ``SphericGuardian`` class TheCity's own ``SphericGuardianWeak``
uses) -- imported from ``sts2_env/monsters/thecity.py`` rather than
duplicated, matching how Exordium's ``JawWorm``/``Cultist`` are reused here
too.
"""

from __future__ import annotations

from typing import Callable, TYPE_CHECKING

from sts2_env.core.enums import PowerId
from sts2_env.core.rng import Rng

if TYPE_CHECKING:
    from sts2_env.core.combat import CombatState

from sts2_env.monsters.exordium import create_cultist, create_jaw_worm
from sts2_env.monsters.thebeyond import (
    apply_time_eater_time_warp,
    create_awakened_one,
    create_darkling,
    create_deca,
    create_donu,
    create_exploder,
    create_giant_head,
    create_jaw_worm_hard_mode,
    create_maw,
    create_nemesis,
    create_orb_walker,
    create_reptomancer,
    create_repulsor,
    create_snake_dagger,
    create_spiker,
    create_spire_growth,
    create_time_eater,
    create_transient,
    create_writhing_mass,
)
from sts2_env.monsters.thecity import create_spheric_guardian

EncounterSetup = Callable[..., None]


def _add(combat: CombatState, rng: Rng, creator, **kwargs) -> None:
    creature, ai = creator(rng, ascension_level=combat.ascension_level, **kwargs)
    combat.add_enemy(creature, ai)


# ---- Weak Encounters (3) ----

def setup_darklings_weak(combat: CombatState, rng: Rng) -> None:
    for slot_index in range(3):
        _add(combat, rng, create_darkling, slot_index=slot_index)


def setup_orb_walker_weak(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_orb_walker)


_SHAPE_POOL = (
    [create_repulsor] * 2
    + [create_exploder] * 2
    + [create_spiker] * 2
)


def setup_three_shapes_weak(combat: CombatState, rng: Rng) -> None:
    chosen = rng.sample(list(_SHAPE_POOL), 3)
    for creator in chosen:
        _add(combat, rng, creator)


WEAK_ENCOUNTERS: list[EncounterSetup] = [
    setup_darklings_weak,
    setup_orb_walker_weak,
    setup_three_shapes_weak,
]


# ---- Normal Encounters (7) ----

def setup_writhing_mass_normal(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_writhing_mass)


def setup_spire_growth_normal(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_spire_growth)


def setup_maw_normal(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_maw)


def setup_transient_normal(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_transient)


def setup_four_shapes_normal(combat: CombatState, rng: Rng) -> None:
    chosen = rng.sample(list(_SHAPE_POOL), 4)
    for creator in chosen:
        _add(combat, rng, creator)


def setup_sphere_and_two_shapes_normal(combat: CombatState, rng: Rng) -> None:
    shape_creators = [create_spiker, create_repulsor, create_exploder]
    _add(combat, rng, rng.choice(shape_creators))
    _add(combat, rng, rng.choice(shape_creators))
    _add(combat, rng, create_spheric_guardian)


def setup_jaw_worm_horde_normal(combat: CombatState, rng: Rng) -> None:
    # Reuses Exordium's JawWorm (JawWorm.cs backs both Exordium's
    # JawWormWeak and TheBeyond's JawWormHordeNormal), but spawned in
    # HardMode (see create_jaw_worm_hard_mode's docstring).
    for _ in range(3):
        _add(combat, rng, create_jaw_worm_hard_mode)


NORMAL_ENCOUNTERS: list[EncounterSetup] = [
    setup_writhing_mass_normal,
    setup_spire_growth_normal,
    setup_maw_normal,
    setup_transient_normal,
    setup_four_shapes_normal,
    setup_sphere_and_two_shapes_normal,
    setup_jaw_worm_horde_normal,
]


# ---- Elite Encounters (3) ----

def setup_giant_head_elite(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_giant_head)


def setup_nemesis_elite(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_nemesis)


def setup_reptomancer_elite(combat: CombatState, rng: Rng) -> None:
    # Per ReptomancerElite.cs: starts with 2 SnakeDagger already alive
    # (slots "dagger3"/"dagger4") alongside Reptomancer, not spawned from
    # an empty start -- Reptomancer's own AfterAddedToRoom applies MinionPower
    # to any teammates already present at that point, so the 2 initial
    # daggers get it too (SpawnDagger's own mid-fight summons already apply
    # it directly -- see create_reptomancer).
    reptomancer, reptomancer_ai = create_reptomancer(rng, ascension_level=combat.ascension_level)
    dagger1, dagger1_ai = create_snake_dagger(rng, ascension_level=combat.ascension_level)
    dagger2, dagger2_ai = create_snake_dagger(rng, ascension_level=combat.ascension_level)
    combat.add_enemy(dagger1, dagger1_ai)
    combat.add_enemy(dagger2, dagger2_ai)
    combat.add_enemy(reptomancer, reptomancer_ai)
    dagger1.apply_power(PowerId.MINION, 1, applier=reptomancer)
    dagger2.apply_power(PowerId.MINION, 1, applier=reptomancer)


ELITE_ENCOUNTERS: list[EncounterSetup] = [
    setup_giant_head_elite,
    setup_nemesis_elite,
    setup_reptomancer_elite,
]


# ---- Boss Encounters (pool of 3; RunManager._roll_act_boss picks 1 via
# rng.choice(pool) at run start, same mechanism every other act uses) ----

def setup_awakened_one_boss(combat: CombatState, rng: Rng) -> None:
    cultist_left, cultist_left_ai = create_cultist(rng, ascension_level=combat.ascension_level)
    cultist_right, cultist_right_ai = create_cultist(rng, ascension_level=combat.ascension_level)
    awakened, awakened_ai = create_awakened_one(rng, ascension_level=combat.ascension_level)
    combat.add_enemy(cultist_left, cultist_left_ai)
    combat.add_enemy(cultist_right, cultist_right_ai)
    combat.add_enemy(awakened, awakened_ai)


def setup_donu_and_deca_boss(combat: CombatState, rng: Rng) -> None:
    _add(combat, rng, create_deca)
    _add(combat, rng, create_donu)


def setup_time_eater_boss(combat: CombatState, rng: Rng) -> None:
    time_eater, time_eater_ai = create_time_eater(rng, ascension_level=combat.ascension_level)
    combat.add_enemy(time_eater, time_eater_ai)
    apply_time_eater_time_warp(combat, time_eater)


BOSS_ENCOUNTERS: list[EncounterSetup] = [
    setup_awakened_one_boss,
    setup_donu_and_deca_boss,
    setup_time_eater_boss,
]


ALL_THEBEYOND_ENCOUNTERS: list[EncounterSetup] = (
    list(WEAK_ENCOUNTERS) +
    list(NORMAL_ENCOUNTERS) +
    list(ELITE_ENCOUNTERS) +
    list(BOSS_ENCOUNTERS)
)
