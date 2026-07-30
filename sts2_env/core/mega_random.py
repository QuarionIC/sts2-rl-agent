"""The game's actual RNG: xoshiro256** seeded by Splitmix64.

Why this exists
---------------
``sts2_env.core.rng.Rng`` wraps a clone of .NET ``System.Random`` (a
subtractive lagged-Fibonacci generator with a 56-entry seed array). The GAME
does not use .NET Random -- ``MegaCrit.Sts2.Core.Random.MegaRandom`` is
xoshiro256** with four 64-bit state words, seeded through Splitmix64.

Those are different generators, so the simulator has never been able to
reproduce the game's random sequence. The existing parity tests do not
contradict this and never claimed to: ``test_named_rng_streams_match_game_
seed_derivation`` proves the SEED DERIVATION matches, and
``test_shuffle_uses_csharp_fisher_yates_sequence`` proves our shuffle is
descending Fisher-Yates. Neither asserts that our draws equal the game's.

The practical consequence was measured live: reconstructed combats were
seeded with the ROUND NUMBER (combat_reconstruct passed
``rng_seed=state["round"]``), so every draw-pile reshuffle diverged from the
game, 83 of 104 whole-combat plans truncated at the first reshuffle, and 100%
of observed plan divergences were "different cards".

What this module is for
-----------------------
Given the game's live RNG state -- the four state words, which the mod can
read out of ``SerializableRng`` -- this reproduces the game's subsequent
draws exactly, so a reconstructed combat reshuffles into the same order the
game will.

Decompiled reference (v0.109.0), all in MegaCrit.Sts2.Core.Random:
    MegaRandom.Splitmix64          seeding mixer
    MegaRandom.Reinitialise(ulong) _s0.._s3 = Splitmix64 x4
    MegaRandom.NextULongInner      xoshiro256**
    MegaRandom.NextDouble          (NextULongInner() >> 11) * 2^-53
    MegaRandom.NextInner(int)      (int)(NextDouble() * maxValue)
    Rng.NextInt(maxExclusive)      _counter++; _random.Next(maxExclusive)
    Rng.Shuffle(list)              descending Fisher-Yates, NextInt(i + 1)
"""
from __future__ import annotations

_MASK64 = 0xFFFFFFFFFFFFFFFF

# 2^-53, spelled the way the decompiled source spells it. Python floats are
# IEEE-754 doubles exactly as C# doubles are, so the multiply and the
# truncating int() cast reproduce the game's arithmetic bit for bit.
_INCR_DOUBLE = 1.1102230246251565e-16


def _rotl(value: int, count: int) -> int:
    value &= _MASK64
    return ((value << count) | (value >> (64 - count))) & _MASK64


def splitmix64(state: int) -> tuple[int, int]:
    """One Splitmix64 step. Returns ``(output, new_state)``.

    C# passes the state by reference and mutates it; Python returns both so
    the caller threads it explicitly.
    """
    state = (state + 0x9E3779B97F4A7C15) & _MASK64
    z = state
    z = ((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9) & _MASK64
    z = ((z ^ (z >> 27)) * 0x94D049BB133111EB) & _MASK64
    return (z ^ (z >> 31)) & _MASK64, state


class MegaRandom:
    """xoshiro256**, matching the game's generator."""

    # Holds four integers and nothing else. clone_combat's closure walker
    # must not descend in here: there is no closure to rebind, and the extra
    # depth on every one of a beam search's millions of clones is what
    # pushed _fix_leaked_closures past the recursion limit in live play.
    _CLONE_OPAQUE = True

    __slots__ = ("_s0", "_s1", "_s2", "_s3")

    def __init__(self, seed: int = 0) -> None:
        self.reinitialise(seed)

    def reinitialise(self, seed: int) -> None:
        seed &= _MASK64
        self._s0, seed = splitmix64(seed)
        self._s1, seed = splitmix64(seed)
        self._s2, seed = splitmix64(seed)
        self._s3, seed = splitmix64(seed)

    @classmethod
    def from_state(cls, s0: int, s1: int, s2: int, s3: int) -> MegaRandom:
        """Restore from the game's live state words.

        This is the whole point of the module: the mod reads these off the
        running game, so the simulation continues the SAME stream rather than
        starting a parallel one that merely looks random.
        """
        instance = cls.__new__(cls)
        instance._s0 = s0 & _MASK64
        instance._s1 = s1 & _MASK64
        instance._s2 = s2 & _MASK64
        instance._s3 = s3 & _MASK64
        return instance

    @property
    def state(self) -> tuple[int, int, int, int]:
        return (self._s0, self._s1, self._s2, self._s3)

    def next_ulong(self) -> int:
        s0, s1, s2, s3 = self._s0, self._s1, self._s2, self._s3
        result = (_rotl((s1 * 5) & _MASK64, 7) * 9) & _MASK64
        t = (s1 << 17) & _MASK64
        s2 ^= s0
        s3 ^= s1
        s1 ^= s2
        s0 ^= s3
        s2 ^= t
        s3 = _rotl(s3, 45)
        self._s0, self._s1, self._s2, self._s3 = s0, s1, s2, s3
        return result

    def next_double(self) -> float:
        return float(self.next_ulong() >> 11) * _INCR_DOUBLE

    def next_int_exclusive(self, max_exclusive: int) -> int:
        """C# ``MegaRandom.Next(int maxValue)``."""
        if max_exclusive < 1:
            raise ValueError("max_exclusive must be > 0")
        return int(self.next_double() * float(max_exclusive))


class GameRng:
    """The game's ``Rng`` wrapper over :class:`MegaRandom`.

    Exposes the same ``next_int(low, high)`` INCLUSIVE contract as
    :class:`sts2_env.core.rng.Rng` so it can be dropped into the simulator's
    RNG-stream slots, while drawing from the game's generator underneath.
    """

    _CLONE_OPAQUE = True

    __slots__ = ("_random", "_counter")

    def __init__(self, random: MegaRandom, counter: int = 0) -> None:
        self._random = random
        self._counter = int(counter)

    @property
    def counter(self) -> int:
        return self._counter

    @property
    def state(self) -> tuple[int, int, int, int]:
        return self._random.state

    def next_int(self, low: int, high: int) -> int:
        """Inclusive on both ends, matching sts2_env.core.rng.Rng.next_int.

        The game spells the same draw as ``NextInt(maxExclusive)``; for a
        shuffle it calls ``NextInt(i + 1)``, i.e. [0, i], which is exactly
        ``next_int(0, i)`` here.
        """
        if low > high:
            raise ValueError(f"low ({low}) must be <= high ({high})")
        self._counter += 1
        return low + self._random.next_int_exclusive(high - low + 1)

    def next_int_exclusive(self, low: int, high: int) -> int:
        return self.next_int(low, high - 1)

    def next_bool(self) -> bool:
        self._counter += 1
        return (self._random.next_ulong() & 0x8000000000000000) != 0

    def next_float(self, upper: float = 1.0) -> float:
        self._counter += 1
        return self._random.next_double() * upper

    def shuffle(self, items: list) -> None:
        """Descending Fisher-Yates, matching C# ``Rng.Shuffle``."""
        for index in range(len(items) - 1, 0, -1):
            swap = self.next_int(0, index)
            items[index], items[swap] = items[swap], items[index]

    def choice(self, items: list):
        if not items:
            raise IndexError("Cannot choose from an empty list")
        return items[self.next_int(0, len(items) - 1)]
