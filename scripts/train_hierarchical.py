#!/usr/bin/env python3
"""Train the two-agent stack: a COMBAT agent and a RUN agent that calls it.

Motivation is in ``sts2_env/gym_env/hierarchical_run_env.py``: the flat policy
plateaued at 8.4 floors because deck-shaping decisions were drowned in ~250-step
episodes. Splitting the problem gives the run agent ~11-40 decisions per
episode, so terminal reward actually reaches a floor-3 card pick.

Phases
------
``--phase combat``
    Trains on :class:`RichSTS2CombatEnv` with the progressive deck sampler, so
    the combat agent sees the full spread of mid-run decks (starter through
    +15 cards, upgrades, relics, potions, HP 50-100%) rather than only the
    states the current run policy happens to reach. Deliberately decoupled:
    the combat agent must not inherit the run agent's deck pathology.

``--phase run``
    Trains on :class:`HierarchicalRunEnv` with a FROZEN combat agent supplied
    via ``--combat-model``. Combats are consumed inside the env, so the run
    agent only ever sees map/reward/shop/rest/event decisions.

Examples
--------
    python scripts/train_hierarchical.py --phase combat --total-steps 4000000
    python scripts/train_hierarchical.py --phase run --total-steps 4000000 \
        --combat-model output/hier/combat/best_model.zip
"""

from __future__ import annotations

import os

for _var in ("OPENBLAS_NUM_THREADS", "OMP_NUM_THREADS", "MKL_NUM_THREADS"):
    os.environ.setdefault(_var, "1")

import argparse
import json
import sys
import time
from functools import partial
from pathlib import Path
from typing import Any

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))

EVAL_SEED_BLOCK = 10_000_000
TRAIN_SEED_STRIDE = 1_000


# ---------------------------------------------------------------------------
# Env factories
# ---------------------------------------------------------------------------

class HarvestedDeckSampler:
    """Sample decks the RUN agent actually brought to combat.

    The progressive sampler guesses at plausible mid-run decks. Once a run
    agent exists, its real decks are strictly better training data -- refitting
    the combat agent on them closes the distribution shift that opens up as
    soon as the run agent starts building decks of its own. Falls back to the
    progressive sampler when the harvest is empty rather than failing, so an
    alternation round can never silently train on nothing.
    """

    def __init__(self, character_id: str, deck_file: str,
                 mix_progressive: float = 0.3):
        import pickle

        from sts2_env.gym_env.rich_combat_env import ProgressiveDeckSampler

        self.fallback = ProgressiveDeckSampler(character_id)
        with open(deck_file, "rb") as fh:
            self.samples = pickle.load(fh)
        # Refitting PURELY on the current run agent's decks would overfit the
        # combat agent to whatever that agent happens to build right now --
        # and it demonstrably builds diluted decks (13.1 cards, 0.57
        # upgrades). Keeping a slice of synthetic decks preserves coverage of
        # the leaner, more upgraded builds a better run agent should reach.
        self.mix_progressive = float(mix_progressive)
        self._relic_cache: dict[str, object] = {}

    def __len__(self) -> int:
        return len(self.samples)

    def __call__(self, np_random):
        from sts2_env.cards.factory import create_card

        if not self.samples:
            return self.fallback(np_random)
        if self.mix_progressive > 0.0 and np_random.random() < self.mix_progressive:
            return self.fallback(np_random)
        card_ids, relic_ids, potion_ids, hp_frac = self.samples[
            int(np_random.integers(0, len(self.samples)))
        ]
        deck = []
        for cid, upgraded in card_ids:
            try:
                deck.append(create_card(cid, upgraded=bool(upgraded)))
            except Exception:
                continue
        if not deck:  # every card failed to materialise -- do not hand back an empty deck
            return self.fallback(np_random)
        relics = []
        from sts2_env.relics.registry import create_relic_by_name

        for rid in relic_ids:
            try:
                relics.append(create_relic_by_name(rid))
            except Exception:
                continue
        return deck, relics, [], float(hp_frac)


def make_combat_env(ascension: int = 0, seed: int = 0, pools=("act1",),
                    deck_file: str | None = None, mix_progressive: float = 0.3):
    import sts2_env.events  # noqa: F401
    from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

    sampler = (
        HarvestedDeckSampler("Necrobinder", deck_file, mix_progressive)
        if deck_file else "progressive"
    )
    env = RichSTS2CombatEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        encounter_pools=tuple(pools),
        deck_sampler=sampler,
    )
    env.reset(seed=seed)
    return env


def make_run_env(combat_model_path: str | None, ascension: int = 0,
                 max_act_count: int = 2, seed: int = 0, combat_device: str = "cpu"):
    """Hierarchical run env with the frozen combat agent loaded in-process.

    The model is loaded inside the factory so each subprocess worker owns its
    own copy -- an SB3 model is not picklable across the spawn boundary in a
    form that keeps torch out of the parent.
    """
    import sts2_env.events  # noqa: F401
    from sts2_env.gym_env.hierarchical_run_env import (
        HierarchicalRunEnv,
        PolicyCombatController,
        RandomCombatController,
    )

    controller = None
    if combat_model_path:
        model = load_shared_combat_model(combat_model_path, combat_device)
        controller = PolicyCombatController(model, deterministic=False)
    else:
        controller = RandomCombatController(seed=seed)

    env = HierarchicalRunEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        max_act_count=max_act_count,
        combat_controller=controller,
    )
    env.reset(seed=seed)
    return env


def make_vec(factory, n_envs: int, vec_mode: str = "auto"):
    """Vectorise, choosing the mode that fits in RAM.

    The run phase cannot use one-env-per-subprocess. Each worker must load its
    own combat model, and the torch runtime alone costs ~765MB resident --
    measured, not estimated -- so 16 workers would want ~12GB on a 15.7GB box
    that has ~5GB free. ``dummy`` keeps every env in one process sharing a
    single torch runtime and a single combat model, trading parallelism for a
    ~7x memory reduction. The combat phase has no such constraint and keeps
    using subprocesses.
    """
    from stable_baselines3.common.vec_env import DummyVecEnv, VecMonitor

    from slim_vecenv import SlimSubprocVecEnv as SubprocVecEnv

    factories = [partial(factory, seed=i * TRAIN_SEED_STRIDE) for i in range(n_envs)]
    if vec_mode == "dummy" or n_envs == 1:
        vec = DummyVecEnv(factories)
    else:
        vec = SubprocVecEnv(factories)
    return VecMonitor(vec)


_SHARED_COMBAT_MODEL: dict[str, Any] = {}


def load_shared_combat_model(path: str, device: str = "cpu"):
    """One combat model per PROCESS, reused by every env in it.

    With DummyVecEnv all envs live in the same process, so loading the model
    once instead of per-env saves ~130MB of parameters per additional env and
    keeps the inference weights in one place.
    """
    model = _SHARED_COMBAT_MODEL.get(path)
    if model is None:
        from sb3_contrib import MaskablePPO

        model = MaskablePPO.load(path, device=device)
        _SHARED_COMBAT_MODEL[path] = model
    return model


# ---------------------------------------------------------------------------
# Evaluation
# ---------------------------------------------------------------------------

def eval_run_agent(model, combat_model_path, n_episodes, ascension, max_act_count,
                   seed_block=EVAL_SEED_BLOCK):
    """Deterministic eval of the run agent. Reports the metrics the campaign
    tracks (floors, act, win) plus the deck statistics that diagnosed the
    plateau -- final deck size and upgrades are the whole point of this
    architecture, so they are first-class here."""
    env = make_run_env(combat_model_path, ascension=ascension,
                       max_act_count=max_act_count, seed=seed_block)
    env.set_shaping_scale(0.0)

    floors, acts, wins, decks, ups, decisions, trunc = [], [], [], [], [], [], 0
    t0 = time.time()
    for i in range(n_episodes):
        obs, info = env.reset(seed=seed_block + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 2000:
            mask = env.action_masks()
            a, _ = model.predict(obs, action_masks=mask, deterministic=True)
            obs, r, done, tr, info = env.step(int(a))
            n += 1
        floors.append(int(info.get("floor", 0)))
        acts.append(int(info.get("act", 0)))
        wins.append(bool(info.get("won", False)))
        decisions.append(int(info.get("run_decisions", n)))
        rs = env._mgr.run_state if env._mgr is not None else None
        if rs is not None:
            decks.append(len(rs.player.deck))
            ups.append(sum(1 for c in rs.player.deck if c.upgraded))
        if tr:
            trunc += 1
        if (i + 1) % 50 == 0:
            print(f"      eval {i+1}/{n_episodes}", flush=True)
    return {
        "mean_floors": float(np.mean(floors)),
        "mean_act": float(np.mean(acts)),
        "win_rate": float(np.mean(wins)),
        "truncation_rate": trunc / max(n_episodes, 1),
        "mean_deck": float(np.mean(decks)) if decks else 0.0,
        "mean_upgrades": float(np.mean(ups)) if ups else 0.0,
        "mean_decisions": float(np.mean(decisions)),
        "episodes": n_episodes,
        "wall_s": round(time.time() - t0, 1),
    }


def eval_combat_agent(model, n_episodes, ascension, pools, deck_file=None):
    env = make_combat_env(ascension=ascension, seed=EVAL_SEED_BLOCK, pools=pools,
                          deck_file=deck_file)
    env.set_shaping_scale(0.0)
    wins, hp_frac = [], []
    t0 = time.time()
    for i in range(n_episodes):
        obs, info = env.reset(seed=EVAL_SEED_BLOCK + i)
        done = tr = False
        n = 0
        while not (done or tr) and n < 1500:
            mask = env.action_masks()
            a, _ = model.predict(obs, action_masks=mask, deterministic=True)
            obs, r, done, tr, info = env.step(int(a))
            n += 1
        combat = env.combat
        p = combat.primary_player if combat is not None else None
        alive = bool(p is not None and p.is_alive)
        wins.append(alive)
        if p is not None and p.max_hp:
            hp_frac.append(max(0.0, p.current_hp) / p.max_hp)
    return {
        "win_rate": float(np.mean(wins)),
        "mean_hp_frac": float(np.mean(hp_frac)) if hp_frac else 0.0,
        "episodes": n_episodes,
        "wall_s": round(time.time() - t0, 1),
    }


# ---------------------------------------------------------------------------
# Callback
# ---------------------------------------------------------------------------

def build_callback(phase, out_dir, eval_freq, eval_episodes, eval_fn):
    from stable_baselines3.common.callbacks import BaseCallback

    class HierCallback(BaseCallback):
        def __init__(self):
            super().__init__(verbose=1)
            self.next_eval = eval_freq
            self.best = -np.inf
            self.hist = Path(out_dir) / "eval_history.jsonl"

        def _on_step(self) -> bool:
            t = self.num_timesteps
            if t >= self.next_eval:
                self.next_eval += eval_freq
                self._do_eval(t)
            return True

        def _do_eval(self, t):
            print(f"\n[{phase}] eval at {t:,} steps ...", flush=True)
            res = eval_fn(self.model, eval_episodes)
            res["steps"] = int(t)
            with self.hist.open("a", encoding="utf-8") as fh:
                fh.write(json.dumps(res) + "\n")
            print(f"[{phase}] {json.dumps(res)}", flush=True)
            key = res.get("mean_floors", res.get("win_rate", 0.0))
            if key > self.best:
                self.best = key
                self.model.save(str(Path(out_dir) / "best_model.zip"))
                print(f"[{phase}] new best ({key:.3f}) -> best_model.zip", flush=True)
            self.model.save(str(Path(out_dir) / f"ckpt_{t:010d}.zip"))

    return HierCallback()


# ---------------------------------------------------------------------------

def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--phase", choices=["combat", "run"], required=True)
    ap.add_argument("--total-steps", type=int, default=4_000_000)
    ap.add_argument("--n-envs", type=int, default=16)
    ap.add_argument("--eval-freq", type=int, default=500_000)
    ap.add_argument("--eval-episodes", type=int, default=200)
    ap.add_argument("--ascension", type=int, default=0)
    ap.add_argument("--max-act-count", type=int, default=2)
    ap.add_argument("--pools", nargs="*", default=["act1"])
    ap.add_argument("--deck-file", default=None,
                    help="Pickle of decks harvested from a run agent; replaces the "
                         "synthetic progressive sampler for --phase combat")
    ap.add_argument("--combat-model", default=None,
                    help="Frozen combat agent for --phase run")
    ap.add_argument("--gamma", type=float, default=0.99)
    ap.add_argument("--lr", type=float, default=3e-4)
    ap.add_argument("--ent-coef", type=float, default=0.01)
    ap.add_argument("--target-kl", type=float, default=0.03)
    ap.add_argument("--n-steps", type=int, default=1024)
    ap.add_argument("--batch-size", type=int, default=4096)
    ap.add_argument("--seed", type=int, default=0)
    ap.add_argument("--output-dir", default=None)
    ap.add_argument("--tensorboard", action="store_true")
    ap.add_argument("--device", default="cuda")
    ap.add_argument("--combat-device", default="cpu",
                    help="Device for the frozen combat model during --phase run")
    ap.add_argument("--vec-mode", choices=["auto", "dummy", "subproc"],
                    default="auto",
                    help="auto: subproc for combat, dummy for run (see make_vec)")
    args = ap.parse_args()

    out_dir = Path(args.output_dir or f"output/hier/{args.phase}")
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.phase == "run" and not args.combat_model:
        print("WARNING: --phase run without --combat-model: combats will be played "
              "by a RANDOM controller. Useful only for plumbing checks.", flush=True)

    from sb3_contrib import MaskablePPO

    from sts2_env.train.policy import rich_policy_kwargs

    if args.phase == "combat":
        factory = partial(make_combat_env, args.ascension, pools=tuple(args.pools),
                          deck_file=args.deck_file)
        eval_fn = lambda m, n: eval_combat_agent(m, n, args.ascension, tuple(args.pools),
                                                 deck_file=args.deck_file)
    else:
        factory = partial(make_run_env, args.combat_model, args.ascension,
                          args.max_act_count, combat_device=args.combat_device)
        eval_fn = lambda m, n: eval_run_agent(
            m, args.combat_model, n, args.ascension, args.max_act_count)

    vec_mode = args.vec_mode
    if vec_mode == "auto":
        # The run phase loads a torch combat model per process; see make_vec.
        vec_mode = "dummy" if args.phase == "run" else "subproc"
    print(f"[{args.phase}] vec mode: {vec_mode} ({args.n_envs} envs)", flush=True)
    train_env = make_vec(factory, args.n_envs, vec_mode)

    model = MaskablePPO(
        "MlpPolicy",
        train_env,
        learning_rate=args.lr,
        n_steps=args.n_steps,
        batch_size=args.batch_size,
        n_epochs=3,
        gamma=args.gamma,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=args.ent_coef,
        vf_coef=0.5,
        target_kl=args.target_kl,
        policy_kwargs=rich_policy_kwargs(hand_encoding="perslot"),
        seed=args.seed,
        device=args.device,
        verbose=1,
        tensorboard_log=str(out_dir / "tb") if args.tensorboard else None,
    )

    print(f"[{args.phase}] training {args.total_steps:,} steps, {args.n_envs} envs, "
          f"gamma {args.gamma}, device {args.device}", flush=True)
    cb = build_callback(args.phase, out_dir, args.eval_freq, args.eval_episodes, eval_fn)
    model.learn(total_timesteps=args.total_steps, callback=cb, progress_bar=False)
    model.save(str(out_dir / "final_model.zip"))
    print(f"[{args.phase}] done -> {out_dir}", flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
