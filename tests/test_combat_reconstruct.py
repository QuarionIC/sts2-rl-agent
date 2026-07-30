"""Tests for the live-combat bridge reconstruction.

This module had NO test coverage despite being load-bearing for every planned
combat in the live game: if it rebuilds a different combat than the game is
showing, the planner searches the wrong problem and plays a line that does not
fit. Both bugs found in it so far -- the COUNTDOWN id mismatch and the
discarded cost modifier -- would have been caught by a test at this level.
"""
from __future__ import annotations

import pytest

from sts2_env.bridge.combat_reconstruct import reconstruct_combat


def _payload(**overrides) -> dict:
    """A minimal payload that reconstructs successfully."""
    state = {
        "type": "combat_action",
        "round": 1,
        "player": {"hp": 50, "max_hp": 66, "energy": 3, "max_energy": 3,
                   "block": 0},
        "hand": [{"id": "STRIKE_NECROBINDER", "cost": 1}],
        "draw_pile": [{"id": "DEFEND_NECROBINDER", "cost": 1}],
        "discard_pile": [],
        "exhaust_pile": [],
        "deck": [{"id": "STRIKE_NECROBINDER", "cost": 1},
                 {"id": "DEFEND_NECROBINDER", "cost": 1}],
        "enemies": [{"combat_id": 1, "id": "CULTIST", "name": "Cultist",
                     "hp": 50, "max_hp": 50, "block": 0, "is_alive": True}],
    }
    state.update(overrides)
    return state


def _hand(combat):
    return combat.combat_player_states[0].hand


def test_baseline_payload_reconstructs():
    combat = reconstruct_combat(_payload())
    assert combat is not None
    assert [c.card_id.name for c in _hand(combat)] == ["STRIKE_NECROBINDER"]


def test_wire_cost_modifier_is_adopted():
    """A discounted card must simulate at the discounted cost.

    SerializeCard sends EnergyCost.GetWithModifiers(CostModifiers.All). When
    reconstruction ignored it, a card the game had discounted to 0 was
    simulated at full price: the planner could not afford a line the game
    allowed, and ended the turn with energy unspent.
    """
    combat = reconstruct_combat(
        _payload(hand=[{"id": "STRIKE_NECROBINDER", "cost": 0}]))
    assert combat is not None
    assert _hand(combat)[0].cost == 0


def test_wire_cost_applies_to_every_pile_not_just_hand():
    combat = reconstruct_combat(_payload(
        hand=[{"id": "STRIKE_NECROBINDER", "cost": 0}],
        draw_pile=[{"id": "DEFEND_NECROBINDER", "cost": 0}],
    ))
    assert combat is not None
    assert combat.combat_player_states[0].draw[0].cost == 0


def test_negative_wire_cost_is_ignored():
    """X-cost cards encode as a negative cost, and is_unplayable keys off
    cost < 0 -- adopting it blindly could flip a playable card unplayable."""
    combat = reconstruct_combat(
        _payload(hand=[{"id": "STRIKE_NECROBINDER", "cost": -1}]))
    assert combat is not None
    assert _hand(combat)[0].cost >= 0


def test_missing_cost_falls_back_to_the_factory():
    combat = reconstruct_combat(
        _payload(hand=[{"id": "STRIKE_NECROBINDER"}]))
    assert combat is not None
    assert _hand(combat)[0].cost >= 0


@pytest.mark.parametrize("garbage", ["", "not-a-number", None, {}])
def test_malformed_cost_does_not_raise(garbage):
    combat = reconstruct_combat(
        _payload(hand=[{"id": "STRIKE_NECROBINDER", "cost": garbage}]))
    assert combat is not None


def test_card_suffix_tolerance_resolves_countdown():
    """The wire sent COUNTDOWN while the simulator registers COUNTDOWN_CARD.

    The card resolved to None, was dropped from every pile, and the planner
    searched an 11-card deck against the game's 12 -- silently.
    """
    from sts2_env.bridge.combat_reconstruct import _to_card_id
    from sts2_env.core.enums import CardId

    resolved = _to_card_id("COUNTDOWN")
    assert resolved is not None
    assert resolved in set(CardId)


def test_unresolvable_card_id_declines_rather_than_dropping(caplog):
    """Dropping a card silently desynchronises the search from the game."""
    with caplog.at_level("ERROR"):
        combat = reconstruct_combat(
            _payload(hand=[{"id": "DEFINITELY_NOT_A_CARD_ID", "cost": 1}]))
    assert combat is None
    assert any("unresolvable card id" in record.getMessage()
               for record in caplog.records)


def _shuffle_rng_payload(counter: int = 0) -> dict:
    return {
        "counter": counter,
        "state0": 0xE220A8397B1DCDAF,
        "state1": 0x6E789E6AA1B965F4,
        "state2": 0x06C45D188009454F,
        "state3": 0xF88BB8A8724C81EC,
    }


def test_wire_shuffle_rng_is_installed_as_the_shuffle_stream():
    """The game's stream must actually reach CombatState.shuffle_rng.

    Without this the reconstruction reshuffles from a .NET-Random stream
    seeded with the ROUND NUMBER, which cannot match the game's xoshiro256**
    -- the measured cause of 83/104 whole-combat plans truncating at the
    first reshuffle.
    """
    from sts2_env.core.mega_random import GameRng, MegaRandom

    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload(17)))
    assert combat is not None

    stream = combat.shuffle_rng
    assert isinstance(stream, GameRng), (
        f"shuffle_rng is {type(stream).__name__}, so the game's stream was "
        f"not installed and reshuffles will still diverge")
    assert stream.counter == 17
    assert stream.state == MegaRandom(0).state


def test_installed_shuffle_stream_reproduces_the_game_sequence():
    """Draws must continue the transmitted stream, not a parallel one."""
    from sts2_env.core.mega_random import GameRng, MegaRandom

    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None

    expected = GameRng(MegaRandom(0))
    assert [combat.shuffle_rng.next_int(0, 9) for _ in range(5)] == [
        expected.next_int(0, 9) for _ in range(5)
    ]


def test_only_the_shuffle_stream_is_replaced():
    """Monster AI, targeting and card generation must be untouched."""
    from sts2_env.core.mega_random import GameRng

    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None
    for stream_name in ("combat_targets_rng", "combat_card_selection_rng",
                        "combat_card_generation_rng"):
        assert not isinstance(getattr(combat, stream_name), GameRng), (
            f"{stream_name} was replaced; only shuffle should be")


def test_shuffle_rng_install_also_forces_stable_reshuffle():
    """RNG parity without sort parity still deals a different hand.

    The game reshuffles via CardPileCmd.Shuffle ->
    list.StableShuffle(RunState.Rng.Shuffle), and StableShuffle sorts the
    combined pile before the Fisher-Yates pass. Our default is the unstable
    (order-dependent) shuffle, so matching only the RNG would leave the draw
    order wrong for a subtler reason.
    """
    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None
    assert getattr(combat, "_force_stable_reshuffle", False) is True


def test_no_shuffle_rng_leaves_reshuffle_semantics_alone():
    """Without the game's stream we must not silently change behaviour."""
    combat = reconstruct_combat(_payload())
    assert combat is not None
    assert getattr(combat, "_force_stable_reshuffle", False) is False


def test_stable_shuffle_sort_key_is_order_independent():
    """Two orderings of the same pile must deal identically under one stream."""
    from sts2_env.core.mega_random import GameRng, MegaRandom

    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None

    state = combat.combat_player_states[0]
    cards = list(state.hand) + list(state.draw) + list(state.discard)
    forward, backward = list(cards), list(reversed(cards))
    combat.stable_shuffle_cards(forward, GameRng(MegaRandom(0)))
    combat.stable_shuffle_cards(backward, GameRng(MegaRandom(0)))
    assert [c.card_id.name for c in forward] == [c.card_id.name for c in backward]


def test_wire_entry_is_recorded_and_can_differ_from_the_enum_name():
    """The game's Entry string is the StableShuffle sort key, not our enum.

    _to_card_id tolerates a missing/extra _CARD suffix, so the simulator's
    CardId name is not always the string the game sorts by. COUNTDOWN is the
    real case: the wire sends COUNTDOWN, the simulator registers
    COUNTDOWN_CARD, and those sort to different positions.
    """
    combat = reconstruct_combat(_payload(
        hand=[{"id": "COUNTDOWN", "cost": 1}],
        deck=[{"id": "COUNTDOWN", "cost": 1},
              {"id": "STRIKE_NECROBINDER", "cost": 1}],
        shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None
    card = combat.combat_player_states[0].hand[0]
    assert card._wire_entry == "COUNTDOWN"
    assert card.card_id.name == "COUNTDOWN_CARD"


def test_stable_shuffle_sorts_by_wire_entry_when_present():
    """Sorting must follow the game's Entry, not the simulator's enum name."""
    from sts2_env.core.mega_random import GameRng, MegaRandom

    combat = reconstruct_combat(_payload(shuffle_rng=_shuffle_rng_payload()))
    assert combat is not None

    state = combat.combat_player_states[0]
    cards = list(state.hand) + list(state.draw)
    # Force the two keys apart: enum name orders one way, wire entry the
    # other. Only a wire-entry sort produces the game's ordering.
    cards[0]._wire_entry = "ZZZ_LAST"
    cards[-1]._wire_entry = "AAA_FIRST"
    expected_first = cards[-1]

    combat.stable_shuffle_cards(cards, GameRng(MegaRandom(0)))
    # After sorting, AAA_FIRST leads; the shuffle then permutes deterministically.
    reference = [expected_first] + [c for c in cards if c is not expected_first]
    assert set(id(c) for c in cards) == set(id(c) for c in reference)
    assert any(getattr(c, "_wire_entry", None) == "AAA_FIRST" for c in cards)


def test_pure_simulation_cards_keep_the_enum_sort_key():
    """No _wire_entry means seeded simulation behaviour is unchanged."""
    from sts2_env.cards.factory import create_card
    from sts2_env.core.enums import CardId
    from sts2_env.core.mega_random import GameRng, MegaRandom

    combat = reconstruct_combat(_payload())
    assert combat is not None
    cards = [create_card(CardId.STRIKE_NECROBINDER),
             create_card(CardId.DEFEND_NECROBINDER)]
    assert all(not hasattr(c, "_wire_entry") for c in cards)
    combat.stable_shuffle_cards(cards, GameRng(MegaRandom(0)))
    assert len(cards) == 2


def test_missing_shuffle_rng_still_reconstructs():
    """Older mod builds omit the field; that must not break planning."""
    combat = reconstruct_combat(_payload())
    assert combat is not None
    assert combat.shuffle_rng is not None


@pytest.mark.parametrize("bad", [{}, {"counter": 1}, {"state0": "x"}, "nope"])
def test_malformed_shuffle_rng_degrades_quietly(bad):
    combat = reconstruct_combat(_payload(shuffle_rng=bad))
    assert combat is not None


def test_unknown_monster_declines():
    combat = reconstruct_combat(_payload(
        enemies=[{"combat_id": 1, "id": "NOT_A_MONSTER", "hp": 10,
                  "max_hp": 10, "block": 0, "is_alive": True}]))
    assert combat is None
