---
name: sts2-research-frontier
description: >
  Publication-first map of what is genuinely novel in sts2-rl-agent, which
  claims are provable today vs. what must still be demonstrated, the prior work
  to position against, three concrete open research problems with in-repo first
  steps and falsifiable milestones, the reproducibility standard any paper
  claim must meet, and the gates before any public/ecosystem release. Load this
  when you are: drafting or reviewing a paper/abstract/README claim, deciding
  which experiment is publication-worthy, doing a related-work search, scoping
  an open problem, or preparing a public release of code/models. Do NOT load
  this for: actually running the training campaign (sts2-training-campaign),
  statistics/eval mechanics (sts2-analysis-toolkit), the hypothesis-to-result
  process itself (sts2-research-methodology), parity audit mechanics
  (sts2-parity-discipline), bridge operation (sts2-bridge-and-realgame), or
  change gating (sts2-change-control).
---

# STS2 Research Frontier — what is novel, what is provable, what to do next

You are inheriting the research agenda of a project whose highest-priority goal
(user-stated, 2026-07-24) is **publishable results**, ahead of
strongest-agent and ecosystem-release goals. This skill tells you what the
publishable core actually is, what evidence each claim still lacks, who you are
competing with, and which three problems are worth your next month. Nothing
here authorizes an external claim: **any paper submission, public README
claim, or release needs explicit user approval — route through
sts2-change-control.**

## 0. Thirty-second state of the world (as of 2026-07-24)

| Fact | Value | Where |
|---|---|---|
| HEAD | `fe25668` (2026-07-24 11:52 -0400), 640 commits | `git log -1` |
| Test suite | 5,276 tests collected (130 files, 88 of them `*parity*`) | `pytest tests --collect-only -q` |
| Simulator | From-scratch Python headless STS2 (beta v0.109.0 + ActsFromThePast + Act4Heart mods), verified against decompiled C# | `sts2_env/`, `decompiled_v0.109.0/` |
| Envs | Rich obs 4184-dim (`RICH_OBS_SIZE`), combat `Discrete(115)`, full-run `Discrete(157)`; legacy 131/151-dim obs still present | `sts2_env/gym_env/` |
| Campaign | Combat-only Stage A plateaued at 63.5% (postmortem in `docs/TRAINING_REVAMP_SPEC.json`); full-run G1–G5 ladder committed at HEAD; a **G1 run is live right now** (first eval at 100k steps in `output/necrobinder_g1_campaign.log`) — do not describe its outcome, check it | `scripts/train_necrobinder.py:58-63` |
| Bridge | C# Harmony mod built+deployed 2026-07-23, **never live-smoke-tested**; rich 4184-dim models cannot be deployed (rejected by `detect_model_mode`) | `bridge_mod/`, `sts2_env/bridge/agent_runner.py:132-165` |
| Target | Necrobinder Ascension 10 full run incl. Corrupt Heart; **95% is aspirational, never a gate** — spec's honest ceiling is ~55–70% model-free, ~70–85% with search | `docs/TRAINING_REVAMP_SPEC.json` `honest_outlook` |

Jargon used in this file (defined once): **parity** = the simulator reproducing
decompiled-game behavior, ideally bit-exactly; **PBRS** = potential-based
reward shaping (Ng–Harada–Russell 1999; policy-invariant by construction);
**SIL** = self-imitation learning on the agent's own winning episodes;
**ExIt** = Expert Iteration (search generates targets, net distills them);
**golden replay** = recording real-game states/actions and replaying them in
the simulator with per-step state comparison; **determinization** = sampling a
concrete hidden state (e.g. reshuffling via RNG reseed) so a perfect-info
search can run on an imperfect-info game; **Wilson CI** = the binomial
confidence interval this project requires on every win-rate claim
(sts2-analysis-toolkit owns the math); **Osty** = Necrobinder's summonable
ally; **Souls** = Necrobinder's resource (sts2-game-and-mods-reference owns
mechanics); **AFTP** = the ActsFromThePast mod.

## 1. Novelty inventory — claim vs. proof obligation

These are the four defensible novelty claims. For each: the asset that exists
today, and the proof that MUST exist before the claim appears in any paper or
public README. House doctrine: only real, experiment-backed results; final
win-rate claims need >=1000 eval episodes with a Wilson 95% CI.

### N1. Exact-parity headless simulator of a commercial roguelike deckbuilder, with bit-exact seeded RNG

- **What exists (verified 2026-07-24):**
  - A port of .NET `System.Random` and the game's
    `StringHelper.GetDeterministicHashCode` seed derivation
    (`sts2_env/core/rng.py:25-36,39-70`), with pinned-sequence tests:
    `Rng(0)` produces `[1559595546, 1755192844, 1649316166, 1198642031,
    442452829]` and named streams (`up_front`, `shuffle`,
    `combat_card_generation`, ...) derive the exact game seeds
    (`tests/test_rng_parity.py`).
  - 88 parity test files (of 130 test files, 5,276 collected tests), each
    citing decompiled C# in docstrings, plus four audit scripts
    (`scripts/parity_reference_audit.py`, `audit_card_static_metadata.py`,
    `audit_card_dynamic_vars.py`, `audit_card_effect_vars.py`) —
    sts2-parity-discipline owns how to run and read them.
- **Why it is novel:** no prior STS simulator claims decompiled-source parity
  or seed-compatible RNG with the shipping game (see §2 — prior sims are
  approximate reimplementations or simplifications), and none targets STS2.
- **What you may claim today:** "decompiled-source-verified simulator with
  seed-compatible RNG and an N-test parity suite." Quantify: "88 parity test
  files / 5,276 tests" (re-count at claim time).
- **What you may NOT claim yet:** "exact" or "bit-exact game replica".
  `docs/KNOWN_ISSUES.md` #11 explicitly labels several card effects
  "audited-not-proven-exact" (e.g. Alchemize, BeatDown, HandOfGreed, Compact,
  WhiteNoise, TheHunt), and 14 Necrobinder card entries deliberately deviate
  from the stale reference doc (PATCHED allowlists). **Proof obligation:**
  golden-replay traces recorded from the live game replaying mismatch-free in
  the simulator (see Open Problem 3) — that is the only end-to-end fidelity
  evidence money can't argue with. Absent that, scope the parity claim to
  "verified against decompiled source, with a documented deviation allowlist."

### N2. Sim-to-real loop for a commercial Godot/C# game: Harmony bridge mod + golden replay harness

- **What exists:** `bridge_mod/` (C# Harmony/AutoSlay mod speaking JSON over
  TCP), `sts2_env/parity/bridge_replay.py` with `BridgeReplayRecorder`,
  `compare_combat_replay()`, `compare_run_replay()`, and a CLI
  (`python -m sts2_env.parity.bridge_replay_cli`); comparator covers combat
  plus map/reward/shop/event/rest/boss-relic slices
  (`docs/BRIDGE_REPLAY_HARNESS.md`).
- **Why it is novel:** the STS1 lineage (CommunicationMod/spirecomm) drives
  the real game but has no simulator to compare against; sim-only projects
  (decapitate-the-spire, MiniStS) have no bridge. Recording live traces and
  diffing them step-by-step against an exact simulator is the differentiator.
- **What you may NOT claim yet (as of 2026-07-24):** that the loop works. The
  mod was built and deployed 2026-07-23 but **never live-smoke-tested**; no
  recorded live trace exists in the repo; full-run traces are explicitly not
  compared end-to-end (`docs/BRIDGE_REPLAY_HARNESS.md` "Current
  Limitations"); and rich 4184-dim models are rejected at load time by
  `detect_model_mode` (`sts2_env/bridge/agent_runner.py:132-165` — accepts
  only 115/131 and 157/151 shapes). `docs/KNOWN_ISSUES.md` #16 lists further
  wire gaps (shop ordering unverified, "sell Foul Potion" unrepresentable,
  reward_screen mapped heuristically). **Proof obligation:** the live
  smoke-test checklist in sts2-bridge-and-realgame, then >=1 recorded combat
  trace with a zero-mismatch replay.

### N3. First full-run RL agent for Slay the Spire 2, including a modded 4-act Heart finale

- **What exists:** the full-run env (`Discrete(157)`, 4184-dim obs) covering
  combat, map, rewards, shop, rest, events, treasure, boss relics across acts
  1–4 with the Act4Heart mod's Corrupt Heart; the G1–G5 curriculum committed
  at HEAD; telemetry (per-act deaths, floors, heart_reached) wired per the
  revamp spec.
- **Status of the result itself: OPEN.** Committed history contains a 0%
  full-run win rate (1M steps, legacy pipeline, `docs/KNOWN_ISSUES.md` #8)
  and a 63.5% **combat-only** Stage A plateau
  (`output/necrobinder_a10/A/best_model.json`; campaign log:
  "5,005,312 steps, best win rate 63.5%"). No honest full-run win rate above
  0% exists yet. The G1 relaunch is running as you read this.
- **Proof obligation:** any nonzero A10 full-run (Heart) win rate under the
  eval protocol (§4) is already a reportable first result; report per-act
  rates with CIs, never a single headline number. "First for STS2" also
  carries a literature obligation: web-search for STS2 RL work at submission
  time — RESEARCH.md's survey is from 2026-03 and pre-dates the beta's
  public modding scene.

### N4. Provably-invariant PBRS + never-halt two-axis curriculum on a long-horizon deckbuilder (and, later, search on an exact simulator)

- **What exists:** the adopted doctrine `docs/TRAINING_REVAMP_SPEC.json`
  (committed in `fe25668`) with the PBRS spec
  (Phi = 0.45*progress + 0.30*effective_hp + 0.20*enemy_down, F =
  gamma*Phi(s')−Phi(s), Phi=0 at terminal) and the invariance argument;
  the G1–G5 ascension×act-count ladder in `scripts/train_necrobinder.py:58-63`
  that never hard-halts.
- **Honesty check (verified 2026-07-24):** PBRS is **spec'd, not implemented**.
  `sts2_env/gym_env/reward_config.py` at HEAD still carries the legacy
  shaping terms (`act_completion=0.25`, `floor=0.004`,
  `combat_hp_retention=0.05`) plus the new `truncation=0.0`; the Phase 0
  commit deliberately kept "strong shaping" for the G1 relaunch. Never
  describe PBRS as the running reward until `reward_config.py` gains a
  `potential()` method and `rich_run_env.py` emits F per step.
- **Proof obligation for a methods claim:** an ablation (PBRS vs. legacy
  shaping vs. no shaping on G1, CIs non-overlapping) plus the falsification
  test sts2-analysis-toolkit sketches for PBRS invariance. A methods paper
  that just *uses* PBRS is not novel; the contribution is the combination
  measured on a full-fidelity commercial game — so the measurements ARE the
  paper.

## 2. Prior work to position against

Source: `RESEARCH.md` (pre-project survey, largely in Chinese, committed
2026-03-16 in `81f6d6b`, likely authored upstream). Treat every venue/stat
below as **needing re-verification against the live source before it enters a
paper** — the survey is 4+ months old and was never audited.

| Prior work | What it is (per RESEARCH.md line) | Your delta |
|---|---|---|
| decapitate-the-spire | STS1 Python headless sim, gym-style, Silent/Exordium focus (:99, :243) | Full 4-act STS2, decompiled-parity + seed-compatible RNG, all-phase full-run env |
| MiniStS | Simplified STS testbed, listed as "AAAI AIIDE/EXAG 2024" (:101, :223) — **verify venue before citing** | Full-fidelity commercial game, not a simplification; mods included |
| conquer-the-spire | STS1 C++ simulator (:100) | Same delta as above; plus bridge validation |
| Miles Oram's C++ remake | "complete Ironclad 4-act experience + Deep RL" for STS1 (:102) — no link recorded; find the source before citing | STS2, Necrobinder, A10+Heart, parity discipline, sim-to-real |
| CommunicationMod / spirecomm / bottled_ai | STS1 real-game bridge lineage; bottled_ai listed at 52% Watcher win rate (:247-249) | Bridge **plus** exact sim plus golden replay; RL not scripted |
| spire-codex | STS2 decompiled data extraction pipeline (:246, §11) | Data only, no game logic — cite as evidence STS2 decompilation is established practice |
| LLM-plays-StS (FDG 2024), Two-Step RL (arXiv 2311.17305), invalid-action-masking (arXiv 2006.14171), Tilburg run-prediction (:222-228) | Academic context | Cite for framing; none trains a full-run high-ascension agent on a faithful sim |

Key positioning sentence the evidence supports: prior STS RL work either
simplifies the game (MiniStS), restricts scope (decapitate: one character/act;
most work: combat only), or acts in the real game without a training-grade
simulator (CommunicationMod lineage). This repo is, to its knowledge, the
first to hold all three: full-fidelity simulator, full-run RL, and a real-game
validation channel — with the caveats of §1 (N2/N3 unproven ends).

Also inherit the survey's negative results as fenced ground: DQN unstable on
large discrete action spaces, AlphaZero not directly applicable
(hidden information), CFR/NFSP cost-prohibitive (:122-126); the ">~1400
actions numerical precision" warning (:138) is unverified folklore — current
spaces (115/157) are far below it.

## 3. Three open problems worth working on

Each problem: why current SOTA fails, this repo's specific asset, the first
three steps IN THIS REPO, and a falsifiable "you have a result when" line.
These are ranked by (evidence-backed) expected publication value.

### OP1 — Honest high-ascension full-run mastery: PBRS + curriculum on a 4184-dim, 157-action, ~3000-step-horizon env

**Why SOTA fails.** Model-free PPO on the full run at A10 gets ~zero gradient
in Acts 3–4/Heart (states nearly never reached on-policy); sparse terminal
reward over 1000+ step episodes washes out; hand-authored shaping is either
hackable or (as Stage A proved) jitters the objective. The committed 0%
full-run result (`docs/KNOWN_ISSUES.md` #8) and the Stage A postmortem
(frozen optimizer: approx_kl 8e-9, lr 1.44e-7 — read
`docs/TRAINING_REVAMP_SPEC.json` `executive_summary`) are this failure
documented in-house.

**This repo's asset.** An exact, cheap, resettable simulator (so curricula and
state banks are trustworthy), the committed never-halt G1–G5 ladder with
per-act/Heart telemetry, and a fully-specified PBRS design with an invariance
argument.

**First three steps.**
1. Read the live G1 run's telemetry before touching anything (a concurrent
   session may own it — coordinate, don't clobber):
   ```
   Get-Content C:\Users\motqu\GitHub\sts2-rl-agent\output\necrobinder_g1_campaign.log -Tail 40
   ```
   and the eval sidecars under `output\necrobinder_g1\G1\`. Launch mechanics
   and branch decisions belong to sts2-training-campaign.
2. Implement spec Phase 1 (PBRS in `reward_config.py` + `rich_run_env.py`) as
   a training-pipeline change through sts2-change-control, then run the
   three-arm G1 ablation: legacy shaping vs. PBRS vs. terminal-only.
3. Add the deck-bag observation (spec Phase 2) and re-run the segment tests
   (`tests/` rich_observation segment tests, see sts2-testing-and-qa) before
   any long run.

**You have a result when:** an A10 full-run (acts 1–4, Heart) win rate > 0
with a 1000-episode Wilson 95% CI, shaping off, deterministic, seed block
10,000,000+ — plus per-act breakdown. A *strong* result is the PBRS arm
beating legacy shaping with non-overlapping CIs at equal steps. Falsified if:
after the full ~200M-step ladder, heart_win stays statistically
indistinguishable from 0 — that outcome triggers OP2 and is itself
publishable as a negative result with the telemetry to explain it.

### OP2 — Determinized search on a bit-exact simulator of a stochastic, hidden-information deckbuilder

**Why SOTA fails.** AlphaZero-style search assumes perfect information;
imperfect-information methods (CFR family) don't scale to this state space
(RESEARCH.md:125). Nobody has shown whether cheap determinized MCTS — sound
in Hearthstone-like settings — closes the endgame gap on a full roguelike run
where the binding constraint is one specific boss (Corrupt Heart: Beat of
Death punishes Necrobinder's 0-cost loops, Invincible caps burst; see
sts2-game-and-mods-reference).

**This repo's asset.** The one thing search needs and almost no game RL
project has: a **clonable, reseedable, exact** state. `CombatState` owns its
`Rng` (`sts2_env/core/combat.py:134`), streams are named and re-derivable
(`tests/test_rng_parity.py`), and the spec already contains a concrete design
(`docs/TRAINING_REVAMP_SPEC.json` Phase 8: PUCT, ~12 determinizations via RNG
reseed, 64–128 sims on elite/boss/Heart turns only, offline ExIt every ~5M
steps). The spec records CombatState deepcopy at ~516us — treat that number
as spec-recorded, re-measure before relying on it.

**First three steps.**
1. Benchmark clone cost yourself (read-only, fast; helper shipped with this
   skill and smoke-tested 2026-07-24 — it printed ~340–400us/copy on the
   campaign machine for a minimal combat, consistent with the spec's ~516us):
   ```
   C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe C:\Users\motqu\GitHub\sts2-rl-agent\.claude\skills\sts2-research-frontier\scripts\bench_deepcopy.py
   ```
   Then extend it to time `RunManager` deepcopy on mid-run states — the spec
   flags that as the real risk (risks_and_mitigations #9), and the minimal
   combat number is only a lower bound.
2. Build `sts2_env/search/combat_mcts.py` exactly per spec Phase 8 (new files
   are training-pipeline class changes — sts2-change-control), with a
   fixed-seed unit test that search on a lethal-available board finds lethal.
3. Evaluate the *frozen* current-best policy with and without 64-sim MCTS on
   boss turns only, 1000 episodes each — no retraining needed for the first
   datapoint.

**You have a result when:** inference-time MCTS lifts heart_win (or boss-fight
win-through) over the same frozen policy with non-overlapping 1000-ep Wilson
CIs at a stated compute budget (sims/turn, ms/move). Falsified if: the lift
is within CI noise at 128 sims — which would itself be a finding about
determinization bias on snowballing bosses (the spec's risk #9). The
escalation trigger is data-driven: adopt search only if telemetry shows
heart_reached high but heart_win flat (spec `concrete_implementation_plan`
Phase 8).

### OP3 — Measured sim-to-real fidelity: golden replay as a transfer metric

**Why SOTA fails.** Sim-trained game agents are almost never validated
against the shipping binary; "our simulator is accurate" is folklore in every
prior STS project. There is no published protocol for *quantifying* simulator
fidelity of a commercial game via recorded-trace replay.

**This repo's asset.** The only complete tooling chain for it: bridge mod +
`BridgeReplayRecorder` + per-step comparators + a parity suite to localize any
mismatch to a mechanic, and a bit-exact RNG so *seeded* live runs are in
principle replayable. Also the sharpest gap: none of it has touched the live
game yet.

**First three steps.**
1. Execute the live smoke-test checklist (owned by sts2-bridge-and-realgame;
   requires the real game — cannot be done in this dev environment).
2. Record combat traces from real play:
   ```
   C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe -m sts2_env.bridge.agent_runner --model-path output\combat_ppo\final_model.zip --record-replay artifacts\live_trace.json --replay-factory <your_factory_module>:make_combat
   ```
   (Use a legacy-shape model — rich models are rejected; closing that
   `detect_model_mode` gap is a prerequisite for policy-level transfer tests
   and is tracked in sts2-bridge-and-realgame.)
3. Replay and diff:
   ```
   C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe -m sts2_env.parity.bridge_replay_cli compare artifacts\live_trace.json --mode combat --factory <your_factory_module>:make_combat
   ```
   Triage every mismatch through sts2-parity-discipline (fix or PATCHED
   allowlist, never silently absorb).

**You have a result when:** you can state "K live-recorded combats, M total
steps, mismatch rate r per compared field class" and, at policy level, a
sim-vs-real win-rate delta with CIs for the same model. Zero mismatches over a
pre-registered K upgrades claim N1 from "decompile-verified" to
"replay-verified". Falsified if: mismatches are widespread and irreducible —
then the paper's fidelity claim must be scoped to the parity-suite level, and
the deltas become the KNOWN_ISSUES-style documented gaps.

## 4. Reproducibility standard for the paper

Every number in a paper must be regenerable by a stranger. Minimum bundle per
claim (this is the frontier-skill contract; mechanics live in
sts2-analysis-toolkit and sts2-testing-and-qa):

- [ ] **Protocol line**: episodes (>=1000 for final claims), Wilson 95% CI,
  `deterministic=True`, shaping off (`shaping_scale=0`), seed block
  (campaign convention: 10,000,000+), env class + `RICH_OBS_VERSION` (=1 as
  of 2026-07-24, `sts2_env/gym_env/rich_observation.py`).
- [ ] **Code identity**: commit hash; `git status` clean or diff attached.
  Working tree is often dirty mid-campaign — never benchmark a dirty tree
  for a reportable number.
- [ ] **Game identity**: STS2 beta v0.109.0 + ActsFromThePast + Act4Heart
  (both ACTIVE in the campaign config); decompile tree
  `decompiled_v0.109.0/` (241 entries). A game patch invalidates parity —
  re-run the four audit scripts after any re-decompile.
- [ ] **Environment identity** (verified 2026-07-24): Python 3.13.14,
  torch 2.11.0+cu128, stable-baselines3 2.9.0, sb3-contrib 2.9.0, CUDA
  available on RTX 4060 Laptop 8GB. The venv is python-m-venv + pip editable
  + uv-installed torch; **never `uv sync` over it** (stale CPU-only lock —
  house doctrine; rebuild recipe in sts2-build-and-env).
- [ ] **Artifacts**: checkpoint zip (~103MB each for rich policies) + its
  sidecar JSON with `eval_history`; the campaign log; TensorBoard dir.
  Storage/layout in sts2-run-and-operate.
- [ ] **Test gate**: full suite green at the claimed commit (5,276 tests as of
  2026-07-24) + four parity audits — house doctrine, enforced by
  sts2-change-control.
- [ ] **Labels**: aspirational targets labeled aspirational (95% especially);
  unproven mechanisms labeled candidate; negative results reported with the
  same protocol as positives.
- [ ] **Baselines**: random-play baseline for every env claim
  (sts2-analysis-toolkit); the committed historical baselines (0% legacy
  full-run; 63.5% combat-only Stage A) cited as such, never as full-run
  results.

Pre-register the eval before running it (episodes, seeds, metric, success
threshold) — sts2-research-methodology owns the
hypothesis-predicts-numbers-first discipline; this file only insists paper
numbers follow it.

## 5. Ecosystem-release gates

Release is the third-priority goal and currently **blocked**. Gates in order;
none may be skipped, all require explicit user approval (change-control class:
campaign-doctrine / external claims).

1. **Legal blocker (hard, verified 2026-07-24): 9,385 decompiled C# files are
   tracked in git** (`git ls-files | grep -cE "^decompiled"` → 9385 across
   `decompiled/`, `decompiled_v0.109.0/`, `decompiled_mods/`) and the repo is
   pushed to a GitHub fork (`origin` = QuarionIC/sts2-rl-agent; visibility
   unverified from this environment — check it). That is MegaCrit's
   copyrighted game code. Public release requires removing the trees AND
   scrubbing git history (`decompiled/` entered in `3116b21`,
   `decompiled_v0.109.0/` in `0078f70`, `decompiled_mods/` in `6860139`), or
   a rights decision that is the user's alone. Do not let any tooling
   "helpfully" publish this repo.
2. **No LICENSE file exists** (verified: repo root has none). Choosing one is
   a user decision; code the user authored vs. inherited upstream (see §6)
   may complicate it.
3. **Derived-data audit.** `docs/CARDS_REFERENCE.md` (and
   POWERS/MONSTERS/RELICS_REFERENCE.md) are generated from decompiled source;
   CARDS_REFERENCE is additionally **load-bearing runtime data** (parsed by
   `sts2_env/content/descriptions.py` and `sts2_env/cards/factory.py` —
   sts2-docs-and-writing owns this trap). A release must decide whether
   derived tables are shippable and must not break the runtime dependency.
4. **Claims hygiene.** README is deeply stale (May 2026 numbers: wrong obs
   sizes, action spaces, test counts — sts2-docs-and-writing has the map).
   A release README must be rebuilt from verified facts and carry no
   unlabeled aspirational numbers.
5. **Bridge honesty.** No release may say "plays the real game" until the
   live smoke test (OP3 step 1) has passed; today's truthful phrasing is
   "bridge mod built and deployed, live validation pending."
6. **Repro pack.** `uv.lock` is stale/CPU-only relative to the working env —
   ship a working install recipe (sts2-build-and-env), pinned versions from
   §4, and at least one small pre-trained legacy-shape model whose eval
   reproduces a stated number.
7. **Model release ethics/practicals:** checkpoints are ~103MB each; ship the
   eval protocol with any model so downstream numbers stay honest.

## 6. Authorship and provenance caution (for the paper's own integrity)

Verified 2026-07-24: this repo is a fork. `origin` =
github.com/QuarionIC/sts2-rl-agent (user), `upstream` =
github.com/zhiyue/sts2-rl-agent; exactly the 12 commits since 2026-07-23 are
local-authored (Quentin Mot + Claude co-author), while the 628 commits of the
March bootstrap and May parity blitz predate the fork divergence and are
inherited upstream work (`git log upstream/main..main --oneline | wc -l` →
12). Before any submission: establish upstream authorship/consent, decide
credit, and disclose AI assistance per venue policy. The *campaign* (v0.109.0
re-sync, mods, rich envs, curriculum, revamp) is local work; the simulator
core and parity blitz largely are not. Getting this wrong is a
paper-retraction-class mistake.

## 7. What NOT to claim — fenced wrong paths

| Tempting claim | Why it is fenced | What to say instead |
|---|---|---|
| "95% win rate target" as a result or gate | House doctrine: aspirational, above top-human; gating on it killed the first campaign | "aspirational stretch target; honest ceiling ~55–85% depending on search (spec `honest_outlook`)" |
| "Bit-exact simulator of STS2" | KNOWN_ISSUES #11 open; PATCHED deviations; no live replay evidence | "decompiled-source-verified with parity suite + documented deviations" |
| "Agent plays the real game" | Bridge never live-smoke-tested; rich models rejected at load | "bridge built and deployed; live validation pending" |
| "63.5% win rate" as a full-run number | It is combat-only Stage A, 200-ep evals, shaped training | cite it only as the postmortem baseline it is |
| Any 200-episode win-rate claim | ~3.4% stderr; doctrine requires 1000-ep Wilson CI for claims | run the 1000-ep protocol |
| "PBRS-trained agent" (today) | PBRS is spec'd, not implemented (reward_config.py still legacy shaping at HEAD) | "PBRS adopted in the revamp spec; implementation pending" |
| MiniStS venue / bottled_ai 52% / Oram remake details from memory | Sourced only from a 2026-03 unaudited survey | re-verify at the live source before citing |
| Publishing the repo as the paper artifact | 9,385 tracked decompiled files + no license | see §5 gates |

## Provenance and maintenance

Every volatile fact above, with a one-line re-verification command (run from
`C:\Users\motqu\GitHub\sts2-rl-agent`; venv python =
`.venv\Scripts\python.exe`). All facts dated 2026-07-24 unless noted.

| Fact | Re-verify with |
|---|---|
| HEAD `fe25668`, 640 commits, 12 ahead of upstream | `git log -1 --oneline; git log --oneline \| wc -l; git log upstream/main..main --oneline \| wc -l` |
| 5,276 tests / 130 test files / 88 parity files | `.venv\Scripts\python.exe -m pytest tests --collect-only -q \| tail -1` and `ls tests/*parity*.py \| wc -l` |
| Obs/action constants (4184 / 115 / 157 / 151 / 131, RICH_OBS_VERSION=1, NUM_CARD_IDS=586) | `.venv\Scripts\python.exe -c "from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE,RICH_OBS_VERSION,NUM_CARD_IDS; from sts2_env.gym_env.action_space import ACTION_SPACE_SIZE; from sts2_env.gym_env.run_env import RUN_OBS_SIZE,_LAYOUT; print(RICH_OBS_SIZE,RICH_OBS_VERSION,NUM_CARD_IDS,ACTION_SPACE_SIZE,RUN_OBS_SIZE,_LAYOUT.total_actions)"` |
| RNG pinned sequences + named streams | `.venv\Scripts\python.exe -m pytest tests/test_rng_parity.py -q` |
| G1–G5 stage table (A0/2→A10/4, 20M→60M) | `grep -n "StageConfig(" scripts/train_necrobinder.py` |
| Reward still legacy shaping + truncation=0.0 (PBRS pending) | `grep -n "act_completion\|floor\|truncation\|potential" sts2_env/gym_env/reward_config.py` |
| Stage A postmortem: best 63.5%, 5,005,312 steps, halt message | `Get-Content output\necrobinder_a10_campaign.log -Tail 3` |
| G1 run live / its latest state | `Get-Content output\necrobinder_g1_campaign.log -Tail 20` |
| Rich models rejected by the bridge | read `sts2_env/bridge/agent_runner.py:132-165` (`detect_model_mode`) |
| Bridge replay harness scope + limitations | read `docs/BRIDGE_REPLAY_HARNESS.md` ("Current Limitations") |
| KNOWN_ISSUES #11 (not-proven-exact cards) and #16 (bridge gaps) still open | read `docs/KNOWN_ISSUES.md` |
| 9,385 tracked decompiled files; no LICENSE | `git ls-files \| grep -cE "^decompiled"` and `ls LICENSE*` |
| Versions: Python 3.13.14, torch 2.11.0+cu128, SB3/sb3-contrib 2.9.0, CUDA True | `.venv\Scripts\python.exe -c "import sys,torch,stable_baselines3,sb3_contrib; print(sys.version.split()[0],torch.__version__,stable_baselines3.__version__,sb3_contrib.__version__,torch.cuda.is_available())"` |
| Revamp spec is the adopted doctrine (committed) | `git log --oneline -1 -- docs/TRAINING_REVAMP_SPEC.json` |
| Prior-work table provenance | read `RESEARCH.md` §4.2, §10 (survey dated 2026-03-16; re-verify externally before citing) |

Maintenance rule: if any command above disagrees with this file, the repo
wins — fix this file (docs-class change, see sts2-change-control) and
re-stamp the date.
