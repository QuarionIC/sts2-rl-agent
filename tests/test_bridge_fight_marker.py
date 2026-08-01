"""Every combat log line must say which fight and which round it belongs to.

Why this test exists
--------------------
Without a fight boundary in the log there is no way to tell a FRESH divergence
from the compounded tail of an earlier one -- and that distinction is what
decides whether divergence work goes at the shuffle/draw or at card effects.

A night of logs could not answer it, and two attempts to recover the boundary
after the fact both produced confident, wrong answers:

* Reading "Combat turn N: planned ..." as a turn counter. It is emitted once
  per whole-combat PLAN (26 lines against 217 actions) and its values run
  1,3,5,1,1 as the planner replans mid-fight. Treating it as a turn put 91% of
  divergences on "turn 1" purely because the variable was stale between plans.
* Inferring a boundary from the round number decreasing. This is what
  ``test_one_round_fight_still_starts_a_new_fight`` pins: a fight that ends on
  round 1 is followed by a fight that starts on round 1, the round never
  decreases, and the two fights silently merge into one.

The phase transition is exact, so that is what the tracker uses.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import Phase, _fight_marker, _note_phase


@pytest.fixture(autouse=True)
def _reset_tracker():
    agent_runner._FIGHT_TRACKER.update({"ordinal": 0, "in_combat": False})
    yield
    agent_runner._FIGHT_TRACKER.update({"ordinal": 0, "in_combat": False})


def _combat_phase():
    phases = list(Phase.COMBAT_PHASES)
    assert phases, "Phase.COMBAT_PHASES is empty; the tracker cannot work"
    return phases[0]


def test_marker_reports_fight_and_round():
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 3}) == "f1t3"


def test_actions_within_one_fight_share_an_ordinal():
    _note_phase(_combat_phase())
    first = _fight_marker({"round": 1})
    # Several actions in the same turn, then a later turn -- still one fight.
    assert _fight_marker({"round": 1}) == first
    assert _fight_marker({"round": 2}) == "f1t2"


def test_a_new_fight_increments_the_ordinal():
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 1}).startswith("f1")
    _note_phase("MAP")  # left combat
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 1}).startswith("f2")


def test_one_round_fight_still_starts_a_new_fight():
    """The case that defeats a round-decreases heuristic.

    Fight 1 ends on round 1; fight 2 starts on round 1. The round never goes
    backwards, so any inference from the round number alone merges them and
    reports the second fight's opening divergence as though it had compounded
    through a long first fight.
    """
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 1}) == "f1t1"
    _note_phase("MAP")
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 1}) == "f2t1"


def test_non_combat_payloads_do_not_split_a_fight():
    """Card-select and other mid-fight payloads must not look like a boundary.

    A mid-combat Discovery arrives as a card_select message. If that were
    treated as leaving combat, the rest of the fight would be logged as a new
    one and every subsequent divergence would look fresh.
    """
    _note_phase(_combat_phase())
    _note_phase(_combat_phase())
    assert _fight_marker({"round": 2}) == "f1t2"


def test_missing_round_degrades_to_zero_not_a_crash():
    # The marker is diagnostics: it must never be the thing that kills a run.
    _note_phase(_combat_phase())
    assert _fight_marker({}) == "f1t0"
    assert _fight_marker({"round": None}) == "f1t0"
