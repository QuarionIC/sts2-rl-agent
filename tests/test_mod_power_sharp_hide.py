"""The Guardian must apply SHARP_HIDE, and shed it on Roll Attack.

Scope note
----------
The POWER itself is already implemented in ``sts2_env/powers/damage_reactions``
and covered by ``tests/test_sharp_hide_power_parity.py``. This file covers only
what was missing: the Guardian's use of it.

What was wrong
--------------
``create_guardian``'s Close Up applied ``PowerId.THORNS`` as a stand-in, and
Roll Attack removed nothing. Both are wrong against Guardian.cs:

* Guardian.CloseUp applies ``SharpHidePower`` (Guardian.cs:284), not Thorns.
  The two are not interchangeable: Thorns retaliates per HIT TAKEN, Sharp Hide
  once per ATTACK CARD PLAYED and regardless of what the card targets. A 4-hit
  attack cost the player 4x under the stand-in and 1x in the real game.
* Guardian.RollAttack ends with ``PowerCmd.Remove<SharpHidePower>``
  (Guardian.cs:303). Never removing it let a simulated Guardian accumulate
  retaliation across every Defensive Mode cycle, an error that compounds over
  exactly the long boss fight where it matters.

The live wire also sends ``ACTSFROMTHEPAST-SHARP_HIDE_POWER``, so the stand-in
guaranteed a power-id mismatch on every reconstructed Guardian fight on top of
the behavioural one.
"""

from __future__ import annotations

import pytest

import sts2_env.powers  # noqa: F401  -- registers the power classes

from sts2_env.cards.ironclad import create_ironclad_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.exordium import (
    GUARDIAN_BASE_SHARP_HIDE,
    GUARDIAN_CLOSE_UP_MOVE,
    GUARDIAN_DEADLY_SHARP_HIDE,
    GUARDIAN_ROLL_ATTACK_MOVE,
    create_guardian,
)
from sts2_env.powers.damage_reactions import SharpHidePower


def _combat(ascension: int = 0) -> CombatState:
    combat = CombatState(
        player_hp=80, player_max_hp=80,
        deck=create_ironclad_starter_deck(),
        rng_seed=42, character_id="Ironclad",
    )
    combat.ascension_level = ascension
    return combat


def _guardian(ascension: int = 0):
    return create_guardian(Rng(1), ascension_level=ascension)


def test_close_up_applies_sharp_hide():
    creature, ai = _guardian()
    ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(_combat())

    power = creature.powers.get(PowerId.SHARP_HIDE)
    assert power is not None, "Close Up did not apply SHARP_HIDE"
    assert isinstance(power, SharpHidePower), (
        f"SHARP_HIDE resolved to {type(power).__name__}; the Guardian must get "
        f"the real power, not a second implementation"
    )


def test_close_up_does_not_apply_thorns():
    """The stand-in must not come back -- it is a different mechanic."""
    creature, ai = _guardian()
    ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(_combat())
    assert PowerId.THORNS not in creature.powers, (
        "Thorns is back as a Sharp Hide stand-in: it fires per hit taken "
        "rather than per attack played, and nothing removes it on Roll Attack"
    )


def test_roll_attack_removes_sharp_hide():
    creature, ai = _guardian()
    combat = _combat()
    ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(combat)
    assert PowerId.SHARP_HIDE in creature.powers

    ai.states[GUARDIAN_ROLL_ATTACK_MOVE].perform(combat)
    assert PowerId.SHARP_HIDE not in creature.powers, (
        "Guardian.cs:303 removes SharpHidePower on Roll Attack; leaving it "
        "makes the simulated Guardian accumulate retaliation every cycle"
    )


def test_roll_attack_still_deals_its_damage():
    """Removing the power must not have eaten the attack."""
    creature, ai = _guardian()
    combat = _combat()
    before = combat.player.current_hp
    ai.states[GUARDIAN_ROLL_ATTACK_MOVE].perform(combat)
    assert combat.player.current_hp < before


def test_close_up_then_roll_twice_does_not_accumulate():
    """The compounding case the missing removal caused."""
    creature, ai = _guardian()
    combat = _combat()
    for _ in range(3):
        ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(combat)
        ai.states[GUARDIAN_ROLL_ATTACK_MOVE].perform(combat)
    assert PowerId.SHARP_HIDE not in creature.powers

    ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(combat)
    assert creature.powers[PowerId.SHARP_HIDE].amount == GUARDIAN_BASE_SHARP_HIDE, (
        "Sharp Hide stacked across Defensive Mode cycles"
    )


@pytest.mark.parametrize("ascension,expected", [
    (0, GUARDIAN_BASE_SHARP_HIDE),
    (8, GUARDIAN_BASE_SHARP_HIDE),
    (9, GUARDIAN_DEADLY_SHARP_HIDE),
])
def test_amount_scales_at_ascension_9(ascension: int, expected: int):
    """Guardian.cs:85 -- SharpHideThorns => A9 ? 4 : 3."""
    creature, ai = _guardian(ascension)
    ai.states[GUARDIAN_CLOSE_UP_MOVE].perform(_combat(ascension))
    assert creature.powers[PowerId.SHARP_HIDE].amount == expected
