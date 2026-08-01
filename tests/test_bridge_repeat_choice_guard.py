"""A repeated live choice must be varied before the mod kills the run.

run_env has had an anti-dither guard since forensics found deterministic
policies toggling one option forever. The LIVE bridge path had none, so the
same behaviour ran unchecked until RlNonCombatRoomHandlers hit its
MaxRepeatedChoices = 3 and RlAutoSlayer reported "Run finished: terminated".

Measured 2026-07-31 (planner_div6.log)::

    RUN-RL (event): action 145 -> choose([0])
    RUN-RL (event): action 145 -> choose([0])
    RUN-RL (event): action 145 -> choose([0])
    Received: type=run_complete
    Run finished: terminated (run 3 this session)

The guard has to act BELOW the mod's limit of 3, or the run is already dead
by the time it would fire.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge import agent_runner
from sts2_env.bridge.agent_runner import (
    _LAST_NONCOMBAT_CHOICE,
    _break_repeated_choice,
    _screen_fingerprint,
)


def _event(floor=7, options=("Accept", "Refuse")):
    return {
        "type": "event",
        "floor": floor,
        "options": [
            {"index": i, "label": label, "enabled": True}
            for i, label in enumerate(options)
        ],
    }


CHOOSE_0 = {"phase": "run", "method": "choose", "args": [0]}


@pytest.fixture(autouse=True)
def _clear_guard():
    saved = list(_LAST_NONCOMBAT_CHOICE)
    _LAST_NONCOMBAT_CHOICE[:] = [None, 0]
    yield
    _LAST_NONCOMBAT_CHOICE[:] = saved


def test_the_guard_fires_below_the_mods_abort_threshold():
    assert agent_runner.NONCOMBAT_REPEAT_LIMIT < 3, (
        "the mod aborts the room after 3 identical choices, so a guard at or "
        "above 3 can never run"
    )


def test_a_repeated_choice_is_eventually_varied():
    state = _event()
    seen = [_break_repeated_choice(state, CHOOSE_0)["args"][0]
            for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 1)]
    assert seen[-1] != 0, f"never varied: {seen}"
    assert seen[:-1] == [0] * agent_runner.NONCOMBAT_REPEAT_LIMIT


def test_the_first_choices_are_left_alone():
    # The policy gets to make its decision; the guard is a backstop, not an
    # override.
    state = _event()
    assert _break_repeated_choice(state, CHOOSE_0) == CHOOSE_0


def test_a_different_screen_resets_the_counter():
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT):
        _break_repeated_choice(_event(floor=7), CHOOSE_0)
    # A genuinely different screen must not inherit the previous streak.
    assert _break_repeated_choice(_event(floor=8), CHOOSE_0) == CHOOSE_0


def test_a_screen_with_one_option_is_not_rotated():
    state = _event(options=("Only choice",))
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 2):
        decoded = _break_repeated_choice(state, CHOOSE_0)
    assert decoded == CHOOSE_0, "there is nothing else to pick"


def test_non_choose_actions_pass_through():
    skip = {"phase": "run", "method": "skip", "args": []}
    state = _event()
    for _ in range(agent_runner.NONCOMBAT_REPEAT_LIMIT + 2):
        assert _break_repeated_choice(state, skip) == skip


def test_the_fingerprint_distinguishes_screens_by_their_options():
    assert _screen_fingerprint(_event()) == _screen_fingerprint(_event())
    assert _screen_fingerprint(_event()) != _screen_fingerprint(
        _event(options=("Fight", "Flee")))
    assert _screen_fingerprint(_event(floor=7)) != _screen_fingerprint(
        _event(floor=8))


class TestDrawShiftClassification:
    """A one-card draw offset must be named, not filed under CONTENTS.

    Measured overnight 2026-08-01, three of seven "CONTENTS (different cards)"
    divergences were really the same window of the draw sequence offset by one
    card::

        sim  [MELANCHOLY, DEFEND, UNLEASH, DEFILE, STRIKE]
        live [            DEFEND, UNLEASH, DEFILE, STRIKE, DEBILITATE]

    ``sim[1:] == live[:-1]`` exactly. That is a DRAW COUNT disagreement, not a
    card-modelling or shuffle-order one, and calling it CONTENTS points the
    investigation at the wrong subsystem -- the same mistake SIM-AHEAD was
    split out to stop.

    These call the PRODUCTION classifier,
    ``scripts.classify_divergences.classify``.

    They previously called a static ``_classify`` defined in this class that
    reimplemented the shift test, while this docstring claimed the tests drove
    the real classifier. A reimplementation passes whether or not the shipped
    code works -- it is a test of the copy -- and this project has already been
    bitten by exactly that (``test_card_pool_parity`` mapped Sloth through its
    own alias table and stayed green while the live resolver returned None for
    the same string).
    """

    @staticmethod
    def _classify(sim_hand, live_hand):
        """The production classifier, reduced to the DRAW-SHIFT verdict.

        Energies are passed equal so the SIM-AHEAD branch -- which requires
        the sim to hold no more energy AND be a strict sub-multiset -- cannot
        pre-empt the shift test being exercised here.
        """
        from scripts.classify_divergences import classify

        verdict = classify(list(sim_hand), list(live_hand), 3, 3)
        if verdict == "DRAW-SHIFT (game drew more)":
            return "game-drew-more"
        if verdict == "DRAW-SHIFT (sim drew more)":
            return "sim-drew-more"
        return None

    def test_the_measured_case_is_recognised(self):
        assert self._classify(
            ["MELANCHOLY", "DEFEND_NECROBINDER", "UNLEASH", "DEFILE", "STRIKE_NECROBINDER"],
            ["DEFEND_NECROBINDER", "UNLEASH", "DEFILE", "STRIKE_NECROBINDER", "DEBILITATE"],
        ) == "game-drew-more"

    def test_the_mirror_case_is_recognised(self):
        assert self._classify(
            ["TREMBLE", "DEFEND_NECROBINDER", "DEFEND_NECROBINDER", "DRAIN_POWER", "BODYGUARD"],
            ["BEAM_CELL", "TREMBLE", "DEFEND_NECROBINDER", "DEFEND_NECROBINDER", "DRAIN_POWER"],
        ) == "sim-drew-more"

    def test_genuinely_different_hands_are_not_called_a_shift(self):
        # A real CONTENTS divergence must not be relabelled -- that would hide
        # the card-modelling bugs this whole instrument exists to find.
        assert self._classify(
            ["UNLEASH", "DEFEND_NECROBINDER", "BODYGUARD", "SLIMED", "POKE"],
            ["STRIKE_NECROBINDER", "STRIKE_NECROBINDER", "POKE",
             "DEFEND_NECROBINDER", "DEFEND_NECROBINDER"],
        ) is None

    def test_a_pure_reorder_is_not_a_shift(self):
        assert self._classify(
            ["A_CARD", "B_CARD", "C_CARD"], ["C_CARD", "B_CARD", "A_CARD"]
        ) is None

    def test_identical_hands_are_not_a_shift(self):
        assert self._classify(["A_CARD", "B_CARD"], ["A_CARD", "B_CARD"]) is None
