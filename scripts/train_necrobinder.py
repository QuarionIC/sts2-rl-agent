"""Necrobinder full-run curriculum trainer (docs/TRAINING_REVAMP_SPEC.json).

Single entrypoint for the full-run-only ladder. Every stage trains the SAME
RichSTS2RunEnv task; difficulty moves along two axes only (ascension level,
act count). There is no combat-only stage and the ladder never halts:
promotion thresholds are telemetry, not gates -- each stage always trains to
its step budget.

=====  ==========  =========  ==========  ==============
Stage  Ascension   Acts       Budget      Gate (telemetry)
=====  ==========  =========  ==========  ==============
G1     0           1-2        20M         0.80
G2     0           1-4        30M         0.55
G3     4           1-4        40M         0.50
G4     8           1-4        50M         0.45
G5     10          1-4        60M         0.45 (final)
=====  ==========  =========  ==========  ==============

Usage
-----
    python scripts/train_necrobinder.py --stage G1 --total-steps 20000000
    python scripts/train_necrobinder.py --auto            # run the ladder
    python scripts/train_necrobinder.py --stage G1 --resume

Checkpoints: ``<output-dir>/<stage>/ckpt_<steps>.zip`` + sidecar
JSON (stage, steps, eval history, promotion state) every 250k steps and on
eval improvement (``best_model.zip``).
Eval: every 100k steps, deterministic, shaping_scale=0, seed block
10_000_000+. History in ``eval_history.jsonl``.
Optimizer stays alive: constant lr=2e-4 with target_kl=0.03, constant
ent_coef=0.01. Reward shaping is a fixed scale (1.0) -- no win-rate anneal.
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
# Stage table (full-run only; promotion is telemetry, never a gate)
# ---------------------------------------------------------------------------

@dataclass(frozen=True)
class StageConfig:
    name: str
    ascension_level: int
    max_act_count: int                 # 1-4; 4 includes the Act 4 Heart
    promotion_win_rate: float          # telemetry threshold, never halts
    budget: int                        # default per-stage timestep budget


STAGES: dict[str, StageConfig] = {
    "G1": StageConfig("G1", 0, 2, 0.80, 20_000_000),
    "G2": StageConfig("G2", 0, 4, 0.55, 30_000_000),
    "G3": StageConfig("G3", 4, 4, 0.50, 40_000_000),
    "G4": StageConfig("G4", 8, 4, 0.45, 50_000_000),
    "G5": StageConfig("G5", 10, 4, 0.45, 60_000_000),
}
STAGE_ORDER = ["G1", "G2", "G3", "G4", "G5"]

DEFAULT_OUTPUT_ROOT = "output/necrobinder_run"
EVAL_FREQ = 100_000
CHECKPOINT_FREQ = 250_000
EVAL_EPISODES = 200
EVAL_SEED_BLOCK = 10_000_000
ENT_COEF = 0.01
LEARNING_RATE = 2.0e-4
TARGET_KL = 0.03
RUN_MAX_STEPS = 3_000


# ---------------------------------------------------------------------------
# Env factories (module-level so SubprocVecEnv can pickle them on Windows)
# ---------------------------------------------------------------------------

def make_stage_env(stage_name: str, shaping_scale: float = 1.0,
                   max_steps: int = RUN_MAX_STEPS):
    """Create one (unwrapped) run env for a stage. Picklable via partial."""
    from sts2_env.gym_env.reward_config import RewardConfig
    from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv

    stage = STAGES[stage_name]
    cfg = RewardConfig(shaping_scale=shaping_scale)
    return RichSTS2RunEnv(
        character_id="Necrobinder",
        ascension_level=stage.ascension_level,
        max_act_count=stage.max_act_count,
        reward_config=cfg,
        max_steps=max_steps,
    )


# ---------------------------------------------------------------------------
# Evaluation
# ---------------------------------------------------------------------------

def run_eval(model, stage_name: str, n_episodes: int = EVAL_EPISODES,
             seed_block: int = EVAL_SEED_BLOCK, max_episode_steps: int = RUN_MAX_STEPS) -> dict:
    """Evaluate on a dedicated env with shaping_scale=0 (pure sparse reward).

    The run env's info dict carries ``floor``/``act`` every step, so
    mean_floors and deaths_by_act are real telemetry (the old combat-env
    eval was blind to both).
    """
    env = make_stage_env(stage_name, shaping_scale=0.0)
    wins = 0
    truncations = 0
    floors: list[int] = []
    acts: list[int] = []
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
        truncations += bool(info.get("truncated", False))
        floors.append(int(info.get("floor", 0)))
        acts.append(int(info.get("act", 0)))
        if not won:
            act = int(info.get("act", 0))
            deaths_by_act[act] = deaths_by_act.get(act, 0) + 1
    return {
        "win_rate": wins / n_episodes,
        "episodes": n_episodes,
        "mean_floors": float(np.mean(floors)) if floors else 0.0,
        "mean_act": float(np.mean(acts)) if acts else 0.0,
        "truncation_rate": truncations / n_episodes,
        "deaths_by_act": deaths_by_act,
    }


# ---------------------------------------------------------------------------
# Combined eval / checkpoint / promotion-telemetry callback
# ---------------------------------------------------------------------------

def build_callback_class():
    """Defer SB3 imports until needed."""
    from stable_baselines3.common.callbacks import BaseCallback

    class CurriculumCallback(BaseCallback):
        """Eval + checkpoint callback. NEVER stops training: promotion is
        recorded as telemetry only and every stage runs to its budget."""

        def __init__(
            self,
            stage_name: str,
            stage_dir: Path,
            state: dict,
            eval_freq: int = EVAL_FREQ,
            checkpoint_freq: int = CHECKPOINT_FREQ,
            eval_episodes: int = EVAL_EPISODES,
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
            self._last_eval = state.get("last_eval_step", 0)
            self._last_ckpt = state.get("last_ckpt_step", 0)

        def _on_step(self) -> bool:
            t = self.num_timesteps
            if t - self._last_eval >= self.eval_freq:
                self._last_eval = t
                self._do_eval(t)
            if t - self._last_ckpt >= self.checkpoint_freq:
                self._last_ckpt = t
                self._save_checkpoint(t)
            return True

        def _do_eval(self, t: int) -> None:
            if self.verbose:
                print(f"\n[eval] stage {self.stage_name} @ {t:,} steps "
                      f"({self.eval_episodes} episodes, shaping=0) ...")
            start = time.perf_counter()
            metrics = run_eval(self.model, self.stage_name, self.eval_episodes)
            metrics.update({"steps": t, "wall_s": round(time.perf_counter() - start, 1)})
            win_rate = metrics["win_rate"]

            self.state.setdefault("eval_history", []).append(metrics)
            self.state["last_eval_step"] = t
            with open(self.stage_dir / "eval_history.jsonl", "a", encoding="utf-8") as f:
                f.write(json.dumps(metrics) + "\n")
            if self.verbose:
                print(f"[eval] win_rate={win_rate:.1%} mean_floors={metrics['mean_floors']:.1f} "
                      f"mean_act={metrics['mean_act']:.2f} "
                      f"deaths_by_act={metrics['deaths_by_act']} "
                      f"trunc={metrics['truncation_rate']:.1%} ({metrics['wall_s']}s)")

            # best-model checkpoint on improvement
            if win_rate > self.state.get("best_win_rate", -1.0):
                self.state["best_win_rate"] = win_rate
                self.model.save(str(self.stage_dir / "best_model"))
                self._write_sidecar(self.stage_dir / "best_model.json", t)
                if self.verbose:
                    print(f"[eval] new best ({win_rate:.1%}) -> best_model.zip")

            # promotion telemetry: threshold hit on 2 consecutive evals.
            # Never stops training -- the stage always runs to budget.
            if win_rate >= self.stage.promotion_win_rate:
                self.state["promotion_streak"] = self.state.get("promotion_streak", 0) + 1
            else:
                self.state["promotion_streak"] = 0
            if self.state["promotion_streak"] >= 2 and not self.state.get("promoted"):
                self.state["promoted"] = True
                print(f"[promotion] stage {self.stage_name} telemetry criterion "
                      f"(>{self.stage.promotion_win_rate:.0%} x2) met at {t:,} steps "
                      f"(training continues to budget)")

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

def build_model(train_env, tensorboard_dir: str | None):
    from sb3_contrib import MaskablePPO

    from sts2_env.train.policy import rich_policy_kwargs

    return MaskablePPO(
        "MlpPolicy",
        train_env,
        learning_rate=LEARNING_RATE,   # constant; target_kl regulates step size
        n_steps=1024,
        batch_size=4096,
        n_epochs=3,
        gamma=0.997,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=ENT_COEF,             # constant floor; no anneal
        vf_coef=0.5,
        target_kl=TARGET_KL,
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


def make_vec_env(stage_name: str, n_envs: int):
    from stable_baselines3.common.vec_env import DummyVecEnv, SubprocVecEnv, VecMonitor

    factories = [partial(make_stage_env, stage_name) for _ in range(n_envs)]
    vec = SubprocVecEnv(factories) if n_envs > 1 else DummyVecEnv(factories)
    return VecMonitor(vec)


# ---------------------------------------------------------------------------
# Stage runner
# ---------------------------------------------------------------------------

def train_stage(
    stage_name: str,
    args,
    warm_start_from: Path | None = None,
) -> dict:
    from sb3_contrib import MaskablePPO

    stage_dir = Path(args.output_dir) / stage_name
    stage_dir.mkdir(parents=True, exist_ok=True)
    tb_dir = str(stage_dir / "tb") if args.tensorboard else None
    total_steps = args.total_steps or STAGES[stage_name].budget

    state: dict = {}
    resume_from: Path | None = None
    if args.resume:
        found = find_latest_checkpoint(stage_dir)
        if found is not None:
            resume_from, sidecar = found
            state.update(sidecar)
            print(f"[resume] stage {stage_name}: {resume_from}.zip "
                  f"(steps={state.get('steps', '?')})")
        else:
            print(f"[resume] no checkpoint in {stage_dir}; starting fresh")

    train_env = make_vec_env(stage_name, args.n_envs)

    if resume_from is not None:
        model = MaskablePPO.load(
            str(resume_from), env=train_env, device="cuda", tensorboard_log=tb_dir,
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
    )

    print(f"\n=== Stage {stage_name}: {STAGES[stage_name]} "
          f"({total_steps:,} steps) ===")
    start = time.perf_counter()
    model.learn(
        total_timesteps=total_steps,
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
    parser = argparse.ArgumentParser(description="Necrobinder full-run curriculum trainer")
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--stage", choices=STAGE_ORDER, help="Train one stage")
    group.add_argument("--auto", action="store_true",
                       help="Run the full ladder G1->G5 (each stage to budget)")
    parser.add_argument("--start-stage", choices=STAGE_ORDER, default="G1",
                        help="First stage for --auto (default G1)")
    parser.add_argument("--n-envs", type=int, default=24)
    parser.add_argument("--total-steps", type=int, default=None,
                        help="Timestep budget per stage (default: the stage's budget)")
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

    # --auto: the ladder NEVER halts. Every stage trains to its budget and
    # warm-starts the next from its best model (promotion is telemetry only).
    start_idx = STAGE_ORDER.index(args.start_stage)
    prev_best: Path | None = None
    for stage_name in STAGE_ORDER[start_idx:]:
        train_stage(stage_name, args, warm_start_from=prev_best)
        best = Path(args.output_dir) / stage_name / "best_model"
        if best.with_suffix(".zip").exists():
            prev_best = best


if __name__ == "__main__":
    main()
