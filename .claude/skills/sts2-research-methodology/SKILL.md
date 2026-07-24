---
name: sts2-research-methodology
description: >
  How research is done in sts2-rl-agent: the evidence bar a hypothesis must
  clear before it becomes an accepted result, the predict-numbers-before-running
  discipline, and the idea lifecycle (candidate -> spec'd -> phase-gated
  experiment -> adopted or documented retirement), with the Stage-A plateau
  misdiagnosis and the TRAINING_REVAMP_SPEC as worked examples. Load this when
  you are forming a hypothesis about why training/behavior looks the way it
  does, proposing a new training/algorithm/reward idea, deciding whether a
  result is real, writing or reviewing a design spec, or tempted to re-open a
  settled question. Do NOT load this for: executing the current campaign plan
  or choosing launch commands (sts2-training-campaign), computing statistics or
  reading SB3 logs mechanically (sts2-analysis-toolkit), the full incident
  chronicle (sts2-failure-archaeology), landing/approving changes
  (sts2-change-control), or what to claim in a paper (sts2-research-frontier).
---

# sts2-research-methodology — from hunch to accepted result

This skill is the project's scientific method. It exists because this repo has
already paid, twice, for skipping it: a 1M-step full-run training that got 0%
because nobody checked the agent could *see* the game, and a 5M-step Stage-A
run whose "63.5% ceiling" turned out to be three separate mechanisms stacked on
top of each other, one of which (an unreachable promotion gate wired to a
hard halt) silently killed the whole campaign. Both are dissected below as the
canonical worked examples. Follow the procedure here and neither happens again.

Scope boundary: this skill owns *how you reason and what counts as evidence*.
Which experiment to run next is `sts2-training-campaign`. How to compute the
statistics and read the logs is `sts2-analysis-toolkit`. How to land the code
is `sts2-change-control`.

## 0. Jargon used below (defined once)

| Term | Meaning here |
|---|---|
| Stage A / the old ladder | The pre-2026-07-24 combat-only curriculum stage (Necrobinder starter deck vs the full Act-1 pool, A10, 85% promotion gate). Deleted in commit `fe25668`. |
| G-ladder (G1–G5) | The current full-run-only curriculum (`scripts/train_necrobinder.py:58-65`): difficulty moves on two axes (ascension 0→10, act count 2→4); promotion thresholds are telemetry, never halts. |
| PBRS | Potential-based reward shaping, `F(s,s') = γ·Φ(s') − Φ(s)`; provably policy-invariant (Ng–Harada–Russell 1999), so it cannot be reward-hacked. Planned Phase 1 of the revamp (not yet implemented as of 2026-07-24). |
| BC / SIL / ExIt | Behavior cloning from a scripted heuristic; Self-Imitation Learning (advantage-weighted BC on the agent's own won episodes); Expert Iteration (periodically relabel visited states with MCTS targets). All *planned accelerators* in the revamp spec, none implemented as of 2026-07-24. |
| Wilson CI | Wilson score 95% confidence interval on a win rate. At p≈0.5: ±~3.5% for 200 episodes, ±~1.6% for 1000. House rule: final claims need ≥1000 episodes. |
| shaping_scale | Single scalar multiplying all shaping reward terms (`sts2_env/gym_env/reward_config.py:40`). 1.0 in training, **0.0 in every eval** — eval reward must equal the true objective. |
| Souls / Osty | Necrobinder's core resource / her skeleton-pet ally. Mentioned only as diagnosis context here; mechanics live in `sts2-game-and-mods-reference`. |
| Parity | Bit-exact agreement with the decompiled C# (`decompiled_v0.109.0/`). Operational meaning and audit tooling live in `sts2-parity-discipline`. |

## 1. The evidence bar

A hypothesis graduates to an *accepted result* in this repo only when it passes
all five tests. This is house doctrine (user-stated, all repos): only real,
experiment-backed results; no spot-check sign-offs.

**Test 1 — One mechanism explains ALL observations, including the negatives.**
A mechanism that explains why the metric is bad but not why it was *already*
bad at step 100k is not the (whole) answer. The Stage-A postmortem (section 3)
is the canonical case: "LR annealed to zero" was true, verified, and still not
the primary mechanism, because the win rate was flat from the very first eval
when LR was still ~2.4e-4. Keep listing candidate mechanisms until every
observation — especially the ones that *don't* fit your favorite — is covered.
Write the observation-vs-mechanism table explicitly (format in section 3).

**Test 2 — The hypothesis predicted numbers before the run.**
Before launching any experiment, write down what you expect to see, as numbers
with rough timing, and what observation would *falsify* the hypothesis. The
revamp spec does this throughout — e.g. Phase 0: "expect the agent to reach
Act 2 and win-rate to climb within the first few M steps; watch
eval_history.jsonl for non-zero mean_floors" (`docs/TRAINING_REVAMP_SPEC.json`,
`phase1_minimal_changes`, last entry). A run whose outcome you would have
accepted either way is not an experiment; it is a ritual.

**Test 3 — It beats the relevant baseline, measured not remembered.**
Always know what "no learning at all" scores. The single most clarifying number
in the Stage-A postmortem was the random baseline: ~63.4% win on Act-1 Ironclad
combats (`docs/TRAINING_GUIDE.md:88` — that doc is otherwise stale, but this
number reframed everything). Stage A's "best 63.5%" was, to within noise, *the
random policy's score on a cousin task* — i.e. the headline metric contained
almost no evidence of learning. If a Necrobinder-A10 random baseline for the
current task hasn't been measured, measure it first; `sts2-analysis-toolkit`
owns the how.

**Test 4 — It survives an assigned adversarial refutation.**
Before adopting a diagnosis or design, someone (a second agent, a second
session, or you in an explicitly adversarial pass) is assigned to *kill* it:
find an observation it cannot explain, a cheaper mechanism that explains the
same data, or a hidden confound. This is how the revamp was produced: three
full competing designs (A: pure model-free PPO, B: search/ExIt-centric,
C: imitation-centric) were drafted and each critiqued against the others'
strengths — the adopted plan's `rationale` field records why each individual
design *fails* alone ("Design A ... its own authors cap it at ~60-72%";
`docs/TRAINING_REVAMP_SPEC.json`, `rationale`). A design that has never been
argued against by a motivated critic is a candidate, not a decision.

**Test 5 — The claim carries its protocol.**
Every win-rate or performance number stated anywhere (docs, commit messages,
conversation) carries: episode count, seed block, deterministic flag,
shaping_scale=0, and — for final claims — ≥1000 episodes with a Wilson 95% CI.
Aspirational targets (the 95%) are always labeled aspirational, never used as
pass/fail gates. Wording matters: "63.5% best of 25 noisy 200-episode evals"
and "63.5% win rate" are different claims; the first is what Stage A actually
had.

Quick CI check (smoke-tested 2026-07-24):

```powershell
C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe -c "from math import sqrt
n, wins = 1000, 620
z = 1.96; p = wins / n; d = 1 + z*z/n
c = (p + z*z/(2*n)) / d
hw = z * sqrt(p*(1-p)/n + z*z/(4*n*n)) / d
print(f'win_rate {p:.3f}  Wilson 95% CI [{c-hw:.3f}, {c+hw:.3f}]')"
# -> win_rate 0.620  Wilson 95% CI [0.590, 0.650]
```

Note what that interval means in practice: at 200 episodes two evals of the
same frozen policy can differ by ~7 points from seed noise alone. Promotion
telemetry therefore requires the threshold on **2 consecutive evals**
(`scripts/train_necrobinder.py:215-225`), and no single-eval delta under ~5
points at 200 episodes is evidence of anything.

### Claim ladder (what evidence buys what claim)

| You may say... | Only with... |
|---|---|
| "worth investigating" / "candidate" | any observation + a mechanism sketch |
| "diagnosed: X caused Y" | observation-vs-mechanism table covering ALL observations + survived refutation pass |
| "improvement" (internal, directional) | same eval protocol both sides, non-overlapping Wilson CIs, shaping off |
| "result" (docs, commit message) | ≥1000 eval episodes, deterministic, seed block stated, Wilson CI stated |
| external/publication claim | all of the above + user approval; see `sts2-research-frontier` for what may be claimed as novel |

## 2. Predict numbers before you run (the pre-registration habit)

Before any training launch or experiment, write (in the spec, the campaign
log, or at minimum the launch commit message):

1. **Expected trajectory** — e.g. "win rate should climb from the ~X% baseline
   within the first 2-3M steps; mean_act should exceed 1.0 by eval 5."
2. **Falsifier** — the observation that would make you stop and rediagnose,
   e.g. "if win rate is flat across the first 5 evals, the hypothesis is
   wrong; do not just extend the budget."
3. **Decision trigger** — what result routes to which next action, decided
   *now*, not after seeing the data. The revamp spec pre-registers its biggest
   one: simulator search (Phase 8) is adopted **only if** telemetry shows
   `heart_reached` high but `heart_win` flat (`docs/TRAINING_REVAMP_SPEC.json`,
   phase 8: "adopt ONLY if G4/G5 telemetry shows heart_reached high but
   heart_win flat"). This prevents both premature escalation and endless
   overtraining of a plateaued model-free agent.
4. **Honest ceiling** — the band you actually expect, stated even when it is
   below the aspirational target. The spec's `honest_outlook` bands
   (model-free+imitation ~55-70% full-run; +search ~70-85%; 95% aspirational,
   above top-human) are the template: nobody is later surprised, and a result
   inside the band is a success even though it is not 95%.

Why this is enforced: without pre-registered numbers, every outcome gets
rationalized. Stage A ran 5M steps against an 85% gate that the task structure
made unreachable (a 10-card starter deck vs an Act-1 pool where a large
fraction of fights are elites/bosses — the spec's diagnosis), and because no
one had written "if flat by 1M steps, stop and rediagnose," it burned the full
budget and then hard-halted the ladder.

## 3. Worked cautionary example: the Stage-A plateau (2026-07-24)

Read this once in full; it is the repo's best lesson in diagnosis discipline.
The full incident record lives in `sts2-failure-archaeology`; this section
extracts the *method*.

**Timeline.** Stage A (combat-only, Necrobinder starter deck, Act-1 pool, A10,
rich obs, 85% promotion gate) trained 5,005,312 steps in 63.8 min and failed
promotion. The `--auto` ladder then halted itself
(`output/necrobinder_a10_campaign.log`, tail; old halt code at
`git show 18a8059:scripts/train_necrobinder.py` lines 440-445).

**The observations (all had to be explained).** From
`output/necrobinder_a10/A/eval_history.jsonl` and the campaign log tail:

| # | Observation | Verified where |
|---|---|---|
| O1 | Win rate ~flat for the entire run: 60.5% @100k, best 63.5% @2.5M, 62.0% @5M | eval_history.jsonl first/last lines |
| O2 | Final optimizer stats: approx_kl 8.0e-9, clip_fraction 0, lr 1.44e-7, entropy_loss −0.383 | campaign log, final SB3 block |
| O3 | Every eval line: mean_floors=0.0, deaths_by_act={"0": N} | eval_history.jsonl (every line) |
| O4 | Random baseline on a cousin task (Act-1 Ironclad combats) ≈ 63.4% | docs/TRAINING_GUIDE.md:88 |
| O5 | explained_variance 0.974 at end — the value net modeled *something* well | campaign log, final SB3 block |

**Candidate mechanisms and what each actually explains:**

| Mechanism | Explains | Fails to explain |
|---|---|---|
| M1: Linear LR anneal froze the optimizer (lr→1.4e-7, so late training was dead; a naive `--resume` would train at lr≈0 forever) | O2; why the *last* millions of steps changed nothing | O1 — the plateau existed at 100k steps when lr was still ~2.4e-4 |
| M2: Task structurally near-unwinnable (fixed 10-card starter deck, uniformly sampled Act-1 pool including elites/bosses; those fights are lost regardless of policy) | O1, O4 (score ≈ random because the winnable subset is won cheaply and the rest can't be won), O5 (the value net learned to *predict* the loss it couldn't prevent) | — |
| M3: Eval telemetry blind (combat env never sets info['floor']/['act'], so the diagnostic fields were constants) | O3; why M2 stayed invisible for 50 evals — deaths_by_act could never show "dies to elites" | the plateau itself |

**Resolution.** All three mechanisms were real. M2 is the *primary* mechanism
(it explains the headline observation); M1 is a genuine secondary defect that
would have sabotaged any resume-based fix; M3 is the meta-failure that let M2
hide. The adopted response fixed all three structurally: full-run-only ladder
with the agent drafting its own deck, constant lr 2e-4 + target_kl 0.03, and
run-env eval whose info carries floor/act so `mean_floors`/`deaths_by_act`
are real telemetry (commit `fe25668`, 2026-07-24 11:52; current code
`scripts/train_necrobinder.py:103-142, 263-272`).

**Method lessons (each is a rule now):**

1. **First-found mechanism is not the answer.** M1 was found first (it is the
   loudest signal in the logs) and was *insufficient*. Rule: after finding a
   mechanism, explicitly ask "does this explain the earliest data point?"
2. **A metric that never changes is measuring nothing.** mean_floors=0.0 on
   50 consecutive evals should have been treated as an alarm, not background.
   Rule: any diagnostic field that is constant across all evals is presumed
   broken until shown otherwise.
3. **Compare against the score of no learning.** Without O4, "63.5%" looked
   like partial progress toward 85%. With it, it looked like ~zero learning.
4. **Never wire an aspiration to a halt.** The 85% gate was a guess; the
   `break` was real. The current trainer's promotion thresholds are telemetry
   only and every stage runs to budget (`scripts/train_necrobinder.py:154-155,
   215-225`). Any future proposal to reintroduce a hard gate must route
   through `sts2-change-control` as a campaign-doctrine change.

## 4. The idea lifecycle

Every substantive idea in this project moves through named states. The state
determines what you may claim and where the idea is recorded.

| State | Entry criteria | Artifact of record | Exit |
|---|---|---|---|
| **Candidate** | any plausible mechanism/idea | conversation, KNOWN_ISSUES note, or postmortem bullet | spec'd, or dropped silently (candidates are cheap) |
| **Spec'd** | written design with predicted numbers, falsifiers, decision triggers, honest ceiling, risks | a doc in `docs/` (house template below) | phase-gated experiment approved by the user (campaign-doctrine changes always need user approval — `sts2-change-control`) |
| **Phase-gated experiment** | spec adopted; phases ordered by risk-adjusted ROI; each phase has pre-registered go/no-go telemetry | commits + `output/<run>/eval_history.jsonl` + campaign log | adopted (evidence bar passed) or retired |
| **Adopted** | evidence bar (section 1) passed | committed code + doctrine docs updated | may still be superseded later — by evidence, not vibes |
| **Documented retirement** | falsified, superseded, or ruled out by constraints | written reason in a doc of record (see section 5) | re-opened ONLY with new evidence that the recorded reason no longer holds |

**The worked example done right — the training revamp (2026-07-24):**

1. *Candidate*: Stage-A postmortem produced mechanisms M1–M3 plus a list of
   further defects (mean-pooled hand encoding, deck invisible outside combat,
   hackable/jittery win-rate-driven shaping anneal, truncation scored as
   death).
2. *Spec'd*: three competing full designs were drafted and adversarially
   compared; the synthesis was written as `docs/TRAINING_REVAMP_SPEC.json`
   with phases 0–9, per-phase predicted numbers, pre-registered decision
   triggers, an `honest_outlook`, and `risks_and_mitigations`.
3. *Phase-gated experiment*: Phase 0 (minimal relaunchable changes) was
   implemented and committed as `fe25668` (2026-07-24 11:52 -0400) — full-run
   G-ladder, live optimizer, never-halt, truncation=0.0, sim_error tagged not
   scored as death (`sts2_env/gym_env/rich_run_env.py:164-182`,
   `sts2_env/gym_env/run_env.py:315-364`, `reward_config.py:36`). As of
   2026-07-24 ~12:00 the G1 run is LIVE (`output/necrobinder_g1_campaign.log`;
   healthy optimizer at ~98k steps: approx_kl 7.6e-3, clip_fraction 0.044,
   lr 2.0e-4, entropy −1.13; evals at 500 episodes).
4. *Adoption/retirement of the later phases* (PBRS rewrite, deck-bag obs,
   per-slot concat, BC/SIL/state-bank, search) is decided phase-by-phase by
   the pre-registered triggers — NOT all-at-once. Which phase is next and its
   launch command is `sts2-training-campaign`'s territory.

**House spec template.** New designs of campaign scale follow the field
structure of `docs/TRAINING_REVAMP_SPEC.json` (it is the precedent):
`executive_summary` (diagnosis first, in one breath), `chosen_approach` (and
what it beat), `concrete_implementation_plan` (numbered phases, cheapest-
risk-reduction first), `files_to_change` (absolute paths), a
`phase1_minimal_changes` list that can ship *today*, per-topic specs
(reward/curriculum/architecture/algorithm/exploration/throughput),
`success_metrics` (primary + diagnostic, with eval protocol),
`risks_and_mitigations` (numbered, each with its mitigation), `honest_outlook`
(bands, aspirational labeled), and `rationale` (why the losing alternatives
lose). A spec missing predicted numbers or an honest ceiling is not done.

**Phase ordering rule.** Order phases by risk-adjusted ROI with "first
gradient signal" as the dominant term: the revamp deliberately shipped the
relaunchable spine before any of the higher-upside pieces, because "spending
days before seeing a signal" is itself the failure mode that killed the old
campaign twice (`TRAINING_REVAMP_SPEC.json`, `rationale`).

## 5. Documented retirements — settled battles, do not re-fight

Each row records *why* it was retired. Re-opening any of these requires new
evidence against the recorded reason, routed through `sts2-change-control`.
The fuller stories live in `sts2-failure-archaeology`.

| Retired idea | Reason (recorded where) | Date |
|---|---|---|
| LLM-based policy | RL fine-tuning + rollout inference infeasible on RTX 4060 8GB at required steps/s; purpose-built net meets the same fully-local constraint (`docs/TRAINING_REDESIGN.md:23-33`) | 2026-07-23 |
| Combat-only curriculum stages (A/B) | Structurally unwinnable task (starter deck vs full pool) produced a fake ceiling and zero full-run gradients; deleted (`fe25668`; `TRAINING_REVAMP_SPEC.json` executive_summary) | 2026-07-24 |
| Promotion gate as hard halt | Gate-on-unreachable-threshold + `break` silently killed the campaign (old code: `git show 18a8059:scripts/train_necrobinder.py`, lines 440-445); promotion is telemetry now | 2026-07-24 |
| Win-rate-driven shaping anneal (`max(0, 1 − win_rate·1.25)`) | ~40% reward jitter per noisy 200-ep eval; zeroes shaping exactly where the final push happens (old code: same commit, `_do_eval`; removal: `fe25668`) | 2026-07-24 |
| Linear LR anneal | Froze the optimizer (lr 1.44e-7, approx_kl 8e-9) and made resumes train at lr≈0; replaced by constant 2e-4 + target_kl 0.03 (`train_necrobinder.py:263-272`) | 2026-07-24 |
| Truncation scored as death | Confounds "slow but alive" with loss; now `truncation: 0.0` + `info["truncated"]` (`reward_config.py:36`, `rich_run_env.py:175-178`) | 2026-07-24 |
| Sim-error scored as death | Simulator bugs masqueraded as agent failures in win-rate stats; now tagged `info["sim_error"]`, scored 0.0 (`run_env.py:341-364`) | 2026-07-24 |
| RND / count-based intrinsic bonus (by default) | Adds a non-potential reward term and a hacking surface; curriculum + state bank + imitation are the chosen exploration route. Pre-registered exception: a small annealed RND *may* be enabled in early A0 if `heart_reached` ≈ 0 (`TRAINING_REVAMP_SPEC.json`, exploration_spec) | 2026-07-24 |
| Hand-authored deck-quality reward | Any per-pick bonus is a proxy that can be farmed; deck competence must come from observability + (later) search value targets (`TRAINING_REVAMP_SPEC.json`, reward_spec) | 2026-07-24 |

Note the pattern in the last two rows: a good retirement often records its own
re-opening condition. Prefer that to an unconditional "never."

## 6. Where good ideas have come from (hunting grounds)

Historically verifiable sources of this project's best ideas — check these
first when stuck, in this order of yield:

1. **Postmortems of failed runs.** The 0% full-run failure produced the
   entire rich-observation redesign ("the agent literally cannot see the
   game" — `docs/TRAINING_REDESIGN.md:7-21`); the Stage-A failure produced
   the revamp. Rule: every failed run gets a written postmortem whose root
   causes each name a *mechanism*, not a mood ("reward too sparse over
   1000+ step episodes", not "training is hard").
2. **Telemetry gaps.** Asking "what CAN'T we currently see?" found the
   mean_floors/deaths_by_act blindness and led to run-env eval telemetry, the
   planned per-act/heart_reached/heart_win metrics, and the per-boss HP
   tracking (`TRAINING_REVAMP_SPEC.json`, success_metrics). A cheap, reliable
   idea generator: list the decisions the agent makes that no current metric
   scores.
3. **Reading the decompiled C#.** Ground truth reading has repeatedly
   resolved questions experiments could not: Osty being an ally *monster*
   with a self-looping NOTHING_MOVE proved the always-masked `select_player`
   slice is a non-issue for solo play (`docs/KNOWN_ISSUES.md` #16, vs
   `decompiled_v0.109.0/.../Osty.cs`); mid-turn intent updates and
   combat-count-gated weak pools came from reading `SetMoveImmediate` /
   `ActModel.GenerateRooms` (commit `18a8059`, 2026-07-24). When sim and
   expectation disagree, read the source before hypothesizing; recipes in
   `sts2-parity-discipline`.
4. **Baselines and back-of-envelope checks.** The random-baseline comparison
   (section 3, O4) and the throughput arithmetic ("200M steps at ~3k steps/s
   ≈ 17-20h, so the budget is feasible" — `TRAINING_REVAMP_SPEC.json`,
   throughput_and_infra) both changed decisions before any run was launched.
5. **The simulator itself as an asset inventory.** The single largest
   strategic idea in the revamp — use the exact, cheap, resettable simulator
   for search and state-bank resets — came from asking "what asset do we have
   that generic RL setups don't?" Ideas that exploit a unique asset
   (bit-exact RNG, deepcopy-able states, deterministic replay) beat generic
   algorithm swaps; see `sts2-research-frontier` for which of these are
   publishable.

## 7. Methodology anti-patterns (fences)

These are *reasoning* failures. Campaign-specific operational fences (exact
resume flags, which env to eval on) live in `sts2-training-campaign`.

| Anti-pattern | Why it is wrong here | Receipt |
|---|---|---|
| Concluding "capability ceiling" from a plateau without checking optimizer vitals | Stage A's tail was a *frozen optimizer*, not a ceiling; approx_kl ~1e-8 + clip_fraction 0 means no learning is being attempted | campaign log final SB3 block; `sts2-analysis-toolkit` for how to read them |
| Adopting the first mechanism that fits | M1 fit the logs and missed the primary cause | section 3 |
| Claiming improvement from one 200-episode eval | ±~3.5% CI at 200 eps; two evals of the same policy differ by up to ~7 points | section 1, Test 5 |
| Evaluating with shaping on, or on a different task than the claim | Shaped reward ≠ objective; Stage-A combat win % was silently compared against full-run ambitions | `run_eval` builds shaping_scale=0 envs (`train_necrobinder.py:111`) |
| Gating on an aspirational number | 95% is above top-human; the old 85% gate was a guess that halted everything | sections 3-5 |
| Trusting doc/docstring numbers without re-deriving | TRAINING_REDESIGN.md says "~2.5k dims"; actual RICH_OBS_SIZE is 4184. TRAINING_GUIDE.md describes abandoned pipelines | verify: import and print (Provenance) |
| Treating truncations/sim-errors as agent deaths in analysis | Both are now tagged in `info`; scoring them as deaths biases win rates down and hides sim bugs | `reward_config.py:36`, `run_env.py:363-364` |
| Rationalizing outcomes post-hoc | If you didn't predict numbers, you can't be surprised, and surprise is the signal | section 2 |
| Re-litigating a documented retirement without new evidence | The recorded reason is the bar; argue against *it*, not the conclusion | section 5 |
| "It passed a spot check" as sign-off | House doctrine: full suite + parity audits per `sts2-change-control` before any sim-behavior result counts | change-control skill |

## 8. Runbook: taking a hunch to an accepted result

1. **Write the hunch as a mechanism.** One sentence: "X causes Y via Z."
   If you cannot name Z, it is an observation, not a hypothesis.
2. **Gather ALL observations first.** Pull the eval history, the log tail,
   and the baseline before theorizing:

   ```powershell
   cd C:\Users\motqu\GitHub\sts2-rl-agent
   Get-Content output\necrobinder_g1_campaign.log -Tail 40
   Get-Content output\necrobinder_g1\G1\eval_history.jsonl | Select-Object -First 3
   Get-Content output\necrobinder_g1\G1\eval_history.jsonl | Select-Object -Last 3
   ```

   (Paths as of 2026-07-24; the live run writes to `output/necrobinder_g1/`.
   Substitute the current stage dir. Log-reading semantics:
   `sts2-analysis-toolkit`.)
3. **Build the observation-vs-mechanism table** (section 3 format). Add
   candidate mechanisms until every row is explained by at least one, and
   check your preferred mechanism against the *earliest* data point.
4. **Design the discriminating experiment.** The best experiment is the one
   whose two possible outcomes route to different actions. Pre-register:
   expected numbers, falsifier, decision trigger, honest ceiling (section 2).
   Prefer cheap reads (decompiled source, existing artifacts, a
   500-episode eval of an existing checkpoint) over new training runs.
5. **Refutation pass.** Assign someone/something to break the diagnosis
   before you spend compute on it (section 1, Test 4).
6. **Run under the standard eval protocol.** Deterministic, shaping_scale=0,
   stated seed block, action masks passed to `predict()`. Episode counts:
   200-500 for direction, ≥1000 for claims. Mechanics: `sts2-analysis-toolkit`
   and `sts2-testing-and-qa`.
7. **Compare CIs, not point estimates.** Overlapping Wilson CIs = no result
   yet; either collect more episodes or stop claiming.
8. **Record the outcome in the artifact of record.** Adopted → code +
   doctrine docs via `sts2-change-control` (full test regime; campaign-
   doctrine changes need user approval). Retired → written reason + ideally a
   re-opening condition (section 5 pattern). Incident-shaped findings → the
   KNOWN_ISSUES ledger with its status vocabulary (Fixed /
   verified-from-source-only / documented-not-fixed); templates in
   `sts2-docs-and-writing`.
9. **Update the spec if the result changes a pre-registered trigger.** The
   spec is doctrine; silently diverging from it is a doctrine change and
   needs the user.

## 9. Provenance and maintenance

All facts in this skill verified directly against the repo on **2026-07-24**
at HEAD `fe25668` (working tree additionally carried uncommitted
content-description/web changes not relevant here). The G1 training run was
live while this was written — every "as of 2026-07-24" number about it will
drift within hours. Re-verify before relying:

| Fact | Re-verify with |
|---|---|
| HEAD / whether more revamp phases have landed | `git -C C:\Users\motqu\GitHub\sts2-rl-agent log --oneline -5` |
| G-ladder stage table, constants (lr 2e-4, target_kl 0.03, ent 0.01, eval 100k/200, ckpt 250k, max_steps 3000) | `Get-Content C:\Users\motqu\GitHub\sts2-rl-agent\scripts\train_necrobinder.py -TotalCount 76` |
| Promotion = telemetry, never halts | read `scripts/train_necrobinder.py:215-225` and confirm no `break` in `main()` |
| Stage-A evidence (60.5%→63.5%→62.0%, frozen-optimizer tail) | `Get-Content output\necrobinder_a10_campaign.log -Tail 30` and `output\necrobinder_a10\A\eval_history.jsonl` |
| Old halt + old shaping anneal (for the history) | `git show 18a8059:scripts/train_necrobinder.py` (halt ~lines 440-445; anneal in `_do_eval`) |
| G1 live-run status and health | `Get-Content output\necrobinder_g1_campaign.log -Tail 30` |
| Revamp spec contents / whether superseded | `Get-Content docs\TRAINING_REVAMP_SPEC.json` (committed in `fe25668`) |
| Random baseline 63.4% (Act-1 Ironclad; measure fresh for Necrobinder) | `docs/TRAINING_GUIDE.md:88` (doc otherwise stale — trust only this number's provenance, or re-measure) |
| Truncation/sim_error handling | `reward_config.py:36`, `rich_run_env.py:164-182`, `run_env.py:315-364` |
| RICH_OBS_SIZE (4184 as of 2026-07-24; grows with revamp Phase 2) | `.venv\Scripts\python.exe -c "from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; print(RICH_OBS_SIZE)"` |
| Test count (5,276 collected as of 2026-07-24) | `.venv\Scripts\python.exe -m pytest tests --collect-only -q` (tail line) |
| Wilson CI arithmetic | the one-liner in section 1 (smoke-tested 2026-07-24) |
| KNOWN_ISSUES status vocabulary / open items | `Get-Content docs\KNOWN_ISSUES.md` |

Maintenance rules: (1) when a revamp phase lands or a G-stage completes, this
skill's *examples* stay valid as history but section 4's "current state"
paragraph and the runbook's output paths must be re-dated; (2) if a documented
retirement in section 5 is ever legitimately re-opened, update the table row
with the new evidence and outcome rather than deleting it — retirement history
is itself evidence; (3) nothing here overrides `sts2-change-control`; if this
skill and that one ever disagree about what counts as "done," that skill wins
and this one gets fixed.
