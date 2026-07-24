---
name: sts2-game-and-mods-reference
description: >
  Domain reference for Slay the Spire 2 AS IMPLEMENTED in this simulator:
  the Necrobinder kit (Osty, Souls, Doom, BoundPhylactery), beta v0.109.0
  deltas and the PATCHED card allowlists, the act/slot system, the
  ActsFromThePast mod (legacy acts, shrine pools, 15 shared shrines), the
  Act4Heart mod (three keys, 4-node Act 4, Corrupt Heart), ascension A1-A10
  as implemented, and how to navigate the three decompiled C# ground-truth
  trees. Load this when you need to know WHAT a game rule, character
  mechanic, mod behavior, act layout, boss moveset, or ascension effect IS
  in this project, or which decompiled tree to trust for it. Do NOT load
  this for: how to run audits or port RNG calls (sts2-parity-discipline),
  engine internals like hook order or pending_choice mechanics
  (sts2-architecture-contract), launching training or the play CLI
  (sts2-run-and-operate), triaging a failing run (sts2-debugging-playbook),
  or the real-game bridge (sts2-bridge-and-realgame).
---

# STS2 Game and Mods Reference (as implemented in sts2-rl-agent)

This is the domain pack for the game itself: Slay the Spire 2 beta
**v0.109.0** plus the two ACTIVE gameplay mods (**ActsFromThePast** and
**Act4Heart**), as reimplemented by the Python simulator in `sts2_env/`.
Everything below was re-verified against the repo on **2026-07-24**
(HEAD `fe25668`; note the working tree carried uncommitted `sts2_env/content/`
and `sts2_env/web/` tooltip-preview edits at that time — none affect game
rules, but run `git -C C:\Users\motqu\GitHub\sts2-rl-agent status` yourself
before trusting line numbers).

You already know Slay the Spire 1 and RL. This file teaches only what is
STS2-specific, v0.109.0-specific, mod-specific, or simulator-specific.

---

## 1. Ground-truth trees and precedence

Three decompiled C# trees sit at the repo root. Every parity claim in this
project cites one of them.

| Tree | What it is | Use it for | Size (2026-07-24) |
|---|---|---|---|
| `decompiled/` | Pre-patch decompile of the game | Machine-parsed static card metadata (the audit scripts regex-parse `decompiled/MegaCrit.Sts2.Core.Models.Cards`) | 219 namespace dirs |
| `decompiled_v0.109.0/` | Current-game decompile (the version the user actually plays) | **Behavioral truth.** Bridge/Harmony signatures, post-patch balance, anything the two trees disagree on | 222 namespace dirs |
| `decompiled_mods/` | Decompiled mod DLLs: `Act4Heart`, `ActsFromThePast`, `BaseLib`, `Downfall` | Mod mechanics (hooks, patches, key logic) | 2,586 `.cs` files |

Precedence rule: **`decompiled_v0.109.0/` wins on behavior.** `decompiled/`
remains the parse target of the static-metadata audit only because it is the
tree the parser was written against; deliberate v0.109.0-matching deviations
from it are enumerated in PATCHED allowlists (section 3). Of the four mods
in `decompiled_mods/`, only ActsFromThePast and Act4Heart are simulated;
BaseLib is a mod-support library and Downfall was verified inert for
Necrobinder (commit `0078f70` message). Do not implement Downfall content.

Searching the trees (PowerShell, copy-paste from repo root):

```powershell
# Find a class file by name across all three trees
Get-ChildItem -Recurse -Filter "CorruptHeart*.cs" decompiled,decompiled_v0.109.0,decompiled_mods | Select-Object FullName

# Grep a mechanic inside the current-game tree
Select-String -Path "decompiled_v0.109.0\MegaCrit.Sts2.Core.Models.Monsters\Osty.cs" -Pattern "NOTHING_MOVE|MoveState"

# Grep the mod trees for a Harmony patch class
Select-String -Path "decompiled_mods\Act4Heart\**\*.cs" -Pattern "FixAct3Boss" -List
```

Useful mod anchor files (all verified to exist):

- `decompiled_mods/Act4Heart/Act4Heart.Hooks/Act4Hooks.cs` — act-append + `FixAct3Boss_IL_`
- `decompiled_mods/Act4Heart/Act4Heart/TheEndingMap.cs` — the 4-node Act 4 map
- `decompiled_mods/Act4Heart/Act4Heart.Keys/` — `GreenKeyHooks`/`RedKeyHooks`/`BlueKeyHooks`, `RecallSiteOption`
- `decompiled_mods/ActsFromThePast/ActsFromThePast.Patches.Events/ShrinePatches.cs` — shrine interleave + shared-event gating
- `decompiled_mods/ActsFromThePast/ActsFromThePast.SharedEvents/` — the 15 shared shrines

---

## 2. Necrobinder kit

Character config (`sts2_env/characters/all.py:126-140`, mirrored in
`sts2_env/run/run_manager.py:117-122`):

| Stat | Value |
|---|---|
| Starting HP | **66** (a v0.109.0 change; audits/tests assume it) |
| Starting gold | 99 |
| Max energy | 3 |
| Starter relic | **BoundPhylactery** |
| Starter deck (10) | 4x StrikeNecrobinder, 4x DefendNecrobinder, Bodyguard, Unleash |
| Post-combat heal | 0 (only Ironclad has one, +6) |
| Card pool | 88 cards: 4 Basic, 20 Common, 36 Uncommon, 26 Rare, 2 Ancient (`sts2_env/cards/necrobinder.py:1-5`) |
| Mechanic flags | `has_osty=True`. NOT `uses_stars` — Stars are **Regent's** resource, not Necrobinder's |

### Osty (the pet)

Osty is a summonable ally skeleton, the center of the Necrobinder kit.
Simulator model (`sts2_env/core/combat.py:2973-3038`, `summon_osty`):

- An ally `Creature` with `is_pet=True`, `is_osty=True`, `monster_id="OSTY"`,
  living in `combat.allies` (and aliased as `combat.osty` for the primary
  player). It is **not** a player creature and never takes an action.
- In the real game Osty is likewise a *monster* model whose move state
  machine is a single self-looping `NOTHING_MOVE` that does nothing
  (`decompiled_v0.109.0/MegaCrit.Sts2.Core.Models.Monsters/Osty.cs`; see
  the solo-play analysis in `docs/KNOWN_ISSUES.md` line 207). The sim's
  in-combat ally has no AI at all; a `create_osty` factory with the
  NOTHING_MOVE loop exists in `sts2_env/monsters/act3.py:1755` for the
  SoulNexus encounter.
- **Summon semantics** (`summon_osty(owner, amount)`): no Osty yet -> create
  with `max_hp = current_hp = amount`; Osty alive -> `gain_max_hp(amount)`
  (summons STACK onto a living Osty); Osty dead -> revive at `amount` HP
  (fires `after_osty_revived`). `modify_summon_amount` hooks apply first.
- **DIE_FOR_YOU redirect**: every Osty carries the `DIE_FOR_YOU` power
  (`sts2_env/powers/remaining_a.py:1186-1203`) — *powered attacks*
  (`ValueProp.MOVE` and not `UNPOWERED`) targeting the owner are redirected
  to the living Osty. Non-attack HP loss and unpowered damage still hit the
  player. The damage pipeline has a dedicated before/after-Osty modifier
  split; that ordering contract belongs to sts2-architecture-contract.
- **Osty attacks**: "Osty attacks for N" cards deal damage *from* the Osty
  creature (`combat.deal_damage(osty, target, ...)` in
  `sts2_env/cards/necrobinder.py:_deal_osty_damage_single`), so Strength on
  Osty and the CALCIFY power ("Osty's attacks deal +X") scale them; if Osty
  is dead or absent the attack simply does nothing.
- **BoundPhylactery** (`sts2_env/relics/starter.py:112-124`): summon 1 Osty
  at combat start (`before_combat_start`) and +1 per turn from round 2
  onward (`after_energy_reset_late`). Upgraded form **PhylacteryUnbound**:
  5 at combat start, +2 per player turn. The starter-relic upgrade map
  (`run_state.py:1293-1299`) pairs BOUND_PHYLACTERY -> PHYLACTERY_UNBOUND.

### Souls

A **Soul** is a status-rarity card, not a resource counter:
`CardId.SOUL`, cost 0, Skill, self-target, `exhaust`, effect "draw 2 cards"
(3 when upgraded) — `sts2_env/cards/status.py:1227-1244`. Necrobinder cards
(Dirge, Soul Storm, etc.), a potion, and a relic generate Souls into piles
(`make_soul` call sites across `cards/necrobinder.py`, `potions/effects.py:704`,
`relics/uncommon.py:175`). Some card effects count Souls in specific piles,
e.g. `necrobinder.py:980` counts SOUL cards in the exhaust pile. The rich
observation exposes per-pile Soul counts (see the obs layout in
sts2-architecture-contract).

### Doom

`PowerId.DOOM` (`sts2_env/powers/turn_effects.py:994-1021`): at end of the
owner's side's turn, any creature whose current HP <= its Doom amount is
killed outright (`kill_doomed_creatures`). Several Necrobinder cards apply
or exploit Doom; parity tests live in
`tests/test_necrobinder_soul_and_doom_parity.py`.

---

## 3. v0.109.0 deltas and the PATCHED allowlists

The sim was realigned to the live game at commit `0078f70` (2026-07-23,
"Checkpoint: match sim to v0.109.0 + active mods"). Per that commit: fixed
all vanilla drift — Necrobinder HP 66, Eidolon/BorrowedTime/Seance reworks,
~15 card tweaks, ~25 monster stat/moveset updates, and the Act 3 boss swap
**Doormaker -> Aeonglass** (Doormaker's setup is kept for tests but no
longer wired into `BOSS_ENCOUNTERS`; `sts2_env/encounters/act3.py:169-179`).

Because the static-metadata audit parses the OLDER `decompiled/` tree,
cards deliberately matching v0.109.0 instead are skipped via allowlists.
Verbatim, as of 2026-07-24 (both audits pass at HEAD):

- `scripts/audit_card_static_metadata.py:26-32` `PATCHED_NECROBINDER_CARD_IDS`
  (5): BANSHEES_CRY, BORROWED_TIME, DIRGE, EIDOLON, SEANCE
- `scripts/audit_card_dynamic_vars.py:25-38` `PATCHED_NECROBINDER_CARD_IDS`
  (12): BORROWED_TIME, DANSE_MACABRE, DEATH_MARCH, DEBILITATE_CARD, DEFY,
  GRAVE_WARDEN, HAUNT, REAVE, SCULPTING_STRIKE, SIC_EM, SOUL_STORM,
  THE_SCYTHE

Union: **16 distinct cards** (BORROWED_TIME appears in both). Example of
what a delta looks like: BorrowedTime was reworked in v0.109.0 from "cost 0,
gain 1 Energy, Doom 3 self" to "cost 1, gain 4 Energy (+2 upgraded), apply
Borrowed Time" (`docs/CARDS_REFERENCE.md:3552`).

Rules (enforcement details live in sts2-parity-discipline):

1. A new deliberate v0.109.0-matching deviation must be added to the
   allowlist in **both** audit scripts (where applicable) and cite the
   `decompiled_v0.109.0` file in a comment.
2. Never "fix" an allowlisted card back toward `decompiled/` — that silently
   regresses live-game parity while making the audit look cleaner.
3. `docs/CARDS_REFERENCE.md` is load-bearing runtime data:
   `sts2_env/content/descriptions.py` derives tooltip text from it (see
   sts2-docs-and-writing before editing it).

---

## 4. The run skeleton: acts, slots, pool_key

A run is always **4 acts**: three per-run-randomized slots plus the fixed
Act4Heart ending act (`sts2_env/run/run_state.py:1440-1443`):

```python
acts = [select_act_for_slot(slot, rng.act_selection) for slot in range(3)] + [ACT_3]
```

Slot candidates, rolled uniformly via the dedicated `act_selection` RNG
stream (`sts2_env/map/acts.py`; registration at lines 351-376):

| Slot | Candidates (`act_id`) | `pool_key` | Rooms / weak fights | Real boss pool (`encounters/<pool_key>.py BOSS_ENCOUNTERS`) |
|---|---|---|---|---|
| 0 | Overgrowth (vanilla default) | `act1` | 15 / 3 | Vantom, CeremonialBeast, TheKin |
| 0 | Underdocks (vanilla alternate) | `act4` | 15 / 3 | WaterfallGiant, SoulFysh, LagavulinMatriarch |
| 0 | Exordium (AFTP, legacy) | `exordium` | 15 / 3 | SlimeBoss, Guardian, Hexaghost |
| 1 | Hive (vanilla default) | `act2` | 14 / 2 | TheInsatiable, KnowledgeDemon, KaiserCrab |
| 1 | TheCity (AFTP, legacy) | `thecity` | 14 / 2 | Champ, Collector, BronzeAutomaton |
| 2 | Glory (vanilla default) | `act3` | 13 / 2 | Queen(+TorchHeadAmalgam), TestSubject, Aeonglass |
| 2 | TheBeyond (AFTP, legacy) | `thebeyond` | 13 / 2 | AwakenedOne, DonuAndDeca, TimeEater |
| 3 (fixed) | TheEnding (Act4Heart) | `act4_heart` | hand-authored | CorruptHeart |

Jargon: a **pool_key** is the `ActConfig` field naming which
`sts2_env/encounters/<pool_key>.py` module supplies the act's
WEAK/NORMAL/ELITE/BOSS encounter pools; `RunManager._get_encounter_pools`
(`run_manager.py:161-211`) switches on it. A **legacy act** is an AFTP
STS1-recreation act (`is_legacy=True`), which changes event-pool building
(section 5).

### Traps in this area (all verified 2026-07-24)

- **`ActConfig.boss_ids` / `*_encounter_ids` / `elite_ids` are decorative.**
  Encounter selection uses only the setup-function lists in
  `encounters/<pool_key>.py`. Proof: Overgrowth's `boss_ids` says
  `["TheLich"]` and Hive's says Collector/Automaton/Champ (`map/acts.py:68,102`)
  while the pools actually fought are the ones in the table above. Editing
  the ID lists changes nothing; several are stale text.
- **`act4` naming**: `encounters/act4.py` + `monsters/act4.py` are vanilla
  **Underdocks** (an Act-1-slot alternate; the module name predates the mod).
  The real 4th act is `encounters/act4_heart.py`. The `else` fallback branch
  of `_get_encounter_pools` (`run_manager.py:200-205`) resolves to
  act4/Underdocks.
- **Import cycle**: `map/acts.py` must never import the events package at
  module level; legacy-act construction is deferred to first registry access
  (`_ensure_alternate_acts_registered`, `acts.py:354-375`; broke web/CLI
  play, fixed in commit `b3e97b1`). To add an act variant: (1) `ActConfig`
  with unique `pool_key`/`act_id` in `map/acts.py`, (2) create
  `encounters/<pool_key>.py` with the four setup-function lists, (3) add an
  `elif` in `_get_encounter_pools`, (4) `register_act_candidate(slot, cfg)`
  inside `_ensure_alternate_acts_registered` — never at import time.
- **Stale comments** to ignore: `run_state.py:1403-1405` claims
  `act_selection` is "a no-op today" (false since commit `6860139` — every
  slot now has 2-3 candidates); `map/acts.py:216-223` claims legacy acts
  "are NOT registered here" (they are, 130 lines lower); the `acts.py:173`
  comment calling TheEnding "identifier 'glory'" collides with vanilla
  Glory's `act_id` and is unverified — trust `act_id="TheEnding"` in code.

### Encounter pacing and boss visibility

- **Weak-vs-regular gate is combat-count based** (fixed in commit `18a8059`
  per decompiled `ActModel.GenerateRooms`): the first
  `ActConfig.num_weak_encounters` REGULAR monster combats in an act draw
  from the weak pool; "?"-node monster fights count, elites/bosses do not
  (`run_state.py:1449-1457` counter, `run_manager.py:599-615` gate,
  locked by `tests/test_weak_encounter_combat_count.py`).
- **The act boss is rolled up-front** when the act map is generated
  (`run_manager.py:289-290, 359-363`) using `RunManager._rng` (the seed+9999
  root — the two-RNG-roots contract is sts2-architecture-contract's topic),
  so it is knowable from the map screen, matching the real game.
- **Winning**: `enter_next_act` past the last act sets
  `is_over=True, player_won=True` (`run_state.py:1684-1691`). "Final boss"
  reward semantics use `RunState.final_boss_act_index`, which is **2** (the
  Act-3 slot boss) whenever there are >= 4 acts (`run_state.py:1494-1506`,
  mirroring `Act4Hooks.cs:FixAct3Boss_IL_`) — never use `len(acts)-1` for
  final-boss gating.
- **Boss relic screen**: hardcoded 11-relic pool (`run_manager.py:214-226`:
  Astrolabe, BlackStar, CallingBell, Ectoplasm, PandorasBox,
  PhilosophersStone, RunicPyramid, SneckoEye, Sozu, TouchOfOrobas,
  VelvetChoker), shuffled by `RunManager._rng`, offers 3 excluding owned
  (`run_manager.py:781-787`).
- **Unknown ("?") node odds** (`run/odds.py:36-90`): base MONSTER 0.10,
  ELITE -1.0 (impossible until boosted), TREASURE 0.02, SHOP 0.03,
  remainder EVENT; unrolled types accumulate their base each roll. A
  first-run tutorial path forces 2 events then a monster — irrelevant for
  training (unlock state marks runs as non-first).

---

## 5. ActsFromThePast (AFTP)

AFTP contributes: the three legacy acts (table above, all STS1 boss
recreations), 15 shared shrine events, a shrine-interleaved event-pool
builder for legacy acts, event act-restrictions, and a handful of global
relics (Necronomicon, NlothsGift, OddMushroom in `relics/shop_event.py`;
MarkOfTheBloom in `relics/thebeyond.py` — it zeroes ALL healing via a
duck-typed `modify_heal_amount` hook on `PlayerState.heal`/`Creature.heal`,
so any healing change must go through those methods or it bypasses the mod).

### Event gating model (`sts2_env/run/events.py`)

`EventModel` gating fields (lines 44-63): `is_shared`,
`is_legacy_exclusive`, `is_shrine`, `is_one_time_event`,
`allowed_act_numbers` (1-based act number = act_index + 1, the mod's
`IActRestricted`). Config flags in `sts2_env/core/constants.py:50-51`
(mirroring `ActsFromThePastConfig.cs`):

- `ALLOW_NON_LEGACY_SHARED_EVENTS_IN_LEGACY_ACTS = True` — base-game shared
  events DO appear in legacy acts.
- `ALLOW_LEGACY_SHARED_EVENTS_IN_NON_LEGACY_ACTS = False` — AFTP shrines
  NEVER appear in vanilla acts (`event_allowed_in_act`, events.py:252-271).

Event availability = registered (import side effect via
`sts2_env/events/__init__.py` — a new event module MUST be imported there)
AND `is_allowed(run_state)` AND not blocked-by-visited AND
`event_allowed_in_act`. **Repeatable shrines bypass the visited-events
exclusion** (`_event_blocked_by_visited`, events.py:274-284, mirroring the
mod's `RepeatableShrineValidityPatch`).

### Legacy event-pool construction

At room generation, each legacy act's event order is rebuilt by
`build_legacy_event_pool` (`events.py:351-394`, mirroring
`ShrinePatches.EventPoolPatch`): filter candidates through
`event_allowed_in_act` + `allowed_act_numbers`, split shrines from
regulars, then interleave — each output slot draws a shrine with
probability `SHRINE_DRAW_CHANCE = 0.25` while shrines remain. Uses the
dedicated stream `Rng(seed, f"legacy_event_pool_{i}")`
(`run_state.py:1563-1581`); vanilla acts instead get
`rng.up_front.shuffle(event_ids)`. Never reroute these to another stream —
determinism of existing seeds depends on it.

`pick_event` trap (events.py:300-334): it walks the act's ordered pool from
`act.events_visited`, but if NO candidate passes the filters it
**force-picks** `pool[event_index % len(pool)]` anyway — an "impossible"
event can still appear. Keep this in mind before declaring an event-gating
bug (sts2-debugging-playbook).

### The 15 shared shrines (`sts2_env/events/aftp_shared.py`)

All 15 AFTP SharedEvents are shrines (`is_shrine=True`), all
`is_legacy_exclusive`, implemented for CLASSIC mode only (every
`RebalancedMode` branch of the C# is a dead branch here). Split verified
against the module (9 one-time / 6 repeatable, matching
`IShrineEvent.IsOneTimeEvent`):

| Kind | Events |
|---|---|
| One-time (9) | BonfireSpirits, Duplicator, FaceTrader (acts 1-2 only), Lab, OminousForge, TheDivineFountain, TheWomanInBlue, WeMeetAgain, DesignerInSpire (acts 2-3 only) |
| Repeatable (6) | GoldenShrine, MatchAndKeep, Purifier, Transmogrifier, UpgradeShrine, WheelOfChange |

Numbers worth knowing (all read from the implementation; details and C#
citations in the module docstring, `aftp_shared.py:1-74`): GoldenShrine
Pray +50g / Desecrate +275g + Regret; WheelOfChange equal 1/6 odds
(act-scaled gold 100/200/300, relic, full heal, Decay curse, remove-a-card,
damage = trunc(15% max HP)); TheWomanInBlue 1/2/3 potions at 20/30/40g,
leaving costs ceil(5% max HP); FaceTrader touch = max(1, maxHP//10) damage
for 50g. **MatchAndKeep is a real interactive memory minigame** (12
face-down cards, 5 attempts of 2 flips; option labels reveal already-seen
cards, so perfect play is learnable by the agent). One documented sim
deviation: its curse pairs draw from the vanilla curse pool (the mod would
add Pain/Necronomicurse).

---

## 6. Act4Heart

Modeled as an **always-on** `Act4HeartModifier` appended to
`RunState.modifiers` at construction (`run_state.py:1464-1469`; class at
`run/modifiers.py:599-710`) — never gated behind unlocks, confirmed against
the user's game logs.

### Act 4 ("TheEnding")

- Hand-authored map, exactly Start -> Rest Site -> Elite -> Boss in a
  straight line; no shops/events/treasure/"?" nodes
  (`map/generator.py:95-125`, C# `TheEndingMap.cs`).
- On Act-4 entry every player heals ALL missing HP, reduced to
  `floor(missing * 0.75)` at ascension >= 2
  (`modifiers.py:634-643`; constants at `modifiers.py:574-576`).
- **The KeyDoor is NOT gated**: in the mod, the "Delusion" option (always
  available when keys are missing) proceeds to Act 4 exactly like
  "Succeed", so the sim always advances Act 3 -> Act 4 regardless of keys
  (`modifiers.py:609-612`). The keys are therefore OPTIONAL side objectives,
  not progression requirements.

### The three keys

Key relics themselves are inert markers (rarity ANCIENT + pool EVENT, so
normal relic rolls can never offer them — `relics/act4_heart_keys.py`).
All behavior lives in the modifier/rest-site/treasure code:

| Key | How obtained | Implementation |
|---|---|---|
| Emerald | Kill the act's secretly pre-marked "Super Elite" (one elite map node per act, acts 1-3) | Marking: `Rng(run_seed + act_index, "se_coord")` picks among elite nodes (`modifiers.py:654-665`). At combat start the elite rolls one of 4 buffs via `Rng(seed + act_number, "se_buff")`: Strength `act_number`, Metallicize `2*act+2`, non-decaying Regenerate `2*act+1` (`REGENERATE_A4H`), or +25% max HP (`modifiers.py:680-701`). Key arrives as an extra `RelicReward` (`modify_rewards`) |
| Ruby | Choose the extra "Recall" option at any Rest Site | `RecallOption` (`run/rest_site.py:270-293`), offered while the key is missing (`modifiers.py:647-650`) |
| Sapphire | SKIP the Treasure Room relic | `RunManager._do_treasure_skip` (`run_manager.py:2184-2211`, C# `BlueKeyHooks.cs`) |

### Corrupt Heart (`sts2_env/monsters/act4_heart.py:85-206`)

HP **750** (800 at A8+). On spawn: **Beat of Death 1** (2 at A9+) and
**Invincible 300** (200 at A9+).

- **Beat of Death** (`powers/act4_heart.py:30-58`): whoever PLAYS a card
  takes Amount damage — unpowered (no Strength/Vuln scaling) but
  **blockable**.
- **Invincible** (`powers/act4_heart.py:64-109`): caps total unblocked HP
  loss per turn-cycle at Amount; the accumulator resets when the Heart's
  side starts its turn.
- Move script: opens with **Debilitate** (Vulnerable 2 + Weak 2 + Frail 2
  and one each of Dazed/Slimed/Wound/Burn/Void into the discard), then
  random-branches between **Blood Shots** (2 x 15 hits) and **Echo** (45),
  with a bitfield forcing the other attack next; once both attacks have
  been used it plays **Buff**, then returns to the attack branch. Every
  Buff: reset negative Strength to 0 and gain **+2 Strength**, plus a
  monotonic ladder that never resets — 1st Artifact 2, 2nd Beat of Death
  +1, 3rd Painful Stabs 1, 4th **+10 Strength**, 5th and beyond
  **+50 Strength** each. Stalling loses; plan lethal around the Invincible
  cap per cycle.

### Spire Shield + Spire Spear (the Act-4 elite duo)

Both spawn with Artifact 1 (2 at A9+) and back-attack powers
(`BACK_ATTACK_LEFT`/`RIGHT`). Shield (HP 110/125): Bash 14 + Strength/Focus
-1 (Focus only possible vs orb-capable characters, i.e. never vs
Necrobinder), Fortify 30 block to all, Smash 38 with block-gained-equal-to-
damage-dealt (flat 99 at A9+). Spear (HP 160/180): Burn Strike 6x2 + 2
Burns (into DRAW pile at A9+), Skewer 10x4, Piercer +2 Strength to both.
Spear's alternation bitfield starts at 2, so its opener sequence skips an
early Piercer (`act4_heart.py:347-350`). The `EmptyFightAct4Weak` encounter
is unreachable via the hand-authored map, and a zero-enemy combat resolves
as an instant win defensively (`core/combat.py:856-863`).

---

## 7. Ascension A1-A10 as implemented

Applied by `RunState._apply_ascension_effects` (`run_state.py:1583-1639`,
called from `initialize_run`) plus per-site checks. The campaign target is
A10, so the two gaps below matter.

| Level | Name | Effect | Where implemented (verified) |
|---|---|---|---|
| A1 | SwarmingElites | Elite map nodes 5 -> `round(5*1.6)` = 8 | `map/generator.py:345,350` |
| A2 | WearyTraveler | (C#: Neow heals 80% of missing HP) | **No-op in sim** — the sim's Neow is a pure relic-choice event with no heal (`events/act3.py:120-255`); no WearyTraveler code exists outside the docstring. Note: the *separate* Act4Heart entry-heal 75% reduction also keys off asc>=2 (`modifiers.py:575-576`) and IS implemented |
| A3 | Poverty | Starting gold x0.75 | `run_state.py:1606-1607` |
| A4 | TightBelt | -1 max potion slot | `run_state.py:1610-1611` |
| A5 | AscendersBane | Curse (unplayable, ethereal) in starting deck | `run_state.py:1614-1626` |
| A6 | Gloom | -1 rest site in map gen | `map/generator.py:346,358-359,367-368` |
| A7 | Scarcity | Reward-card upgrade scaling 0.25 -> 0.125 | `run/rewards.py:29-31,181-185` |
| A8 | ToughEnemies | Higher monster HP | Per-monster `_ascension_value(asc, 8, ...)` in every `monsters/*.py` (the `run_state.py:1632-1634` comment claiming monsters "use fixed values" is STALE) |
| A9 | DeadlyEnemies | Higher monster damage / nastier movesets | Per-monster `_ascension_value(asc, 9, ...)` |
| A10 | DoubleBoss | Two boss fights per act | **NOT implemented** as of 2026-07-24: `run_state.has_double_boss` is set (`run_state.py:1639`) but no code anywhere reads it (verified by repo-wide grep); `RunManager` always runs exactly one boss per act |

The A10 gap is a live parity issue for the campaign's headline claim
("Necrobinder A10 full-run"): the sim's A10 is easier than the real game's
if the real v0.109.0 A10 does spawn double bosses. Before fixing or citing
it, check `docs/KNOWN_ISSUES.md` / `docs/PARITY_GAPS.md` (it is documented
in neither as of 2026-07-24) and route the change through
sts2-change-control (sim-behavior class: full suite + parity audits).
Verify the real behavior first in
`decompiled_v0.109.0/` (search `AscensionManager` / `DoubleBoss`) before
assuming either direction.

---

## 8. Assorted run-layer reference numbers (verified 2026-07-24)

- Shop pricing (`run/shop.py:27-76`): rare card 150 / uncommon 75 / other
  50; colorless x1.15; price variance 0.95-1.05 (relics 0.85-1.15); sale
  item halved; card removal 75 + 25 per prior removal.
- Rest heal = `floor(maxHP * 0.3)` plus relic/modifier hooks
  (`run/rest_site.py`).
- Back-to-back shops from "?" nodes are blacklisted
  (`run_state.py` unknown-roll blacklist).
- RunManager phases: MAP_CHOICE, COMBAT, CARD_REWARD, BOSS_RELIC, SHOP,
  REST_SITE, EVENT, TREASURE, RUN_OVER (`run_manager.py:244-252`). A
  **pending_choice** (a mid-flow card-selection prompt on `RunState` or
  `CombatState`) can interrupt nearly any phase; the resumption-callback
  contract is sts2-architecture-contract's topic — just never transition
  phases directly after an action without checking
  `run_state.pending_choice`.
- Mod-related tests to run after touching anything in this file's scope:
  `tests/test_act4_heart_mod.py`, `tests/test_acts_from_the_past_foundations.py`,
  `tests/test_events_aftp_shared.py`, `tests/test_events_exordium.py`,
  `tests/test_thecity_events.py`, `tests/test_events_thebeyond.py`,
  `tests/test_exordium_monsters.py`, `tests/test_thecity_monsters.py`,
  `tests/test_thebeyond_monsters.py`, `tests/test_weak_encounter_combat_count.py`,
  and the `test_necrobinder_*` parity files — then the FULL suite per house
  rule (sts2-change-control).

```powershell
# Quick scoped check before the full suite
C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe -m pytest -q `
  tests/test_act4_heart_mod.py tests/test_acts_from_the_past_foundations.py `
  tests/test_events_aftp_shared.py tests/test_weak_encounter_combat_count.py
```

---

## Provenance and maintenance

Every fact above was verified 2026-07-24 at HEAD `fe25668` on the live repo.
Things drift; re-verify with these before trusting a stale copy of this
skill (run from `C:\Users\motqu\GitHub\sts2-rl-agent`; `py` =
`.venv\Scripts\python.exe`):

| Fact (date-stamped 2026-07-24) | Re-verify with |
|---|---|
| HEAD `fe25668`; tree state | `git log --oneline -3; git status --short` |
| Necrobinder HP 66 / BoundPhylactery / starter deck | `py -c "from sts2_env.characters.all import NECROBINDER as n; print(n.starting_hp, n.starting_relic, n.starting_deck)"` |
| 88-card Necrobinder pool | `py -c "from sts2_env.core.card_pools import NECROBINDER_CARD_POOL as p; print(len(p))"` |
| PATCHED allowlists (5 static / 12 dynamic) | `Select-String -Path scripts\audit_card_*.py -Pattern "PATCHED_NECROBINDER" -Context 0,14` |
| Both card audits pass at HEAD | `py scripts\audit_card_static_metadata.py; py scripts\audit_card_dynamic_vars.py` (exit 0 each) |
| Act slot candidates (3/2/2) and pool_keys | `py -c "from sts2_env.map.acts import act_candidates_for_slot as c; print([[a.act_id for a in c(s)] for s in range(3)])"` |
| Real boss pools per act | `Select-String -Path sts2_env\encounters\*.py -Pattern "BOSS_ENCOUNTERS: list" -Context 0,5` |
| 15 AFTP shrines (9 one-time / 6 repeatable) | `py -c "from sts2_env.events.aftp_shared import AFTP_SHARED_EVENT_IDS as e; print(len(e))"` |
| Shrine interleave chance 0.25 | `Select-String -Path sts2_env\run\events.py -Pattern "SHRINE_DRAW_CHANCE"` |
| KeyDoor ungated; Act-4 heal constants | `Select-String -Path sts2_env\run\modifiers.py -Pattern "ACT4_HEART_|Delusion"` |
| Corrupt Heart 750/800 HP, BoD 1/2, Invincible 300/200 | `Select-String -Path sts2_env\monsters\act4_heart.py -Pattern "CORRUPT_HEART_(BASE|TOUGH|DEADLY)"` |
| A10 double boss still unimplemented (write-only flag) | `Get-ChildItem -Recurse -Include *.py sts2_env,tests,scripts \| Select-String "has_double_boss"` (expect only the two `run_state.py` lines) |
| A2 Neow heal still absent | `Get-ChildItem -Recurse -Include *.py sts2_env \| Select-String "WearyTraveler"` (expect docstring hits only) |
| Decompiled tree dir counts (219 / 222 / 2,586 files) | `(Get-ChildItem -Directory decompiled).Count; (Get-ChildItem -Directory decompiled_v0.109.0).Count; (Get-ChildItem -Recurse -Filter *.cs decompiled_mods).Count` |
| v0.109.0 realignment scope | `git show -s --format=%B 0078f70` |
| Weak-pool gate combat-count based | `py -m pytest -q tests/test_weak_encounter_combat_count.py` |

If any command's output disagrees with this file, the repo wins — update
this skill (docs-class change; see sts2-change-control) rather than
trusting it.
