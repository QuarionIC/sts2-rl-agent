"""Enemy powers from the active mods must resolve, or plans are made blind.

Unresolved CARDS are safe-ish: reconstruct_combat refuses the fight outright,
which is loud and correct. Unresolved POWERS are not. combat_reconstruct's
_restore_powers logs a warning and continues:

    enemy ACTSFROMTHEPAST-FUNGI_BEAST powers: 1 power id(s) unrecognised
    (e.g. ACTSFROMTHEPAST-SPORE_CLOUD_POWER) -- the planner will search
    without them

"The planner will search without them" means every plan for that fight is
computed against an enemy that is missing a buff. Sharp Hide (retaliate when
the player plays an Attack) and Strength Up (enemy grows every turn) change
what the correct line IS, so the planner does not merely misjudge the
outcome -- it picks a different, worse action, and nothing in the game says
so. Measured live 2026-07-31 during a Fungi Beast fight.

ActsFromThePast is a permanently-installed gameplay mod for this project, so
its content is not optional: it is part of the game the agent plays.
"""

from __future__ import annotations

import re
from pathlib import Path

import pytest

from sts2_env.bridge.combat_reconstruct import _to_power_id


REPO_ROOT = Path(__file__).resolve().parents[1]
MOD_ROOT = REPO_ROOT / "decompiled_mods" / "ActsFromThePast"

#: Powers known to be unimplemented, each with what it actually does. Entries
#: come OFF this list as they are implemented; nothing is ever added to it to
#: make a failure go away -- a new name here means the mod gained content the
#: simulator has not modelled, which is exactly what this test exists to say.
KNOWN_MISSING = {
    "EXPLOSIVE_POWER": "counts down, then detonates for heavy damage",
    "FADING_POWER": "owner dies after N turns",
    "SHARP_HIDE_POWER": "retaliates when the player plays an Attack",
    "SPORE_CLOUD_POWER": "applies Vulnerable to the player when the owner dies",
    # These two look aliasable and are not; see _POWER_ID_ALIASES in
    # combat_reconstruct for why mapping them onto RITUAL / REGEN would
    # simulate the wrong enemy. They need their own PowerId, which widens the
    # observation (powers are raw one-hot, not embedded), so they are batched
    # with the other additions rather than landed mid-training-round.
    "STRENGTH_UP_POWER": "applies Strength at turn END, unlike RITUAL's turn start",
    "REGEN_ENEMY_POWER": "heals every turn forever, unlike REGEN which decays",
}


def _slugify(name: str) -> str:
    """Class name -> the Entry string the wire carries (StringHelper.Slugify)."""
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", name.strip()).upper()


def _mod_power_entries() -> list[str]:
    return sorted(_slugify(p.stem) for p in MOD_ROOT.rglob("*Power.cs"))


pytestmark = pytest.mark.skipif(
    not MOD_ROOT.is_dir(), reason=f"no decompiled mod tree at {MOD_ROOT}"
)


def test_the_mod_tree_is_actually_present():
    entries = _mod_power_entries()
    assert len(entries) >= 20, f"only {len(entries)} power classes found"


@pytest.mark.parametrize("entry", _mod_power_entries())
def test_every_mod_power_resolves(entry: str):
    if entry in KNOWN_MISSING:
        pytest.xfail(f"not implemented: {KNOWN_MISSING[entry]}")
    assert _to_power_id(entry) is not None, (
        f"mod power {entry!r} does not resolve. Live, an enemy carrying it is "
        f"reconstructed WITHOUT it and the planner picks its line against an "
        f"enemy that is missing a buff."
    )


def test_the_known_missing_list_has_not_gone_stale():
    # If one of these starts resolving, implement-and-remove rather than
    # leaving a permanent xfail that hides the next real gap.
    resolved = {name for name in KNOWN_MISSING if _to_power_id(name) is not None}
    assert not resolved, (
        f"{sorted(resolved)} now resolve -- remove them from KNOWN_MISSING so "
        f"the parametrised test guards them for real."
    )


def test_every_known_missing_power_still_exists_in_the_mod():
    entries = set(_mod_power_entries())
    stale = set(KNOWN_MISSING) - entries
    assert not stale, (
        f"{sorted(stale)} are no longer in the mod; drop them from KNOWN_MISSING."
    )
