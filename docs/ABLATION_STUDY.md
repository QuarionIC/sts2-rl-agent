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

Selection metric, in priority order: floor **slope** per million steps, then
final mean floors, then act-2 reaches — each required to clear the noise band.

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
| A0 | completed | 6.68 | 6.63 | 6.79 | +0.05 | 0 |
| A1 | completed | 6.46 | 7.46 | 6.99 | +0.26 | 2 |
| A2 | pending | | | | | |
| A3 | pending | | | | | |
| A4 | pending | | | | | |
| A5 | pending | | | | | |
| A6 | pending | | | | | |
| A7 | pending | | | | | |

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
