---
name: sts2-analysis-toolkit
description: >
  Measure, don't eyeball: how to read SB3/MaskablePPO training logs (approx_kl,
  clip_fraction, learning_rate, entropy_loss — with the stage-A frozen-optimizer
  worked example), compute Wilson-CI win-rate statistics and size evals, run
  random-baseline comparisons, profile throughput (env vs learn vs IPC,
  benchmark_rich_env), validate the 4184-dim rich observation layout, reason
  about PBRS reward invariance and what would falsify it, and run honest
  ablations. Load this when interpreting eval_history.jsonl or campaign logs,
  deciding whether a win-rate difference is real, diagnosing a plateau vs an
  optimizer freeze, benchmarking steps/s, or checking obs offsets. Do NOT load
  it for launching/resuming training or TensorBoard ops (sts2-run-and-operate),
  deciding WHICH experiment to run next (sts2-training-campaign), fixing
  sim/training bugs (sts2-debugging-playbook), the evidence-bar/idea-lifecycle
  process (sts2-research-methodology), or obs/policy design rationale
  (sts2-architecture-contract).
---

# sts2-analysis-toolkit — measurement and statistics for the Necrobinder campaign

This skill is the project's measurement discipline: how to turn raw logs,
eval files, and benchmarks into claims that survive scrutiny. House rule
(all repos): every win-rate or performance claim carries its protocol
(episodes, seeds, deterministic flag, shaping off), and final claims need
>= 1000 eval episodes with a Wilson 95% CI. Aspirational targets (the 95%
A10 goal) are always labeled aspirational.

Two helper scripts ship with this skill (both smoke-tested 2026-07-24 with
the repo venv, no dependencies beyond numpy):

- `.claude\skills\sts2-analysis-toolkit\scripts\summarize_eval_history.py`
- `.claude\skills\sts2-analysis-toolkit\scripts\random_baseline.py`

Run everything from the repo root `C:\Users\motqu\GitHub\sts2-rl-agent` with
`.venv\Scripts\python.exe` (never bare `python`; never `uv sync` — see
sts2-build-and-env).

## Jargon (defined once)

| Term | Meaning here |
|---|---|
| SB3 / MaskablePPO | stable-baselines3 2.9.0 + sb3-contrib 2.9.0 `MaskablePPO` — the only training algorithm in use |
| rich obs | the 4184-dim float32 vector (`sts2_env/gym_env/rich_observation.py`, `RICH_OBS_SIZE`) shared by combat and run envs |
| shaping_scale | single scalar in [0,1] multiplying every non-terminal reward term (`reward_config.py`); 1.0 in training, **always 0.0 in eval** |
| PBRS | potential-based reward shaping, F(s,s') = γ·Φ(s') − Φ(s) — adopted doctrine, **not yet implemented** as of 2026-07-24 |
| Wilson CI | Wilson score interval for a binomial proportion; the required error bar on every win rate |
| eval seed block | trainer evals reset with `seed = 10_000_000 + episode_index` so eval seeds never collide with training seeds |
| plateau | eval win rate whose best-vs-first Wilson CIs overlap across the whole run — i.e. no defensible improvement |
| optimizer freeze | approx_kl ≈ 1e-8, clip_fraction = 0, lr ≈ 0 — updates are numerically dead regardless of what the curve looks like |
| sim_error | `info["sim_error"]=True`: a simulator exception force-ended the episode; scored 0.0, **not** a death (commit fe25668) |
| G-stages | the full-run-only ladder G1–G5 (ascension x act-count) in `scripts/train_necrobinder.py` at HEAD fe25668 |

## Live-state warning (2026-07-24)

- HEAD = fe25668 (2026-07-24 11:52, "Phase 0 training revamp"). The trainer
  was rewritten that morning: stages A/B and the combat-env path are DELETED;
  the ladder is G1–G5, full-run only, never halts, constant lr=2e-4 +
  target_kl=0.03. Anything you read about "stage A/B", linear LR anneal, or
  win-rate shaping anneal describes the PRE-revamp trainer that produced
  `output/necrobinder_a10/` — historical data, not current code.
- A G1 training run was LIVE while this skill was written
  (`output/necrobinder_g1_campaign.log` ends mid-eval at 100k steps,
  16 envs, 500 eval episodes, TensorBoard on). **Do not run benchmarks,
  large evals, or the random-baseline script while a training run is live**
  — CPU/GPU contention invalidates throughput numbers and steals training
  throughput. Check first: `Get-Process python* | Select-Object Id,CPU,StartTime`.
- Before quoting any trainer/reward specific, run `git log --oneline -3` and
  `git status --short` yourself; a concurrent session advances this work.

## 1. Training-log forensics

### 1.1 Where the numbers live

The trainer prints SB3 blocks to stdout only; the `output\*_campaign.log`
files are operator redirections (e.g.
`... train_necrobinder.py --stage G1 ... 2>&1 | Tee-Object output\necrobinder_g1_campaign.log`).
Eval results also go to `<output-dir>\<stage>\eval_history.jsonl` (one JSON
per line) and into every checkpoint's sidecar JSON. TensorBoard event files
land in `<output-dir>\<stage>\tb\` only when `--tensorboard` was passed
(launching TensorBoard itself is sts2-run-and-operate's topic).

### 1.2 Metric reading table (SB3 `train/` and `rollout/` blocks)

| Metric | What it is | Healthy (this project) | Pathology signature |
|---|---|---|---|
| `approx_kl` | mean KL between old/new policy per update | ~1e-3 .. 3e-2 (target_kl=0.03 caps epochs) | ~1e-8 → optimizer freeze; >> 0.03 sustained → lr too hot / bad advantages |
| `clip_fraction` | fraction of ratios hitting the 0.2 clip | 0.01 .. 0.2 | exactly 0 alongside tiny approx_kl → no learning signal at all |
| `learning_rate` | current lr | constant 2.0e-4 at HEAD | 1.44e-07 = the old linear-anneal freeze (see worked example) |
| `entropy_loss` | −entropy·ent_coef contribution (more negative = MORE entropy) | early ~−1.1..−1.4, drifting toward 0 slowly | fast collapse toward ~−0.4 while win rate is flat = premature determinism (stage A did exactly this) |
| `explained_variance` | value-net fit to returns | > ~0.7 once training settles | < 0 early is normal for a few iterations; persistently low on full runs → value/horizon problem |
| `ep_rew_mean` | TRAINING reward incl. shaping (shaping_scale=1) | rises with competence | do NOT read as win rate — shaped reward is not the objective; only eval (shaping=0) win rate counts |
| `ep_len_mean` | mean episode length in env steps | rises as the agent survives longer floors | stuck near ~20–30 on the run env = dying in the first combats |
| `fps` | end-to-end steps/s since learn() start | ~1150–1300 at 16 envs (measured 2026-07-24) | big drops → eval blocking, disk, or a competing process |
| `value_loss`, `policy_gradient_loss` | usual PPO losses | small and moving | pg_loss ~−2e-7 with frozen kl → updates numerically dead |

### 1.3 Worked example: frozen optimizer vs healthy optimizer (real logs)

**Stage A (pre-revamp, `output/necrobinder_a10_campaign.log`, final block
at 5,005,312 steps, 2026-07-24 10:51):**

```
approx_kl            | 8.029929e-09
clip_fraction        | 0
learning_rate        | 1.44e-07
entropy_loss         | -0.383
explained_variance   | 0.974
policy_gradient_loss | -2.12e-07
fps                  | 1307
```

Eval win rate was 60.5% at 100k, best 63.5% at 2.5M, 62.0% at 5M — flat
from the very first eval, and all Wilson CIs overlap (verify with the
summarizer, section 2.3). The old linear LR schedule
(2.5e-4 · progress_remaining) had annealed to ~zero, so the last chunk of
training was numerically dead. Lesson, in order:
**check optimizer aliveness (approx_kl, clip_fraction, lr) BEFORE claiming a
capability ceiling.** The "63.5% ceiling" was partly a schedule artifact;
the revamp spec's stage-A postmortem (docs/TRAINING_REVAMP_SPEC.json,
executive_summary) adds the structural causes (impossible starter-deck-vs-
elite task, mean-pooled hand, blind deck obs).

**G1 (revamp trainer, `output/necrobinder_g1_campaign.log`, iterations 5–6,
~82–98k steps, 2026-07-24):**

```
approx_kl            | 0.0049 .. 0.0076
clip_fraction        | 0.031 .. 0.044
learning_rate        | 0.0002        (constant, by design)
entropy_loss         | -1.12 .. -1.13
explained_variance   | 0.71 .. 0.79
ep_len_mean          | 67 -> 135     (rising: surviving deeper)
ep_rew_mean          | -0.921 -> -0.836
fps                  | ~1150-1227    (16 envs)
```

This is what alive looks like: KL well inside target_kl, non-zero clipping,
high entropy, episode length climbing. `ep_rew_mean` is negative early
because most episodes still end in death (−1) — that is expected, watch the
trend and the evals, not the absolute value.

### 1.4 Forensics checklist (any disappointing run)

1. `git log --oneline -3` + `git status --short` — which trainer produced this?
2. Tail the campaign log: is the optimizer alive (approx_kl > ~1e-4,
   clip_fraction > 0, lr as configured)? If not, the run tells you nothing
   about capability.
3. Summarize `eval_history.jsonl` (section 2.3): is best CI-separated from
   first? If not → plateau; hand off to sts2-training-campaign for what to
   change.
4. `Select-String -Path <campaign log> -Pattern "failed during phase"` —
   count sim-error episodes. At HEAD they score 0.0 and tag
   `info["sim_error"]`, but each one is a simulator bug to report
   (KNOWN_ISSUES #14 discipline; fixing them is sts2-debugging-playbook).
5. Check `truncation_rate` in eval rows (revamp schema): a rising rate after
   the truncation=0.0 change may mean stall-to-avoid-death
   (TRAINING_REVAMP_SPEC risks item 4). Pre-revamp data has no such field.
6. Only after 1–5: reason about task difficulty, observability, or
   architecture (sts2-architecture-contract).

## 2. Win-rate statistics

### 2.1 Wilson CI and eval sizing

A win rate without an interval is not a result. Use the Wilson score
interval (robust near 0%/100%, unlike the normal approximation). No scipy
in the venv (verified 2026-07-24) — use the shipped script:

```powershell
.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\summarize_eval_history.py --wilson 127 200
# -> 127/200 = 63.5%  Wilson 95% CI [56.6%, 69.9%] (half-width ~6.6%)
```

Sizing table (95% CI half-width near p = 0.5–0.65, the regime this campaign
lives in):

| Episodes | ~CI half-width | Use |
|---|---|---|
| 200 | ±6.6–6.9% | pre-revamp trainer default; telemetry only, never a claim |
| 500 | ±4.2–4.4% | current G1 live-run setting (`--eval-episodes 500`); coarse trend |
| 1000 | ±3.0–3.1% | house minimum for any stated result (doctrine + spec success_metrics) |
| 2000 | ±2.1–2.2% | needed to resolve differences under ~5 points |

Two-run comparison rule of thumb: with 1000 episodes per arm at p≈0.6, the
minimum CI-separable difference is ~4–5 points. If you expect a smaller
effect, either grow the eval or don't claim the difference. "Non-overlapping
Wilson CIs" is the house acceptance test (spec success_metrics: "a claim of
an improvement must survive the CI").

Promotion-streak context: the trainer requires the gate on 2 consecutive
evals precisely because a single 200–500-episode eval is noise
(±4–7 points); at HEAD promotion is telemetry only and never halts training.

### 2.2 The eval protocol (what makes a number admissible)

Every admissible win rate states all of:

- env config: `RichSTS2RunEnv`, character, ascension, `max_act_count`
- `shaping_scale=0` (pure sparse reward — `run_eval` builds the eval env
  this way, train_necrobinder.py:111)
- `deterministic=True` in `model.predict`, with `action_masks=` passed
  explicitly (unmasked predict picks invalid actions; run_env falls back
  silently)
- episode count and seed block (trainer: `seed = 10_000_000 + ep`)
- what truncations and sim_errors were scored as (at HEAD: truncation → not
  a win, reward 0.0, counted in `truncation_rate`; sim_error → not a death,
  reward 0.0). Pre-revamp stage-A data scored truncation as death — do not
  mix the two eras in one table without saying so.

Final campaign claims: A10, `max_act_count=4`, 1000 episodes, Wilson 95% CI
(doctrine + spec Phase 9). Launching such an eval is sts2-run-and-operate's
runbook; this skill owns whether the number means anything.

### 2.3 eval_history.jsonl: schemas and the summarizer

Two schemas exist on disk (verified 2026-07-24):

| Field | pre-revamp (stages A/B, `output/necrobinder_a10/A/`) | revamp (G-stages, from run_eval at HEAD) |
|---|---|---|
| `win_rate`, `episodes`, `steps`, `wall_s`, `mean_floors`, `deaths_by_act` | yes | yes |
| `shaping_scale` | yes (the old win-rate anneal's value) | no (constant 1.0 in training, 0 in eval) |
| `mean_act`, `truncation_rate` | no | yes |

**Trap:** in pre-revamp stage-A/B rows, `mean_floors` is always 0.0 and
`deaths_by_act` is always `{"0": N}`. The old combat env never set
`info["floor"]`/`info["act"]`, so those columns are artifacts — only
`win_rate` is meaningful there. The run env's `_build_info`
(sts2_env/gym_env/run_env.py:749-765) provides real `act`/`floor`/`hp`/
`gold`/`deck_size`, so revamp-era telemetry is trustworthy.

Summarize any history file (handles both schemas, prints Wilson CIs, and
gives a plateau verdict):

```powershell
.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\summarize_eval_history.py output\necrobinder_a10\A\eval_history.jsonl
.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\summarize_eval_history.py output\necrobinder_g1\G1 --last 10
```

On the real stage-A file it prints
`best vs first CI-separated: NO` — the statistical statement of the
plateau that killed the old ladder. The same `eval_history` array is
embedded in every checkpoint sidecar JSON, so a deleted jsonl is
recoverable from the newest sidecar.

## 3. Random-baseline comparisons

A trained policy that is not CI-separated from uniform-random-over-valid-
actions has learned nothing on that task. Report the baseline next to any
headline number.

```powershell
# The campaign task family (RichSTS2RunEnv, Necrobinder):
.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\random_baseline.py --env run --acts 1 --ascension 10 --episodes 1000
# The retired stage-A-style combat task (RichSTS2CombatEnv, default pool):
.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\random_baseline.py --env combat --episodes 1000
```

The script uses the same masked-uniform pattern as
`scripts/benchmark_rich_env.py:bench_single_env`, evaluates with
`shaping_scale=0`, and defaults to seed block 20,000,000+ (disjoint from
the trainer's 10M eval block). It reports win rate + Wilson CI,
truncation/sim_error counts, mean floors/act, and env throughput.

Known baseline numbers — treat with care:

- docs/TRAINING_GUIDE.md:88 claims "random baseline ~63.4% for Act 1
  **Ironclad** encounters" on the OLD 131-dim combat env. That doc is stale
  throughout (wrong GPU, wrong action-space size, broken commands — see
  sts2-docs-and-writing); the number is for a different character, env, and
  era. Never use it as the Necrobinder baseline.
- `scripts/benchmark.py` doubles as a random-baseline measurement, but only
  for the old `STS2CombatEnv()` at its defaults (Ironclad, ascension 0 —
  combat_env.py:41-42). Fine for throughput, wrong task for the campaign.
- No 1000-episode random baseline for the retired Necrobinder-A10 stage-A
  combat pool was ever recorded (open gap, 2026-07-24). The stage-A plateau
  at ~62% is uncomfortably close to the stale Ironclad random figure — one
  more reason the revamp treats stage-A history as uninformative about
  capability. If you need that comparison, measure it with the script above
  (`--env combat --ascension 10 --episodes 1000`) on an idle machine.
- Random on the full-run env is near 0% by construction (random play dies
  in the first combats; smoke test: 0/5 wins, mean floor 1.8 at A10 acts=1,
  2026-07-24). For full-run work the more useful floors are per-act reach
  rates (`mean_act`, `deaths_by_act`) — the revamp telemetry.

## 4. Throughput profiling (env vs learn vs IPC)

### 4.1 Tools, cheapest first

All throughput numbers are machine-state-dependent: run on an idle machine
(no live training), on AC power, and say so. Numbers below are
RTX 4060 Laptop 8GB + 16 SubprocVecEnv workers unless stated.

| Tool | Command | Measures |
|---|---|---|
| SB3 log `fps` | read the campaign log | end-to-end (rollout+optimize) steps/s of the actual run; ~1150–1307 observed 2026-07-24 |
| `scripts/benchmark_rich_env.py` | `.venv\Scripts\python.exe scripts\benchmark_rich_env.py --seconds 10 --n-envs 16 [--skip-learn] [--skip-subproc] [--learn-steps 20000]` | single-env steps/s old vs rich (combat+run), N-env SubprocVecEnv steps/s, and a short GPU `.learn()` rate — the three-way split that separates env cost, IPC cost, and optimize cost |
| `scripts/benchmark.py` | `.venv\Scripts\python.exe scripts\benchmark.py` | old combat env only: episodes/s, steps/s, random win rate (Ironclad A0). Legacy sanity check |
| cProfile one-off | snippet below | where a single env step spends its time |
| `nvidia-smi -l 2` | during training | GPU util; low util = CPU/IPC bound |

Interpretation model: `.learn()` fps < SubprocVecEnv fps < single-env·N.
The gap between single-env·N and SubprocVecEnv is IPC + serialization
(the 4184-float obs is ~19KB pickled per env-step); the gap between
SubprocVecEnv and `.learn()` is rollout bookkeeping + optimization.
Design-review measurements (docs/TRAINING_REVAMP_SPEC.json,
throughput_and_infra, 2026-07-24): env-only 4442 steps/s vs `.learn()`
1321 at 16 envs, GPU ~35% utilized — the bottleneck is CPU env-stepping +
IPC, not the GPU. Re-verified independently 2026-07-24: the campaign logs
show `.learn()` fps 1150–1307.

### 4.2 Hot-path profile (verified 2026-07-24)

```powershell
.venv\Scripts\python.exe -c "
import cProfile, pstats, io, numpy as np
from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv
from sts2_env.gym_env.reward_config import RewardConfig
env = RichSTS2RunEnv(ascension_level=0, max_act_count=1, reward_config=RewardConfig(shaping_scale=0.0))
rng = np.random.default_rng(0); obs, info = env.reset(seed=123)
pr = cProfile.Profile(); pr.enable()
for _ in range(500):
    valid = np.flatnonzero(env.action_masks())
    obs, r, term, trunc, info = env.step(int(valid[rng.integers(len(valid))]))
    if term or trunc: obs, info = env.reset()
pr.disable(); s = io.StringIO(); pstats.Stats(pr, stream=s).sort_stats('cumulative').print_stats(10); print(s.getvalue())"
```

Measured result (500 steps, 2026-07-24, training live in background so
absolute times are pessimistic but ratios hold):
`can_play_card` (sts2_env/core/combat.py:1163) was called 10,237 times
(~20x per step) for ~36% of cumulative step time, and `action_masks` ran
~2x per step (once inside `step()`'s info, once by the caller). These are
exactly the spec's Phase 5 targets (mask caching, `can_play_card`
memoization). If you re-profile after Phase 5 lands, expect these ratios to
change — re-run the snippet rather than quoting this paragraph.

### 4.3 Budget math

Wall-clock forecast = planned steps / end-to-end fps. At the measured
~1200–1300 fps, the G1 budget (20M steps) is ~4.5 h and the full ~200M-step
ladder is ~2 days; the spec's Phase 5 throughput work targets 3–3.5k fps
(~17–20 h for 200M). House rule from the spec: if the target fps is missed,
re-benchmark and cut the step budget (to ~120M), never assume. Disk: rich
checkpoints are ~103 MB each (verified: best_model.zip = 102,882,666 bytes),
every 250k steps → ~2.2 GB per 5M steps + sidecars; prune old `ckpt_*.zip`
before multi-stage runs (what-lands-where details: sts2-run-and-operate).

## 5. Observation-layout validation

The rich obs is a flat vector whose meaning lives entirely in module-level
offset constants. A wrong offset silently corrupts training (spec risks
item 5) — validate, don't trust.

### 5.1 Ground truth: the segment table

`segment_table()` (sts2_env/gym_env/rich_observation.py:252) is the
canonical (name, offset, size) map. Print it after ANY obs-related change:

```powershell
.venv\Scripts\python.exe -c "import sts2_env.gym_env.rich_observation as ro; print(ro.RICH_OBS_SIZE); [print(f'{n:14s} off={o:5d} size={s}') for n,o,s in ro.segment_table()]"
```

Verified output (2026-07-24, HEAD fe25668) — RICH_OBS_SIZE = 4184:

| segment | offset | size | notes |
|---|---|---|---|
| ids_hand | 0 | 10 | raw integer CardId indices (embedding block starts the vector) |
| ids_potion | 10 | 5 | raw potion ids |
| ids_boss | 15 | 1 | raw boss id |
| hand_scalars | 16 | 120 | 10 slots x 12 scalars (order at rich_observation.py:157-160) |
| pile_bags | 136 | 1758 | 3 piles x NUM_CARD_IDS (586), counts / 5.0 |
| pile_sizes | 1894 | 3 | |
| player_core | 1897 | 10 | hp, energy, pending_choice flags, ... |
| player_powers | 1907 | 293 | full PowerId amounts / 20 |
| necro | 2200 | 17 | Osty 4 + Souls-per-zone 4 + 3 ally slots x 3 |
| enemies | 2217 | 1575 | 5 x (7 core + 15 intents + 293 powers) |
| relics | 3792 | 309 | RelicId binary + count |
| potion_flags | 4101 | 5 | usable flags aligned with ids_potion |
| run | 4106 | 78 | act/floor/gold/keys/deck-agg/act-slots/map lookahead/phase/ascension |

**Do not quote the docstring's inline counts** (582 cards / 282 powers /
295 relics etc.) — they are stale comments; the enums grew to 586/293/308
and every offset is computed, so the table above (from a live import) is
the only citable source. The `# 282`-style inline comments at
rich_observation.py:182-226 are similarly stale.

### 5.2 The segment tests

```powershell
.venv\Scripts\python.exe -m pytest tests\test_rich_observation.py -q
```

26 tests, ~0.6 s (verified green 2026-07-24). They pin: segments are
contiguous and cover the vector exactly, the ID block is first (the
`RichFeaturesExtractor` slices it for embeddings), sizes derive from live
enums, boss/potion vocabs fit their padded embedding tables, the run env
zeroes combat segments out of combat (and vice versa), and combat-env vs
run-env layout identity (`test_obs_layout_identical_to_combat_env`) — the
invariant that makes weight transfer work.

Rules when the obs changes (e.g. the spec's Phase 2 deck bag):

1. Add new offset constants and extend `segment_table()` in the same commit.
2. Extend `tests/test_rich_observation.py` (contiguity/cover tests will
   fail loudly if you forget — that is the point).
3. The extractor asserts `obs_size == RICH_OBS_SIZE`
   (sts2_env/train/policy.py:61) — a size bump invalidates every existing
   checkpoint (from-scratch retrain; the spec accepts this). Never load an
   old checkpoint into a resized obs.
4. Obs changes are training-pipeline changes: full gate per
   sts2-change-control (full suite — 5290 collected as of 2026-07-24 — plus
   a smoke rollout) before any long run.

## 6. PBRS: the invariance argument and what would falsify it

### 6.1 Status first (date-stamped, verify before relying on this)

As of 2026-07-24 (HEAD fe25668) PBRS is **adopted doctrine but NOT
implemented**. The committed reward (`sts2_env/gym_env/reward_config.py`)
is still the legacy shaping set, now at a constant scale:

| Term | Value | Notes |
|---|---|---|
| win / death | +1.0 / −1.0 | terminal, never scaled |
| truncation | 0.0 | new field (fe25668); timeout is not a death, value net bootstraps |
| sim_error terminal | 0.0 | forced-loss-from-bug, tagged in info, not a death |
| act_completion | +0.25 per act | x shaping_scale |
| floor | +0.004 per floor | x shaping_scale |
| combat_hp_retention | +0.05 · hp_end/hp_start per combat win | x shaping_scale |
| shaping_scale | constant 1.0 (train) / 0.0 (eval) | the win-rate anneal is deleted |

The PBRS replacement is Phase 1 of docs/TRAINING_REVAMP_SPEC.json
(reward_spec): delete the three shaping terms, add
F(s,s') = γ_shape·Φ(s') − Φ(s) every step with γ_shape = 0.997 (== the PPO
gamma), Φ = 0 at terminal, and
Φ(s) = 0.45·progress + 0.30·effective_hp + 0.20·enemy_down (each in [0,1];
exact definitions in the spec). Whether Phase 1 has landed since: check
`git log --oneline -- sts2_env/gym_env/reward_config.py` and look for a
`potential(` method.

### 6.2 The invariance argument (why PBRS "cannot be farmed")

For any potential function Φ over states with Φ(terminal) = 0, the shaped
return of an episode s₀…s_T telescopes:

  Σₜ γᵗ·F(sₜ, sₜ₊₁) = Σₜ γᵗ·(γ·Φ(sₜ₊₁) − Φ(sₜ)) = γᵀ·Φ(s_T) − Φ(s₀) = −Φ(s₀)

The shaping contribution to the return is a constant of the start state,
independent of the policy — so argmax over policies is unchanged
(Ng, Harada & Russell, 1999). Contrast the legacy terms: `floor` pays for
entering rooms (routing bias), `act_completion` double-counts progress, and
`combat_hp_retention` pays for over-blocking and punishes correct HP/Osty
spends — each is a farmable proxy. Under PBRS no state-visiting loop can
mint reward: revisiting a state nets Φ − Φ = 0.

### 6.3 What would falsify or void the invariance HERE (checklist)

Each item is a concrete implementation hazard for this codebase; when
Phase 1 lands, audit against all six:

1. **γ_shape ≠ training gamma.** The telescoping requires the shaping
   discount to equal the MDP discount (PPO `gamma=0.997` at HEAD,
   train_necrobinder.py:267). If someone retunes PPO gamma without touching
   γ_shape, F silently becomes non-potential. Guard: read both from one
   constant.
2. **Φ(terminal) ≠ 0.** If the terminal transition emits γ·Φ(s_T) − Φ with
   Φ(s_T) > 0 (e.g. the win screen still has full effective_hp), the agent
   is paid for the terminal state's potential on top of +1 — a real
   incentive distortion near the end. Guard: explicit `phi = 0.0 if
   terminated else ...` and a unit test.
3. **Truncation leaks potential.** A truncated episode ends at a
   non-terminal state, so the telescoped sum is γᵀ·Φ(s_T) − Φ(s₀) ≠ −Φ(s₀).
   With truncation reward 0.0 + bootstrapping this is mostly benign, but a
   policy that learns to stall at high-Φ states until timeout is the
   farming loop that remains. Watch `truncation_rate` in eval history; a
   climb after PBRS lands is the falsifying observation.
4. **Φ not a function of (augmented) state.** `enemy_down` "carried at its
   last value between combats" is legitimate only because the carried value
   is part of the sim state the value net can see. If Φ ever reads
   action history or RNG internals not represented in the obs, invariance
   formally survives but the value net can no longer represent V and
   explained_variance will tank — treat that as the symptom.
5. **Function-approximation bias.** Invariance is exact only at the optimum;
   with finite budget and a neural policy, a badly scaled Φ steers the
   search path. This is expected and accepted (spec risks item 3) — the
   required control is the G1 ablation: PBRS-on vs PBRS-off (shaping 0),
   same seeds/protocol, CI-separated eval win rates. If shaping-on trains
   to a CI-separated LOWER final eval than sparse, Φ's scaling is wrong.
6. **Eval with shaping on.** Any eval where shaping_scale ≠ 0 reports the
   shaped surrogate, not the objective. `run_eval` already builds
   shaping_scale=0 envs; keep it that way and state it in every claim.

Verification recipe once implemented — the telescoping unit test: roll any
episode, sum the emitted F terms discounted by γᵗ, and assert equality with
γᵀ·Φ(s_T) − Φ(s₀) within float tolerance; plus a terminal-Φ-is-zero test.
Write both into `tests/test_rich_observation.py`'s RewardConfig test class
or a new test file (then the full gate: sts2-change-control).

## 7. Ablation discipline

The house evidence bar (one mechanism must explain all observations;
predictions before runs) is sts2-research-methodology's topic. The
measurement mechanics that make an ablation valid are this skill's:

1. **One variable per arm.** The revamp bundles many changes (reward,
   architecture, optimizer, curriculum); a G1-vs-stage-A comparison
   attributes nothing. To attribute, hold HEAD fixed and toggle exactly one
   thing (e.g. `RewardConfig(shaping_scale=0.0)` vs `1.0` in
   `make_stage_env`).
2. **Cheapest stage that can show the effect.** G1 (A0, acts 1–2) is the
   designated ablation arena (spec risks item 3); a G1 run at current fps
   is hours, G5 is days.
3. **Identical protocol per arm:** same env config, same eval cadence, same
   eval seed block, shaping 0, deterministic, ≥1000 episodes for the final
   comparison; identical training seeds if you run one seed per arm — and
   say so, because single-seed PPO differences of a few points are commonly
   seed noise. If the budget allows only one seed, demand CI separation on
   the 1000-episode eval AND a consistent gap across the eval history, not
   a single crossing.
4. **Decide the acceptance test before launching** (predicted direction and
   size, what CI separation you need) and write it down — in the run's
   output dir next to eval_history.jsonl, so the artifact and its
   pre-registration travel together.
5. **Negative results are results.** A retired idea gets documented with
   its numbers (sts2-research-methodology's lifecycle), not silently
   deleted — the stage-A gate failure is the cautionary example of an
   unstated assumption (that the gate was reachable) never being tested.

## 8. Symptom → cause → check (analysis-specific)

| Symptom | Likely cause | Discriminating check |
|---|---|---|
| Eval win rate flat from first eval to last | task at random-baseline level, blind obs, or frozen optimizer | optimizer stats in log; random baseline on same config; CIs overlap? |
| approx_kl ~1e-8, clip_fraction 0 | lr effectively 0 (pre-revamp linear anneal + resume) | `learning_rate` in the log block; at HEAD lr is constant 2e-4, so this should not recur |
| `mean_floors=0.0`, `deaths_by_act={"0":N}` in every row | pre-revamp combat-env eval artifact (info lacked floor/act) | schema check: no `truncation_rate` field → pre-revamp file |
| ep_rew_mean rising but eval win rate flat | shaped reward being farmed or shaping >> terminal signal | compare against shaping_scale=0 eval; inspect reward terms hit (legacy shaping is farmable, section 6.2) |
| Win rate lower than expected, log shows `failed during phase` | sim_error force-losses (scored 0.0 at HEAD but each hides a bug) | count via Select-String; report to sts2-debugging-playbook |
| truncation_rate climbing across evals | stalling to avoid death (truncation=0.0 incentive) | inspect ep_len_mean trend; cap is max_steps=3000 |
| Benchmark numbers wildly off documented values | live training run stealing CPU/GPU, or laptop on battery | `Get-Process python*`; re-run idle |
| Two evals differ by <5 points at ≤1000 eps | noise | Wilson CIs overlap → not a finding |
| `RichFeaturesExtractor expects obs size 4184, got ...` on load | checkpoint from a different obs version | obs was resized; old checkpoints are dead (section 5.2 rule 3) |
| eval_history.jsonl missing | run launched without evals reached yet, or file deleted | recover the embedded `eval_history` array from the newest sidecar JSON |

## Provenance and maintenance

All facts verified 2026-07-24 against HEAD fe25668 on the live repo, with a
G1 training run in progress. Volatile facts and their one-line re-checks
(run from `C:\Users\motqu\GitHub\sts2-rl-agent`):

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| HEAD fe25668; trainer is G1–G5 full-run-only, constant lr 2e-4, target_kl 0.03, n_steps 1024, gamma 0.997, ent_coef 0.01, n_envs default 24 | `git log --oneline -1` ; `Select-String -Path scripts\train_necrobinder.py -Pattern "LEARNING_RATE|TARGET_KL|n_steps|gamma"` |
| EVAL_FREQ 100k, EVAL_EPISODES 200 (default), EVAL_SEED_BLOCK 10,000,000, CHECKPOINT_FREQ 250k, RUN_MAX_STEPS 3000, output root `output/necrobinder_run` | `Select-String -Path scripts\train_necrobinder.py -Pattern "^EVAL_|^CHECKPOINT|^RUN_MAX|OUTPUT_ROOT"` |
| PBRS not yet implemented; reward = win+1/death−1/trunc 0 + act 0.25/floor 0.004/hp-retention 0.05 at constant scale | `Select-String -Path sts2_env\gym_env\reward_config.py -Pattern "potential|act_completion|truncation"` |
| RICH_OBS_SIZE 4184; segment offsets as tabled; enums 586/293/308 | the segment-table one-liner in section 5.1 |
| 26 obs/reward tests green in test_rich_observation.py | `.venv\Scripts\python.exe -m pytest tests\test_rich_observation.py -q` |
| Full suite 5290 collected (~1 s) | `.venv\Scripts\python.exe -m pytest tests --collect-only -q` |
| Stage A history: 50 evals, best 63.5%, no CI-separated improvement; final block approx_kl 8.03e-9, lr 1.44e-7, clip 0, fps 1307 | the summarizer on `output\necrobinder_a10\A\eval_history.jsonl` ; `Get-Content output\necrobinder_a10_campaign.log -Tail 25` |
| G1 run live: 16 envs, 500 eval episodes, tensorboard on, healthy optimizer (kl ~5e-3, clip ~0.03, entropy −1.1) | `Get-Content output\necrobinder_g1_campaign.log -Tail 40` |
| Checkpoints ~103 MB each, 22 zips in stage-A dir | `Get-ChildItem output\necrobinder_a10\A\*.zip \| Measure-Object -Property Length -Sum` |
| fps ~1150–1307 end-to-end at 16 envs; spec's 4442 env-only vs 1321 learn split; GPU 35% util | re-run `scripts\benchmark_rich_env.py` on an idle machine (numbers drift with hardware state) |
| can_play_card ~20x/step, ~36% of step time; action_masks ~2x/step | the cProfile snippet in section 4.2 (expect change after spec Phase 5 lands) |
| No scipy in venv (Wilson done by hand in shipped scripts) | `.venv\Scripts\python.exe -c "import scipy"` (expect ModuleNotFoundError) |
| Shipped scripts smoke-tested | `.venv\Scripts\python.exe .claude\skills\sts2-analysis-toolkit\scripts\summarize_eval_history.py --wilson 127 200` ; `...\random_baseline.py --env run --acts 1 --episodes 5` |
| TRAINING_GUIDE.md baseline/results tables stale (Ironclad-era) | `Select-String -Path docs\TRAINING_GUIDE.md -Pattern "63.4|4070"` |

If any re-check disagrees with this skill, trust the repo, fix this file,
and re-date the fact.
