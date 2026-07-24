---
name: sts2-run-and-operate
description: >
  Operate the sts2-rl-agent SIMULATOR side: play the sim by hand (combat CLI,
  full-run CLI, web UI), run throughput benchmarks, launch/resume/monitor
  Necrobinder curriculum training (scripts/train_necrobinder.py G1-G5),
  understand checkpoint/sidecar/eval_history anatomy and the output/ layout,
  run TensorBoard, run standalone checkpoint evals with Wilson CIs, and budget
  disk. Load this when the task is "run X", "resume training", "evaluate this
  checkpoint", "why is the log stuck", "how much disk will the ladder eat", or
  "let me play the sim". Do NOT load this for: WHICH run/stage to launch next
  or campaign strategy (sts2-training-campaign), interpreting training metrics
  like approx_kl/entropy curves (sts2-analysis-toolkit), anything touching the
  real game / bridge mod / AutoSlay (sts2-bridge-and-realgame), recreating the
  venv or toolchain (sts2-build-and-env), or landing code changes
  (sts2-change-control).
---

# Running and operating the STS2 simulator

You are operating a from-scratch Python headless simulator of Slay the Spire 2
(beta v0.109.0, plus the ActsFromThePast and Act4Heart mods), its Gymnasium
envs, and a MaskablePPO curriculum trainer. This skill is the operator's
manual: how to start things, watch things, stop things, and account for the
artifacts they produce. Everything here was re-verified against the repo on
2026-07-24 unless marked otherwise.

Repo root (all commands assume you are here):

```
C:\Users\motqu\GitHub\sts2-rl-agent
```

**Non-negotiable rule 1:** always use the venv interpreter, never bare
`python`:

```
.venv\Scripts\python.exe <script> ...
```

**Non-negotiable rule 2:** never run `uv sync` or reinstall over `.venv` — the
lock file is stale and CPU-only; the live venv carries a manually installed
torch 2.11.0+cu128 (CUDA on the RTX 4060 Laptop 8GB). Details and recovery:
see the **sts2-build-and-env** skill.

## Live state as of 2026-07-24 (~12:00 EDT) — re-check before acting

The campaign moves fast and other sessions run concurrently. Observed state
when this skill was written:

| Fact | Value (2026-07-24) | Re-check with |
|---|---|---|
| HEAD | `fe25668` "Phase 0 training revamp: full-run-only ladder (G1-G5), never-halt curriculum, live optimizer" (11:52 EDT) | `git log --oneline -1` |
| Working tree | dirty: modified `sts2_env/content/__init__.py`, `sts2_env/content/descriptions.py`, `sts2_env/web/play_run.py`; untracked `sts2_env/content/preview.py` (a concurrent session's web-tooltip work) | `git status --short` |
| **A G1 training run was LIVE** | launched 11:55 EDT, log `output/necrobinder_g1_campaign.log`, TensorBoard dir `output/necrobinder_g1/G1/tb/`, GPU ~33% util | see "Is a training run live?" below |
| Old stage-A artifacts | `output/necrobinder_a10/` (2.2 GB, 21 combat-only ckpts, **incompatible** with the new trainer — see traps) | `ls output/necrobinder_a10/A` |
| Test collection | 5,290 tests collected (grew from 5,276 at `18a8059` with the dirty tree) | `.venv\Scripts\python.exe -m pytest --collect-only -q tests` |

**Before launching ANY training, check whether one is already live.** Two
trainers contend for the single GPU and the CPU workers and will slow each
other down badly:

```powershell
Get-Process python -ErrorAction SilentlyContinue | Select-Object Id,StartTime,Path
nvidia-smi --query-gpu=utilization.gpu,memory.used --format=csv
```

A live run shows a `.venv\Scripts\python.exe` process (its working set can
read tiny — Store Python venv stub — so trust `nvidia-smi` and log growth, not
memory). Also check whether the campaign log file is still growing. Caveat:
stdout redirected to a file is block-buffered, so the log can lag several
minutes behind reality, especially during an in-process eval (see monitoring).

## Playing the simulator by hand

Three human interfaces exist. All are read-only with respect to training
artifacts and safe to run anytime.

### 1. Single-combat CLI (Ironclad, Act 1 only)

```
.venv\Scripts\python.exe scripts\play_interactive.py
```

No flags. Prompts you to pick an Act-1 encounter (or random), then an
interactive turn loop (`play <card>`, targets, `end`). Ironclad starter deck
only — this is a debugging toy, not the campaign env.

### 2. Full-run CLI (any character, any ascension)

```
.venv\Scripts\python.exe scripts\play_run_interactive.py --character Necrobinder --ascension 10 --seed 42
```

Flags (verified in `sts2_env/cli/play_run.py:567-576`):
`--character {Ironclad,Silent,Defect,Necrobinder,Regent}`, `--ascension N`
(default 0), `--seed N` (default random), `--skip-neow` (start directly on the
map, skipping the Neow-style run-start bonus choice). Plays the entire run:
map, combats, shops, events, rest sites, the Act 4 Heart. Type `h` for help,
`q` to quit.

### 3. Web UI (browser, nicest for inspecting runs)

```
.venv\Scripts\python.exe scripts\play_run_web.py --open
```

Flags (verified in `sts2_env/web/play_run.py:875-884`): `--host` (default
127.0.0.1), `--port` (default 8765), `--open` (launch browser),
`--ascension N` (default 0, per-run overridable). Serves a single-page UI at
`http://127.0.0.1:8765/`; JSON API: `GET /api/state`, `POST
/api/new?character=Necrobinder&ascension=10&seed=42`, `POST /api/action`.
Character is picked per run in the UI. Note (2026-07-24): `web/play_run.py`
has uncommitted edits in the working tree from a concurrent session — if the
UI misbehaves, `git stash list`/`git diff` before assuming a regression.

Jargon: **Necrobinder** is the STS2 character the campaign trains — summons
**Osty** (an ally modeled as a monster that never acts on its own) and spends
**Souls** (a per-zone resource) as its mechanic; **Act4Heart** is the mod
adding a 4-node Act 4 ending at the Corrupt Heart; **AFTP** (ActsFromThePast)
is the mod adding legacy act variants. Mechanics live in
**sts2-game-and-mods-reference**.

## Benchmarks

Two benchmark scripts. House doctrine: run `scripts/benchmark.py` after any
performance-relevant sim change (see **sts2-change-control**).

```
:: legacy combat env, random masked actions, 1000 episodes; prints eps/s + steps/s
.venv\Scripts\python.exe scripts\benchmark.py

:: rich-env throughput: old-vs-rich single env, SubprocVecEnv, short GPU .learn()
.venv\Scripts\python.exe scripts\benchmark_rich_env.py --seconds 10 --n-envs 16 --learn-steps 20000
```

`benchmark.py` takes no flags (`scripts/benchmark.py:8-47`).
`benchmark_rich_env.py` flags (verified `scripts/benchmark_rich_env.py:101-108`):
`--seconds`, `--n-envs`, `--learn-steps`, `--skip-learn`, `--skip-subproc`.
The `.learn()` leg occupies the GPU — do not run it while a training run is
live. Interpreting the numbers (env vs learn vs IPC split) is
**sts2-analysis-toolkit** territory.

## Training: the one production entrypoint

`scripts/train_necrobinder.py` is the only supported trainer. As of commit
`fe25668` (2026-07-24) it implements Phase 0 of the adopted revamp doctrine
(`docs/TRAINING_REVAMP_SPEC.json`, now tracked): a **full-run-only ladder that
never halts**. The old combat-only stages A/B are deleted.

Do NOT use:

- `scripts/train_full_run.py` — verified broken (passes `act_count=` /
  `reward_shaping=` kwargs that `STS2RunEnv.__init__` does not accept, reads a
  nonexistent `env.run_state`). Instant `TypeError`.
- `scripts/train_combat.py` — legacy 131-dim Ironclad path; not the campaign.
- `docs/TRAINING_GUIDE.md` — stale (wrong GPU, wrong action space, broken
  commands). The doc of record is `docs/TRAINING_REVAMP_SPEC.json` +
  `docs/TRAINING_REDESIGN.md`. See **sts2-docs-and-writing** for the full
  staleness map.

### Stage table (telemetry gates, never halts) — `train_necrobinder.py:58-65`

| Stage | Ascension | Acts | Budget (steps) | Gate (telemetry only) |
|---|---|---|---|---|
| G1 | 0 | 1–2 | 20M | 0.80 |
| G2 | 0 | 1–4 | 30M | 0.55 |
| G3 | 4 | 1–4 | 40M | 0.50 |
| G4 | 8 | 1–4 | 50M | 0.45 |
| G5 | 10 | 1–4 | 60M | 0.45 (final) |

Every stage trains the SAME task class — `RichSTS2RunEnv` (full run,
4184-dim observation, `Discrete(157)` actions) — varying only ascension and
`max_act_count` (the env terminates with a WIN once the player clears that
many acts; acts=4 includes the Act 4 Heart). "Gate" is a promotion
*telemetry* threshold (win rate on 2 consecutive evals); it is logged but
**never stops training** — each stage always runs to its budget. This is
deliberate doctrine: the previous ladder hard-halted on an unreachable 85%
combat gate and killed the campaign (see **sts2-failure-archaeology**).
WHICH stage to run next and what the numbers should look like is
**sts2-training-campaign**'s job.

### Hyperparameters (constants in `train_necrobinder.py:67-75, 260-277`, as of fe25668)

| Knob | Value | Note |
|---|---|---|
| Algorithm | MaskablePPO (sb3-contrib 2.9.0), `MlpPolicy` + `rich_policy_kwargs()` | custom features extractor, `sts2_env/train/policy.py` |
| learning_rate | **2.0e-4 constant** | the old linear-decay-to-zero schedule is deleted; `target_kl=0.03` regulates step size |
| ent_coef | **0.01 constant** | the old progress-tied anneal is deleted |
| n_steps / batch / epochs | 1024 / 4096 / 3 | |
| gamma / gae_lambda / clip | 0.997 / 0.95 / 0.2 | |
| device | `cuda` | |
| EVAL_FREQ / CHECKPOINT_FREQ | 100,000 / 250,000 steps | overridable via flags |
| EVAL_EPISODES | 200 default (`--eval-episodes`; the spec's launch command uses 500) | |
| EVAL_SEED_BLOCK | 10,000,000 | eval episode i resets with seed 10,000,000+i |
| RUN_MAX_STEPS | 3,000 | per-episode step cap; timeout = truncation, NOT death |
| Reward | win +1 / death −1 / truncation 0 / sim_error 0; shaping terms (act 0.25, floor 0.004, HP-retention 0.05) × `shaping_scale` fixed at 1.0 in training, 0.0 in eval | `sts2_env/gym_env/reward_config.py` |

**Not yet implemented** (spec Phases 1+, as of 2026-07-24): PBRS
(potential-based reward shaping — provably policy-invariant shaping
`F = γΦ(s′) − Φ(s)`), deck-bag observation, per-slot concat policy, BC
bootstrap, SIL, Act-4 state bank, MCTS. If you see those terms in the spec,
they are adopted doctrine but pending code. Do not describe the current
trainer as PBRS-based.

### Launching

The spec's Phase-0 relaunch command (`docs/TRAINING_REVAMP_SPEC.json`,
`phase1_minimal_changes`, last entry):

```
.venv\Scripts\python.exe scripts\train_necrobinder.py --stage G1 --total-steps 20000000 --n-envs 24 --eval-episodes 500 --progress
```

All flags (verified via `--help`, 2026-07-24): `--stage {G1..G5}` XOR
`--auto`; `--start-stage` (first stage for `--auto`); `--n-envs` (default
24); `--total-steps` (default: the stage's budget — **absolute** target, see
resume traps); `--eval-freq`; `--eval-episodes`; `--checkpoint-freq`;
`--resume`; `--tensorboard` (enables TB logging to `<stage_dir>/tb`);
`--progress` (progress bar); `--output-dir` (default
`output/necrobinder_run`).

`--auto` runs G1→G5 sequentially, each to its budget, warm-starting every
stage from the previous stage's `best_model.zip` via `transfer_weights`
(name+shape-matching tensors; `sts2_env/train/policy.py`). It never halts on
a missed gate.

To keep a campaign log (the convention behind
`output/necrobinder_g1_campaign.log`), redirect stdout+stderr. From cmd or a
`cmd /c` wrapper (avoids PowerShell 5.1's stderr-wrapping quirk):

```
cmd /c ".venv\Scripts\python.exe scripts\train_necrobinder.py --stage G1 --n-envs 24 --eval-episodes 500 --tensorboard > output\necrobinder_g1_campaign.log 2>&1"
```

Note the redirected log is block-buffered and can lag minutes behind.
Expect roughly 1,200 fps at 16 envs early in G1 (observed 2026-07-24);
16 envs × 1024 n_steps = 16,384 steps per rollout iteration (24 envs →
24,576).

Windows-specific launch invariants (violating them breaks multi-env training
only on Windows): env factories are module-level + `functools.partial` so
SubprocVecEnv can pickle them under spawn (`train_necrobinder.py:79-96`), and
the script has the `if __name__ == "__main__":` guard. Keep both if you edit
the trainer (through **sts2-change-control**).

### Resuming — the #1 operational trap

```
.venv\Scripts\python.exe scripts\train_necrobinder.py --stage G1 --resume --total-steps 30000000
```

Semantics (verified `train_necrobinder.py:316-366`):

- `--resume` loads the **latest** `ckpt_*.zip` in `<output-dir>/<stage>/`
  (falls back to `best_model.zip`), restores callback state from its sidecar
  JSON, and calls `model.learn(reset_num_timesteps=False)`.
- **`--total-steps` is an ABSOLUTE timestep target, not an increment.** SB3
  trains until `num_timesteps >= total_timesteps`. Resuming a stage that
  already reached its budget with the same `--total-steps` (or the default
  budget) exits almost immediately having learned nothing. To extend a
  finished stage, pass a **larger** absolute number.
- The old lr-annealed-to-zero resume trap (resume trained at lr≈1e-7 forever)
  is gone since fe25668 — LR is constant — but the absolute-target semantics
  above still hold.
- Resume restores `best_win_rate`, `promotion_streak`, and eval history from
  the sidecar, so telemetry continues seamlessly.
- `--resume` and warm-start are different things: `--resume` continues the
  SAME stage in place; `--auto`'s warm-start copies weights into a FRESH
  optimizer for the NEXT stage.

## Checkpoint anatomy and the output/ layout

Everything lands under the gitignored `output/` (also gitignored: `*.zip`,
`*.pt`, `*.log` — model artifacts never show in `git status`; see
`.gitignore`).

```
output/
  necrobinder_run/            <- DEFAULT_OUTPUT_ROOT for the new G-ladder (nothing in it yet if runs used custom dirs)
  necrobinder_g1/             <- live G1 run's --output-dir (2026-07-24)
    G1/
      ckpt_0000250000.zip     <- checkpoint every 250k steps, ~102.9 MB each
      ckpt_0000250000.json    <- sidecar (see below)
      best_model.zip/.json    <- saved whenever an eval sets a new best win rate
      eval_history.jsonl      <- one JSON line per eval
      tb/                     <- TensorBoard events (only with --tensorboard)
  necrobinder_g1_campaign.log <- operator's stdout redirection (convention)
  necrobinder_a10/            <- OLD stage-A combat-only run (2.2 GB) - incompatible, keep only for archaeology
  combat_ppo/, necro_a10_ppo/ <- 2026-07-23-era 131-dim legacy runs (~426 KB models)
```

Sidecar JSON keys (written by `_write_sidecar`,
`train_necrobinder.py:235-246`): `stage`, `steps`, `best_win_rate`,
`promotion_streak`, `promoted`, `last_eval_step`, `last_ckpt_step`,
`eval_history` (full list of eval dicts). Note: pre-revamp sidecars in
`necrobinder_a10/` additionally carry `shaping_scale`; new ones do not (the
anneal is deleted).

`eval_history.jsonl` line format (new trainer): `win_rate`, `episodes`,
`mean_floors`, `mean_act`, `truncation_rate`, `deaths_by_act` (act-index →
count), `steps`, `wall_s`. Old stage-A lines always show `mean_floors: 0.0`
and `deaths_by_act: {"0": N}` — that was eval blindness on the combat env,
not real telemetry; run-env evals now populate them for real (info carries
`floor`/`act` every step, `sts2_env/gym_env/run_env.py:749-765`).

### Disk budgeting (measured 2026-07-24)

- One checkpoint = 102,882,6xx bytes ≈ **102.9 MB** (the [1024,1024,512]
  torso over 4184-dim obs).
- Checkpoint every 250k steps ⇒ **~412 MB per 1M steps**, plus `best_model`.
- G1 alone (20M) ⇒ ~80 ckpts ≈ **8.2 GB**. The full G1–G5 ladder (200M
  steps) ⇒ ~800 ckpts ≈ **82 GB** if never pruned.
- Free disk on C: was **596 GB** (2026-07-24) — fine, but prune mid-stage
  checkpoints after a stage completes (keep `best_model.zip`, the final
  ckpt, and their sidecars; sidecars are tiny, keep them all). Deleting old
  `output/necrobinder_a10/` reclaims 2.2 GB once its archaeology value is
  exhausted — ask the user first.
- Raising `--checkpoint-freq` is the knob if disk pressure appears.

## Monitoring a live run

1. **Campaign log**: `Get-Content output\necrobinder_g1_campaign.log -Tail 40`.
   SB3 prints a metrics block per iteration; `[eval] ...` lines bracket
   in-process evals; `[ckpt] saved ...` confirms checkpoints; `[promotion]`
   lines are telemetry only. Reading the metrics (approx_kl, entropy,
   explained_variance...) is **sts2-analysis-toolkit**'s domain.
2. **Eval blocks training.** `run_eval` runs in-process on the training
   thread with a fresh single env (`train_necrobinder.py:103-142`): at
   `--eval-episodes 500` on full runs expect the trainer to go quiet for
   minutes per eval (observed 2026-07-24: the live G1 run's log showed no
   eval result 5+ minutes after the `[eval] @ 100,000` line — in-process eval
   plus log buffering). A silent log during `[eval]` is normal, not a hang. Shrinking `--eval-freq` multiplies this cost.
3. **TensorBoard** (tensorboard 2.21.0 is installed in the venv):

```
.venv\Scripts\python.exe -m tensorboard.main --logdir output\necrobinder_g1\G1\tb --port 6006
```

   Only exists if the run was launched with `--tensorboard`.
4. **eval_history.jsonl** is append-only and safe to read while live:

```
Get-Content output\necrobinder_g1\G1\eval_history.jsonl -Tail 5
```

5. **Stopping a run**: Ctrl+C in its terminal is the clean path (SB3
   checkpointing is atomic-enough; you lose at most the steps since the last
   ckpt — resume via `--resume`). If you must kill a detached run, kill the
   parent trainer PID (the SubprocVecEnv workers die with it); never delete
   the stage dir of a run you intend to resume.

## Evaluating a checkpoint (standalone)

House doctrine (all repos): every win-rate claim carries its protocol —
episodes, seed block, deterministic flag, shaping off — and a **final claim
needs ≥1000 episodes with a Wilson 95% CI** (the score interval for a
binomial proportion; at p≈0.5, n=1000 gives ±~3.1%, vs ±~7% at n=200).
200-episode in-trainer evals are steering telemetry, never claims.

This skill ships a smoke-tested evaluator (Wilson CI included):

```
.venv\Scripts\python.exe .claude\skills\sts2-run-and-operate\scripts\eval_checkpoint.py --ckpt output\necrobinder_g1\G1\best_model.zip --stage G1 --episodes 1000
```

- `--stage G1..G5` presets (ascension, max_act_count) to match the stage
  table; or set `--ascension`/`--max-act-count` explicitly. **Final campaign
  claims are measured at A10 full-run (`--ascension 10 --max-act-count 4`),
  not at the training stage's difficulty** (spec `success_metrics`).
- Always evaluates with `shaping_scale=0` (pure sparse reward),
  `deterministic=True`, masked actions, seeds `--seed-block + i` (default
  10,000,000 = the trainer's block; use a different block, e.g. 20,000,000,
  for a held-out eval).
- Reports `win_rate` + Wilson 95% CI, `truncation_rate`, `sim_error_rate`,
  `mean_floors`, `mean_act`, `deaths_by_act`; `--json-out FILE` appends the
  result as a JSON line.
- `--device cpu` avoids contending with a live training run (slower but
  polite).
- Only run-env checkpoints (Discrete(157)) load. The old
  `output/necrobinder_a10/A/*` checkpoints are combat-only Discrete(115) and
  will fail — expected.

Smoke-tested 2026-07-24 with a fresh (untrained) model: 3 episodes, A0
acts-1, cpu — ran clean, 0/3 wins, correct CI. If `sim_error_rate` is
non-zero, the sim threw during those episodes (forced loss scored 0, tagged
`info["sim_error"]`, `run_env.py:337-364`) — grep the console for
`STS2RunEnv.step failed during phase` and route the bug through
**sts2-debugging-playbook** before trusting the eval.

## Symptom → cause → fix (operations)

| Symptom | Cause | Fix |
|---|---|---|
| Resume "finishes" in seconds, no training | `--total-steps` is absolute and the ckpt already reached it | Pass a larger absolute `--total-steps` |
| Training crawls; GPU shows two python users | Second trainer launched while one was live | Check for live runs first (nvidia-smi + process list); stop one |
| `TypeError: __init__() got an unexpected keyword argument 'act_count'` | You ran `scripts/train_full_run.py` (verified broken) | Use `train_necrobinder.py` |
| Trainer commands from `docs/TRAINING_GUIDE.md` fail | Doc is stale (pre-revamp) | Use this skill / `--help`; doc map in **sts2-docs-and-writing** |
| `--stage A` rejected | Stages A–F deleted in fe25668; ladder is G1–G5 | Use `--stage G1..G5` |
| Loading an old `necrobinder_a10` ckpt fails (mask/action shape) | Combat-only Discrete(115) ckpt vs run env Discrete(157) | Expected; those models are archaeology, not warm-start material |
| Log frozen at `[eval] ...` for minutes | In-process eval blocks the training thread; log buffering adds lag | Wait; 500-ep full-run evals take minutes. Check nvidia-smi still shows activity |
| `mean_floors=0.0, deaths_by_act={"0":N}` in old eval lines | Old combat-env eval blindness (info lacked floor/act) | Ignore those fields in stage-A history; run-env evals are real |
| Multi-env training dies only on Windows (pickling error) | Env factory not module-level, or missing `__main__` guard | Restore the `partial(make_stage_env, ...)` pattern (`train_necrobinder.py:79-96, 294-299`) |
| Eval or play wins look too low, console shows tracebacks | Sim bugs force-lose episodes (`sim_error`) | Grep log for `STS2RunEnv.step failed`; file via **sts2-debugging-playbook**. New trainer scores these 0, not −1, but they still aren't wins |
| Agent picks "invalid" actions in a custom eval loop | Forgot masks: `model.predict(obs, action_masks=env.action_masks(), deterministic=True)` | Always pass masks; invalid actions are silently ignored/fallback in the envs |
| `uv sync` ran and CUDA is gone | Stale CPU-only lock clobbered torch 2.11.0+cu128 | Recovery procedure in **sts2-build-and-env**; never `uv sync` here |

## Operating rules that route elsewhere

- **Do not** decide stage order, budgets, or interpret plateau curves here —
  that is **sts2-training-campaign** (doctrine) and **sts2-analysis-toolkit**
  (measurement).
- **Do not** publish or repeat win-rate numbers without the ≥1000-episode
  Wilson-CI protocol above; 95% is an ASPIRATIONAL stretch target, never a
  pass/fail gate (spec `honest_outlook`).
- **Do not** point any of this at the real game — the bridge/AutoSlay side
  (including its rich-model incompatibility and never-run live smoke test) is
  **sts2-bridge-and-realgame**.
- Any edit to the trainer, envs, or reward code — even "just operational" —
  goes through **sts2-change-control** (sim-behavior changes require the full
  5,000+-test suite plus the four parity audit scripts
  `scripts/audit_card_static_metadata.py`, `scripts/audit_card_dynamic_vars.py`,
  `scripts/audit_card_effect_vars.py`, `scripts/parity_reference_audit.py`).

## Provenance and maintenance

Every fact below can drift. Stamp: verified 2026-07-24 (~12:00 EDT), HEAD
`fe25668`, dirty working tree (content/web edits by a concurrent session).

| Fact (2026-07-24) | Re-verify with |
|---|---|
| HEAD fe25668; G1–G5 ladder; combat stages deleted | `git log --oneline -3` |
| Trainer flags & defaults (n-envs 24, output `output/necrobinder_run`) | `.venv\Scripts\python.exe scripts\train_necrobinder.py --help` |
| Stage table & hyperparams (lr 2e-4 const, target_kl 0.03, n_steps 1024, gamma 0.997) | `Read scripts/train_necrobinder.py:49-76, 260-277` |
| Reward terms (win +1, death −1, truncation 0, shaping ×1.0) | `Read sts2_env/gym_env/reward_config.py:34-40` |
| Obs 4184 / actions run 157 / RUN max_steps 3000 | `.venv\Scripts\python.exe -c "from sts2_env.gym_env.rich_run_env import RichSTS2RunEnv,DEFAULT_RUN_MAX_STEPS; e=RichSTS2RunEnv(max_act_count=1); print(e.observation_space.shape, e.action_space, DEFAULT_RUN_MAX_STEPS)"` |
| Live G1 run (11:55 EDT, ~1200 fps at 16 envs, 500-ep evals) — ALMOST CERTAINLY STALE, re-check | `Get-Content output\necrobinder_g1_campaign.log -Tail 20` + `nvidia-smi` |
| Checkpoint size ~102.9 MB; stage-A dir 2.2 GB; 596 GB free | `ls -la output/necrobinder_g1/G1` (or a10/A), `df -h .` |
| Sidecar keys (no `shaping_scale` post-revamp) | `Read scripts/train_necrobinder.py:235-246` |
| `train_full_run.py` still broken (drifted kwargs) | `grep -n "act_count=" scripts/train_full_run.py` then compare `sts2_env/gym_env/run_env.py:255-262` init signature |
| Play CLI/web flags; characters (Ironclad, Silent, Defect, Necrobinder, Regent) | `.venv\Scripts\python.exe -c "from sts2_env.run.run_manager import SUPPORTED_CHARACTER_IDS; print(SUPPORTED_CHARACTER_IDS)"`; `grep -n add_argument sts2_env/cli/play_run.py sts2_env/web/play_run.py` |
| tensorboard 2.21.0 installed; torch 2.11.0+cu128 CUDA OK | `.venv\Scripts\python.exe -m pip list \| findstr /i "tensorboard torch"`; `.venv\Scripts\python.exe -c "import torch; print(torch.cuda.is_available())"` |
| Test collection 5,290 (dirty tree) | `.venv\Scripts\python.exe -m pytest --collect-only -q tests` |
| Spec doctrine (PBRS etc. pending, phases 1–9) | `Read docs/TRAINING_REVAMP_SPEC.json` (`concrete_implementation_plan`) |
| eval_checkpoint.py stage presets match trainer STAGES | diff `STAGE_PRESETS` in `.claude/skills/sts2-run-and-operate/scripts/eval_checkpoint.py` against `scripts/train_necrobinder.py:58-64` — **update the presets if the stage table changes** |
