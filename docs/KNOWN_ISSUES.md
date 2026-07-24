# Known Issues and Limitations

Current known issues, bugs, and limitations of the STS2 RL Agent project.

---

## Fixed Issues

### 1. Energy always displayed as 3 with CardCmd.AutoPlay

**Status:** Fixed

**Problem:** The C# bridge mod initially used `CardCmd.AutoPlay()` to execute card plays. This method bypasses the normal energy deduction, so the player's energy always stayed at 3 (max) regardless of cards played. The agent could play unlimited cards per turn.

**Fix:** Switched to `PlayCardAction` which properly spends energy:
```csharp
var playAction = new PlayCardAction(card, target);
RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(playAction);
```

**Location:** `bridge_mod/RlCombatHandler.cs` line 187-188

### 2. EchoForm / modify_card_play_count was missing

**Status:** Fixed

**Problem:** The hook for modifying how many times a card is played was not implemented. Powers like EchoForm (play each card twice) had no effect.

**Fix:** Added `modify_card_play_count` to `core/hooks.py` and wired it into `CombatState.play_card()`.

**Location:** `sts2_env/core/hooks.py` lines 189-200, `sts2_env/core/combat.py` line 255

### 3. Enemy round-1 block not cleared

**Status:** Fixed

**Problem:** Enemies that gained block before their first turn (from combat-start effects) were not having their block cleared at the start of the enemy turn on round 1.

**Fix:** The enemy turn now always clears block for each alive enemy, regardless of round number.

**Location:** `sts2_env/core/combat.py` `_execute_enemy_turn()`

### 4. State adapter and action mask protocol mismatches

**Status:** Fixed

**Problem:** The Python `StateAdapter` was expecting different field names and formats than what the C# mod was actually sending. For example, target type strings like `"AnyEnemy"` vs `"ANY_ENEMY"`, and power list format differences.

**Fix:** Updated `state_adapter.py` to handle both formats:
```python
_UNTARGETED_TYPES = {TargetTypeName.SELF, TargetTypeName.NONE, TargetTypeName.ALL_ENEMIES,
                     "SELF", "NONE", "ALL_ENEMIES", "Self", "None", "AllEnemies"}
```

**Location:** `sts2_env/bridge/state_adapter.py` lines 69-71

### 5. Full-run models only used the trained policy for combat

**Status:** Fixed

**Problem:** `agent_runner.py`'s docstring and `AGENT_USAGE_GUIDE.md` claimed that loading a full-run model would "handle all phases via the trained policy," but `run_agent()` unconditionally used hardcoded heuristics (`_pick_map_node`, `_pick_card_reward_index`, `_pick_rest_option`, `_pick_shop_option`, `_pick_event_option`, `_pick_treasure_option`, `_pick_boss_relic_option`) for every non-combat phase regardless of which model was loaded. A full-run `MaskablePPO` model's policy for map routing, card rewards, shop, rest, events, treasure, and boss relics was never actually consulted.

**Fix:** Added `sts2_env/bridge/run_state_adapter.py`'s `RunStateAdapter`, which encodes/decodes to and from exactly the same unified `Discrete(157)` action space and 151-dim observation space `STS2RunEnv` (`gym_env/run_env.py`) trains against — reusing `StateAdapter` for the combat slice rather than duplicating it. `agent_runner.py` now calls `detect_model_mode()` at load time (comparing `action_space.n`/`observation_space.shape[0]` against the full-run vs. combat-only sizes) and routes every phase through `RunStateAdapter` + `model.predict()` when a full-run model is loaded, logging which mode was detected. The heuristic functions are unchanged and still used for combat-only models.

Also fixed a real bridge-JSON ordering bug found while auditing this: `RlMapHandler.cs`'s non-first-move branch re-derived the reachable-node list by rescanning `allPoints` filtered by coordinate membership, which reflects scene/visual (row, col) order rather than `MapPoint.Children`'s own insertion order (path generation can add children out of column order). Since `run_env.py`'s `_step_map_choice` indexes into `RunManager.get_available_actions()` — which iterates `MapPoint.Children` directly — this could feed a full-run policy a differently-ordered node list than it was trained against. Fixed to build the list from `lastNode.Point.Children` directly.

Several other bridge-protocol gaps were found but deliberately left unfixed / flagged rather than papered over — see issue #16 below.

**Location:** `sts2_env/bridge/run_state_adapter.py` (new), `sts2_env/bridge/agent_runner.py` (`detect_model_mode`, full-run dispatch branch in `run_agent()`), `bridge_mod/RlMapHandler.cs` (children-order fix)

### 6. AnimationSpeedPatch fails to apply

**Status:** Fixed (verified against decompiled v0.109.0)

**Problem:** The Harmony patch targeting `MegaAnimationState.SetTimeScale` failed to apply because the game renamed the method's parameter from `timeScale` to `scale` (v0.109.0 signature: `public void SetTimeScale(float scale)`). Harmony binds injected prefix parameters **by name**, so the prefix's `ref float timeScale` no longer matched and the patch was skipped at apply time.

**Fix:** Renamed the prefix parameter to `ref float scale` to match the current game signature (`decompiled_v0.109.0/MegaCrit.Sts2.Core.Bindings.MegaSpine/MegaAnimationState.cs` line 105).

**Location:** `bridge_mod/MainFile.cs` `AnimationSpeedPatch` class

### 7. Mod abandon-run popup path may not match all versions

**Status:** Verified correct for v0.109.0

**Problem:** The Godot scene tree paths used to find the abandon-run confirmation popup (`VerticalPopup/YesButton`) were unverified and might not have matched all game versions.

**Verification (v0.109.0):** Confirmed against decompiled source: the main menu's `AbandonRunButton` (at `MainMenuTextButtons/AbandonRunButton`) opens `NAbandonRunConfirmPopup` via `NModalContainer.Instance.Add(...)` (tracked by `NModalContainer.OpenModal`), and that popup gets its `NVerticalPopup` child via `GetNode("VerticalPopup")`, which in turn gets its yes button via `GetNode<NPopupYesNoButton>("YesButton")` (`NPopupYesNoButton : NButton`). So the mod's `VerticalPopup/YesButton` lookup is exactly right for this version. Runtime behavior still unverified (no live game in the dev environment); re-check on future game updates.

**Location:** `bridge_mod/RlAutoSlayer.cs` `PlayMainMenuAsync()`; ground truth in `decompiled_v0.109.0/MegaCrit.Sts2.Core.Nodes.CommonUi/NAbandonRunConfirmPopup.cs` and `NVerticalPopup.cs`

---

## Open Issues

### 8. Full-run training needs significantly more steps and better reward shaping

**Severity:** High (fundamental training challenge)

**Problem:** The full-run environment produces 0% win rate even after 1M training steps. The agent learns to progress further through Act 1 (avg 8.9 floors vs 3.9 for random) but cannot complete a run.

**Root causes:**
- Sparse reward: only +1 at run victory, -1 at death. No intermediate signal.
- Long episodes: a full run spans thousands of steps.
- Multi-phase action space: `Discrete(157)` across combat, map, rewards, shop, rest, event, treasure, and player-selection slices.
- Compounding decisions: bad deck choices early doom later combats.

**Mitigation:** Reward shaping is available (`--reward-shaping` flag) but only provides small floor-progression bonuses. A fundamental redesign of the reward function or training approach (hierarchical RL, curriculum learning) is needed.

### 9. Only Ironclad combat model trained

**Severity:** Medium

**Problem:** The combat training pipeline only creates Ironclad starter decks. All training and evaluation use the Ironclad character.

**Impact:** The trained model is specific to Ironclad. It cannot play Silent, Defect, Necrobinder, or Regent effectively because:
- Different starter decks and starting HP
- Character-specific mechanics (orbs, stars, pets)
- Different card pools with different effect distributions

**Workaround:** The simulator supports all 5 characters (cards, powers, monsters are all implemented). Training scripts need to be extended to support character selection.

### 10. Combat potion actions were missing from the RL action space

**Status:** Fixed

**Problem:** The combat action space originally only covered card plays and end turn, so the agent could not use potions strategically during combat.

**Fix:** The combat action space now includes fixed-width potion actions, `CombatState` can execute potion uses directly, and the bridge path serializes and decodes potion actions as explicit `POTION` commands.

**Location:** `sts2_env/core/constants.py`, `sts2_env/core/combat.py`, `sts2_env/gym_env/action_space.py`, `sts2_env/gym_env/combat_env.py`, `sts2_env/bridge/state_adapter.py`, `bridge_mod/RlCombatHandler.cs`

### 11. Some card effects may not match the real game exactly

**Severity:** Medium (simulator fidelity)

**Problem:** The headless simulator reimplements card effects based on the decompiled C# source, but exact parity is still broader than the currently audited test surface. The earlier helper-level gaps are fixed, but some card and relic interactions still need direct decompiled-backed regression tests before they should be treated as exact.

**Examples of still-audited-not-proven-exact areas:**
- selected colorless/event cards such as `Alchemize`, `BeatDown`, and `HandOfGreed`
- selected Defect and Silent follow-up effects such as `Compact`, `WhiteNoise`, and `TheHunt`
- wider relic-hook interactions outside the targeted parity suites

**Impact:** The trained model may develop strategies that exploit simulator inaccuracies and fail to transfer to the real game. The bridge mod's real-game evaluation is the ground truth.

### 12. Reconnection timing issues

**Severity:** Low

**Problem:** If the Python agent connects before the game has finished loading and the AutoSlayer has started, there can be a race condition where the first state message arrives before the agent is ready.

**Workaround:** Start the game first, wait for the main menu to appear, then start the Python agent. The agent runner has reconnection retry logic (`_reconnect_with_retry` with 10 attempts, 3s delay).

**Location:** `sts2_env/bridge/agent_runner.py` lines 288-309

### 13. `inspect.signature` on hot path

**Status:** Fixed

**Severity:** Low (performance)

**Problem:** `fire_after_card_drawn` used to call `inspect.signature(method).parameters` for every card draw to determine the parameter count of each power's `on_card_drawn` method. This was slow.

**Fix:** All power `on_card_drawn` implementations now use `(owner, card, from_hand_draw, combat)`, and the dispatcher calls that signature directly.

**Location:** `sts2_env/core/hooks.py`

### 14. `run_env` exception handling used to hide simulation bugs

**Status:** Fixed

**Problem:** `STS2RunEnv.step()` used to convert internal simulation exceptions into silent losses, which made debugging difficult.

```python
try:
    if phase == RunManager.PHASE_COMBAT:
        self._step_combat(action)
    # ...
except Exception:
    if not self._mgr.is_over:
        self._mgr.run_state.lose_run()
```

**Fix:** `STS2RunEnv.step()` now logs the exception before forcing the run to end, so failures are visible in logs instead of disappearing into episode outcomes.

**Location:** `sts2_env/gym_env/run_env.py`

### 15. Pile-summary distribution shift between simulator and bridge

**Status:** Fixed

**Problem:** The observation vector used to encode pile-composition features in simulator mode even though bridge mode could not provide them.

**Fix:** The simulator now keeps those three pile-composition slots zeroed as well, so simulator and bridge observations match on that segment without changing observation size.

**Location:** `sts2_env/gym_env/observation.py`, `sts2_env/bridge/state_adapter.py`

### 16. Full-run bridge adapter: unresolved ordering/coverage gaps vs. `RunManager`

**Severity:** Medium — affects full-run model quality against the real game only (no live game available to verify against in this environment)

**Problem:** `RunStateAdapter` (see issue #5) assumes the bridge JSON's option lists are positionally aligned with what `RunManager.get_available_actions()` would produce for the same phase. This was verified where possible by reading the C# handlers in `bridge_mod/`, but several gaps remain that could not be fixed or fully verified without a live game to test against:

- **Run-level observation fields are mostly unavailable on the wire.** *Fixed on the C# side (2026-07-23):* every bridge payload (combat, map, rewards, card reward/select/bundle, rest, shop, event, treasure, boss relic, crystal sphere, game over, run complete) now carries `act`, `act_index`, `floor`, `act_floor`, `gold`, `deck_size`, `relic_count`, `relics` (id list), `num_potions`, `max_potion_slots`, `potions` (id list, non-combat only -- combat keeps its richer list), `ascension_level`, `room_type`, `is_elite`, `is_boss`, plus top-level `hp`/`max_hp` ints, via the shared `RunStateBridgeFields.Apply()` helper in `bridge_mod/RlNonCombatRoomHandlers.cs`. The field names match exactly what `RunStateAdapter` already reads, so no Python change is needed for gold/deck/relic/potion/ascension/act-floor/elite/boss dims. **Remaining gap:** the run-level HP-ratio dim still falls back to 1.0 at non-combat phases because `_hp_ratio()` in `run_state_adapter.py` only reads a `player` dict -- and the bridge deliberately must NOT add a top-level `player` key to non-combat payloads (the combat `StateAdapter` treats any state with a `player` key as a combat observation, while full-run training zeroes the combat block at non-combat phases). A small Python-side change to make `_hp_ratio()` also read the now-present top-level `hp`/`max_hp` ints would close this without another mod rebuild.
- **Shop ordering is unverified.** `RunManager._actions_shop()` orders buyable items as colored cards, then colorless cards, then relics, then potions, then a "sell Foul Potion" action, then card removal. `RlShopRoomHandler.cs` instead orders by `NMerchantSlot` type via `room.Inventory.GetAllSlots()`, whose internal ordering is defined in the compiled game (not available in this repo) and could not be confirmed to match. The "sell Foul Potion" action in particular has **no bridge-JSON representation at all** in `RlShopRoomHandler.cs` — a full-run policy can never actually take that action against the real game.
- **Card bundles, `card_select` prompts, and the Crystal Sphere minigame have no representation in `run_env.py`'s action space** (its own mask/step logic never looks for `pick_card_bundle` or per-cell actions), so the trained policy never learned meaningful behavior for them. `RunStateAdapter` always takes a fixed, deterministic fallback action in these cases rather than inventing policy-driven behavior.
- **The aggregate post-combat "reward screen"** (`reward_screen`, `RlRewardsScreenHandler.cs`) offers card/potion/relic/gold rewards together with a proceed button, which does not correspond 1:1 to `run_env.py`'s single-reward-at-a-time `CARD_REWARD` semantics. It is mapped heuristically (first pickable reward vs. proceed).
- **Combat pending-choice prompts and multi-creature `select_player` are not exposed by `RlCombatHandler.cs` at all** — it only ever reports a single controllable creature and has no JSON representation of mid-combat card-selection choices (e.g. Exhume/Warcry). `run_env.py`'s `player_select` action slice (150-156) is therefore always left fully masked out by the bridge adapter. **Resolved as a non-issue for solo play (2026-07-23, verified against decompiled v0.109.0):** the Python simulator only emits `select_player` actions when more than one *player* creature is controllable (`run_manager.py` filters on `is_player`), which only happens in multiplayer co-op. Necrobinder's Osty pet is not a player creature in the real game either — it is summoned as an ally via `OstyCmd.Summon`/`PlayerCmd.AddPet<Osty>` into `combatState.Allies` with the `Osty` *monster* model, whose move state machine is a single self-looping `"NOTHING_MOVE"` that does nothing (`decompiled_v0.109.0/MegaCrit.Sts2.Core.Models.Monsters/Osty.cs`). Osty only ever acts through the owner's card/relic effects; the player never selects an acting creature in solo play, so the bridge never needs to expose this slice for the solo Necrobinder use case.
- **Boss relic ordering** is assumed to match via shared RNG-algorithm parity (this repo's RNG is built to match the decompiled game bit-for-bit) but was not independently re-verified here.

**Fix:** One concrete, provable ordering bug was fixed (see issue #5, the `RlMapHandler.cs` children-order fix). The rest are documented here rather than silently shipped as "probably fine," per design — see `sts2_env/bridge/run_state_adapter.py`'s module docstring for the same list inline with the code.

**Location:** `sts2_env/bridge/run_state_adapter.py`, `bridge_mod/RlShopRoomHandler` (in `RlNonCombatRoomHandlers.cs`), `bridge_mod/RlCombatHandler.cs`, `bridge_mod/RlRewardsScreenHandler.cs`
