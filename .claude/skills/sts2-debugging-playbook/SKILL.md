---
name: sts2-debugging-playbook
description: >
  Load this skill when something in sts2-rl-agent is BROKEN or behaving
  strangely and you need to diagnose it: a test fails, training stalls or
  plateaus, win rates look wrong, the simulator crashes or diverges from the
  real game, the bridge mod won't load, the live agent misbehaves, an import
  blows up, or logs show suspicious numbers. It gives symptom→cause→fix triage
  tables for the three layers (simulator, training pipeline, real-game
  bridge), discriminating experiments to isolate the faulty layer, and the
  historical traps that cost real time. Do NOT load this for: landing/gating a
  fix (sts2-change-control), the full incident chronicle with evidence trails
  (sts2-failure-archaeology), parity-audit methodology and RNG porting rules
  (sts2-parity-discipline), launching/operating training runs
  (sts2-run-and-operate), choosing WHICH training experiment to run next
  (sts2-training-campaign), bridge build/deploy/protocol reference
  (sts2-bridge-and-realgame), env/venv reconstruction (sts2-build-and-env),
  or log-statistics methodology (sts2-analysis-toolkit). This skill owns
  diagnosis only.
---

# STS2 Debugging Playbook

You are debugging **sts2-rl-agent**: a from-scratch Python headless simulator
of Slay the Spire 2 (beta v0.109.0) verified against decompiled C#, plus
Gymnasium RL envs, a MaskablePPO curriculum trainer, and a C# bridge mod that
drives the real game over TCP. Failures can originate in any of the three
layers — or in your measurement of them. This playbook gets you from symptom
to root-cause layer fast, then to the specific known failure mode.

Diagnosis ends where landing a fix begins: any code change you make as a
result of debugging is gated by **sts2-change-control** (sim behavior changes
require the FULL 5,276-test suite plus the four parity audit scripts green —
no spot-check sign-offs). Do not re-litigate incidents already settled in
**sts2-failure-archaeology**; this file carries only the compressed versions
you need while triaging.

All commands below are run from the repo root `C:\Users\motqu\GitHub\sts2-rl-agent`
in PowerShell. Always use `.venv\Scripts\python.exe`, never bare `python`.
Never run `uv sync` or reinstall over `.venv` — the lock file is stale and
CPU-only; the working env is Store Python 3.13 + uv-installed torch
2.11.0+cu128 (see sts2-build-and-env).

---

## 0. Live-state warning (as of 2026-07-24)

This repo is under active concurrent development. Before ANY debugging:

```powershell
git log -1 --oneline
git status --short
Get-Content output\necrobinder_g1_campaign.log -Tail 5
```

State observed 2026-07-24 (re-verify — it WILL drift):

- HEAD = `fe25668` "Phase 0 training revamp: full-run-only ladder (G1-G5),
  never-halt curriculum, live optimizer". The pre-revamp stage A–F combat
  curriculum is GONE from `scripts/train_necrobinder.py`; anything describing
  stages "A"–"F", win-rate-annealed shaping, or linear LR decay is historical.
- A **G1 training run appears live** (`output/necrobinder_g1_campaign.log`
  ended mid-eval at 100k steps when checked). Never launch a second training
  run while one is running — the RTX 4060 Laptop GPU has 8 GB and one run
  saturates it. Check the log tail and GPU before launching anything.
- Uncommitted working-tree changes existed in `sts2_env/content/__init__.py`,
  `sts2_env/content/descriptions.py`, `sts2_env/web/play_run.py`, plus
  untracked `sts2_env/content/preview.py` — a concurrent session's
  work-in-progress. Do not describe, "fix", or commit someone else's diff;
  if uncommitted changes touch the file you're debugging, `git stash list` /
  ask before concluding anything about committed behavior.

---

## 1. Glossary (terms used in this file)

| Term | Meaning |
|---|---|
| Necrobinder | The STS2 character the campaign trains. Souls resource, Osty pet. HP 66. |
| Souls | Necrobinder's secondary resource (tracked per zone in the rich obs). |
| Osty | Necrobinder's pet. In the real game it is an ally **monster** with a self-looping do-nothing move (`NOTHING_MOVE`), not a player creature — the player never selects it as an actor in solo play. |
| AFTP / Act4Heart | The two Steam Workshop mods active in the campaign config: ActsFromThePast (alternate legacy acts per act slot) and Act4Heart (keys + Act 4 Corrupt Heart). The sim implements both. |
| parity | Bit/behavior-exact agreement between the Python sim and the decompiled C# game source (`decompiled_v0.109.0/` = current ground truth). See sts2-parity-discipline. |
| pending_choice | `CombatState.pending_choice` — the resumable-pause mechanism for mid-combat card-selection prompts (e.g. Exhume). While set, combat action indices 1..N are reinterpreted as choice options. |
| rich obs | The 4184-dim float32 observation (`rich_observation.py`, `RICH_OBS_VERSION=1`) used by the current campaign. Legacy obs: 131-dim combat, 151-dim run. |
| shaping | Potential-style reward bonuses (floor/act/HP-retention) scaled by a constant `shaping_scale`; terminal ±1 never scaled. Eval always uses `shaping_scale=0`. |
| sim_error | `info["sim_error"]=True` — a run episode force-ended because the simulator itself raised, not because the agent died. Scored 0.0, not −1. |
| bridge | The TCP loop (newline-delimited JSON, port 9002) between `sts2_env/bridge/` (Python) and `bridge_mod/` (C# Godot mod) driving the real game. |
| Harmony | The C# runtime-patching library the mod uses. Binds patch parameters **by name** — a game-side rename silently un-applies a patch. |
| AutoSlay | The game's shipped self-play automation framework; the mod unlocks it and substitutes RL handlers for its decisions. |
| golden replay | Recorded live bridge trace compared field-by-field against a deterministic sim re-run (`sts2_env/parity/bridge_replay.py`). |
| Wilson CI | The 95% binomial confidence interval required on any win-rate claim (≥1000 episodes for final claims). See sts2-analysis-toolkit. |

---

## 2. First five minutes: universal triage checklist

Run these in order before forming any hypothesis:

1. **Confirm repo state** — `git log -1 --oneline; git status --short`.
   Note anything uncommitted; separate "committed behavior" from "in-flight".
2. **Confirm the suite still collects** (fast, ~1.5 s, catches import
   breakage across the whole package):
   ```powershell
   .venv\Scripts\python.exe -m pytest tests --collect-only -q
   ```
   Expected: `5276 tests collected` (count as of 2026-07-24; it grows).
   A collection ERROR here = import-time failure; go to §4 row "ImportError".
3. **Confirm which code generation you are in.** Two generations coexist:

   | | obs dims | actions | default char | status |
   |---|---|---|---|---|
   | Legacy combat `STS2CombatEnv` | 131 | Discrete(115) | Ironclad A0 | maintained, not the campaign |
   | Legacy run `STS2RunEnv` | 151 | Discrete(157) | Ironclad A0 | base class, bridge-aligned |
   | Rich run `RichSTS2RunEnv` | 4184 | Discrete(157) | **Necrobinder A10** | the campaign env |

   Copy-pasting between generations silently changes character, ascension,
   and observation. Verify constants live:
   ```powershell
   .venv\Scripts\python.exe -c "from sts2_env.gym_env.observation import OBS_SIZE; from sts2_env.gym_env.run_env import RUN_OBS_SIZE, TOTAL_ACTIONS; from sts2_env.gym_env.rich_observation import RICH_OBS_SIZE; from sts2_env.core.constants import ACTION_SPACE_SIZE; print(OBS_SIZE, RUN_OBS_SIZE, TOTAL_ACTIONS, RICH_OBS_SIZE, ACTION_SPACE_SIZE)"
   ```
   Expected: `131 151 157 4184 115`.
4. **Check the incident ledger** — `docs/KNOWN_ISSUES.md`. If your symptom
   matches an entry, read its status before doing anything (statuses:
   Fixed / Open / verified-from-source-only / documented-not-fixed).
5. **Check for a live training run** (§0) before running anything that
   touches the GPU or `output/`.

---

## 3. Which layer is broken?

Route by where the symptom first appears:

| Symptom class | Start at |
|---|---|
| pytest failure, sim exception, wrong card/relic/monster behavior, RNG divergence between two sim runs | §4 Simulator |
| Training hangs, plateaus, crashes, weird SB3 log numbers, resume does nothing, eval numbers don't match training reward | §5 Training |
| Game won't load the mod, agent can't connect, live agent plays garbage, live behavior ≠ sim behavior | §6 Bridge |
| A win-rate number "changed" between two measurements with different protocols | Not a bug until proven: check episodes/seeds/deterministic/shaping first (sts2-analysis-toolkit). 200-episode evals have ~±3.4 pp 1-sigma noise at 62%. |

Discriminating principle: **the sim is testable in isolation; the bridge is
not.** If a behavior is wrong live, first reproduce it in the sim (CLI or a
seeded test). If the sim is correct and live is wrong, it's a bridge/adapter
bug; if the sim is also wrong, it's a parity bug — go to
sts2-parity-discipline for the audit recipe.

---

## 4. Simulator failure modes

### 4.1 Symptom → cause → fix

| Symptom | Likely cause | Diagnosis / fix |
|---|---|---|
| `pytest --collect-only` errors with ImportError / circular import | A registry or module builds content eagerly at import time, re-forming the cycle `map.acts → events.* → run → map → map.acts` | The guard is lazy registration behind a re-entrancy guard (`sts2_env/map/acts.py`) plus the fresh-interpreter regression test `tests/test_import_order_no_cycle.py`. Reproduce: `.venv\Scripts\python.exe -c "import sts2_env.events; import sts2_env.map.acts"`. Never add module-level registry construction. |
| Training win rate depressed; runs "die" with no obvious combat cause | Simulator exception force-ended episodes as losses | `STS2RunEnv.step()` catches all dispatch exceptions, logs them, tags `info["sim_error"]=True`, scores 0.0 (`run_env.py:315-365`). Grep every training log: `Select-String -Path output\*.log -Pattern "STS2RunEnv.step failed during phase"`. Any hit = a real sim bug to reproduce and fix; the episodes are already excluded from death scoring (since fe25668), but the bug itself remains. |
| A combat never ends in the run env; episode scored as a death | `max_combat_turns` (default 200) exceeded → the env sets player HP to 0 and calls `lose_run()` (`run_env.py:531-537`) | This IS scored as a real death. If an agent shows a spike of deaths with very long combats, suspect a degenerate loop the sim allows (see the Dazed trap, §7.3, for the live-game analogue). Reproduce interactively (§4.2). |
| A card/relic/power does the wrong thing | Parity gap vs decompiled C# | Reproduce with a seeded test, then follow the parity-test recipe in sts2-parity-discipline (cite the `decompiled_v0.109.0/` file in the test docstring). Check `docs/KNOWN_ISSUES.md` #11 first — some effects are "audited-not-proven-exact" (e.g. Alchemize, BeatDown, HandOfGreed, Compact, WhiteNoise, TheHunt). |
| Two runs from the same seed diverge | RNG stream misrouting or iteration-order nondeterminism | The sim mirrors the game's per-named-stream seeded RNG (`sts2_env/core/rng.py`: `next_int` is INCLUSIVE on both ends; `next_int_exclusive` exists separately). Divergence usually means something consumed from the wrong stream or iterated an unordered collection. Ownership: sts2-parity-discipline (porting rules), sts2-architecture-contract (two RNG roots + named-streams contract). |
| Agent's chosen action seems ignored in a gym env | Invalid actions are ignored SILENTLY at debug level (`rich_combat_env.py:324-328`); in the run env an unsuccessful `play_card` falls back to `end_turn` (`run_env.py:513-526`) | This is by design — masking is what keeps the agent honest. If you see it during a manual rollout, your loop isn't passing `action_masks` to `model.predict()` (§5.1). |
| Combat action indices "mean something else" mid-combat | `pending_choice` is set: indices 1..N are choice options, not hand cards | Never interpret an action id without its mask context. `get_action_mask` also folds ANY_ALLY targets into the enemy-target index range. See sts2-architecture-contract for the layout contract. |

### 4.2 Reproducing sim behavior by hand

Interactive CLIs (best first move for "the sim did something weird"):

```powershell
# Single combat, interactive:
.venv\Scripts\python.exe scripts\play_interactive.py
# Full run, interactive CLI:
.venv\Scripts\python.exe scripts\play_run_interactive.py
# Full run, local web UI (NOTE 2026-07-24: sts2_env/web/play_run.py had
# uncommitted local edits - prefer the CLI for committed-behavior repro):
.venv\Scripts\python.exe scripts\play_run_web.py
```

Targeted tests (131 test files; name-match your subject):

```powershell
.venv\Scripts\python.exe -m pytest tests -q -k "necrobinder"
.venv\Scripts\python.exe -m pytest tests\test_run_env.py -q -x
```

### 4.3 The four parity audit scripts

These are structural sweeps — they find *missing coverage leads*, not proofs
of correctness. All four must be green before any sim-behavior fix lands
(sts2-change-control owns that gate). Verified invocations (2026-07-24):

```powershell
.venv\Scripts\python.exe scripts\audit_card_static_metadata.py    # prints "card static metadata audit passed"
.venv\Scripts\python.exe scripts\audit_card_dynamic_vars.py       # prints "card dynamic var audit passed"
.venv\Scripts\python.exe scripts\audit_card_effect_vars.py        # prints "card effect var audit passed"
.venv\Scripts\python.exe scripts\parity_reference_audit.py --show-missing   # decompiled-class coverage scan; flags: --surface cards|powers|relics|... --json
```

Deliberate deviations from the game belong in the PATCHED allowlists inside
the audit scripts — procedure in sts2-parity-discipline.

---

## 5. Training failure modes

The production trainer is `scripts/train_necrobinder.py` — a full-run-only
ladder G1→G5 (as of fe25668, 2026-07-24): every stage trains `RichSTS2RunEnv`
(Necrobinder), difficulty moves only along ascension × act-count, promotion
thresholds are telemetry (the ladder NEVER halts), constant lr=2e-4 with
`target_kl=0.03`, constant `ent_coef=0.01`, `n_steps=1024`, batch 4096,
n_epochs 3, gamma 0.997, default `--n-envs 24`, eval every 100k steps,
checkpoints every 250k to `output/necrobinder_run/<stage>/` (default; live G1
run used `--output-dir output/necrobinder_g1`). Operating it is
sts2-run-and-operate's topic; which experiment to run is
sts2-training-campaign's. This section is what to do when it misbehaves.

### 5.1 Symptom → cause → fix

| Symptom | Likely cause | Diagnosis / fix |
|---|---|---|
| `--resume` starts, prints, then exits almost immediately having trained ~0 steps | `--total-steps` is an ABSOLUTE timestep target, not an increment. Resume keeps `num_timesteps` (`reset_num_timesteps=False`) and `model.learn(total_timesteps=...)` returns once the target is met | Pass a larger `--total-steps` than the checkpoint's step count (check the latest `ckpt_*.json` sidecar in the stage dir: `Get-Content (Get-ChildItem output\necrobinder_g1\G1\ckpt_*.json | Sort-Object Name | Select-Object -Last 1) | ConvertFrom-Json | Select-Object steps,best_win_rate` — sidecars appear from the first 250k-step checkpoint onward). |
| Logs show `approx_kl` ~1e-8, `clip_fraction` 0, tiny `learning_rate` | The optimizer is frozen. Under the CURRENT trainer (constant lr) this should not happen from schedule alone — the classic cause is resuming a **pre-revamp checkpoint**: `MaskablePPO.load()` restores the pickled linear-anneal schedule from the old stage-A era, silently reintroducing lr→0 | Compare `learning_rate` in the SB3 log block to 2e-4. Healthy reference (live G1 @ ~98k steps, 2026-07-24): `approx_kl 0.0076, clip_fraction 0.044, learning_rate 0.0002, entropy_loss -1.13, explained_variance 0.79, fps ~1192`. Frozen reference (stage-A postmortem): `approx_kl 8.0e-09, clip_fraction 0, learning_rate 1.44e-07`. Do NOT warm-resume old `output/necrobinder_a10/` checkpoints into the new ladder without checking; details in sts2-failure-archaeology. |
| Win rate flat from the very first eval | Either (a) the agent cannot SEE the decision-relevant state, or (b) it is already near the task's random baseline | (a) is the settled 0%-full-run postmortem lesson (§7.5): before blaming the algorithm, verify the obs encodes what the decision needs (segment tests, sts2-analysis-toolkit). (b): measure a random baseline under the SAME protocol before calling any number "learning" — `scripts/benchmark.py` plays random masked actions on the legacy combat env and prints a win rate; for the rich run env write the equivalent loop (sts2-analysis-toolkit). Historical anchor: stage A plateaued 60.5%→63.5% over 5M steps while random Ironclad Act-1 combat was ~63% — most of that "skill" was baseline. |
| Training "hangs" periodically | In-process eval: `CurriculumCallback._do_eval` runs the full eval loop on the training thread (200 episodes default; the live G1 run uses 500). Historical worse case: unmasked eval episodes ran tens of thousands of steps before the TimeLimit fix (§7.2) | Check the log for `[eval] stage ...` lines; eval wall time is printed. Shrinking `--eval-freq` multiplies this cost. If a manual eval loop hangs, you forgot the step cap or the mask. |
| Manual eval picks invalid actions / scores absurdly | `model.predict()` called without masks | Always: `action, _ = model.predict(obs, action_masks=env.action_masks(), deterministic=True)`. The envs ignore invalid actions silently (§4.1), so an unmasked policy quietly burns steps. |
| `AttributeError` on a wrapped env (e.g. `action_masks` missing) | Gymnasium wrappers don't forward custom attributes | Use `env.unwrapped.action_masks()` (pattern in `scripts/train_combat.py:62-64`). |
| SubprocVecEnv crashes at startup on Windows (pickling error) | Env factory is a closure/lambda | Factories must be module-level functions bound with `functools.partial` (`train_necrobinder.py:82-97`, `make_vec_env`). Windows uses spawn; everything sent to workers must pickle. |
| Disk fills during a stage | Rich-policy checkpoints are ~103 MB each, every 250k steps; a 60M-step G5 budget writes ~24 GB of ckpts | Budget/prune per sts2-run-and-operate. Don't delete `best_model.zip` or sidecar JSONs — sidecars carry resume state (eval history, promotion streak). |
| Eval win rate ≠ training episode reward trends | Training reward includes shaping (`shaping_scale=1.0`: floor 0.004/floor, act 0.25, HP-retention 0.05); eval uses `shaping_scale=0` (pure ±1) by design | Not a bug. Only shaping-off numbers are reportable (house rule). If someone quotes a shaped-env number as a win rate, reject it. |
| Truncated episodes counted as deaths | Depends on the env: `RichSTS2RunEnv` scores truncation `cfg.truncation = 0.0` and tags `info["truncated"]` (fe25668); the BASE `STS2RunEnv` still scores truncation as `REWARD_DEATH` (`run_env.py:358-359`) | If you build eval on the base env or an old checkpoint's env, truncation==death returns. `run_eval` in the trainer reports `truncation_rate` — a rising value with flat win rate means the step cap (`RUN_MAX_STEPS=3000`) is binding, not that the agent is dying. |
| `mean_floors=0.0`, `deaths_by_act={'0': N}` in an old eval_history.jsonl | Stage-A-era combat-env evals never set `info['floor']/'act'` | Historical artifact only; the current run-env eval records real `floor`/`act`/`mean_act`. Don't diagnose old combat-stage JSONLs by those fields — only `win_rate` was meaningful there. |
| You want to reproduce one eval episode exactly | Eval seeds are deterministic: episode `i` uses seed `10_000_000 + i` (`EVAL_SEED_BLOCK`, `train_necrobinder.py:71`, `run_eval`) | Rebuild the env via `make_stage_env(stage, shaping_scale=0.0)` and `env.reset(seed=10_000_000 + i)` with `deterministic=True` prediction. |

### 5.2 Dead scripts — do not debug them into life

- `scripts/train_full_run.py` is **verified broken** (2026-07-24): it passes
  `act_count=`/`reward_shaping=` kwargs that `STS2RunEnv.__init__`
  (`run_env.py:255-262`: `character_id, ascension_level, max_steps,
  max_combat_turns, render_mode`) no longer accepts → instant `TypeError`.
  Superseded by `train_necrobinder.py`. If a runbook tells you to use it, the
  runbook is stale (`docs/TRAINING_GUIDE.md` is stale throughout — wrong GPU,
  wrong action-space size, broken commands; see sts2-docs-and-writing).
- `scripts/train_combat.py` still works but targets the LEGACY 131-dim
  Ironclad env — it is not the campaign and its results don't transfer.
- `RichSTS2CombatEnv` still exists in the package but is no longer used by
  the trainer (fe25668 removed the combat-only stages).

---

## 6. Bridge failure modes

Reference material (protocol, build, deploy, adapters) lives in
sts2-bridge-and-realgame; this is the triage view. Big picture: the C# mod
(`bridge_mod/`) unlocks the game's AutoSlay framework via a Harmony patch,
substitutes RL handlers, and serves newline-delimited JSON on TCP
127.0.0.1:9002; Python (`sts2_env/bridge/agent_runner.py`) drives a trained
model against it. **As of 2026-07-24 the full live loop has never been
smoke-tested end-to-end** — treat every live-only behavior as unverified.

### 6.1 Symptom → cause → fix

| Symptom | Likely cause | Diagnosis / fix |
|---|---|---|
| Game hangs at "Loading assembly DLL" | Mod built with `Microsoft.NET.Sdk` instead of `Godot.NET.Sdk/4.5.1` | Rebuild from the Alchyr template (docs/TROUBLESHOOTING.md "Mod Loading"). |
| "Pack version unsupported: 1" | Hand-made or wrong-Godot `.pck`; the game needs PCK format v3 from `dotnet publish` via Godot 4.5.1 Mono exactly | See docs/MOD_BUILD_GUIDE.md; note the export can report exit code −1 and still write a valid file (check `GDPC` magic bytes). |
| "did not supply a mod manifest" | `mod_manifest.json` not INSIDE the .pck | Keep it in the project root next to `project.godot`. |
| Game crashes/hangs on first "Load Mods" click | Normal — first consent requires a game restart | Restart; subsequent launches load mods silently. |
| Mod deployed but nothing happens | Wrong deploy dir or patches skipped | Check `%APPDATA%\Godot\app_userdata\Slay the Spire 2\logs\godot.log` for the two markers: `[STS2Bridge] Harmony: 3/3 patches applied.` and `[STS2Bridge] TCP server started on port 9002.` Deploy dir: `<Steam>\steamapps\common\Slay the Spire 2\mods\STS2BridgeMod\` (+ `mods\BaseLib\`). Title screen must show "Running Modded". |
| `ConnectionRefusedError` on port 9002 | Game takes 30+ s to start the TCP server | `STS2GameClient(reconnect_attempts=30, reconnect_delay=2.0)`. Start game first, wait for main menu, then start the agent. |
| Agent connects; all observation values are 0 | v1-vs-v2 protocol drift: v2 sends flat state (no `combat_state` wrapper) | Adapter pattern `combat = state.get("combat_state") or state` already handles this; if you see zeros, something regressed or a NEW payload shape appeared — dump one raw message with `--verbose --log-level DEBUG`. |
| Agent only ever sends END_TURN | Mask computed from a field that isn't on the wire (historically `phase`) | Same flat-state pattern in `compute_action_mask`. |
| Game-side C# exception "The given key was not present in the dictionary" on actions | Sending v1 `{"type": "PLAY"}` instead of v2 `{"action": "play"}` | Use `STS2GameClient` helper methods. |
| Energy stuck at 3; unlimited card plays | `CardCmd.AutoPlay()` — a debug API that never spends energy | The fix is `new PlayCardAction(card, target)` enqueued via `RunManager.Instance.ActionQueueSynchronizer` (`bridge_mod/RlCombatHandler.cs` ~line 187; KNOWN_ISSUES #1). Never reintroduce AutoPlay. |
| "Room type not assigned" timeout after character select | Game API called off the Godot main thread (`Task.Run`) | Use `TaskHelper.RunSafely(...)` or `Callable.From(...).CallDeferred()`. All game APIs are main-thread-only. |
| A Harmony patch silently stops working after a game update | Harmony binds prefix parameters BY NAME; a game-side parameter rename un-applies the patch with only a `SKIP:` log line, no error | After EVERY game update, diff the three patched signatures (`NGame.IsReleaseGame`, `Cmd.CustomScaledWait`, `MegaAnimationState.SetTimeScale`) against fresh decompiled source. Precedent: v0.109.0 renamed `timeScale`→`scale` (KNOWN_ISSUES #6). |
| Live agent replays the same unplayable card forever | Action mask ignored a server-side validity flag | The per-card `playable` flag is authoritative and IS honored (`state_adapter.py:268`); the historical Dazed livelock (§7.3) is the incident. General rule: the game rejects invalid plays and re-sends the same state — any Python/C# mask disagreement is a potential livelock. |
| Live run's decisions look scrambled / nonsensical outside combat | Option-list ORDERING mismatch: bridge option position IS the action encoding; the C# handler must enumerate exactly as `RunManager.get_available_actions()` would | One ordering bug is already fixed (map nodes must come from `MapPoint.Children` insertion order — KNOWN_ISSUES #5); **shop ordering remains unverified** and "sell Foul Potion" has no bridge representation at all (KNOWN_ISSUES #16). |
| Recorded live run has bizarre stretches of random-looking play | On agent timeout (30 s) or disconnect, the game does NOT pause — it plays RANDOMLY and continues | Any run where the agent process hiccupped is invalid data. Check agent logs for gaps before trusting a trace or win-rate. |
| `ValueError: Unrecognized model action/observation space` at agent start | `detect_model_mode` (`agent_runner.py:132-165`) accepts ONLY (115 act / 131 obs) or (157 act / 151 obs). **Rich 4184-dim campaign models cannot be loaded by the bridge at all** | OPEN gap (2026-07-24). Closing it needs a `RichRunStateAdapter` + a (157, 4184) detect branch — a bridge change gated by sts2-change-control; see sts2-bridge-and-realgame. |

### 6.2 OPEN bug: live intent one-hot is silently all-zero (2026-07-24)

Found by code audit; **not yet in docs/KNOWN_ISSUES.md** and not fixed at
HEAD `fe25668`. Three-way casing disagreement on the enemy-intent string:

- C# wire: `RlCombatHandler.cs:574` sends `firstIntent.IntentType.ToString()`
  → PascalCase (`Attack`, `Defend`, ...; full enum in
  `decompiled_v0.109.0/MegaCrit.Sts2.Core.MonsterMoves.Intents/IntentType.cs`
  — note there is no `MultiAttack` member; multi-hit is `AttackIntent.Repeats`
  → `intent_hits`).
- Python adapter: `state_adapter.py:55-61` matches exact UPPER_SNAKE keys
  (`"ATTACK"`, `"MULTI_ATTACK"`, ...) with NO case normalization at the
  lookup (`state_adapter.py:195-198`). Powers ARE normalized with `.upper()`
  (`state_adapter.py:361`); intents are not.
- Sim replay serializer: `bridge_replay.py:476` emits `intent_type.name`
  (UPPER_SNAKE), so golden comparison of live vs sim traces will also
  mismatch on `intent`.

Consequence: every live-bridge combat observation has a zero intent one-hot
(`intent_damage`/`intent_hits` still populate). The test suite never catches
it because tests feed sim-cased strings (`tests/test_bridge_state_adapter.py:21`
uses `"ATTACK"`). If you fix it: normalize case on lookup, add a test using
the REAL C# casing, log it in KNOWN_ISSUES.md, and route through
sts2-change-control. Until fixed, do not interpret live-agent combat behavior
as evidence about the policy's intent-reading.

---

## 7. Traps that cost real time (compressed stories)

Full evidence trails and statuses: sts2-failure-archaeology. These are the
versions you need mid-triage — each with its guard so you don't re-trip it.

1. **Energy/AutoPlay (bridge, fixed).** The mod first played cards via
   `CardCmd.AutoPlay()`; energy never decreased and the agent played
   unlimited cards per turn. Fix: enqueue `PlayCardAction` on the game's
   action queue. Guard: none automated (C# runtime behavior) — the rule is
   documented here, in KNOWN_ISSUES #1, and docs/TROUBLESHOOTING.md.
2. **Eval stall / TimeLimit (training, fixed 81b50c7).** SB3's EvalCallback
   evaluated WITHOUT masks; the legacy combat env only truncated on turn
   count, so eval episodes ran tens of thousands of steps and training looked
   hung. Fix: wrap in `gymnasium.wrappers.TimeLimit` + masked eval; and since
   wrappers don't forward attributes, `mask_fn` must use `env.unwrapped`
   (`scripts/train_combat.py:62-77`). Guard: current trainer's `run_eval`
   caps steps explicitly and passes masks; no dedicated regression test.
3. **Infinite Dazed loop (bridge, fixed 5a676c3).** The bridge mask ignored
   the mod's per-card `playable` flag; the live agent replayed an unplayable
   Dazed forever. Fix: honor `playable` in `compute_action_mask`
   (`state_adapter.py:268`). Guard: adapter code path; note (2026-07-24) no
   test exercises the `playable` flag — adding one is cheap insurance when
   touching the adapter.
4. **Harmony silent rename (bridge, fixed for v0.109.0).** Game renamed
   `SetTimeScale(float timeScale)` → `(float scale)`; the by-name-bound
   prefix silently stopped applying. Fix: rename the prefix param. Guard:
   the post-game-update signature-diff checklist (sts2-bridge-and-realgame);
   watch godot.log for `SKIP:` lines.
5. **0% full-run postmortem (training, superseded).** 1M steps, 0% wins:
   the 131-dim obs simply didn't encode Souls/Osty/relics/potions/deck — the
   agent literally could not see the game — plus sparse terminal reward over
   1000+-step episodes and a small MLP. Lesson order: obs visibility →
   reward density → capacity → curriculum, BEFORE algorithm blame. This
   produced the rich-obs redesign. (KNOWN_ISSUES #8, docs/TRAINING_REDESIGN.md.)
6. **Stage-A plateau + frozen optimizer (training, superseded by fe25668).**
   The combat-only stage A plateaued 60.5%→63.5% over 5M steps against an
   85% gate and the auto-ladder hard-halted; final logs showed
   `approx_kl 8.0e-9, clip_fraction 0, lr 1.44e-7` — the linear LR anneal had
   frozen the policy long before budget end, and ~63% was near the random
   baseline anyway. The G1–G5 revamp (constant lr + target_kl, never-halt
   ladder, full-run-only, truncation≠death) is the institutional response —
   spec in `docs/TRAINING_REVAMP_SPEC.json` (tracked). Do not recreate:
   annealed-lr resumes, gates on unreachable thresholds, or halting ladders.
7. **Silent loss swallowing (sim/training, fixed).** `run_env.step()` used
   to convert simulator exceptions into unlogged episode losses — sim bugs
   masqueraded as agent deaths in training curves. Now it logs
   (`STS2RunEnv.step failed during phase ...`) and tags `info["sim_error"]`
   (scored 0.0 since fe25668). Guard: the log line; grep it after every run.
   (KNOWN_ISSUES #14; no dedicated test as of 2026-07-24.)
8. **Full-run "policy" that never ran (bridge, fixed).** Docs claimed
   full-run models drove all phases live; in fact `agent_runner` used
   hardcoded heuristics for every non-combat phase regardless of model. The
   fix (RunStateAdapter + `detect_model_mode`) also surfaced the map-node
   ordering bug. Moral: docs are claims, not evidence — verify the dispatch
   path. (KNOWN_ISSUES #5.)
9. **Circular import fix-the-fix (sim, fixed b3e97b1, 8 minutes after the
   bug shipped).** Eager legacy-act registration at import time formed
   `map.acts → events → run → map` and crashed the play CLIs. Fix: lazy
   registration behind a re-entrancy guard. Guard:
   `tests/test_import_order_no_cycle.py` runs the exact events-first import
   order in a fresh interpreter.

Also historical, cheap to check: searching `TODO|FIXME|HACK` in this repo
yields only false positives (`HACK` matches SHACKLES/SHACKLING card names);
there are no real TODO markers to mine for hints.

---

## 8. After diagnosis: reporting and landing

- **Landing any fix**: sts2-change-control. Minimum for sim-behavior changes:
  full suite green (`.venv\Scripts\python.exe -m pytest tests -q`, 5,276
  tests), all four audit scripts (§4.3), and
  `.venv\Scripts\python.exe scripts\benchmark.py` if performance-relevant.
- **New incident or open bug**: add it to `docs/KNOWN_ISSUES.md` using its
  status vocabulary (Fixed / Open / verified-from-source-only /
  documented-not-fixed) — template in sts2-docs-and-writing. The intent-casing
  bug (§6.2) should be ledgered when someone next touches the bridge.
- **Any win-rate or performance claim** produced while debugging carries its
  protocol (episodes, seeds, deterministic flag, shaping off); final claims
  need ≥1000 episodes with a Wilson 95% CI (sts2-analysis-toolkit). The 95%
  A10 target is aspirational, never a pass/fail gate.
- **Do not** debug on top of someone else's uncommitted diff, launch training
  while a run is live, or trust a stale doc over code
  (`docs/TRAINING_GUIDE.md`, `docs/GAME_BRIDGE_REFERENCE.md` (v1 design),
  and parts of `docs/AGENT_USAGE_GUIDE.md` and `docs/PROTOCOL.md` are known
  stale — staleness map in sts2-docs-and-writing).

---

## 9. Provenance and maintenance

Every fact below was verified directly against the repo on **2026-07-24**
(HEAD `fe25668`). Re-verify before relying on anything date-sensitive.

| Fact (as of 2026-07-24) | Re-verify with |
|---|---|
| HEAD = fe25668 (Phase 0 revamp, G1–G5 ladder); uncommitted content/web edits present | `git log -1 --oneline; git status --short` |
| 5,276 tests collected, ~1.5 s | `.venv\Scripts\python.exe -m pytest tests --collect-only -q` |
| Obs/action sizes 131 / 151 / 157 / 4184 / 115 | the one-liner in §2 step 3 |
| Trainer: constant lr 2e-4, target_kl 0.03, ent 0.01, n_steps 1024, γ 0.997, never-halt ladder, output `output/necrobinder_run` | `Get-Content scripts\train_necrobinder.py -TotalCount 80` |
| Reward: win +1 / death −1 / truncation 0.0 / act 0.25 / floor 0.004 / HP-ret 0.05; eval shaping=0 | `Get-Content sts2_env\gym_env\reward_config.py` |
| `sim_error` tagged + scored 0.0; base-env truncation still = death | `Select-String -Path sts2_env\gym_env\run_env.py -Pattern "sim_error"` and lines 353–359 |
| G1 run live (log ends mid-eval @100k, 500-ep evals, healthy approx_kl ≈0.008) | `Get-Content output\necrobinder_g1_campaign.log -Tail 25` |
| Stage-A history: 50 evals, best 63.5% @2.5M, final 62.0% @5M, frozen lr 1.44e-7 | `Get-Content output\necrobinder_a10\A\ckpt_0005005312.json` (JSON `eval_history`); `Get-Content output\necrobinder_a10_campaign.log -Tail 30` |
| `train_full_run.py` broken (`act_count`/`reward_shaping` kwargs vs `run_env.py:255-262`) | `Select-String -Path scripts\train_full_run.py -Pattern "act_count"` |
| Intent-casing bug open; C# PascalCase vs Python UPPER_SNAKE vs replay `.name` | `Select-String -Path sts2_env\bridge\state_adapter.py -Pattern "_INTENT_STR_TO_IDX" -Context 0,8`; `Select-String -Path bridge_mod\RlCombatHandler.cs -Pattern "IntentType.ToString"` |
| Rich models rejected by bridge (`detect_model_mode` = 115/131 or 157/151 only) | `Select-String -Path sts2_env\bridge\agent_runner.py -Pattern "Unrecognized model"` |
| Audit scripts pass and print "... audit passed" | run the three commands in §4.3 |
| `playable` flag honored at `state_adapter.py:268`; no test covers it | `Select-String -Path sts2_env\bridge\state_adapter.py -Pattern "playable"`; `Get-ChildItem tests | Select-String -Pattern "playable"` |
| KNOWN_ISSUES numbering used here (#1, #5, #6, #8, #11, #14, #16) | `Get-Content docs\KNOWN_ISSUES.md` |
| godot.log markers + path | `Select-String -Path docs\MOD_BUILD_GUIDE.md -Pattern "godot.log|3/3 patches"` |
| Live smoke test still undone; mod deployed 2026-07-23 | `Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2\mods\STS2BridgeMod"` (file dates); docs/BRIDGE_REPLAY_HARNESS.md §untested |

Maintenance rules: when the trainer, reward config, or bridge adapters
change, update §5/§6 rows and this table in the same PR (change-control class
"docs"). When a game update lands, §6's Harmony row and the intent enum in
§6.2 must be re-checked against a fresh decompile. If the intent-casing bug
or the rich-adapter gap gets fixed, move its entry from OPEN to a §7 story.
