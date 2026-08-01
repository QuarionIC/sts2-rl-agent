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

#: The gameplay mods actually INSTALLED for agent runs.
#:
#: Downfall is deliberately absent. It was subscribed until 2026-07-31 and
#: injected 807 card classes and 238 powers from eight other characters into
#: Necrobinder runs; the simulator models none of it, so 61-89% of combat
#: payloads were refused and the agent played heuristics through most of
#: every run while the logs said the planner was engaged. It was unsubscribed
#: rather than modelled. decompiled_mods/Downfall survives only as reference.
INSTALLED_MODS = ("ActsFromThePast", "Act4Heart")
MOD_ROOT = REPO_ROOT / "decompiled_mods"

#: Powers known to be unimplemented, each with what it actually does. Entries
#: come OFF this list as they are implemented; nothing is ever added to it to
#: make a failure go away -- a new name here means the mod gained content the
#: simulator has not modelled, which is exactly what this test exists to say.
KNOWN_MISSING = {
}


def _slugify(name: str) -> str:
    """Class name -> the Entry string the wire carries (StringHelper.Slugify)."""
    return re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", name.strip()).upper()


def _mod_power_entries() -> list[str]:
    """Every power class in the installed mods.

    Matches ``*Power*.cs``, not ``*Power.cs``. The narrower glob MISSED every
    class with a suffix after "Power" -- Act4Heart names its
    ``MetallicizePowerA4h`` and ``RegeneratePowerA4h`` that way -- so this
    test reported Act4Heart as fully covered while METALLICIZE_POWER_A4H was
    being dropped in live play. Found by the bridge, not by the guard that
    exists to find it.
    """
    entries = set()
    for mod in INSTALLED_MODS:
        root = MOD_ROOT / mod
        if not root.is_dir():
            continue
        for path in root.rglob("*Power*.cs"):
            # A power lives in a ``*.Powers`` namespace. ``*.Patches.Powers``
            # holds Harmony patches ABOUT powers, which no enemy carries --
            # widening the filename glob swept those in, so the namespace is
            # what decides.
            parent = path.parent.name
            if not parent.endswith(".Powers") or ".Patches." in parent:
                continue
            # Interface stubs (ICustomPower) are contracts, and *PowerModel is
            # the abstract per-mod base class.
            if path.stem.startswith("I") and path.stem[1:2].isupper():
                continue
            if path.stem.endswith("PowerModel"):
                continue
            entries.add(_slugify(path.stem))
    return sorted(entries)


pytestmark = pytest.mark.skipif(
    not MOD_ROOT.is_dir(), reason=f"no decompiled mod tree at {MOD_ROOT}"
)


def test_the_mod_trees_are_actually_present():
    entries = _mod_power_entries()
    assert len(entries) >= 20, f"only {len(entries)} power classes found"
    for mod in INSTALLED_MODS:
        assert (MOD_ROOT / mod).is_dir(), f"{mod} reference tree is missing"


@pytest.mark.parametrize("entry", _mod_power_entries())
def test_every_mod_power_resolves(entry: str):
    if entry in KNOWN_MISSING:
        pytest.xfail(f"not implemented: {KNOWN_MISSING[entry]}")
    assert _to_power_id(entry) is not None, (
        f"mod power {entry!r} does not resolve. Live, an enemy carrying it is "
        f"reconstructed WITHOUT it and the planner picks its line against an "
        f"enemy that is missing a buff."
    )


#: How the mod actually decorates a power id on the wire.
#:
#: _slugify above reproduces only the class-name half. The live payload also
#: carries a mod prefix -- this file's own docstring quotes
#: ``ACTSFROMTHEPAST-SPORE_CLOUD_POWER`` -- so testing the bare slug checked a
#: string the bridge never receives.
#:
#: The FIRST version of this list appended "_POWER" unconditionally and
#: produced SPORE_CLOUD_POWER_POWER, failing 64 cases against a string no wire
#: ever carries: the class names already end in Power, so _slugify already
#: emits the suffix. Inventing plausible-looking shapes is the same mistake as
#: inventing plausible-looking log lines, so these are derived from the entry
#: rather than pattern-matched by eye, and pinned below against a real
#: payload captured from output/overnight.
def _wire_shapes(entry: str) -> list[str]:
    return [entry, f"{MOD_WIRE_PREFIX}{entry}"]


MOD_WIRE_PREFIX = "ACTSFROMTHEPAST-"

#: Verbatim from output/overnight/session_20260801_001556_*.log.
_CAPTURED_WIRE_ID = "METALLICIZE_POWER_A4H"
_CAPTURED_PREFIXED = "ACTSFROMTHEPAST-SPORE_CLOUD_POWER"


def test_the_captured_wire_ids_resolve():
    """Two ids copied from real logs, not constructed by this file."""
    assert _to_power_id(_CAPTURED_WIRE_ID) is not None
    assert _to_power_id(_CAPTURED_PREFIXED) is not None


def test_the_shape_builder_matches_a_captured_payload():
    """The generated shapes must include the form actually observed.

    Without this the parametrization could drift to shapes the wire never
    uses -- which is precisely what happened on the first attempt -- and the
    suite would still be green while covering nothing real.
    """
    assert _CAPTURED_PREFIXED in _wire_shapes("SPORE_CLOUD_POWER"), (
        "the shape builder no longer produces the form seen live"
    )


@pytest.mark.parametrize("entry", _mod_power_entries())
@pytest.mark.parametrize("shape_index", [0, 1])
def test_every_mod_power_resolves_in_its_live_wire_shape(entry: str,
                                                         shape_index: int):
    """The resolver must accept the decorated id, not just the class slug.

    An unrecognised power does not decline the fight -- the runner logs
    "the planner will search without them" and continues -- so this failing
    open is invisible in the live logs except as slightly worse play.
    """
    if entry in KNOWN_MISSING:
        pytest.xfail(f"not implemented: {KNOWN_MISSING[entry]}")
    wire = _wire_shapes(entry)[shape_index]
    assert _to_power_id(wire) is not None, (
        f"{wire!r} does not resolve, though the bare slug {entry!r} does. "
        f"This is the form the wire carries; the planner would search against "
        f"an enemy missing this buff and never say so."
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
