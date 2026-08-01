"""The card embedding vocabulary must only ever grow at the end.

``rich_observation.CARD_IDS`` is ``list(CardId)`` in DECLARATION order and
``CARD_ID_TO_IDX`` maps each card to its position in that list. Four separate
bag segments of the observation (three pile bags plus the run-level deck bag)
are indexed by that position.

So inserting a member in the middle of the CardId enum does two bad things at
once:

* it shifts every later card's index, silently repointing a trained
  checkpoint's learned weights at a DIFFERENT card -- the model keeps loading
  and keeps running, it just now believes Strike is Wraith Form; and
* it widens the observation, which at least fails loudly.

The second masked the first when SIDESTEP and friends were added
2026-07-31: the width error was obvious, the permutation underneath it was
not.

These tests pin anchor indices so a mid-enum insertion fails here, naming the
consequence, instead of quietly degrading every model trained before it.
"""

from __future__ import annotations

from sts2_env.core.enums import CardId
from sts2_env.gym_env.rich_observation import (
    CARD_ID_TO_IDX,
    CARD_IDS,
    DECK_BAG_OFF,
    NUM_CARD_IDS,
    PILE_BAGS_OFF,
    PILE_BAG_SIZE,
    RICH_OBS_SIZE,
)


#: Cards spread across the enum, with the index each had when the vocabulary
#: was pinned. Every checkpoint trained since reads these positions.
#: NEVER edit these numbers to make a test pass -- a change here means the
#: vocabulary was permuted and existing checkpoints are invalid.
ANCHOR_INDEXES = {
    "STRIKE_IRONCLAD": 0,
    "JUGGERNAUT_CARD": 73,
    "SHIV": 120,
    "MIRAGE": 164,
    "SERPENT_FORM_CARD": 197,
    "MADNESS": 585,
}


def test_anchor_cards_keep_their_embedding_index():
    for name, expected in ANCHOR_INDEXES.items():
        card = CardId[name]
        assert CARD_ID_TO_IDX[card] == expected, (
            f"{name} moved from index {expected} to {CARD_ID_TO_IDX[card]}. "
            f"A CardId member was inserted before it, which repoints every "
            f"trained checkpoint's card embeddings at the wrong cards. Append "
            f"new members at the END of the enum instead."
        )


def test_recently_added_cards_sit_at_the_tail():
    # These were appended deliberately (see the APPEND-ONLY marker in enums.py).
    tail = [card.name for card in CARD_IDS[-4:]]
    assert tail == ["NOT_YET", "SIDESTEP", "ABUNDANCE", "DOWSING"], tail


def test_vocabulary_only_grows():
    # A SHRINKING vocabulary is as damaging as a permuted one, and removing a
    # card is the tempting fix when the game deletes one (v0.110.0 deleted
    # Scare). Keep the slot; drop it from the pools instead.
    assert NUM_CARD_IDS >= 590, (
        f"vocabulary shrank to {NUM_CARD_IDS}. Removing a CardId shifts every "
        f"later card down one index. If the game deleted a card, leave the "
        f"enum member in place and remove it from the card pools."
    )


def test_observation_layout_matches_the_vocabulary():
    # The invariant the bags depend on: each bag is exactly one slot per card.
    assert PILE_BAG_SIZE == NUM_CARD_IDS
    assert DECK_BAG_OFF > PILE_BAGS_OFF + 3 * PILE_BAG_SIZE - 1
    assert RICH_OBS_SIZE > DECK_BAG_OFF + NUM_CARD_IDS


def test_every_card_has_exactly_one_index():
    assert len(CARD_ID_TO_IDX) == NUM_CARD_IDS
    assert sorted(CARD_ID_TO_IDX.values()) == list(range(NUM_CARD_IDS))


class TestPowerVocabularyStability:
    """PowerId has the CARD problem, worse: no embedding to migrate.

    NUM_POWER_IDS sizes PLAYER_POWERS_SIZE and, through ENEMY_BLOCK_SIZE,
    every one of the five enemy slots -- and all of that sits inside
    RichFeaturesExtractor's flat passthrough. Cards go through an embedding
    table, so adding one means appending rows; powers are raw one-hot, so
    adding one widens the observation at SIX separate offsets and moves the
    extractor's output width with it.

    A mid-enum insertion is therefore not merely a resize: every flat feature
    after the insertion point shifts, and a checkpoint loaded against it reads
    the wrong column for every power, every intent and every enemy scalar that
    follows -- while still loading and still running.
    """

    def test_the_recently_added_powers_sit_at_the_tail(self):
        from sts2_env.core.enums import PowerId

        tail = [p.name for p in list(PowerId)[-6:]]
        assert tail == ["SPORE_CLOUD", "SHARP_HIDE", "STRENGTH_UP",
                        "REGEN_ENEMY", "EXPLOSIVE", "FADING"], tail

    def test_power_anchors_keep_their_index(self):
        from sts2_env.core.enums import PowerId
        from sts2_env.gym_env.rich_observation import POWER_ID_TO_IDX

        # NEVER edit these to make a test pass: a change means the vocabulary
        # was permuted and every trained checkpoint now reads the wrong power.
        anchors = {"STRENGTH": 20, "VULNERABLE": 25, "WEAK": 26,
                   "POISON": 47, "ARTIFACT": 186}
        for name, expected in anchors.items():
            assert POWER_ID_TO_IDX[PowerId[name]] == expected, (
                f"{name} moved to {POWER_ID_TO_IDX[PowerId[name]]}; a PowerId "
                f"was inserted before it and existing checkpoints are invalid"
            )

    def test_the_observation_layout_still_derives_from_the_power_count(self):
        import sts2_env.gym_env.rich_observation as R

        assert R.PLAYER_POWERS_SIZE == R.NUM_POWER_IDS
        # Every enemy slot carries a full power vector; if this stops holding,
        # migrate_checkpoint_powers' offset arithmetic is wrong.
        n_slots = R.ENEMIES_SIZE // R.ENEMY_BLOCK_SIZE
        assert n_slots == 5
        assert R.ENEMY_BLOCK_SIZE > R.NUM_POWER_IDS
