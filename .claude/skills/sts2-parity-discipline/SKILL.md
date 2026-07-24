---
name: sts2-parity-discipline
description: >
  Load this skill whenever you are (a) porting any behavior from the decompiled
  Slay the Spire 2 C# into the Python simulator, (b) adding or changing a card,
  power, relic, potion, monster, event, or encounter, (c) writing or reviewing a
  parity test, (d) running or interpreting the four parity audit scripts,
  (e) porting a random call / choosing an RNG stream, (f) recording a deliberate
  deviation from the pre-patch decompile (PATCHED allowlists), or (g) writing any
  sentence that claims the simulator "matches the game". Do NOT load it for:
  pytest suite mechanics, fixtures, or eval statistics (use sts2-testing-and-qa);
  what a specific card/mod/ascension actually does (use
  sts2-game-and-mods-reference); why the engine is designed the way it is —
  pending_choice model, hook dispatch design, two RNG roots rationale (use
  sts2-architecture-contract); the checklist for landing a change (use
  sts2-change-control); live-game bridge replay/golden traces (use
  sts2-bridge-and-realgame); or the history of past parity incidents (use
  sts2-failure-archaeology).
---

# STS2 Parity Discipline

Runbook for keeping the Python simulator (`sts2_env/`) verifiably faithful to
the decompiled Slay the Spire 2 C# source, and for never overclaiming that
faithfulness. Repo root is `C:\Users\motqu\GitHub\sts2-rl-agent`; every command
below assumes you run it from there with the project venv python
(`.venv\Scripts\python.exe`, never bare `python`).

Jargon used throughout, defined once:

- **Parity** — the property that a simulator behavior produces the exact same
  numbers, ordering, and RNG consumption as the decompiled C# game code. It is
  claimed per-surface and per-test, never globally (see "What parity is NOT").
- **Ground truth** — the decompiled C# trees at repo root. `decompiled/` is the
  older pre-patch decompile; `decompiled_v0.109.0/` is the current-game (beta
  v0.109.0) decompile; `decompiled_mods/` covers the ActsFromThePast and
  Act4Heart mods. Precedence rules are in "Two decompiled trees" below.
- **PATCHED allowlist** — the `PATCHED_NECROBINDER_CARD_IDS` frozensets inside
  two audit scripts that enumerate deliberate deviations from the stale
  `decompiled/` reference toward the newer `decompiled_v0.109.0/` values.
- **Osty** — Necrobinder's pet, implemented as an ally `Creature` with an
  `is_osty` flag (not a player creature). It matters here only because the
  damage pipeline has Osty-specific redirect hooks whose ordering is parity-audited.
- **Coverage gate** — `scripts/parity_reference_audit.py`, a name-mention
  heuristic (NOT a behavior proof) that every decompiled surface class is
  referenced in implementation code and in a direct test.

Sibling ownership reminder: this skill owns the *discipline* (how to audit, how
to port, how to test, how to record deviations, how to phrase claims). The
change-landing gates live in **sts2-change-control**; per-content test-writing
mechanics live in **sts2-testing-and-qa**; engine design rationale lives in
**sts2-architecture-contract**.

---

## 1. What "parity" means operationally

A behavior is "at parity" in this repo when ALL of the following hold:

1. **Machine-checked static/dynamic metadata equality.** Card cost, type,
   rarity, target type, keywords, tags, upgrade deltas, and DynamicVars values
   are regex-parsed out of `decompiled/MegaCrit.Sts2.Core.Models.Cards/*.cs`
   by `sts2_env/cards/reference_static_metadata.py` (reference dir pinned at
   `sts2_env/cards/reference_static_metadata.py:16`) and compared against the
   Python card factories by the audit scripts (section 2).
2. **A hand-written behavior test whose docstring cites the C# file.** The test
   constructs the scenario directly, executes one action, and asserts exact
   numbers a human derived by reading the decompiled source (section 4).
3. **A name mention counted by the coverage gate** for the surface class
   (section 3) — this is what makes the class show up as "covered".
4. **Bit-exact RNG consumption** where randomness is involved: same stream,
   same call count, same bounds semantics (section 5).

If any of those four is missing, the behavior is *implemented*, not *at parity*.
Say so explicitly in commits, docs, and papers.

The standard for ever claiming **exact whole-game parity** is written down in
`docs/PARITY_GAPS.md` ("Standard for Claiming Exact Parity", lines 266-277) and
is currently NOT met — see section 9.

---

## 2. The four audit scripts

House doctrine (user rule, all repos): any simulator behavior change requires
the full test suite green (5,276 tests as of 2026-07-24) **plus** these four
scripts, before the change counts as done. `sts2-change-control` owns the full
gate; the four commands themselves are:

```bat
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe scripts\audit_card_static_metadata.py
.venv\Scripts\python.exe scripts\audit_card_dynamic_vars.py
.venv\Scripts\python.exe scripts\audit_card_effect_vars.py
.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --show-missing
```

| Script | Checks | Pass/fail semantics |
| --- | --- | --- |
| `audit_card_static_metadata.py` | Every non-runtime CardId's cost/type/rarity/target/keywords/tags/upgrade metadata vs the parsed `decompiled/` base constructors, both unupgraded and upgraded | Prints each mismatch; **exit 1 on any mismatch**, else prints `card static metadata audit passed`, exit 0 (`scripts/audit_card_static_metadata.py:116-124`) |
| `audit_card_dynamic_vars.py` | DynamicVars/CanonicalVars numeric values (damage, block, magic numbers) incl. upgraded deltas, vs `decompiled/` | Same: exit 1 on mismatch, `card dynamic var audit passed` on success |
| `audit_card_effect_vars.py` | AST-scans registered card effects for `effect_vars` keys that the factory never outputs (typo/missing-key detector) | Same: exit 1 on mismatch, `card effect var audit passed` on success |
| `parity_reference_audit.py` (the coverage gate) | Every decompiled class filename across 8 surfaces has an implementation mention and a direct-test mention | **ALWAYS exits 0** — see warning below |

**Warning — the coverage gate's exit code is useless as a gate.** `main()` in
`scripts/parity_reference_audit.py:478-492` unconditionally returns 0. You must
read the printed table (every row must show 0 missing) or parse `--json`
output. A CI-style check must do something like:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --json | .venv\Scripts\python.exe -c "import json,sys; r=json.loads(sys.stdin.buffer.read().decode('utf-8-sig')); bad=[(s['surface'],s['missing_implementation'],s['missing_tests']) for s in r if s['missing_implementation'] or s['missing_tests']]; print(bad or 'coverage gate clean'); sys.exit(1 if bad else 0)"
```

(the `utf-8-sig` decode matters: Windows PowerShell 5.1 re-encodes piped text
with a UTF-8 BOM, which plain `json.load` rejects. Smoke-tested 2026-07-24:
prints `[('encounters', [], ['ToadpolesNormal'])]`, exit 1 — correct, because
that gap is real.)

Verified state as of 2026-07-24 (commit `fe25668`): the three card audits all
pass with exit 0. The coverage gate reports **one open regression**:
`encounters` has 1 missing direct test mention, `ToadpolesNormal`
(reproduce with `--surface encounters --show-missing`). The tables in
`docs/PARITY_COVERAGE_BACKLOG.md` claiming 0-missing across all surfaces are
stale on this point. Treat ToadpolesNormal as a lead to close next time
encounters or their tests are touched; do not silently re-baseline it.

Full current table (2026-07-24): cards 577, encounters 88, events 68,
modifiers 17, monsters 121, potions 64, powers 260, relics 290 — 0 missing
implementation everywhere; 0 missing tests everywhere except encounters (1).

`parity_reference_audit.py` CLI flags (parser at
`scripts/parity_reference_audit.py:427-475`): `--root PATH`,
`--surface {cards,encounters,events,modifiers,monsters,potions,powers,relics}`
(repeatable), `--include-deprecated`, `--direct-test-references`,
`--code-implementation-references`, `--show-missing`, `--json`. The doctrine
gate always uses the strict trio
`--direct-test-references --include-deprecated --code-implementation-references`.

---

## 3. Coverage-gate semantics: what a "mention" is and is not

The gate is a **lead-finding heuristic**. Understand exactly what it counts so
you neither game it nor over-trust it:

- For each `.cs` filename under the surface's reference dir (e.g.
  `decompiled/MegaCrit.Sts2.Core.Models.Powers/`), it generates aliases:
  the CamelCase name, its snake_case, its UPPER_SNAKE, plus suffix-stripped
  variants (`FooPower` also matches `Foo`) — `aliases_for()` at
  `scripts/parity_reference_audit.py:121-142`.
- **Implementation mention** (with `--code-implementation-references`): the
  alias appears as an identifier token anywhere in the surface's Python source
  tree (e.g. `sts2_env/powers/`). A string literal or comment counts too.
- **Direct test mention** (with `--direct-test-references`): the alias appears
  inside a `test_*` function / `Test*` class in `tests/` — in its identifiers,
  attribute names, string literals, or docstring — or in module-level
  assignments those tests reference (AST walk at
  `scripts/parity_reference_audit.py:186-275`). This is why the parity-test
  recipe (section 4) says to name the C# file in the docstring: `"Matches
  BorrowedTime.cs: ..."` is what satisfies the gate.

Consequences you must internalize:

- **A green gate proves nothing about behavior.** A test that merely *names*
  `WhirlwindPower` while asserting the wrong numbers passes the gate. The gate
  answers "which surfaces have zero targeted attention", not "which are
  correct". `docs/PARITY_COVERAGE_BACKLOG.md:27` says this in-repo.
- **Do not satisfy the gate with a bare name-drop.** If the gate flags a class,
  write a real behavior test per section 4. A docstring mention without
  assertions is gate-gaming and will be treated as such in review.
- **Doc tables rot; the script does not.** Both parity docs have been caught
  stale (`docs/PARITY_GAPS.md:8` admits its own predecessor rotted;
  the 0-missing table in `PARITY_COVERAGE_BACKLOG.md` misses the ToadpolesNormal
  regression). Always re-run the gate; never quote a doc table as current.
  Note also `PARITY_COVERAGE_BACKLOG.md` is append-heavy: only the top
  "Post-Agent Updates" section reflects (approximately) current state; the
  bottom Summary table (Cards 14.4% etc.) is a frozen 2026-03-17 baseline.

---

## 4. Parity-test recipe

The canonical template is `tests/test_necrobinder_parity.py:40-63`. Recipe:

1. **Read the C# first.** Find the class under
   `decompiled/MegaCrit.Sts2.Core.Models.<Surface>/<Name>.cs`, then check
   whether `decompiled_v0.109.0/` has a differing version (section 6). Derive
   the exact expected numbers by hand from the C#.
2. **Build a minimal seeded combat** — do not go through the RL env:

   ```python
   import sts2_env.powers  # noqa: F401  (registration side effect)

   from sts2_env.cards.factory import create_card
   from sts2_env.cards.necrobinder import create_necrobinder_starter_deck
   from sts2_env.core.combat import CombatState
   from sts2_env.core.enums import CardId, PowerId
   from sts2_env.core.rng import Rng
   from sts2_env.monsters.act1_weak import create_shrinker_beetle

   def _make_combat() -> CombatState:
       combat = CombatState(
           player_hp=70,
           player_max_hp=70,
           deck=create_necrobinder_starter_deck(),
           rng_seed=42,
           character_id="Necrobinder",
       )
       creature, ai = create_shrinker_beetle(Rng(42))
       combat.add_enemy(creature, ai)
       combat.start_combat()
       return combat
   ```

3. **Overwrite the scenario, execute one action, assert exact numbers**:

   ```python
   def test_borrowed_time_gains_energy_and_applies_borrowed_time_power(self):
       """Matches BorrowedTime.cs: gain energy, then apply BorrowedTimePower to the owner."""
       combat = _make_combat()
       combat.hand = [create_card(CardId.BORROWED_TIME)]
       combat.energy = 1

       assert combat.play_card(0)
       assert combat.energy == 4
       assert combat.player.get_power_amount(PowerId.BORROWED_TIME) == 1
   ```

4. **Cite the C# file in the docstring** (`Matches <Name>.cs: <one-line
   semantics>`). This is simultaneously documentation and what the coverage
   gate counts as a direct test mention.
5. If the effect pauses for a choice, drive it via
   `combat.resolve_pending_choice(index)` (confirm multi-choices with `None`)
   and assert the post-resolution state — never reach into resolver internals.

Checklist before calling the test done:

- [ ] Docstring names the `.cs` file.
- [ ] Assertions are exact integers/enums derived from the C#, not `>` / `!=`.
- [ ] Seed is pinned (42 by convention) and no wall-clock/`random` involved.
- [ ] `import sts2_env.powers` (and `sts2_env.potions` if potions are involved)
      is present — registration happens by import side effect. Inside
      `tests/`, `conftest.py` already forces power registration and resets the
      card instance counter per test; a standalone script gets neither
      (mechanics owned by **sts2-testing-and-qa**).
- [ ] The full suite and four audits still pass (gate owned by
      **sts2-change-control**).

Trap: in a standalone `CombatState` with no attached run state, ALL named RNG
streams silently collapse into the single combat-local `Rng(rng_seed)` — see
the fallback in `_run_rng` (`sts2_env/core/combat.py:463-468`). A unit test
built this way **cannot detect stream-routing bugs**. If your change touches
which stream a roll uses, you must also assert at the run level (construct a
`RunState`/`RunManager`, or assert stream `.counter` deltas on
`run_state.rng.<stream>` directly).

---

## 5. RNG porting rules

Background (one line; design rationale owned by **sts2-architecture-contract**):
there are two RNG roots — the combat-local `Rng(rng_seed)` used only when no
run is attached, and `RunRngSet` (`sts2_env/run/run_state.py:1383-1414`), which
derives every named stream as `Rng(deterministic_hash_code(str(master_seed)),
stream_name)`; the implementation `sts2_env/core/rng.py` is a bit-exact port of
.NET seeded `System.Random` plus the game's deterministic string hash, pinned
by `tests/test_rng_parity.py` down to exact output sequences and per-stream
derived seeds.

When porting any C# call site that consumes randomness, follow these five
rules. Dozens of past parity bugs (`docs/PARITY_GAPS.md:27-63`) are exactly
violations of them.

### Rule 1 — Route to the same named stream the C# uses

Named streams (from `RunRngSet.__init__`, run-seed based unless noted):

| Stream attr on `run_state.rng` | Stream name string | Convenience property on `CombatState` |
| --- | --- | --- |
| `up_front` | `up_front` | — |
| `shuffle` | `shuffle` | `combat.shuffle_rng` |
| `unknown_map_point` | `unknown_map_point` | — |
| `combat_card_generation` | `combat_card_generation` | `combat.combat_card_generation_rng` |
| `combat_potion_generation` | `combat_potion_generation` | `combat.combat_potion_generation_rng` |
| `combat_card_selection` | `combat_card_selection` | `combat.combat_card_selection_rng` |
| `combat_energy_costs` | `combat_energy_costs` | `combat.combat_energy_costs_rng` |
| `combat_targets` | `combat_targets` | `combat.combat_targets_rng` |
| `monster_ai` | `monster_ai` | `combat.monster_ai_rng` |
| `niche` | `niche` | — |
| `combat_orbs` | `combat_orbs` | `combat.combat_orbs_rng` |
| `treasure_room` | `treasure_room_relics` | — |
| `act_selection` | `act_selection` | — |
| `rewards` (player seed = run seed + 1) | `rewards` | — |
| `shops` (player seed) | `shops` | — |
| `transformations` (player seed) | `transformations` | — |
| per-act maps | `act_1_map`, `act_2_map`, ... via `get_map_rng(act_index)` | — |

Inside combat code, always go through the `CombatState` property (they resolve
lazily via `_run_rng`, `sts2_env/core/combat.py:463-500`), never grab
`run_state.rng.X` directly from a card effect. Events derive their own streams
(`create_event_rng`) — match whether the decompiled event uses its event RNG or
`base.Rng` (`docs/PARITY_GAPS.md:34`).

Picking a *plausible* stream instead of the *same* stream desyncs every
subsequent roll on both streams for the rest of the run, silently. The fix log
categories to learn from: card generation → `CombatCardGeneration`; card
selection from piles → `CombatCardSelection`; random enemy targets →
`CombatTargets`; random energy costs (Snecko) → `CombatEnergyCosts`; orbs →
`CombatOrbs`; relic "niche" selections (WarPaint/Whetstone family) → `Niche`
not `rewards` (`docs/PARITY_GAPS.md:38-48`).

### Rule 2 — Inclusive vs exclusive upper bound

`Rng.next_int(low, high)` is **inclusive-high** — a deliberate project-wide
convention (`sts2_env/core/rng.py:137-146`). C# `Random.Next(min, max)` is
exclusive-high. When porting a C# call site, use
`next_int_exclusive(low, high)` (`rng.py:148-152`), which delegates to
`next_int(low, high - 1)`. Mixing the conventions has caused real bugs (event
gold rolls had to be moved back to "original exclusive upper-bound RNG
semantics", `docs/PARITY_GAPS.md:33`). Also available and parity-pinned:
`next_bool()` (== `next_int(0,1) == 0`), `shuffle()` (C# Fisher-Yates,
pinned `[1,2,3,4,5] -> [3,2,5,1,4]` at seed 42 in
`tests/test_rng_parity.py:75-82`), `choice`, `sample`, `next_float`,
`next_float_range`, `next_gaussian_int` (rejection-sampling, not clamping),
`fork()`.

### Rule 3 — Stable-sort candidates BEFORE consuming the stream

When the C# selects from a pile, the candidate list order feeds the roll.
Python pile iteration order is not guaranteed to match C# collection order, so
the convention is: sort candidates deterministically, then shuffle/pick with
the stream. Use the engine helper:

```python
combat.stable_shuffle_cards(candidates, combat.shuffle_rng)
# sorts by (card_id.name, upgraded) then rng.shuffle -- combat.py:2633-2635
```

(worked example: Catastrophe, `sts2_env/cards/colorless.py:218-227`).
EXCEPTION: if the original deliberately shuffles an unstable list, match that —
`AggressionPower` uses an unstable `CombatCardSelection` shuffle *without*
pre-sorting (`docs/PARITY_GAPS.md:61`). Read the C# to know which case you are in.

### Rule 4 — Never consume RNG for instance IDs

Card clone/dupe paths allocate new instance ids from the monotonic counter in
`sts2_env/cards/base.py:522-546` (`new_card_instance_id_after(...)` /
`clone_card_for_deck`, e.g. `sts2_env/run/run_state.py:534-535`) — **never**
from any RNG stream. The original `CreateClone`/`CreateDupe` do not consume
RNG, and past dupes that did desynced everything after them
(`docs/PARITY_GAPS.md:98`).

### Rule 5 — Verify consumption count, not just values

Two implementations can return the same value while consuming a different
number of samples, which desyncs later rolls. `Rng.counter` exists for this:
assert counter deltas in tests when the port has any nontrivial control flow
(loops, rejection sampling, conditional rolls). `test_rng_parity.py` pins
counters as well as values — follow that pattern.

---

## 6. Two decompiled trees and the PATCHED allowlist procedure

### Tree precedence (as of 2026-07-24)

| Tree | What it is | Role |
| --- | --- | --- |
| `decompiled/` (219 top-level namespace dirs) | Pre-patch decompile | The **parsed reference** for the metadata audits (`reference_static_metadata.py:16` pins `decompiled/MegaCrit.Sts2.Core.Models.Cards`) and the default reading target for behavior ports |
| `decompiled_v0.109.0/` (222 top-level dirs) | Current-game beta v0.109.0 decompile | The **live-game truth**: bridge/Harmony signatures, and post-patch balance values. Wins on any conflict with `decompiled/` |
| `decompiled_mods/` | ActsFromThePast + Act4Heart (+ vendored libs) | Ground truth for mod content — content specifics owned by **sts2-game-and-mods-reference** |

So a card can be "wrong vs `decompiled/`" while being *correct vs the live
game*. The audit scripts reconcile this via allowlists; there is **no script
that wholesale-diffs the two trees**, so divergence beyond the allowlisted
cards is unaudited — when in doubt, open both files and diff by eye.

### Current allowlists (verified 2026-07-24)

- `scripts/audit_card_static_metadata.py:26-32` —
  `PATCHED_NECROBINDER_CARD_IDS = {BANSHEES_CRY, BORROWED_TIME, DIRGE, EIDOLON,
  SEANCE}` (5 ids; static metadata deviations).
- `scripts/audit_card_dynamic_vars.py:25-38` —
  `PATCHED_NECROBINDER_CARD_IDS = {BORROWED_TIME, DANSE_MACABRE, DEATH_MARCH,
  DEBILITATE_CARD, DEFY, GRAVE_WARDEN, HAUNT, REAVE, SCULPTING_STRIKE, SIC_EM,
  SOUL_STORM, THE_SCYTHE}` (12 ids; numeric-value deviations).
- Union: 16 distinct cards (BORROWED_TIME appears in both). If another doc
  quotes a different count, the scripts are the truth.
- Also: `RUNTIME_ONLY_CARD_IDS = {CardId.GENERIC}` is excluded from the static
  audit entirely.

### Procedure for a deliberate v0.109.0-matching deviation

1. Confirm the difference by reading BOTH
   `decompiled/MegaCrit.Sts2.Core.Models.Cards/<Name>.cs` and
   `decompiled_v0.109.0/MegaCrit.Sts2.Core.Models.Cards/<Name>.cs`.
2. Implement the `decompiled_v0.109.0/` behavior in the factory/effect.
3. Run the audits. Add the `CardId` to the allowlist **only in the script(s)
   that actually flag it** (static vs dynamic are independent), with a comment
   citing the `decompiled_v0.109.0` file — the allowlist header comment at
   `audit_card_static_metadata.py:22-25` is the template.
4. Add/extend the parity test citing the v0.109.0 file in its docstring.
5. Land through the normal gate (**sts2-change-control**). Allowlist edits are
   simulator-behavior-class changes: full suite + all four audits.

**The reverse trap is worse than the audit failure:** if an audit mismatch
appears on a card and you "fix" the Python back toward `decompiled/`, you may
be silently regressing live-game parity. On any card-metadata mismatch, check
`decompiled_v0.109.0/` FIRST, and check whether the card is already
allowlisted, before touching the implementation.

---

## 7. Hittable vs alive enemies

`CombatState` exposes two enemy views (`sts2_env/core/combat.py:439-461`):

- `alive_enemies` — every enemy with `is_alive`.
- `hittable_enemies` — alive enemies passing `can_hit_creature()`, i.e. no
  power vetoes via `should_allow_hitting` (untargetable states).

Rule: when the C# uses `HittableEnemies`, use `combat.hittable_enemies`; use
`alive_enemies` only when the original iterates all living enemies. Picking
wrong is a top-recurring historical bug class — a dozen relics
(BagOfMarbles, MercuryHourglass, CharonsAshes, ...) plus Dark/Glass orbs had to
be fixed to hittable-only (`docs/PARITY_GAPS.md:81-82`). Related: orb damage
carries the `UNPOWERED` `ValueProp` flag (unpowered = not scaled by
Strength-class attack powers), per the same fix batch.

---

## 8. Hook-order regression classes

The second big historical fix category after RNG routing. When touching the
damage pipeline, power lifecycle, or any `fire_*` dispatcher, re-check these
invariants — each is parity-audited and each was once a real bug
(`docs/PARITY_GAPS.md:80-98`; pipeline source `sts2_env/core/damage.py:115-263`
and `sts2_env/core/hooks.py`):

| # | Invariant | Where enforced |
| --- | --- | --- |
| 1 | `apply_damage` order: before-damage hooks (Thorns) → block absorb → `modify_hp_lost_before_osty` (HardenedShell caps BEFORE redirect) → `modify_unblocked_damage_target` (Osty redirect) → `modify_hp_lost_after_osty` (Intangible/TungstenRod/Buffer AFTER redirect) → HP loss | `damage.py:141-166` |
| 2 | Post-damage hook order: record damage events → `on_block_broken` → `after_current_hp_changed` → `after_damage_given` (per result, with `combat._active_damage_result` set) → `after_damage_received` → deferred `kill_creature` | `damage.py:204-261` |
| 3 | Killed targets do NOT receive `after_damage_received` | guard at `damage.py:243` |
| 4 | Overkill must not inflate the unblocked damage passed to hooks (`DamageResult.unblocked_damage` = actual `hp_lost`) | `damage.py:188-197` |
| 5 | Direct `kill_creature()` must emit the HP-change hook before death processing | `docs/PARITY_GAPS.md:91` |
| 6 | Multi-hit attack loops stop when the attacker dies mid-loop (FiendFire class) | `docs/PARITY_GAPS.md:80,83` |
| 7 | Result-sensitive powers (SicEm, Imbalanced, Slippery, VitalSpark, EmotionChip, Reflect) read blocked/fully-blocked/block-broken off the active `DamageResult` — never recompute | `damage.py:22-36,222-259` |
| 8 | Dispatch order per creature is Powers then Relics (C# `IterateHookListeners`, `decompiled/MegaCrit.Sts2.Core.Combat/CombatState.cs:264`); card hooks and run-modifier hooks dispatch separately | `hooks.py:40-67` |
| 9 | Turn-boundary auto-removal: a power at amount 0 is auto-removed only if it actually used a turn hook, with `allow_negative` respected (`amount == 0` vs `amount <= 0`) | `hooks.py:855-877, 926-944` |
| 10 | Legacy-vs-new turn hooks: `on_turn_end_enemy_side` / `on_turn_start_own_side` fire ONLY when the new-style hook (`after_turn_end` / `after_side_turn_start`) is not overridden. Overriding both double-ticks a duration power | same lines as #9 |

Practical implications when implementing effects:

- **Never mutate `current_hp` directly.** Route damage through
  `combat.deal_damage(...)` / `damage.apply_damage(...)` so block, Osty
  redirect, result metadata, and hook order all happen; hand-rolled damage
  breaks every result-sensitive power in row 7.
- **Never block or loop waiting for a player decision.** Mid-resolution
  choices go through `combat.request_card_choice` /
  `request_multi_card_choice` with a resolver closure
  (`combat.py:2489-2555`); the engine's `resolve_pending_choice`
  (`combat.py:2557-2593`) resumes stashed continuations. Design rationale and
  the staged turn-setup machine are owned by **sts2-architecture-contract**.
- When you fix a hook-order bug, add the invariant to a parity test citing the
  C# — that is how every row in the table above became guarded.

---

## 9. What parity is NOT (claim discipline)

Nothing in this repo licenses the sentence "the simulator exactly matches the
game." `docs/PARITY_GAPS.md` (rewritten 2026-05-18, still the doc of record) is
explicit; its "Standard for Claiming Exact Parity" requires, before any exact-
match claim: remaining blockers closed, every divergence implemented-or-proven-
unreachable, a compiled bridge smoke-tested against a live game across a full
run, full-run replay comparison, and no-op markers limited to base-class
defaults. As of 2026-07-24 none of the live-game legs are met:

- The C# bridge mod is implemented and Python-tested, built and deployed
  2026-07-23, but has **never been live-smoke-tested** against the running
  game (`docs/PARITY_GAPS.md:229-254`; open gaps inventoried in
  `docs/KNOWN_ISSUES.md` issue 16). Everything real-game is owned by
  **sts2-bridge-and-realgame**. Note the 2026-05-22 claim in PARITY_GAPS.md
  that dotnet was absent is itself stale — the mod has since been built; the
  *live smoke test* is what remains undone.
- Full-run bridge replay comparison does not exist yet
  (`docs/BRIDGE_REPLAY_HARNESS.md`).
- Coverage breadth and random-call boundary audits remain open blockers
  (`docs/PARITY_GAPS.md:211-223`).

Approved phrasings, strongest first:

1. "Bit-exact RNG parity with the decompiled seeded `System.Random` and named
   stream derivation, pinned by `tests/test_rng_parity.py`." (provable)
2. "Card static/dynamic metadata machine-audited against the decompiled
   pre-patch reference, with 16 allowlisted deliberate v0.109.0 deviations."
3. "Behavior X is parity-tested against `<Name>.cs`." (per-test)
4. "All 8 decompiled surfaces have implementation and direct-test coverage per
   the name-mention gate" — only after re-running the gate, and currently
   FALSE for encounters (ToadpolesNormal).
5. Never: "exact parity", "faithful replica", "matches the real game" —
   without the PARITY_GAPS standard being met and cited.

For a paper (highest project goal is publishable results), the honest claim
today is "decompiled-source-verified simulator with machine-audited metadata
parity, bit-exact RNG, and per-behavior parity tests; live-game validation via
the bridge is implemented but not yet field-verified." Keep it that way until
**sts2-bridge-and-realgame**'s smoke checklist is executed.

Historical note (details owned by **sts2-failure-archaeology**): the Harmony
`SetTimeScale` incident (`docs/KNOWN_ISSUES.md` issue 6) established the rule
that on every game update you must re-verify decompiled **parameter names**,
not just method names, because Harmony binds prefix params by name.

---

## 10. Provenance and maintenance

Every fact below can drift. Date stamps refer to verification on **2026-07-24**
at commit `fe25668` (working tree had unrelated uncommitted `sts2_env/content/`
+ web-play changes; a concurrent session may be advancing the training revamp).
Re-verify before relying on any of these:

| Fact (as of 2026-07-24) | Re-verify with |
| --- | --- |
| HEAD `fe25668`, campaign revamp phase committed | `git -C C:\Users\motqu\GitHub\sts2-rl-agent log --oneline -3` |
| 5,276 tests collected | `.venv\Scripts\python.exe -m pytest tests/ --collect-only -q` |
| Three card audits pass, exit 0 | run the three `audit_card_*.py` commands in section 2 |
| Coverage gate: all surfaces 0-missing except encounters 1 missing test (`ToadpolesNormal`) | `.venv\Scripts\python.exe scripts\parity_reference_audit.py --direct-test-references --include-deprecated --code-implementation-references --show-missing` |
| Coverage gate always exits 0 (must read output) | `Get-Content scripts\parity_reference_audit.py \| Select-String -Pattern "return 0"` (main at lines 478-492) |
| Static allowlist = 5 ids; dynamic allowlist = 12 ids | `findstr /n "PATCHED_NECROBINDER" scripts\audit_card_static_metadata.py scripts\audit_card_dynamic_vars.py` then read the frozensets |
| Reference parse dir is `decompiled/` (pre-patch) | `findstr /n "REFERENCE_CARD_DIR" sts2_env\cards\reference_static_metadata.py` (line 16) |
| RNG streams and seed math (`next_int` inclusive; stream seeds; Fisher-Yates order) | `.venv\Scripts\python.exe -m pytest tests\test_rng_parity.py -q` (spot-run 2026-07-24: green; 87 passed together with `test_necrobinder_parity.py` + `test_combat_parity.py` in 0.49s) |
| `_run_rng` fallback to combat-local rng (stream bugs invisible in standalone tests) | read `sts2_env/core/combat.py:463-468` |
| `stable_shuffle_cards` sorts by `(card_id.name, upgraded)` | read `sts2_env/core/combat.py:2633-2635` |
| Damage pipeline hook order and killed-target guard | read `sts2_env/core/damage.py:115-263` |
| `Hook.cs` citations (`ModifyDamageInternal` line 1902, `ModifyBlock` line 960) | `findstr /n "ModifyDamageInternal ModifyBlock" decompiled\MegaCrit.Sts2.Core.Hooks\Hook.cs` |
| C# dispatch order Powers→Relics→Potions→Orbs→AllCards | read `decompiled/MegaCrit.Sts2.Core.Combat/CombatState.cs:264-310` |
| Bridge built/deployed but never live-smoke-tested | `docs/PARITY_GAPS.md` sections 3 and Standard; `docs/KNOWN_ISSUES.md` issue 16; current status owned by **sts2-bridge-and-realgame** |
| Decompiled tree sizes (219 / 222 top-level dirs) | `(Get-ChildItem decompiled -Directory).Count; (Get-ChildItem decompiled_v0.109.0 -Directory).Count` |

Maintenance rules for this skill file itself:

- If a game patch lands (new decompile), sections 2, 6, and 9 must be re-run
  end-to-end before trusting them; the allowlists and the
  `decompiled_v0.109.0/` name will both change.
- If the ToadpolesNormal gap is closed, update section 2's "verified state"
  paragraph — and if a new gap appears, name it the same way rather than
  restoring a clean-table claim.
- Never let this file quote a coverage table without a date stamp; the in-repo
  precedent is that undated parity tables rot within weeks.
