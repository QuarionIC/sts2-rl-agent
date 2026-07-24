#!/usr/bin/env python3
"""Benchmark deepcopy cost of a minimal seeded CombatState (search feasibility).

Search (MCTS/ExIt, TRAINING_REVAMP_SPEC.json Phase 8) clones the state at
every expansion, so us/deepcopy bounds achievable simulations per move.
The spec records ~516us for CombatState; this script re-measures on a
minimal Necrobinder-vs-ShrinkerBeetle combat (2026-07-24 result on the
campaign machine: ~340us). Real mid-run states with more enemies, powers,
and piles will be slower -- treat this as a lower bound and re-measure on
states sampled from actual runs before sizing a search budget.

Usage (from the repo root):
    .venv\\Scripts\\python.exe .claude\\skills\\sts2-research-frontier\\scripts\\bench_deepcopy.py

Read-only: builds an in-memory combat, runs deepcopy in a loop, prints timing.
NOTE: import sts2_env.cards.factory BEFORE sts2_env.core.combat -- importing
core.combat first triggers a circular import via the card-registration side
effects (same class of trap as tests/test_import_order_no_cycle.py guards).
"""

from __future__ import annotations

import copy
import sys
import time
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[4]))

from sts2_env.cards.factory import create_card  # noqa: E402  (import order: see docstring)
from sts2_env.core.combat import CombatState  # noqa: E402
from sts2_env.core.enums import CardId  # noqa: E402
from sts2_env.core.rng import Rng  # noqa: E402
from sts2_env.monsters.act1_weak import create_shrinker_beetle  # noqa: E402


def main(reps: int = 1000) -> None:
    deck = [create_card(CardId.STRIKE_NECROBINDER) for _ in range(5)]
    deck += [create_card(CardId.DEFEND_NECROBINDER) for _ in range(5)]
    combat = CombatState(
        player_hp=80,
        player_max_hp=80,
        deck=deck,
        rng_seed=42,
        character_id="Necrobinder",
    )
    creature, ai = create_shrinker_beetle(Rng(42))
    combat.add_enemy(creature, ai)
    combat.start_combat()

    # Warmup, then measure.
    for _ in range(50):
        copy.deepcopy(combat)
    t0 = time.perf_counter()
    for _ in range(reps):
        copy.deepcopy(combat)
    per_copy_us = (time.perf_counter() - t0) / reps * 1e6

    print(f"CombatState deepcopy (minimal seed-42 combat): {per_copy_us:.0f} us/copy over {reps} reps")
    print(f"Implied clone budget: ~{1e6 / per_copy_us:,.0f} clones/sec/core")


if __name__ == "__main__":
    main()
