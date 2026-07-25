"""Tests for reproducibility seeding (``train_necrobinder.py --seed``) and the
multi-seed confirmation harness (``scripts/confirm_phase.py``).

Three properties are load-bearing and each is tested directly:

1. ``--seed`` fixes everything that decides a run: python/numpy/torch RNGs
   (hence the initial policy tensors), the SB3 model seed, and the TRAINING
   vec-env seeds -- which occupy a disjoint block per seed, so two seeds are
   genuinely independent runs rather than 15/16 shared env streams.
2. ``--seed`` NEVER reaches the eval env: eval episode e always runs
   ``EVAL_SEED_BLOCK + e`` so every seed is scored on the same held-out runs.
3. The confirmation statistics (least-squares slope, mean/SE, Welch t) are
   correct on synthetic data with known answers, and the decision rule only
   declares a winner when the mean slope gap exceeds the pooled SE.

The trainer's real argparse is exercised through ``main()`` with a stubbed
``train_stage``, which also pins the flag set the LIVE ablation harness
passes -- an incompatible trainer edit would fail here.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path
from types import SimpleNamespace

import numpy as np
import pytest

REPO = Path(__file__).resolve().parent.parent
SCRIPTS = REPO / "scripts"
if str(SCRIPTS) not in sys.path:
    sys.path.insert(0, str(SCRIPTS))

import ablation_study as ab  # noqa: E402
import confirm_phase as cp  # noqa: E402
import train_necrobinder as tn  # noqa: E402


# ---------------------------------------------------------------------------
# Seed block derivation
# ---------------------------------------------------------------------------

class TestSeedBlocks:
    def test_stride_and_disjointness(self):
        assert tn.TRAIN_SEED_STRIDE == 1_000
        n_envs = 24                      # the trainer's default, > ablation's 16
        blocks = []
        for seed in range(6):
            base = tn.train_env_seed_base(seed)
            assert base == seed * tn.TRAIN_SEED_STRIDE
            blocks.append(set(range(base, base + n_envs)))
        for i in range(len(blocks)):
            for j in range(i + 1, len(blocks)):
                assert not blocks[i] & blocks[j], "seed blocks overlap"

    def test_naive_seed_plus_index_would_overlap(self):
        """Why the stride exists: SB3's own seed+idx spacing shares almost
        every env seed between consecutive --seed values."""
        naive0 = set(range(0, 16))
        naive1 = set(range(1, 17))
        assert len(naive0 & naive1) == 15
        strided0 = set(range(tn.train_env_seed_base(0), tn.train_env_seed_base(0) + 16))
        strided1 = set(range(tn.train_env_seed_base(1), tn.train_env_seed_base(1) + 16))
        assert not strided0 & strided1

    def test_never_reaches_eval_block(self):
        limit = tn.EVAL_SEED_BLOCK // tn.TRAIN_SEED_STRIDE
        assert tn.train_env_seed_base(limit - 1) + 1_000 <= tn.EVAL_SEED_BLOCK
        with pytest.raises(ValueError, match="eval block"):
            tn.train_env_seed_base(limit)
        with pytest.raises(ValueError, match=">= 0"):
            tn.train_env_seed_base(-1)

    def test_seed_everything_is_reproducible(self):
        import random

        tn.seed_everything(1234)
        a = (random.random(), float(np.random.rand()))
        tn.seed_everything(1234)
        b = (random.random(), float(np.random.rand()))
        assert a == b
        tn.seed_everything(4321)
        assert (random.random(), float(np.random.rand())) != a


# ---------------------------------------------------------------------------
# Training-env reproducibility: same seed -> identical rollout stream
# ---------------------------------------------------------------------------

class TestTrainEnvReproducibility:
    @staticmethod
    def _stream(seed: int, steps: int = 120) -> tuple[list[float], np.ndarray]:
        """Reward stream + first obs for one env reset on ``seed``.

        Actions come from a FIXED action RNG, so the only source of
        difference between two calls is the env's own seeding.
        """
        env = tn.make_stage_env("G1", shaping_scale=1.0)
        obs, _ = env.reset(seed=seed)
        first = np.array(obs, copy=True)
        rng = np.random.default_rng(0)
        rewards: list[float] = []
        for _ in range(steps):
            mask = env.action_masks()
            action = int(rng.choice(np.flatnonzero(mask)))
            obs, reward, terminated, truncated, _ = env.step(action)
            rewards.append(float(reward))
            if terminated or truncated:
                break
        return rewards, first

    def test_same_seed_identical_stream(self):
        base = tn.train_env_seed_base(0)
        r1, o1 = self._stream(base)
        r2, o2 = self._stream(base)
        assert r1 == r2
        np.testing.assert_array_equal(o1, o2)

    def test_different_seed_different_stream(self):
        r0, o0 = self._stream(tn.train_env_seed_base(0))
        r1, o1 = self._stream(tn.train_env_seed_base(1))
        assert r0 != r1 or not np.array_equal(o0, o1)


# ---------------------------------------------------------------------------
# SB3 plumbing: VecEnv.seed(base) gives env i the seed base + i
# ---------------------------------------------------------------------------

import gymnasium as gym  # noqa: E402


class _RecordingEnv(gym.Env):
    """Minimal gym env that records the seed it was reset with."""

    def __init__(self, log: list):
        self.log = log
        self.observation_space = gym.spaces.Box(low=0.0, high=1.0, shape=(1,),
                                               dtype=np.float32)
        self.action_space = gym.spaces.Discrete(2)
        self.render_mode = None
        self.spec = None
        self.metadata: dict = {}

    def reset(self, seed=None, options=None):
        self.log.append(seed)
        return np.zeros(1, dtype=np.float32), {}

    def step(self, action):
        return np.zeros(1, dtype=np.float32), 0.0, True, False, {}

    def close(self):
        pass

    def render(self):
        return None


class TestVecEnvSeedPlumbing:
    def test_seed_base_distributed_to_envs(self):
        pytest.importorskip("stable_baselines3")
        from stable_baselines3.common.vec_env import DummyVecEnv

        log: list = []
        vec = DummyVecEnv([lambda: _RecordingEnv(log) for _ in range(4)])
        base = tn.train_env_seed_base(2)
        vec.seed(base)
        vec.reset()
        assert log == [base + i for i in range(4)]
        # Seeds are consumed once: later (auto-)resets are unseeded, so the
        # env RNG streams continue deterministically from the initial seed.
        log.clear()
        vec.reset()
        assert log == [None] * 4
        vec.close()

    def test_unseeded_default_passes_none(self):
        pytest.importorskip("stable_baselines3")
        from stable_baselines3.common.vec_env import DummyVecEnv

        log: list = []
        vec = DummyVecEnv([lambda: _RecordingEnv(log) for _ in range(2)])
        vec.reset()
        assert log == [None, None]       # historical behavior with no --seed
        vec.close()


# ---------------------------------------------------------------------------
# Model construction: seed forwarded to SB3 (and None by default)
# ---------------------------------------------------------------------------

class _FakeAlgo:
    def __init__(self, *args, **kwargs):
        self.args = args
        self.kwargs = kwargs


class TestBuildModelSeed:
    @staticmethod
    def _args(**over):
        base = dict(seed=None, gamma=None, ent_coef=None, hand_encoding=None,
                    anchor_model=None, sil=False)
        base.update(over)
        return SimpleNamespace(**base)

    def test_seed_forwarded(self, monkeypatch):
        pytest.importorskip("sb3_contrib")
        import sb3_contrib

        monkeypatch.setattr(sb3_contrib, "MaskablePPO", _FakeAlgo)
        model = tn.build_model("VEC", None, self._args(seed=7))
        assert model.kwargs["seed"] == 7

    def test_default_seed_is_none(self, monkeypatch):
        pytest.importorskip("sb3_contrib")
        import sb3_contrib

        monkeypatch.setattr(sb3_contrib, "MaskablePPO", _FakeAlgo)
        model = tn.build_model("VEC", None, self._args())
        assert model.kwargs["seed"] is None

    def test_missing_attr_is_none(self, monkeypatch):
        """Callers that predate --seed (older wrappers) still work."""
        pytest.importorskip("sb3_contrib")
        import sb3_contrib

        monkeypatch.setattr(sb3_contrib, "MaskablePPO", _FakeAlgo)
        model = tn.build_model("VEC", None, SimpleNamespace())
        assert model.kwargs["seed"] is None


class TestPolicyInitDeterminism:
    """The strong assertion: identical seed -> bit-identical initial policy."""

    @staticmethod
    def _params(seed: int):
        torch = pytest.importorskip("torch")
        from gymnasium import spaces
        from sb3_contrib.common.maskable.policies import MaskableActorCriticPolicy

        from sts2_env.gym_env import rich_observation as ro
        from sts2_env.train.policy import rich_policy_kwargs

        tn.seed_everything(seed)
        policy = MaskableActorCriticPolicy(
            spaces.Box(low=ro.RICH_OBS_LOW, high=ro.RICH_OBS_HIGH,
                       shape=(ro.RICH_OBS_SIZE,), dtype=np.float32),
            spaces.Discrete(157),
            lambda _: 3e-4,
            **rich_policy_kwargs(),
        )
        return torch.cat([p.detach().flatten() for p in policy.parameters()])

    def test_same_seed_identical_params(self):
        torch = pytest.importorskip("torch")
        a = self._params(0)
        b = self._params(0)
        assert a.numel() > 1_000_000
        assert torch.equal(a, b)

    def test_different_seed_different_params(self):
        torch = pytest.importorskip("torch")
        assert not torch.equal(self._params(0), self._params(1))


# ---------------------------------------------------------------------------
# Eval-seed isolation
# ---------------------------------------------------------------------------

class _StubEvalEnv:
    """Env that closes an episode after one step and logs its reset seed."""

    def __init__(self, log: list):
        self.log = log

    def reset(self, seed=None):
        self.log.append(seed)
        return np.zeros(3, dtype=np.float32), {}

    def action_masks(self):
        return np.ones(2, dtype=bool)

    def step(self, action):
        return (np.zeros(3, dtype=np.float32), 0.0, True, False,
                {"won": False, "floor": 4, "act": 0, "truncated": False})


class _StubModel:
    def predict(self, obs, action_masks=None, deterministic=True):
        return 0, None

    def save(self, path):
        Path(str(path) + ".zip").write_bytes(b"stub")


class TestEvalSeedIsolation:
    def test_run_eval_uses_held_out_block(self, monkeypatch):
        log: list = []
        monkeypatch.setattr(tn, "make_stage_env",
                            lambda *a, **kw: _StubEvalEnv(log))
        metrics = tn.run_eval(_StubModel(), "G1", n_episodes=4)
        assert log == [tn.EVAL_SEED_BLOCK + e for e in range(4)]
        assert metrics["episodes"] == 4

    def test_seed_everything_does_not_move_eval_seeds(self, monkeypatch):
        log: list = []
        monkeypatch.setattr(tn, "make_stage_env",
                            lambda *a, **kw: _StubEvalEnv(log))
        tn.seed_everything(0)
        tn.run_eval(_StubModel(), "G1", n_episodes=3)
        first = list(log)
        log.clear()
        tn.seed_everything(12345)
        tn.run_eval(_StubModel(), "G1", n_episodes=3)
        assert log == first == [tn.EVAL_SEED_BLOCK + e for e in range(3)]

    def test_env_kwargs_carry_no_seed(self):
        args = SimpleNamespace(seed=2, legacy_shaping=False, no_deck_obs=False,
                              gamma=None)
        kwargs = tn.env_kwargs_from_args(args)
        assert "seed" not in kwargs
        assert set(kwargs) == {"legacy_shaping", "include_deck_obs", "gamma_shape"}

    def test_callback_never_overrides_the_seed_block(self, monkeypatch, tmp_path):
        pytest.importorskip("stable_baselines3")
        captured: dict = {}

        def fake_run_eval(model, stage_name, n_episodes, **kwargs):
            captured["stage"] = stage_name
            captured["n_episodes"] = n_episodes
            captured["kwargs"] = kwargs
            return {"win_rate": 0.0, "episodes": n_episodes,
                    "mean_floors": 6.0, "mean_act": 0.5,
                    "truncation_rate": 0.0, "deaths_by_act": {}}

        monkeypatch.setattr(tn, "run_eval", fake_run_eval)
        CurriculumCallback = tn.build_callback_class()
        cb = CurriculumCallback("G1", tmp_path, {}, eval_freq=500_000,
                                checkpoint_freq=10**12, eval_episodes=200,
                                verbose=0, env_kwargs={})
        cb.model = _StubModel()
        cb._do_eval(500_000)
        assert "seed_block" not in captured["kwargs"]
        assert captured["n_episodes"] == 200
        history = (tmp_path / "eval_history.jsonl").read_text(encoding="utf-8")
        assert json.loads(history.strip())["steps"] == 500_000


# ---------------------------------------------------------------------------
# The trainer's real CLI: confirmation flags AND the live harness's flags
# ---------------------------------------------------------------------------

class TestTrainerCli:
    @staticmethod
    def _parse(argv: list[str], monkeypatch) -> SimpleNamespace:
        seen: dict = {}

        def fake_train_stage(stage_name, args, warm_start_from=None):
            seen["stage"] = stage_name
            seen["args"] = args
            return {}

        monkeypatch.setattr(tn, "train_stage", fake_train_stage)
        monkeypatch.setattr(sys, "argv", ["train_necrobinder.py", *argv])
        tn.main()
        return seen

    def test_live_ablation_harness_flags_still_parse(self, monkeypatch):
        """Regression guard for the RUNNING screen: every arm's exact flag
        set must keep parsing, with --seed absent (unseeded default)."""
        for arm, (_desc, flags) in ab.ARMS.items():
            argv = [*ab.BASE_FLAGS, "--output-dir", "output/ablation/tmp", *flags]
            seen = self._parse(argv, monkeypatch)
            args = seen["args"]
            assert seen["stage"] == "G1", arm
            assert args.seed is None, arm
            assert args.total_steps == ab.ARM_STEPS
            assert args.n_envs == ab.N_ENVS
            assert args.eval_freq == ab.EVAL_FREQ
            assert args.eval_episodes == ab.EVAL_EPISODES

    def test_confirm_phase_commands_parse(self, monkeypatch, tmp_path):
        for config, flags in cp.resolve_configs("A0,A1,A4", {}):
            cmd = cp.build_cmd(config, flags, 2, tmp_path / config)
            seen = self._parse(cmd[2:], monkeypatch)   # drop python + script
            args = seen["args"]
            assert args.seed == 2
            assert args.total_steps == cp.RUN_STEPS == 3_000_000
            assert args.eval_freq == cp.EVAL_FREQ == 500_000
            assert args.eval_episodes == 200
            assert args.n_envs == 16
            assert cp.RUN_STEPS // cp.EVAL_FREQ == 6      # six eval points

    def test_seed_validation_rejects_eval_block(self, monkeypatch):
        bad = tn.EVAL_SEED_BLOCK // tn.TRAIN_SEED_STRIDE
        with pytest.raises(ValueError):
            self._parse(["--stage", "G1", "--seed", str(bad)], monkeypatch)


class TestExactDeterminismFlag:
    """--deterministic is OPT-IN: default off keeps the historical path."""

    @staticmethod
    def _run_main(argv, monkeypatch) -> tuple[SimpleNamespace, list]:
        calls: list = []
        monkeypatch.setattr(tn, "enable_exact_determinism",
                            lambda: calls.append(True))
        seen: dict = {}
        monkeypatch.setattr(tn, "train_stage",
                            lambda stage, args, warm_start_from=None:
                            seen.update(args=args) or {})
        monkeypatch.setattr(sys, "argv", ["train_necrobinder.py", *argv])
        tn.main()
        return seen["args"], calls

    def test_flag_enables_determinism(self, monkeypatch):
        args, calls = self._run_main(
            ["--stage", "G1", "--seed", "0", "--deterministic"], monkeypatch)
        assert args.deterministic is True
        assert calls == [True]

    def test_default_leaves_torch_untouched(self, monkeypatch):
        args, calls = self._run_main(["--stage", "G1", "--seed", "0"], monkeypatch)
        assert args.deterministic is False
        assert calls == []

    def test_cublas_workspace_set_at_import_from_argv(self):
        """cuBLAS reads CUBLAS_WORKSPACE_CONFIG when it creates its handle,
        long before argparse runs -- the import-time argv peek is what makes
        the flag work at all, so it is tested in a real interpreter."""
        import subprocess

        code = (
            "import sys, os;"
            f"sys.path.insert(0, {str(SCRIPTS)!r});"
            "sys.argv = ['train_necrobinder.py', '--deterministic'];"
            "import train_necrobinder;"
            "print(os.environ.get('CUBLAS_WORKSPACE_CONFIG'))"
        )
        out = subprocess.run([sys.executable, "-c", code], capture_output=True,
                             text=True, timeout=180)
        assert out.returncode == 0, out.stderr
        assert out.stdout.strip() == ":4096:8"

        code_off = code.replace("'--deterministic'", "'--stage'")
        out = subprocess.run([sys.executable, "-c", code_off],
                             capture_output=True, text=True, timeout=180)
        assert out.returncode == 0, out.stderr
        assert out.stdout.strip() == "None"     # untouched without the flag


# ---------------------------------------------------------------------------
# Confirmation harness: config resolution
# ---------------------------------------------------------------------------

def _screen(**slopes) -> dict:
    """Screen results where every arm sits at the SAME final level (so the
    level gate never fires unless a test sets ``final_floors`` itself)."""
    return {"arms": {
        name: {"status": "completed", "floor_slope_per_m": slope,
               "final_floors": 6.0, "evals": [], "flags": []}
        for name, slope in slopes.items()
    }}


class TestConfigResolution:
    def test_explicit_configs(self):
        got = cp.resolve_configs("A0,A1,A4", {})
        assert [name for name, _ in got] == ["A0", "A1", "A4"]
        assert dict(got)["A1"] == ["--legacy-shaping"]
        assert dict(got)["A4"] == ["--sil"]

    def test_baseline_always_included_first(self):
        got = cp.resolve_configs("A5", {})
        assert [name for name, _ in got] == ["A0", "A5"]

    def test_default_is_baseline_plus_top_two(self):
        screen = _screen(A0=0.05, A1=0.26, A2=-0.10, A4=0.31, A5=0.20)
        assert cp.top_screened_arms(screen) == ["A4", "A1"]
        assert [n for n, _ in cp.resolve_configs(None, screen)] == ["A0", "A4", "A1"]

    def test_default_ignores_unfinished_arms(self):
        screen = _screen(A1=0.26, A4=0.31)
        screen["arms"]["A5"] = {"status": "crashed (rc=1)",
                                "floor_slope_per_m": 9.9}
        screen["arms"]["A6"] = {"status": "completed", "floor_slope_per_m": None}
        assert cp.top_screened_arms(screen) == ["A4", "A1"]

    def test_default_needs_a_finished_screen(self):
        with pytest.raises(SystemExit, match="completed screen arm"):
            cp.resolve_configs(None, _screen(A1=0.2))

    def test_level_gate_excludes_a_high_slope_hole_climber(self):
        """The real A2 case: best slope in the screen (+0.55/M) while
        finishing 2.31 floors BEHIND baseline. It must not be carried into
        the (expensive) confirmation phase on slope alone."""
        screen = _screen(A0=0.05, A1=0.26, A2=0.55, A4=0.20)
        screen["arms"]["A0"]["final_floors"] = 6.79
        screen["arms"]["A2"]["final_floors"] = 4.48      # -2.31 vs baseline
        screen["arms"]["A1"]["final_floors"] = 6.99
        screen["arms"]["A4"]["final_floors"] = 6.60
        assert cp.top_screened_arms(screen) == ["A1", "A4"]
        assert not cp.passes_screen_level_gate(screen["arms"]["A2"],
                                               screen["arms"]["A0"])
        assert cp.passes_screen_level_gate(screen["arms"]["A1"],
                                           screen["arms"]["A0"])

    def test_level_gate_uses_the_screens_own_noise_band(self):
        assert cp.ab_noise_floors() == ab.NOISE_FLOORS
        base = {"final_floors": 6.0}
        inside = {"final_floors": 6.0 - ab.NOISE_FLOORS + 0.01}
        outside = {"final_floors": 6.0 - ab.NOISE_FLOORS - 0.01}
        assert cp.passes_screen_level_gate(inside, base)
        assert not cp.passes_screen_level_gate(outside, base)
        assert cp.passes_screen_level_gate({"final_floors": None}, base)
        assert cp.passes_screen_level_gate({"final_floors": 1.0}, None)

    def test_final_config_from_selection(self):
        screen = _screen(A1=0.26)
        screen["selection"] = {"final_flags": ["--legacy-shaping", "--sil"]}
        got = dict(cp.resolve_configs("A0,FINAL", screen))
        assert got["FINAL"] == ["--legacy-shaping", "--sil"]

    def test_final_empty_flags_is_pure_baseline(self):
        screen = _screen(A1=0.26)
        screen["selection"] = {"final_flags": []}
        assert dict(cp.resolve_configs("FINAL", screen))["FINAL"] == []
        assert "pure baseline" in cp.describe("FINAL", [])

    def test_final_without_selection_errors(self):
        with pytest.raises(SystemExit, match="FINAL"):
            cp.resolve_configs("A0,FINAL", _screen(A1=0.2))

    def test_unknown_config_errors(self):
        with pytest.raises(SystemExit, match="unknown config"):
            cp.resolve_configs("A0,A99", {})

    def test_no_early_stop_in_confirmation(self):
        """The screen early-stops hopeless arms; truncating a confirmation
        seed would bias its slope, so the option must not exist here."""
        src = (SCRIPTS / "confirm_phase.py").read_text(encoding="utf-8")
        assert "EARLY_STOP" not in src
        assert cp.load_results()["config"]["early_stop"] is False


# ---------------------------------------------------------------------------
# Statistics on synthetic data with known answers
# ---------------------------------------------------------------------------

class TestSlope:
    def test_exact_line(self):
        assert cp.lsq_slope([0, 1, 2, 3], [1, 3, 5, 7]) == pytest.approx(2.0)

    def test_known_noisy_slope(self):
        xs = [0.0, 1.0, 2.0, 3.0, 4.0]
        ys = [1.0, 1.0, 4.0, 3.0, 6.0]     # Sxy = 12, Sxx = 10
        assert cp.lsq_slope(xs, ys) == pytest.approx(1.2)

    def test_degenerate_inputs(self):
        assert cp.lsq_slope([1.0], [2.0]) is None
        assert cp.lsq_slope([1.0, 1.0], [2.0, 3.0]) is None
        assert cp.lsq_slope([1.0, 2.0], [1.0]) is None

    def test_eval_slope_from_eval_records(self):
        evals = [{"steps": s, "mean_floors": 5.0 + 0.5 * (s / 1e6)}
                 for s in range(500_000, 3_000_001, 500_000)]
        assert len(evals) == 6
        assert cp.eval_slope(evals) == pytest.approx(0.5)

    def test_six_points_beat_three_on_noise(self):
        """Sanity check of the reason for --eval-freq 500k: with the same
        per-eval noise, the least-squares slope estimator over 6 points has
        a strictly smaller variance than over 3 (endpoint difference)."""
        rng = np.random.default_rng(7)
        three_x = [1.0, 2.0, 3.0]
        six_x = [0.5 * i for i in range(1, 7)]
        def spread(xs):
            est = [cp.lsq_slope(xs, [0.5 * x + rng.normal(0, 0.5) for x in xs])
                   for _ in range(4000)]
            return float(np.std(est))
        assert spread(six_x) < spread(three_x)


class TestMeanSe:
    def test_known_values(self):
        stat = cp.mean_se([1.0, 2.0, 3.0])
        assert stat["n"] == 3
        assert stat["mean"] == pytest.approx(2.0)
        assert stat["sd"] == pytest.approx(1.0)               # ddof=1
        assert stat["se"] == pytest.approx(1.0 / np.sqrt(3))

    def test_matches_numpy_ddof1(self):
        vals = [0.31, 0.05, 0.42, -0.11]
        stat = cp.mean_se(vals)
        assert stat["sd"] == pytest.approx(float(np.std(vals, ddof=1)))
        assert stat["se"] == pytest.approx(
            float(np.std(vals, ddof=1) / np.sqrt(len(vals))))

    def test_single_and_empty(self):
        assert cp.mean_se([4.0]) == {"n": 1, "mean": 4.0, "sd": None, "se": None}
        assert cp.mean_se([])["mean"] is None

    def test_zero_variance(self):
        stat = cp.mean_se([2.0, 2.0, 2.0])
        assert stat["sd"] == 0.0 and stat["se"] == 0.0


class TestWelch:
    def test_textbook_example(self):
        """Classic unequal-variance example: t = -2.46, df = 12.8, p = 0.029."""
        a = [27.5, 21.0, 19.0, 23.6, 17.0, 17.9, 16.9, 15.6, 4.0, 7.8]
        b = [27.1, 22.0, 20.8, 23.4, 23.4, 23.5, 25.8, 22.0, 26.1, 15.6]
        res = cp.welch_ttest(a, b)
        assert res["t"] == pytest.approx(-2.4556, abs=5e-4)
        assert res["df"] == pytest.approx(12.8412, abs=5e-4)
        assert res["p_two_sided"] == pytest.approx(0.0291, abs=5e-4)

    def test_t_distribution_critical_values(self):
        for df, crit in ((1, 12.7062), (5, 2.5706), (10, 2.2281),
                         (30, 2.0423), (100, 1.9840)):
            assert 2 * cp.t_sf(crit, df) == pytest.approx(0.05, abs=1e-4)
        assert cp.t_sf(0.0, 5) == pytest.approx(0.5)

    def test_regularised_incomplete_beta(self):
        assert cp.betainc_reg(1, 1, 0.5) == pytest.approx(0.5)
        assert cp.betainc_reg(2, 3, 0.5) == pytest.approx(0.6875)
        assert cp.betainc_reg(2, 3, 0.0) == 0.0
        assert cp.betainc_reg(2, 3, 1.0) == 1.0

    def test_identical_samples_p_one(self):
        res = cp.welch_ttest([1.0, 2.0, 3.0], [1.0, 2.0, 3.0])
        assert res["t"] == pytest.approx(0.0)
        assert res["p_two_sided"] == pytest.approx(1.0)

    def test_undefined_cases_return_none(self):
        assert cp.welch_ttest([1.0], [1.0, 2.0]) is None      # n < 2
        assert cp.welch_ttest([1.0, 1.0], [2.0, 2.0]) is None  # zero variance

    def test_well_separated_samples_are_significant(self):
        res = cp.welch_ttest([10.0, 10.2, 9.8], [1.0, 1.1, 0.9])
        assert res["t"] > 0
        assert res["p_two_sided"] < 0.001


# ---------------------------------------------------------------------------
# Decision rule + end-to-end analysis on synthetic runs
# ---------------------------------------------------------------------------

def _run(config: str, seed: int, slope: float, start: float = 6.0,
         status: str = "completed", flags: list[str] | None = None) -> dict:
    """A synthetic run record whose 6 eval points have exactly ``slope``."""
    evals = [
        {"steps": s, "mean_floors": start + slope * (s / 1e6),
         "win_rate": 0.0, "episodes": 200, "deaths_by_act": {"0": 190}}
        for s in range(500_000, 3_000_001, 500_000)
    ]
    rec = cp.summarize(config, flags or [], seed, evals, status, 42.0)
    assert rec["slope_per_m"] == pytest.approx(slope)
    return rec


def _results(**by_config) -> dict:
    runs = {}
    for config, slopes in by_config.items():
        for seed, slope in enumerate(slopes):
            runs[cp.run_key(config, seed)] = _run(config, seed, slope)
    return {"runs": runs}


class TestDecisionRule:
    def test_clear_win(self):
        res = _results(A0=[0.00, 0.05, 0.10], A1=[0.90, 0.95, 1.00])
        analysis = cp.analyze(res)
        assert analysis["winners"] == ["A1"]
        assert analysis["recommended"] == "A1"
        assert "WIN on slope" in analysis["verdicts"][0]

    def test_gap_inside_pooled_se_is_not_distinguishable(self):
        # Means differ by 0.10/M; each SE is ~0.29/M -> pooled ~0.41/M.
        res = _results(A0=[-0.35, 0.05, 0.45], A1=[-0.25, 0.15, 0.55])
        analysis = cp.analyze(res)
        assert analysis["winners"] == []
        assert analysis["recommended"] == "A0"
        assert "NOT DISTINGUISHABLE" in analysis["verdicts"][0]
        assert "simplest/cheapest" in analysis["reason"]

    def test_level_gate_disqualifies_before_slope(self):
        """A config climbing out of a hole must not win on slope. Slopes are
        exact, so A1 gains 3 floors over the run but still ends far below A0."""
        res = _results(A0=[0.00, 0.05, 0.10])
        for seed, slope in enumerate([1.00, 1.05, 1.10]):
            res["runs"][cp.run_key("A1", seed)] = _run("A1", seed, slope,
                                                       start=1.0)
        analysis = cp.analyze(res)
        a1 = analysis["per_config"]["A1"]
        a0 = analysis["per_config"]["A0"]
        assert a1["slope"]["mean"] > a0["slope"]["mean"]     # better slope
        assert a1["final"]["mean"] < a0["final"]["mean"]     # worse level
        assert analysis["winners"] == []
        assert "DISQUALIFIED on level" in analysis["verdicts"][0]
        assert analysis["recommended"] == "A0"

    def test_level_gate_tolerates_noise_sized_gaps(self):
        """A level deficit INSIDE the pooled SE must NOT disqualify.

        A0 finals 6.00/6.15/6.30 (mean 6.15, SE 0.087); A1 finals
        6.00/6.10/6.20 (mean 6.10, SE 0.058) -> gap -0.05 against a pooled SE
        of 0.104, so A1 stays in and wins on its much better slope.
        """
        res = _results(A0=[0.00, 0.05, 0.10])
        for seed, (slope, start) in enumerate(
                [(0.90, 3.30), (0.95, 3.25), (1.00, 3.20)]):
            res["runs"][cp.run_key("A1", seed)] = _run("A1", seed, slope,
                                                       start=start)
        analysis = cp.analyze(res)
        a0 = analysis["per_config"]["A0"]["final"]
        a1 = analysis["per_config"]["A1"]["final"]
        assert a1["mean"] < a0["mean"]                       # slightly behind
        assert a0["mean"] - a1["mean"] < cp.pooled_se(a0, a1)  # but in noise
        assert "DISQUALIFIED" not in analysis["verdicts"][0]
        assert analysis["winners"] == ["A1"]

    def test_worse_config_never_wins(self):
        res = _results(A0=[0.30, 0.35, 0.40], A1=[-0.30, -0.35, -0.40])
        analysis = cp.analyze(res)
        assert analysis["winners"] == []
        assert analysis["recommended"] == "A0"

    def test_aggregate_statistics_are_correct(self):
        res = _results(A0=[0.0, 0.1, 0.2])
        cfg = cp.analyze(res)["per_config"]["A0"]
        assert cfg["seeds"] == [0, 1, 2]
        assert cfg["slopes"] == pytest.approx([0.0, 0.1, 0.2])
        assert cfg["slope"]["mean"] == pytest.approx(0.1)
        assert cfg["slope"]["se"] == pytest.approx(0.1 / np.sqrt(3))
        # final floors: start 6.0 + slope * 3M
        assert cfg["final"]["mean"] == pytest.approx(6.0 + 0.1 * 3)

    def test_single_seed_cannot_win(self):
        res = _results(A0=[0.0, 0.05, 0.1], A1=[5.0])
        analysis = cp.analyze(res)
        assert analysis["winners"] == []
        assert "NOT DISTINGUISHABLE" in analysis["verdicts"][0]
        assert "standard error" in analysis["verdicts"][0]

    def test_crashed_seeds_are_excluded_but_recorded(self):
        res = _results(A0=[0.0, 0.1, 0.2])
        crashed = _run("A0", 3, 0.0, status="crashed (rc=1)")
        crashed["slope_per_m"] = None
        res["runs"]["A0_s3"] = crashed
        cfg = cp.analyze(res)["per_config"]["A0"]
        assert cfg["seeds"] == [0, 1, 2]
        assert cfg["statuses"][3].startswith("crashed")

    def test_missing_baseline_is_reported_not_crashed(self):
        analysis = cp.analyze(_results(A1=[0.5, 0.6, 0.7]))
        assert analysis["winners"] == []
        assert any("A0 baseline" in v for v in analysis["verdicts"])

    def test_cheapest_winner_recommended(self):
        res = _results(A0=[0.0, 0.0, 0.0])
        for seed, slope in enumerate([0.9, 0.95, 1.0]):
            res["runs"][cp.run_key("A1", seed)] = _run(
                "A1", seed, slope, flags=["--legacy-shaping"])
        for seed, slope in enumerate([0.9, 0.95, 1.0]):
            res["runs"][cp.run_key("FINAL", seed)] = _run(
                "FINAL", seed, slope, flags=["--legacy-shaping", "--sil"])
        analysis = cp.analyze(res)
        assert set(analysis["winners"]) == {"A1", "FINAL"}
        assert analysis["recommended"] == "A1"   # fewest flags among winners


class TestReporting:
    def test_markdown_has_numbers_and_the_power_caveat(self):
        res = _results(A0=[0.0, 0.1, 0.2], A1=[0.3, 0.4, 0.5])
        res["updated"] = "2026-07-25 12:00:00"
        md = cp.render_markdown(res, cp.analyze(res))
        assert "| A0 |" in md and "| A1 |" in md
        assert "LOW POWER" in md
        assert "Welch t vs A0" in md
        assert "Recommended config:" in md

    def test_write_docs_replaces_only_the_marked_block(self, monkeypatch, tmp_path):
        doc = tmp_path / "ABLATION_STUDY.md"
        doc.write_text(
            "# Study\n\n## Screen results\n\n| arm | slope |\n|---|---|\n"
            f"| A0 | +0.05 |\n\n## Confirmation phase\n\n{cp.DOC_START}\n\n"
            f"_placeholder_\n\n{cp.DOC_END}\n\n## Tail section\n\nkeep me\n",
            encoding="utf-8")
        monkeypatch.setattr(cp, "DOC_PATH", doc)
        res = _results(A0=[0.0, 0.1, 0.2], A1=[0.3, 0.4, 0.5])
        cp.write_docs(res, cp.analyze(res))
        text = doc.read_text(encoding="utf-8")
        assert "## Screen results" in text and "| A0 | +0.05 |" in text
        assert "## Tail section" in text and "keep me" in text
        assert "_placeholder_" not in text
        assert text.count(cp.DOC_START) == 1 and text.count(cp.DOC_END) == 1
        assert "| A1 |" in text

    def test_write_docs_appends_when_markers_missing(self, monkeypatch, tmp_path):
        doc = tmp_path / "ABLATION_STUDY.md"
        doc.write_text("# Study\n\noriginal content\n", encoding="utf-8")
        monkeypatch.setattr(cp, "DOC_PATH", doc)
        res = _results(A0=[0.0, 0.1, 0.2])
        cp.write_docs(res, cp.analyze(res))
        text = doc.read_text(encoding="utf-8")
        assert "original content" in text          # never truncates the doc
        assert cp.DOC_START in text and cp.DOC_END in text

    def test_print_report_is_ascii_safe(self, capsys):
        res = _results(A0=[0.0, 0.1, 0.2], A1=[0.3, 0.4, 0.5])
        cp.print_report(res, cp.analyze(res))
        out = capsys.readouterr().out
        out.encode("cp1252")                       # Windows console safe
        assert "+/-" in out and "LOW statistical power" in out


class TestRunPlumbing:
    def test_build_cmd_shape(self, tmp_path):
        cmd = cp.build_cmd("A1", ["--legacy-shaping"], 1, tmp_path / "A1_s1")
        assert cmd[0] == sys.executable and cmd[1].endswith("train_necrobinder.py")
        assert cmd[-1] == "--legacy-shaping"       # config flags come last
        for flag, value in (("--stage", "G1"), ("--total-steps", "3000000"),
                            ("--n-envs", "16"), ("--eval-freq", "500000"),
                            ("--eval-episodes", "200"), ("--seed", "1")):
            assert cmd[cmd.index(flag) + 1] == value

    def test_summarize_fields(self):
        rec = _run("A1", 2, 0.4, flags=["--legacy-shaping"])
        assert rec["config"] == "A1" and rec["seed"] == 2
        assert rec["n_evals"] == 6
        assert rec["final_floors"] == pytest.approx(6.0 + 0.4 * 3)
        assert rec["final_act2"] == 10                # 200 - 190 act-0 deaths
        assert rec["flags"] == ["--legacy-shaping"]

    def test_summarize_survives_zero_evals(self):
        rec = cp.summarize("A0", [], 0, [], "crashed (rc=1)", 1.0)
        assert rec["slope_per_m"] is None and rec["final_floors"] is None
        assert rec["n_evals"] == 0

    def test_reuses_ablation_hardening(self):
        """The confirmation harness must not fork its own process handling."""
        assert cp.kill_tree is ab.kill_tree
        assert cp.preflight_commit is ab.preflight_commit
        assert cp.commit_available_mb is ab.commit_available_mb
        assert cp.read_evals is ab.read_evals
        assert isinstance(cp.commit_available_mb(), int)
