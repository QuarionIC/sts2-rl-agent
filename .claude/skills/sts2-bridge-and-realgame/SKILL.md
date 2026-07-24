---
name: sts2-bridge-and-realgame
description: >
  Everything about connecting the RL agent to the REAL Slay the Spire 2 game:
  the C# bridge mod (build, deploy, verify, Harmony patches, game-update
  survival), the TCP wire protocol, the Python state adapters and agent_runner,
  the replay/golden-comparison workflow, and the still-undone live smoke test.
  Load this when you are building or debugging bridge_mod/, running
  sts2_env/bridge/agent_runner.py against the live game, adding or fixing a
  bridge adapter field, updating the mod after a game patch, or recording /
  comparing live replay traces. Do NOT load this for: which model to train or
  launch (sts2-training-campaign), simulator parity audits and RNG porting
  (sts2-parity-discipline), operating the simulator/CLI/web play
  (sts2-run-and-operate), venv/dotnet/Godot installation from scratch
  (sts2-build-and-env), or the history of past bridge incidents
  (sts2-failure-archaeology).
---

# STS2 Bridge and Real-Game Operations

This skill covers the sim-to-real bridge: a Godot 4.5.1 / .NET 9 C# mod inside
the retail Slay the Spire 2 client, talking newline-delimited JSON over TCP to
a Python agent process that runs a trained MaskablePPO policy. All file:line
citations are against HEAD `fe25668` (2026-07-24) unless stated otherwise.

## Status snapshot (2026-07-24)

| Fact | State |
|---|---|
| Mod built + auto-deployed to Steam mods folder | YES, 2026-07-23 20:28 (Debug config, DLL 172,544 bytes) |
| Installed game version | v0.109.0 (`release_info.json` in the game dir, built 2026-07-17) — matches `decompiled_v0.109.0/` |
| Live end-to-end smoke test (game + mod + agent, full run) | **NEVER DONE.** `%APPDATA%\Godot\app_userdata\Slay the Spire 2\` does not even exist on this machine, consistent with the modded game never having been launched here |
| Rich-obs models (obs 4184) usable over the bridge | **NO** — `detect_model_mode` rejects them (see Open gaps) |
| Legacy models usable over the bridge | 115/131 combat-only and 157/151 full-run, auto-detected |
| Bridge Python tests | 73 collected across 5 files, all green at HEAD |

Working env rule (owned by sts2-build-and-env, restated because you will need
it here): never `uv sync` or reinstall over `.venv`; always invoke
`.venv\Scripts\python.exe`, never bare `python`.

## Glossary (defined once, used throughout)

- **Bridge**: the whole sim-to-real pathway — C# mod (`bridge_mod/`) + TCP
  protocol + Python package (`sts2_env/bridge/`).
- **AutoSlay**: the game's own shipped auto-play framework
  (`MegaCrit.Sts2.Core.AutoSlay`), normally locked behind
  `NGame.IsReleaseGame()`. The mod's whole strategy is to unlock it and swap
  its random decision handlers for TCP-driven ones (~300 lines of custom code
  instead of ~2,500; design rationale in `docs/AUTOSLAY_BRIDGE.md`, in
  Chinese).
- **Harmony**: the runtime C# method-patching library the mod uses
  (`0Harmony.dll` ships with the game).
- **Adapter**: Python class translating bridge JSON to the exact observation
  vector / action mask a trained model expects (`StateAdapter` for the
  131-dim combat layout, `RunStateAdapter` for the 151-dim full-run layout).
- **Golden replay**: a recorded live trace of bridge states + actions,
  re-compared field-by-field against a deterministic simulator reconstruction
  (`sts2_env/parity/bridge_replay.py`). "Parity" in this skill means the live
  game and the simulator agree on those compared fields; the four sim-side
  audit scripts belong to sts2-parity-discipline, not here.
- **PCK**: Godot resource pack. The game refuses `.pck` files exported by a
  Godot newer than 4.5.1. A valid one starts with the `GDPC` magic bytes.
- **request_id**: correlation token the C# server attaches to each state that
  expects a reply; the Python client echoes it automatically
  (`client.py:189-194`), and the server drops stale replies
  (`BridgeServer.cs:361-365`).

## Architecture

```
STS2 game process (Godot/.NET 9)                Python agent process
  MainFile.cs [ModInitializer]                    agent_runner.py main loop
    3 Harmony patches                               STS2GameClient (client.py)
    BridgeServer (TCP 127.0.0.1:9002)  <-- JSON --> StateAdapter (131/115)
    RlAutoSlayer (drives the run)                   RunStateAdapter (151/157)
      Rl*Handler per room/screen                    MaskablePPO .predict()
```

One JSON object per line. Game sends a typed state, blocks up to 30 s for the
agent's reply, then **falls back to random play and keeps going** — see
"Robustness contract" below.

### C# side inventory (`bridge_mod/`)

| File | Role |
|---|---|
| `MainFile.cs` | Entry (`[ModInitializer]`, `MainFile.cs:20`). Applies 3 Harmony patches, starts `BridgeServer` on port 9002 (`MainFile.cs:68-69`), waits for the main menu, then starts `RlAutoSlayer` (`MainFile.cs:100-121`) |
| `BridgeServer.cs` | Singleton TCP server, loopback only (`BridgeServer.cs:73`), one client at a time, request_id-correlated request/response (`SendStateAndWaitForActionAsync`, `BridgeServer.cs:125-159`) |
| `RlAutoSlayer.cs` | Rewrite of the game's AutoSlay run loop. **Hardcodes** `PreferredCharacterId = "Necrobinder"`, `PreferredAscension = 10`, `FinalRunFloor = 49`, `RunTimeoutMinutes = 60` (`RlAutoSlayer.cs:77-80`). Auto-abandons any existing run, clicks through menus, unlocks all epochs, sets FastMode (`RlAutoSlayer.cs:221-232`), installs `CardSelectCmd.UseSelector(new RlCardSelector())` (`RlAutoSlayer.cs:237`) |
| `RlCombatHandler.cs` | Combat: serializes state, executes play/end_turn/potion. 30 s `AgentTimeout` (`RlCombatHandler.cs:41`). Plays cards via `new PlayCardAction(card, target)` enqueued on `ActionQueueSynchronizer` (`RlCombatHandler.cs:346-347`) — NOT `CardCmd.AutoPlay`, which bypassed energy deduction (KNOWN_ISSUES #1; do not reintroduce) |
| `RlNonCombatRoomHandlers.cs` | Rest/shop/event/treasure handlers, `NonCombatBridgeProtocol` constants, and `RunStateBridgeFields.Apply()` (`RlNonCombatRoomHandlers.cs:83-156`) which merges run-level fields into EVERY payload |
| `RlMapHandler.cs` / `RlRewardsScreenHandler.cs` / `RlCardRewardScreenHandler.cs` / `RlCardBundleScreenHandler.cs` / `RlCrystalSphereScreenHandler.cs` | Screen handlers registered in the `RlAutoSlayer` ctor (`RlAutoSlayer.cs:130-143`) |
| `RlCardSelector.cs` | Implements the game's `ICardSelector` (TestSupport) so deck upgrade/transform/enchant/hand-select prompts bypass their UI screens and go to the agent; deck-screen entries in the handler map stay on the game's default AutoSlay handlers and are expected to be intercepted upstream by this selector |
| `STS2BridgeMod.csproj` | Build config: auto-detects Steam path, references game DLLs, `CopyToModsFolderOnBuild` deploy target (`STS2BridgeMod.csproj:117-127`), `GodotExport` PCK target (`:129-134`), `GodotPath` pinned to `C:/megadot/Godot_v4.5.1-stable_mono_win64/..._console.exe` (`:29`) |

Room routing (`RlAutoSlayer.cs:116-126`): Monster/Elite/Boss → one shared
`RlCombatHandler`; Event/Shop/Treasure/RestSite → the Rl room handlers.
Event-triggered combat routes through the same bridge combat handler
(`RlNonCombatRoomHandlers.cs:876-891`) — implemented, never live-tested.

### Python side inventory (`sts2_env/bridge/`)

| File | Role |
|---|---|
| `protocol.py` | Constants: port 9002 (`protocol.py:20`), `BridgeStateType` (16 state types, `:31-49`), `BridgeAction` verbs (`:105-113`) |
| `client.py` | `STS2GameClient`: newline-JSON socket, 60 s default receive timeout, auto request_id echo, `play_card/end_turn/choose/choose_many/skip/use_potion/ping` helpers (`client.py:203-256`) |
| `state_adapter.py` | Combat JSON → 131-dim obs (`encode_observation`) + 115-wide mask (`compute_action_mask`) + action decode. Layout: player 4 + powers 6 + hand 10×5 + piles 6 + enemies 5×13 (`state_adapter.py:8-15`) |
| `run_state_adapter.py` | Full-run JSON → 151-dim obs (131 combat + 20 run dims) + 157-wide mask + decode. Adapts TO `run_env.py` semantics, never the reverse (`run_state_adapter.py:4-11`) |
| `agent_runner.py` | Main loop. `detect_model_mode` (`agent_runner.py:132-165`) accepts exactly (115, 131) or (157, 151); combat-only mode uses hand-written heuristics for non-combat phases (`agent_runner.py:337-401`) |

### Which docs to trust (2026-07-24)

| Doc | Verdict |
|---|---|
| `docs/MOD_BUILD_GUIDE.md` | Current and accurate; the build/deploy source of truth |
| `docs/PROTOCOL.md` | Mostly accurate EXCEPT: intent-type list (lines 134-143) is wrong (see Open gaps); `game_over` example shows `result` but the mod sends a `message` field and reserves `result` for `run_complete` (harmless — both are terminal to `agent_runner.py:62-65`) |
| `docs/BRIDGE_REPLAY_HARNESS.md` | Current; its "Current Limitations" list is the honest live-test debt list |
| `docs/AUTOSLAY_BRIDGE.md` | The v2 design rationale actually implemented (Chinese). Read for WHY, not exact APIs |
| `docs/GAME_BRIDGE_REFERENCE.md` | **ABANDONED v1 design** (hand-rolled Harmony hooks on CombatManager/PlayCardAction, ".NET 8"). Never implement from it |
| `docs/AGENT_USAGE_GUIDE.md` | Stale in two places: claims gold/deck/relic/ascension are "not yet fully exposed" (line 319) — false since `RunStateBridgeFields.Apply` (2026-07-23); "default combat environment uses Ironclad" (line 326) — true only for the legacy `scripts/train_combat.py` path, not the Necrobinder campaign |
| `docs/PARITY_GAPS.md` §3 | The live-smoke-test debt is real, but "no dotnet on PATH (2026-05-22)" is stale — the SDK is installed per-user and the mod was built 2026-07-23 |
| `run_state_adapter.py` module docstring lines 40-44 | Stale: claims run-level dims default to 0 on the wire; the C# side has sent them since 2026-07-23 and the adapter code reads them |

## Wire protocol

### Lifecycle

1. Game boots, mod initializes, TCP server listens on 127.0.0.1:9002.
2. `RlAutoSlayer` waits for the main menu, abandons any saved run, and
   auto-starts a Necrobinder A10 run with a random seed (`MainFile.cs:118`).
3. For each decision, the game serializes a typed state, attaches a
   `request_id`, sends it, and awaits one reply line.
4. Python replies with an `action` message echoing the `request_id`.
5. On timeout (30 s) or disconnect: random play for that decision, run
   continues (`PROTOCOL.md:456-457`, `RlCombatHandler.cs:126-127`).

### State types (game → agent, `state["type"]`)

`combat_action`, `card_select`, `map_select`, `reward_screen`, `card_bundle`,
`crystal_sphere`, `card_reward`, `rest_site`, `shop`, `event`, `treasure`,
`boss_relic`, `game_over`, `run_complete`, `pong`, `error`
(`protocol.py:31-49`). `game_over` carries `message`; `run_complete` carries
`result` ∈ {`victory`, `terminated`} (`RlNonCombatRoomHandlers.cs:68-70`).

Legacy residue: `Phase.COMBAT_WAITING` and the `game_state` wrapper exist in
`protocol.py` and are still handled by `agent_runner._phase_for_state`
(`agent_runner.py:466-478`), but `grep` shows `bridge_mod/` never emits either
— the live wire always uses the typed messages above. Don't build on them.

### Action verbs (agent → game, `"action"` field)

| Verb | Payload | Used for |
|---|---|---|
| `play` | `card_index`, `target_index` (-1 = untargeted) | Combat card play |
| `end_turn` | — | Combat |
| `choose` | `index` (or `indexes` list for multi-select) | Every non-combat screen |
| `skip` | — | Skippable choice screens |
| `potion` | `slot`, `target_index` | Combat potion use |
| `ping` | — | Health check, answered by `pong` |

All indices are 0-based into the arrays of the MOST RECENT state message
(`PROTOCOL.md:493`). Dead enemies stay in `enemies` with `is_alive: false`, so
enemy indices are stable for the whole combat (`PROTOCOL.md:501`).

### Run-level fields on every payload

`RunStateBridgeFields.Apply` (`RlNonCombatRoomHandlers.cs:89-155`) merges into
every state: `act` (1-based), `act_index` (0-based), `floor`, `act_floor`,
`gold`, `deck_size`, `relic_count`, `relics`, `num_potions`,
`max_potion_slots`, `potions` (id list; combat keeps its richer per-slot
list), `ascension_level`, `room_type`, `is_elite`, `is_boss`, plus top-level
`hp`/`max_hp` ints. Names deliberately match what `run_state_adapter.py`
reads.

### Protocol invariants — violate these and adapters break silently

1. **Never put a top-level `player` dict in a non-combat payload.**
   `StateAdapter` classifies any state containing `player` as a combat
   observation (`state_adapter.py:117-119`); full-run training zeroes the
   combat block at non-combat phases. Non-combat HP travels as top-level
   `hp`/`max_hp` ints instead (`RlNonCombatRoomHandlers.cs:111-117`,
   `MOD_BUILD_GUIDE.md:251`).
2. **Option-list position IS the action encoding.** There is no id-based
   addressing: `choose(i)` means "the i-th entry of the option/node/card list
   in the last state". Any C# change that reorders a serialized list is a
   sim/real semantics change (see the unverified-shop-ordering gap below).
3. **Enum strings crossing the boundary must be case-normalized.** C#
   `enum.ToString()` yields PascalCase (`"Attack"`); the sim's Python
   `Enum.name` yields UPPER_SNAKE (`"ATTACK"`). The powers path normalizes
   with `.upper()` (`state_adapter.py:361`); the intent path does not and is
   silently broken (Open gap #2). When adding any adapter field: normalize on
   read, and write the test with the REAL C# casing, not the sim casing.
4. **`playable` is authoritative on the C# side.** Sending `play` for an
   unplayable card logs a game-side error and executes nothing
   (`PROTOCOL.md:497`, `RlCombatHandler.cs:189-193`); the loop then
   re-receives the same state — if the Python mask logic disagrees with the
   game about playability, a deterministic policy livelocks (each identical
   re-offer burns the 30 s timeout into random fallback).

### Robustness contract — random-play contamination

If the agent process crashes, stalls > 30 s, or disconnects, the game does
NOT pause: it plays randomly and continues (`RlCombatHandler.cs:41,126-127`;
every screen handler has the same 30 s `AgentTimeout`). Consequence for
evaluation honesty (house rule: only real, experiment-backed results): **any
run during which the agent process hiccupped is invalid data** — discard the
run and its trace. Watch the agent log for reconnect warnings and the game
log for `falling back to random` lines before counting a result.

## Build and deploy runbook

Prereqs (recreating them from scratch is sts2-build-and-env territory): .NET 9
SDK, game installed via Steam, Godot 4.5.1 Mono only if re-exporting the PCK.

**Trap (verified 2026-07-24):** `C:\Program Files\dotnet\dotnet.exe` is on
PATH but has NO SDKs installed — `dotnet build` from it fails with "No .NET
SDKs were found". The working SDK is per-user: `%USERPROFILE%\.dotnet\dotnet.exe`
(9.0.316). Use it explicitly:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent\bridge_mod
& "$env:USERPROFILE\.dotnet\dotnet.exe" build
```

Plain `dotnet build`, Debug config — no `-c Release` is documented or used
anywhere; the deployed DLL is a Debug build (`MOD_BUILD_GUIDE.md:74-77`).
The build automatically:

- compiles `STS2BridgeMod.dll`;
- copies DLL + `STS2BridgeMod.json` + `mod_manifest.json` to
  `<Steam>\steamapps\common\Slay the Spire 2\mods\STS2BridgeMod\` and BaseLib
  (from the NuGet cache) to `mods\BaseLib\` (`STS2BridgeMod.csproj:117-127`);
- if `GodotPath` exists, also runs the PCK export (`:129-134`).

Non-default Steam library: append `-p:SteamLibraryPath="D:/SteamLibrary/steamapps"`.

PCK facts (all verified against `STS2BridgeMod.csproj` + `MOD_BUILD_GUIDE.md:85-86,209-217`):

- The export may report `exited with code -1` with many "no solution file"
  warnings and STILL write a valid PCK (`ContinueOnError="WarnAndContinue"`).
  Verify the file starts with `GDPC`.
- C#-only changes do NOT need a PCK re-export; the old `.pck` keeps working.
- Godot must be exactly 4.5.1 Mono — the game refuses PCKs from newer Godot
  (`STS2BridgeMod.csproj:22`).

### Verify a deployment

Run the preflight script shipped with this skill (read-only; smoke-tested
2026-07-24):

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe .claude\skills\sts2-bridge-and-realgame\scripts\bridge_preflight.py --model-path output\combat_ppo\final_model.zip
```

It checks deployed files + PCK magic + dotnet SDK + godot.log markers +
model/bridge compatibility (add `--ping` while the game is running). Manual
equivalent checklist:

- [ ] `mods\STS2BridgeMod\{STS2BridgeMod.dll, STS2BridgeMod.json, STS2BridgeMod.pck, mod_manifest.json}` and `mods\BaseLib\` exist with fresh timestamps
- [ ] Launch game via Steam; title screen shows "Running Modded"
- [ ] `%APPDATA%\Godot\app_userdata\Slay the Spire 2\logs\godot.log` contains
      `[STS2Bridge] Harmony: 3/3 patches applied.` and
      `[STS2Bridge] TCP server started on port 9002.` (`MOD_BUILD_GUIDE.md:147-160`).
      Any `SKIP: <PatchName>` line means a Harmony patch silently failed — see
      the game-update checklist below
- [ ] TCP ping succeeds (preflight `--ping`, or `MOD_BUILD_GUIDE.md:162-172`)

## Running an agent against the live game

Order matters (KNOWN_ISSUES #12): start the game first, wait for the main
menu (the mod auto-starts a Necrobinder A10 run from there), then:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe -m sts2_env.bridge.agent_runner --model-path output\combat_ppo\final_model.zip --verbose
```

Full flag set (argparse verified, `agent_runner.py:795-863`): `--host`,
`--port 9002`, `--stochastic` (default is deterministic), `--verbose`,
`--log-level`, `--record-replay trace.json`, `--replay-factory mod:fn`,
`--action-delay 1.0` (pause before non-combat decisions, for watching),
`--combat-delay 0.5` (pause before combat actions; end turn stays instant).

Model mode is auto-detected from spaces (`agent_runner.py:132-165`):

| action_space.n | obs dim | Mode |
|---|---|---|
| 115 | 131 | Combat-only: policy drives combat; hand-written heuristics drive map/rewards/shop/rest/event/treasure/boss-relic (`agent_runner.py:337-401`) |
| 157 | 151 | Full-run legacy: `RunStateAdapter` drives every phase |
| anything else | — | `ValueError` at `agent_runner.py:159` — includes ALL rich-obs models (Open gap #1) |

The client reconnects on drops (10 attempts, 3 s delay) and pings on receive
timeouts. Remember the contamination rule: hiccup ⇒ discard the run.

## Open gaps (each verified 2026-07-24 — read before any live work)

1. **Rich models cannot connect at all.** The current Necrobinder campaign
   trains rich observations: stage-A checkpoints in
   `output/necrobinder_a10/A/` are (action 115, obs 4184); the G1+ revamp
   trains (157, 4184) via `rich_run_env`. `detect_model_mode` accepts only
   (115, 131) / (157, 151), and `grep -i rich sts2_env/bridge/` has zero
   matches — nobody has written the rich adapter. Before ANY real-game
   evaluation of a campaign model someone must: (a) implement a
   `RichRunStateAdapter` that builds the 4184-dim layout
   (`sts2_env/gym_env/rich_observation.py` is ground truth) from bridge JSON,
   (b) add a (157, 4184) branch to `detect_model_mode`, (c) audit which rich
   features the wire cannot supply (deck composition is sent as `deck_size` +
   `relics` ids only — the rich obs wants much more) and zero them
   IDENTICALLY in a training-side variant, or accept the measured
   distribution shift. This is deliberate deferred work, not an oversight.
2. **Live intent one-hot is all-zero (silent, unfixed).** C# sends
   `firstIntent.IntentType.ToString()` — PascalCase `"Attack"`, `"Defend"`,
   ... per `decompiled_v0.109.0/MegaCrit.Sts2.Core.MonsterMoves.Intents/IntentType.cs`
   (`RlCombatHandler.cs:574`). The Python lookup table keys are UPPER_SNAKE
   `"ATTACK"`, `"MULTI_ATTACK"`, ... (`state_adapter.py:55-61`) and the
   lookup does no case normalization (`state_adapter.py:196`). So on every
   live observation the 5-slot intent one-hot is zero; only `intent_damage` /
   `intent_hits` populate. Also: `MULTI_ATTACK` can never occur — the C# enum
   has no such member (multi-hit is `AttackIntent.Repeats`, serialized as
   `intent_hits`, `RlCombatHandler.cs:586-587`). And `PROTOCOL.md:134-143`
   documents a THIRD vocabulary (`SingleAttack`/`MultiAttack`) matching
   neither side. Tests only ever feed `"ATTACK"`
   (`tests/test_bridge_state_adapter.py:21`), which is why the suite is
   green. Not yet in KNOWN_ISSUES.md. Fix per invariant 3: normalize with
   `.upper()` at `state_adapter.py:196`, map C# `Attack`→ATTACK with
   `intent_hits>1`→MULTI_ATTACK if you want that slot to fire, update
   PROTOCOL.md, and add a test feeding literal `"Attack"`.
3. **`base_damage`/`base_block` never sent (found by inspection 2026-07-24,
   flagged nowhere).** The C# card serializer sends only
   `id/cost/type/target/playable(+upgraded)` (`RlCombatHandler.cs:512-539`),
   but the adapter reads `card.get("base_damage")` / `card.get("base_block")`
   (`state_adapter.py:161-162`) and the simulator populates those two dims in
   training (`sts2_env/gym_env/observation.py:84-85`). Live hand cards
   therefore always show damage=0/block=0 to the model — a real train/infer
   distribution shift for 131-dim combat models, same family as fixed issue
   KNOWN_ISSUES #15. Fix on the C# side (serialize the card's damage/block)
   or zero the dims in training; either way, both sides must match.
4. **Shop ordering unverified + sell-Foul-Potion action missing**
   (KNOWN_ISSUES #16). `RlShopRoomHandler` orders by
   `room.Inventory.GetAllSlots()` (`RlNonCombatRoomHandlers.cs:511`), whose
   internal order lives in the compiled game; the sim's
   `RunManager._actions_shop()` order (cards, colorless, relics, potions,
   sell-Foul-Potion, removal) could not be confirmed to match. The "sell Foul
   Potion" action has NO bridge representation at all — a full-run policy can
   never take it live. (Foul Potion: a cursed potion the shop lets you sell;
   the sim models the sell action, the bridge doesn't.)
5. **`reward_screen` is a heuristic mapping** (KNOWN_ISSUES #16). The live
   post-combat rewards screen offers card/potion/relic/gold + proceed
   together; `run_env.py` has single-reward-at-a-time semantics. The adapter
   maps it as "first pickable reward vs. proceed"
   (`run_state_adapter.py:233-242`). Never live-verified.
6. **`card_bundle` / `card_select` / `crystal_sphere` take fixed fallbacks.**
   No representation in `run_env.py`'s 157-action space, so the adapter
   always takes a deterministic, model-independent action
   (`run_state_adapter.py:243-246`, module docstring). The policy cannot
   express preferences on these screens.
7. **`player_select` slice (actions 150-156) is always masked out** —
   resolved as a NON-issue for solo play 2026-07-23: `select_player` only
   fires in multiplayer co-op; Necrobinder's Osty is an ally *monster* (a
   summoned pet with a self-looping `NOTHING_MOVE`), not a controllable
   player creature (KNOWN_ISSUES #16, verified against
   `decompiled_v0.109.0/.../Osty.cs`). Do not "fix" this.
8. **KNOWN_ISSUES #16's hp-ratio "remaining gap" sentence is stale.** It says
   `_hp_ratio()` only reads a `player` dict; the code now also reads the
   top-level `hp`/`max_hp` ints (`run_state_adapter.py:496-501`), so the
   suggested Python-side fix has landed. Update the ledger entry when
   touching it (ledger discipline: sts2-docs-and-writing).
9. **The live smoke test itself** — see the checklist below. Until it passes,
   treat the entire bridge as "implemented and Python-tested, not
   field-verified" (`PARITY_GAPS.md:253-255`).

## Harmony game-update checklist

Harmony binds injected prefix parameters BY NAME. When the game renames a
parameter, the patch fails to apply **silently** — logged as
`SKIP: <PatchName>` (`MainFile.cs:53-56`), never thrown. This actually
happened: `AnimationSpeedPatch` broke when `SetTimeScale`'s parameter was
renamed `timeScale` → `scale` (fixed for v0.109.0; remarks at
`MainFile.cs:158-163`, `MOD_BUILD_GUIDE.md:241`). After EVERY game update:

- [ ] Re-decompile the new game version (workflow: sts2-build-and-env) and
      diff the exact signatures of all three patched methods:
      `NGame.IsReleaseGame()`, `Cmd.CustomScaledWait(float fastSeconds,
      float standardSeconds)`, `MegaAnimationState.SetTimeScale(float scale)`
- [ ] Rename patch parameters in `MainFile.cs` to match, rebuild, redeploy
- [ ] Launch and confirm `Harmony: 3/3 patches applied.` — a `2/3` with a
      `SKIP` line means the game still runs but slower or with AutoSlay
      locked (if `IsReleaseGamePatch` is the casualty, nothing works at all)
- [ ] Re-check the AutoSlay APIs the mod calls (`RlAutoSlayer` compiles
      against `sts2.dll`, so removed/renamed game APIs surface as build
      errors — a build failure after an update is EXPECTED triage, not a
      mystery)
- [ ] Re-run the 73 bridge tests, then the full suite per sts2-change-control

The three patches and their effects (`MOD_BUILD_GUIDE.md:233-239`):
`IsReleaseGamePatch` (forces false — unlocks AutoSlay; the load-bearing one),
`WaitSpeedPatch` (all timed waits ×0.1), `AnimationSpeedPatch` (Spine
animations ×5). Net ~5-10× speedup.

## Replay recording and golden comparison

Record while running live:

```powershell
.venv\Scripts\python.exe -m sts2_env.bridge.agent_runner --model-path output\combat_ppo\final_model.zip --record-replay artifacts\run_trace.json --replay-factory my_module:make_run_manager
```

Inspect and compare against a deterministic simulator reconstruction:

```powershell
.venv\Scripts\python.exe -m sts2_env.parity.bridge_replay_cli show artifacts\run_trace.json
.venv\Scripts\python.exe -m sts2_env.parity.bridge_replay_cli compare artifacts\run_trace.json --mode run --factory my_module:make_run_manager
```

- The `--factory` (`module:function`) must deterministically recreate the
  IDENTICAL combat or run (seed-pinned); `--factory-kw key=value` passes
  kwargs; `--mode` is `combat` or `run` (`bridge_replay_cli.py:82-115`).
- Combat comparison checks player/hand/enemies/piles/round; choice screens
  check option order / action / enabled, deliberately ignoring localized
  labels (`BRIDGE_REPLAY_HARNESS.md:197-243`).
- There are NO committed replay fixture JSONs anywhere in the repo — tests
  build traces in-memory (`tests/test_bridge_replay_harness.py`); live traces
  go wherever `--record-replay` points (convention: `artifacts\`).
- **Known comparison landmine:** the sim-side serializer emits
  `intent_type.name` — UPPER_SNAKE (`bridge_replay.py:476`) — while the live
  wire sends PascalCase (Open gap #2), so live-vs-sim comparison currently
  mismatches on `intent` until that bug is fixed. The sim serializer also
  emits `base_damage`/`base_block` (`bridge_replay.py:503-504`) which the
  live wire lacks (Open gap #3).

## The live smoke test (still undone — the single biggest bridge debt)

Nobody has ever run game + mod + agent through a complete run. Specifically
untested live (`BRIDGE_REPLAY_HARNESS.md:244-253`, `PARITY_GAPS.md` §3):
event-triggered combat routing, `RlCardSelector` interception of deck
upgrade/transform/enchant/choose-a-card flows, full-run lifecycle replay
comparison, and the `reward_screen` heuristic. The AutoSlay coverage test
(`tests/test_bridge_autoslay_coverage.py`) only regex-matches the C# SOURCE
of `RlAutoSlayer.cs` — it proves handler registration in text, not runtime
reachability.

Runbook for whoever finally does it:

1. Preflight: run `bridge_preflight.py --model-path <m.zip>`; all PASS.
2. Launch STS2 via Steam. Confirm "Running Modded" + the godot.log markers.
   Expect the mod to abandon any saved run and sit ready at/after the menu.
3. Start the agent with a COMPATIBLE model (115/131 or 157/151 — a rich
   checkpoint will die at load, Open gap #1), `--verbose`,
   `--record-replay artifacts\smoke_trace.json`, and generous delays
   (`--action-delay 1.0 --combat-delay 0.5`) so you can watch.
4. Watch the first combat: verify in the agent log that hand contents, energy
   and enemy HP track the screen; verify energy DECREASES when cards are
   played (the historical AutoPlay energy bug, KNOWN_ISSUES #1, shows up
   here if ever regressed).
5. Let the run reach: an event (does event-triggered combat hand control to
   the combat handler and back?), a shop (does each `choose(i)` buy the item
   you expected? — Open gap #4), a rest-site upgrade (does `RlCardSelector`
   intercept the card-select screen instead of the default UI?), a
   post-combat rewards screen (does the pick/proceed heuristic act sanely? —
   Open gap #5), and a boss relic choice.
6. Run to `game_over`/`run_complete`. No timeouts/reconnects allowed for the
   trace to count (contamination rule).
7. `bridge_replay_cli show` the trace; attempt `compare` with a seed-matched
   factory; file every mismatch (expect `intent` and `base_damage`/`base_block`
   mismatches until gaps #2/#3 are fixed).
8. Write results into `docs/KNOWN_ISSUES.md` (update #16) and
   `docs/PARITY_GAPS.md` §3, then route fixes via sts2-change-control.

Do not claim any live win-rate from smoke runs: evaluation protocol and
sample-size rules (1000-episode Wilson CI etc.) are owned by
sts2-testing-and-qa / sts2-analysis-toolkit.

## Changing the bridge safely

- Classify changes per sts2-change-control: C# mod changes and Python adapter
  changes are separate classes; adapter changes also require the FULL test
  suite (`.venv\Scripts\python.exe -m pytest tests/ -q`, 5,276 tests at
  HEAD), not just the 73 bridge tests.
- Bridge test files: `test_bridge_state_adapter.py`,
  `test_bridge_run_state_adapter.py`, `test_bridge_replay_harness.py`,
  `test_bridge_autoslay_coverage.py`, `test_bridge_client_protocol.py`
  (73 collected in ~0.5 s).
- When adding a wire field: add it in C# via the shared serializers
  (`RunStateBridgeFields.Apply` for run-level fields), read it defensively in
  Python (`_first_int`-style, tolerate absence), respect invariants 1-3, feed
  the REAL C# casing in the new test, and update `docs/PROTOCOL.md` in the
  same change.
- Adapters adapt TO the frozen training layouts (`observation.py`,
  `run_env.py`, `rich_observation.py`) — never bend a training layout to suit
  the wire; existing checkpoints depend on it (`run_state_adapter.py:9-11`).
- The combat action space (115) is a prefix of the run action space (157) —
  weight-transfer invariant owned by sts2-architecture-contract; the bridge
  relies on it in `RunStateAdapter.compute_action_mask`
  (`run_state_adapter.py:173-176`).

## Provenance and maintenance

Facts here were verified 2026-07-24 against HEAD `fe25668` plus the deployed
artifacts on this machine. Volatile facts and their one-line re-checks (run
from the repo root; PowerShell):

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| HEAD is `fe25668`; bridge files last touched by `0078f70` | `git log --oneline -1; git log --oneline -1 -- sts2_env/bridge bridge_mod` |
| 73 bridge tests collect | `.venv\Scripts\python.exe -m pytest --collect-only -q tests/test_bridge_state_adapter.py tests/test_bridge_run_state_adapter.py tests/test_bridge_replay_harness.py tests/test_bridge_autoslay_coverage.py tests/test_bridge_client_protocol.py` |
| Space sizes 115/131, 157/151, rich 4184 | `.venv\Scripts\python.exe -c "from sts2_env.core.constants import ACTION_SPACE_SIZE; from sts2_env.gym_env.observation import OBS_SIZE; from sts2_env.gym_env.run_env import RUN_OBS_SIZE, TOTAL_ACTIONS; from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; print(ACTION_SPACE_SIZE, OBS_SIZE, TOTAL_ACTIONS, RUN_OBS_SIZE, RICH_OBS_SIZE)"` |
| No rich adapter in the bridge yet | `Select-String -Path sts2_env\bridge\*.py -Pattern "rich" -SimpleMatch` (expect no output) |
| Intent-casing bug still present | `Select-String -Path sts2_env\bridge\state_adapter.py -Pattern "intent_str" -Context 1` (bug present while `_INTENT_STR_TO_IDX.get(intent_str)` has no `.upper()`) |
| `base_damage` still absent from C# serializer | `Select-String -Path bridge_mod\RlCombatHandler.cs -Pattern "base_damage"` (bug present while no output) |
| Mod deployed and current | `.venv\Scripts\python.exe .claude\skills\sts2-bridge-and-realgame\scripts\bridge_preflight.py` |
| Installed game still v0.109.0 | `Get-Content "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\release_info.json"` |
| Working dotnet SDK is per-user | `& "$env:USERPROFILE\.dotnet\dotnet.exe" --list-sdks` |
| Live smoke test still undone | `Test-Path "$env:APPDATA\Godot\app_userdata\Slay the Spire 2"` (False ⇒ modded game never launched here; if True, read the log and update the Status snapshot) |
| Hardcoded run config (Necrobinder, A10, floor 49, 60 min) | `Select-String -Path bridge_mod\RlAutoSlayer.cs -Pattern "Preferred|FinalRunFloor|RunTimeoutMinutes"` |
| KNOWN_ISSUES #16 / PARITY_GAPS §3 wording | re-read `docs/KNOWN_ISSUES.md` and `docs/PARITY_GAPS.md` before citing them; both contain sentences already stale at this writing (flagged above) |

If any re-check disagrees with this skill, the repo wins — update this file
and date-stamp the change.
