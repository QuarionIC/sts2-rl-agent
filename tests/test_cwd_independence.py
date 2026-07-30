"""Data files must resolve from the package, never the working directory.

Live 2026-07-30: the bridge runner was launched from a parent directory, so
``Path("docs/CARDS_REFERENCE.md")`` missed and every ``create_card()`` raised
FileNotFoundError. combat_reconstruct reported that as "unresolvable card id"
for cards as ordinary as STRIKE_NECROBINDER and declined every payload, so
the combat planner and the RL run agent both fell back to heuristics for an
entire session -- while the log still said each was engaged. An invalid
session's worth of measurements came out looking merely unremarkable.

The description loader is worse: it swallows OSError, so a wrong path there
yields empty descriptions with no error at all.
"""
from __future__ import annotations

import os
from pathlib import Path

import pytest


@pytest.fixture
def foreign_cwd(tmp_path):
    """Run the test body from a directory that is not the repo root."""
    previous = os.getcwd()
    os.chdir(tmp_path)
    try:
        yield tmp_path
    finally:
        os.chdir(previous)


def test_create_card_works_from_any_cwd(foreign_cwd):
    from sts2_env.cards.factory import _reference_cards, create_card
    from sts2_env.core.enums import CardId

    _reference_cards.cache_clear()      # force a real read from this cwd
    card = create_card(CardId.STRIKE_NECROBINDER)
    assert card.card_id is CardId.STRIKE_NECROBINDER
    assert card.cost >= 0


def test_reference_effects_load_from_any_cwd(foreign_cwd):
    from sts2_env.content.descriptions import _reference_effects

    _reference_effects.cache_clear()
    effects = _reference_effects()
    assert len(effects) > 100, (
        "card descriptions came back empty -- this loader swallows OSError, "
        "so a cwd-relative path fails silently here")


def test_cards_reference_path_is_absolute():
    from sts2_env.cards.factory import _CARDS_REFERENCE

    assert Path(_CARDS_REFERENCE).is_absolute()
    assert Path(_CARDS_REFERENCE).exists()
