"""Determinized PUCT MCTS over deepcopy'd CombatState for ONE combat decision.

Scope (docs/TRAINING_REVAMP_SPEC.json Phase 8): combat decisions only --
the 115-action combat slice (end turn / play card / use potion / resolve
pending combat choice). Non-combat phases (map, rewards, shop, rest, events)
are the policy net's job and are out of scope here.

Design
------
* **One deepcopy per simulation, at the root.** Each simulation clones the
  root ``CombatState`` once (``copy.deepcopy`` drags the reachable
  ``run_state`` graph along, which is exactly what makes the clone safe:
  rng streams, relic hooks and player state are all private to the clone;
  measured ~2.1 ms p50 / ~2.8 ms p90 on real mid-run G1 states). The
  selection path is then REPLAYED on that clone, so tree nodes stay purely
  statistical (no cached states). This is the minimal-clone scheme under
  determinization: node-cached states would be invalid across
  determinizations, and n_sims clones is the same clone count as
  "clone at expansion" with a shared tree.
* **Determinization by reseeding.** Simulation ``i`` uses determinization
  ``d = i % n_determinizations``: the clone's rng streams (both the
  standalone ``combat.rng`` and the run-level ``RunRngSet`` streams that
  :meth:`CombatState._run_rng` actually serves in-run) are re-seeded from
  ``(base_seed, d)`` and the hidden draw pile is reshuffled (unknown draw
  order is the hidden information; the true state's future rng draws must
  not leak into the search). Same ``d`` => same possible world along any
  action path, so statistics for a node aggregate over ~n_determinizations
  sampled worlds. The draw pile is NOT reshuffled while a pending choice is
  open (choice options may reference concrete pile cards).
* **Priors / leaf values from the policy net.** Node priors are the policy
  net's masked action distribution restricted to the legal combat slice;
  leaf values come from the value head. Both run through
  :class:`SB3PolicyEvaluator` (single features-extractor pass, ``no_grad``,
  CPU-friendly, hash-cached -- determinized worlds repeatedly reach
  identical states, so the cache absorbs a large share of net evals).
* **Terminal values**: player dead => ``loss_value`` (-1). Combat won =>
  the value net evaluated at the terminal state (HP-sensitive: winning at
  60 HP must beat winning at 5 HP), or ``win_value`` when
  ``win_value_from_net`` is off (used by tests to make win detection
  exact).
* **PUCT** ``Q(a) + c_puct * P(a) * sqrt(N) / (1 + N(a))``, c_puct ~= 1.5,
  optional root Dirichlet noise for data generation.

Because different determinizations can disagree about which actions are
legal below the root (different redraws => different hands), the legal set
is recomputed from the simulation's own clone at every descent step and
PUCT argmaxes over that set only; actions first seen under a later
determinization fall back to the node's uniform-at-expansion default prior.

API
---
``mcts_action_distribution(env_or_state, policy, n_sims) ->
(visit_probs over the 115 combat actions, root_value)``
"""

from __future__ import annotations

import copy
import functools
import hashlib
import math
import types
from dataclasses import dataclass
from typing import Any, Callable, Protocol

import numpy as np

# Import order: the cards package MUST be fully initialized before
# sts2_env.core.combat starts importing (core.combat -> cards.base triggers
# the cards package init, which imports core.combat back -> circular import
# when this module is a process's FIRST sts2_env import, e.g. in spawned
# ExIt workers). Same trap documented in
# .claude/skills/sts2-research-frontier/scripts/bench_deepcopy.py.
import sts2_env.cards.factory  # noqa: F401  (must precede core.combat)

from sts2_env.core.combat import CombatState
from sts2_env.core.constants import ACTION_END_TURN, ACTION_SPACE_SIZE
from sts2_env.core.rng import Rng
from sts2_env.gym_env.action_space import (
    action_to_card_and_target,
    action_to_potion_and_target,
    get_action_mask,
    is_potion_action,
)
from sts2_env.gym_env.rich_observation import RichObservationEncoder

#: The combat slice of the unified run action space (115 = 1 end-turn +
#: 60 card plays + 54 potion uses; pending-choice screens reuse 0..N).
COMBAT_ACTIONS = ACTION_SPACE_SIZE

#: Run-level rng streams that combat actually draws from when attached to a
#: run (CombatState._run_rng resolves these on run_state.rng; the standalone
#: ``combat.rng`` is only the fallback for bare combats).
RUN_RNG_STREAMS = (
    "shuffle",
    "combat_card_generation",
    "combat_potion_generation",
    "combat_card_selection",
    "combat_energy_costs",
    "combat_targets",
    "monster_ai",
    "combat_orbs",
)

_SEED_MASK = 0x7FFFFFFF


# ---------------------------------------------------------------------------
# Config
# ---------------------------------------------------------------------------

@dataclass
class MCTSConfig:
    n_simulations: int = 96
    n_determinizations: int = 12
    c_puct: float = 1.5
    #: Root Dirichlet noise (data generation only; 0 disables).
    dirichlet_alpha: float = 0.3
    dirichlet_eps: float = 0.0
    #: Max decisions per simulation path before bootstrapping with the value
    #: net (also the guard against pending-choice toggle cycles in-tree).
    max_depth: int = 60
    loss_value: float = -1.0
    win_value: float = 1.0
    #: Won-combat leaves: evaluate the value net at the terminal state
    #: (HP-sensitive) instead of the constant ``win_value``.
    win_value_from_net: bool = True
    seed: int = 0


# ---------------------------------------------------------------------------
# Cloning: deepcopy + closure rebinding
# ---------------------------------------------------------------------------
#
# ``copy.deepcopy`` treats FUNCTION objects as atomic, but the simulator
# builds monster AIs as factory closures over their creature
# (monsters/*.py: ``def chomp(combat): _deal_damage_to_player(combat,
# creature, ...)``) and pending choices can capture the live combat the
# same way. A plain deepcopy therefore yields clones whose AI moves still
# act on the ORIGINAL creatures -- simulations visibly corrupted the live
# env (enemy block climbing 0 -> 196 across searches) before this fix.
# ``clone_combat`` repairs that: it walks the cloned graph and rebuilds
# every function whose closure cells reference an original object that the
# deepcopy memo mapped to a clone. Sim internals stay untouched.

#: Types the closure-rebinding walk never descends into.
_WALK_ATOMIC = (
    type(None), bool, int, float, complex, str, bytes, bytearray,
    type, types.ModuleType, types.BuiltinFunctionType,
)


def _rebind_function(fn: types.FunctionType, memo: dict,
                     recurse=None, register=None) -> types.FunctionType:
    """A copy of ``fn`` whose closure cells point at the deepcopy'd
    counterparts of their contents (unchanged cells are shared).

    ``recurse`` is the walker's own fix function. It matters: a closure cell
    can itself hold ANOTHER closure, and mapping only through ``memo`` leaves
    that inner function untouched -- still bound to the ORIGINAL combat. That
    hole let simulated card generation write into the live hand (measured:
    3 of 60 searches added DISCOVERY/VOLLEY/SECRET_TECHNIQUE to the live
    player's hand and removed cards from the live draw pile). Passing the
    walker in makes the rebinding transitive.
    """
    cells = fn.__closure__ or ()
    if not cells:
        return fn

    # BUILD THE REPLACEMENT FIRST, THEN FILL ITS CELLS.
    #
    # Closures can form CYCLES -- a function whose cell leads, directly or
    # through other closures, back to itself. The walker pre-registers
    # containers before descending ("pre-register for cycles") but used to
    # register functions only AFTER _rebind_function returned, so a cyclic
    # closure re-entered with nothing registered and recursed until Python
    # gave up. That killed the live runner mid-session with RecursionError,
    # twice, after ~2800 log lines of ordinary play.
    #
    # Creating the function up front and registering it via `register`
    # closes the cycle: re-entry finds the replacement already in `seen` and
    # returns it, and because cell contents are writable the cells can be
    # filled afterwards -- including with the replacement itself, which is
    # what a self-referential closure actually wants.
    new_cells = tuple(types.CellType() for _ in cells)
    rebound = types.FunctionType(
        fn.__code__, fn.__globals__, fn.__name__, fn.__defaults__, new_cells
    )
    rebound.__kwdefaults__ = fn.__kwdefaults__
    rebound.__dict__.update(fn.__dict__)
    if register is not None:
        register(rebound)

    changed = False
    for cell, new_cell in zip(cells, new_cells):
        try:
            contents = cell.cell_contents
        except ValueError:  # empty cell -- leave the replacement empty too
            continue
        replacement = memo.get(id(contents), contents)
        if replacement is contents and recurse is not None:
            # Not a deepcopied object -- but it may be a function, method or
            # partial that itself closes over originals.
            if isinstance(contents, (types.FunctionType, types.MethodType)) \
                    or isinstance(contents, functools.partial):
                replacement = recurse(contents)
        if replacement is not contents:
            changed = True
        new_cell.cell_contents = replacement

    if not changed and register is None:
        # Nothing needed rebinding and nobody has seen the replacement, so
        # hand back the original and let it stay shared.
        return fn
    return rebound


def _fix_leaked_closures(root: Any, memo: dict) -> None:
    """Walk ``root`` (a fresh clone) and rebind every reachable closure
    function whose cells leak originals. In-place for object attributes,
    dict values, and list items; tuples/frozensets are rebuilt bottom-up."""
    seen: dict[int, Any] = {}

    def fix(obj: Any) -> Any:
        if isinstance(obj, _WALK_ATOMIC):
            return obj
        # OPAQUE OBJECTS: nothing inside can hold a leaked closure, so
        # descending is pure cost -- and on a deep combat it is the cost that
        # pushed this walk past Python's recursion limit. The reconstructed
        # shuffle-RNG chain (GameRng / MegaRandom / the run-state shim) holds
        # only integers and each other; it appeared in the graph the moment
        # live combats started carrying the game's real RNG, and clone_combat
        # runs millions of times during a beam search.
        if getattr(type(obj), "_CLONE_OPAQUE", False):
            return obj
        oid = id(obj)
        if oid in seen:
            return seen[oid]
        if isinstance(obj, types.FunctionType):
            # Register the replacement the moment it exists, BEFORE its cells
            # are walked, so a cyclic closure resolves instead of recursing.
            def register(replacement, _oid=oid):
                seen[_oid] = replacement

            fixed = _rebind_function(obj, memo, fix, register)
            seen[oid] = fixed
            return fixed
        if isinstance(obj, types.MethodType):
            # A BOUND METHOD carries its receiver in __self__, which the
            # attribute walk below cannot reach (a method's __dict__ is the
            # underlying function's). If __self__ is an original, every call
            # through this method mutates the LIVE object.
            recv = memo.get(id(obj.__self__), obj.__self__)
            func = fix(obj.__func__)
            if recv is not obj.__self__ or func is not obj.__func__:
                rebuilt = types.MethodType(func, recv)
                seen[oid] = rebuilt
                return rebuilt
            return obj
        if isinstance(obj, functools.partial):
            # partial.func/args/keywords are read-only attributes, NOT entries
            # in __dict__, so the attribute walk cannot see or replace them.
            nf = fix(obj.func)
            na = tuple(fix(a) for a in obj.args)
            nk = {k: fix(v) for k, v in (obj.keywords or {}).items()}
            if (nf is not obj.func
                    or any(n is not o for n, o in zip(na, obj.args))
                    or any(nk[k] is not v for k, v in (obj.keywords or {}).items())):
                rebuilt = functools.partial(nf, *na, **nk)
                seen[oid] = rebuilt
                return rebuilt
            return obj
        seen[oid] = obj  # pre-register for cycles; tuples overwrite below
        if isinstance(obj, dict):
            for k, v in list(obj.items()):
                nv = fix(v)
                if nv is not v:
                    obj[k] = nv
        elif isinstance(obj, list):
            for i, v in enumerate(obj):
                nv = fix(v)
                if nv is not v:
                    obj[i] = nv
        elif isinstance(obj, tuple):
            items = [fix(v) for v in obj]
            if any(n is not o for n, o in zip(items, obj)):
                rebuilt = tuple(items)
                seen[oid] = rebuilt
                return rebuilt
        elif isinstance(obj, (set, frozenset)):
            items = [fix(v) for v in obj]
            if any(n is not o for n, o in zip(items, obj)):
                rebuilt = type(obj)(items)
                seen[oid] = rebuilt
                return rebuilt
        else:
            d = getattr(obj, "__dict__", None)
            if d is not None:
                for k, v in list(d.items()):
                    nv = fix(v)
                    if nv is not v:
                        setattr(obj, k, nv)
            for klass in type(obj).__mro__:
                for slot in getattr(klass, "__slots__", ()):
                    if slot in ("__dict__", "__weakref__"):
                        continue
                    try:
                        v = getattr(obj, slot)
                    except AttributeError:
                        continue
                    nv = fix(v)
                    if nv is not v:
                        setattr(obj, slot, nv)
        return obj

    fix(root)


def clone_combat(combat: CombatState) -> CombatState:
    """Deepcopy a CombatState AND rebind leaked closure cells so the clone
    is fully self-contained (safe to simulate on). Use this -- never a bare
    ``copy.deepcopy`` -- for search clones."""
    memo: dict = {}
    clone = copy.deepcopy(combat, memo)
    _fix_leaked_closures(clone, memo)
    return clone


# ---------------------------------------------------------------------------
# State plumbing: clone rng access, determinization, action application
# ---------------------------------------------------------------------------

def combat_run_state(combat: CombatState):
    """The run_state reachable from a combat (None for bare test combats).

    Mirrors the resolution chain of :meth:`CombatState._run_rng`.
    """
    state = getattr(combat, "_primary_player_state", None)
    player_state = getattr(state, "player_state", None)
    return getattr(player_state, "run_state", None)


def determinize(combat: CombatState, det_seed: int) -> None:
    """Re-seed ALL rng streams the clone can draw from and reshuffle the
    hidden draw pile, in place.

    Must only ever be called on a clone -- never on the live state.
    Reshuffling is skipped while a pending choice is open, because choice
    options may reference concrete cards at concrete pile positions.
    """
    det_seed &= _SEED_MASK
    combat.rng = Rng(det_seed)
    rs = combat_run_state(combat)
    if rs is not None and getattr(rs, "rng", None) is not None:
        for name in RUN_RNG_STREAMS:
            setattr(rs.rng, name, Rng(det_seed, name))
        # Keep the alias in RunRngSet coherent.
        rs.rng.combat_potion = rs.rng.combat_potion_generation
    if combat.pending_choice is None:
        for state in combat.combat_player_states:
            combat.shuffle_rng.shuffle(state.draw)


def apply_combat_action(combat: CombatState, action: int) -> None:
    """Apply one combat-slice action to a (cloned) CombatState.

    Exactly the raw-CombatState semantics that RunManager delegates to
    mid-combat (run_manager.py _do_combat_*): pending choices map 0=confirm
    / 1+i=option i; otherwise 0=end turn, card and potion slices as laid
    out by sts2_env.gym_env.action_space.
    """
    if combat.pending_choice is not None:
        if action == ACTION_END_TURN:
            combat.resolve_pending_choice(None)
        else:
            combat.resolve_pending_choice(action - 1)
    elif action == ACTION_END_TURN:
        combat.end_player_turn()
    elif is_potion_action(action):
        slot_idx, target_idx = action_to_potion_and_target(action)
        if slot_idx is not None:
            combat.use_potion(slot_idx, target_index=target_idx)
    else:
        hand_idx, target_idx = action_to_card_and_target(action)
        if hand_idx is not None:
            combat.play_card(hand_idx, target_idx)


# ---------------------------------------------------------------------------
# Observation builders (clone -> policy-net input)
# ---------------------------------------------------------------------------

class _CloneMgrView:
    """Duck-typed RunManager view over a CLONED combat for
    RichObservationEncoder.encode_run: the combat segments come from the
    clone, the run segment from the clone's own (deep-copied) run_state,
    and the static bits (room type, boss setup) from the live manager --
    they cannot change mid-combat."""

    def __init__(self, combat: CombatState, run_state, room_type, act_boss_setup):
        self._combat = combat
        self._run_state = run_state
        self._current_room_type = room_type
        self.act_boss_setup = act_boss_setup
        self._offered_potion = None
        self._offered_relic = None

    @property
    def run_state(self):
        return self._run_state

    @property
    def phase(self) -> str:
        return "COMBAT"

    def get_combat_state(self) -> CombatState:
        return self._combat


def make_run_obs_builder(env: Any) -> Callable[[CombatState], np.ndarray]:
    """Obs builder for a RichSTS2RunEnv: encode the clone with the run-level
    context of ITS OWN deep-copied run_state (HP/gold/deck stay consistent
    with what happens inside the simulated combat)."""
    mgr = env._mgr
    assert mgr is not None, "env must be reset() before search"
    encoder = getattr(env, "_encoder", None) or RichObservationEncoder()
    room_type = getattr(mgr, "_current_room_type", None)
    boss_setup = getattr(mgr, "act_boss_setup", None)
    live_rs = mgr.run_state

    def build(combat: CombatState) -> np.ndarray:
        rs = combat_run_state(combat) or live_rs
        view = _CloneMgrView(combat, rs, room_type, boss_setup)
        return encoder.encode_run(view)

    return build


def make_bare_obs_builder() -> Callable[[CombatState], np.ndarray]:
    """Obs builder for a bare CombatState (tests / combat-only callers):
    combat segments only, run segment zeroed."""
    encoder = RichObservationEncoder()

    def build(combat: CombatState) -> np.ndarray:
        return encoder.encode_combat(combat)

    return build


# ---------------------------------------------------------------------------
# Policy evaluators
# ---------------------------------------------------------------------------

class Evaluator(Protocol):
    """(obs, legal mask over the FULL policy action space) ->
    (probs over that space, scalar value)."""

    action_dim: int

    def evaluate(self, obs: np.ndarray, mask: np.ndarray) -> tuple[np.ndarray, float]:
        ...


class SB3PolicyEvaluator:
    """Masked action distribution + value from an SB3 MaskablePPO policy in
    ONE features-extractor pass (``get_distribution`` + ``predict_values``
    would run the big extractor twice), ``no_grad``, with a digest-keyed
    result cache (determinized worlds repeatedly revisit identical states).
    """

    def __init__(self, policy_or_model: Any, cache_size: int = 4096):
        policy = getattr(policy_or_model, "policy", policy_or_model)
        self.policy = policy
        self.action_dim = int(policy.action_space.n)
        self.cache_size = int(cache_size)
        self._cache: dict[bytes, tuple[np.ndarray, float]] = {}
        self.cache_hits = 0
        self.cache_misses = 0

    def evaluate(self, obs: np.ndarray, mask: np.ndarray) -> tuple[np.ndarray, float]:
        import torch as th

        key = hashlib.blake2b(
            obs.tobytes() + mask.tobytes(), digest_size=16
        ).digest()
        hit = self._cache.get(key)
        if hit is not None:
            self.cache_hits += 1
            return hit
        self.cache_misses += 1

        policy = self.policy
        with th.no_grad():
            obs_t, _ = policy.obs_to_tensor(obs)
            features = policy.extract_features(obs_t)
            if policy.share_features_extractor:
                latent_pi, latent_vf = policy.mlp_extractor(features)
            else:
                pi_features, vf_features = features
                latent_pi = policy.mlp_extractor.forward_actor(pi_features)
                latent_vf = policy.mlp_extractor.forward_critic(vf_features)
            logits = policy.action_net(latent_pi)[0].cpu().numpy().astype(np.float64)
            value = float(policy.value_net(latent_vf).item())

        legal = np.asarray(mask, dtype=bool)
        probs = np.zeros(self.action_dim, dtype=np.float64)
        if legal.any():
            z = logits[legal] - logits[legal].max()
            e = np.exp(z)
            probs[legal] = e / e.sum()
        result = (probs, value)
        if len(self._cache) >= self.cache_size:
            self._cache.clear()
        self._cache[key] = result
        return result


class UniformEvaluator:
    """Uniform priors over legal actions, constant value (tests / pure MCTS)."""

    def __init__(self, action_dim: int = COMBAT_ACTIONS, value: float = 0.0):
        self.action_dim = int(action_dim)
        self.value = float(value)

    def evaluate(self, obs: np.ndarray, mask: np.ndarray) -> tuple[np.ndarray, float]:
        legal = np.asarray(mask, dtype=bool)
        probs = np.zeros(self.action_dim, dtype=np.float64)
        n = int(legal.sum())
        if n:
            probs[legal] = 1.0 / n
        return probs, self.value


# ---------------------------------------------------------------------------
# The tree
# ---------------------------------------------------------------------------

class _Node:
    __slots__ = ("n", "w", "priors", "default_prior", "expanded", "children")

    def __init__(self) -> None:
        self.n = 0
        self.w = 0.0
        self.priors: dict[int, float] = {}
        self.default_prior = 0.0
        self.expanded = False
        self.children: dict[int, _Node] = {}

    @property
    def q(self) -> float:
        return self.w / self.n if self.n else 0.0

    def set_priors(self, probs115: np.ndarray, legal115: np.ndarray) -> None:
        idx = np.flatnonzero(legal115)
        self.priors = {int(a): float(probs115[a]) for a in idx}
        self.default_prior = 1.0 / len(idx) if len(idx) else 0.0
        self.expanded = True


class CombatMCTS:
    """Determinized PUCT search over a cloned CombatState.

    One instance per decision is fine (and what
    :func:`mcts_action_distribution` does); the tree is not reused across
    decisions because the true state advances.
    """

    def __init__(
        self,
        evaluator: Evaluator,
        obs_builder: Callable[[CombatState], np.ndarray],
        config: MCTSConfig | None = None,
    ):
        self.evaluator = evaluator
        self.obs_builder = obs_builder
        self.cfg = config or MCTSConfig()
        #: simulations that died to a sim exception (diagnostics)
        self.sim_errors = 0

    # -- evaluation helpers ------------------------------------------------

    def _full_mask(self, mask115: np.ndarray) -> np.ndarray:
        full = np.zeros(self.evaluator.action_dim, dtype=bool)
        full[:COMBAT_ACTIONS] = mask115
        return full

    def _eval_state(
        self, combat: CombatState, mask115: np.ndarray
    ) -> tuple[np.ndarray, float]:
        """(priors over the legal combat slice re-normalized to sum 1, value)."""
        obs = self.obs_builder(combat)
        probs, value = self.evaluator.evaluate(obs, self._full_mask(mask115))
        p = probs[:COMBAT_ACTIONS] * mask115
        s = p.sum()
        if s > 1e-12:
            p = p / s
        else:  # policy mass entirely outside the combat slice: uniform
            n = int(mask115.sum())
            p = mask115.astype(np.float64) / max(n, 1)
        return p, value

    def _state_value(self, combat: CombatState) -> float:
        obs = self.obs_builder(combat)
        _, value = self.evaluator.evaluate(
            obs, np.zeros(self.evaluator.action_dim, dtype=bool)
        )
        return value

    def _terminal_value(self, combat: CombatState) -> tuple[bool, float]:
        player = combat.primary_player
        if player is not None and not player.is_alive:
            return True, self.cfg.loss_value
        if combat.is_over:
            if self.cfg.win_value_from_net:
                return True, self._state_value(combat)
            return True, self.cfg.win_value
        return False, 0.0

    # -- selection ---------------------------------------------------------

    def _select(self, node: _Node, legal115: np.ndarray) -> int:
        sqrt_n = math.sqrt(max(node.n, 1))
        best_a, best_score = -1, -math.inf
        for a in np.flatnonzero(legal115):
            a = int(a)
            child = node.children.get(a)
            n_a = child.n if child is not None else 0
            q_a = child.q if child is not None else 0.0
            p_a = node.priors.get(a, node.default_prior)
            score = q_a + self.cfg.c_puct * p_a * sqrt_n / (1 + n_a)
            if score > best_score:
                best_a, best_score = a, score
        return best_a

    # -- main --------------------------------------------------------------

    def run(
        self,
        root_combat: CombatState,
        root_mask115: np.ndarray | None = None,
        base_seed: int | None = None,
    ) -> tuple[np.ndarray, float]:
        """Search from ``root_combat`` (NOT mutated) and return
        ``(visit_probs over the 115 combat actions, root value)``.

        ``root_mask115`` further restricts the root's legal set (pass the
        env's combat mask slice so anti-dither restrictions carry over);
        below the root the raw simulator mask rules.
        """
        cfg = self.cfg
        if base_seed is None:
            base_seed = cfg.seed
        base_seed &= _SEED_MASK

        raw_root_mask = get_action_mask(root_combat).astype(bool)
        legal_root = raw_root_mask.copy()
        if root_mask115 is not None:
            legal_root &= np.asarray(root_mask115, dtype=bool)
            if not legal_root.any():
                legal_root = raw_root_mask
        n_legal = int(legal_root.sum())

        root = _Node()
        priors, root_prior_value = self._eval_state(root_combat, legal_root)
        root.set_priors(priors, legal_root)

        visit_probs = np.zeros(COMBAT_ACTIONS, dtype=np.float64)
        if n_legal <= 1:
            # Forced move: nothing to search.
            if n_legal == 1:
                visit_probs[int(np.flatnonzero(legal_root)[0])] = 1.0
            return visit_probs, root_prior_value

        if cfg.dirichlet_eps > 0.0:
            noise_rng = np.random.default_rng(base_seed)
            idx = np.flatnonzero(legal_root)
            noise = noise_rng.dirichlet([cfg.dirichlet_alpha] * len(idx))
            for a, nz in zip(idx, noise):
                a = int(a)
                root.priors[a] = (
                    (1 - cfg.dirichlet_eps) * root.priors[a]
                    + cfg.dirichlet_eps * float(nz)
                )

        for sim in range(cfg.n_simulations):
            det = sim % max(cfg.n_determinizations, 1)
            det_seed = (base_seed + 0x9E3779B1 * (det + 1)) & _SEED_MASK
            state = clone_combat(root_combat)
            determinize(state, det_seed)

            node = root
            path = [root]
            depth = 0
            value = 0.0
            legal = legal_root
            while True:
                if node is not root:
                    terminal, value = self._terminal_value(state)
                    if terminal:
                        break
                    legal = get_action_mask(state).astype(bool)
                    if not legal.any():
                        value = self._state_value(state)
                        break
                    if not node.expanded:
                        p, value = self._eval_state(state, legal)
                        node.set_priors(p, legal)
                        break
                if depth >= cfg.max_depth:
                    value = self._state_value(state)
                    break
                action = self._select(node, legal)
                if action < 0:
                    value = self._state_value(state)
                    break
                try:
                    apply_combat_action(state, action)
                except Exception:
                    # Simulator bug in a hypothetical line: neutral value,
                    # never scored as a death (run_env sim_error convention).
                    self.sim_errors += 1
                    value = 0.0
                    child = node.children.setdefault(action, _Node())
                    path.append(child)
                    break
                child = node.children.get(action)
                if child is None:
                    child = _Node()
                    node.children[action] = child
                node = child
                path.append(node)
                depth += 1

            for visited in path:
                visited.n += 1
                visited.w += value

        total = sum(child.n for child in root.children.values())
        if total > 0:
            for a, child in root.children.items():
                visit_probs[a] = child.n / total
        else:  # all simulations failed before leaving the root: fall back
            for a, p in root.priors.items():
                visit_probs[a] = p
        return visit_probs, root.q if root.n else root_prior_value


# ---------------------------------------------------------------------------
# Convenience entry point
# ---------------------------------------------------------------------------

def mcts_action_distribution(
    env_or_state: Any,
    policy: Any,
    n_sims: int = 96,
    config: MCTSConfig | None = None,
    root_mask: np.ndarray | None = None,
    base_seed: int | None = None,
) -> tuple[np.ndarray, float]:
    """Run one combat-decision MCTS and return
    ``(visit_probs over the 115-action combat slice, root_value)``.

    Parameters
    ----------
    env_or_state : a reset ``RichSTS2RunEnv`` currently in the COMBAT phase
        (searched state = its live combat; obs get full run-level context;
        the root legal set defaults to the env's own combat mask slice, so
        anti-dither restrictions are respected) OR a bare ``CombatState``
        (run segment zeroed -- tests / combat-only callers).
    policy : an SB3 MaskablePPO model or policy (wrapped in
        :class:`SB3PolicyEvaluator`), or any object with an
        ``evaluate(obs, mask) -> (probs, value)`` method and ``action_dim``.
    n_sims : simulation budget (overrides ``config.n_simulations``).
    config : optional :class:`MCTSConfig`.
    root_mask : optional extra root restriction over the 115 slice
        (defaults to the env's combat mask slice when an env is passed).
    base_seed : determinization seed base for this decision (defaults to
        ``config.seed``); vary per decision for fresh determinizations.
    """
    cfg = config or MCTSConfig()
    if n_sims is not None and int(n_sims) != cfg.n_simulations:
        cfg = MCTSConfig(**{**cfg.__dict__, "n_simulations": int(n_sims)})

    if isinstance(env_or_state, CombatState):
        combat = env_or_state
        obs_builder = make_bare_obs_builder()
    else:
        env = env_or_state
        mgr = getattr(env, "_mgr", None)
        assert mgr is not None, "env must be reset() before search"
        combat = mgr.get_combat_state()
        assert combat is not None and not combat.is_over, (
            "mcts_action_distribution requires a live combat "
            f"(phase={mgr.phase!r})"
        )
        obs_builder = make_run_obs_builder(env)
        if root_mask is None:
            root_mask = np.asarray(
                env.action_masks()[:COMBAT_ACTIONS], dtype=bool
            )

    if hasattr(policy, "evaluate") and hasattr(policy, "action_dim"):
        evaluator: Evaluator = policy
    else:
        evaluator = SB3PolicyEvaluator(policy)

    mcts = CombatMCTS(evaluator, obs_builder, cfg)
    return mcts.run(combat, root_mask115=root_mask, base_seed=base_seed)
