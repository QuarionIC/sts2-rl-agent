"""Every card the LIVE game can hand us must resolve through the bridge path.

Why this test exists
--------------------
When a card id on the wire does not resolve, ``reconstruct_combat`` refuses
the whole fight (combat_reconstruct.py: "combat planner declining"), because
a simulator holding fewer cards than the game draws the wrong card on every
subsequent draw. Refusing is correct -- but it silently demotes both the
combat planner AND the combat RL agent to heuristics for the rest of that
fight, and for the rest of the RUN if the card sits in the deck.

Measured 2026-07-31: the game's Silent pool gained ``Sidestep`` in v0.110.0.
The simulator had no such card, so a Silent run that was offered Sidestep --
including from a mid-combat Discovery, which pulls from the same pool -- lost
its combat model from that point on. Nothing failed loudly; the run just got
worse.

The existing parity tests did not catch it because they validate against
``decompiled/``, a stale copy of the reference tree. This test pins the
version-specific tree the game is actually running.

What it checks
--------------
The BRIDGE path specifically -- ``_to_card_id`` then ``create_card`` -- not a
test-local alias table. ``test_card_pool_parity`` maps ``Sloth ->
SLOTH_STATUS`` through its own ``_EXPLICIT_CARD_ALIASES``, so it passed while
the live resolver returned None for the same string. A parity test that
knows an alias the production resolver does not know is not testing the
thing that runs.

Scope
-----
Single-player-reachable cards only. ``IRunState.CardMultiplayerConstraint``
returns ``SingleplayerOnly`` whenever ``Players.Count <= 1``, and
``CardPoolModel.GetUnlockedCards`` then removes every ``MultiplayerOnly``
card from every pool. 17 of the game's card classes are MultiplayerOnly
(Cacophony, Soulbound, Underworld, TheBall, ...), so the simulator omitting
them is correct, not a gap.
"""

from __future__ import annotations

import re
from pathlib import Path

import pytest

from sts2_env.bridge.combat_reconstruct import _to_card_id
from sts2_env.cards.factory import create_card


REPO_ROOT = Path(__file__).resolve().parents[1]

#: The tracked reference tree, which IS the version the bridge plays against --
#: decompiled/ was refreshed to v0.110.0 on 2026-07-31.
#:
#: This pointed at an untracked decompiled_v0.110.0/ copy, so on a fresh clone
#: the directory was absent and the whole module SKIPPED: a coverage guard that
#: silently covers nothing. Pointing it at the tracked tree also removes 28MB
#: of duplicate. Refresh decompiled/ on each game patch and this follows.
LIVE_CARD_DIR = REPO_ROOT / "decompiled" / "MegaCrit.Sts2.Core.Models.Cards"

#: StringHelper.Slugify's own regexes: ModelDb.GetEntry(type) is
#: Slugify(type.Name), and Entry is exactly what RlCombatHandler.SerializeCard
#: puts on the wire as "id". The game's camel-case pattern is
#: ``([A-Za-z0-9]|\G(?!^))([A-Z])``; Python's re has no \G, so _slugify below
#: reproduces its effect with a lookbehind instead.
_WHITESPACE_RE = re.compile(r"\s+")
_SPECIAL_CHAR_RE = re.compile(r"[^A-Z0-9_]")

_MULTIPLAYER_ONLY_RE = re.compile(
    r"CardMultiplayerConstraint\s+MultiplayerConstraint\s*=>\s*"
    r"CardMultiplayerConstraint\.MultiplayerOnly"
)


def _slugify(name: str) -> str:
    """Python port of MegaCrit StringHelper.Slugify.

    Python's ``re`` has no ``\\G``; the C# pattern's ``|\\G(?!^)`` clause only
    makes consecutive capitals split (``ABC`` -> ``A_B_C``), which a global
    lookbehind reproduces for the character class in use here.
    """
    spaced = re.sub(r"(?<=[A-Za-z0-9])([A-Z])", r"_\1", name.strip())
    upper = _WHITESPACE_RE.sub("_", spaced.upper())
    return _SPECIAL_CHAR_RE.sub("", upper)


def _single_player_card_entries() -> list[str]:
    entries = []
    for path in sorted(LIVE_CARD_DIR.glob("*.cs")):
        if _MULTIPLAYER_ONLY_RE.search(path.read_text(encoding="utf-8", errors="replace")):
            continue
        entries.append(_slugify(path.stem))
    return entries


pytestmark = pytest.mark.skipif(
    not LIVE_CARD_DIR.is_dir(),
    reason=f"no decompiled reference at {LIVE_CARD_DIR}",
)


def test_slugify_matches_the_games_entry_format():
    # Anchors the port against names taken from the live wire.
    assert _slugify("Sidestep") == "SIDESTEP"
    assert _slugify("TheBall") == "THE_BALL"
    assert _slugify("NotYet") == "NOT_YET"
    assert _slugify("SerpentForm") == "SERPENT_FORM"
    assert _slugify("MinionDiveBomb") == "MINION_DIVE_BOMB"


def test_reference_tree_has_the_expected_shape():
    entries = _single_player_card_entries()
    # Guards against a glob that silently matches nothing -- an empty list
    # would make every assertion below vacuously true.
    assert len(entries) > 500, f"only {len(entries)} card classes found"
    assert "SIDESTEP" in entries
    assert "SCARE" not in entries, "Scare was removed in v0.110.0"
    assert "CACOPHONY" not in entries, "Cacophony is MultiplayerOnly"


@pytest.mark.parametrize("entry", _single_player_card_entries())
def test_every_single_player_card_resolves_and_builds(entry: str):
    card_id = _to_card_id(entry)
    assert card_id is not None, (
        f"wire id {entry!r} does not resolve to a CardId. Live, this card "
        f"arriving in any pile makes reconstruct_combat decline the fight, "
        f"which disables the combat planner and the combat RL agent."
    )
    # Both faces: an upgraded-only constructor failure is just as fatal, and
    # upgraded cards are what the run agent spends rest sites producing.
    for upgraded in (False, True):
        card = create_card(card_id, upgraded=upgraded)
        assert card is not None
