---
name: sts2-change-control
description: >
  Load this skill BEFORE landing any change in sts2-rl-agent — code, docs,
  training config, bridge C#, or campaign doctrine. It defines how changes are
  classified, which verification gate each class must pass before it counts as
  done, the project's non-negotiable rules (each with the incident that created
  it), commit conventions, and what requires explicit user approval. Load it
  when you are about to: edit simulator/env/trainer/bridge code, commit,
  claim a win rate or performance number, edit docs/TRAINING_REVAMP_SPEC.json,
  or publish/report anything externally. Do NOT load it for diagnosis
  (use sts2-debugging-playbook), for how to write a specific parity test
  (sts2-parity-discipline / sts2-testing-and-qa), for which training run to
  launch next (sts2-training-campaign), or for rebuilding the environment
  (sts2-build-and-env). This skill owns exactly one question: what must be
  true before a change counts as done.
---

# STS2 Change Control

Repo: `C:\Users\motqu\GitHub\sts2-rl-agent`. All commands below are run from
that directory in PowerShell or Git Bash, always with the venv interpreter
`.venv\Scripts\python.exe` — never bare `python`.

This project is a from-scratch headless Python simulator of Slay the Spire 2
(beta v0.109.0 + two active mods), verified against decompiled C#, plus
Gymnasium RL envs, a MaskablePPO curriculum trainer, and a C# bridge mod to the
real game. Its credibility rests entirely on two things: **parity** (the
simulator provably matches the decompiled game source) and **honest results**
(every number is backed by a stated experimental protocol). Change control
exists to protect both. A change that "works on my machine" but skipped its
gate is not done; it is a liability.

## Jargon used in this skill (defined once)

| Term | Meaning |
|---|---|
| **parity** | The simulator reproducing decompiled-C# game behavior exactly (values, RNG consumption, hook order). Ground truth precedence: `decompiled_v0.109.0/` > `decompiled/` (pre-patch) ; `decompiled_mods/` for the two active mods. Operational detail: sts2-parity-discipline. |
| **PATCHED allowlist** | Explicit frozensets of card IDs (`PATCHED_NECROBINDER_CARD_IDS`) in the audit scripts and `tests/test_all_cards_unit_coverage.py:63-78` that whitelist *deliberate* deviations from the stale pre-patch reference source. The only legal way to deviate. |
| **sim behavior** | Anything that changes what the simulated game *does*: card/power/relic/monster/event/shop/map/RNG logic, `docs/CARDS_REFERENCE.md` (it is parsed at runtime), hook order, combat/run flow. |
| **doctrine of record** | `docs/TRAINING_REVAMP_SPEC.json` (tracked at HEAD since commit `fe25668`, 2026-07-24): the adopted G1–G5 full-run-only campaign design. Changing it is a campaign-doctrine change (see approval matrix). |
| **Wilson 95% CI** | The confidence interval required on any final win-rate claim (>=1000 eval episodes). Statistics detail: sts2-analysis-toolkit. |
| **rich obs** | The current 4184-dim observation (`RICH_OBS_SIZE`, `sts2_env/gym_env/rich_observation.py:228`), vs the legacy 131/151-dim obs. Combat actions Discrete(115), run actions Discrete(157). |

## The two all-repo user rules (stated 2026-07-24, apply to every change)

1. **Comprehensive test regime after every change.** Sim-behavior changes
   require the FULL suite green (5,276 tests — it runs in ~25 s, there is no
   excuse to spot-check), plus all four parity audit scripts, plus
   `scripts/benchmark.py` if the change could affect performance. No
   spot-check sign-offs, ever. "The three tests I touched pass" is not done.
2. **Only real, experiment-backed results.** Every win-rate or performance
   claim carries its protocol: episodes, seed block, deterministic flag,
   shaping off. Final/external claims need >=1000 eval episodes with a Wilson
   95% CI. Aspirational targets (the 95% A10 goal) are always labeled
   aspirational — they are never pass/fail gates (see incident N1 below).

## Change classification

Classify every change into exactly one primary class (a change touching two
classes must pass BOTH gates). When unsure, escalate to the stricter class.

| Class | What falls in it | Examples |
|---|---|---|
| **A. Sim behavior** | `sts2_env/` core/cards/powers/relics/monsters/potions/encounters/events/run/map/content, `docs/CARDS_REFERENCE.md`, anything altering game logic or RNG consumption | Fix a card effect; add a mod event; change shop pricing; reorder a hook |
| **B. Training pipeline** | `scripts/train_*.py`, `sts2_env/gym_env/` (envs, obs, reward, action space), `sts2_env/train/` (policy), eval/benchmark scripts | Change reward shaping; grow the obs; retune PPO hparams; alter the stage ladder |
| **C. Bridge / C#** | `bridge_mod/` (C# mod), `sts2_env/bridge/` (client, adapters, agent_runner), `docs/PROTOCOL.md` wire format | New Harmony patch; new bridge JSON field; adapter ordering change |
| **D. Docs only** | `docs/*.md` (EXCEPT `CARDS_REFERENCE.md`), `README.md`, `CONTRIBUTING.md`, comments/docstrings with zero behavior change | Update KNOWN_ISSUES; fix a stale count |
| **E. Campaign doctrine** | `docs/TRAINING_REVAMP_SPEC.json`, stage/gate/budget tables in `scripts/train_necrobinder.py`, success-metric definitions, anything redefining what the campaign is trying to do or how success is measured | Change a stage budget; redefine the eval protocol; adopt/retire a method (BC, SIL, MCTS) |

Classification traps (each has burned this project):

- `docs/CARDS_REFERENCE.md` **is class A, not D**. It is parsed at runtime by
  `sts2_env/content/descriptions.py:344` and `sts2_env/cards/factory.py:202`,
  and asserted against by `tests/test_all_cards_unit_coverage.py`. Editing it
  can change card factories, tooltips, and test outcomes.
- Observation/action-layout edits are class B but carry class-A-grade risk: a
  wrong offset silently corrupts training with no error. The revamp spec
  mandates segment-table tests plus a shape/logit smoke test before any long
  run (see sts2-analysis-toolkit for the segment-test recipe).
- Anything touching import-time registration side effects (new content module,
  registry change) must additionally pass the fresh-interpreter import test —
  `tests/test_import_order_no_cycle.py` exists because eager registry
  construction broke `python -m sts2_env.web.play_run` (commit `b3e97b1`,
  8 minutes after `6860139` introduced it).

## The gate per class

### Class A — sim behavior (the full regime, no exceptions)

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe -m pytest tests/ -q
.venv\Scripts\python.exe scripts\audit_card_static_metadata.py
.venv\Scripts\python.exe scripts\audit_card_dynamic_vars.py
.venv\Scripts\python.exe scripts\audit_card_effect_vars.py
.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --show-missing
```

Expected (all verified green at HEAD `fe25668` + working tree, 2026-07-24):
`5276 passed` in ~25 s; "card static metadata audit passed"; "card dynamic var
audit passed"; "card effect var audit passed"; the reference audit printing
"Missing ... none" for all 8 surfaces (cards, encounters, events, modifiers,
monsters, potions, powers, relics) and exiting 0.

If the change is performance-relevant (hot combat/run paths, hooks, RNG):

```powershell
.venv\Scripts\python.exe scripts\benchmark.py
```

Baseline as of 2026-07-24: ~115 episodes/sec, ~2917 steps/sec, 29.4% random
win rate on the legacy combat env, single process. A large regression (>~20%)
needs a stated justification in the commit body. For rich-env throughput use
`scripts\benchmark_rich_env.py` (flags: `--seconds`, `--n-envs`,
`--learn-steps`, `--skip-learn`, `--skip-subproc`).

Additional class-A obligations:

- New/changed behavior needs a decompiled-C#-cited parity test (docstring
  names the C# file). Recipe: sts2-parity-discipline. The reference audit is a
  name-mention coverage gate, **not** behavior proof — a green audit with a
  vacuous test is a violation of rule 2 in spirit.
- Deliberate deviation from the decompiled reference (e.g. simulating the
  v0.109.0 rework while `decompiled/` is pre-patch)? Add the CardId to the
  PATCHED allowlists in `scripts/audit_card_static_metadata.py:26` and/or
  `scripts/audit_card_dynamic_vars.py:25` (and the test whitelist at
  `tests/test_all_cards_unit_coverage.py:63-78` if applicable) with the
  standard comment explaining why. Never weaken the audit itself.

### Class B — training pipeline

Everything in class A's pytest gate (the suite covers envs, obs, reward,
policy transfer), plus:

- Obs/action layout changes: run the segment tests for the touched layout
  before anything else, then a shape/logit smoke (load or build the model,
  one forward pass, check logits are finite and the mask applies).
- Reward changes: state in the commit body which reward terms changed and why
  the change cannot be reward-hacked (the doctrine of record adopted
  potential-based shaping — PBRS, shaping via a potential function whose
  telescoping sum is policy-invariant — precisely to close hacking surfaces;
  do not reintroduce hand-authored per-event bonuses without a doctrine
  change, class E).
- Anything that alters training dynamics (hparams, shaping, curriculum) is
  **not validated by tests alone**. It is "landed" when the suite is green,
  but "validated" only after a training relaunch shows the expected
  telemetry. State which one it is in the commit body. As of 2026-07-24 the
  phase-0 revamp commit `fe25668` is exactly in this state: landed, suite
  green, NOT yet validated by a relaunch. Which run to launch and what to
  expect: sts2-training-campaign.
- `.venv` is untouchable: never `uv sync` or reinstall over it — the lock is
  stale and CPU-only; the working env is Store Python 3.13.14 + pip editable
  + uv-installed torch 2.11.0+cu128. Env changes route through
  sts2-build-and-env and are effectively class E for approval purposes.

### Class C — bridge / C#

Python side (`sts2_env/bridge/`): class A pytest gate applies (bridge adapter
tests are in the suite). C# side (`bridge_mod/`): build with
`dotnet build bridge_mod/ -c Release` (DLL only) or `dotnet publish` (PCK) —
full build/deploy/verify procedure and its many traps live in
sts2-bridge-and-realgame; do not improvise from memory. Additional gates:

- Any Harmony patch or patch-adjacent change: diff the target method signature
  against `decompiled_v0.109.0/` **by name**, because Harmony binds injected
  prefix parameters by name and a game-side rename silently un-applies the
  patch with zero errors (incident N5 below).
- Any change to option-list construction or ordering in a C# handler: the
  option-list POSITION is the action encoding; ordering must match
  `RunManager.get_available_actions()` exactly (incident N6 below). Known
  unverified orderings are catalogued in `docs/KNOWN_ISSUES.md` #16 — do not
  mark them verified without a live-game check.
- The green Python bridge test suite is explicitly weaker than a live smoke
  test (`docs/BRIDGE_REPLAY_HARNESS.md:41-45`). The mod built and deployed
  2026-07-23 has **never** been live-smoke-tested; no change may claim
  real-game correctness until that checklist (sts2-bridge-and-realgame) runs.
- Rich 4184-dim models cannot currently be deployed through the bridge —
  `detect_model_mode` (`sts2_env/bridge/agent_runner.py:132`) accepts only the
  legacy 115/131 and 157/151 shapes. Closing that gap is a class C change
  that also needs a class E note, since it is on the campaign critical path.

### Class D — docs only

Cheapest gate, but not zero, because this repo has been actively harmed by
stale docs (incident N7): as of 2026-07-24 `README.md`, `TRAINING_GUIDE.md`,
and `AGENT_USAGE_GUIDE.md` still recommend `scripts/train_full_run.py`, which
crashes at env construction (passes `act_count=`/`reward_shaping=` kwargs
that `STS2RunEnv.__init__` no longer accepts).

- Verify every command you write into a doc by running it (read-only forms).
- Verify every count/constant by import, not by copying from another doc.
- Date-stamp volatile claims. Doc-of-record precedence and the staleness map:
  sts2-docs-and-writing.
- Incident entries go in `docs/KNOWN_ISSUES.md` following its existing status
  vocabulary (Fixed / Verified-from-source-only / Open, severity, Location).
  Nothing may contradict that ledger; if your change fixes a listed issue,
  update its status in the same commit.

### Class E — campaign doctrine

No code gate can validate a doctrine change; the gate is **process**:

1. Explicit user approval BEFORE adoption (see approval matrix).
2. The change is written into `docs/TRAINING_REVAMP_SPEC.json` (or its
   successor doc), not just into code, so doctrine and implementation cannot
   drift apart silently.
3. Method adoptions/retirements follow the idea lifecycle
   (candidate → spec'd → phase-gated experiment → adopted or documented
   retirement): sts2-research-methodology.

## Non-negotiables, each with its incident

These are settled battles. Re-fighting them requires a class E approval, not a
code review. Fuller archaeology of each: sts2-failure-archaeology.

| # | Rule | The incident behind it |
|---|---|---|
| N1 | **Never gate a campaign on an unreachable threshold; never hard-halt a ladder; aspirational targets are never pass/fail.** | Stage A (combat-only, bare starter deck vs the full Act-1 pool) plateaued at 63.5% against an unreachable 85% promotion gate, and `if not promoted: break` silently killed the entire campaign after 5M steps / 63.8 min (`output/necrobinder_a10_campaign.log`, 2026-07-24 10:51). The fix (`fe25668`) made promotion telemetry-only; the ladder never halts. |
| N2 | **Never conclude "capability plateau" without checking the optimizer.** Any resume/extend decision must confirm the LR schedule is alive. | The same stage-A run ended with approx_kl 8e-9, clip_fraction 0, lr 1.44e-7 — the linear anneal froze the optimizer long before budget end, and `--resume` would have trained at ~zero LR. Doctrine is now constant lr 2e-4 + target_kl 0.03. Log forensics: sts2-analysis-toolkit. |
| N3 | **Never score truncation or a simulator error as a death.** Truncation rewards 0.0 and tags `info['truncated']`; sim errors tag `info['sim_error']=True`, reward 0.0, and are logged. | Pre-revamp, truncation==death polluted the terminal signal, and `run_env.step()` once swallowed simulator exceptions as silent losses (`docs/KNOWN_ISSUES.md` #14) — simulator bugs masqueraded as agent deaths in training curves. Both closed in `fe25668` / earlier fixes; `sts2_env/gym_env/reward_config.py:36` (`truncation: float = 0.0`) is the current contract. |
| N4 | **Never evaluate on the shaped training env, with stochastic actions, or on tiny samples; never present a 200-episode number as a result.** | The stage-A promotion machinery ran on 200-episode evals (stderr ~3.4%) noisy enough to whipsaw both the streak-of-2 promotion test and the (since-deleted) shaping anneal; the old combat-env eval was also diagnostically blind (mean_floors always 0.0). run_eval semantics: shaping_scale=0, deterministic, seed block 10,000,000+. Note (2026-07-24): `run_eval` still *defaults* to 200 episodes (`scripts/train_necrobinder.py:70`) — that default is for in-training telemetry only; claims use `--eval-episodes 1000`+ and a Wilson CI, which is doctrine (spec Phase 9) but not yet automated in the script. |
| N5 | **After any game update, re-verify every Harmony patch target signature against the fresh decompile.** | v0.109.0 renamed `SetTimeScale(float timeScale)` → `(float scale)`; Harmony binds prefix params by name, so `AnimationSpeedPatch` silently stopped applying with zero errors (`docs/KNOWN_ISSUES.md` #6). |
| N6 | **Bridge option-list ordering IS the action encoding; never derive it from anything but the game's own iteration order.** | `RlMapHandler.cs` ordered reachable map nodes by coordinate scan instead of `MapPoint.Children` insertion order, silently feeding the policy scrambled indices (`docs/KNOWN_ISSUES.md` #5). Shop ordering remains unverified (#16) — leave it flagged, do not "fix" it blind. |
| N7 | **A doc that recommends a command is wrong until the command has been run.** | `train_full_run.py` kwarg drift: three docs recommended a script that TypeErrors at env construction, contributing to the 0%-win-rate era's confusion (`docs/TRAINING_REDESIGN.md` "Why the previous attempt got 0%", item 5). |
| N8 | **Deviations from decompiled behavior are allowlisted, never silent.** | The v0.109.0 Necrobinder reworks vs the pre-patch reference source: 14 cards deviate deliberately and every one is named in a PATCHED allowlist with a comment. The audits stay strict; the allowlist carries the burden of proof. |
| N9 | **Registries are lazy; no content construction at module import time.** | Eager legacy-act registration created the import cycle map.acts → events → run → map, breaking web/CLI play (`b3e97b1`). Guarded by `tests/test_import_order_no_cycle.py`. Design rationale: sts2-architecture-contract. |

## What requires explicit user approval

Ask the user and wait for a clear yes BEFORE, not after:

| Needs approval | Why |
|---|---|
| Any change to `docs/TRAINING_REVAMP_SPEC.json` or the G1–G5 stage/gate/budget table, eval protocol, or success metrics | Campaign doctrine was adopted by decision review; silent doctrine drift is how the stage-A halt happened |
| Adopting or retiring a campaign method (BC, SIL, state bank, ExIt, MCTS) outside the spec's own decision triggers | Same |
| Any external claim: README numbers, paper text, publishing results, pushing to `upstream` (zhiyue/sts2-rl-agent) | Rule 2; highest project goal is publishable results, and a retracted claim is fatal to that |
| Touching `.venv`, `uv.lock`, or reinstalling any package | The working CUDA env is fragile and irreplaceable without hours of work (sts2-build-and-env) |
| Deleting checkpoints or anything under `output/` | `output/` is gitignored — deletions are unrecoverable; checkpoints are ~103 MB each and disk pressure is real, but pruning is the user's call |
| History rewrites, force-push, new branches as a workflow change | Repo convention is linear commits on `main` (see below) |
| Weakening any audit, test gate, or PATCHED-allowlist mechanism | The gates ARE the project's credibility |

## Commit and PR conventions

Verified against `git log` at HEAD (2026-07-24):

- **Branching:** single branch `main`, linear history, zero reverts in 639+
  commits. `origin` = QuarionIC/sts2-rl-agent (the user's fork; keep it fully
  pushed). `upstream` = zhiyue/sts2-rl-agent — never push there without
  approval. There is no CI (`.github/` does not exist); the gates above are
  run locally and that is the whole regime.
- **Subject line:** imperative, plain, names every concern in the commit,
  semicolon- or comma-separated if several (e.g. `Fix bridge mod for current
  game API; add watchable agent delays`; `Mid-turn intents, combat-count
  enemy pools, web tooltips, Champ boundary`). No conventional-commits
  prefixes, no ticket numbers.
- **Body:** bulleted, one bullet per concern, citing files and the decompiled
  C# source for parity changes (e.g. "per Champ.cs"). For sim-behavior
  commits, include the suite result line, exactly in the house style:
  `Suite: 5276 passed, 0 failed.` (update the number if the count changed —
  and if you ADDED behavior, the count should have changed; a class-A commit
  that adds behavior with zero new tests is suspect).
- **Trailer:** campaign-era commits end with
  `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>` when Claude
  co-authored.
- **One concern per PR/commit is the CONTRIBUTING.md ideal** (`CONTRIBUTING.md`
  "Pull Request Guidelines"); in practice era-3 commits are cohesive
  multi-concern checkpoints with exhaustive bodies. Either is acceptable; an
  exhaustive body is not optional.
- **Scope hygiene:** `git status` before committing. As of 2026-07-24 the
  working tree carries unrelated in-flight work (tooltip/preview changes in
  `sts2_env/content/` and `sts2_env/web/play_run.py`, plus untracked
  `sts2_env/content/preview.py`) — stage only your files; never `git add -A`
  into someone else's in-flight change. A concurrent session may be advancing
  work while you operate.

## Definition of done — checklist

Before you may say a change is done:

- [ ] Classified (A–E); if it straddles classes, both gates identified.
- [ ] Class gate run in full, output matches the expected results above —
      pasted or summarized honestly, not "tests pass".
- [ ] Parity changes cite the decompiled C# file in test docstring and commit
      body; deliberate deviations added to the PATCHED allowlists.
- [ ] No contradiction introduced against `docs/KNOWN_ISSUES.md`; ledger
      updated in the same commit if an issue's status changed.
- [ ] Any number stated carries its protocol; anything unproven is labeled
      open/candidate/aspirational; training-dynamics changes explicitly
      labeled "landed, not yet validated by relaunch" until they are.
- [ ] Approval obtained for anything in the approval matrix.
- [ ] Commit follows the conventions; working tree checked for
      unrelated in-flight files before staging.
- [ ] Docs affected by the change updated (or an issue filed in
      KNOWN_ISSUES) — do not mint the next `train_full_run.py`.

## Live state snapshot (2026-07-24, re-verify before relying on it)

- HEAD = `fe25668` "Phase 0 training revamp: full-run-only ladder (G1-G5),
  never-halt curriculum, live optimizer" (2026-07-24 11:52). This SUPERSEDES
  older notes describing the revamp as uncommitted: the phase-0/phase-1
  minimal changes and `docs/TRAINING_REVAMP_SPEC.json` are committed and
  tracked. The revamp is landed but NOT validated by a training relaunch.
- Working tree: uncommitted tooltip/preview work (see Scope hygiene above).
- Full suite: `5276 passed in 24.48s` with that tree; all four audit scripts
  green; benchmark ~115 eps/s / 29.4% random win rate.
- `main` == `origin/main` (fully pushed); `upstream/main` is behind by the
  entire campaign era.

## Provenance and maintenance

Every load-bearing fact above, with a one-line re-verification command (run
from the repo root). Facts verified 2026-07-24 against HEAD `fe25668`.

| Fact | Re-verify with |
|---|---|
| HEAD, era, revamp committed | `git log --oneline -3` |
| Working-tree in-flight files | `git status --short` |
| Suite size and green run (~25 s) | `.venv\Scripts\python.exe -m pytest tests/ -q` |
| Four audit scripts green | run the four commands in the Class A gate block; each prints "passed"/"none" and exits 0 |
| PATCHED allowlists exist | `.venv\Scripts\python.exe -c "import re;print(open('scripts/audit_card_static_metadata.py').read().count('PATCHED'))"` (>=2) |
| CARDS_REFERENCE is runtime data | `grep -n "CARDS_REFERENCE" sts2_env/cards/factory.py sts2_env/content/descriptions.py` |
| Benchmark baseline (115 eps/s, 29.4% random) | `.venv\Scripts\python.exe scripts\benchmark.py` |
| G1–G5 ladder, never-halt, promotion=telemetry | `sed -n "1,35p" scripts/train_necrobinder.py` |
| run_eval default 200 eps / seed block 10M / shaping 0 | `grep -n "EVAL_EPISODES\|EVAL_SEED_BLOCK\|shaping_scale=0.0" scripts/train_necrobinder.py` |
| truncation=0.0 contract | `grep -n "truncation" sts2_env/gym_env/reward_config.py` |
| Rich obs 4184 / combat 115 / run 157 | `.venv\Scripts\python.exe -c "from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; from sts2_env.gym_env.action_space import ACTION_SPACE_SIZE; from sts2_env.gym_env.run_env import _LAYOUT; print(RICH_OBS_SIZE, ACTION_SPACE_SIZE, _LAYOUT.total_actions)"` |
| Rich models rejected by bridge | `grep -n "def detect_model_mode" -A 30 sts2_env/bridge/agent_runner.py` |
| train_full_run.py still broken (docs trap) | `grep -n "act_count\|reward_shaping" scripts/train_full_run.py` (kwargs `STS2RunEnv.__init__` no longer accepts) |
| No CI, single branch, remotes | `git branch -a; git remote -v; git ls-files .github` (empty = no CI) |
| Commit conventions / trailer | `git log --format="%s%n%(trailers)" -5` |
| Doctrine of record tracked | `git ls-files docs/TRAINING_REVAMP_SPEC.json` |
| KNOWN_ISSUES ledger current | `git log -1 --format="%h %ci" -- docs/KNOWN_ISSUES.md` |

Maintenance rules for this skill: if any re-verification command's output
changes (test count, HEAD, benchmark baseline, stage table, eval defaults),
update the corresponding date-stamped fact in place. If the campaign adopts a
new doctrine document, update the "doctrine of record" definition and the
approval matrix in the same edit. This skill must never drift into
contradicting `docs/KNOWN_ISSUES.md` — when in doubt, the ledger wins and
this file gets fixed.
