---
name: sts2-architecture-contract
description: >
  Load-bearing design decisions of the sts2-rl-agent simulator and WHY they
  exist: the CombatState god object and pending_choice resumable-pause model,
  staged turn setup, hook dispatch order, damage/block pipelines and
  result-sensitive hooks, the two-RNG-root + named-streams contract,
  act-slot/pool_key routing with lazy registration, registries populated by
  import side effect, observation/action layout invariants (embedding-ID block
  first; combat Discrete(115) as a prefix of run Discrete(157)), and bridge
  wire-format invariants. Load this BEFORE writing or reviewing any change to
  sts2_env/core, sts2_env/run, sts2_env/map, card/power/relic/event content,
  gym observation or action layouts, or bridge adapters — it tells you which
  invariants you must not break and how to extend each subsystem the intended
  way. Do NOT load this for: how to run audits or write parity tests
  (sts2-parity-discipline), diagnosing a failure you're seeing
  (sts2-debugging-playbook), game/mod content facts like card values or
  ascension effects (sts2-game-and-mods-reference), training runs and
  hyperparameters (sts2-training-campaign / sts2-run-and-operate), bridge
  build/deploy/ops (sts2-bridge-and-realgame), or landing a change
  (sts2-change-control).
---

# STS2 Simulator Architecture Contract

This skill is the "why it is built this way" document for the from-scratch
Python simulator of Slay the Spire 2 (beta v0.109.0) in this repo. Every
section states a design decision, the invariants it imposes, the file:line
ground truth, and the intended extension path. Violating any invariant here
has historically produced silent desyncs, invisible-in-unit-test bugs, or
broken training runs — treat these as contracts, not suggestions.

All facts verified 2026-07-24 at HEAD `fe25668` ("Phase 0 training revamp").
Re-verification commands are in the final section.

Jargon used throughout (defined once):

| Term | Meaning |
|---|---|
| parity | Behavioral equality with the decompiled C# game source (`decompiled_v0.109.0/` preferred, `decompiled/` is the older pre-patch tree). See sts2-parity-discipline. |
| pending_choice | The simulator's pause token: a `PendingCardChoice` object that freezes combat (or run) resolution mid-effect until the agent/player picks an option. |
| pool_key | String on an `ActConfig` (e.g. `"act1"`, `"thecity"`) that selects which `sts2_env/encounters/<pool_key>.py` module supplies that act's fights. |
| Osty | The Necrobinder's skeleton pet. In this sim (and the real game) it is an ally `Creature` with `is_osty=True`, NOT a player creature; it acts only through the owner's cards/relics. |
| Souls | Necrobinder resource cards (`SOUL` CardId) counted per zone in the rich observation. |
| AFTP | "Acts from the Past" mod: adds legacy STS1 acts (Exordium/TheCity/TheBeyond) as act-slot alternates. Active in the campaign config. |
| Act4Heart | Mod that appends a fixed 4th act ("TheEnding", Corrupt Heart boss). Always on in this sim. |
| god object | A single class that owns nearly all state for its domain — a deliberate choice here, see Contract 1. |

## 0. The layer cake (orientation)

```
sts2_env/core/      combat engine: CombatState, hooks, damage, rng, enums
sts2_env/cards|powers|relics|potions|monsters|encounters|events/   content, registered into core
sts2_env/map/       acts, map generation
sts2_env/run/       RunState + RunManager (phase machine over combats)
sts2_env/gym_env/   Gymnasium envs + observation/action encodings
sts2_env/train/     policy net, weight transfer
sts2_env/bridge/    TCP adapters to the real game (C# mod in bridge_mod/)
```

Dependencies point downward only: core knows nothing about gym or bridge.
Two deliberate exceptions where core is mod-aware: `constants.py:45-51`
(AFTP shared-event pool flags) and `combat.py:856-863` (a zero-monster
encounter — Act4Heart's `EmptyFightAct4Weak` — resolves as an instant win).

---

## Contract 1: CombatState god object + resumable-pause model

### The decision

`CombatState` (`sts2_env/core/combat.py:115`, ~3,930-line file) owns *all*
combat state: per-player piles, enemies + their `MonsterAI` map, allies/Osty,
the attack-context stack, and per-turn/per-combat event ledgers (damage,
draw, card-play, power events) that result-sensitive powers read. Constructor
signature at `combat.py:118`:
`CombatState(player_hp, player_max_hp, deck, rng_seed, relics=None, gold=0,
character_id=None, potions=None, max_potion_slots=3, player_state=None,
ally_players=None, room=None, ascension_level=0)`.

Why a god object: the decompiled C# `CombatState` is one, and matching its
shape 1:1 keeps parity porting mechanical (find the C# member, find the same
member here). Do not refactor it into services — you would have to re-derive
hook ordering and event-ledger semantics from scratch.

### The pause model (the single most important invariant)

The engine must be able to stop mid-effect and wait for an RL action, then
resume exactly where it stopped — combat is a resumable coroutine built from
closures, not an actual coroutine:

- Any effect that needs a player decision calls
  `combat.request_card_choice(...)` (`combat.py:2489`) or
  `request_multi_card_choice(...)` (`combat.py:2520`), passing a `resolver`
  closure. These set `combat.pending_choice`. Single-option non-skippable
  choices auto-resolve inline without pausing (`combat.py:2504-2507`,
  `2539-2542`).
- Engine code that fires hooks checks `self.pending_choice` after every hook
  batch and, if set, stashes a continuation lambda in one of three resume
  slots: `_pending_draw`, `_pending_play`, `_pending_turn_setup`.
- `resolve_pending_choice(choice_index)` (`combat.py:2557`) runs the
  resolver, then resumes in fixed order: pending draw → pending play →
  pending turn setup → `_check_combat_end()`.

Rules when writing any effect or hook:

1. NEVER block, loop-until-input, or recurse to get a decision. Request a
   choice with a resolver closure and return.
2. After calling anything that can fire hooks (deal damage, draw, apply a
   power), check `combat.pending_choice is not None` and `combat.is_over`
   before continuing; stash a continuation if you must do more work after.
3. Resolvers run inside `acting_player_view(owner)` — do not re-wrap.

### Staged turn setup

Turn start is a string-staged state machine so it can suspend at any point:
`start_combat` (`combat.py:826`) → `_start_player_turn` (`combat.py:869`) →
`_continue_player_turn_setup(player_index, stage)` (`combat.py:886`) with
stages, in order:

`"block"` → `"before_hand_draw"` → `"card_before_hand_draw"` →
`"before_hand_draw_late"` → `"draw"` → `"after_player_turn_start"` →
`"post_after_player_turn_start"`

Every stage checks `pending_choice` and stores
`self._pending_turn_setup = lambda idx=...: self._continue_player_turn_setup(idx, "<next stage>")`
before returning. If you add a turn-start hook site, add it as a new named
stage in this chain — do not fire it outside the machine, or a pending choice
raised inside it will lose the rest of turn setup.

Turn end mirrors this: `end_player_turn` (`combat.py:1556`) fires
before-turn-end hooks, orb before-turn-end, `_resolve_end_of_turn_hand`
(ethereal exhaust → turn-end-in-hand effects → retain → flush,
`combat.py:1602`), the extra-turn check (`combat.py:1588-1593`), then
`_execute_enemy_turn` (`combat.py:1691`). Enemy moves run through
`_continue_enemy_moves` (`combat.py:1717`), which likewise suspends on
`pending_choice` per move; `_finish_enemy_turn` (`combat.py:1743`) re-rolls
every surviving enemy's next move via `ai.roll_move(self.monster_ai_rng)`
(`combat.py:1749-1751`) before looping back to the player.

### The run layer has the same pattern

`RunState.pending_choice` interrupts reward application, shops, boss relics,
rest sites, and events. Any run-layer code that can trigger a deck choice
must stash a resume callback on `RunManager._resume_after_run_choice` (or
`_resume_after_reward_chain` for reward-queue continuations) instead of
transitioning phases directly — see the pattern at `run_manager.py:514`,
`:735`, `:1598`, `:1647`. Forgetting this silently drops the post-choice
transition (a recurring historical bug class; see sts2-debugging-playbook).

### How the RL env sees a pause

`gym_env/action_space.py:70-79`: when `combat.pending_choice` is set, the
action mask reuses combat action indices — index 0 = confirm/skip (only if
`can_confirm()`/`allow_skip`), indices `1..N` = choice options. Action ids
are therefore phase-dependent; never interpret an action id without its mask
context.

---

## Contract 2: Hook dispatch order

### The decision

All triggered behavior flows through module-level dispatch functions in
`sts2_env/core/hooks.py` (`fire_*`, `modify_*`, `should_*`), a direct port
of C# `CombatState.IterateHookListeners`. Dispatch order per C#:
**Powers → Relics → Potions → Orbs → AllCards → Modifiers**
(`hooks.py:1-10`). Only powers and relics are fully generic listeners here:

| Listener class | How dispatched | Where |
|---|---|---|
| Powers | `_iter_power_listeners`: every creature's powers, in creature order then power-dict insertion order | `hooks.py:40-44` |
| Relics | `_iter_relic_listeners`: per combat-player-state, after all powers | `hooks.py:47-60` |
| Cards | Targeted dispatch via `fire_card_*` helpers in `sts2_env/cards/registry.py` over `combat.unique_cards_in_piles(...)`, called from specific engine sites | registry.py (13 `fire_card_*` helpers) |
| Run modifiers | `_iter_modifier_listeners`: `run_state.modifiers` (e.g. `Act4HeartModifier`) | `hooks.py:63-67` |

Note the `hooks.py` module docstring understates this ("supports ... powers
and relics") — cards and run modifiers ARE dispatched, just not through the
generic iterator. Trust the code, not that sentence.

### Invariants

1. Never iterate `creature.powers` yourself to trigger behavior — add or use
   a `hooks.py` dispatch function so ordering stays Powers→Relics.
2. Hook-order regressions are a top historical bug class (killed targets
   receiving `after_damage_received`, overkill inflating hook damage, etc. —
   see sts2-failure-archaeology). Any change to firing order needs a parity
   test citing the decompiled C# order (see sts2-parity-discipline).
3. The "only listeners that actually changed the value" pattern:
   `modify_block` (`hooks.py:335-389`) tracks `id()` of every listener whose
   delta ≠ 0 / multiplier ≠ 1.0 and calls `after_modifying_block_amount` only
   on those. The same id-tracked pattern recurs for card-play-count and
   power-amount hooks. Preserve it when adding modify-style hooks — C#
   callbacks fire only for actual modifiers.
4. Turn-tick auto-removal: `fire_after_turn_end` / `fire_after_side_turn_start`
   auto-remove powers whose amount hits 0 only if the power actually used a
   turn hook, guarded by `allow_negative`. A power overriding both the
   new-style and legacy turn hooks double-ticks — override exactly one.

### Extension recipes

New power: subclass `PowerInstance` (`sts2_env/powers/base.py:20`), override
only the hooks you need (all are no-op stubs), set the class attrs
`power_type` / `stack_type` / `allow_negative` / `is_temporary` /
`should_scale_in_multiplayer` (`base.py:27-32`), then
`register_power_class(PowerId.X, cls)` (`sts2_env/core/creature.py:18`) in a
module imported by `sts2_env/powers/__init__.py` (see Contract 6).

New card behavior: write a `make_*` factory in the character module
(auto-discovered by `sts2_env/cards/factory.py` from the 8 modules listed at
`factory.py:47-56`) and attach effects with the `@register_effect(CardId.X)`
decorator family in `cards/registry.py` (~27 registries, `registry.py:107-133`).

Power application order (`creature.py:168` `apply_power`):
`modify_power_amount_given` (giver relics + multiplayer scaling) →
`after_modifying_power_amount_given` → `modify_power_amount_received`
(target relics) → `after_modifying_power_amount_received` → Artifact debuff
block → stack/create → `combat.after_power_amount_changed`. Apply powers
through `apply_power`/`apply_power_to`, never by mutating `creature.powers`.

---

## Contract 3: Damage and block pipelines + result-sensitive hooks

### The decision

Damage math is split across two files on purpose, mirroring the C# split:

**Amount calculation** — `hooks.modify_damage` (`hooks.py:162`, "matching
Hook.ModifyDamageInternal (Hook.cs:1902)"):
enchant additive → power additive → relic additive → power multiplicative →
relic multiplicative → enchant multiplicative → cap → `max(0, floor)`.
Constants: `VULNERABLE_MULTIPLIER=1.5`, `WEAK_MULTIPLIER=0.75`,
`FRAIL_MULTIPLIER=0.75` (`constants.py:10-12`).

**Application** — `damage.apply_damage` (`sts2_env/core/damage.py:115`):
before-damage hooks (Thorns) → block absorption →
`modify_hp_lost_before_osty` → `modify_unblocked_damage_target` (Osty
redirect) → `modify_hp_lost_after_osty` (Intangible/TungstenRod/Buffer,
deliberately late) → HP loss. Then, in parity-audited order
(`damage.py:204-261`): record damage events → `on_block_broken` →
`after_current_hp_changed` → `after_damage_given` (per result, with
`combat._active_damage_result` set around each call) →
`after_damage_received` (SKIPPED for killed targets) → `kill_creature`.

**Block** — `hooks.modify_block` (`hooks.py:335`, "Hook.ModifyBlock
(Hook.cs:960)"): enchant → additive (Dexterity) → multiplicative (Frail) →
multiplayer scaling → floor, then the id-tracked
`after_modifying_block_amount` callbacks (Contract 2, rule 3).

### Invariants

1. NEVER mutate `creature.current_hp` directly to deal damage. Go through
   `combat.deal_damage` / `apply_damage`. Hand-rolled damage skips block,
   the Osty redirect, result metadata, and hook ordering — and breaks every
   result-sensitive power (SicEm, Imbalanced, Buffer, TungstenRod,
   ReaperForm, HandDrill).
2. `DamageResult` (`damage.py:21-36`) carries `blocked / hp_lost /
   was_killed / unblocked_damage / overkill_damage / was_block_broken /
   was_fully_blocked`. Result-sensitive hooks read the active result via
   `combat._active_damage_result` — if you fire `after_damage_given/received`
   manually you must reproduce the sentinel save/restore at
   `damage.py:222-259`. (Better: don't fire them manually.)
3. The before/after-Osty split exists so HardenedShell-class caps run BEFORE
   the redirect and Intangible/TungstenRod/Buffer run AFTER, on the actual
   damage taker. New HP-loss modifiers must pick the correct side.
4. Healing goes through `PlayerState.heal` / `creature.heal`, which
   duck-types a `modify_heal_amount` relic hook (added for AFTP's
   MarkOfTheBloom). Direct `current_hp += n` healing bypasses mod hooks.
5. Targeting: `combat.hittable_enemies` (`combat.py:452`) vs
   `combat.alive_enemies` (`combat.py:440`). AoE relic/orb effects use
   hittable (the C# `HittableEnemies`); picking the wrong one is a top-5
   recurring bug class.
6. `ValueProp` (`core/enums.py`) IntFlag tags every value with its origin
   (`UNBLOCKABLE`, `UNPOWERED`, `MOVE`, ...); `is_powered_attack()` = MOVE
   and not UNPOWERED. Orb damage is UNPOWERED (parity fix). Pass props
   through unchanged; hooks filter on them.

---

## Contract 4: Two RNG roots + named-streams contract

### The decision

Bit-exact RNG parity with the game is achieved by construction, not by
testing alone:

- `sts2_env/core/rng.py` reimplements .NET's seeded `System.Random`
  (subtractive generator, 56-element seed array, `rng.py:39-98`) plus
  `deterministic_hash_code` = C# `StringHelper.GetDeterministicHashCode`
  (`rng.py:25-36`). `Rng(seed, name)` offsets the seed by the hash of the
  stream name (`rng.py:109-115`).
- **Convention trap**: `Rng.next_int(low, high)` is INCLUSIVE-high (project
  convention, `rng.py:137`); `next_int_exclusive` matches C#
  `Random.Next(min, max)` (`rng.py:148`). When porting a C# call site, use
  `next_int_exclusive`. Mixing these caused real desyncs (event gold rolls).

There are exactly TWO RNG roots in a run — do not mix them:

| Root | Seeded | Used for |
|---|---|---|
| `RunState.rng` = `RunRngSet(seed)` (`run_state.py:1383`) | `deterministic_hash_code(str(master_seed))`; player streams use seed+1 | Named deterministic streams: `up_front`, `shuffle`, `unknown_map_point`, `combat_card_generation`, `combat_potion_generation`, `combat_card_selection`, `combat_energy_costs`, `combat_targets`, `monster_ai`, `niche`, `combat_orbs`, `treasure_room` ("treasure_room_relics"), `act_selection`; player-seed `rewards`/`shops`/`transformations`; per-act maps via `get_map_rng(i)` = `Rng(seed, f"act_{i+1}_map")` |
| `RunManager._rng` = `Rng(seed + 9999)` (`run_manager.py:132,266`) | seed + `RUN_MANAGER_RNG_SEED_OFFSET` | Encounter picks, combat seeds, boss rolls, boss-relic shuffle |

`CombatState` resolves streams lazily via `_run_rng(stream_name)`
(`combat.py:463-468`) with named properties (`shuffle_rng`,
`combat_targets_rng`, `monster_ai_rng`, ... `combat.py:470-500`), falling
back to the combat-local `self.rng` (`Rng(rng_seed)`) when no `RunState` is
attached.

### Invariants and porting rules

1. **The fallback is a test blind spot**: standalone-`CombatState` unit
   tests merge every stream into one `combat.rng`, so stream-routing bugs
   are invisible there. Only full-run/seeded tests catch them. When touching
   stream routing, add a run-attached test (see sts2-testing-and-qa).
2. When porting a C# random call: (a) find which named stream the original
   uses; (b) consume it via the `CombatState.<stream>_rng` property or
   `run_state.rng.<stream>`; (c) inclusive-vs-exclusive per rule above;
   (d) stable-sort candidate lists BEFORE consuming the stream unless the
   original explicitly used an unstable shuffle; (e) never consume RNG to
   allocate clone/dupe instance ids.
3. New random subsystems mint a NEW named stream (`Rng(seed, "my_stream")`)
   — e.g. legacy event pools use `Rng(seed, f"legacy_event_pool_{i}")` —
   so existing runs stay reproducible. Never piggyback on an existing
   stream: you would shift every later roll in it.
4. Gym env / trainer code must never touch these roots; env-level
   stochasticity (encounter sampling etc.) uses its own numpy/py RNG.

Stale comment warning: `run_state.py:1403-1405` claims `act_selection` is "a
no-op today" — false since AFTP acts were wired (slots have 3/2/2
candidates); the stream IS consumed every run.

---

## Contract 5: Act-slot / pool_key routing + lazy registration

### The decision

A run's acts are built as
`[select_act_for_slot(slot, rng.act_selection) for slot in range(NUM_ACT_SLOTS)] + [ACT_3.to_mutable()]`
(`run_state.py:1440-1443`, `NUM_ACT_SLOTS=3` at `map/acts.py:224`).

- Slot candidates (uniform `rng.choice` when >1, `acts.py:251-264`):
  slot 0 = Overgrowth | Underdocks | Exordium; slot 1 = Hive | TheCity;
  slot 2 = Glory | TheBeyond.
- `ACT_3` ("TheEnding", `acts.py:172-192`) — the Act4Heart ending act,
  Corrupt Heart boss — is ALWAYS appended fixed, never randomized.
- `Act4HeartModifier` is unconditionally in `RunState.modifiers` from
  construction (`run_state.py:1467-1469`; class at
  `run/modifiers.py:599`). It deliberately skips the KeyDoor gate — the run
  always proceeds Act 3 → Act 4 (`modifiers.py:609-613`).
- "Final boss" semantics use `RunState.final_boss_act_index`
  (`run_state.py:1494-1506`): returns 2 whenever there are ≥4 acts,
  mirroring the mod's `FixAct3Boss_IL` patch. Use that property, never
  `len(acts) - 1`, when gating end-of-run/boss rewards.

Encounter routing: `RunManager._get_encounter_pools(pool_key)`
(`run_manager.py:161-211`) imports `encounters/<pool_key>.py` and returns its
`WEAK_ENCOUNTERS / NORMAL_ENCOUNTERS / ELITE_ENCOUNTERS / BOSS_ENCOUNTERS`
lists. These are lists of **setup functions** `setup_fn(combat, rng)` — the
`weak_encounter_ids` / `strong_encounter_ids` / `elite_ids` / `boss_ids`
lists on `ActConfig` are documentation only; editing them changes nothing
about which fights occur. Naming trap: `pool_key "act4"` = vanilla
Underdocks (legacy module naming); the real 4th act is `"act4_heart"`
(`run_manager.py:181-205`).

### Lazy registration and the import cycle (the WHY)

Legacy-act `ActConfig`s are built lazily in `_ensure_alternate_acts_registered`
(`acts.py:354-376`), on first registry access, NOT at module import.
Rationale (`acts.py:358-364`): building them imports the events package, and
`map.acts → events.* → run → map/__init__ → map.acts` forms an import cycle
whenever `sts2_env.events` is imported before `map.acts` finishes (the web
`play_run` entry point does exactly that). This broke web/CLI play and was
fixed in commit `b3e97b1`. The flag is set BEFORE building as a re-entrancy
guard (`acts.py:369`).

### Recipe: add a new act variant

1. Create an `ActConfig` with a unique `pool_key` and `act_id` in
   `map/acts.py`.
2. Create `sts2_env/encounters/<pool_key>.py` exposing the four
   setup-function lists.
3. Add an `elif` branch in `run_manager._get_encounter_pools`.
4. Register via `register_act_candidate(slot, cfg)` (`acts.py:233`) INSIDE
   `_ensure_alternate_acts_registered` — never at module import (cycle).
5. If the act is a legacy act (`is_legacy=True`), its event order is rebuilt
   at act entry by `build_legacy_event_pool` (`run/events.py`) with its own
   `legacy_event_pool_{i}` stream; vanilla acts just get
   `rng.up_front.shuffle(event_ids)`.

The rich observation's boss vocabulary and act-candidate one-hots read the
candidate registry dynamically (`rich_observation.py:96-135`), so a new act
joins the obs automatically — but it changes `BOSS_NAME_TO_IDX` indices,
which invalidates trained models (see Contract 7, invariant 5).

---

## Contract 6: Registries populated by import side effect

### The decision

Nothing self-registers lazily at use time; content registers when its module
is imported, and package `__init__.py` files do the importing:

| Content | Registration | Imported by |
|---|---|---|
| Powers | `register_power_class(PowerId, cls)` into `_POWER_CLASSES` (`creature.py:15-24`) | `sts2_env/powers/__init__.py` (12 module imports) |
| Card effects | `@register_effect(CardId)`-family decorators into ~27 dicts (`cards/registry.py:107-133`) | `sts2_env/cards/__init__.py` (8 module imports) |
| Card factories | `cards/factory.py` introspects the 8 card modules for `make_*` functions (`factory.py:47-56`) | first factory use (lru-cached) |
| Events | `EventModel` instantiation at module scope | `sts2_env/events/__init__.py` (8 module imports incl. `aftp_shared`) |
| Relics | `RELIC_REGISTRY` in `relics/registry.py` | `sts2_env/relics/__init__.py` |
| Act candidates | lazy, on first access (Contract 5 — the one exception) | — |

### Invariants

1. Adding a module without adding its import to the package `__init__.py`
   means the content silently does not exist: powers fall back to a generic
   instance, card effects no-op, events never appear. There is no runtime
   error. Structural tests (all-cards coverage, the
   description-coverage test) are what catch this — see sts2-testing-and-qa.
2. Never import `sts2_env.events` (or anything that transitively pulls it)
   at module scope inside `sts2_env/map/` — that recreates the b3e97b1 cycle.
   A second, adjacent cycle (verified live 2026-07-24): `sts2_env.core.combat`
   cannot be imported COLD. `combat.py:24` does
   `from sts2_env.cards.base import ...`, which runs `cards/__init__.py`,
   which imports `ironclad_basic`, which needs `CombatState` from the
   still-partially-initialized combat module → `ImportError`. It only works
   when `sts2_env.cards` began initializing FIRST (then `cards.base` is
   already in `sys.modules` when combat asks for it). Tests survive because
   `tests/conftest.py` from-imports `sts2_env.cards.base` before combat; the
   gym/run entry points establish the same order transitively. Rule for any
   cold script, REPL one-liner, or new entry point: `import sts2_env.cards`
   before touching `sts2_env.core.combat`. (`import sts2_env.powers` alone
   is NOT sufficient — verified.)
3. Import order matters for test isolation: tests rely on the conftest
   importing packages once. If you add a registry, make registration
   idempotent (all current ones are: dict assignment by id).
4. Card/power tooltip text is derived data in
   `sts2_env/content/descriptions.py` (from sim data + `docs/CARDS_REFERENCE.md`);
   a test enforces total `PowerId` coverage, so adding a `PowerId` without
   description coverage fails the suite.

---

## Contract 7: Observation / action layout invariants

### The four spaces (all verified by import, 2026-07-24)

| Space | Size | Where defined |
|---|---|---|
| Legacy combat obs | 131 | `gym_env/observation.py` (`OBS_SIZE`) |
| Legacy run obs | 151 = 131 + 20 | `gym_env/run_env.py:206` (`RUN_OBS_SIZE`) |
| Rich obs (current, combat AND run) | **4184** | `gym_env/rich_observation.py:228` (`RICH_OBS_SIZE`) |
| Combat actions | Discrete(**115**) | `core/constants.py:33-43` (`ACTION_SPACE_SIZE`) |
| Run actions | Discrete(**157**) | `gym_env/run_env.py:110-160` (`TOTAL_ACTIONS`) |

Combat action layout (`constants.py:33-43`): 0 = end turn / confirm choice;
1-10 = untargeted card by hand slot; 11-60 = card x enemy (10 slots x 5
enemies); 61-114 = potions (9 slots x 6 target options,
`POTION_ACTION_START=61`). During a pending choice, indices 1..N are reused
as choice options (Contract 1).

Run action layout (`run_env.py:110-152`, built additively): combat 0-114,
map 115-119, card-reward 120-123 (+extra 124-126), boss-relic 127-129, shop
130-139, rest 140-144, event 145-148, treasure/reroll 149, player-select
150-156.

### Invariant 1: combat Discrete(115) is an index-prefix of run Discrete(157)

`_build_action_layout` starts the combat block at 0 by construction. This is
what makes curriculum weight transfer work:
`sts2_env/train/policy.py:134-164` `transfer_weights` copies every
name+shape-matching tensor and prefix-copies `action_net` rows
`[:115]` into the 157-row head. If you insert, remove, or reorder ANY combat
action, every trained combat checkpoint stops being transferable and every
saved model's action semantics silently shift. Extend action spaces only by
APPENDING, and treat existing indices as frozen ABI.

### Invariant 2: the embedding-ID block comes first

The rich obs (one flat float32 vector shared by `RichSTS2CombatEnv` and
`RichSTS2RunEnv` precisely so weights transfer between curriculum stages;
combat env zeroes the run segment, run env zeroes combat segments while out
of combat — `rich_observation.py:1-15`) puts all integer embedding IDs in a
contiguous 16-dim block at offset 0: 10 hand-card ids, 5 potion ids, 1 boss
id (`rich_observation.py:144-153`). Rationale: the SB3 features extractor
(`train/policy.py`) slices `obs[:, :16]`, embeds those ids (shared 64-dim
card table also projected over the pile bag-of-cards vectors via
`bags @ embedding.weight[1:]`, `policy.py:100`), and passes everything from
`PILE_SIZES_OFF` onward straight through. Anything you add to the obs must
go AFTER the ID block, and new categorical ids must extend the ID block
only by appending (which still breaks saved extractors — see invariant 5).

Segment order (offsets are computed constants — never hardcode numbers):
ID block 16 → hand scalars 10x12 → pile bags 3xNUM_CARD_IDS → pile sizes 3 →
player core 10 → player powers NUM_POWER_IDS → Necrobinder 17 (Osty 4 +
Souls-per-zone 4 + 3 ally slots x3) → enemies 5x(7 + 15 intents +
NUM_POWER_IDS) → relics NUM_RELIC_IDS+1 → potion flags 5 → run segment 78
(act one-hots, floors, hp/gold, 3 Act4Heart keys, deck aggregates,
act-slot candidate one-hots, 3-row map lookahead x9 room types, phase
one-hot 9 + subscreen flags, ascension/elite/boss).

### Invariant 3: sizes derive from enums — docstring numbers are stale

`NUM_CARD_IDS`/`NUM_POWER_IDS`/`NUM_RELIC_IDS` are computed from live enums
(`rich_observation.py:68-78`). Actuals as of 2026-07-24: cards 586, powers
293, relics 308, potions 64, boss vocab 38. The docstring/inline comments
say 582/282/295 — stale, harmless (offsets self-adjust), do not quote them.
Padded vocab constants `POTION_VOCAB_SIZE=96` and `BOSS_VOCAB_SIZE=64`
leave slack so mod-registered potions/bosses do not change the layout
(`rich_observation.py:85,137`).

### Invariant 4: masking is the honesty layer

Invalid actions are silently ignored (combat envs) or fall back to end-turn
(run env). The mask (`get_action_mask`, `gym_env/action_space.py`) is what
keeps the agent honest — every eval/inference call must pass
`action_masks`. Note `ANY_ALLY` card targets are masked into the same
enemy-target index range (`action_space.py:97-100`).

### Invariant 5: growing an enum invalidates trained models

Adding a CardId/PowerId/RelicId (or a boss) shifts embedding indices and
segment offsets, changing what every trained checkpoint's weights mean.
This is accepted (content additions are rare and retraining is the norm) but
must be DECLARED when landing such a change — route through
sts2-change-control and note it in the training campaign log.

---

## Contract 8: Bridge wire-format invariants

Full bridge ops, build, and protocol live in sts2-bridge-and-realgame. The
architecture-level invariants that sim/env changes must respect:

1. **Option-list position IS the action encoding.** For non-combat phases,
   the Python adapter (`sts2_env/bridge/run_state_adapter.py`) maps run-env
   action indices onto the ORDERED option list in the bridge JSON — the
   assumption is that the game-side list order matches
   `RunManager.get_available_actions()` order for the same phase
   (`run_state_adapter.py:15-17`, and the mapping code indexing
   `options[local]`). If you reorder options in ANY `RunManager`
   `_actions_*` method, you break real-game play even though every sim test
   stays green. Docs/KNOWN_ISSUES.md #16 records which orderings are
   verified-by-reading-C#-only (shop ordering notably unverified).
2. **No top-level `player` key in non-combat payloads.** The combat
   `StateAdapter` classifies any state containing `player` as a combat
   observation (`state_adapter.py:117-119`). Non-combat payloads carry
   top-level `hp`/`max_hp` ints instead (C# `RunStateBridgeFields.Apply`).
   Never "helpfully" add a `player` dict to a non-combat payload or handler.
3. **Case-normalize enum strings crossing the C#/Python boundary.** C#
   `enum.ToString()` yields PascalCase; Python `Enum.name` yields
   UPPER_SNAKE. The powers path normalizes with `.upper()`; the intent path
   does NOT — `_INTENT_STR_TO_IDX` (`state_adapter.py:55-61`) keys are
   UPPER_SNAKE (`protocol.py:121-128`), so the live intent one-hot is
   silently all-zero (open bug, found 2026-07-24, not yet in
   KNOWN_ISSUES.md; also `"MULTI_ATTACK"` has no C# enum member — multi-hit
   is `AttackIntent.Repeats`). When adding adapter fields, normalize both
   sides and test with the REAL C# casing.
4. **Model-shape gate.** `agent_runner.detect_model_mode`
   (`agent_runner.py:132-165`) accepts exactly (115 actions, 131 obs) or
   (157 actions, 151 obs) and raises `ValueError` otherwise. The rich
   4184-dim models the current campaign trains CANNOT be loaded by the
   bridge runner until someone writes a rich adapter + a (157, 4184) detect
   branch — an open, known-deferred gap. Do not claim real-game capability
   for rich models.
5. **Osty is not a player creature** — in the sim AND the real game (single
   self-looping `NOTHING_MOVE`; `decompiled_v0.109.0/...Monsters/Osty.cs`,
   analysis in KNOWN_ISSUES.md #16). The run env's `player_select` slice
   (150-156) is solo-play dead space; do not build features that assume Osty
   is selectable.

---

## Known-weak points (stated plainly, as of 2026-07-24)

| # | Weakness | Status | Where |
|---|---|---|---|
| 1 | `_run_rng` falls back to one merged combat stream in standalone tests — stream-routing bugs invisible to unit tests | By design; mitigate with run-attached tests | `combat.py:463-468` |
| 2 | Live-bridge intent one-hot always zero (casing mismatch) | OPEN, not in KNOWN_ISSUES.md ledger yet | `state_adapter.py:55-61,196` |
| 3 | Rich (4184-dim) models rejected by the bridge runner | OPEN / deferred | `agent_runner.py:132-165` |
| 4 | `ActConfig` `*_ids` lists are decorative; real pools are the setup-function lists | By design; trap for editors | `run_manager.py:161-211` |
| 5 | `hooks.py` docstring understates dispatch surface (cards + modifiers also dispatched) | Stale docstring | `hooks.py:8-9` |
| 6 | `rich_observation.py` docstring counts (582/282/295) stale vs live enums (586/293/308) | Harmless; offsets computed | `rich_observation.py:24-29,69-77` |
| 7 | `run_state.py:1403-1405` says `act_selection` is a no-op — false since AFTP acts registered | Stale comment | `run_state.py:1403-1406` |
| 8 | `PlayerState.gain_potion_slots` defined twice; the later definition (allows negative, clamps at 0) silently wins | Latent footgun | `run_state.py:208-210` vs `414-415` |
| 9 | Non-combat option ordering vs `RunManager` verified by reading C# only, never live; shop ordering unverified; sell-Foul-Potion has no bridge representation | OPEN | KNOWN_ISSUES.md #16 |
| 10 | Hand-authored Act 4 map means `ACT_3.num_rooms/num_weak_encounters` are never exercised | By design | `acts.py:176-184` |
| 11 | `sts2_env.core.combat` fails on cold import unless `sts2_env.cards` initializes first (Contract 6, invariant 2) | Latent footgun for new entry points | `combat.py:24`, `cards/__init__.py:7` |

Never re-fight settled battles: the circular import (b3e97b1), the
combat-count weak-pool gating (18a8059), the energy/AutoPlay bug, and the
hook-order fix log are chronicled in sts2-failure-archaeology.

Any change touching these contracts is a "sim behavior" change under
sts2-change-control: full suite (5,276 tests at 18a8059) green + the four
parity audit scripts, before it counts as done.

---

## Provenance and maintenance

All claims verified 2026-07-24 against HEAD `fe25668` (working tree had
unrelated `sts2_env/content/` + `web/play_run.py` modifications). File:line
references drift with edits — re-grep for the symbol if a line looks off.
Run everything from `C:\Users\motqu\GitHub\sts2-rl-agent`.

| Fact | Re-verify with |
|---|---|
| HEAD + tree state | `git log --oneline -3; git status` |
| Obs/action sizes (4184 / 115 / 157 / 151 / 131) | `.venv\Scripts\python.exe -c "import sts2_env.gym_env.rich_observation as ro; from sts2_env.core.constants import ACTION_SPACE_SIZE; from sts2_env.gym_env.run_env import TOTAL_ACTIONS, RUN_OBS_SIZE; from sts2_env.gym_env.observation import OBS_SIZE; print(ro.RICH_OBS_SIZE, ACTION_SPACE_SIZE, TOTAL_ACTIONS, RUN_OBS_SIZE, OBS_SIZE)"` |
| Enum vocab sizes (586/293/308/64/38) | `.venv\Scripts\python.exe -c "import sts2_env.gym_env.rich_observation as ro; print(ro.NUM_CARD_IDS, ro.NUM_POWER_IDS, ro.NUM_RELIC_IDS, ro.NUM_POTION_IDS, len(ro.BOSS_NAME_TO_IDX))"` |
| Turn-setup stage names | `.venv\Scripts\python.exe -c "import sts2_env.cards; import inspect, sts2_env.core.combat as c; src=inspect.getsource(c.CombatState._continue_player_turn_setup); [print(l.strip()) for l in src.splitlines() if 'stage ==' in l]"` (the `sts2_env.cards` import first is mandatory — see Contract 6, invariant 2) |
| Named RNG streams | `.venv\Scripts\python.exe -c "from sts2_env.run.run_state import RunRngSet; import inspect; print(inspect.getsource(RunRngSet.__init__))"` |
| RunManager RNG offset (9999) | `.venv\Scripts\python.exe -c "from sts2_env.run.run_manager import RUN_MANAGER_RNG_SEED_OFFSET as o; print(o)"` |
| Act slot candidates (3/2/2) | `.venv\Scripts\python.exe -c "from sts2_env.map.acts import act_candidates_for_slot; [print(s, [a.act_id for a in act_candidates_for_slot(s)]) for s in range(3)]"` |
| final_boss_act_index == 2 | `.venv\Scripts\python.exe -c "from sts2_env.run.run_state import RunState; print(RunState(seed=1).final_boss_act_index)"` |
| Combat-prefix action layout | `.venv\Scripts\python.exe -c "from sts2_env.gym_env import run_env as r; print(r._COMBAT_START, r._COMBAT_SIZE, r._MAP_START, r._PLAYER_SELECT_START, r.TOTAL_ACTIONS)"` |
| detect_model_mode gate | `.venv\Scripts\python.exe -c "import inspect; from sts2_env.bridge.agent_runner import detect_model_mode as d; print(inspect.getsource(d))"` |
| Intent-casing bug still open | `.venv\Scripts\python.exe -c "from sts2_env.bridge.state_adapter import _INTENT_STR_TO_IDX; print(list(_INTENT_STR_TO_IDX))"` (UPPER_SNAKE keys = still broken vs PascalCase wire) |
| Registration side effects intact | `.venv\Scripts\python.exe -c "import sts2_env.powers, sts2_env.cards, sts2_env.events; from sts2_env.core.creature import _POWER_CLASSES; print(len(_POWER_CLASSES))"` |
| Full gate (any contract change) | `.venv\Scripts\python.exe -m pytest tests/ -q` plus the four audit scripts (see sts2-parity-discipline) |

Volatile facts to re-check on next revisit: whether KNOWN_ISSUES.md has
gained entries for the intent-casing bug and rich-model bridge gap; whether
a rich bridge adapter landed (search `rich` under `sts2_env/bridge/`);
whether stale comments (#5, #6, #7 in the weak-points table) were fixed;
enum sizes after any content addition.
