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

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))

EVAL_SEED_BLOCK = 10_000_000
TRAIN_SEED_STRIDE = 1_000


# ---------------------------------------------------------------------------
# Env factories
# ---------------------------------------------------------------------------

def make_combat_env(ascension: int = 0, seed: int = 0, pools=("act1",)):
    import sts2_env.events  # noqa: F401
    from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

    env = RichSTS2CombatEnv(
        character_id="Necrobinder",
        ascension_level=ascension,
        encounter_pools=tuple(pools),
        deck_sampler="progressive",
    )
    env.reset(seed=seed)
    return env


def make_run_env(combat_model_path: str | None, ascension: int = 0,
                 max_act_count: int = 2, seed: int = 0, gamma_note: str = ""):
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
        from sb3_contrib import MaskablePPO

        model = MaskablePPO.load(combat_model_path, device="cpu")
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


def make_vec(factory, n_envs: int):
    from stable_baselines3.common.vec_env import DummyVecEnv, VecMonitor

    from slim_vecenv import SlimSubprocVecEnv as SubprocVecEnv

    factories = [partial(factory, seed=i * TRAIN_SEED_STRIDE) for i in range(n_envs)]
    vec = SubprocVecEnv(factories) if n_envs > 1 else DummyVecEnv(factories)
    return VecMonitor(vec)


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


def eval_combat_agent(model, n_episodes, ascension, pools):
    env = make_combat_env(ascension=ascension, seed=EVAL_SEED_BLOCK, pools=pools)
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
    args = ap.parse_args()

    out_dir = Path(args.output_dir or f"output/hier/{args.phase}")
    out_dir.mkdir(parents=True, exist_ok=True)

    if args.phase == "run" and not args.combat_model:
        print("WARNING: --phase run without --combat-model: combats will be played "
              "by a RANDOM controller. Useful only for plumbing checks.", flush=True)

    from sb3_contrib import MaskablePPO

    from sts2_env.train.policy import rich_policy_kwargs

    if args.phase == "combat":
        factory = partial(make_combat_env, args.ascension, pools=tuple(args.pools))
        eval_fn = lambda m, n: eval_combat_agent(m, n, args.ascension, tuple(args.pools))
    else:
        factory = partial(make_run_env, args.combat_model, args.ascension,
                          args.max_act_count)
        eval_fn = lambda m, n: eval_run_agent(
            m, args.combat_model, n, args.ascension, args.max_act_count)

    train_env = make_vec(factory, args.n_envs)

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
