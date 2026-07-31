"""Mid-combat card discoveries must be chosen, not skipped.

Background
----------
42 of the game's card classes call CardSelectCmd mid-combat. Discovery is the
canonical one:

    CardSelectCmd.FromChooseACardScreen(choiceContext, cards, Owner,
                                        canSkip: true)
      -> Selector.GetSelectedCards(cards, 0, 1)

Note the ``0``. Measured live 2026-07-31, the runner read ``min_select == 0``
as "select nothing" and returned [], which the caller sends as a skip -- so
the agent declined every card it ever discovered mid-combat. Separately, when
the RL run policy was active the same payload was routed to the RUN agent,
whose observation contains deck/map/relics/potions and no combat state at all,
and which was trained to judge cards as permanent deck additions rather than
as one-turn free tempo.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge.agent_runner import (
    _is_in_combat_card_select,
    _pick_card_select_indexes,
    _score_discovered_card,
    _LAST_COMBAT_STATE,
)


DISCOVERY_OPTIONS = [
    {"index": 0, "id": "FOOTWORK", "type": "Power", "cost": 1},
    {"index": 1, "id": "BACKSTAB", "type": "Attack", "cost": 0},
    {"index": 2, "id": "ACROBATICS", "type": "Skill", "cost": 1},
]


def _discovery(**overrides):
    """The exact payload shape FromChooseACardScreen produces."""
    payload = {
        "type": "card_select",
        "in_combat": True,
        "min_select": 0,
        "max_select": 1,
        "cards": [dict(card) for card in DISCOVERY_OPTIONS],
    }
    payload.update(overrides)
    return payload


@pytest.fixture(autouse=True)
def _clear_combat_cache():
    previous = _LAST_COMBAT_STATE[0]
    _LAST_COMBAT_STATE[0] = None
    yield
    _LAST_COMBAT_STATE[0] = previous


def test_a_discovery_is_taken_rather_than_skipped():
    # The regression: min_select == 0 used to return [] -> client.skip().
    assert _pick_card_select_indexes(_discovery()) != []


def test_a_discovery_picks_the_best_card_not_a_fixed_slot():
    # Slot 0 is the Power; the Attack in slot 1 should win while healthy.
    assert _pick_card_select_indexes(_discovery()) == [1]


def test_a_low_hp_discovery_prefers_a_skill_over_an_attack():
    _LAST_COMBAT_STATE[0] = {"player": {"hp": 8, "max_hp": 70}}
    assert _pick_card_select_indexes(_discovery()) == [2]


def test_a_discovery_of_only_statuses_is_skipped():
    payload = _discovery(cards=[
        {"index": 0, "id": "BURN", "type": "Status", "cost": -1},
        {"index": 1, "id": "WOUND", "type": "Status", "cost": -1},
    ])
    assert _pick_card_select_indexes(payload) == []


def test_a_mandatory_selection_still_selects():
    # min_select >= 1 (a deck upgrade, a transform) must never return [].
    payload = _discovery(in_combat=False, min_select=1, max_select=1)
    assert len(_pick_card_select_indexes(payload)) == 1


def test_a_mandatory_selection_is_not_always_slot_zero():
    payload = _discovery(in_combat=False, min_select=1, max_select=1, cards=[
        {"index": 0, "id": "DEFEND_SILENT", "type": "Skill"},
        {"index": 1, "id": "NEUTRALIZE", "type": "Attack"},
    ])
    assert _pick_card_select_indexes(payload) == [1]


def test_multi_select_returns_the_requested_count():
    payload = _discovery(in_combat=False, min_select=2, max_select=2)
    picked = _pick_card_select_indexes(payload)
    assert len(picked) == 2
    assert len(set(picked)) == 2, "must not select the same slot twice"


def test_in_combat_defaults_to_false_on_a_pre_flag_mod_build():
    # A mod built before the in_combat flag omits the key. Defaulting to
    # False keeps the old routing rather than guessing that a deck upgrade is
    # a combat discovery.
    assert _is_in_combat_card_select({"type": "card_select"}) is False
    assert _is_in_combat_card_select({"type": "card_select", "in_combat": True}) is True


def test_statuses_and_curses_score_below_the_skip_threshold():
    # The skip rule is "best score <= 0"; anything playable must clear it.
    for bad in ("Status", "Curse"):
        assert _score_discovered_card({"type": bad, "cost": -1}, None) <= 0.0
    for good in ("Attack", "Skill", "Power"):
        assert _score_discovered_card({"type": good, "cost": 1}, None) > 0.0
