# From-Scratch Ablation Study — Necrobinder G1

Goal of the study: pick the training components that produce **steady floor
growth** (positive slope) for a from-scratch agent, then combine the winners
("Rainbow-style") into the config for the long run. Target for the campaign is
50%+ Ascension-10 Necrobinder win rate.

Harness: `scripts/ablation_study.py --through-final` (sequential arms, 3M steps
each, 16 envs, evals at 1M/2M/3M with 200 deterministic episodes, shaping off).
Results accumulate in `output/ablation/results.json` (gitignored); this file is
the tracked record.

## Design

One factor changes per arm relative to A0:

| arm | factor |
|-----|--------|
| A0 | baseline: PBRS shaping, per-slot hand encoding, deck-bag obs, lr 2e-4, kl 0.03, ent 0.01, gamma 0.997 |
| A1 | legacy attempt-6 event shaping instead of PBRS |
| A2 | mean-pool hand encoding instead of per-slot |
| A3 | no deck-bag / archetype observation |
| A4 | + self-imitation (SIL) |
| A5 | ent_coef 0.03 |
| A6 | gamma 0.99 |
| A7 | lr 4e-4 with target_kl 0.05 |

Selection rule, in order: a **level gate** (an arm finishing more than the
noise band below baseline is disqualified outright), then floor **slope** per
million steps, then final mean floors, then act-2 reaches — each required to
clear the noise band.

### Why the level gate exists (a rule bug caught mid-study)

The original rule tested slope *first*. Arm A2 (mean-pool) scored the best
slope in the entire screen, **+0.55/M**, while finishing **2.31 floors behind
the baseline** — it started around 3.4 floors and was merely climbing out of a
hole. On the unpatched rule it would have been adopted into the final config on
that slope alone. A high slope from a much worse starting point is not evidence
of a better configuration, so level is now a gate that precedes slope.

The noise band was also recalibrated from 0.3 to **1.0 mean floors**, because
0.3 was smaller than the measured run-to-run variance (below) and would have
promoted seed noise. Consequence, stated plainly: this one-seed screen can only
detect *large* effects.

## METHODOLOGY CAVEAT (read before trusting any single arm)

The same baseline configuration produced **5.445** mean floors on its first
attempt's 1M eval and **6.68** on its rerun's 1M eval — about **1.2 floors of
run-to-run variance from seed/initialization alone**.

Because the least-squares slope over three equally spaced evals reduces to
`(y_last − y_first) / span`, per-eval noise of ±0.5 floors implies **slope noise
of roughly ±0.35 per million steps**. Consequences:

1. Single-seed slope differences smaller than ~0.35/M are **not significant**.
   The observed A1 (+0.26) vs A0 (+0.05) gap is *inside* that band and must not
   be reported as a win.
2. The harness's ±0.3-floor band for *final floors* is also too tight relative
   to measured variance.
3. Therefore these eight arms are a **screen** (coarse ranking, one seed each),
   not a verdict.

**Confirmation phase (required before the 30M long run):** take the top 2-3
screened configs plus the baseline and run **3 seeds each** with evals every
500k (six points per arm, which roughly halves slope-estimate noise), then
compare means against the empirically measured variance.

A related calibration bug, fixed mid-study: the early-stop rule cut any arm
below 6.0 mean floors at its 1M eval, which killed the *baseline* arm (5.45) and
the mean-pool arm (4.67) before either could show a trajectory — while the
baseline's rerun went on to finish at 6.79. Threshold lowered to 3.0 (only
policies that cannot clear the opening rooms). Archived records:
`output/ablation/results.earlystop6.json`.

## Screen results

Filled in as arms complete. Slopes are per million steps; treat differences
under ~0.35/M as noise per the caveat above.

| arm | status | 1M | 2M | 3M | slope/M | act-2 reaches |
|-----|--------|----|----|----|---------|---------------|
| A0 baseline | completed | 6.68 | 6.62 | 6.79 | +0.05 | 0 |
| A1 legacy shaping | completed | 6.46 | 7.46 | 6.99 | +0.26 | 2 |
| A2 mean-pool | completed | 3.37 | 4.51 | 4.47 | +0.55 | 0 |
| A3 no-deck-obs | INVALID (sleep-truncated, rerunning) | 7.07 | 5.77 | — | — | — |
| A4 SIL | completed | 6.17 | 6.8 | 7.1 | +0.46 | 0 |
| A5 ent 0.03 | completed | 6.89 | 5.79 | 6.04 | -0.42 | 0 |
| **A6 gamma 0.99** | completed | 5.57 | 6.96 | 7.14 | +0.79 | 0 |
| A7 lr 4e-4/kl .05 | completed | 7.1 | 6.65 | 6.52 | -0.29 | 0 |

### Verdicts (patched rule: level gate first, then slope; noise band 1.0 floors)

- **A6 (gamma 0.99) — the only WIN.** Slope +0.79/M vs baseline +0.05
  (+1.47 implied floors over the 2M span, clearing the 1.0 band) *and* the
  best final level of any arm (7.14), so it passes the level gate rather than
  gaming it. Mechanism is plausible: gamma 0.997 implies a ~333-step effective
  horizon against ~500-step episodes, while the binding constraint is combat
  execution (37% of deaths are regular Act-1 fights). A 0.99 discount shortens
  the horizon to ~100 steps, cutting advantage-estimate variance and
  concentrating credit where the deaths happen. `--gamma` moves the PBRS
  shaping discount in lockstep, preserving invariance.
  **Single seed — a candidate for confirmation, not a conclusion.**
- **A2 (mean-pool) — DISQUALIFIED on level**, 2.31 floors behind baseline
  despite the study's second-best slope. The per-slot hand encoding is the one
  architectural choice this screen validates, because the effect is large
  enough to survive the conservative band.
- **A1, A4, A5, A7 — within noise.** A4 (SIL, final 7.10) and A1 (legacy
  shaping, final 6.99) are nominally positive and carry into the confirmation
  phase as secondary candidates. A5 (higher entropy) and A7 (wider steps) are
  nominally negative and are not pursued.

### Incidents that shaped this study

1. **Early-stop mis-calibration** cut the baseline (5.45) and mean-pool (4.67)
   arms at their 1M evals under a 6.0-floor rule, before either could show a
   trajectory — while the baseline's rerun finished at 6.79. Threshold → 3.0.
2. **Selection-rule inversion**: slope was tested before level, which would
   have adopted the disqualified mean-pool arm on its high slope. Level is now
   a gate that precedes slope.
3. **Machine sleep**: Modern Standby for 10h37m killed arm A3 through a
   wall-clock timeout that fired on wake, truncating it at 2 of 3 evals and
   producing a meaningless −1.30 slope. Timeouts now measure process-tree CPU
   time; AC sleep is disabled on the training box.
4. **Stale in-memory decision rule**: when the long-running harness reached its
   FINAL arm it applied the *unpatched* selection rule and launched a combined
   config containing the disqualified mean-pool encoding. Killed before it
   consumed a slot. Lesson: patching a decision rule does not reach an
   already-running process — re-derive selections in a fresh process.

## Confirmation phase

Built and ready; **not launched** — the 8-arm screen must finish first
(launching a second trainer would fight the running one for the 16 GB box).
Harness: `scripts/confirm_phase.py`, seed control: `train_necrobinder.py
--seed`.

### Seed control (`--seed`, default unset = historical unseeded behavior)

One flag fixes everything a run depends on:

| what | how |
|------|-----|
| python / numpy / torch (+CUDA) RNGs | `seed_everything(seed)` before any env or model is built |
| SB3 model: policy init, action space, its own env seeding | `seed=` passed to `MaskablePPO`/`SILAnchoredMaskablePPO` |
| training vec-env | `train_env.seed(seed * 1000)` → env *i* resets on `seed*1000 + i`, consumed at the first `learn()` reset |
| SIL replay sampler | already `np.random.default_rng(self.seed)`, so it follows the model seed |

The stride matters: SB3's own spacing is `seed + i`, so `--seed 0` and
`--seed 1` with 16 envs would share **15 of 16** env seeds — the "independent"
seeds of a seed study would be nearly the same runs. `seed * 1000` gives each
seed a disjoint block.

**Eval isolation.** `--seed` never reaches evaluation. `run_eval` always resets
episode *e* on `EVAL_SEED_BLOCK + e` (10,000,000+), so every seed and every
config is scored on the *same* held-out runs; the callback never passes a
`seed_block` override, and the seed is absent from the env kwargs shared with
the eval env. A seed ≥ 10,000 (which would run the training block into the eval
block) is rejected at startup, before anything spawns.

### What "reproducible" means here, measured

- Same `--seed`, two processes: **bit-identical** initial policy tensors,
  first observation, and the entire first rollout (actions, rewards, buffer
  hashes) — verified on CPU *and* CUDA.
- After the first optimizer update on GPU they drift: the rich policy's CUDA
  backward is nondeterministic (three identical forward/backward passes over
  one batch produced three different gradient hashes — atomicAdd in the
  embedding backward; the CPU path is exact). Two 12,288-step `--seed 0` runs
  ended with policies differing by max |Δ| = 2.6e-3.
- `--deterministic` (opt-in) adds `torch.use_deterministic_algorithms(True)`
  and `CUBLAS_WORKSPACE_CONFIG=:4096:8`: the same paired runs then produced
  **identical** logged rollout/train statistics and bit-identical final policy
  tensors (max |Δ| = 0.0). It is off by default because an op without a
  deterministic CUDA kernel raises at runtime, and a crash mid-run is worse
  than float drift for a study that needs *controlled independent* seeds, not
  bit-exact replay.

### Design

- **Configs**: A0 baseline + the top-2 screened arms by slope, *after the level
  gate* (an arm finishing more than the noise band below baseline is excluded —
  A2 would otherwise be carried in on the best slope in the study while
  finishing 2.31 floors behind). Overridable: `--configs A0,A1,A4`, and
  `FINAL` expresses the screen's combined winner config.
- **Runs**: 3M steps, 16 envs, `--eval-freq 500000` → **6 eval points** per run
  (halves slope-estimate noise vs the screen's 3), 200-episode deterministic
  evals, seeds 0/1/2, strictly sequential (one trainer process at a time).
- **No early stop.** The screen cuts hopeless arms; truncating a confirmation
  seed would bias its slope and destroy the comparison.
- **Hardening reused from the screen harness** (imported, not copied): BLAS
  caps, 8 GB commit-headroom preflight, unconditional `taskkill /T` process-tree
  reap, one retry for zero-eval crashes, incremental
  `output/confirm/results.json` with completed runs skipped on relaunch.

### Statistics and the decision rule

Per seed: least-squares floor slope over the 6 eval points, plus final floors.
Across seeds: mean ± standard error (sd with ddof=1 / √n), and a Welch
unequal-variance t-test vs A0 (implemented in-repo; there is no scipy in this
venv — validated against textbook t critical values and a known worked
example).

Decision, in order:

1. **Level gate** — a config whose mean final floors sits more than the pooled
   SE *below* baseline is disqualified, whatever its slope.
2. **Winner** only if `mean_slope(config) − mean_slope(A0) > pooled SE`
   (`√(SE_c² + SE_b²)`). Otherwise: **not distinguishable**, and the
   simpler/cheaper config (fewest extra flags; ties → A0) is recommended.

n = 3 gives **low statistical power**. The per-seed values and the mean ± SE
are the evidence; the p-values are printed as descriptive context and are not
used as a significance gate. Nothing is promoted on a p-value.

### Exact command (run only after the screen finishes)

```powershell
# from the repo root, with the screen's results.json complete
.venv\Scripts\python.exe scripts\confirm_phase.py
```

Defaults: A0 + top-2 arms × seeds 0,1,2 = 9 runs × 3M steps ≈ **8-9 h**
(measured: 44 min training + ~12 min of evals per run). Useful variants:

```powershell
.venv\Scripts\python.exe scripts\confirm_phase.py --dry-run          # print the plan
.venv\Scripts\python.exe scripts\confirm_phase.py --configs A0,A1,FINAL
.venv\Scripts\python.exe scripts\confirm_phase.py --analyze-only --write-docs
```

Detached (the screen itself had to be relaunched this way — agent-parented runs
die with the agent):

```powershell
New-Item -ItemType Directory -Force output\confirm | Out-Null
Start-Process -FilePath .venv\Scripts\python.exe `
  -ArgumentList 'scripts\confirm_phase.py' -NoNewWindow `
  -RedirectStandardOutput output\confirm\study.log `
  -RedirectStandardError output\confirm\study.err
```

`--analyze-only --write-docs` recomputes the statistics from
`output/confirm/results.json` and rewrites **only** the marked block below.

<!-- confirm-phase:results:start -->

_No confirmation runs recorded yet — the phase has not been launched._

<!-- confirm-phase:results:end -->

## Historical reference (prior lineages, for context)

- From-scratch attempts 5/6 (legacy shaping, older obs/policy): steady growth
  7.3 → 8.5 floors over 5-6M steps.
- Behavior-cloning-initialized attempts 7-10: started near 12 floors but flat,
  0 wins; the KL anchor stopped prior erosion, self-imitation did not unstick
  progress. Lineage abandoned per user directive — slope matters more than
  starting level.
- Death forensics over 120 deterministic episodes: 37% regular Act-1 combats,
  27% elites, 17% Act-1 boss, 17% Act-2 — combat execution is the bottleneck,
  not a single fight.
