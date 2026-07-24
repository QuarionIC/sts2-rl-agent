---
name: sts2-testing-and-qa
description: >
  Test-suite anatomy and QA discipline for the sts2-rl-agent repo: how to run
  the 5,276-test pytest suite, what the conftest fixtures and registration
  side effects do, the structural coverage gates (all-cards smoke harness,
  parity_reference_audit name-mention gate, content-description coverage),
  how to add tests for each content type, the seed-pinned "golden" inventory
  (RNG parity pins, bridge replay harness), and the eval-statistics rules a
  win-rate claim must satisfy. Load this when you are: adding or debugging
  tests, deciding which tests a new card/power/relic/monster needs, running
  the pre-merge QA gate, interpreting an audit-script failure, or writing an
  eval protocol for a checkpoint. Do NOT load this for: what counts as "done"
  for a change class or commit conventions (sts2-change-control), RNG porting
  rules and the PATCHED allowlist procedure (sts2-parity-discipline),
  diagnosing a failing sim/training run (sts2-debugging-playbook), training
  launch mechanics (sts2-run-and-operate), which training run to do next
  (sts2-training-campaign), live-game bridge recording and smoke testing
  (sts2-bridge-and-realgame), or statistics theory and log forensics
  (sts2-analysis-toolkit).
---

# STS2 Testing and QA

Everything about verifying this repo: the pytest suite, the audit scripts,
the coverage gates, the golden/seed-pinned inventory, and the statistical
bar an evaluation claim must clear. All commands are copy-pasteable from the
repo root (`C:\Users\motqu\GitHub\sts2-rl-agent`) on Windows. Always use the
venv interpreter, never bare `python`:

```
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe -m pytest tests/ -q
```

Jargon used below, defined once:

- **Parity test** — a test that pins simulator behavior to the decompiled
  C# of the real game (Slay the Spire 2 beta v0.109.0). The docstring cites
  the C# file it matches.
- **pending_choice** — the engine's resumable-pause mechanism: any card
  effect needing a mid-resolution player decision sets
  `combat.pending_choice`; callers resume via `resolve_pending_choice()`.
  Owned by sts2-architecture-contract; it matters here because the generic
  card smoke harness must drain it.
- **Souls / Osty** — Necrobinder mechanics (Soul status cards; Osty is the
  pet, implemented as an ally monster). Owned by
  sts2-game-and-mods-reference.
- **AFTP / Act4Heart** — the two active mods: ActsFromThePast (legacy act
  slots) and Act4Heart (keys + Corrupt Heart act). Owned by
  sts2-game-and-mods-reference; both have dedicated test files listed below.
- **Wilson CI** — Wilson score confidence interval for a binomial win rate;
  the required error bar on any win-rate claim (formula and one-liner in the
  eval section).

## State snapshot (2026-07-24)

- HEAD = `fe25668` ("Phase 0 training revamp: full-run-only ladder (G1-G5)").
  The working tree additionally carries uncommitted edits in
  `sts2_env/content/` and `sts2_env/web/play_run.py` — a concurrent session
  may be advancing them. Run `git status` before trusting any line number in
  those files.
- Full suite: **5,276 passed in 25.4s** on this tree (verified 2026-07-24).
- Known open coverage regression: `parity_reference_audit.py` reports
  encounters **1 missing test mention: `ToadpolesNormal`** (verified
  2026-07-24). The summary table in `docs/PARITY_COVERAGE_BACKLOG.md` still
  says 0 missing — the doc is stale; trust the script.
- `docs/TRAINING_REVAMP_SPEC.json` is now committed (it defines the eval
  doctrine quoted in the eval section).

## Suite anatomy

| Fact | Value (2026-07-24) | How verified |
|---|---|---|
| Tracked files under `tests/` | 132 (130 `test_*.py` + `conftest.py` + empty `__init__.py`) | `git ls-files tests/` |
| `def test_` functions | 3,345 | grep count |
| Collected tests | 5,276 (in ~1.5s) | `pytest tests/ --collect-only -q` |
| Full-suite runtime | ~25s, single process | full run |
| pytest config | `pyproject.toml` `[tool.pytest.ini_options]` = `testpaths = ["tests"]` only | read file |
| pytest version in `.venv` | 9.1.1; **no pytest-cov, no pytest-xdist** | `pip list` |

Consequences you must internalize:

- There are **no markers, no slow tier, no parallel flags**. Run the whole
  suite; it is fast enough. Do not invent `-m` filters or `-n auto` — they
  are unsupported here (`-n` will error: xdist is not installed).
- `CONTRIBUTING.md:69` recommends `pytest tests/ --cov=...` — **that command
  fails** in the current venv (pytest-cov not installed). `CONTRIBUTING.md:45`
  also claims "16 test files, 408 test functions" — badly stale. Trust the
  table above.
- Coverage is enforced **structurally** (see coverage gates below), not by
  line coverage. Do not add line-coverage tooling to "fix" this; the repo's
  model is exhaustive per-content-ID parametrization instead.
- The 3,345 → 5,276 gap is parametrization, chiefly
  `tests/test_all_cards_unit_coverage.py:282` (`@pytest.mark.parametrize`
  over all 577 `CardId`s) and table-driven monster tests.

Standard invocations:

```
.venv\Scripts\python.exe -m pytest tests/ -q
.venv\Scripts\python.exe -m pytest tests/test_combat_parity.py -q
.venv\Scripts\python.exe -m pytest tests/test_combat_flow.py::test_basic_turn_flow -q
.venv\Scripts\python.exe -m pytest tests/ --collect-only -q
```

Collection doubles as an import smoke test: every test module imports at
collection time, so `--collect-only` finishing clean (~1.5s) proves all
modules import without error — a cheap first check after refactors.

### Largest test files (orientation map)

| File | `def test_` count | Covers |
|---|---|---|
| `tests/test_parity_helpers.py` | 216 | helper-level parity |
| `tests/test_power_lifecycle_and_modifier_hooks.py` | 192 | power hooks |
| `tests/test_monster_ai_state_machine_parity.py` | 165 | monster AI |
| `tests/test_potion_registry_usage_and_generation.py` | 90 | potions |
| `tests/test_thecity_monsters.py` | 86 | Act 2 monsters |
| `tests/test_run_flow.py` | 82 | run layer |
| `tests/test_encounter_setup_and_pools_parity.py` | 79 | encounters |
| `tests/test_necrobinder_card_effects_reference_parity.py` | 77 | Necrobinder cards |
| `tests/test_defect_card_effects_reference_parity.py` | 76 | Defect cards |

Mod coverage (the campaign depends on these): `tests/test_act4_heart_mod.py`
(34 tests), `tests/test_acts_from_the_past_foundations.py` (33),
`tests/test_events_aftp_shared.py` (62). Bridge coverage (Python-side only):
`test_bridge_replay_harness.py` (29), `test_bridge_run_state_adapter.py`
(32), `test_bridge_autoslay_coverage.py` (6), `test_agent_runner_model_mode.py`
(10), `test_agent_runner_noncombat_policy.py` (11).

## Fixtures and global state

`tests/conftest.py` (53 lines) is the entire shared-fixture surface:

- `reset_ids` — **autouse**; calls
  `sts2_env.cards.base.reset_instance_counter()` (defined at
  `sts2_env/cards/base.py:544`) before every test. Card instance IDs are a
  global mutable counter; without this, tests are order-dependent.
- `player` — 80 HP player `Creature`.
- `enemy` — 50 HP `Creature`, `monster_id="TEST_ENEMY"`.
- `rng` — `Rng(42)`.
- `simple_combat` — Ironclad starter deck vs ShrinkerBeetle,
  `rng_seed=42`, `start_combat()` already called.

conftest's top-of-file `import sts2_env.powers  # noqa: F401` is
load-bearing: powers (and potions) register themselves into global
registries **by import side effect**. Many test files repeat
`import sts2_env.powers` / `import sts2_env.potions` locally.

**Trap for any standalone script or notebook** (the test suite gets this for
free from conftest; your ad-hoc script does not):

```python
import sts2_env.powers   # noqa: F401  registration side effect
import sts2_env.potions  # noqa: F401
from sts2_env.cards.base import reset_instance_counter
# ... and call reset_instance_counter() between constructed scenarios
```

Forgetting the imports yields missing-registry errors (or silently absent
power behavior), not an obvious "you forgot to import" message.

## The QA gate (house rule: run after every sim-behavior change)

Per the all-repo rule, a sim behavior change is not done until ALL of the
following are green. No spot-check sign-offs. (What class of change requires
which gate, and what "done" means, is owned by sts2-change-control — this
section is the mechanics.)

```
.venv\Scripts\python.exe -m pytest tests/ -q
.venv\Scripts\python.exe scripts\audit_card_static_metadata.py
.venv\Scripts\python.exe scripts\audit_card_dynamic_vars.py
.venv\Scripts\python.exe scripts\audit_card_effect_vars.py
.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --show-missing
.venv\Scripts\python.exe scripts\benchmark.py
```

(`benchmark.py` is required when the change is performance-relevant;
`CONTRIBUTING.md:284-285` requires suite + benchmark before any PR.)

**Exit-code semantics differ — this matters for automation:**

| Command | On failure | Verified |
|---|---|---|
| `pytest tests/` | nonzero exit | standard |
| `audit_card_static_metadata.py` | prints mismatches, **exit 1** (`return 1` at line 122) | 2026-07-24 |
| `audit_card_dynamic_vars.py` | **exit 1** (line 83) | 2026-07-24 |
| `audit_card_effect_vars.py` | **exit 1** (line 119) | 2026-07-24 |
| `parity_reference_audit.py` | **always exit 0** (`main()` ends `return 0`, scripts/parity_reference_audit.py:492) — you MUST read the "Missing" columns | 2026-07-24 |
| `benchmark.py` | no threshold; prints eps/sec + win rate — compare against previous runs yourself | 2026-07-24 |

What the three card audits check (all vs regex-parsed decompiled C# in
`decompiled/MegaCrit.Sts2.Core.Models.Cards`): static metadata
(cost/type/rarity/target/keywords/tags), DynamicVars values including
upgraded deltas, and AST-scanned effect-var keys vs factory output.
Deliberate v0.109.0 deviations live in `PATCHED_NECROBINDER_CARD_IDS`
allowlists inside the audit scripts — the procedure for adding one is owned
by sts2-parity-discipline; never "fix" a card back toward the stale
`decompiled/` tree to silence an audit.

Current gate status (2026-07-24): all three card audits pass; the reference
audit reports exactly one gap, encounters → `ToadpolesNormal` missing a test
mention. If you see more than that, you introduced a regression. If you see
zero, someone fixed it — either way, re-run; never trust the doc table.

## Structural coverage gates

Three mechanisms enforce breadth. All are **coverage gates, not behavior
proofs** — `docs/PARITY_COVERAGE_BACKLOG.md:27` says so explicitly, and the
backlog's own history shows why the distinction matters: writing the direct
per-card test is what repeatedly *found* real logic bugs (PrepTime+ granting
4 instead of 6 Vigor, DeathsDoor repeat-count fallback, Neurosurge upgrade
scaling). Passing the gate means "someone looked"; only the assertion
content proves behavior.

### Gate 1 — the all-cards harness (`tests/test_all_cards_unit_coverage.py`)

`test_every_card_has_direct_unit_case` (line 283) parametrizes over every
`CardId` (577) and requires each to be:

1. constructible (`_card_for_test`),
2. effect-registered in `_CARD_EFFECTS` unless it is a status/curse/quest or
   runtime-only card (`_RUNTIME_ONLY_CARD_IDS = {CardId.GENERIC}`, line 57),
3. playable in a generic smoke combat with **all pending choices resolved**
   (`_resolve_all_pending_choices` drains `combat.pending_choice`, asserting
   resolution within 20 steps).

`test_factory_backed_playable_cards_have_smoke_execution` (line 326) replays
every factory-backed playable card through the same harness.

Two facts about this file that bite people:

- It parses **`docs/CARDS_REFERENCE.md` at runtime** (line 87,
  `_reference_cards()`): that doc is load-bearing runtime data, not prose.
  Malformed edits to it fail card tests. (Doc discipline is owned by
  sts2-docs-and-writing.)
- It carries its own `_PATCHED_NECROBINDER_CARD_IDS` set (lines 63-78, 14
  cards) exempting deliberate v0.109.0 deviations from the reference-doc
  comparison — keep it in sync with the audit scripts' allowlists when
  adding a deviation (procedure: sts2-parity-discipline).

### Gate 2 — the name-mention audit (`scripts/parity_reference_audit.py`)

Cross-references every decompiled C# class filename across 8 surfaces
against Python mentions. Surface → reference dir → implementation path
(from `SURFACES` at scripts/parity_reference_audit.py:72-114):

| Surface | Total | Decompiled dir (`decompiled/MegaCrit.Sts2.Core.Models.*`) | Implementation scanned |
|---|---|---|---|
| cards | 577 | `.Cards` | `sts2_env/cards` |
| relics | 290 | `.Relics` | `sts2_env/relics` |
| potions | 64 | `.Potions` | `sts2_env/potions` |
| powers | 260 | `.Powers` | `sts2_env/powers` |
| monsters | 121 | `.Monsters` | `sts2_env/monsters` |
| events | 68 | `.Events` | `sts2_env/events` |
| encounters | 88 | `.Encounters` | `sts2_env/encounters` |
| modifiers | 17 | `.Modifiers` | `sts2_env/run/modifiers.py` |

How a "direct test reference" is counted (this determines how you satisfy
the gate): with `--direct-test-references`, the script AST-parses every test
file and collects text only from `test_*` functions, `Test*` classes, and
the module-level assignments they reference
(`direct_test_reference_text`, line 186). Against that text it matches
aliases of the C# class name: CamelCase, snake_case, UPPER_SNAKE, with
surface suffixes stripped/added (`aliases_for`, line 121). So a docstring
like `"""Matches BorrowedTime.cs: ..."""` inside a test function satisfies
the gate, as does using `CardId.BORROWED_TIME` in the test body. A mention
in a helper function that no test references does NOT count.

Useful narrower invocations:

```
.venv\Scripts\python.exe scripts\parity_reference_audit.py --surface encounters --direct-test-references --include-deprecated --code-implementation-references --show-missing
.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --json
```

### Gate 3 — content-description coverage (`tests/test_content_descriptions.py`)

Generated effect-text descriptions (used by the web UI and observation
tooling) have their own coverage tests, including
`test_power_description_covers_every_power_id` (line 103) which iterates
every `PowerId` (293 members). If you add a `PowerId`, this test forces you
to add a description. Note (2026-07-24): `sts2_env/content/` currently has
uncommitted working-tree edits — the suite passes with them, but re-check
`git status` before citing line numbers there.

## Adding tests per content type

The canonical combat parity recipe (template:
`tests/test_necrobinder_parity.py:40-63`):

```python
"""<Module docstring>"""
import sts2_env.powers  # noqa: F401

from sts2_env.cards.factory import create_card
from sts2_env.cards.necrobinder import create_necrobinder_starter_deck
from sts2_env.core.combat import CombatState
from sts2_env.core.enums import CardId, PowerId
from sts2_env.core.rng import Rng
from sts2_env.monsters.act1_weak import create_shrinker_beetle


def _make_combat() -> CombatState:
    combat = CombatState(
        player_hp=70, player_max_hp=70,
        deck=create_necrobinder_starter_deck(),
        rng_seed=42, character_id="Necrobinder",
    )
    creature, ai = create_shrinker_beetle(Rng(42))
    combat.add_enemy(creature, ai)
    combat.start_combat()
    return combat


def test_borrowed_time_gains_energy_and_applies_power():
    """Matches BorrowedTime.cs: gain energy, then apply BorrowedTimePower."""
    combat = _make_combat()
    combat.hand = [create_card(CardId.BORROWED_TIME)]
    combat.energy = 1
    assert combat.play_card(0)
    assert combat.energy == 4
    assert combat.player.get_power_amount(PowerId.BORROWED_TIME) == 1
```

The pattern: pinned seed (almost always 42), overwrite `combat.hand` /
`combat.energy` / HP to a hand-crafted scenario, execute ONE action, assert
**exact** numbers a human derived by reading the decompiled C#, and cite the
C# filename in the docstring (which simultaneously satisfies the audit
gate). 47 test files carry `.cs` citations; 25 mention `decompiled`
literally (counted 2026-07-24). Run-layer variant: build
`RunState(seed=..., character_id=...)` or a `RunManager` and assert exact
HP/phase transitions (see `tests/test_relic_rest_site_hooks_parity.py`).

Per-content-type checklist — what a new X must ship with before the gates
pass (implementation registration mechanics are owned by
sts2-architecture-contract; this is the test side):

| Content type | Structural gates that will fail without a test | Minimum new test |
|---|---|---|
| Card | Gate 1 (all-cards harness: constructible + registered + smoke-playable) AND Gate 2 (cards surface) | direct per-card parity test with `.cs` docstring; if it opens choices, the smoke harness must be able to drain them |
| Power | Gate 2 (powers) AND Gate 3 (description per PowerId) | lifecycle/hook parity test + description entry |
| Relic | Gate 2 (relics) | hook parity test (combat or rest-site level) |
| Potion | Gate 2 (potions) | registry/usage test (pattern: `test_potion_registry_usage_and_generation.py`) |
| Monster | Gate 2 (monsters) | AI state-machine parity test (pattern: `test_monster_ai_state_machine_parity.py`; move-state names are parity-checked) |
| Event | Gate 2 (events) | event flow test incl. event-RNG derivation if it rolls |
| Encounter | Gate 2 (encounters) | setup/pool parity test (pattern: `test_encounter_setup_and_pools_parity.py`) — this is the surface currently carrying the ToadpolesNormal gap |
| Modifier | Gate 2 (modifiers) | modifier-hook test |

Where to put it: match the existing file for that character/act/surface
(see the orientation map above) rather than creating new files; the audit
scans all of `tests/` so location does not affect the gate, but discovery
does.

**RNG caveat when writing combat-only tests:** a standalone `CombatState`
(no attached RunState) silently routes every named RNG stream to the single
combat-local `Rng(rng_seed)` — stream-routing bugs are invisible at this
level and only surface in run-level or seed tests. If your change touches
stream routing, add a run-level test too. Stream rules and porting
discipline: sts2-parity-discipline.

## The golden / certified inventory

There are **no recorded golden data files committed to the repo**
(`git ls-files "*.json"` → only `bridge_mod/STS2BridgeMod.json`,
`bridge_mod/mod_manifest.json`, `docs/TRAINING_REVAMP_SPEC.json`, verified
2026-07-24). The "golden" layer is two things:

### 1. Inline seed-pinned constants — `tests/test_rng_parity.py`

This file certifies bit-exactness against C# `System.Random` and is the
single most load-bearing test file in the repo. It pins:

- The exact first five draws of `Rng(0).next_int(0, 2_147_483_646)`:
  `1559595546, 1755192844, 1649316166, 1198642031, 442452829` (lines 9-21).
- Every named stream's derived seed from `deterministic_hash_code("42")`,
  e.g. `Rng(seed, "shuffle").seed == 1_089_005_703`,
  `Rng(seed+1, "rewards").seed == 2_616_644_287` (lines 24-42), and the same
  via `RunRngSet(42)` attributes (lines 45-60).
- Event RNG derivation (`AromaOfChaos` → `3_201_353_244`, lines 63-72).
- The C# Fisher-Yates shuffle order: `[1,2,3,4,5]` → `[3,2,5,1,4]` with
  `rng.counter == 4` (lines 75-82).

If any of these ever fails after your change, you broke RNG parity with the
real game — revert first, understand second. If you add a named stream, pin
its derived seed here in the same style.

### 2. The bridge replay harness — `sts2_env/parity/bridge_replay.py` (864 lines)

The golden-comparison mechanism for real-game parity: record a live bridge
session to a JSON trace, then replay the recorded actions through the
simulator and diff every resulting state. Key exports
(docs/BRIDGE_REPLAY_HARNESS.md:55-64): `BridgeReplayRecorder`,
`save_replay_trace()` / `load_replay_trace()`,
`combat_state_to_bridge_state()`, `run_manager_to_bridge_state()`,
`compare_combat_replay()`, `compare_run_replay()`. Trace format:
`{version, mode, metadata.scenario_factory, initial_state, steps[{action,
resulting_state}]}`. CLI:

```
.venv\Scripts\python.exe -m sts2_env.parity.bridge_replay_cli show artifacts\my_trace.json
.venv\Scripts\python.exe -m sts2_env.parity.bridge_replay_cli compare artifacts\my_trace.json --mode run --factory my_module:make_run_manager
```

What `compare_combat_replay` diffs per step: player HP/block/energy/powers,
hand order + card metadata, enemy order + intents, pile counts, round
number; it stops at the first mismatch and returns a diff list
(docs/BRIDGE_REPLAY_HARNESS.md:191-199). Traces are transient artifacts —
none are committed; each trace must be paired with a deterministic
simulator factory that recreates the same setup.

Hard limitations (docs/BRIDGE_REPLAY_HARNESS.md:243-253, still true
2026-07-24): **no full-run end-to-end comparison**; run comparison covers
actionable slices only; option-label comparison is intentionally loose for
non-combat screens. Recording from a live game (the
`--record-replay` runner flag, mod build, godot.log verification) is owned
by sts2-bridge-and-realgame.

### The Python-side bridge guards are NOT live proof

`tests/test_bridge_autoslay_coverage.py` guards the C# mod's handler wiring
by **statically scanning `bridge_mod/RlAutoSlayer.cs` and decompiled
`CardSelectCmd.cs` with regexes from Python** (lines 6-12) — it proves
handlers are registered in source, not that they work in a running game.
`docs/KNOWN_ISSUES.md` issue 16 is explicit: "no live game available to
verify against in this environment". A green bridge test suite plus green
replay-harness tests does not certify the live bridge. See "Under-tested
areas" below.

## Eval statistics discipline

House rule (all repos): every win-rate or performance claim carries its
protocol — episodes, seeds, deterministic flag, shaping off. Final/external
claims need >= 1000 eval episodes with a Wilson 95% CI. Aspirational targets
(the 95% A10 goal) are always labeled aspirational.

### The reference eval implementation — `run_eval()` in `scripts/train_necrobinder.py`

As of HEAD `fe25668` (2026-07-24), `run_eval` (scripts/train_necrobinder.py:103-142)
defines what "an eval" means here:

- Dedicated env: `make_stage_env(stage_name, shaping_scale=0.0)` — **pure
  sparse reward, shaping off** (`RewardConfig.shaping_scale` clamped to
  [0,1]; 0.0 disables all shaping terms).
- Seed block: `env.reset(seed=EVAL_SEED_BLOCK + ep)` with
  `EVAL_SEED_BLOCK = 10_000_000` (line 71) — disjoint from training seeds.
- Deterministic policy: `model.predict(obs, action_masks=masks,
  deterministic=True)` (line 123).
- Default `EVAL_EPISODES = 200` (line 70); override with
  `--eval-episodes N`.
- Returned telemetry: `win_rate`, `episodes`, `mean_floors`, `mean_act`,
  `truncation_rate`, `deaths_by_act` — the run env's `info` carries
  `floor`/`act` every step (`run_env.py:749-757`) and sets `info["won"]` /
  `info["truncated"]` at episode end (`rich_run_env.py:178-181`).
- Scoring rules baked into the env (committed at `fe25668`): a step-limit
  truncation is **not** scored as a death (reward `cfg.truncation`, default
  0.0, bootstrap instead), and a forced loss from a simulator bug
  (`info["sim_error"]`, set at `run_env.py:364`) is **not** scored as death
  either (`rich_run_env.py:170-177`). `run_eval`'s `win_rate` counts only
  wins; truncations are reported separately as `truncation_rate`. When you
  report, report both — a rising win rate with a rising truncation rate is
  not an improvement claim.

History appends to `<output-dir>/<stage>/eval_history.jsonl`; best
checkpoints save as `best_model.zip` + sidecar JSON. Launch/resume mechanics:
sts2-run-and-operate. Which stage to run and what numbers to expect:
sts2-training-campaign.

### What a claim requires

Adopted doctrine (`docs/TRAINING_REVAMP_SPEC.json` `success_metrics`,
committed 2026-07-24): the primary metric is A10 Necrobinder full 4-act
(Corrupt Heart) win rate on **1000 deterministic eval episodes, shaping
off, seed block 10_000_000+, reported with a Wilson 95% CI**; an improvement
claim must survive the CI (no overlapping CIs from noise). The old 200-ep
default has stderr ~3.4% at p=0.5 — fine for training telemetry, too noisy
for claims.

Wilson 95% CI one-liner (no in-repo implementation exists as of 2026-07-24;
smoke-tested — 124/200 prints `0.620 [0.551, 0.684]`):

```
.venv\Scripts\python.exe -c "import math; w,n=124,200; p=w/n; z=1.959964; d=1+z*z/n; c=p+z*z/(2*n); m=z*math.sqrt(p*(1-p)/n+z*z/(4*n*n)); print(f'{p:.3f} [{(c-m)/d:.3f}, {(c+m)/d:.3f}]')"
```

Replace `w,n` with your wins and episodes. Deeper statistics (eval sizing,
log forensics, ablation discipline) are owned by sts2-analysis-toolkit.

Claim checklist — a win-rate number may leave your terminal only with ALL of:

- [ ] eval env constructed with `shaping_scale=0.0` (never the shaped
      training env)
- [ ] `deterministic=True` with action masks
- [ ] seed block stated (`10_000_000 + ep` unless you have a reason;
      state the reason)
- [ ] episode count stated; >= 1000 for final/external claims
- [ ] Wilson 95% CI attached; improvement claims have non-overlapping CIs
- [ ] `truncation_rate` and `deaths_by_act` reported alongside
- [ ] evaluated at the difficulty you are claiming (an A10 claim runs at
      ascension 10 with 4 acts, not the training stage's ceiling)
- [ ] aspirational targets labeled aspirational

Anti-patterns already paid for once (chronicle: sts2-failure-archaeology):
evaluating on the shaped env, 200-episode "final" claims, scoring
truncation or sim-error as death, and combat-env evals that were blind to
floor/act (every line showed `mean_floors=0.0` — the run-env eval exists
precisely to fix that).

### Random baseline — `scripts/benchmark.py`

```
.venv\Scripts\python.exe scripts\benchmark.py
```

1000 random-valid-action episodes on `STS2CombatEnv`, per-episode seeds
`range(1000)`, prints episodes/sec, steps/sec, avg steps/ep, win rate
(scripts/benchmark.py:8-43). Two uses: performance-regression check
(required before PRs) and a random-policy floor for combat win rates. It is
combat-only; there is no committed full-run random baseline script — if you
need one, `run_eval` semantics with a random policy is the pattern to copy.

## Under-tested areas (2026-07-24) — do not oversell green

| Area | Status | Evidence |
|---|---|---|
| Live game bridge | Never smoke-tested against a running game; C# wiring guarded only by static source scans | `docs/KNOWN_ISSUES.md` issue 16; `docs/BRIDGE_REPLAY_HARNESS.md:248-252`; `tests/test_bridge_autoslay_coverage.py` |
| Full-run replay comparison | Not supported by the harness | `docs/BRIDGE_REPLAY_HARNESS.md:47,245` |
| RNG stream routing in combat-only tests | Falls back to one merged stream; wrong-stream bugs invisible | `CombatState._run_rng` fallback; see sts2-parity-discipline |
| Shop ordering / Foul-Potion sell action on the wire | Unverified / missing | `docs/KNOWN_ISSUES.md` issue 16 |
| Behavior beyond name mentions | Gate 2 counts mentions, not assertions | `docs/PARITY_COVERAGE_BACKLOG.md:27` |
| Encounters surface | 1 missing test mention (`ToadpolesNormal`) | audit run 2026-07-24 |
| The uncommitted revamp validation | The G1-G5 trainer committed at `fe25668` has NOT yet passed a training relaunch; unit tests green ≠ training validated | `git log`; campaign status: sts2-training-campaign |

## Symptom → cause → fix (test-suite failures)

| Symptom | Likely cause | Fix |
|---|---|---|
| Test passes alone, fails in suite (or vice versa) with instance-id assertions | Card instance counter is global; something bypassed the autouse `reset_ids` fixture (e.g. cards built at module import time) | Build cards inside tests/fixtures; call `reset_instance_counter()` in your helper |
| `KeyError`/missing power class in a standalone script | Registration-by-import side effect missing | `import sts2_env.powers` and `import sts2_env.potions` at top |
| `test_every_card_has_direct_unit_case[X]` fails "should play successfully" | New card not effect-registered, broken target routing, or a pending choice the generic harness cannot drain | Register via the `@register_effect` family; ensure choices resolve (single-option non-skippable choices auto-resolve) |
| `test_factory_backed_cards_match_reference_core_metadata` fails after a deliberate v0.109.0 change | Card not in `_PATCHED_NECROBINDER_CARD_IDS` (test file) and/or the audit-script allowlists | Follow the PATCHED-allowlist procedure in sts2-parity-discipline — update all allowlists together |
| Card tests fail after editing `docs/CARDS_REFERENCE.md` | That doc is parsed at runtime by the all-cards test | Restore the `### `-header / `- **Field:** value` format exactly |
| `test_rng_parity.py` fails | You changed RNG derivation or stream naming — this is a real parity break | Revert; consult sts2-parity-discipline before touching `rng.py` |
| `pytest -n auto` or `--cov` errors | xdist / pytest-cov not installed; do not install over `.venv` (house rule 4) | Run single-process, no cov |
| Audit "passes" (exit 0) but coverage regressed | `parity_reference_audit.py` always exits 0 | Read the Missing columns / use `--json` and check counts |
| `test_power_description_covers_every_power_id` fails | New `PowerId` without a description | Add it in `sts2_env/content/descriptions.py` |
| Collection errors (before any test runs) | Import failure in some module — collection imports everything | `.venv\Scripts\python.exe -m pytest tests/ --collect-only -q` and read the first traceback |

## Provenance and maintenance

Every dated fact above, with a one-line re-verification command (run from
repo root). If a command's output disagrees with this file, the repo wins —
update this file.

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| HEAD `fe25668`, dirty `sts2_env/content/` + `web/play_run.py` | `git log -1 --oneline; git status --short` |
| 132 tracked files under `tests/` | `git ls-files tests/ | find /c /v ""` (PowerShell: `(git ls-files tests/).Count`) |
| 5,276 tests collected (~1.5s) | `.venv\Scripts\python.exe -m pytest tests/ --collect-only -q` |
| 5,276 passed (~25s) | `.venv\Scripts\python.exe -m pytest tests/ -q` |
| pytest 9.1.1; no pytest-cov/xdist | `.venv\Scripts\python.exe -m pip list \| findstr pytest` |
| pytest config = `testpaths` only | `findstr /c:"[tool.pytest.ini_options]" /c:"testpaths" pyproject.toml` |
| `reset_instance_counter` at `sts2_env/cards/base.py:544` | `findstr /n "def reset_instance_counter" sts2_env\cards\base.py` |
| All-cards gate parametrizes 577 CardIds at line 282-283 | `findstr /n "parametrize" tests\test_all_cards_unit_coverage.py` |
| `parity_reference_audit.py` always exits 0 | `.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references; $LASTEXITCODE` (PowerShell) |
| ToadpolesNormal is the only missing test mention | `.venv\Scripts\python.exe scripts\parity_reference_audit.py --surface encounters --direct-test-references --include-deprecated --code-implementation-references --show-missing` |
| Three card audits pass, exit 1 on mismatch | run each; `findstr /n "return 1" scripts\audit_card_static_metadata.py` |
| RNG pins (streams, Fisher-Yates) | `.venv\Scripts\python.exe -m pytest tests/test_rng_parity.py -q` |
| `run_eval` semantics (lines 70-71, 103-142) | `findstr /n "EVAL_SEED_BLOCK shaping_scale deterministic" scripts\train_necrobinder.py` |
| Eval doctrine (1000 ep, Wilson CI) in committed spec | `.venv\Scripts\python.exe -c "import json; print(json.load(open('docs/TRAINING_REVAMP_SPEC.json'))['success_metrics'][:200])"` |
| No committed replay traces | `git ls-files *.json` |
| 47 test files cite `.cs`; 25 mention `decompiled` | `grep -rl ".cs" tests/*.py` via Git Bash, or `(Get-ChildItem tests\test_*.py | Select-String -List "\.cs").Count` |
| Harness limitations (no full-run compare) | read `docs/BRIDGE_REPLAY_HARNESS.md` "Current Limitations" |
| KNOWN_ISSUES 16 live-bridge gaps still open | read `docs/KNOWN_ISSUES.md` issue 16 status |

Volatile facts most likely to drift: the test count (grows with every
content addition), the ToadpolesNormal gap (someone will fix it), the dirty
working tree (a concurrent session is active), `CONTRIBUTING.md`'s stale
counts (may get corrected), and the trainer's eval constants if the revamp
iterates. Re-verify all five before relying on them after 2026-07-24.
