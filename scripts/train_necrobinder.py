"""Necrobinder Ascension-10 curriculum trainer (docs/TRAINING_REDESIGN.md).

Single entrypoint for the staged curriculum:

=====  =============================================================  ==========
Stage  Config                                                         Promotion
=====  =============================================================  ==========
A      Combat-only, Act 1 encounters, starter deck, A10               >85% win
B      Combat-only, mixed-act pools (incl. legacy + Heart), sampled   >75% win
       decks
C      Full run, Act 1 only, A10                                      >80% win
D      Full run, Acts 1-2                                             >70% win
E      Full run, Acts 1-3                                             >60% win
F      Full run, Acts 1-4 (Heart), shaping annealed                   95% target
=====  =============================================================  ==========

Usage
-----
    python scripts/train_necrobinder.py --stage A --total-steps 2000000
    python scripts/train_necrobinder.py --auto            # run the ladder
    python scripts/train_necrobinder.py --stage C --resume

Checkpoints: ``output/necrobinder_a10/<stage>/ckpt_<steps>.zip`` +
sidecar JSON (stage, steps, shaping_scale, eval history, promotion state)
every 250k steps and on eval improvement (``best_model.zip``).
Eval: every 100k steps, 200 episodes, shaping_scale=0, seed block
10_000_000+, deterministic. History in ``eval_history.jsonl``.
Shaping anneal: ``shaping_scale = max(0, 1 - win_rate * 1.25)`` after each
eval, pushed into every training env.
"""

from __future__ import annotations

import argparse
import json
import time
from dataclasses import dataclass
from functools import partial
from pathlib import Path

import numpy as np

# ---------------------------------------------------------------------------
# Stage table
# ---------------------------------------------------------------------------

@dataclass(frozen=True)
class StageConfig:
    name: str
    env_kind: str                      # "combat" | "run"
    encounter_pools: tuple[str, ...]   # combat env only
    deck_sampler: str                  # combat env only
    max_act_count: int                 # run env only
    promotion_win_rate: float


STAGES: dict[str, StageConfig] = {
    "A": StageConfig("A", "combat", ("act1",), "starter", 1, 0.85),
    "B": StageConfig(
        "B", "combat",
        # thebeyond is import-guarded in resolve_encounter_pool and joins
        # automatically once the module lands.
        ("act1", "act2", "act3", "act4heart", "exordium", "thecity", "thebeyond"),
        "progressive", 1, 0.75,
    ),
    "C": StageConfig("C", "run", (), "starter", 1, 0.80),
    "D": StageConfig("D", "run", (), "starter", 2, 0.70),
    "E": StageConfig("E", "run", (), "starter", 3, 0.60),
    "F": StageConfig("F", "run", (), "starter", 4, 0.95),
}
STAGE_ORDER = "ABCDEF"

DEFAULT_OUTPUT_ROOT = "output/necrobinder_a10"
EVAL_FREQ = 100_000
CHECKPOINT_FREQ = 250_000
EVAL_EPISODES = 200
EVAL_SEED_BLOCK = 10_000_000
ENT_COEF_START = 0.01
ENT_COEF_END = 0.003
LR_START = 2.5e-4


# ---------------------------------------------------------------------------
# Env factories (module-level so SubprocVecEnv can pickle them on Windows)
# ---------------------------------------------------------------------------

def make_stage_env(stage_name: str, shaping_scale: float = 1.0, max_steps: int = 10_000):
    """Create one (unwrapped) env for a stage. Picklable via functools.partial."""
    from sts2_env.gym_env.reward_config import RewardConfig

    stage = STAGES[stage_name]
    cfg = RewardConfig(shaping_scale=shaping_scale)
    if stage.env_kind == "combat":
        from sts2_env.gym_env.rich_combat_env import RichSTS2CombatEnv

        return RichSTS2CombatEnv(
            character_id="Necrobinder",
            ascension_level=10,
            encounter_pools=stage.encounter_pools,
            deck_sampler=stage.deck_sampler,
            reward_config=cfg,
            max_episode_steps=1000,
        )
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    return RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=10,
        max_act_count=stage.max_act_count,
        reward_config=cfg,
        max_steps=max_steps,
    )


# ---------------------------------------------------------------------------
# Evaluation
# ---------------------------------------------------------------------------

def run_eval(model, stage_name: str, n_episodes: int = EVAL_EPISODES,
             seed_block: int = EVAL_SEED_BLOCK, max_episode_steps: int = 10_000) -> dict:
    """Evaluate on a dedicated env with shaping_scale=0 (pure sparse reward)."""
    env = make_stage_env(stage_name, shaping_scale=0.0)
    wins = 0
    floors: list[int] = []
    deaths_by_act: dict[int, int] = {}
    for ep in range(n_episodes):
        obs, info = env.reset(seed=seed_block + ep)
        done = False
        steps = 0
        while not done and steps < max_episode_steps:
            masks = env.action_masks()
            action, _ = model.predict(obs, action_masks=masks, deterministic=True)
            obs, reward, terminated, truncated, info = env.step(int(action))
            done = terminated or truncated
            steps += 1
        won = bool(info.get("won", False))
        wins += won
        floors.append(int(info.get("floor", 0)))
        if not won:
            act = int(info.get("act", 0))
            deaths_by_act[act] = deaths_by_act.get(act, 0) + 1
    return {
        "win_rate": wins / n_episodes,
        "episodes": n_episodes,
        "mean_floors": float(np.mean(floors)) if floors else 0.0,
        "deaths_by_act": deaths_by_act,
    }


# ---------------------------------------------------------------------------
# Combined eval / checkpoint / anneal / promotion callback
# ---------------------------------------------------------------------------

def build_callback_class():
    """Defer SB3 imports until needed."""
    from stable_baselines3.common.callbacks import BaseCallback

    class CurriculumCallback(BaseCallback):
        def __init__(
            self,
            stage_name: str,
            stage_dir: Path,
            state: dict,
            eval_freq: int = EVAL_FREQ,
            checkpoint_freq: int = CHECKPOINT_FREQ,
            eval_episodes: int = EVAL_EPISODES,
            stop_on_promotion: bool = False,
            verbose: int = 1,
        ):
            super().__init__(verbose)
            self.stage_name = stage_name
            self.stage = STAGES[stage_name]
            self.stage_dir = stage_dir
            self.state = state
            self.eval_freq = eval_freq
            self.checkpoint_freq = checkpoint_freq
            self.eval_episodes = eval_episodes
            self.stop_on_promotion = stop_on_promotion
            self._last_eval = state.get("last_eval_step", 0)
            self._last_ckpt = state.get("last_ckpt_step", 0)

        # -- entropy anneal: 0.01 -> 0.003 over training progress --
        def _on_rollout_start(self) -> None:
            progress_remaining = self.model._current_progress_remaining
            self.model.ent_coef = ENT_COEF_END + (ENT_COEF_START - ENT_COEF_END) * progress_remaining

        def _on_step(self) -> bool:
            t = self.num_timesteps
            continue_training = True
            if t - self._last_eval >= self.eval_freq:
                self._last_eval = t
                continue_training = self._do_eval(t)
            if t - self._last_ckpt >= self.checkpoint_freq:
                self._last_ckpt = t
                self._save_checkpoint(t)
            return continue_training

        def _do_eval(self, t: int) -> bool:
            if self.verbose:
                print(f"\n[eval] stage {self.stage_name} @ {t:,} steps "
                      f"({self.eval_episodes} episodes, shaping=0) ...")
            start = time.perf_counter()
            metrics = run_eval(self.model, self.stage_name, self.eval_episodes)
            metrics.update({"steps": t, "wall_s": round(time.perf_counter() - start, 1)})
            win_rate = metrics["win_rate"]

            # shaping anneal, pushed to every training env
            shaping = max(0.0, 1.0 - win_rate * 1.25)
            self.state["shaping_scale"] = shaping
            metrics["shaping_scale"] = shaping
            try:
                self.model.get_env().env_method("set_shaping_scale", shaping)
            except Exception as exc:  # pragma: no cover
                print(f"[warn] could not push shaping_scale: {exc}")

            self.state.setdefault("eval_history", []).append(metrics)
            self.state["last_eval_step"] = t
            with open(self.stage_dir / "eval_history.jsonl", "a", encoding="utf-8") as f:
                f.write(json.dumps(metrics) + "\n")
            if self.verbose:
                print(f"[eval] win_rate={win_rate:.1%} mean_floors={metrics['mean_floors']:.1f} "
                      f"deaths_by_act={metrics['deaths_by_act']} shaping={shaping:.3f} "
                      f"({metrics['wall_s']}s)")

            # best-model checkpoint on improvement
            if win_rate > self.state.get("best_win_rate", -1.0):
                self.state["best_win_rate"] = win_rate
                self.model.save(str(self.stage_dir / "best_model"))
                self._write_sidecar(self.stage_dir / "best_model.json", t)
                if self.verbose:
                    print(f"[eval] new best ({win_rate:.1%}) -> best_model.zip")

            # promotion: threshold hit on 2 consecutive evals
            if win_rate >= self.stage.promotion_win_rate:
                self.state["promotion_streak"] = self.state.get("promotion_streak", 0) + 1
            else:
                self.state["promotion_streak"] = 0
            if self.state["promotion_streak"] >= 2:
                self.state["promoted"] = True
                print(f"[promotion] stage {self.stage_name} criterion "
                      f"(>{self.stage.promotion_win_rate:.0%} x2) met at {t:,} steps")
                if self.stop_on_promotion:
                    return False
            return True

        def _save_checkpoint(self, t: int) -> None:
            path = self.stage_dir / f"ckpt_{t:010d}"
            self.model.save(str(path))
            self.state["last_ckpt_step"] = t
            self._write_sidecar(path.with_suffix(".json"), t)
            if self.verbose:
                print(f"[ckpt] saved {path}.zip")

        def _write_sidecar(self, path: Path, t: int) -> None:
            sidecar = {
                "stage": self.stage_name,
                "steps": t,
                "shaping_scale": self.state.get("shaping_scale", 1.0),
                "best_win_rate": self.state.get("best_win_rate", -1.0),
                "promotion_streak": self.state.get("promotion_streak", 0),
                "promoted": self.state.get("promoted", False),
                "last_eval_step": self.state.get("last_eval_step", 0),
                "last_ckpt_step": self.state.get("last_ckpt_step", 0),
                "eval_history": self.state.get("eval_history", []),
            }
            path.write_text(json.dumps(sidecar, indent=2), encoding="utf-8")

    return CurriculumCallback


# ---------------------------------------------------------------------------
# Model construction / resume / warm-start
# ---------------------------------------------------------------------------

def linear_lr(progress_remaining: float) -> float:
    return LR_START * progress_remaining


def build_model(train_env, tensorboard_dir: str | None):
    from sb3_contrib import MaskablePPO

    from sts2_env.train.policy import rich_policy_kwargs

    return MaskablePPO(
        "MlpPolicy",
        train_env,
        learning_rate=linear_lr,
        n_steps=512,
        batch_size=4096,
        n_epochs=4,
        gamma=0.999,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=ENT_COEF_START,
        vf_coef=0.5,
        policy_kwargs=rich_policy_kwargs(),
        device="cuda",
        verbose=1,
        tensorboard_log=tensorboard_dir,
    )


def find_latest_checkpoint(stage_dir: Path) -> tuple[Path, dict] | None:
    """Latest (model_path_without_ext, sidecar) in a stage dir, or None."""
    candidates = sorted(stage_dir.glob("ckpt_*.zip"))
    best = stage_dir / "best_model.zip"
    latest: Path | None = candidates[-1] if candidates else None
    if latest is None and best.exists():
        latest = best
    if latest is None:
        return None
    sidecar_path = latest.with_suffix(".json")
    sidecar = json.loads(sidecar_path.read_text(encoding="utf-8")) if sidecar_path.exists() else {}
    return latest.with_suffix(""), sidecar


def make_vec_env(stage_name: str, n_envs: int, shaping_scale: float):
    from stable_baselines3.common.vec_env import DummyVecEnv, SubprocVecEnv, VecMonitor

    factories = [partial(make_stage_env, stage_name, shaping_scale) for _ in range(n_envs)]
    vec = SubprocVecEnv(factories) if n_envs > 1 else DummyVecEnv(factories)
    return VecMonitor(vec)


# ---------------------------------------------------------------------------
# Stage runner
# ---------------------------------------------------------------------------

def train_stage(
    stage_name: str,
    args,
    warm_start_from: Path | None = None,
    stop_on_promotion: bool = False,
) -> dict:
    from sb3_contrib import MaskablePPO

    stage_dir = Path(args.output_dir) / stage_name
    stage_dir.mkdir(parents=True, exist_ok=True)
    tb_dir = str(stage_dir / "tb") if args.tensorboard else None

    state: dict = {"shaping_scale": 1.0}
    resume_from: Path | None = None
    if args.resume:
        found = find_latest_checkpoint(stage_dir)
        if found is not None:
            resume_from, sidecar = found
            state.update(sidecar)
            print(f"[resume] stage {stage_name}: {resume_from}.zip "
                  f"(steps={state.get('steps', '?')}, shaping={state['shaping_scale']:.3f})")
        else:
            print(f"[resume] no checkpoint in {stage_dir}; starting fresh")

    train_env = make_vec_env(stage_name, args.n_envs, state["shaping_scale"])

    if resume_from is not None:
        model = MaskablePPO.load(
            str(resume_from), env=train_env, device="cuda", tensorboard_log=tb_dir,
            custom_objects={"learning_rate": linear_lr},
        )
        reset_num_timesteps = False
    else:
        model = build_model(train_env, tb_dir)
        reset_num_timesteps = True
        if warm_start_from is not None:
            from sts2_env.train.policy import transfer_weights

            print(f"[warm-start] loading weights from {warm_start_from}.zip")
            src = MaskablePPO.load(str(warm_start_from), device="cuda")
            transfer_weights(src, model)
            del src

    CurriculumCallback = build_callback_class()
    callback = CurriculumCallback(
        stage_name=stage_name,
        stage_dir=stage_dir,
        state=state,
        eval_freq=args.eval_freq,
        checkpoint_freq=args.checkpoint_freq,
        eval_episodes=args.eval_episodes,
        stop_on_promotion=stop_on_promotion,
    )

    print(f"\n=== Stage {stage_name}: {STAGES[stage_name]} ===")
    start = time.perf_counter()
    model.learn(
        total_timesteps=args.total_steps,
        callback=callback,
        reset_num_timesteps=reset_num_timesteps,
        progress_bar=args.progress,
    )
    elapsed = time.perf_counter() - start

    final_path = stage_dir / f"ckpt_{model.num_timesteps:010d}"
    model.save(str(final_path))
    callback._write_sidecar(final_path.with_suffix(".json"), model.num_timesteps)
    print(f"[stage {stage_name}] done in {elapsed/60:.1f} min "
          f"({model.num_timesteps:,} steps, best win rate "
          f"{state.get('best_win_rate', 0):.1%})")
    train_env.close()
    return state


def main():
    parser = argparse.ArgumentParser(description="Necrobinder A10 curriculum trainer")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--stage", choices=list(STAGE_ORDER), help="Train one stage")
    group.add_argument("--auto", action="store_true",
                       help="Run the full ladder with promotion criteria")
    parser.add_argument("--start-stage", choices=list(STAGE_ORDER), default="A",
                        help="First stage for --auto (default A)")
    parser.add_argument("--n-envs", type=int, default=16)
    parser.add_argument("--total-steps", type=int, default=5_000_000,
                        help="Timestep budget per stage (default 5M)")
    parser.add_argument("--eval-freq", type=int, default=EVAL_FREQ)
    parser.add_argument("--eval-episodes", type=int, default=EVAL_EPISODES)
    parser.add_argument("--checkpoint-freq", type=int, default=CHECKPOINT_FREQ)
    parser.add_argument("--resume", action="store_true",
                        help="Resume from the latest checkpoint of the stage")
    parser.add_argument("--tensorboard", action="store_true")
    parser.add_argument("--progress", action="store_true", help="Show progress bar")
    parser.add_argument("--output-dir", type=str, default=DEFAULT_OUTPUT_ROOT)
    args = parser.parse_args()

    if args.stage:
        train_stage(args.stage, args)
        return

    # --auto: ladder with promotion + warm starts
    start_idx = STAGE_ORDER.index(args.start_stage)
    prev_best: Path | None = None
    for stage_name in STAGE_ORDER[start_idx:]:
        state = train_stage(
            stage_name, args,
            warm_start_from=prev_best,
            stop_on_promotion=(stage_name != "F"),
        )
        best = Path(args.output_dir) / stage_name / "best_model"
        if best.with_suffix(".zip").exists():
            prev_best = best
        if not state.get("promoted") and stage_name != "F":
            print(f"[auto] stage {stage_name} exhausted its budget without meeting "
                  f"the promotion criterion; continuing to the next stage anyway "
                  f"is unsafe -- stopping. Re-run with --resume or a larger "
                  f"--total-steps.")
            break


if __name__ == "__main__":
    main()
