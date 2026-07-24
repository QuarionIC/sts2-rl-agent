---
name: sts2-failure-archaeology
description: >
  The incident chronicle of sts2-rl-agent: every reconstructable investigation and
  failure — symptom, root cause, evidence (commit/file/line), current status — so
  settled battles are never re-fought. Load this skill when: you suspect a bug you
  are seeing has happened before; you are about to "fix" something that looks wrong
  (energy stuck at 3, agent looping one card, all-zero observations, training that
  plateaus or stalls, ImportError on play_run, a Harmony patch silently missing);
  you need the authoritative history of the 0% full-run attempt or the 2026-07-24
  stage-A plateau postmortem; or you are writing up a new incident and need the
  format and ledger discipline. Do NOT load this skill for live triage of a NEW
  unknown failure (use sts2-debugging-playbook), for how to land or gate a fix
  (sts2-change-control), for what the current training campaign should do next
  (sts2-training-campaign), for bridge operation details (sts2-bridge-and-realgame),
  or for parity/RNG porting rules (sts2-parity-discipline). This skill is the
  historical record and the list of open wounds; siblings own the procedures.
---

# STS2 Failure Archaeology

This is the project's incident chronicle. Repo root: `C:\Users\motqu\GitHub\sts2-rl-agent`.
Read it before concluding any observed misbehavior is new, and before "fixing"
anything listed under **Settled battles** at the bottom — several past bugs look
like obvious code smells and re-breaking them is silent.

All commands below run from the repo root. Always use the venv interpreter:

```powershell
cd C:\Users\motqu\GitHub\sts2-rl-agent
.venv\Scripts\python.exe --version
```

## Status vocabulary (used for every incident)

| Status | Meaning |
|---|---|
| FIXED | Fix landed, covered by tests or verified against decompiled source |
| FIX-LANDED-UNVALIDATED | Fix committed but the validating experiment has not run yet |
| OPEN | Known, unfixed; workaround or deferral documented |
| DOCUMENTED-NOT-FIXED | Deliberately left broken/unverifiable, recorded in `docs/KNOWN_ISSUES.md` |
| SUPERSEDED | The failing component was replaced rather than repaired |

`docs/KNOWN_ISSUES.md` is the living ledger (16 numbered issues as of 2026-07-24).
This skill adds the incidents that ledger does not carry (training postmortems,
commit-level fix-the-fix stories, and one open bug found in audit that has not
been ledgered yet — see incident 14). Nothing here overrides the ledger; if they
ever disagree, re-check the code and fix whichever is stale via sts2-change-control.

## Timeline: three eras, three failure generations

640 commits as of 2026-07-24 (verify: `git log --oneline | Measure-Object -Line`).

| Era | Dates | Commits | Character | Failure generation |
|---|---|---|---|---|
| 1. Bootstrap | 2026-03-16..18 | 8 | Monolithic initial drop `81f6d6b` (156 files): sim + agent + bridge at once | Gen 1: bridge-mod bring-up traps (energy, threading, mod loading, protocol drift) |
| 2. Parity blitz | 2026-05-18..22 | 620 | Micro-commit parity sweeps (peak 184/day on 05-21), inherited from upstream `zhiyue/sts2-rl-agent` | Gen 2: RNG-stream routing and hook-order regressions (owned by sts2-parity-discipline) |
| 3. Necrobinder A10 campaign | 2026-07-23..24 | 12 | Local-authored: v0.109.0 + mods re-sync, curriculum trainer, revamp | Gen 3: training-pipeline and adapter failures (this skill's core) |

Zero reverts in the entire history (`git log -i --grep=revert --oneline` is empty);
mistakes were always fixed forward. Only branch: `main`.

Jargon used below, defined once:

- **Souls** — Necrobinder's core class resource (a counter spent by class cards).
- **Osty** — Necrobinder's pet. In the real game an *ally monster* with a single
  self-looping `NOTHING_MOVE`, not a player creature (`decompiled_v0.109.0/MegaCrit.Sts2.Core.Models.Monsters/Osty.cs`).
- **AFTP** — the ActsFromThePast mod (legacy act slots); active in the campaign config.
- **Parity** — behavioral equality between this simulator and the decompiled C# game.
- **Bridge** — the TCP JSON link between a trained policy and the real game via the
  C# mod in `bridge_mod/` (see sts2-bridge-and-realgame).
- **Rich obs** — the current 4184-dim observation (`sts2_env/gym_env/rich_observation.py`);
  "legacy obs" is the old 131-dim combat / 151-dim run vector.
- **PBRS** — potential-based reward shaping (policy-invariant shaping; part of the
  adopted revamp, see sts2-training-campaign).

---

## Generation 1 — bridge bring-up (2026-03, inherited)

### Incident 1: Energy stuck at 3, unlimited card plays — FIXED

- **Symptom:** live game showed energy pinned at 3; the agent played arbitrarily
  many cards per turn.
- **Root cause:** the mod executed plays via `CardCmd.AutoPlay()`, a debug API that
  bypasses energy deduction entirely.
- **Fix:** enqueue a real `PlayCardAction(card, target)` through
  `RunManager.Instance.ActionQueueSynchronizer` — `bridge_mod/RlCombatHandler.cs:346`
  (the ledger cites "line 187-188"; the file has since grown — the fix is at 346
  as of 2026-07-24, and the stale header comment at `RlCombatHandler.cs:9` still
  mentions AutoPlay; ignore it).
- **Evidence:** `docs/KNOWN_ISSUES.md` issue #1; `docs/TROUBLESHOOTING.md`
  "Energy always shows 3 after playing cards" (line 181).
- **Rule:** never reintroduce `CardCmd.AutoPlay` for gameplay. It is free.

### Incident 2: "Room type not assigned" timeout after character select — FIXED

- **Symptom:** AutoSlayer picks a character, then the run times out before the
  first map node.
- **Root cause:** game API calls (`NGame.StartRun`, room transitions) issued from
  `Task.Run` worker threads; the Godot API is main-thread-only and fails with
  this cryptic message instead of throwing.
- **Fix:** route all game calls through `TaskHelper.RunSafely` /
  `Callable.From(...).CallDeferred()`.
- **Evidence:** `docs/TROUBLESHOOTING.md` "Room type not assigned" (line 243).
- **Rule:** any new C# handler code touching game state must marshal to the main
  thread. Symptom-to-cause mapping for other mod-loading failures (wrong SDK,
  "Pack version unsupported: 1", missing manifest, first "Load Mods" crash) is in
  `docs/TROUBLESHOOTING.md` lines 9-88 and owned by sts2-bridge-and-realgame.

### Incident 3: All-zero observations / END_TURN-only agent — FIXED

- **Symptom:** agent connects fine but every observation is zeros, or it only
  ever sends `end_turn`.
- **Root cause:** v1-vs-v2 protocol drift. The v2 (AutoSlay-based) mod sends
  combat state FLAT at the top level with no `combat_state` wrapper and no
  `phase` field (the message `type` IS the phase), and expects `{"action": "play"}`
  not `{"type": "PLAY"}`. Adapters written against v1 silently decoded nothing.
- **Fix:** the defensive pattern `combat = state.get("combat_state") or state`,
  now at `sts2_env/bridge/state_adapter.py:117`; plus dual-format string sets for
  target types (`state_adapter.py:71-84`, ledger issue #4).
- **Evidence:** `docs/TROUBLESHOOTING.md` lines 105-175; `docs/KNOWN_ISSUES.md` #4.
- **Rule:** enum-ish strings crossing the C#/Python boundary must be accepted in
  BOTH casings (C# `ToString()` is PascalCase, Python `Enum.name` is UPPER_SNAKE).
  This rule was NOT applied to enemy intents — that is open incident 14 below.

---

## Generation 3 — the July 2026 campaign

(Generation 2, the May parity blitz's RNG-stream and hook-order fix classes, is
chronicled operationally in sts2-parity-discipline — inclusive-vs-exclusive
`next_int`, stream routing, stable-sort-before-consume, killed-targets-skip-
`after_damage_received`. Do not re-derive those rules here.)

### Incident 4: Training "hang" — unmasked eval episodes — FIXED (81b50c7, 2026-07-23)

- **Symptom:** training appeared to hang partway through a run.
- **Root cause:** two stacked issues. (a) `STS2CombatEnv` truncates only on turn
  count, and SB3's `EvalCallback` evaluates WITHOUT action masks — eval episodes
  wandered for tens of thousands of steps. (b) After wrapping envs, masks broke,
  because gymnasium wrappers don't forward custom attributes.
- **Fix:** wrap train/eval envs in `gymnasium.wrappers.TimeLimit`
  (`--max-episode-steps`, default 1000) and make `mask_fn` call `env.unwrapped`
  — `scripts/train_combat.py:62-77` (comment at 62-64 states the wrapper gotcha).
- **Evidence:** commit `81b50c7` (2026-07-23 15:39) message spells out both causes.
- **Rule:** every env handed to any SB3 eval path gets a `TimeLimit`; every
  `mask_fn`/attribute access on a wrapped env goes through `.unwrapped`.

### Incident 5: Infinite Dazed replay against the real game — FIXED (5a676c3, 2026-07-23)

- **Symptom:** live agent replays an unplayable Dazed card forever; game rejects
  it each time, re-sends the same state, loop never advances.
- **Root cause:** the Python action mask computed playability from energy/cost
  alone and ignored the mod's authoritative per-card `playable` flag, so
  sim-side legality and game-side legality disagreed. The game executes nothing
  on an illegal play and re-prompts — a livelock, not an error.
- **Fix:** respect `card.get("playable", True)` in the mask —
  `sts2_env/bridge/state_adapter.py:266-268`.
- **Evidence:** commit `5a676c3` (2026-07-23 17:38): "state_adapter.py: respect
  the mod's per-card 'playable' flag in the action mask (fixes infinite Dazed
  replay loop)".
- **Rule:** the game's validity flags are authoritative; simulator-side legality
  logic is never sufficient for masking against the live game.

### Incident 6: Harmony patch silently un-applied after game update — FIXED (recurs by design)

- **Symptom:** animations run at normal speed; no error anywhere; mod log shows
  the patch counted as skipped.
- **Root cause:** Harmony binds injected prefix parameters BY NAME. Game v0.109.0
  renamed `SetTimeScale(float timeScale)` to `(float scale)`; the prefix
  parameter `ref float timeScale` no longer matched, so `AnimationSpeedPatch`
  silently failed to apply.
- **Fix:** rename the prefix parameter to `ref float scale` —
  `bridge_mod/MainFile.cs:171` (remarks at 161 record the history).
- **Evidence:** `docs/KNOWN_ISSUES.md` #6, verified against
  `decompiled_v0.109.0/MegaCrit.Sts2.Core.Bindings.MegaSpine/MegaAnimationState.cs`.
- **Rule:** this incident WILL recur on every game update. After any update,
  re-decompile and diff the exact signatures — method names AND parameter names —
  of every patched method before trusting the bridge. Checklist lives in
  sts2-bridge-and-realgame.

### Incident 7: Full-run models never actually drove non-combat phases — FIXED (ledger #5)

- **Symptom:** a "full-run" trained model behaved exactly like the hardcoded
  heuristics outside combat. Docs claimed the policy handled all phases.
- **Root cause:** `agent_runner.run_agent()` unconditionally called heuristic
  pickers (`_pick_map_node`, `_pick_card_reward_index`, ...) for every non-combat
  phase, regardless of model type. The full-run policy's map/shop/event/rest
  decisions were never consulted. Nobody noticed because nobody compared
  policy-vs-heuristic behavior — an eval-blindness failure, not a crash.
- **Fix:** `RunStateAdapter` (`sts2_env/bridge/run_state_adapter.py`) +
  `detect_model_mode()` (`sts2_env/bridge/agent_runner.py:132-165`) routing all
  phases through `model.predict()` for full-run models.
- **The latent second bug found during the fix:** `bridge_mod/RlMapHandler.cs`
  ordered reachable map nodes by coordinate scan, not `MapPoint.Children`
  insertion order — silently scrambling action indices vs. training-time order.
  Fixed to read `lastNode.Point.Children` directly (`RlMapHandler.cs:66-80`).
- **Evidence:** `docs/KNOWN_ISSUES.md` #5 (both bugs, in detail).
- **Rules:** (a) option-list POSITION is the action encoding — any bridge
  enumeration must match `RunManager.get_available_actions()` ordering exactly;
  several orderings (shop) remain unverified, see ledger #16. (b) When docs claim
  a code path is exercised, verify with a discriminating experiment; see
  sts2-research-methodology.

### Incident 8: Circular import broke `play_run` — FIXED (b3e97b1, an 8-minute fix-the-fix)

- **Symptom:** `python -m sts2_env.web.play_run` (and the CLI) crashed with
  `ImportError: ... _event_result_with_rewards`.
- **Root cause:** commit `6860139` (2026-07-24 09:45, the 508-file AFTP
  completion) built the legacy-act candidate registry eagerly at
  `sts2_env/map/acts.py` import time, forming the cycle
  `map.acts -> events.* -> run -> map -> map.acts` whenever `sts2_env.events`
  was imported first — exactly what play_run does.
- **Fix:** commit `b3e97b1` (09:53, eight minutes later) defers construction to
  first registry access behind a re-entrancy guard —
  `sts2_env/map/acts.py:351-369` (flag set BEFORE building, line 369) — plus a
  fresh-interpreter regression test `tests/test_import_order_no_cycle.py`.
- **Rule:** content registries populate lazily on first access, never at module
  import time. Any change to registry bootstrapping must keep
  `test_import_order_no_cycle.py` green. Rationale and the full registry design
  are in sts2-architecture-contract.

### Incident 9: Silent-loss swallowing in `run_env.step()` — FIXED in two stages

- **Symptom:** simulator bugs during training looked like agent deaths;
  measured win rates were biased downward with no error surfaced anywhere.
- **Root cause:** `STS2RunEnv.step()` wrapped phase dispatch in a bare
  `try/except` that force-lost the run on any exception, silently.
- **Fix, stage 1** (ledger #14): log the exception before losing the run.
- **Fix, stage 2** (`fe25668`, 2026-07-24 11:52): tag `info["sim_error"] = True`
  and score the forced loss 0.0 instead of -1 — `sts2_env/gym_env/run_env.py:337-364`.
  `RichSTS2RunEnv` likewise skips the death reward on `sim_error`
  (`sts2_env/gym_env/rich_run_env.py:170-172`).
- **Rule:** after any long training run, grep logs for the marker before trusting
  win rates: `Select-String -Path <log> -Pattern "STS2RunEnv.step failed"`. A
  nonzero hit count means the win-rate denominator includes simulator bugs.
- **Related trap still present by design:** invalid actions are silently ignored
  in the combat envs and an unsuccessful `play_card` falls back to `end_turn` in
  the run env — masking is what keeps the agent honest; always pass
  `action_masks` to `predict()`.

### Incident 10: The 0% full-run attempt — SUPERSEDED (postmortem is doctrine)

- **Symptom:** 0% win rate after 1M full-run training steps, even though the
  agent clearly learned something (avg 8.9 floors reached vs 3.9 for random).
- **Root causes** (all four, per `docs/TRAINING_REDESIGN.md:7-21` and ledger #8):
  1. **Blind observation.** The 131-dim combat vector omitted Souls, Osty,
     relics, potions, deck composition, upgrade status, and all but 6 power
     types; the 20 run dims had no map lookahead, boss identity, or key state.
     "The agent literally cannot see the game it is being asked to master."
  2. **Sparse terminal reward** (+1/-1) over 1000+ step episodes.
  3. **Tiny network:** MlpPolicy [256,256] over the lossy vector.
  4. **No curriculum:** training started on the hardest configuration.
  A fifth, operational cause: `scripts/train_full_run.py` had drifted — it still
  passes `act_count=`/`reward_shaping=` kwargs (`train_full_run.py:26-27,52-53`)
  that `STS2RunEnv.__init__` no longer accepts (`run_env.py:255-262` takes only
  `character_id, ascension_level, max_steps, max_combat_turns, render_mode`) —
  instant `TypeError`. The script remains in-tree, broken, DOCUMENTED-NOT-FIXED;
  it is superseded by `scripts/train_necrobinder.py`. Never use it.
- **Status:** SUPERSEDED by the rich-obs curriculum (which then produced
  incident 11). The postmortem's diagnostic ORDER is the settled lesson.
- **Rule:** when a training run flatlines, check in this order: can the agent
  SEE the decision-relevant state → is the reward reachable → capacity →
  curriculum. Only then blame the algorithm.

### Incident 11: The stage-A plateau — FIX-LANDED-UNVALIDATED (the live one, 2026-07-24)

The immediate predecessor of the current campaign plan. Know it cold before
touching training.

- **Symptom:** stage A of the six-stage curriculum (combat-only, Act-1
  encounters, Necrobinder starter deck, A10, rich obs) exhausted its 5M-step
  budget at 63.5% best combat win rate vs. the 85% promotion gate, and the
  `--auto` ladder halted itself. Win rate was flat from the FIRST eval:
  60.5% at 100k steps → best 63.5% (sidecar shows it at 2.5M) → 62.0% at 5M,
  across 50 evals of 200 episodes.
- **Hard evidence** (all on disk, verify commands in Provenance):
  - `output/necrobinder_a10_campaign.log` tail: `[stage A] done in 63.8 min
    (5,005,312 steps, best win rate 63.5%)` then the `[auto] ... stopping` halt
    message; final SB3 block: `approx_kl 8.03e-09`, `clip_fraction 0`,
    `learning_rate 1.44e-07`, `fps 1307`.
  - `output/necrobinder_a10/A/best_model.json`: `best_win_rate 0.635`,
    `promoted False`, first evals 0.605/0.61/0.625.
  - 21 checkpoints of ~100 MB each under `output/necrobinder_a10/A/`.
- **Root causes** (per `docs/TRAINING_REVAMP_SPEC.json` `executive_summary`,
  the adopted design review; structural, not algorithmic):
  1. **Unwinnable task mix:** stage A sampled the bare 10-card starter deck
     uniformly against the full Act-1 pool where ~27% of fights are
     elites/bosses a starter deck cannot beat — the 85% gate was unreachable.
  2. **Ladder hard-halt:** `if not promoted: break` meant not a single
     full-run gradient was ever taken.
  3. **Frozen optimizer:** linear LR anneal reached 1.44e-7 by budget end
     (`approx_kl` 8e-9, `clip_fraction` 0) — the last stretch of training was
     a no-op, and `--resume` with the same `--total-steps` would resume at
     lr≈0 (`--total-steps` is an ABSOLUTE target, not an increment).
  4. **Architecture blindness:** mean-pooled hand encoding hid which card sat
     in which slot from the per-slot action head; the deck was invisible
     outside combat (7 aggregate scalars).
  5. **Hackable, jittery shaping:** the win-rate-driven shaping anneal moved
     the reward function ~40% between evals.
  6. **Truncation scored as death** (legacy behavior; note the base
     `STS2RunEnv` STILL scores truncation as `REWARD_DEATH` at
     `run_env.py:358-359` — only `RichSTS2RunEnv`, the campaign env, scores it
     `cfg.truncation` = 0.0 and tags `info["truncated"]`,
     `rich_run_env.py:175-178`).
  7. **Eval-telemetry blindness:** `run_eval` read `info['floor']`/`info['act']`
     which the combat env never set — every stage-A eval line shows
     `mean_floors=0.0 deaths_by_act={0: N}` (see the log). Only `win_rate` was
     real. The plateau's SHAPE was therefore harder to diagnose than it should
     have been.
  A near-random baseline suspicion compounds cause 1: `docs/TRAINING_GUIDE.md:88`
  reports ~63.4% random-baseline combat win for Ironclad Act 1 (different
  character/setup — indicative, not comparable), so 60-63% for stage A may sit
  near the floor of the task mix rather than reflecting learning.
- **Fix:** commit `fe25668` (2026-07-24 11:52, "Phase 0 training revamp")
  deletes stages A/B and the combat-only path entirely; the ladder is now
  full-run-only G1-G5 (`scripts/train_necrobinder.py:58-65`), with constant
  `lr=2e-4` + `target_kl=0.03`, constant `ent_coef=0.01`, no shaping anneal, no
  ladder halt, and real eval telemetry (`mean_act`, `truncation_rate`,
  `train_necrobinder.py:138-141`).
- **Status: FIX-LANDED-UNVALIDATED.** As of 2026-07-24 no training relaunch has
  validated `fe25668`. Do not claim the plateau is solved; do not cite G-ladder
  win rates that do not exist yet. What to run next and expected numbers per
  gate are owned by sts2-training-campaign.
- **Rules:**
  - Before concluding "capability plateau," read `approx_kl` / `clip_fraction` /
    `learning_rate` in the SB3 logs. kl≈1e-8 with clip_fraction 0 means the
    optimizer is frozen, and the ceiling is a schedule artifact.
  - Never gate a curriculum on a threshold you have not shown is reachable for
    the stage's task distribution.
  - Never trust telemetry fields you have not verified the env actually sets.

### Incident 12: Eval statistics too weak for the claims made — SETTLED AS DOCTRINE

- **Symptom:** stage-A promotion decisions rode on single 200-episode evals;
  1-sigma at ~62% win rate is ~3.4 percentage points, comparable to the entire
  observed 60.5→63.5% "improvement."
- **Resolution:** promotion already required the threshold on 2 consecutive
  evals (retained at `train_necrobinder.py:221`); house doctrine now requires
  ≥1000 eval episodes with a Wilson 95% CI for any final/external claim, eval
  with shaping off and deterministic. Protocol details: sts2-analysis-toolkit;
  the rule itself: sts2-change-control.
- **Rule:** never publish or gate on a ≤200-episode eval.

---

## Open wounds (verified still-present, 2026-07-24)

### Incident 13: Rich models cannot drive the bridge — OPEN

- **Symptom:** loading any campaign-trained model into the live-game runner
  fails: `ValueError: Unrecognized model action/observation space...`.
- **Root cause:** `detect_model_mode()` (`sts2_env/bridge/agent_runner.py:132-165`,
  raise at 159) accepts exactly two shapes: 115 actions/131 obs (combat-only)
  or 157/151 (legacy full-run). The whole campaign trains 157/4184 rich-obs
  models. `RunStateAdapter` still encodes the OLD 151-dim vector
  (`sts2_env/bridge/run_state_adapter.py` imports `RUN_OBS_SIZE` from
  `run_env.py`); `grep -i rich sts2_env/bridge/` has zero hits.
- **What a fix requires:** a `RichRunStateAdapter` reproducing the
  `rich_observation.py` layout from bridge JSON, plus a `(157, 4184)` branch in
  `detect_model_mode`. Nontrivial: several rich segments (pile bags, full power
  vectors, map lookahead) need wire fields the mod may not send yet. Scope and
  design belong to sts2-bridge-and-realgame; land via sts2-change-control.
- **Status: OPEN.** No real-game evaluation of any campaign model is possible
  until this closes.

### Incident 14: Live intent one-hot is silently all-zero — OPEN, NOT YET LEDGERED

Found in a 2026-07-24 audit; re-verified against the code the same day. Not yet
recorded in `docs/KNOWN_ISSUES.md` — whoever fixes it (or touches the adapter)
should ledger it first per sts2-change-control.

- **Symptom (predicted; never observed live because the bridge has never been
  live-smoke-tested):** every live-bridge combat observation has an all-zero
  enemy-intent one-hot block; `intent_damage`/`intent_hits` still populate.
- **Root cause — a three-way casing mismatch:**
  1. C# sends `firstIntent.IntentType.ToString()` = PascalCase (`"Attack"`,
     `"Defend"`, ...) — `bridge_mod/RlCombatHandler.cs:574`; enum members in
     `decompiled_v0.109.0/MegaCrit.Sts2.Core.MonsterMoves.Intents/IntentType.cs`.
  2. Python matches UPPER_SNAKE keys with NO case normalization at lookup —
     `_INTENT_STR_TO_IDX` (`sts2_env/bridge/state_adapter.py:55-61`, keys from
     `protocol.py:121-138`), lookup at `state_adapter.py:196`. The powers path
     normalizes with `.upper()`; the intent path does not.
  3. `docs/PROTOCOL.md` documents intent strings (`"SingleAttack"`,
     `"MultiAttack"`) matching NEITHER side. `MULTI_ATTACK` cannot occur at
     all — the C# enum has no such member (multi-hit is `AttackIntent.Repeats`,
     surfaced as `intent_hits`).
- **Blast radius:** (a) any live combat-only model run so far had a degraded
  observation; (b) golden replay comparison will mismatch on `intent`, because
  the sim-side serializer emits `intent_type.name` = UPPER_SNAKE
  (`sts2_env/parity/bridge_replay.py:476`); (c) the test suite cannot see it —
  `tests/test_bridge_state_adapter.py:21` feeds `"ATTACK"`, the sim casing,
  not the real wire casing.
- **Fix shape:** case-normalize at the adapter boundary (both `.upper()` and
  mapping PascalCase members), correct `docs/PROTOCOL.md`, and add a test using
  the REAL C# casing. Also decide what `Attack` maps to given the adapter's
  5-slot intent list expects a MULTI_ATTACK distinction the wire never makes.
- **Status: OPEN** as of 2026-07-24.

### Incident 15: The bridge has never survived a live smoke test — OPEN

The mod is built and deployed (2026-07-23 20:28, Debug config, auto-copied to
the Steam mods folder), but no one has run game + mod + agent end-to-end
through a complete run. Explicitly untested live: event-triggered combat
routing, `RlCardSelector` interception of upgrade/transform/enchant screens,
full-run replay comparison, the reward_screen heuristic, and every ordering
flagged in ledger #16 (shop ordering; "sell Foul Potion" has NO bridge-JSON
representation at all). Additionally, on agent timeout (30s) or disconnect the
game does NOT pause — it plays RANDOMLY and keeps going, so any run where the
agent process hiccupped is contaminated data. The smoke-test checklist is owned
by sts2-bridge-and-realgame; treat every "works against the real game" claim as
unproven until it runs.

---

## Settled battles — do not re-fight

Re-verify against code if in doubt, but the burden of proof is on reopening:

- [ ] Do not "simplify" `combat = state.get("combat_state") or state` — it is
      the v1/v2 protocol compatibility shim (incident 3).
- [ ] Do not remove the `playable`-flag check from the bridge action mask
      (incident 5).
- [ ] Do not rename Harmony prefix parameters for style — they bind by name to
      game internals (incident 6).
- [ ] Do not reorder bridge option lists or "clean up" enumeration code —
      position IS the action encoding (incident 7).
- [ ] Do not make registry construction eager "for clarity" —
      `tests/test_import_order_no_cycle.py` guards this (incident 8).
- [ ] Do not remove the `sim_error` tagging or re-silence exceptions in
      `run_env.step()` (incident 9).
- [ ] Do not resurrect `scripts/train_full_run.py`; it is drifted and broken
      (incident 10).
- [ ] Do not reintroduce a linear LR anneal, a win-rate-coupled shaping anneal,
      a promotion hard-halt, or truncation==death into the trainer without a
      design review through sts2-change-control — each one is a named root
      cause of incident 11.
- [ ] Do not resume any stage with an unchanged absolute `--total-steps` after
      budget exhaustion (incident 11, cause 3).
- [ ] Do not trust `CardCmd.AutoPlay` (incident 1) or off-main-thread game
      calls (incident 2).
- [ ] Grep trap: `TODO|FIXME|HACK|XXX` searches in this repo return only false
      positives — `HACK` matches SHACKLES/SHACKLING card names. There are no
      real TODO markers; absence of hits is not evidence of completeness.

## Adding a new incident

1. Reproduce and root-cause it first (sts2-debugging-playbook), land the fix
   through the gates (sts2-change-control).
2. Record it in `docs/KNOWN_ISSUES.md` with an honest status — the ledger
   distinguishes "Fixed", "Verified correct for v0.109.0 (from source only)",
   and documented-not-fixed. Never paper over a gap you could not verify;
   ledger #16 is the model for flagging-without-fixing.
3. Add the entry here with the five fields used above: Symptom, Root cause,
   Evidence (commit hash + `file:line`), Fix, Status — plus the distilled rule
   if the incident generalizes. Date-stamp anything that can drift.
4. If the fix guards against recurrence, name the test that now guards it.
   An incident without a guarding test or a re-verification command will rot.

## Provenance and maintenance

All facts in this file were re-verified against the repo on **2026-07-24**, at
HEAD `fe25668` (note: HEAD advanced during authoring day — earlier same-day
documents cite `18a8059`; the working tree also carried uncommitted
`sts2_env/content/` and `web/play_run.py` edits from a concurrent session, none
of which this skill depends on). One-line re-verification commands:

```powershell
# HEAD, era structure, no-reverts claim, commit count (640 @ 2026-07-24)
git -C C:\Users\motqu\GitHub\sts2-rl-agent log -1 --oneline
git -C C:\Users\motqu\GitHub\sts2-rl-agent log --oneline | Measure-Object -Line
git -C C:\Users\motqu\GitHub\sts2-rl-agent log -i --grep=revert --oneline

# Incident commits exist with the cited dates/messages
git -C C:\Users\motqu\GitHub\sts2-rl-agent show -s --format="%h %ad %s" 81b50c7 5a676c3 6860139 b3e97b1 fe25668 --date=iso

# Ledger still has 16 issues; statuses unchanged
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\docs\KNOWN_ISSUES.md -Pattern "^### "

# Incident 11 evidence still on disk
Get-Content C:\Users\motqu\GitHub\sts2-rl-agent\output\necrobinder_a10_campaign.log -Tail 5
Get-Content C:\Users\motqu\GitHub\sts2-rl-agent\output\necrobinder_a10\A\best_model.json | ConvertFrom-Json | Select-Object best_win_rate, promoted

# Incident 13 still open: detect_model_mode rejects rich shapes; no 'rich' in bridge
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\sts2_env\bridge\agent_runner.py -Pattern "Unrecognized model"
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\sts2_env\bridge\*.py -Pattern "rich" -SimpleMatch

# Incident 14 still open: UPPER_SNAKE keys, no normalization at lookup, PascalCase wire
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\sts2_env\bridge\state_adapter.py -Pattern "_INTENT_STR_TO_IDX"
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\bridge_mod\RlCombatHandler.cs -Pattern "IntentType.ToString"

# Incident 9/11 fixes still present (sim_error tag; G-ladder; constant lr + target_kl)
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\sts2_env\gym_env\run_env.py -Pattern "sim_error"
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\scripts\train_necrobinder.py -Pattern "target_kl|STAGE_ORDER"

# Incident 10: train_full_run.py still drifted vs STS2RunEnv.__init__
Select-String -Path C:\Users\motqu\GitHub\sts2-rl-agent\scripts\train_full_run.py -Pattern "act_count=|reward_shaping="

# Guarding tests still collected
C:\Users\motqu\GitHub\sts2-rl-agent\.venv\Scripts\python.exe -m pytest C:\Users\motqu\GitHub\sts2-rl-agent\tests\test_import_order_no_cycle.py --collect-only -q
```

Volatile facts to re-stamp on next edit: HEAD hash and commit count; incident 11
status (flips to VALIDATED/refuted after the first G-ladder relaunch — update
alongside sts2-training-campaign); incidents 13-15 statuses (flip when the rich
adapter, intent fix, or live smoke test lands, and mirror any fix into
`docs/KNOWN_ISSUES.md`); `RlCombatHandler.cs` line numbers (file drifts; the
ledger's "187-188" is already stale vs. the actual 346).
