---
name: sts2-docs-and-writing
description: >
  Load this skill whenever you need to decide which sts2-rl-agent document to
  trust, or you are about to write or edit any documentation in this repo:
  triaging stale docs (README, TRAINING_GUIDE, PARITY_GAPS...), editing
  docs/CARDS_REFERENCE.md (it is parsed at RUNTIME - treat as code), adding an
  entry to the docs/KNOWN_ISSUES.md ledger, writing a new doc or postmortem,
  interpreting the Chinese-language historical docs, fixing README debt, or
  applying house code/writing style. Do NOT load it for the CONTENT of other
  domains: training doctrine and launch commands live in
  sts2-training-campaign, bridge protocol truth in sts2-bridge-and-realgame,
  parity audit mechanics in sts2-parity-discipline, incident narratives in
  sts2-failure-archaeology, test-suite anatomy in sts2-testing-and-qa, and the
  rules for landing any change in sts2-change-control. This skill owns: which
  doc is authoritative for what, how to keep the written record honest, and
  how to write new record entries that will not rot.
---

# sts2-docs-and-writing: docs-of-record, staleness map, and writing discipline

This repo has 639+ commits, three sharply separated development eras, and a
documentation set where the newest doc is one day old and the oldest is four
months old and partly in Chinese. Several documents confidently recommend a
training script that crashes on construction. One "documentation" file is
actually runtime data parsed by the simulator. This skill tells you exactly
which document to trust for what, and how to write documentation here without
adding to the rot.

Jargon used below, defined once:

- **doc-of-record**: the single file that is authoritative for a topic; every
  other file mentioning that topic is either a pointer or drift.
- **era**: one of the three development bursts (Mar 2026 bootstrap, May 2026
  parity blitz, Jul 2026 Necrobinder campaign) separated by ~2-month gaps.
- **parity**: the simulator matching the decompiled C# game source
  (`decompiled_v0.109.0/` is current ground truth). Owned by
  sts2-parity-discipline.
- **ledger**: `docs/KNOWN_ISSUES.md`, the living numbered incident/limitation
  record.
- **legacy obs / rich obs**: the old 131-dim (combat) / 151-dim (run)
  observation vectors vs. the current 4184-dim rich observation
  (`RICH_OBS_SIZE`, `sts2_env/gym_env/rich_observation.py`).
- **AFTP / Act4Heart**: the two ACTIVE game mods (ActsFromThePast adds legacy
  acts; Act4Heart adds the Act-4 Corrupt Heart finale). Content details in
  sts2-game-and-mods-reference.
- **PBRS**: potential-based reward shaping, the reward scheme adopted by the
  training revamp. Details in sts2-training-campaign.
- **Osty**: Necrobinder's summonable ally pet (implemented as an ally monster,
  not a player creature).

## 0. Live-state warning (as of 2026-07-24, HEAD = fe25668)

The repo is under ACTIVE concurrent development. During the writing of this
skill (2026-07-24 midday), the working tree changed under us twice: commit
`fe25668` (2026-07-24 11:52, "Phase 0 training revamp") landed the G1-G5
full-run-only trainer and committed `docs/TRAINING_REVAMP_SPEC.json`, and a
separate session is currently modifying `sts2_env/content/descriptions.py`,
`sts2_env/web/play_run.py`, and CLI/web preview files (uncommitted).

Before repeating ANY volatile fact from this skill or from any doc, run:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
git log --oneline -5
git status --short
```

and prefer what the tree says over what any document (including this one)
says. Every dated fact below carries its verification command in the final
"Provenance and maintenance" section.

## 1. The three documentation eras

Trust in this repo is stratified by WHEN a doc was last really written, and
`git log -1` per file is the instrument. Memorize the one-liner:

```powershell
git -C C:\Users\motqu\GitHub\sts2-rl-agent log -1 --format="%h %ci" -- docs/TRAINING_REDESIGN.md
```

| Era | Dates | What was written | Default trust level |
|---|---|---|---|
| 1. Bootstrap | 2026-03-16..18 | RESEARCH.md, DECOMPILED_ARCHITECTURE.md, GAME_SYSTEMS_REFERENCE.md, TRAINING_GUIDE.md, TROUBLESHOOTING.md, POWERS/RELICS_REFERENCE.md, the Chinese bridge design notes | Historical. Mechanism explanations may still be right; every number, path, and recommendation is suspect |
| 2. Parity blitz | 2026-05-18..22 | README rewrite, CONTRIBUTING.md, PARITY_GAPS.md, SIMULATOR_ARCHITECTURE.md, PROTOCOL.md, BRIDGE_REPLAY_HARNESS.md, MONSTERS_REFERENCE.md | Accurate for the LEGACY envs and the May-era bridge; blind to the rich obs stack, the mods, and the July campaign |
| 3. Necrobinder campaign | 2026-07-23.. | KNOWN_ISSUES.md, MOD_BUILD_GUIDE.md, TRAINING_REDESIGN.md, AGENT_USAGE_GUIDE.md (all `0078f70`), CARDS_REFERENCE.md touch-up (`18a8059`), TRAINING_REVAMP_SPEC.json (`fe25668`) | Current era. Still not uniformly fresh - see the map below |

Two hard rules that fall out of the era structure:

1. **A July commit date does not mean July-fresh content.** `0078f70`
   (2026-07-23) is a giant squash commit; it touched AGENT_USAGE_GUIDE.md
   without fixing its stale sections (it still recommends the broken
   `scripts/train_full_run.py` at lines 81-123). Check the CLAIMS, not just
   the date.
2. **Anything only touched before 2026-07-23 describes the pre-campaign
   world**: no Necrobinder mod defaults, no rich obs, no G-ladder, no
   v0.109.0 alignment.

## 2. The staleness map (verified 2026-07-24)

Per-file verdicts. "Last commit" verified with the `git log -1` one-liner
above on 2026-07-24.

| File | Last commit | Verdict and notes |
|---|---|---|
| `docs/TRAINING_REVAMP_SPEC.json` | fe25668 2026-07-24 | **Doc-of-record for campaign doctrine.** Adopted G1-G5 plan, PBRS reward spec, stage-A postmortem. Changes to it are campaign-doctrine class - route through sts2-change-control |
| `scripts/train_necrobinder.py` (module docstring) | fe25668 2026-07-24 | **Doc-of-record for how to launch training.** G1-G5 stage table, usage lines, checkpoint/eval layout. Operating detail in sts2-run-and-operate |
| `docs/KNOWN_ISSUES.md` | 0078f70 2026-07-23 | **The ledger.** Current, with two internal drift spots (section 8 below) |
| `docs/MOD_BUILD_GUIDE.md` | 0078f70 2026-07-23 | Current for building/deploying the bridge mod (sts2-bridge-and-realgame owns the procedure) |
| `docs/TRAINING_REDESIGN.md` | 0078f70 2026-07-23 | **Split verdict.** Its "Why the previous attempt got 0%" postmortem and LLM-policy rejection remain authoritative history. Its stage A-F curriculum, annealed reward, and 200-ep eval sections are SUPERSEDED by TRAINING_REVAMP_SPEC.json + fe25668 (stages are now G1-G5, full-run only, never-halt, PBRS direction, 1000-ep final evals). Its "Fidelity prerequisites" checklist says TheCity/TheBeyond "in progress" - one day staler than commit 6860139 ("Complete Acts from the Past mod", 2026-07-24) |
| `docs/AGENT_USAGE_GUIDE.md` | 0078f70 2026-07-23 | Current ONLY for legacy-model bridge deployment. Sections at lines 81-123 recommend broken `train_full_run.py`; never mentions `train_necrobinder.py`, the rich env, or the fact that rich 4184-dim models are REJECTED by `detect_model_mode` (`sts2_env/bridge/agent_runner.py:132-165` accepts only 115/131 and 157/151). That gap is undocumented anywhere except KNOWN_ISSUES-adjacent code comments - see sts2-bridge-and-realgame |
| `docs/CARDS_REFERENCE.md` | 18a8059 2026-07-24 | **Load-bearing runtime data, not prose** - see section 6. Content is pre-v0.109.0 except one corrected entry (BorrowedTime, fixed in 18a8059); 14 Necrobinder entries known-wrong by design |
| `README.md` | 58a8af5 2026-05-22 | Heavily stale; nearly every number is wrong. Full debt list in section 4. Fine for the architecture diagram's SHAPE, wrong on its labels |
| `CONTRIBUTING.md` | 4ca4268 2026-05-21 | Split verdict - see section 5. Code style and add-content recipes verified still accurate; stats, paths, and env-setup nuance stale |
| `docs/PROTOCOL.md` | c00a4b8 2026-05-22 | Wire-format base reference, but predates the 2026-07-23 bridge extension that added run-state fields (`act`, `floor`, `gold`, `relics`, ... via `RunStateBridgeFields.Apply()` - documented in KNOWN_ISSUES #16, absent from PROTOCOL.md). Current truth: sts2-bridge-and-realgame |
| `docs/BRIDGE_REPLAY_HARNESS.md` | 25afd99 2026-05-22 | Harness mechanics doc; predates July bridge changes. Cross-check with sts2-bridge-and-realgame before relying on payload details |
| `docs/PARITY_GAPS.md` | 25afd99 2026-05-22 | Self-declared snapshot "As of 2026-05-18". Its residual-blocker list predates the entire v0.109.0 alignment (0078f70, 6860139, 18a8059) and the July dotnet install - do NOT cite its open items as current. Current parity status: sts2-parity-discipline |
| `docs/SIMULATOR_ARCHITECTURE.md` | 913558b 2026-05-22 | Accurate for legacy envs (Discrete(115)/131-dim, 151=131+20); omits the entire rich stack (`rich_observation.py`, `rich_combat_env.py`, `rich_run_env.py`, `reward_config.py`) and all mod content |
| `docs/PARITY_COVERAGE_BACKLOG.md` | 89c4b87 2026-05-21 | Historical backlog snapshot |
| `docs/MONSTERS_REFERENCE.md` | 09dca86 2026-05-18 | Pre-mod, pre-v0.109.0 convenience snapshot |
| `docs/POWERS_REFERENCE.md`, `docs/RELICS_REFERENCE.md` | 81f6d6b 2026-03-16 | Headers say "auto-generated" but NO generator exists in the repo (section 7). Counts stale: doc says 260 powers / 290 relics; live enums are 293 / 308 |
| `docs/GAME_SYSTEMS_REFERENCE.md` | 81f6d6b 2026-03-16 | March-era mechanics notes from the OLD decompile. Verify any number against `decompiled_v0.109.0/` before use |
| `docs/TRAINING_GUIDE.md` | 4688743 2026-03-16 | **Fully superseded.** Recommends broken `train_full_run.py`; claims "Tested hardware: RTX 4070 Ti SUPER" while the actual campaign hardware is an RTX 4060 Laptop 8GB (TRAINING_REDESIGN.md:30 is operative). Do not follow |
| `docs/DECOMPILATION_GUIDE.md` | 4688743 2026-03-16 | March-era; re-decompilation workflow now owned by sts2-build-and-env |
| `docs/TROUBLESHOOTING.md` | 0bfd5b2 2026-03-17 | Bridge bring-up lore (energy bug, thread safety, PCK format) still referenced by KNOWN_ISSUES and still true; but its obs advice ("OBS_SIZE = 131", line ~352) is stale for the rich stack |
| `RESEARCH.md` | 81f6d6b 2026-03-16 | Historical pre-project research, largely in Chinese (section 9) |
| `DECOMPILED_ARCHITECTURE.md` | 81f6d6b 2026-03-16 | Historical, Chinese; cites a nonexistent `extraction/decompiled/` path (actual tree: `decompiled/` at repo root, itself the OLD patch) |
| `docs/AUTOSLAY_BRIDGE.md`, `docs/GAME_BRIDGE_REFERENCE.md` | 81f6d6b 2026-03-16 | Chinese bridge design notes; superseded by the built mod + MOD_BUILD_GUIDE + PROTOCOL |

Ground-truth precedence for game behavior (one-line restatement; owned by
sts2-game-and-mods-reference and sts2-parity-discipline):
`decompiled_v0.109.0/` > `decompiled/` (old patch, still parsed by some audit
tooling) ; `decompiled_mods/` for AFTP + Act4Heart. The `docs/*_REFERENCE.md`
files are convenience snapshots of the OLD decompile - cite the decompiled
tree, not the snapshot, for any parity claim.

## 3. Training-doctrine document precedence

Three documents describe training. Their precedence order (as of 2026-07-24):

1. `docs/TRAINING_REVAMP_SPEC.json` (fe25668) - ADOPTED doctrine: G1-G5
   full-run-only ladder, PBRS reward spec, never-halt curriculum, honest
   ceiling statement ("95% is a stretch target, not a gate").
2. `scripts/train_necrobinder.py` docstring (fe25668) - what the code
   actually does right now (Phase 0 of the spec is implemented; later phases
   - PBRS rewrite, deck-bag obs, per-slot policy, BC/SIL/state-bank/MCTS -
   are spec'd but NOT yet implemented as of fe25668).
3. `docs/TRAINING_REDESIGN.md` (0078f70) - historical for its curriculum and
   reward sections; still authoritative for the 0%-win postmortem and the
   LLM-policy rejection rationale.
4. `docs/TRAINING_GUIDE.md` (2026-03) - do not follow.

When writing anything about training: name the stage letters correctly
(G1-G5, not A-F, since fe25668), never present the old A-F table as current,
and label unimplemented spec phases as "spec'd, not implemented" with a date.
The campaign content itself is owned by sts2-training-campaign - link it,
don't restate it.

## 4. README debt list (every item verified 2026-07-24)

README.md (last real edit 2026-05-22) is the single most misleading file in
the repo because it looks authoritative. If you are asked to fix it, this is
the verified debt; if you are reading it, this is the correction table.

| README claim (location) | Verified reality (2026-07-24) |
|---|---|
| 131-dim observation (README:15, 306) | Legacy combat obs is still 131 (`observation.py`, `OBS_SIZE=131`) but the campaign trains the rich stack: `RICH_OBS_SIZE=4184` (`rich_observation.py`) |
| Discrete(61) combat / Discrete(100) run (README:13-17, 302-307) | `ACTION_SPACE_SIZE=115` (combat), run env Discrete(157) with 151-dim legacy obs (`run_env.py`, `_LAYOUT.total_actions=157`, `RUN_OBS_SIZE=151`) |
| Training via `train_combat.py` + `train_full_run.py` (README:27-28, 149) | Entrypoint of record is `scripts/train_necrobinder.py` (G1-G5). **`scripts/train_full_run.py` is BROKEN**: it passes `act_count=`/`reward_shaping=` kwargs (train_full_run.py:25-27) that `STS2RunEnv.__init__` does not accept (signature verified by import: `character_id, ascension_level, max_steps, max_combat_turns, render_mode`) - TypeError at env construction. `train_combat.py` still runs and has `--character`/`--ascension` but trains the legacy 131-dim env |
| 408 test functions, 14 test files (README:38, 264) | 5,276 collected tests, ~131 `tests/test_*.py` files (`pytest tests --collect-only -q`) |
| 577 cards / 260 powers / 290 relics (README:39-42) | `len(CardId)=586`, `len(PowerId)=293`, `len(RelicId)=308` (verified by import). README's 121 monsters / 63 potions are pre-mod counts (not re-verified; mod acts added monsters) |
| 133 source files, ~50,000 LOC (README:36-37) | ~287 `.py` files (sts2_env+scripts+tests, incl. in-flight untracked), 11 hand-written `.cs` files in `bridge_mod/`; ~132k Python LOC per the July audit |
| ~1,200 combats/sec, ~92% Act 1 Ironclad win rate (README:45-46) | UNVERIFIED legacy claims - do not repeat without rerunning `scripts/benchmark.py` and a fresh eval with protocol (see sts2-analysis-toolkit) |
| Documentation table (README:321-337) | Omits TRAINING_REDESIGN.md, TRAINING_REVAMP_SPEC.json, AGENT_USAGE_GUIDE.md, PARITY_GAPS.md, PARITY_COVERAGE_BACKLOG.md, BRIDGE_REPLAY_HARNESS.md, DECOMPILATION_GUIDE.md, and all four `*_REFERENCE.md` docs |

A README rewrite is a docs-class change (see sts2-change-control): no full
test suite needed, but every number you write must carry a re-verification
command or it will rot exactly like the current one did.

## 5. CONTRIBUTING.md: trust the recipes, not the stats

Verified split (2026-07-24):

**Still accurate (verified against code):**
- Code style conventions (CONTRIBUTING.md:90-103). House style, restated:
  Python 3.11+ features welcome (`match`, `type` aliases, `X | Y`);
  type hints on all signatures; `from __future__ import annotations` at the
  top of every module; Google-style docstrings, module docstring per file;
  imports grouped stdlib/third-party/local with `TYPE_CHECKING` blocks for
  cycle-prone hints; game constants in `core/constants.py`, enums in
  `core/enums.py`; `snake_case.py` files, `PascalCase` classes,
  `UPPER_SNAKE_CASE` constants and Card/Power/Relic ID enum members matching
  the C# `Id.Entry` names; no global mutable state except the import-time
  card-effect and power-class registries.
- Add-a-card / add-a-power / add-a-monster recipes: `@register_effect` exists
  at `sts2_env/cards/registry.py:136`; `register_power_class` at
  `sts2_env/core/creature.py:18` (note: creature.py, NOT a powers/ module);
  `MoveState(state_id, effect_fn, intents, follow_up_id=None,
  must_perform_once=False)` and `RandomBranchState.add_branch(state_id,
  repeat_type, max_times, weight, cooldown)` match
  `sts2_env/monsters/state_machine.py:42-49` and `:143-150`.
- PR guidelines (one concern per PR, tests required, full suite + benchmark
  before submitting) - consistent with house doctrine; the binding version
  lives in sts2-change-control.

**Stale (do not repeat):**
- "16 test files, 408 test functions" (CONTRIBUTING.md:45) and the 14-file
  test list at :72-87 - reality is ~131 files / 5,276 collected.
- Content counts in the layout tree (:35-39): 577/260/121/290/63 - see
  section 4.
- Every `decompiled/` path in the recipes (:109, :151, :184, :229, :241):
  mechanically the workflow is right, but point it at
  `decompiled_v0.109.0/` (current) instead of `decompiled/` (old patch).
- `pip install -e ".[dev,train]"` (:23) is approximately right but the real
  working env is Store Python 3.13.14 venv + pip editable + uv-installed
  torch 2.11.0+cu128, and **you must never `uv sync`/reinstall over
  `.venv`** - the lock is stale and CPU-only. Environment truth is owned by
  sts2-build-and-env.
- ":261 Regenerate docs/CARDS_REFERENCE.md ... from the new decompiled
  source" - there is no regeneration tool in the repo (section 7).

## 6. docs/CARDS_REFERENCE.md is load-bearing runtime data

This is the single most dangerous documentation trap in the repo: a file in
`docs/` that looks like generated prose is actually parsed at runtime by the
simulator and asserted against by the test suite.

**Consumers (verified 2026-07-24):**

| Consumer | What it does with the file |
|---|---|
| `sts2_env/cards/factory.py:202` (`_reference_cards()`, lru_cached) | Parses `### ` card entries and `- **Field:** value` lines into card metadata used by the card factory |
| `sts2_env/content/descriptions.py:341-344` (line nos. shifting - file is being modified by a concurrent session; anchor on the parse function's docstring "Parse ``docs/CARDS_REFERENCE.md``") | Derives web-UI tooltip effect text for all 586 cards; combines reference parsing with a `_CARD_OVERRIDES` table of curated overrides (~35 at commit 18a8059) |
| `tests/test_all_cards_unit_coverage.py:87` | Structural coverage gate: asserts implemented cards against reference entries |

**Known-wrong-by-design entries:** the file was generated from the
PRE-v0.109.0 decompile (header still says "Total cards parsed: 577"; the live
enum has 586). Fourteen Necrobinder cards were deliberately reworked in code
to match v0.109.0 while the doc still shows pre-patch text. They are
whitelisted as `_PATCHED_NECROBINDER_CARD_IDS` in
`tests/test_all_cards_unit_coverage.py:63-78`: BANSHEES_CRY, BORROWED_TIME,
DANSE_MACABRE, DEATH_MARCH, DEBILITATE_CARD, DIRGE, EIDOLON, HAUNT, REAVE,
SCULPTING_STRIKE, SEANCE, SIC_EM, SOUL_STORM, THE_SCYTHE. Only the
BorrowedTime ENTRY in the doc was corrected (commit 18a8059, 2026-07-24)
because the tooltip generator surfaced the divergence.

**Checklist for editing docs/CARDS_REFERENCE.md** (this is a sim-behavior
adjacent change, not a docs-class change - see sts2-change-control):

1. Confirm the correction against `decompiled_v0.109.0/` (cite file), NOT
   against `decompiled/` or memory of STS1.
2. Preserve the exact machine-readable format: `### CardName` headings and
   `- **Field:** value` lines (both parsers regex on these).
3. If the card is one of the 14 whitelisted Necrobinder entries, decide
   deliberately: correcting the doc entry to v0.109.0 may allow removing the
   card from `_PATCHED_NECROBINDER_CARD_IDS` - do both or neither, with the
   test updated in the same change.
4. Run the FULL suite (house rule - runtime data changed):
   ```powershell
   cd C:\Users\motqu\GitHub\sts2-rl-agent
   .venv\Scripts\python.exe -m pytest tests/ -q
   ```
5. Spot-check tooltips still render:
   ```powershell
   .venv\Scripts\python.exe -c "from sts2_env.cards.factory import create_card; from sts2_env.core.enums import CardId; from sts2_env.content.descriptions import card_description; print(card_description(create_card(CardId.BORROWED_TIME)))"
   ```
   (Verified working 2026-07-24; `card_description` takes a card INSTANCE,
   not a CardId. The module is being modified by an in-flight session - if
   the import fails, re-check its public functions first.)

## 7. The "auto-generated" reference docs have no generator

`docs/POWERS_REFERENCE.md` and `docs/MONSTERS_REFERENCE.md` headers say
"Auto-generated from decompiled source", and CONTRIBUTING.md:261 says
"Regenerate docs/CARDS_REFERENCE.md ... from the new decompiled source" -
but no generator script exists anywhere in the repo (verified 2026-07-24:
grep for `REFERENCE` in `scripts/` finds nothing; the only `.py` files
mentioning `CARDS_REFERENCE` are the three consumers in section 6, and
nothing consumes POWERS/MONSTERS/RELICS_REFERENCE at all). The generators
were never committed by the upstream author.

Consequences:

- You CANNOT regenerate these docs; you can only hand-patch entries (as
  18a8059 did for BorrowedTime) or write a new generator.
- Because CARDS_REFERENCE.md is runtime data (section 6), writing a
  regenerator against `decompiled_v0.109.0/` is a real project, not a doc
  chore: it would change factory metadata and tooltip text for 577+ entries
  and must clear the full suite plus the parity audit scripts
  (`scripts/audit_card_static_metadata.py`, `audit_card_effect_vars.py`,
  `audit_card_dynamic_vars.py`, `scripts/parity_reference_audit.py` - flags
  and semantics owned by sts2-parity-discipline).
- POWERS/MONSTERS/RELICS_REFERENCE have zero code consumers, so hand-editing
  them is safe but low-value; prefer citing `decompiled_v0.109.0/` directly.

## 8. KNOWN_ISSUES.md ledger discipline

`docs/KNOWN_ISSUES.md` is the living incident/limitation ledger: 16 numbered
entries as of 0078f70 (2026-07-23). The incident STORIES are owned by
sts2-failure-archaeology; this section owns how to WRITE ledger entries.

**Structure (as practiced):** two top-level sections, `## Fixed Issues`
(items 1-7) and `## Open Issues` (items 8-16); each item is a
`### N. Short title` with bold-labeled fields.

**Status vocabulary (use these exact meanings):**

| Marker | Meaning |
|---|---|
| `**Status:** Fixed` | Fixed in code, with the fix location cited |
| `**Status:** Fixed (verified against decompiled v0.109.0)` | Fixed and the fix re-checked against current decompiled source |
| `**Status:** Verified correct for v0.109.0` | Checked from decompiled SOURCE ONLY - runtime behavior never tested (no live game in dev env); re-verify on every game update (e.g. item 7) |
| `**Severity:** High/Medium/Low` (no Status) | Open issue; severity, not status |
| documented-not-fixed | Open gaps listed inside an entry deliberately, "rather than silently shipped as 'probably fine'" (item 16 is the model) |

**Known drift INSIDE the ledger (as of 2026-07-24)** - fix these when
touching the file, and do not propagate them:

- Items 10, 13, 14, 15 carry `Status: Fixed` but sit under `## Open Issues`.
  Move resolved items to the Fixed section (keeping their numbers) or accept
  that section headers are unreliable and the per-item Status is the truth.
- Item 9's remedy sentence ("Training scripts need to be extended to support
  character selection") is stale: `scripts/train_combat.py` has had
  `--character`/`--ascension` since 5a676c3 (2026-07-23), and Necrobinder A10
  models exist under `output/`. The underlying limitation framing also
  predates the campaign.
- Item 8's 0%-win framing predates the redesign; the analysis was carried
  into TRAINING_REDESIGN.md and superseded operationally by the revamp spec.

**Template for a new ledger entry** (copy, fill, keep the field order):

```markdown
### N. <Short imperative-free title>

**Status:** Fixed | Fixed (verified against decompiled v0.109.0) | Verified correct for v0.109.0
<or, if open:>
**Severity:** High|Medium|Low (<one-clause scope, e.g. "affects bridge only">)

**Problem:** <symptom first, then mechanism. Past tense for fixed issues.>

**Root causes:** <bulleted, only if multiple>

**Fix:** <what changed, with a code snippet if the pattern is reusable>
<or:> **Workaround:** <operator-level mitigation>
<or:> **Verification (v0.109.0):** <what was checked in decompiled source, with file paths>

**Location:** `<file>` <line/symbol>, ground truth in `decompiled_v0.109.0/<path>`
```

Rules: number sequentially (next free integer - numbers are stable IDs and
never reused); always cite Location with file paths; cite decompiled ground
truth for any parity-adjacent claim; if you found sub-gaps you are NOT
fixing, list them explicitly in the entry (item 16 style) instead of
omitting them. Adding a ledger entry is a docs-class change, but the fix it
describes is gated by its own change class (sts2-change-control).

## 9. The Chinese-language historical docs

Four documents are wholly or largely in Chinese, all from era 1 (81f6d6b,
2026-03-16), all written by the upstream author (`zhiyue/sts2-rl-agent`;
this repo's eras 1-2 are inherited fork history - the July campaign is the
local work):

| File | Content | Residual value |
|---|---|---|
| `RESEARCH.md` | Pre-project research: algorithm selection (MaskablePPO over DQN/AlphaZero/CFR), spire-codex data-pipeline analysis, 4-phase roadmap, prior-work notes | Prior-work list feeds sts2-research-frontier. Its warning that action spaces >~1400 hit numerical issues is unverified folklore (current run space: 157) |
| `DECOMPILED_ARCHITECTURE.md` | Deep analysis of the ORIGINAL decompile for building the simulator | Mechanism narratives still useful for orientation; cites nonexistent `extraction/decompiled/`; every specific number predates v0.109.0 |
| `docs/AUTOSLAY_BRIDGE.md` | Design note: discovering the game's built-in AutoSlay automation framework as the bridge foothold | Historical rationale for why the bridge is AutoSlay-based |
| `docs/GAME_BRIDGE_REFERENCE.md` | Bridge technical design (2-component TCP architecture) | Superseded by the built mod + MOD_BUILD_GUIDE.md + PROTOCOL.md |

When writing: never cite these as current authority; translate/extract the
relevant claim into a current-era doc (with a fresh verification against
`decompiled_v0.109.0/`) instead of linking readers into a stale Chinese doc.

## 10. House writing rules for any new or updated doc

These derive from the user's standing rules (2026-07-24) plus observed repo
practice. They apply to READMEs, docs/, postmortems, commit messages, and
external write-ups alike.

1. **Only real, experiment-backed results.** Every win-rate or performance
   number carries its protocol: episodes, seed block, deterministic flag,
   shaping off. Final claims need >=1000 eval episodes with a Wilson 95%
   confidence interval. The 95% campaign target is ASPIRATIONAL and must be
   labeled as such every time it appears (the revamp spec's own language:
   "a stretch target, not a gate"). Statistics mechanics: sts2-analysis-toolkit.
2. **Parity claims cite decompiled files.** "Matches the game" is only
   writable with a `decompiled_v0.109.0/<path>` citation; deliberate
   deviations go in the PATCHED allowlists (sts2-parity-discipline), not in
   prose disclaimers.
3. **Date-stamp volatile facts.** Any count, win rate, file list, or status
   gets "(as of YYYY-MM-DD)" and ideally a re-verification command. The
   repo's best example is PARITY_GAPS.md's "As of 2026-05-18" self-dating -
   it is what makes that doc safely ignorable today instead of silently
   misleading.
4. **State the doc's era and supersession explicitly.** New docs that replace
   old ones must say so in both files (PARITY_GAPS.md's "the previous
   version ... is superseded by this rewrite" is the pattern). Never leave
   two docs claiming the same authority.
5. **Unproven = labeled open/candidate.** Do not write "should work",
   "probably fine", or silent omissions; the ledger's item-16 style
   (enumerate the unverified gaps) is mandatory practice.
6. **Commit messages are part of the record.** July-era practice (read
   `git show 18a8059 -s` for the model): bulleted what-and-why, decompiled
   citations for parity changes, and the suite result line
   ("Suite: 5276 passed, 0 failed"). Follow it; the gate itself is
   sts2-change-control's.
7. **One home per fact.** Before writing, check whether a doc-of-record
   already owns the topic (section 2 map); update the owner and link it,
   instead of restating in a second file. Restatements are how README got
   nine wrong numbers.
8. **Documentation changes are still changes.** Docs-class edits need no
   test suite run, EXCEPT: `docs/CARDS_REFERENCE.md` (runtime data - full
   suite, section 6), anything embedding commands (run the command before
   committing it), and campaign-doctrine documents
   (`docs/TRAINING_REVAMP_SPEC.json`) which require explicit user approval
   per sts2-change-control.

**Template header for a new doc in `docs/`:**

```markdown
# <Title>

<One-paragraph purpose. Who reads this and when.>

Status: current | snapshot | historical. As of YYYY-MM-DD (HEAD <shorthash>).
Supersedes: <file or "nothing">. Superseded by: <file or "nothing yet">.
Ground truth: <decompiled_v0.109.0/ paths, code paths, or experiment logs>.
```

## Provenance and maintenance

All facts verified directly against the repo on 2026-07-24 (HEAD = fe25668,
working tree carrying uncommitted web/CLI preview work). Re-verify with the
commands below; run everything from `C:\Users\motqu\GitHub\sts2-rl-agent`.

| Fact (as of 2026-07-24) | Re-verification one-liner |
|---|---|
| HEAD = fe25668; revamp spec committed; tree has uncommitted preview work | `git log --oneline -3 ; git status --short` |
| Per-doc last-commit dates in the staleness map | `git log -1 --format="%h %ci" -- <file>` |
| Era boundaries (Mar 16-18, May 18-22, Jul 23-24 activity) | `git log --format="%ad" --date=short \| sort \| uniq -c` (Git Bash) |
| 5,276 tests collected, ~131 test files | `.venv\Scripts\python.exe -m pytest tests --collect-only -q` |
| OBS_SIZE=131, RICH_OBS_SIZE=4184, combat Discrete(115), run Discrete(157)/151 | `.venv\Scripts\python.exe -c "from sts2_env.gym_env.observation import OBS_SIZE; from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; from sts2_env.gym_env.action_space import ACTION_SPACE_SIZE; from sts2_env.gym_env.run_env import RUN_OBS_SIZE, _LAYOUT; print(OBS_SIZE, RICH_OBS_SIZE, ACTION_SPACE_SIZE, RUN_OBS_SIZE, _LAYOUT.total_actions)"` |
| 586 cards / 293 powers / 308 relics | `.venv\Scripts\python.exe -c "from sts2_env.core.enums import CardId, PowerId; from sts2_env.relics import RelicId; print(len(CardId), len(PowerId), len(RelicId))"` (note: RelicId imports from `sts2_env.relics`, not `core.enums`) |
| `train_full_run.py` still broken (kwarg drift) | `.venv\Scripts\python.exe -c "import inspect; from sts2_env.gym_env.run_env import STS2RunEnv; print(inspect.signature(STS2RunEnv.__init__))"` then compare with `scripts/train_full_run.py:25-27` |
| Trainer of record = G1-G5 never-halt ladder | `.venv\Scripts\python.exe -c "print(open('scripts/train_necrobinder.py').read(1800))"` |
| CARDS_REFERENCE consumers (3) and no other REFERENCE consumers | `grep -rn "CARDS_REFERENCE" --include=*.py sts2_env tests scripts` and same for `POWERS_REFERENCE\|MONSTERS_REFERENCE\|RELICS_REFERENCE` (Git Bash; expect zero hits for the latter) |
| 14-card Necrobinder whitelist | `grep -n "_PATCHED_NECROBINDER_CARD_IDS" -A 16 tests/test_all_cards_unit_coverage.py` |
| CARDS_REFERENCE header still "577 parsed"; only BorrowedTime corrected | `head -3 docs/CARDS_REFERENCE.md ; git log --oneline -3 -- docs/CARDS_REFERENCE.md` |
| `detect_model_mode` accepts only 115/131 and 157/151 | `grep -n "def detect_model_mode" -A 35 sts2_env/bridge/agent_runner.py` |
| KNOWN_ISSUES ledger: 16 items; items 10/13/14/15 marked Fixed under Open | `grep -n "^### \|^\*\*Status\|^\*\*Severity" docs/KNOWN_ISSUES.md` |
| `train_combat.py` has `--character`/`--ascension` (item-9 remedy stale) | `grep -n "character\|ascension" scripts/train_combat.py \| head` |
| No generator for the *_REFERENCE docs | `grep -rn "REFERENCE" scripts/` (expect empty) |
| Chinese-language docs unchanged since 81f6d6b | `git log -1 --format=%h -- RESEARCH.md DECOMPILED_ARCHITECTURE.md docs/AUTOSLAY_BRIDGE.md docs/GAME_BRIDGE_REFERENCE.md` |
| CONTRIBUTING recipes still match code | `grep -n "def register_effect\|register_power_class" sts2_env/cards/registry.py sts2_env/core/creature.py` |
| PROTOCOL.md missing July run-state bridge fields | `grep -n "act_floor\|relic_count\|RunStateBridgeFields" docs/PROTOCOL.md` (expect empty) vs `docs/KNOWN_ISSUES.md` item 16 |

Maintenance triggers: after any commit touching `docs/`, refresh the
staleness map dates; after the in-flight web-preview session lands, re-check
`sts2_env/content/descriptions.py` symbol names and line numbers in section
6; after any game update, assume every `*_REFERENCE.md` drifted further and
re-run the parity audits (sts2-parity-discipline); if `README.md` gets
rewritten, delete section 4 of this skill and replace it with a pointer.
