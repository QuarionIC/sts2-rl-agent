"""Tests for the game's actual RNG (xoshiro256** + Splitmix64).

The simulator's own Rng wraps a .NET System.Random clone, which is a
DIFFERENT generator from the game's MegaRandom. The two existing parity
tests in test_rng_parity.py do not contradict that and never claimed to --
they pin the seed derivation and the Fisher-Yates structure, not the draw
sequence. This module pins the generator itself.

Splitmix64 and xoshiro256** are public-domain reference algorithms with
published test vectors, so the implementation can be verified exactly
without a C# runtime.
"""
from __future__ import annotations

import pytest

from sts2_env.core.mega_random import GameRng, MegaRandom, splitmix64

# Published Splitmix64 outputs for an initial state of 0. These are the
# reference vectors, not values captured from our own implementation, so they
# constitute an independent check rather than a change-detector.
SPLITMIX64_FROM_ZERO = [
    0xE220A8397B1DCDAF,
    0x6E789E6AA1B965F4,
    0x06C45D188009454F,
    0xF88BB8A8724C81EC,
]


def test_splitmix64_matches_reference_vectors():
    state = 0
    outputs = []
    for _ in range(4):
        value, state = splitmix64(state)
        outputs.append(value)
    assert outputs == SPLITMIX64_FROM_ZERO


def test_seeding_uses_four_splitmix64_draws():
    """MegaRandom.Reinitialise sets _s0.._s3 from four Splitmix64 steps."""
    assert list(MegaRandom(0).state) == SPLITMIX64_FROM_ZERO


def test_next_ulong_matches_xoshiro256_starstar_reference():
    """First outputs for the canonical splitmix64(0)-seeded state."""
    rng = MegaRandom(0)
    assert [rng.next_ulong() for _ in range(3)] == [
        0x99EC5F36CB75F2B4,
        0xBF6E1F784956452A,
        0x1A5F849D4933E6E0,
    ]


def test_state_stays_within_64_bits():
    rng = MegaRandom(12345)
    for _ in range(200):
        rng.next_ulong()
        for word in rng.state:
            assert 0 <= word <= 0xFFFFFFFFFFFFFFFF


def test_from_state_round_trips():
    """Restoring live state must continue the stream, not restart it."""
    original = MegaRandom(999)
    for _ in range(10):
        original.next_ulong()
    snapshot = original.state

    restored = MegaRandom.from_state(*snapshot)
    assert [restored.next_ulong() for _ in range(5)] == [
        original.next_ulong() for _ in range(5)
    ]


def test_next_double_is_in_unit_interval():
    rng = MegaRandom(7)
    for _ in range(500):
        value = rng.next_double()
        assert 0.0 <= value < 1.0


def test_next_int_exclusive_respects_bound():
    rng = MegaRandom(3)
    for _ in range(500):
        assert 0 <= rng.next_int_exclusive(10) < 10


def test_next_int_exclusive_rejects_non_positive():
    with pytest.raises(ValueError):
        MegaRandom(0).next_int_exclusive(0)


def test_game_rng_next_int_is_inclusive_both_ends():
    """Matches sts2_env.core.rng.Rng.next_int so it can drop into its slot."""
    rng = GameRng(MegaRandom(1))
    seen = {rng.next_int(0, 2) for _ in range(400)}
    assert seen == {0, 1, 2}


def test_game_rng_counter_increments_per_draw():
    rng = GameRng(MegaRandom(1))
    for _ in range(7):
        rng.next_int(0, 5)
    assert rng.counter == 7


def test_game_rng_counter_starts_where_the_game_left_off():
    assert GameRng(MegaRandom(1), counter=41).counter == 41


def test_shuffle_consumes_one_draw_per_element_after_the_first():
    """Descending Fisher-Yates: n elements cost n-1 draws, as in C# Rng.Shuffle."""
    rng = GameRng(MegaRandom(42))
    items = [1, 2, 3, 4, 5]
    rng.shuffle(items)
    assert rng.counter == 4
    assert sorted(items) == [1, 2, 3, 4, 5]


def test_shuffle_is_deterministic_for_a_given_state():
    first = [1, 2, 3, 4, 5, 6, 7, 8]
    second = list(first)
    GameRng(MegaRandom(2024)).shuffle(first)
    GameRng(MegaRandom(2024)).shuffle(second)
    assert first == second


def test_different_states_give_different_shuffles():
    a = list(range(12))
    b = list(range(12))
    GameRng(MegaRandom(1)).shuffle(a)
    GameRng(MegaRandom(2)).shuffle(b)
    assert a != b


def test_rng_objects_are_opaque_to_the_clone_walker():
    """clone_combat must not descend into RNG state.

    An optimization, NOT the RecursionError fix -- I initially blamed the
    crash on this branch of the object graph and was wrong; the crash was a
    cyclic closure (see test_cyclic_closure_does_not_recurse_forever) and it
    recurred with these markers in place.

    What this does buy: combat_reconstruct attaches a GameRng and a small
    shim chain to every reconstructed combat, and the walker would otherwise
    traverse it on EVERY clone -- millions of times in a beam search -- to
    rebind closures that cannot exist there. These objects hold integers and
    each other.
    """
    from sts2_env.bridge.combat_reconstruct import _RngChain

    for klass in (MegaRandom, GameRng, _RngChain):
        assert getattr(klass, "_CLONE_OPAQUE", False) is True, (
            f"{klass.__name__} lost its _CLONE_OPAQUE marker; clone_combat "
            f"will walk into RNG state again")


def test_clone_walker_skips_opaque_objects():
    """The walker honours the marker rather than merely tolerating it.

    The memo must actually map an original to a replacement, otherwise
    _rebind_function has nothing to rebind and BOTH objects come back
    unchanged -- a test that passes with the fix reverted.
    """
    from sts2_env.search.combat_mcts import _fix_leaked_closures

    original = ["ORIGINAL"]
    replacement = ["REPLACEMENT"]

    def make_closure():
        # Closes over `original`, so the walker rebinds it to `replacement`.
        def leaked():
            return original
        return leaked

    class Opaque:
        _CLONE_OPAQUE = True

        def __init__(self, fn):
            self.fn = fn

    class Transparent:
        def __init__(self, fn):
            self.fn = fn

    opaque = Opaque(make_closure())
    transparent = Transparent(make_closure())
    root = {"opaque": opaque, "transparent": transparent}

    _fix_leaked_closures(root, {id(original): replacement})

    # Control: the walker DOES reach and rebind an ordinary object.
    assert transparent.fn() is replacement, (
        "walker failed to rebind a transparent object -- this test cannot "
        "distinguish skipped from unreachable")
    # The point: it did not descend into the opaque one.
    assert opaque.fn() is original


def test_cloned_rng_is_independent_not_shared():
    """Opaque must not mean shared -- beam nodes need their own RNG state."""
    import copy

    original = GameRng(MegaRandom(5), counter=3)
    clone = copy.deepcopy(original)
    clone.next_int(0, 100)
    assert clone.counter == 4
    assert original.counter == 3, "clone shares state with the original"


def test_cyclic_closure_does_not_recurse_forever():
    """A closure that reaches itself must not blow the stack.

    _fix_leaked_closures pre-registers containers before descending but used
    to register FUNCTIONS only after rebinding returned, so a cyclic closure
    re-entered with nothing registered and recursed until Python gave up.
    That killed the live runner mid-session with RecursionError twice, after
    thousands of lines of ordinary play, and no test caught it because the
    test fixtures have shallow, acyclic closures.

    The self-reference must resolve to the REBOUND function -- pointing it
    back at the original would reintroduce exactly the state leak this
    walker exists to prevent.
    """
    import sys

    from sts2_env.search.combat_mcts import _fix_leaked_closures

    original = ["ORIGINAL"]
    replacement = ["REPLACEMENT"]

    def build(captured):
        def f():
            return f, captured      # both are genuine closure cells
        return f

    leaked = build(original)
    assert len(leaked.__closure__) == 2, "fixture lost its closure cells"

    root = {"f": leaked}
    limit = sys.getrecursionlimit()
    sys.setrecursionlimit(300)      # fail fast rather than hang
    try:
        _fix_leaked_closures(root, {id(original): replacement})
    finally:
        sys.setrecursionlimit(limit)

    rebound = root["f"]
    inner, captured = rebound()
    assert rebound is not leaked, "walker did not rebind at all"
    assert inner is rebound, "self-reference still points at the original"
    assert captured is replacement, "leaked original was not rebound"
