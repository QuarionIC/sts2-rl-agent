"""An index the screen cannot accept must never reach the game.

_send_noncombat_action called client.<method>(*args) with whatever the policy
produced, unchecked. Live 2026-07-31 (planner_div6.log)::

    RUN-RL (shop): action 139 -> choose([9]) | heuristic would pick 1
    RUN-RL (shop): action 138 -> choose([8]) | heuristic would pick 1
    WARNING: Timeout waiting for state. Sending ping...
    Received: type=run_complete
    Run finished: terminated (run 4 this session)

The game did not reject the index -- it went silent. An out-of-range choice
HANGS the room until the mod's timeout terminates the run, which is the worst
failure shape available: it costs the whole run and looks like the game is
just thinking.

A policy trained against a differently-sized option list will produce these,
so the guard also exists to make that disagreement loud instead of fatal.
"""

from __future__ import annotations

import pytest

from sts2_env.bridge.agent_runner import _selectable_indexes, _validate_choice


def _shop(n_items=3):
    return {
        "type": "shop",
        "floor": 12,
        "options": [
            {"index": i, "label": f"item{i}", "enabled": True}
            for i in range(n_items)
        ],
    }


def _choose(i):
    return {"phase": "run", "method": "choose", "args": [i]}


def test_an_out_of_range_choice_is_replaced():
    decoded = _validate_choice(_shop(3), _choose(9))
    assert decoded["args"][0] in (0, 1, 2)


def test_an_in_range_choice_is_untouched():
    assert _validate_choice(_shop(3), _choose(2)) == _choose(2)


def test_a_disabled_option_is_not_selectable():
    state = _shop(3)
    state["options"][2]["enabled"] = False
    assert _selectable_indexes(state) == [0, 1]
    assert _validate_choice(state, _choose(2))["args"][0] in (0, 1)


def test_a_payload_with_no_option_list_is_left_alone():
    # Unknown shape must not be read as "nothing is selectable", which would
    # suppress every choice on payloads this helper does not model.
    state = {"type": "boss_relic", "floor": 16}
    assert _selectable_indexes(state) is None
    assert _validate_choice(state, _choose(4)) == _choose(4)


def test_non_choose_methods_pass_through():
    skip = {"phase": "run", "method": "skip", "args": []}
    assert _validate_choice(_shop(3), skip) == skip


def test_map_nodes_and_cards_are_also_validated():
    nodes = {"type": "map_select", "floor": 3,
             "nodes": [{"index": 0}, {"index": 1}]}
    assert _selectable_indexes(nodes) == [0, 1]
    assert _validate_choice(nodes, _choose(7))["args"][0] in (0, 1)

    cards = {"type": "card_reward", "floor": 3,
             "cards": [{"index": 0, "type": "Attack"},
                       {"index": 1, "type": "Skill"}]}
    assert _selectable_indexes(cards) == [0, 1]
    assert _validate_choice(cards, _choose(5))["args"][0] in (0, 1)


@pytest.mark.parametrize("bad", [-1, 99, 3])
def test_every_out_of_range_index_lands_somewhere_legal(bad):
    allowed = set(_selectable_indexes(_shop(3)))
    assert _validate_choice(_shop(3), _choose(bad))["args"][0] in allowed
