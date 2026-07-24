---
name: sts2-training-campaign
description: >
  THE executable Necrobinder Ascension-10 training campaign: current campaign
  state, the adopted G1-G5 full-run ladder (docs/TRAINING_REVAMP_SPEC.json),
  exact launch/resume commands, expected numbers at each gate with
  if-X-then-branch-Y logic, the ranked solution menu (BC bootstrap, SIL, Act-4
  state bank, ExIt, inference-time MCTS) with adoption triggers, fenced wrong
  paths, and the claim/promotion protocol. Load this when deciding WHICH
  training run to launch next, interpreting eval_history.jsonl or a campaign
  log against the gates, diagnosing a win-rate plateau, choosing the next
  intervention, or judging whether a win-rate claim is honest. Do NOT load this
  for generic operating mechanics (TensorBoard, checkpoint anatomy, disk
  layout: sts2-run-and-operate), log-forensics technique and statistics
  (sts2-analysis-toolkit), landing/gating code changes (sts2-change-control),
  historical incident detail (sts2-failure-archaeology), or real-game
  deployment of trained models (sts2-bridge-and-realgame).
---

# Necrobinder A10 training campaign — executable playbook

You are inheriting a live RL campaign. This skill tells you where it stands,
what was decided and why, what to run next, what numbers to expect, and what
never to do again. Everything here was re-verified against the repo on
2026-07-24; volatile facts are date-stamped and re-verifiable via the
Provenance section at the end.

**Campaign goal:** Necrobinder, Ascension 10, full 4-act run ending at the
Corrupt Heart (Act4Heart mod), highest honest win rate, fully local inference.
**95% is an ASPIRATIONAL stretch target** — above top-human full-run rates —
and is never a pass/fail gate. The previous ladder died precisely because it
gated on an unreachable threshold (see §3).

## 1. Glossary (defined once, used throughout)

| Term | Meaning |
|---|---|
| Ladder / stage / gate | The staged curriculum G1→G5. A "gate" is a stage's win-rate threshold — since commit `fe25668` it is **telemetry only** and never halts training. |
| A10 | Ascension 10 (difficulty modifiers stack A1–A10; see sts2-game-and-mods-reference). |
| Heart | The Corrupt Heart, final boss of Act 4 (Act4Heart mod): Beat of Death, Invincible cap, strength snowball. Act **index 3** internally. |
| AFTP | ActsFromThePast mod — act-slot system mixing legacy acts into the run. Both mods are ACTIVE in the campaign config. |
| Souls / Osty | Necrobinder's resource / starting ally monster (an ally-side monster, not a player). Kit details: sts2-game-and-mods-reference. |
| shaping_scale | Single scalar in [0,1] multiplying every shaping reward term (`sts2_env/gym_env/reward_config.py`). Constant 1.0 in training, 0.0 in eval. The old win-rate-driven anneal is deleted. |
| PBRS | Potential-based reward shaping: `F(s,s') = γ·Φ(s') − Φ(s)`, provably policy-invariant (Ng–Harada–Russell 1999). Adopted spec for Phase 1; **not yet implemented** as of 2026-07-24. |
| BC | Behavioral cloning — supervised pretraining on (obs, action) pairs from a scripted heuristic. |
| SIL | Self-Imitation Learning — auxiliary advantage-weighted BC on the agent's own winning episodes. |
| ExIt | Expert Iteration — periodically relabel visited states with search (MCTS) targets and distill back into the net. |
| MCTS / PUCT | Monte-Carlo Tree Search / its polynomial-UCB selection rule; here run over deepcopied `CombatState`s. |
| State bank | Ring buffer of `run_state` snapshots taken at first Act-4 entry, used to reset a fraction of envs directly into Act 4. |
| Warm-start | `transfer_weights(src, dst)` (`sts2_env/train/policy.py:134`) copies every name+shape-matching tensor; `action_net` rows are prefix-copied when the source action space is smaller. |
| Sidecar | The JSON written next to every checkpoint zip carrying `stage, steps, best_win_rate, promotion_streak, promoted, eval_history` — resume state beyond the weights. |
| Wilson CI | Score-interval confidence bound for a binomial win rate; the house-required error bar on every win-rate claim (details: sts2-analysis-toolkit). |
| Truncation | Episode ended by step cap, not death. Since `fe25668` it scores **0.0**, not −1. |
| sim_error | `info["sim_error"]=True`: the env force-ended the run because the simulator raised. Scores 0.0, never −1 (`run_env.py:353-359`). |

## 2. Where the campaign stands (as of 2026-07-24 ~12:00 EDT)

Timeline of ground truth, oldest first:

1. **First full-run attempt: 0% wins after 1M steps** (docs/KNOWN_ISSUES.md #8,
   docs/TRAINING_REDESIGN.md "Why the previous attempt got 0%"): blind 131-dim
   obs, sparse terminal reward, [256,256] MLP, no curriculum. Settled; the rich
   pipeline replaced it.
2. **Stage-A plateau (2026-07-24 morning)**: the old A–F curriculum's Stage A
   (combat-only, Act-1 pool, starter deck, A10) ran 5,005,312 steps in 63.8 min
   and failed its 0.85 gate. Verified from
   `output/necrobinder_a10/A/eval_history.jsonl` (50 evals × 200 episodes):
   first eval **60.5%** @100k, best **63.5%** @2.5M, last **62.0%** @5M, full
   range 57.0–63.5% — flat from the very first eval. Final optimizer state
   (campaign log tail): `learning_rate 1.44e-07, approx_kl 8.03e-09,
   clip_fraction 0, entropy_loss -0.383` — the linear LR anneal had frozen the
   optimizer long before the budget ended. The `--auto` ladder halted itself.
   21 checkpoints (~103 MB each, 2.2 GB) remain in `output/necrobinder_a10/A/`.
3. **Revamp adopted**: `docs/TRAINING_REVAMP_SPEC.json` (tracked since
   `fe25668`) is the campaign doctrine — postmortem, G1–G5 ladder, PBRS,
   architecture fixes, accelerator menu, honest outlook. Changing it is a
   campaign-doctrine change requiring explicit user approval
   (sts2-change-control).
4. **Phase 0 committed**: `fe25668` (2026-07-24 11:52, "Phase 0 training
   revamp") rewrote `scripts/train_necrobinder.py` to the full-run-only G1–G5
   ladder, deleted the combat stages and the LR/entropy anneals, made gates
   telemetry-only, and decoupled truncation/sim_error from death. §4 documents
   the committed behavior.
5. **G1 is LIVE right now** (observed 2026-07-24 12:02): process
   `python.exe -u scripts/train_necrobinder.py --stage G1 --total-steps
   20000000 --n-envs 16 --eval-episodes 500 --tensorboard --output-dir
   output/necrobinder_g1` started ~11:55. Note the operator chose
   `--n-envs 16` (spec suggested 24) and 500 eval episodes (code default 200).
   Early optimizer telemetry is HEALTHY (contrast with item 2):
   `approx_kl 0.0076, clip_fraction 0.044, entropy_loss -1.13,
   explained_variance 0.791, learning_rate 0.0002, fps ~1200,
   ep_len_mean ~135, ep_rew_mean -0.84` at ~98k steps. The first eval
   (500 episodes @100k) had been running **>10 minutes** (started ~11:57,
   still unfinished at 12:07) when this skill was authored — see the
   eval-cost warning in §5.

**Check the live state before acting on anything above:**

```powershell
Get-CimInstance Win32_Process -Filter "Name='python.exe'" | Select-Object ProcessId, CommandLine
Get-Content output\necrobinder_g1_campaign.log -Tail 30
Get-Content output\necrobinder_g1\G1\eval_history.jsonl -Tail 5
```

**Implementation status of the spec's phases** (verified against the tree
2026-07-24 — `sts2_env/search/` does not exist; `scripts/heuristic_agent.py`,
`gen_bc_data.py`, `bc_pretrain.py`, `play.py` do not exist; `policy.py` still
mean-pools the hand; `rich_observation.py` has no deck bag;
`rich_run_env.py` has no `set_ascension_ceiling`/`init_state`):

| Spec phase | Content | Status 2026-07-24 |
|---|---|---|
| P0 | Relaunchable full-run ladder, live optimizer, never-halt, truncation≠death | **DONE** (`fe25668`), G1 running |
| P1 | PBRS rewrite of `reward_config.py`/`rich_run_env.py` | not started |
| P2 | Deck bag + 8 archetype scalars in `rich_observation.py` (grows RICH_OBS_SIZE, currently 4184) | not started |
| P3 | Per-slot hand concat (replace `hand_x.mean(dim=1)`, `policy.py:95`), embed dims 64→96 | not started |
| P4 | CurriculumController pushing ascension/act-count into live envs per eval | not started (committed code = fixed difficulty per stage, warm-start between stages) |
| P5 | Throughput: mask caching, `can_play_card` memoization, IPC shrink | not started |
| P6 | Scripted-heuristic BC bootstrap | not started |
| P7 | SIL + Act-4 state bank | not started |
| P8 | Combat MCTS + meta rollouts (offline ExIt + inference-time) | not started |
| P9 | 1000-ep eval with Wilson CI, acts_reached/heart_reached/heart_win telemetry | partial (mean_act + truncation_rate added; no CI, no heart split, eval runs at the stage's own difficulty) |

## 3. Stage-A postmortem — settled root causes (do not re-fight)

The spec's executive summary names five structural causes; each is closed by a
specific mechanism. Full incident detail: sts2-failure-archaeology.

| Root cause | Evidence | Structural fix | Status |
|---|---|---|---|
| Unreachable gate + hard halt: combat-only Stage A drew uniformly from the full Act-1 pool with a bare starter deck (~27% elite/boss fights a starter can't win), gate 0.85, `if not promoted: break` | 50 flat evals 57–63.5%; `[auto] ... stopping` in `output/necrobinder_a10_campaign.log` | Stages A/B deleted; gates are telemetry; ladder never halts | closed (`fe25668`) |
| Frozen optimizer: linear LR anneal → ~0; resume re-pinned the decayed schedule | `learning_rate 1.44e-07, approx_kl 8e-9, clip_fraction 0` at 5M | Constant `learning_rate=2e-4` + `target_kl=0.03`; `linear_lr` deleted | closed (`fe25668`) |
| Entropy collapse: progress-tied ent anneal 0.01→0.003 | `entropy_loss -0.383` at end of Stage A vs −1.13 fresh | Constant `ent_coef=0.01` | closed (`fe25668`) |
| Hackable/jittery shaping: win-rate-driven `shaping_scale` anneal repriced the reward ~40% per noisy 200-ep eval and zeroed shaping above 80% | old `train_necrobinder.py` anneal (deleted); spec `reward_spec` | Constant shaping now; full PBRS replacement is Phase 1 (pending) | half-closed |
| Truncation and sim bugs scored as death | old `rich_run_env.py`; spec postmortem | `truncation: 0.0` field; `info["sim_error"]` tagged, 0.0 reward (`run_env.py:337-364`, `rich_run_env.py:168-178`) | closed (`fe25668`) |
| Blind policy/obs: mean-pooled hand destroys slot identity while action *i* = "play slot *i*"; deck invisible outside combat (7 aggregate scalars) | `policy.py:95` (still present); spec `architecture_spec` | Phases 2–3 | **open** |

Also settled: an LLM policy was considered and rejected (RTX 4060 Laptop 8GB,
rollout inference throughput — docs/TRAINING_REDESIGN.md "Decision"). Do not
reopen without new hardware.

## 4. The G-ladder as committed (code ground truth, `fe25668`)

Single entrypoint: `scripts/train_necrobinder.py`. Every stage trains the SAME
task — `RichSTS2RunEnv(character_id="Necrobinder", ...)`, Discrete(157)
actions, 4184-dim rich obs — with difficulty on two axes only:

| Stage | Ascension | Acts (`max_act_count`) | Budget | Gate (telemetry) |
|---|---|---|---|---|
| G1 | 0 | 1–2 | 20M | 0.80 |
| G2 | 0 | 1–4 (Heart open) | 30M | 0.55 |
| G3 | 4 | 1–4 | 40M | 0.50 |
| G4 | 8 | 1–4 | 50M | 0.45 |
| G5 | 10 | 1–4 | 60M | 0.45 (final) |

(`STAGES` dict, `train_necrobinder.py:58-64`. `max_act_count=N` ends the
episode with a WIN as soon as `run_state.current_act_index >= N`,
`rich_run_env.py:162-166`.)

Hyperparameters (`build_model`, `train_necrobinder.py:255-277`): MaskablePPO
"MlpPolicy" + `rich_policy_kwargs()` (shared 64-dim card embedding for hand
slots and pile bags, 16-dim potion/boss embeddings, [1024,1024,512] pi/vf
torso); `learning_rate=2e-4` constant, `target_kl=0.03`, `n_steps=1024`,
`batch_size=4096`, `n_epochs=3`, `gamma=0.997`, `gae_lambda=0.95`,
`clip_range=0.2`, `ent_coef=0.01` constant, `vf_coef=0.5`, `device="cuda"`.

Reward at HEAD (still the pre-PBRS shaping — Phase 1 pending): terminal
win +1 / death −1 / truncation 0 / sim_error 0, never annealed; shaping
(× `shaping_scale`, constant 1.0): +0.004/floor, +0.25/act completed,
+0.05 × (hp_end/hp_start) per combat won (`reward_config.py:34-40`,
`rich_run_env.py:136-178`).

Constants: eval every 100k steps, checkpoint every 250k, 200 eval episodes
(default), eval seed block 10,000,000+, `max_steps=3000` per episode,
default output root `output/necrobinder_run` (`train_necrobinder.py:67-75`).

### Launch commands (repo root, Windows)

```powershell
# One stage (budget defaults to the stage's own budget):
.venv\Scripts\python.exe scripts\train_necrobinder.py --stage G1 --tensorboard --progress

# The whole ladder, each stage to budget, warm-starting from the previous best:
.venv\Scripts\python.exe scripts\train_necrobinder.py --auto --tensorboard

# Resume the latest checkpoint of a stage:
.venv\Scripts\python.exe scripts\train_necrobinder.py --stage G1 --resume --total-steps 25000000
```

Flag semantics that bite (all verified in `main()`/`train_stage()`):

| Flag | Semantics | Trap |
|---|---|---|
| `--total-steps` | **ABSOLUTE** timestep target for `model.learn`; resume keeps `num_timesteps` (`reset_num_timesteps=False`) | Resuming a stage that already reached its budget with the default does ~nothing — pass a LARGER absolute value. (The old lr→0 resume freeze is gone — LR is constant now — but the absolute-target semantics remain.) |
| `--total-steps` with `--auto` | Applied to EVERY stage (`total_steps = args.total_steps or stage.budget`) | Don't pass it with `--auto` unless you want the same cap on all five stages. |
| `--auto` | Runs G1→G5 to budget regardless of gates; warm-starts each stage from the previous stage's `best_model.zip` via `transfer_weights` (full tensor copy here — obs/action sizes are identical across G-stages) | Optimizer state is NOT carried across stages (fresh Adam). |
| `--resume` | Picks the lexicographically-last `ckpt_*.zip` (10-digit zero-padded, so ordering is correct), falls back to `best_model.zip`; restores sidecar state | With `--auto`, applies per-stage. If no checkpoint exists it silently starts fresh (message printed). |
| `--start-stage G3` | With `--auto`, begin the ladder at G3 | Without a warm start (`prev_best` is None on the first stage run). |
| `--n-envs` | Default 24 (spec); the live G1 run used 16 | SubprocVecEnv; env factories are module-level partials for Windows pickling. |
| `--eval-episodes` | Default 200; live G1 run uses 500 | See eval-cost warning, §5. |

Output layout per stage: `<output-dir>/<stage>/ckpt_<steps>.zip` + sidecar
`.json`, `best_model.zip` + `best_model.json` (written on every eval
improvement), `eval_history.jsonl` (append-only), `tb/` (with
`--tensorboard`). Operating mechanics (TensorBoard, checkpoint anatomy,
what-lands-where): sts2-run-and-operate.

**Disk math (measured 2026-07-24):** one checkpoint ≈ 103 MB. At the default
250k checkpoint freq the full 200M-step ladder writes ~800 checkpoints
≈ **80+ GB** (596 GB were free on C: at authoring time). Either raise
`--checkpoint-freq` or prune mid-stage checkpoints after each stage —
`best_model.zip` + the final checkpoint are what later stages and analysis
need.

### Monitoring a running stage

```powershell
# Live log (the operator redirects stdout to a campaign log by convention):
Get-Content output\necrobinder_g1_campaign.log -Tail 40 -Wait

# TensorBoard:
.venv\Scripts\python.exe -m tensorboard.main --logdir output/necrobinder_g1

# Eval history with Wilson 95% CI (smoke-tested verbatim in PowerShell):
.venv\Scripts\python.exe -c "import json,math; w=lambda p,n,z=1.96:((p+z*z/(2*n)-z*math.sqrt(p*(1-p)/n+z*z/(4*n*n)))/(1+z*z/n),(p+z*z/(2*n)+z*math.sqrt(p*(1-p)/n+z*z/(4*n*n)))/(1+z*z/n)); rows=[json.loads(l) for l in open('output/necrobinder_g1/G1/eval_history.jsonl')]; [print('steps=%12s win=%5.1f%% CI95=[%.1f%%, %.1f%%] mean_act=%s trunc=%s wall=%ss'%(format(r['steps'],','),100*r['win_rate'],100*w(r['win_rate'],r['episodes'])[0],100*w(r['win_rate'],r['episodes'])[1],r.get('mean_act','-'),r.get('truncation_rate','-'),r.get('wall_s','-'))) for r in rows[-8:]]"
```

Healthy-optimizer markers to check in the SB3 log blocks every time (the
stage-A failure signature is the exact opposite): `learning_rate` pinned at
2e-4, `approx_kl` in ~1e-3..3e-2 (target_kl=0.03 caps it), `clip_fraction`
> 0, `entropy_loss` not collapsing toward 0 (fresh policy ≈ −1.3; stage A
died at −0.38), `explained_variance` > ~0.7 and rising. Deeper log forensics:
sts2-analysis-toolkit.

## 5. Reading evals correctly

`run_eval` (`train_necrobinder.py:103-142`) builds ONE fresh in-process env
with `shaping_scale=0` (pure sparse reward — eval reward IS the objective) and
plays `--eval-episodes` deterministic episodes with action masks, seeds
10,000,000+ep. Per-eval record fields and their exact semantics:

| Field | Meaning | Gotcha |
|---|---|---|
| `win_rate` | Fraction of episodes with `info["won"]` | Includes early-wins at the stage's act cap — see below. |
| `mean_floors`, `mean_act` | From `info["floor"]`/`info["act"]` at episode end | **Act indices are 0-based**: Act 1=0, Act 2=1, Act 3=2, Act 4/Heart=3. A G1 win ends with `act`=2 (finished acts 0 and 1). |
| `deaths_by_act` | Count of non-won episodes keyed by 0-based act index at end | Key `"3"` = died in Act 4 (Heart region) — the closest thing to `heart` telemetry until Phase 9 lands. |
| `truncation_rate` | Episodes ended by the 3000-step cap | Watch this after the truncation=0 change: a rising rate means the agent may be stalling to avoid death (spec risk #4). |
| `wall_s` | Eval wall time | See cost warning below. |

Three honesty rules when quoting these numbers:

1. **Stage win rates are NOT comparable across stages** and none of them is
   "the" campaign number: G1's win rate is measured at A0 over 2 acts. Only a
   G5-config eval (A10, 4 acts, Heart) measures the campaign goal. `run_eval`
   currently evaluates at the stage's OWN difficulty (spec Phase 9 wants an
   additional always-A10 eval; not implemented).
2. **200–500-episode evals steer training; they do not support claims.** At
   62% observed over 200 episodes the Wilson 95% CI is [55.1%, 68.4%] — 13
   points wide (computed from the actual stage-A history with the §4
   one-liner). House rule: any reported result needs ≥1000 eval episodes,
   deterministic, shaping off, seed block stated, Wilson 95% CI attached, and
   aspirational targets labeled aspirational. Protocol details:
   sts2-analysis-toolkit; gating: sts2-change-control.
3. **A random-policy baseline for the current stage config must be measured,
   not assumed.** The only baseline on record — ~63.4% for Act-1 *Ironclad
   combat* (docs/TRAINING_GUIDE.md:88, a stale doc otherwise; see
   sts2-docs-and-writing) — is what exposed stage A's 62% as possibly
   near-random. Before celebrating any early G-number, run the same eval with
   a random masked policy (sts2-analysis-toolkit owns the recipe).

### Eval cost warning (dated observation)

Evals run **in-process on the training thread** and block rollout collection.
On 2026-07-24 the first G1 eval (500 full-run episodes, ep_len ≈ 135 at that
point) had already run >10 minutes against ~83 s to train 100k steps at
~1200 fps — i.e. with `--eval-freq 100000 --eval-episodes 500`, wall-clock can
become eval-dominated, and episode length (hence eval time) GROWS as the agent
survives longer. Knobs: `--eval-freq` (e.g. 250k–500k) and `--eval-episodes`.
Check `wall_s` in `eval_history.jsonl` and re-decide. Changing the eval
protocol that gates/claims are measured on is campaign doctrine → user
approval via sts2-change-control; changing interim-eval frequency to keep
throughput sane is an operational call — log it in the campaign log.

## 6. The decision-gated plan: expected observations and branches

The sequencing decision embedded in the spec: the live Phase-0 G1 run is the
**telemetry baseline**, not the final G1. Phases 1–3 (PBRS, deck-bag obs,
per-slot concat) each change the reward or the obs/feature contract, which
means a from-scratch retrain (spec: "grows RICH_OBS_SIZE -> from-scratch
retrain, acceptable"). Expect the ladder to be restarted from G1 after they
land; do not sink the full 200M budget into the pre-P1-3 model unless the
baseline is shockingly strong.

### G1 (A0, acts 1–2, gate 0.80 telemetry) — LIVE as of 2026-07-24

Expected if healthy: win rate climbing within the first few M steps (the old
0%-full-run failure and the stage-A flatline both showed NO climb from eval
1 — climbing at all is the signal Phase 0 was designed to produce);
`mean_act` → 2.0 on wins; `mean_floors` non-zero (the old combat-env eval
always showed 0.0 — if you see all-zero floors you are reading a stage-A-era
file); optimizer markers per §4. Spec's honest band for A0 acts1-2 once
P1–P3+BC are in: ~0.80 (that is why the gate is 0.80).

| If you observe | Then |
|---|---|
| Win rate flat near 0 for >3–5M steps, `mean_act` < 1 | Agent dying in Act 1. Check `truncation_rate` and grep the campaign log for `STS2RunEnv.step failed` (sim_error losses); if clean, this is the cold-start the BC bootstrap (P6) exists for — pull it forward rather than burning budget. |
| Win rate climbs then plateaus well under gate with healthy optimizer markers | Capability ceiling of the blind architecture — proceed to P1–P3 and restart; the baseline number is the ablation control. |
| `approx_kl` pinned ≈ target_kl 0.03 every update, entropy falling fast | LR too hot for the phase; a constant-LR reduction (e.g. 1e-4) is a training-pipeline change — route through sts2-change-control, do not hand-edit mid-run. |
| `truncation_rate` rising above a few % | Stall-to-avoid-death emerging (truncation=0 risk). Inspect episodes; consider scoring repeated stalls as terminal-0 per spec risk #4. |
| `sim_error` tags appearing in eval or training logs | A simulator bug is being converted to 0-reward episodes. File it per sts2-testing-and-qa and fix via sts2-change-control before trusting win rates. |
| Eval wall time exploding | §5 eval-cost knobs. |

### G2 (A0, acts 1–4, gate 0.55) — first Heart exposure

This stage exists to open the Heart EARLY so Act-4 states are visited long
before G5 (deep-exploration fix) and — once P7 lands — to start filling the
state bank. Expected: win rate drops vs G1 (the run is twice as long and ends
at the Heart); `deaths_by_act` mass shifts to keys `"2"`/`"3"`. If
`deaths_by_act["3"]` dominates while acts 0–2 are mostly cleared, the Heart is
already the binding constraint — that is the designed trigger for P7
(state bank + SIL) and, if `heart` losses stay flat after P7, for P8 search.
If instead the agent rarely REACHES act 3 (`mean_act` < 2.5), the problem is
acts 1–3 deckbuilding, not the Heart: prioritize P2/P3 (deck visibility) over
search.

### G3/G4/G5 (ascension ramp at full act count; gates 0.50/0.45/0.45)

Ascension climbs 0→4→8→10 while the task stays the full 4-act run. Expected:
each step down in gate reflects real added difficulty; warm-start means each
stage should start well above random. Per-reset ascension sampling and the
in-run curriculum controller (P4) are pending — as committed, each stage is a
fixed-difficulty run, so anti-forgetting relies on warm-start only. Watch for
win-rate collapse right after a stage transition (>15–20 points below the
previous stage's plateau suggests the ascension jump is too large — the spec's
per-reset sampler {ceiling 0.6, U(0..ceiling) 0.3, ceiling+2 0.1} is the
designed fix; implementing it is P4).

Spec's honest per-act targets at A10 (labels, not gates): Act 1 >90%,
Acts 1–2 >80%, Acts 1–3 >65%, full-run-to-Heart maximize (~55–70% model-free
+ imitation; ~70–85% with inference-time search; 95% aspirational).

## 7. Solution menu, ranked (adoption triggers and theory obligations)

Ranked by risk-adjusted ROI per the spec's rationale. None are implemented as
of 2026-07-24. Each is a training-pipeline change → sts2-change-control; the
spec already constitutes design approval, but deviations from its parameters
need user sign-off.

| # | Lever | What (spec section) | Adopt when | Theory obligation before trusting results |
|---|---|---|---|---|
| 1 | Scripted-heuristic BC bootstrap (P6) | `scripts/heuristic_agent.py` (rule-based Necrobinder), `gen_bc_data.py` (whole-run worker rollouts, no per-step IPC), `bc_pretrain.py` (masked-CE + value regression → `bc_init.zip` as G1 init). Targets: >75% Act-1-only, >40% Acts-1-2 at RL start | Cold start observed (G1 flat near 0), or before the post-P1-3 ladder restart as a matter of course | Report the scripted agent's OWN eval'd win rate first (it is the floor); BC can imprint heuristic bias — keep BC weight to early G1, rely on the entropy floor + SIL to escape it |
| 2 | Self-Imitation Learning (P7) | Callback: ~50k-transition buffer of WON episodes, advantage-weighted BC every 4 PPO updates, λ_sil=0.1, active from G4 | Wins exist but are rare (win rate ~5–30%) and learning is slow — SIL replays the sparse terminal | Cut λ_sil if `entropy_loss` drops below ~−0.6 (spec); SIL on a shaped reward requires the shaping to be PBRS (invariant) or the buffer returns are biased — another reason P1 precedes P7 |
| 3 | Act-4 state bank (P7) | Snapshot `mgr.run_state` on FIRST Act-4 entry, only if HP>40%; ring buffer cap 2000; `statebank_frac` of resets (0→0.25 across stages) load a banked state via a new `init_state` reset option | `deaths_by_act["3"]` dominates while acts 0–2 are cleared (Heart reached, Heart lost) | Distribution shift: early banks come from a weak policy — refresh the bank as the policy improves, keep frac ≤0.25; win-rate on banked resets is NOT a full-run win rate (never mix them in one metric) |
| 4 | Offline ExIt (P8a) | `sts2_env/search/combat_mcts.py` (PUCT over deepcopied `CombatState`, ~12 determinizations via rng reseed) + `meta_rollout.py` (deepcopied `RunManager`, K=12 playouts for draft/shop/map value targets); every ~5M steps relabel ~20k visited states, distill | SIL+state-bank plateau with `heart_win` flat; or deckbuilding incoherence persists after P2/P3 | Search targets must beat the policy's own value head on held-out states before distilling; RunManager deepcopy cost must be measured first (CombatState ≈ 516 µs per spec; RunManager unmeasured) |
| 5 | Inference-time MCTS (P8b) | Shallow 64–128-sim determinized PUCT on elite/boss/Heart turns only, policy argmax elsewhere (`scripts/play.py`); fully local, latency-free in a turn-based game | The single largest lever toward 80–90%+; adopt when the model-free number has plateaued and is worth amplifying | Report policy-only and policy+search numbers SEPARATELY, same protocol; determinized MCTS can be over-optimistic vs the Heart's snowball — validate on the sim eval before any real-game claim. NOTE: real-game bridge cannot run rich 4184-dim models at all yet (open issue — sts2-bridge-and-realgame) |

Deliberately NOT in the menu (spec `exploration_spec`): RND/count-based
intrinsic reward (adds a non-potential, hackable term; only reconsider —
small, annealed-to-zero, early-A0 — if Act-4 reach stays near 0 after G2),
and hand-authored deck-quality rewards (hackable proxy; deck competence comes
from observability + search targets).

## 8. Wrong paths — fenced

Each of these burned real time or is structurally forbidden. Do not do them;
if one seems necessary, that is a doctrine change → user approval.

| Forbidden move | Why (evidence) |
|---|---|
| Resurrect combat-only stages A/B or gate the ladder on a hard halt | Root cause #1 of the campaign failure (§3). `RichSTS2CombatEnv` still exists in `sts2_env/gym_env/` for other uses; the TRAINER is full-run-only by doctrine. |
| Resume with a linear/decaying LR schedule pinned by `custom_objects` | The stage-A optimizer freeze (`lr 1.44e-07`, `approx_kl 8e-9`). Constant LR + target_kl is doctrine. |
| Resume without raising `--total-steps` past the completed budget | `--total-steps` is ABSOLUTE; `model.learn` exits ~immediately. Symptom: a "finished" stage seconds after resume. |
| Evaluate with shaping on, or hand-roll an eval without action masks | Shaped eval reward ≠ objective; unmasked `model.predict` picks invalid actions (envs silently ignore/substitute them — `rich_combat_env.py:324-328`, `run_env.py:514-518` — so the run "works" and the number is garbage). Always `shaping_scale=0` + `action_masks=`. |
| Publish/compare 200-ep win rates as results | ±6–7 points at 95% CI (§5). ≥1000 episodes + Wilson CI for claims. |
| Score truncation or sim_error as death (or revert `fe25668`'s decoupling) | Silent losses depressed every pre-revamp metric; sim bugs must surface as `sim_error`, not as agent failure. |
| Compare win rates across stages or against TRAINING_GUIDE.md tables | Different (ascension, act-count) = different task; TRAINING_GUIDE.md figures are Ironclad/old-env and the doc is stale throughout (sts2-docs-and-writing). |
| Use `scripts/train_full_run.py` or `scripts/train_combat.py` | `train_full_run.py` is VERIFIED BROKEN (passes `act_count=`/`reward_shaping=` kwargs `STS2RunEnv.__init__` no longer accepts; reads nonexistent `env.run_state`). `train_combat.py` is the legacy 131-dim Ironclad path. `train_necrobinder.py` is the only campaign entrypoint. |
| Add reward terms outside the PBRS potential (post-P1) | Non-potential terms reintroduce reward hacking; the spec's non-hackability argument (telescoping to a policy-independent constant) only holds for pure F=γΦ(s')−Φ(s). Ablation duty: sts2-analysis-toolkit. |
| `uv sync` / reinstall over `.venv` to "fix" a training issue | Stale CPU-only lock would replace cu128 torch and silently wreck throughput. Environment repair: sts2-build-and-env. |
| Launch after code changes without the test gate | Sim/env/reward/obs changes require the FULL test suite green (~5.3k tests; plus parity audits when sim behavior moved) BEFORE burning GPU-hours: sts2-change-control. |

## 9. Validation and promotion protocol

Before ANY relaunch after code changes (classification and full gate:
sts2-change-control):

1. Full suite green: `.venv\Scripts\python.exe -m pytest tests\ -q`
   (5,290 collected in the 2026-07-24 working tree; the count drifts — trust
   the collect command, not this number).
2. If obs/feature code moved (P2/P3): run the rich-observation segment tests
   and a shape/logit smoke test (build the model, one forward pass, assert
   finite logits and the `RichFeaturesExtractor` size assert passes) — a wrong
   offset silently corrupts training (spec risk #5). Recipes:
   sts2-testing-and-qa.
3. If reward code moved (P1): unit-test the telescoping property on a recorded
   episode (sum of F over the episode ≈ γ^T·Φ_T − Φ_0) before trusting any
   curve.
4. If throughput code moved (P5): `.venv\Scripts\python.exe
   scripts\benchmark_rich_env.py` before/after; re-scale the step budget to
   measured fps rather than assuming (spec: cut to ~120M if 3–3.5k steps/s is
   missed).
5. Log the launch command + git SHA in the campaign log file. The live-run
   convention is one log per campaign attempt at `output\<name>_campaign.log`.

A stage "promotion" (threshold on 2 consecutive evals) is telemetry recorded
in the sidecar (`promotion_streak`, `promoted`) — it never halts or advances
anything by itself. Advancing the ladder = the `--auto` loop or an explicit
decision. Requires user approval: changing stage gates/budgets/reward
spec/eval protocol (campaign doctrine), and ANY externally-visible win-rate
claim, which must carry: episodes (≥1000), seed block, deterministic flag,
shaping=0, eval config (ascension/act count), git SHA, Wilson 95% CI.

## 10. Honest ceiling (say this in every status report)

From the spec's `honest_outlook`, unchanged by anything observed since:
model-free + imitation realistically lands A10 full-run-to-Heart at
**~55–70%**; inference-time search plausibly **~70–85%**; irreducible RNG
variance (unwinnable draws/maps) plus the Heart's structural anti-synergy
against Necrobinder (Beat of Death punishes many-cheap-cards; Invincible caps
burst; strength snowball punishes long fights) likely cap a fully-local agent
below 95%. **95% is aspirational** — treat any plan that assumes it as a
red flag, and report per-act rates with CIs instead of chasing one headline
number.

## Provenance and maintenance

Author basis: repo at commit `fe25668` (2026-07-24 11:52 EDT), working tree
carrying unrelated uncommitted web/content changes; a G1 training run was live
during authoring (started ~11:55 EDT, first eval unfinished at 12:02). Facts
most likely to drift: the live-run state (§2 item 5), the phase-status table
(§2), eval history numbers, and disk usage. One-line re-verification for each
load-bearing fact:

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| HEAD is `fe25668`, Phase-0 revamp committed | `git -C C:\Users\motqu\GitHub\sts2-rl-agent log --oneline -3` |
| G1 run live / finished / superseded, exact flags | `Get-CimInstance Win32_Process -Filter "Name='python.exe'" \| Select-Object CommandLine` and `Get-Content output\necrobinder_g1_campaign.log -Tail 20` |
| Stage table G1–G5, gates, budgets, hyperparams | `Get-Content scripts\train_necrobinder.py -TotalCount 80` and lines 255–277 |
| Gates never halt; promotion is telemetry | `Select-String -Path scripts\train_necrobinder.py -Pattern "NEVER|never halts|promotion"` |
| Reward terms and truncation/sim_error semantics | `Get-Content sts2_env\gym_env\reward_config.py` ; `Select-String -Path sts2_env\gym_env\rich_run_env.py -Pattern "truncation|sim_error"` |
| Phases 1–3 still pending (mean-pool at policy.py:95; RICH_OBS_SIZE 4184; no deck bag) | `Select-String -Path sts2_env\train\policy.py -Pattern "mean\(dim=1\)"` ; `.venv\Scripts\python.exe -c "from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; print(RICH_OBS_SIZE)"` |
| Phases 6–8 still pending | `Test-Path sts2_env\search, scripts\heuristic_agent.py, scripts\bc_pretrain.py` |
| Stage-A postmortem numbers (60.5→63.5→62.0, 50×200-ep evals) | run the §4 one-liner against `output/necrobinder_a10/A/eval_history.jsonl` |
| Stage-A optimizer freeze (lr 1.44e-07, kl 8e-9) | `Get-Content output\necrobinder_a10_campaign.log -Tail 30` |
| Checkpoint size ~103 MB / disk free | `Get-ChildItem output\necrobinder_a10\A\best_model.zip \| Select-Object Length` ; `Get-PSDrive C` |
| Test count (5,290 collected 2026-07-24, drifts) | `.venv\Scripts\python.exe -m pytest tests --collect-only -q \| Select-Object -Last 1` |
| Spec is the adopted doctrine and tracked | `git -C C:\Users\motqu\GitHub\sts2-rl-agent log --oneline -- docs/TRAINING_REVAMP_SPEC.json` |
| `train_full_run.py` still broken/legacy | `Select-String -Path scripts\train_full_run.py -Pattern "act_count=|reward_shaping="` vs `STS2RunEnv.__init__` signature (`sts2_env\gym_env\run_env.py:255`) |

If any check above disagrees with this skill, the repo wins — update this
file (docs-class change per sts2-change-control) and re-stamp the date.
