"""The reshuffle sort must compare exactly what the game compares.

ListExtensions.StableShuffle does::

    List<T> list2 = list.ToList();
    list2.Sort();                 // T : IComparable<T>
    return list.UnstableShuffle(rng);

``list2.Sort()`` dispatches to AbstractModel.CompareTo, which is::

    return Id.CompareTo(other.Id);

and ModelId.CompareTo compares (Category, Entry) ordinal -- and NOTHING
else. In particular it does not compare the upgrade level.

The simulator's key was ``(entry, card.upgraded)``. That invented an ordering
the game does not have: two copies of one card, one upgraded, were ordered
deterministically here while the game leaves their relative order to
List.Sort. Necrobinder decks carry part-upgraded duplicates as a matter of
course, so this fired on ordinary reshuffles -- observed live 2026-07-31 as a
post-reshuffle hand of [..., REANIMATE] where the game dealt [..., WISP].

Known limit, deliberately not papered over: for piles larger than 16, .NET's
introsort partitions and is no longer stable, so equal-comparing cards can
still land in an order Python's stable sort will not reproduce. Matching that
needs an introsort port. Sorting on the Entry alone is correct for the
comparison itself and matches .NET exactly for the <= 16 case, where
introsort degenerates to a stable insertion sort.
"""

from __future__ import annotations

from sts2_env.cards.factory import create_card
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId
from sts2_env.core.rng import Rng


def _combat() -> CombatState:
    return CombatState(
        player_hp=70, player_max_hp=70,
        deck=[create_card(CardId.STRIKE_NECROBINDER)],
        rng_seed=1, character_id="Necrobinder",
    )


class _FixedRng:
    """A shuffle that does nothing, so the SORT is what is under test."""

    def shuffle(self, cards):  # noqa: D401 - test double
        return cards


def test_upgrade_level_does_not_reorder_equal_entries():
    combat = _combat()
    plain = create_card(CardId.STRIKE_NECROBINDER, upgraded=False)
    upgraded = create_card(CardId.STRIKE_NECROBINDER, upgraded=True)

    # Upgraded FIRST in the pile. The game compares Entry only, so these tie,
    # and a stable sort must leave the upgraded copy in front.
    cards = [upgraded, plain]
    combat.stable_shuffle_cards(cards, _FixedRng())
    assert cards[0] is upgraded, (
        "the upgrade level was used as a tiebreak; the game's "
        "ModelId.CompareTo does not look at it"
    )

    # ...and the mirror image, to prove it is order-preserving rather than
    # just ordering the other way round.
    cards = [plain, upgraded]
    combat.stable_shuffle_cards(cards, _FixedRng())
    assert cards[0] is plain


def test_different_entries_sort_ordinally():
    combat = _combat()
    wisp = create_card(CardId.WISP)
    reanimate = create_card(CardId.REANIMATE)
    cards = [wisp, reanimate]
    combat.stable_shuffle_cards(cards, _FixedRng())
    assert [c.card_id for c in cards] == [CardId.REANIMATE, CardId.WISP], (
        "REANIMATE < WISP ordinally"
    )


def test_the_wire_entry_wins_over_the_enum_name():
    # The simulator registers COUNTDOWN_CARD where the wire says COUNTDOWN,
    # and the game sorts by its own Entry. Sorting by the enum name would
    # place the card differently and deal a different card.
    combat = _combat()
    a = create_card(CardId.STRIKE_NECROBINDER)
    b = create_card(CardId.DEFEND_NECROBINDER)
    a._wire_entry = "ZZZ_LAST"
    b._wire_entry = "AAA_FIRST"
    cards = [a, b]
    combat.stable_shuffle_cards(cards, _FixedRng())
    assert cards == [b, a]


def test_a_pile_of_duplicates_keeps_its_relative_order():
    combat = _combat()
    copies = [create_card(CardId.STRIKE_NECROBINDER) for _ in range(5)]
    copies[2].upgraded = True
    order = list(copies)
    combat.stable_shuffle_cards(copies, _FixedRng())
    assert copies == order, "equal-comparing cards must not be reordered"
