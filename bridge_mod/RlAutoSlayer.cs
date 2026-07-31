// RlAutoSlayer.cs -- RL-agent-driven AutoSlayer.
//
// This is a modified version of the game's AutoSlayer that replaces
// random decision handlers with RL agent handlers communicating via TCP.
// The overall game flow (main menu, room loop, screen draining, map navigation)
// is preserved from the original AutoSlayer.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Rooms;
using MegaCrit.Sts2.Core.AutoSlay.Handlers.Screens;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Events.Custom.CrystalSphere;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Timeline;
using MegaCrit.Sts2.Core.Timeline.Epochs;

namespace STS2BridgeMod;

/// <summary>
/// RL-agent-driven AutoSlayer. Mirrors the structure of the game's built-in
/// AutoSlayer but replaces random decision handlers with RL agent handlers
/// that communicate with Python via BridgeServer TCP.
///
/// Combat, map navigation, card rewards, events, shops, rest sites, treasure,
/// and boss relic choices are bridge-driven. Most other screen handlers still
/// use AutoSlay helpers.
/// </summary>
public class RlAutoSlayer
{
    private const string MainMenuPath = "/root/Game/RootSceneContainer/MainMenu";
    private const string RestSiteProceedButtonPath =
        "/root/Game/RootSceneContainer/Run/RoomContainer/RestSiteRoom/ProceedButton";
    private const string EventRoomPath =
        "/root/Game/RootSceneContainer/Run/RoomContainer/EventRoom";
    private const string AbandonRunOptionsButtonPath =
        "/root/Game/RootSceneContainer/Run/GlobalUi/TopBar/RightAlignedStuff/Options";
    private const string AbandonRunButtonPath =
        "/root/Game/RootSceneContainer/Run/GlobalUi/CapstoneScreenContainer/OptionsScreen/AbandonRunButton";
    private const string AbandonRunProceedButtonPath =
        "/root/Game/RootSceneContainer/Run/GlobalUi/OverlayScreensContainer/GameOverScreen/UI/ProceedButton";
    private const string AbandonRunMenuButtonPath = "MainMenuTextButtons/AbandonRunButton";
    private const string AbandonPopupPrimaryYesButtonPath = "VerticalPopup/YesButton";
    private const string AbandonPopupFallbackYesButtonPath = "YesButton";
    private const string SingleplayerButtonPath = "MainMenuTextButtons/SingleplayerButton";
    private const string CharacterSelectScreenPath = "Submenus/CharacterSelectScreen";
    private const string StandardRunButtonPath = "Submenus/SingleplayerSubmenu/StandardButton";
    private const string CharacterButtonContainerPath = "CharSelectButtons/ButtonContainer";
    private const string CharacterConfirmButtonPath =
        "Submenus/CharacterSelectScreen/ConfirmButton";
    // Defaults; overridden per-run by sts2_agent_config.txt written by
    // scripts/agent_config.py. Kept as the fallback so a missing or
    // malformed config behaves exactly as before it existed.
    private const string DefaultCharacterId = "Ironclad";
    private const int DefaultAscension = 0;

    private const int DefaultRunCount = 1;

    /// <summary>
    /// Set by RlGameOverScreenHandler when the run ends (death or victory).
    /// PlayRunAsync's room loop checks it each iteration; cleared per run in
    /// RunAsync. This is the only reliable end-of-run signal -- the game-over
    /// screen is gone by the time the loop next looks.
    /// </summary>
    public static bool RunEnded { get; set; }

    private static string PreferredCharacterId => ReadConfig("character", DefaultCharacterId);
    private static int PreferredAscension
    {
        get
        {
            string raw = ReadConfig("ascension", DefaultAscension.ToString());
            return int.TryParse(raw, out int v) ? v : DefaultAscension;
        }
    }

    /// <summary>
    /// How many runs to play back-to-back before stopping (config key "runs").
    ///
    /// Previously the slayer played exactly ONE run per game launch, so any
    /// win-rate measurement needed a human to restart the game between runs --
    /// and a death aborted the session outright. Defaults to 1 so existing
    /// behaviour is unchanged unless asked for.
    /// </summary>
    private static int PreferredRunCount
    {
        get
        {
            string raw = ReadConfig("runs", DefaultRunCount.ToString());
            return int.TryParse(raw, out int v) && v > 0 ? v : DefaultRunCount;
        }
    }

    /// <summary>
    /// Read one "key=value" line from sts2_agent_config.txt, searched beside
    /// the mod assembly then the game directory. Any failure returns
    /// <paramref name="fallback"/> -- a broken config must never stop a run
    /// from starting.
    /// </summary>
    private static string ReadConfig(string key, string fallback)
    {
        try
        {
            string asmDir = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location) ?? ".";
            foreach (string dir in new[] { asmDir, System.AppContext.BaseDirectory })
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string path = System.IO.Path.Combine(dir, "sts2_agent_config.txt");
                if (!System.IO.File.Exists(path)) continue;
                foreach (string line in System.IO.File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!t.Substring(0, eq).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                        continue;
                    string value = t.Substring(eq + 1).Trim();
                    if (value.Length > 0) return value;
                }
            }
        }
        catch { }
        return fallback;
    }
    // Runaway guard only. Act 4 (Heart) sits past floor 50, so a fixed
    // "final floor" would cut a winning run short before its last act.
    private const int AbsoluteMaxFloor = 200;
    // Runs now play to a win or a death rather than stopping at a floor
    // number, and a full A0 run with a 90s planner budget per combat can
    // outlast an hour. Too tight a cap would abort exactly the deep runs we
    // most want to see finish.
    private const int RunTimeoutMinutes = 180;
    private const int RunStateTimeoutSeconds = 60;
    private const int RoomAssignmentTimeoutSeconds = 60;
    private const int NonCombatSettleDelayMs = 500;
    // Post-boss transitions were 10s and 5s -- far tighter than every sibling
    // timeout in this class (run state 60, room assignment 60, main menu 30),
    // and too tight for the real thing: the boss death animation, reward
    // screens and the act-change sequence together routinely exceed 10s.
    // Measured live 2026-07-30: a run that CLEARED the Act 1 boss then failed
    // with "Act transition did not start after boss", so beating the boss was
    // scored as a failed run and Act 2 was never reached.
    //
    // Capped at 25s rather than raised to 60: the watchdog is reset
    // immediately before these waits and fires at 30s of no progress, so a
    // longer timeout would simply trade this failure for a watchdog abort.
    private const int BossTransitionTimeoutSeconds = 25;
    private const int ActTransitionTimeoutSeconds = 25;
    // Long enough for an overlay's intro tween to finish and enable its
    // buttons (NWheelSpinScreen bounces in over ~1s then enables), short
    // enough to stay inside the watchdog window, which is reset each poll.
    private const int UnknownScreenDismissTimeoutSeconds = 10;
    // Long enough for a screen to play its outro. NWheelSpinScreen spins for
    // ~2s then holds ~1s before bouncing out, so a short wait would return
    // mid-animation and re-enter the drain loop on a screen with every button
    // disabled.
    private const int UnknownScreenCloseTimeoutSeconds = 15;
    private const int OverlayCloseRetryLimit = 3;
    private const int OverlayDrainSettleDelayMs = 100;
    private const int EventProceedTimeoutSeconds = 5;
    private const int RewardsScreenTimeoutSeconds = 10;
    private const int MainMenuTimeoutSeconds = 30;
    private const int AbandonPopupTimeoutSeconds = 5;
    private const int AbandonRunSettleDelayMs = 1000;
    private const int MenuClickSettleDelayMs = 500;
    private const int CharacterSelectDelayMs = 100;
    private readonly Dictionary<RoomType, IRoomHandler> _roomHandlers;
    private readonly Dictionary<Type, IScreenHandler> _screenHandlers;
    private readonly RlMapHandler _mapHandler;

    private CancellationTokenSource? _cts;
    private Rng? _random;
    private Watchdog? _watchdog;
    private IDisposable? _cardSelectorScope;
    private bool _completionSignalSent;

    public static bool IsActive { get; private set; }

    /// <summary>
    /// Public watchdog for use by handlers. Since we can't set
    /// AutoSlayer.CurrentWatchdog (private setter), we expose our own.
    /// </summary>
    public static Watchdog? CurrentWatchdog { get; private set; }

    public RlAutoSlayer()
    {
        // Use our RL combat handler for all combat room types
        var combatHandler = new RlCombatHandler();
        _roomHandlers = new Dictionary<RoomType, IRoomHandler>
        {
            [RoomType.Monster] = combatHandler,
            [RoomType.Elite] = combatHandler,
            [RoomType.Boss] = combatHandler,
            [RoomType.Event] = new RlEventRoomHandler(),
            [RoomType.Shop] = new RlShopRoomHandler(),
            [RoomType.Treasure] = new RlTreasureRoomHandler(),
            [RoomType.RestSite] = new RlRestSiteRoomHandler(),
        };

        _mapHandler = new RlMapHandler();

        _screenHandlers = new Dictionary<Type, IScreenHandler>
        {
            [typeof(NRewardsScreen)] = new RlRewardsScreenHandler(),
            [typeof(NCardRewardSelectionScreen)] = new RlCardRewardScreenHandler(),
            [typeof(NDeckUpgradeSelectScreen)] = new DeckUpgradeScreenHandler(),
            [typeof(NDeckTransformSelectScreen)] = new DeckTransformScreenHandler(),
            [typeof(NDeckEnchantSelectScreen)] = new DeckEnchantScreenHandler(),
            [typeof(NDeckCardSelectScreen)] = new DeckCardSelectScreenHandler(),
            [typeof(NSimpleCardSelectScreen)] = new SimpleCardSelectScreenHandler(),
            [typeof(NChooseACardSelectionScreen)] = new ChooseACardScreenHandler(),
            [typeof(NChooseABundleSelectionScreen)] = new RlCardBundleScreenHandler(),
            [typeof(NChooseARelicSelection)] = new RlChooseARelicScreenHandler(),
            [typeof(NGameOverScreen)] = new RlGameOverScreenHandler(),
            [typeof(NCrystalSphereScreen)] = new RlCrystalSphereScreenHandler(),
        };
    }

    public void Start(string seed, string? logFile = null)
    {
        if (logFile != null)
        {
            AutoSlayLog.OpenLogFile(logFile);
        }
        IsActive = true;
        SetAutoSlayerActive(true);
        _cts = new CancellationTokenSource();
        Task task = RunAsync(seed, _cts.Token);
        TaskHelper.RunSafely(task);
    }

    public void Stop()
    {
        IsActive = false;
        SetAutoSlayerActive(false);
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// Set AutoSlayer.IsActive via NonInteractiveMode.AutoSlayerCheck.
    /// The original AutoSlayer static constructor wires this up, but since
    /// we're running our own slayer, we set it directly to report our state.
    /// </summary>
    private static void SetAutoSlayerActive(bool active)
    {
        NonInteractiveMode.AutoSlayerCheck = () => active;
    }

    private async Task RunAsync(string seed, CancellationToken ct)
    {
        // PLAY N RUNS BACK-TO-BACK.
        //
        // This used to play exactly one run and then tear everything down, so
        // a win-rate measurement needed a human to restart the game between
        // runs -- and because a single failure propagated straight to the
        // outer teardown, ONE death ended the whole session. Both together
        // made unattended measurement impossible, which is the thing a 50%
        // win-rate target actually requires.
        //
        // Each run is isolated: its own seed, its own completion signal, and
        // its own catch so a failed run costs one run rather than the session.
        int runs = PreferredRunCount;
        int completed = 0, failed = 0;
        Logger.Log($"[RlAutoSlayer] Session starting: {runs} run(s), base seed {seed}");
        try
        {
            for (int i = 0; i < runs; i++)
            {
                if (ct.IsCancellationRequested) break;
                string runSeed = i == 0 ? seed : $"{seed}-{i}";
                _completionSignalSent = false;
                RunEnded = false;
                Logger.Log($"[RlAutoSlayer] === Run {i + 1}/{runs} starting, seed: {runSeed} ===");
                try
                {
                    await WaitHelper.WithTimeout(
                        (CancellationToken token) => PlayRunAsync(runSeed, token),
                        TimeSpan.FromMinutes(RunTimeoutMinutes),
                        ct);
                    completed++;
                    Logger.Log($"[RlAutoSlayer] === Run {i + 1}/{runs} completed, seed: {runSeed} ===");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    failed++;
                    Logger.Log($"[RlAutoSlayer] === Run {i + 1}/{runs} FAILED: {ex.Message} -- "
                               + "continuing to the next run ===");
                }
                finally
                {
                    // Per-run teardown only. The card selector scope is
                    // re-installed by PlayRunAsync, so it must be released
                    // between runs or the next run stacks another one.
                    _cardSelectorScope?.Dispose();
                    _cardSelectorScope = null;

                    // Exactly one completion signal per run, so the Python
                    // side sees a clean run boundary rather than silence.
                    if (!_completionSignalSent)
                    {
                        BridgeServer.Instance.SendState(RunCompleteState(
                            NonCombatBridgeProtocol.TerminatedResult));
                        _completionSignalSent = true;
                    }
                }
            }
        }
        finally
        {
            IsActive = false;
            SetAutoSlayerActive(false);
            CurrentWatchdog = null;
            SetSharedCurrentWatchdog(null);
            _watchdog = null;
            _cardSelectorScope?.Dispose();
            _cardSelectorScope = null;
            AutoSlayLog.CloseLogFile();
            Logger.Log($"[RlAutoSlayer] Session finished: {completed} completed, "
                       + $"{failed} failed, of {runs} requested.");
        }
    }

    private async Task PlayRunAsync(string seed, CancellationToken ct)
    {
        await WaitHelper.Until(() => NGame.Instance != null, ct,
            AutoSlayConfig.gameInitTimeout, "Game instance not initialized");

        NGame.Instance.DebugSeedOverride = seed;
        SaveManager.Instance.PrefsSave.FastMode = FastModeType.Fast;
        SaveManager.Instance.SetFtuesEnabled(enabled: false);

        // Unlock all epochs
        SaveManager.Instance.ObtainEpochOverride(
            EpochModel.GetId<Silent1Epoch>(), EpochState.Revealed);
        SaveManager.Instance.ObtainEpochOverride(
            EpochModel.GetId<Regent1Epoch>(), EpochState.Revealed);
        SaveManager.Instance.ObtainEpochOverride(
            EpochModel.GetId<Defect1Epoch>(), EpochState.Revealed);
        SaveManager.Instance.ObtainEpochOverride(
            EpochModel.GetId<Necrobinder1Epoch>(), EpochState.Revealed);

        _random = new Rng((uint)StringHelper.GetDeterministicHashCode(seed));

        // Install our RL card selector for deck upgrade/transform/card selection screens
        _cardSelectorScope = CardSelectCmd.UseSelector(new RlCardSelector());

        RaiseWatchdogTimeout(TimeSpan.FromSeconds(120));

        _watchdog = new Watchdog();
        CurrentWatchdog = _watchdog;
        SetSharedCurrentWatchdog(_watchdog);
        _watchdog.Reset("Playing main menu");

        // Runs after the first inherit whatever state the previous run left
        // behind, so get back to a known screen before assuming one.
        await RecoverToMainMenuAsync(ct);

        await PlayMainMenuAsync(ct);

        await WaitHelper.Until(
            () => RunManager.Instance.DebugOnlyGetState() != null, ct,
            TimeSpan.FromSeconds(RunStateTimeoutSeconds), "Run state not initialized");

        RunState runState = RunManager.Instance.DebugOnlyGetState();
        Logger.Log($"[RlAutoSlayer] RunState available. Floor: {runState.TotalFloor}");

        await WaitHelper.Until(
            () => {
                var room = runState.CurrentRoom;
                if (room != null)
                    Logger.Log($"[RlAutoSlayer] Waiting for room... type={room.RoomType}");
                return room != null && room.RoomType != RoomType.Unassigned;
            },
            ct, TimeSpan.FromSeconds(RoomAssignmentTimeoutSeconds), "Room type not assigned");

        // Main game loop
        // PLAY THE RUN OUT -- to a win or a death, not to a floor number.
        //
        // This used to stop at FinalRunFloor and abandon, which threw away
        // the end of every successful run: the agent could never finish an
        // act 3 boss, never reach act 4, and a "completed" run told us
        // nothing about whether it would have won. The run now ends the way
        // the game ends it, and the RunEnded checks inside the loop are what
        // terminate it.
        //
        // AbsoluteMaxFloor is a runaway guard, not a target: it exists so a
        // room loop that somehow stops advancing cannot spin forever.
        while (runState.TotalFloor < AbsoluteMaxFloor)
        {
            ct.ThrowIfCancellationRequested();

            // THE RUN CAN END INSIDE THE LOOP, and a dead run has no current
            // room. Reading CurrentRoom.RoomType then throws
            // NullReferenceException, which is what failed run 1 of the first
            // multi-run session: the death-path fix let
            // WaitForRewardsScreenAsync return on NGameOverScreen, and control
            // fell straight back to here with CurrentRoom already null.
            //
            // A game-over screen means this run is over -- leave the loop
            // cleanly so RunAsync records a completed run and starts the next
            // one, instead of unwinding through an exception.
            if (RunEnded)
            {
                Logger.Log("[RlAutoSlayer] Run ended (game over handled) -- "
                           + "leaving the room loop");
                break;
            }
            if (NOverlayStack.Instance?.Peek() is NGameOverScreen)
            {
                Logger.Log("[RlAutoSlayer] Game over screen up -- ending this run");
                break;
            }
            if (runState.CurrentRoom == null)
            {
                Logger.Log("[RlAutoSlayer] No current room (run ended) -- ending this run");
                break;
            }

            RoomType roomType = runState.CurrentRoom.RoomType;
            _watchdog.Reset(
                $"Entering {roomType} room (Act {runState.CurrentActIndex + 1}, Floor {runState.ActFloor})");
            Logger.Log(
                $"[RlAutoSlayer] Entering {roomType} (Act {runState.CurrentActIndex + 1}, Floor {runState.ActFloor})");

            await HandleRoomAsync(roomType, ct);

            // After combat rooms, wait for rewards screen
            if (roomType == RoomType.Monster || roomType == RoomType.Elite ||
                roomType == RoomType.Boss)
            {
                await WaitForRewardsScreenAsync(ct);
            }
            else
            {
                await Task.Delay(NonCombatSettleDelayMs, ct);
            }

            await DrainOverlayScreensAsync(ct);

            // THE RUN CAN END MID-ITERATION -- check here, not just at the top.
            //
            // Dying to a room's combat sets RunEnded while the game-over screen
            // is handled inside the rewards wait / screen drain above. The
            // loop-top check has already run by then, so without this the rest
            // of the body executes against a run that no longer exists. Dying
            // to a BOSS was the visible case: the boss branch below sat waiting
            // the full BossTransitionTimeoutSeconds for an act transition that
            // could never come, and an ordinary boss death was scored as
            // "Act transition did not start after boss".
            if (RunEnded)
            {
                Logger.Log("[RlAutoSlayer] Run ended during this room -- "
                           + "leaving the room loop");
                break;
            }

            if (roomType == RoomType.RestSite)
            {
                await ClickRestSiteProceedIfNeeded(ct);
            }
            if (roomType == RoomType.Event)
            {
                await ClickEventProceedIfNeeded(ct);
            }

            // Boss room: handle act transition
            if (roomType == RoomType.Boss)
            {
                _watchdog.Reset("Waiting for act transition after boss");
                RoomType postBossRoomType = RoomType.Boss;
                await WaitHelper.Until(delegate
                {
                    AbstractRoom currentRoom = runState.CurrentRoom;
                    if (currentRoom == null) return false;
                    postBossRoomType = currentRoom.RoomType;
                    return postBossRoomType != RoomType.Boss;
                }, ct, TimeSpan.FromSeconds(BossTransitionTimeoutSeconds),
                    "Act transition did not start after boss");

                Logger.Log($"[RlAutoSlayer] Post-boss transition: room type is now {postBossRoomType}");

                if (postBossRoomType == RoomType.Event &&
                    runState.CurrentActIndex >= runState.Acts.Count - 1)
                {
                    _watchdog.Reset(
                        $"Entering {postBossRoomType} room (Act {runState.CurrentActIndex + 1}, Floor {runState.ActFloor})");
                    await HandleRoomAsync(postBossRoomType, ct);
                    await Task.Delay(NonCombatSettleDelayMs, ct);
                    await DrainOverlayScreensAsync(ct);
                    _watchdog.Reset("Waiting for main menu after victory");
                    await WaitForMainMenuAsync(ct);
                    Logger.Log("[RlAutoSlayer] Victory! Run completed and returned to main menu");

                    // Notify Python of victory
                    BridgeServer.Instance.SendState(RunCompleteState(
                        NonCombatBridgeProtocol.VictoryResult));
                    _completionSignalSent = true;
                    return;
                }

                await WaitHelper.Until(
                    () => runState.VisitedMapCoords.Count == 0, ct,
                    TimeSpan.FromSeconds(ActTransitionTimeoutSeconds),
                    "Act transition did not complete (VisitedMapCoords not cleared)");
            }

            _watchdog.Reset("Navigating map");
            await _mapHandler.HandleAsync(_random, ct);
        }

        // The room loop now exits for three reasons, and only ONE of them
        // leaves a run to abandon: hitting the runaway guard. If the run already
        // ended (death or victory), the Run node and its whole GlobalUi
        // subtree are gone, so AbandonRunAsync's UI lookups throw --
        // "Node /root/Game/RootSceneContainer/Run/GlobalUi/TopBar/
        // RightAlignedStuff/Options of type NButton not found", which scored
        // an ordinary death as a FAILED run.
        if (RunEnded)
        {
            Logger.Log("[RlAutoSlayer] Run already ended (game over handled) "
                       + "-- nothing to abandon");
            return;
        }

        Logger.Log($"[RlAutoSlayer] Room loop hit the {AbsoluteMaxFloor}-floor "
                   + "runaway guard without the run ending. Abandoning");
        await AbandonRunAsync(ct);
    }

    private async Task HandleRoomAsync(RoomType roomType, CancellationToken ct)
    {
        if (!_roomHandlers.TryGetValue(roomType, out IRoomHandler handler))
        {
            Logger.Log($"[RlAutoSlayer] No handler for room type: {roomType}");
            return;
        }
        await WaitHelper.WithTimeout(
            (CancellationToken token) => handler.HandleAsync(_random, token),
            handler.Timeout, ct);
    }

    private async Task DrainOverlayScreensAsync(CancellationToken ct)
    {
        if (NOverlayStack.Instance == null)
        {
            await WaitHelper.Until(() => NOverlayStack.Instance != null, ct,
                AutoSlayConfig.nodeWaitTimeout, "Overlay stack not initialized");
        }

        HashSet<IOverlayScreen> handledScreens = new HashSet<IOverlayScreen>();
        int consecutiveFailures = 0;

        while (true)
        {
            NOverlayStack? instance = NOverlayStack.Instance;
            if (instance == null || instance.ScreenCount <= 0)
                break;

            ct.ThrowIfCancellationRequested();

            IOverlayScreen currentOverlay = NOverlayStack.Instance.Peek();
            if (currentOverlay == null)
                break;

            if (handledScreens.Contains(currentOverlay))
            {
                consecutiveFailures++;
                if (consecutiveFailures >= OverlayCloseRetryLimit)
                {
                    Logger.Log(
                        $"[RlAutoSlayer] Infinite loop: screen {currentOverlay.GetType().Name} not closing after {OverlayCloseRetryLimit} attempts");
                    throw new InvalidOperationException(
                        "Screen " + currentOverlay.GetType().Name + " not closing after being handled");
                }
            }
            else
            {
                handledScreens.Add(currentOverlay);
                consecutiveFailures = 0;
            }

            Node node = (Node)currentOverlay;
            Type type = node.GetType();

            if (!_screenHandlers.TryGetValue(type, out IScreenHandler handler))
            {
                // NO DEDICATED HANDLER -- TRY A GENERIC DISMISSAL.
                //
                // Breaking here leaves the screen on the stack forever: the
                // room never finishes, the watchdog fires at 30s and the run
                // is scored FAILED. Measured live 2026-07-30 on
                // NWheelSpinScreen (an ActsFromThePast minigame) -- 6 of 8
                // runs in one session died to a screen whose only requirement
                // was one click on a proceed button.
                //
                // Deliberately generic rather than a per-screen handler: the
                // mods add screens we do not know about, and a screen that
                // needs one click should never cost a run. A dedicated
                // handler is still preferable wherever the CHOICE matters --
                // this only exists to keep the run alive.
                if (await TryDismissUnknownScreenAsync(node, type, ct))
                {
                    await Task.Delay(OverlayDrainSettleDelayMs, ct);
                    continue;
                }
                Logger.Log($"[RlAutoSlayer] No handler for screen type: "
                           + $"{type.Name}, and nothing clickable on it");
                break;
            }

            _watchdog?.Reset("Handling screen: " + type.Name);
            Logger.Log($"[RlAutoSlayer] Handling screen: {type.Name}");
            await WaitHelper.WithTimeout(
                (CancellationToken token) => handler.HandleAsync(_random, token),
                handler.Timeout, ct);

            if (currentOverlay is NRewardsScreen &&
                (NMapScreen.Instance?.IsOpen ?? false))
            {
                break;
            }

            await Task.Delay(OverlayDrainSettleDelayMs, ct);
        }
    }

    /// <summary>
    /// Click something -- anything enabled -- on a screen we have no handler
    /// for, so an unknown overlay costs a click instead of the whole run.
    ///
    /// Polls rather than checking once: these screens animate in, and their
    /// buttons are typically created disabled and enabled at the end of the
    /// intro tween (NWheelSpinScreen does exactly that). A single check on
    /// arrival finds nothing enabled and gives up a few hundred milliseconds
    /// too early.
    /// </summary>
    private async Task<bool> TryDismissUnknownScreenAsync(
        Node screen, Type type, CancellationToken ct)
    {
        DateTime deadline = DateTime.UtcNow
            + TimeSpan.FromSeconds(UnknownScreenDismissTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            // RE-CHECK VALIDITY EVERY ITERATION.
            //
            // We poll this screen for up to 10s and search its children each
            // time. Godot can free it in that window -- these screens close
            // themselves -- and touching a freed node throws
            // "InvalidOperationException: Handle is not initialized" from
            // DelegateUtils.DelegateHash, which takes down the whole GAME, not
            // just the run. Measured live: a 20-run session died at run 3 with
            // that exception raised through DrainOverlayScreensAsync.
            //
            // A screen that no longer exists has been dismissed by definition.
            if (!GodotObject.IsInstanceValid(screen) || !screen.IsInsideTree())
            {
                Logger.Log($"[RlAutoSlayer] {type.Name} closed on its own "
                           + "while we were looking at it");
                return true;
            }

            _watchdog?.Reset($"Dismissing unhandled screen {type.Name}");

            // Prefer an explicit proceed button; it is what these screens
            // expect and is unambiguous when several controls are present.
            NProceedButton proceed = UiHelper.FindFirst<NProceedButton>(screen);
            if (proceed != null && GodotObject.IsInstanceValid(proceed)
                    && proceed.IsEnabled && ((Control)proceed).IsVisibleInTree())
            {
                Logger.Log($"[RlAutoSlayer] {type.Name}: clicking its proceed button");
                await UiHelper.Click(proceed);
                await WaitForScreenToCloseAsync(screen, type, ct);
                return true;
            }

            NButton button = UiHelper.FindAll<NButton>(screen)
                .FirstOrDefault(b => GodotObject.IsInstanceValid(b) && b.IsEnabled
                                     && ((Control)b).IsVisibleInTree());
            if (button != null)
            {
                Logger.Log($"[RlAutoSlayer] {type.Name}: no handler, clicking "
                           + $"its first enabled button");
                await UiHelper.Click(button);
                await WaitForScreenToCloseAsync(screen, type, ct);
                return true;
            }

            await Task.Delay(250, ct);
        }
        return false;
    }

    /// <summary>
    /// After clicking something on an unhandled screen, give it time to act.
    ///
    /// These screens usually PLAY something before closing: NWheelSpinScreen
    /// disables its button, spins for ~2s, pauses, then bounces out. Returning
    /// the instant the click lands meant the drain loop re-entered mid-spin,
    /// found the button disabled, concluded "nothing clickable on it" and gave
    /// up -- failing the run on a screen that was seconds from closing itself.
    /// Measured live: run 1 of a Silent batch died exactly this way.
    /// </summary>
    private async Task WaitForScreenToCloseAsync(
        Node screen, Type type, CancellationToken ct)
    {
        DateTime deadline = DateTime.UtcNow
            + TimeSpan.FromSeconds(UnknownScreenCloseTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (!GodotObject.IsInstanceValid(screen) || !screen.IsInsideTree())
                return;
            IOverlayScreen top = NOverlayStack.Instance?.Peek();
            if (!ReferenceEquals(top, screen))
                return;
            _watchdog?.Reset($"Waiting for {type.Name} to close");
            await Task.Delay(250, ct);
        }
        Logger.Log($"[RlAutoSlayer] {type.Name} still up after "
                   + $"{UnknownScreenCloseTimeoutSeconds}s; continuing anyway");
    }

    private async Task ClickRestSiteProceedIfNeeded(CancellationToken ct)
    {
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        NProceedButton nodeOrNull = root.GetNodeOrNull<NProceedButton>(
            RestSiteProceedButtonPath);
        if (nodeOrNull != null && nodeOrNull.IsEnabled)
        {
            Logger.Log("[RlAutoSlayer] Clicking rest site proceed button");
            await UiHelper.Click(nodeOrNull);
        }
    }

    private async Task ClickEventProceedIfNeeded(CancellationToken ct)
    {
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        Node eventRoom = root.GetNodeOrNull(EventRoomPath);
        if (eventRoom == null)
            return;

        NEventOptionButton proceedOption = null;
        await WaitHelper.Until(delegate
        {
            NMapScreen? instance = NMapScreen.Instance;
            if (instance != null && instance.IsOpen) return true;

            List<NEventOptionButton> list = (from o in UiHelper.FindAll<NEventOptionButton>(eventRoom)
                where !o.Option.IsLocked && o.Option.IsProceed
                select o).ToList();
            if (list.Count > 0)
            {
                proceedOption = list[0];
                return true;
            }
            return false;
        }, ct, TimeSpan.FromSeconds(EventProceedTimeoutSeconds),
            "Event proceed option or map did not appear");

        if (proceedOption != null)
        {
            Logger.Log("[RlAutoSlayer] Clicking event proceed option");
            await UiHelper.Click(proceedOption);
        }
    }

    private async Task WaitForRewardsScreenAsync(CancellationToken ct)
    {
        Logger.Log("[RlAutoSlayer] Waiting for rewards screen");
        await WaitHelper.Until(
            // A LOST combat shows NGameOverScreen -- never a rewards screen and
            // never the map. Without it in this predicate a death timed out
            // here and threw AutoSlayTimeoutException("Rewards screen did not
            // appear after combat"), which aborted RunAsync entirely. That was
            // not cosmetic: RunAsync does not start another run, so after ANY
            // death the game had to be restarted by hand, making unattended
            // multi-run win-rate measurement impossible. Observed live
            // 2026-07-30 after a death to ACTSFROMTHEPAST-SENTRY.
            //
            // NGameOverScreen already has a handler (RlGameOverScreenHandler in
            // the screen-handler table), so satisfying the wait is all that is
            // needed -- the normal dispatch then drives the game-over flow.
            // FOURTH CONDITION: the game is already back at the main menu.
            //
            // The game-over screen is transient. On a death the game can run
            // the whole game-over -> main-menu sequence before this predicate
            // is first evaluated, and then NONE of the three conditions above
            // will EVER become true -- the screen we were told to wait for has
            // been and gone. The wait then burns its full timeout and throws,
            // scoring an ordinary death as a FAILED run.
            //
            // Measured live 2026-07-30 (session 10): runs 2, 3 and 4 each died
            // and each failed with "Neither rewards screen, map, nor game-over
            // screen appeared after combat", with "[Startup] Time to main menu"
            // logged BEFORE the combat-finished line in every case. Run 1 died
            // too and passed -- the only difference was that its game-over
            // screen was still up when we looked. A race, not a state.
            //
            // Being at the main menu means the run is over, so set RunEnded the
            // way RlGameOverScreenHandler would have: downstream code keys off
            // that flag to skip the abandon path and to leave the room loop.
            () => {
                if (NOverlayStack.Instance?.Peek() is NRewardsScreen) return true;
                if (NOverlayStack.Instance?.Peek() is NGameOverScreen) return true;
                if (NMapScreen.Instance?.IsOpen ?? false) return true;
                if (IsAtMainMenu())
                {
                    Logger.Log("[RlAutoSlayer] Back at the main menu after "
                               + "combat -- the run ended and the game-over "
                               + "screen was missed");
                    RunEnded = true;
                    return true;
                }
                return false;
            },
            ct, TimeSpan.FromSeconds(RewardsScreenTimeoutSeconds),
            "Neither rewards screen, map, nor game-over screen appeared after combat");
    }

    /// <summary>
    /// True when the main menu node exists and is visible. Deliberately
    /// GetNodeOrNull: the node is absent (not merely hidden) for the whole
    /// duration of a run, and a hard lookup would throw rather than answer.
    /// </summary>
    private static bool IsAtMainMenu()
    {
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        return root.GetNodeOrNull<Control>(MainMenuPath)?.IsVisibleInTree() ?? false;
    }

    /// <summary>
    /// Put the game back at the main menu before starting a run.
    ///
    /// PlayMainMenuAsync assumes the main menu is already up, which is true
    /// for the first run of a session and false after any run that ended
    /// badly. When a run FAILS mid-run the game is left wherever it was --
    /// usually still inside the run, where the MainMenu node does not exist at
    /// all -- so the next run's PlayMainMenuAsync waits the full 30s for a
    /// node that cannot appear and fails too. That is self-sustaining: one bad
    /// run poisons every run after it.
    ///
    /// Measured live 2026-07-30 (session 10): 1 completed, 7 failed. After the
    /// first three deaths, runs 5 and 7 failed with "Node /root/Game/
    /// RootSceneContainer/MainMenu of type Control not found" and runs 6 and 8
    /// with "Watchdog timeout: No progress for 30.0s. Last activity: Playing
    /// main menu" -- four failures that were purely inherited state.
    ///
    /// Best effort by design: if recovery cannot get us there, say so and let
    /// PlayMainMenuAsync produce the real error.
    /// </summary>
    private async Task RecoverToMainMenuAsync(CancellationToken ct)
    {
        if (IsAtMainMenu())
            return;

        Logger.Log("[RlAutoSlayer] Not at the main menu -- recovering before "
                   + "starting the run");

        // Recovery legitimately outlasts the watchdog's 30s no-progress
        // window (an abandon has its own confirmation popup and settle
        // delays), so keep it fed rather than letting it abort the run we are
        // in the middle of rescuing.
        _watchdog?.Reset("Recovering to the main menu");

        // Overlays and modals sit on top of everything and swallow clicks.
        try
        {
            await DrainOverlayScreensAsync(ct);
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Overlay drain during recovery failed: {ex.Message}");
        }

        if (IsAtMainMenu())
            return;

        // Still not there: a run is very likely still live. Abandon it, which
        // is the only route from inside a run back to the menu.
        if (RunManager.Instance?.DebugOnlyGetState() != null)
        {
            Logger.Log("[RlAutoSlayer] A run is still active -- abandoning it");
            _watchdog?.Reset("Abandoning the previous run");
            try
            {
                await AbandonRunAsync(ct);
            }
            catch (Exception ex)
            {
                Logger.Log($"[RlAutoSlayer] Abandon during recovery failed: {ex.Message}");
            }
        }

        _watchdog?.Reset("Waiting for the main menu after recovery");
        try
        {
            await WaitForMainMenuAsync(ct);
            return;
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Recovery could not reach the main menu "
                       + $"through the UI: {ex.Message}");
        }

        // LAST RESORT: ask the GAME to go back, rather than clicking at it.
        //
        // Every UI route depends on a button being present, and when a run is
        // wedged the buttons are exactly what is missing -- with the map
        // screen up the top bar has no Options entry, so AbandonRunAsync has
        // nothing to click. Without a non-UI fallback the failure CASCADES:
        // one wedged run leaves the game mid-run, and every subsequent run
        // dies in recovery. Measured live: an event that would not advance
        // took down runs 2 through 6 of a 10-run batch, each with the same
        // "Waiting for the main menu after recovery" watchdog timeout.
        //
        // NGame.ReturnToMainMenuAfterRun is public and is what the game itself
        // calls when a run finishes, so it tears the run down the supported
        // way instead of leaving half-freed state behind.
        try
        {
            Logger.Log("[RlAutoSlayer] Falling back to "
                       + "NGame.ReturnToMainMenuAfterRun()");
            _watchdog?.Reset("Returning to the main menu programmatically");
            await NGame.Instance.ReturnToMainMenuAfterRun();
            await WaitForMainMenuAsync(ct);
            Logger.Log("[RlAutoSlayer] Programmatic return to the main menu "
                       + "succeeded");
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Programmatic return also failed: "
                       + $"{ex.Message}. The next run will very likely fail too.");
        }
    }

    private async Task WaitForMainMenuAsync(CancellationToken ct)
    {
        Logger.Log("[RlAutoSlayer] Waiting for main menu");
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        await WaitHelper.Until(
            () => root.GetNodeOrNull<Control>(MainMenuPath)?.IsVisibleInTree() ?? false,
            ct, TimeSpan.FromSeconds(MainMenuTimeoutSeconds),
            "Main menu did not appear after game over");
    }

    private async Task PlayMainMenuAsync(CancellationToken ct)
    {
        Logger.Log("[RlAutoSlayer] Playing main menu");
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        Control mainMenu = await WaitHelper.ForNode<Control>(
            root, MainMenuPath, ct, TimeSpan.FromSeconds(MainMenuTimeoutSeconds));

        // Abandon existing run if present (best effort)
        try
        {
            NButton abandonBtn = mainMenu.GetNodeOrNull<NButton>(
                AbandonRunMenuButtonPath);
            if (abandonBtn != null && abandonBtn.Visible)
            {
                Logger.Log("[RlAutoSlayer] Abandoning existing run");
                await UiHelper.Click(abandonBtn);
                await Task.Delay(MenuClickSettleDelayMs, ct);
                // Try to find and click Yes on the confirmation popup
                try
                {
                    await WaitHelper.Until(
                        () => NModalContainer.Instance?.OpenModal != null, ct,
                        TimeSpan.FromSeconds(AbandonPopupTimeoutSeconds), "Abandon popup");
                    Node popup = (Node)NModalContainer.Instance.OpenModal;
                    NButton yesBtn = popup.GetNodeOrNull<NButton>(AbandonPopupPrimaryYesButtonPath)
                        ?? popup.GetNodeOrNull<NButton>(AbandonPopupFallbackYesButtonPath);
                    if (yesBtn != null)
                    {
                        await UiHelper.Click(yesBtn);
                        await Task.Delay(MenuClickSettleDelayMs, ct);
                    }
                }
                catch
                {
                    Logger.Log("[RlAutoSlayer] Popup not found, trying to continue anyway");
                }
                await Task.Delay(AbandonRunSettleDelayMs, ct);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Could not abandon run: {ex.Message}, continuing...");
        }

        // Click singleplayer
        NButton spButton = mainMenu.GetNode<NButton>(
            SingleplayerButtonPath);
        Logger.Log("[RlAutoSlayer] Clicking singleplayer");
        await UiHelper.Click(spButton);

        // Navigate to character select
        Control charSelectScreen = mainMenu.GetNodeOrNull<Control>(
            CharacterSelectScreenPath);
        NButton standardButton = mainMenu.GetNodeOrNull<NButton>(
            StandardRunButtonPath);
        await WaitHelper.Until(delegate
        {
            charSelectScreen = mainMenu.GetNodeOrNull<Control>(
                CharacterSelectScreenPath);
            standardButton = mainMenu.GetNodeOrNull<NButton>(
                StandardRunButtonPath);
            bool csVisible = charSelectScreen?.Visible ?? false;
            bool sbVisible = standardButton?.Visible ?? false;
            return csVisible || sbVisible;
        }, ct, AutoSlayConfig.nodeWaitTimeout,
            "Neither CharacterSelectScreen nor SingleplayerSubmenu became visible");

        if (standardButton?.Visible ?? false)
        {
            Control csCtrl = charSelectScreen;
            if (csCtrl == null || !csCtrl.Visible)
            {
                Logger.Log("[RlAutoSlayer] Clicking standard run");
                await UiHelper.Click(standardButton);
                await WaitHelper.Until(
                    () => mainMenu.GetNodeOrNull<Control>(
                        CharacterSelectScreenPath)?.Visible ?? false,
                    ct, AutoSlayConfig.nodeWaitTimeout,
                    "CharacterSelectScreen did not become visible");
                charSelectScreen = mainMenu.GetNode<Control>(
                    CharacterSelectScreenPath);
            }
        }

        // Select the preferred character (agent is trained on this character)
        Node buttonContainer = charSelectScreen.GetNode(
            CharacterButtonContainerPath);
        List<NCharacterSelectButton> buttons =
            UiHelper.FindAll<NCharacterSelectButton>(buttonContainer);
        foreach (NCharacterSelectButton btn in buttons)
        {
            btn.UnlockIfPossible();
        }
        List<NCharacterSelectButton> available =
            buttons.Where(b => !b.IsLocked).ToList();

        // Pick the preferred character instead of random
        NCharacterSelectButton selectedChar = available.FirstOrDefault(
            b => b.Character.Id.Entry.Contains(PreferredCharacterId,
                StringComparison.OrdinalIgnoreCase))
            ?? available.First();
        Logger.Log($"[RlAutoSlayer] Selecting character: {selectedChar.Character.Id}");
        selectedChar.Select();
        await Task.Delay(CharacterSelectDelayMs, ct);

        // Force the preferred ascension level (clamped to what is unlocked)
        if (charSelectScreen is NCharacterSelectScreen selectScreen
            && selectScreen.Lobby != null)
        {
            StartRunLobby lobby = selectScreen.Lobby;
            int target = Math.Min(PreferredAscension, lobby.MaxAscension);
            if (lobby.Ascension != target)
            {
                lobby.SyncAscensionChange(target);
            }
            Logger.Log(
                $"[RlAutoSlayer] Ascension set to {lobby.Ascension} " +
                $"(max unlocked: {lobby.MaxAscension})");
        }
        else
        {
            Logger.Log("[RlAutoSlayer] Could not access lobby to set ascension");
        }

        NButton confirmBtn = await WaitHelper.ForNode<NButton>(
            mainMenu, CharacterConfirmButtonPath, ct);
        Logger.Log("[RlAutoSlayer] Confirming character");
        await UiHelper.Click(confirmBtn);
    }

    private async Task AbandonRunAsync(CancellationToken ct)
    {
        Node root = ((SceneTree)Engine.GetMainLoop()).Root;
        await Task.Delay(AbandonRunSettleDelayMs, ct);

        // The Options button is not always in the top bar. Observed live with
        // the map screen up: RightAlignedStuff held only SaveIndicator,
        // Padding, TimerContainer and Map. WaitHelper.ForNode then waits its
        // full timeout and throws, which turned "recover before the next run"
        // into a second stall on top of the one being recovered from.
        //
        // Nothing to click means nothing to abandon, so say so and let the
        // caller move on -- recovery is best-effort by design.
        NButton options = root.GetNodeOrNull<NButton>(AbandonRunOptionsButtonPath);
        if (options == null)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (options == null && DateTime.UtcNow < deadline)
            {
                _watchdog?.Reset("Looking for the run Options button");
                await Task.Delay(250, ct);
                options = root.GetNodeOrNull<NButton>(AbandonRunOptionsButtonPath);
            }
        }
        if (options == null)
        {
            Logger.Log("[RlAutoSlayer] No Options button in the run top bar -- "
                       + "cannot abandon from here; leaving the run as-is");
            return;
        }
        await UiHelper.Click(options);
        await UiHelper.Click(await WaitHelper.ForNode<NButton>(
            root,
            AbandonRunButtonPath,
            ct));
        await UiHelper.Click(await WaitHelper.ForNode<NButton>(
            root,
            AbandonRunProceedButtonPath,
            ct));
    }

    private static string RunCompleteState(string result)
    {
        return JsonSerializer.Serialize(RunStateBridgeFields.Apply(new Dictionary<string, object>
        {
            [NonCombatBridgeProtocol.TypeField] = NonCombatBridgeProtocol.RunCompleteState,
            [NonCombatBridgeProtocol.ResultField] = result,
        }));
    }

    private static void SetSharedCurrentWatchdog(Watchdog? watchdog)
    {
        try
        {
            PropertyInfo? property = typeof(AutoSlayer).GetProperty(
                "CurrentWatchdog",
                BindingFlags.Public | BindingFlags.Static);
            property?.SetValue(null, watchdog);
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Could not mirror watchdog: {ex.Message}");
        }
    }

    /// <summary>
    /// Raise AutoSlayConfig.watchdogTimeout from its stock 30s.
    ///
    /// The watchdog measures NO PROGRESS, not response latency, so a long
    /// agent decision counts against the same window as the game's own
    /// serialize and animation work. The deterministic combat planner is
    /// budgeted at 90s per whole-combat search, which the stock 30s cannot
    /// accommodate: measured live 2026-07-30, a 28s plan tripped the
    /// watchdog at 39.9s and RlAutoSlayer aborted the run one card into the
    /// first fight.
    ///
    /// watchdogTimeout is a public static READONLY TimeSpan, not a const,
    /// so reflection can set it -- the value is read at each Check(), so a
    /// one-time write before the Watchdog is constructed applies for the
    /// whole run. Failure is non-fatal and logged: the run still plays, it
    /// just keeps the stock 30s window and will abort on a long plan.
    /// </summary>
    private static void RaiseWatchdogTimeout(TimeSpan timeout)
    {
        try
        {
            FieldInfo? field = typeof(AutoSlayConfig).GetField(
                "watchdogTimeout",
                BindingFlags.Public | BindingFlags.Static);
            if (field == null)
            {
                Logger.Log("[RlAutoSlayer] watchdogTimeout field not found -- "
                           + "keeping stock timeout");
                return;
            }
            TimeSpan before = (TimeSpan)(field.GetValue(null) ?? TimeSpan.Zero);
            field.SetValue(null, timeout);
            TimeSpan after = (TimeSpan)(field.GetValue(null) ?? TimeSpan.Zero);
            if (after != timeout)
            {
                Logger.Log($"[RlAutoSlayer] watchdogTimeout write did not take "
                           + $"(still {after.TotalSeconds:F0}s) -- long plans "
                           + "will abort the run");
                return;
            }
            Logger.Log($"[RlAutoSlayer] watchdogTimeout raised "
                       + $"{before.TotalSeconds:F0}s -> {after.TotalSeconds:F0}s");
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlAutoSlayer] Could not raise watchdogTimeout: {ex.Message}");
        }
    }
}

/// <summary>
/// RL-aware GameOverScreenHandler. Same as the original but also notifies
/// the Python agent about game over.
/// </summary>
public class RlGameOverScreenHandler : IScreenHandler, IHandler
{
    private const int HandlerTimeoutMinutes = 2;
    private const int ContinueButtonTimeoutSeconds = 30;
    private const int SummaryAnimationTimeoutSeconds = 90;
    private const int WatchdogRefreshCycles = 20;

    public Type ScreenType => typeof(NGameOverScreen);
    public TimeSpan Timeout => TimeSpan.FromMinutes(HandlerTimeoutMinutes);

    public async Task HandleAsync(Rng random, CancellationToken ct)
    {
        Logger.Log("[RlGameOver] Game over screen appeared");

        // TELL THE ROOM LOOP THE RUN IS OVER.
        //
        // Reaching this screen IS the end of the run, and it is the only
        // moment the fact is observable: by the time PlayRunAsync's loop comes
        // round again the screen has been dismissed and the game is back at
        // the main menu, so a "is the game-over screen up?" test there always
        // reads false. Without this flag the loop kept iterating rooms after a
        // death -- entering an Elite on a run that no longer existed, finding
        // no map screen, and failing with "Combat not started". Every death
        // was scored as a failed run for that reason.
        RlAutoSlayer.RunEnded = true;

        NGameOverScreen screen =
            (NGameOverScreen)NOverlayStack.Instance.Peek();

        // Notify Python that the game is over
        BridgeServer.Instance.SendState(JsonSerializer.Serialize(
            RunStateBridgeFields.Apply(new Dictionary<string, object>
            {
                [NonCombatBridgeProtocol.TypeField] = NonCombatBridgeProtocol.GameOverState,
                [NonCombatBridgeProtocol.MessageField] = NonCombatBridgeProtocol.GameOverMessage,
            })));

        NGameOverContinueButton continueButton =
            UiHelper.FindFirst<NGameOverContinueButton>(screen);
        if (continueButton == null)
        {
            Logger.Log("[RlGameOver] Continue button not found");
            return;
        }

        // A GAME-OVER SCREEN MEANS THE RUN IS ALREADY OVER.
        //
        // Throwing here scored a finished run as FAILED over a UI button --
        // the same class of mistake as the earlier death-path bugs, where
        // post-run code treated "could not click something" as "the run went
        // wrong". Measured live: a Silent run died normally and was recorded
        // as "Continue button did not become enabled" after the watchdog sat
        // on NGameOverScreen for 25s.
        //
        // Mark the run ended and let recovery take it back to the menu (it
        // has a programmatic route now). The run's OUTCOME is already
        // decided; only the paperwork is stuck.
        try
        {
            await WaitHelper.Until(() => continueButton.IsEnabled, ct,
                TimeSpan.FromSeconds(ContinueButtonTimeoutSeconds),
                "Continue button did not become enabled");
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlGameOver] Continue button never enabled ({ex.Message}); "
                       + "the run is over regardless -- marking it ended");
            RlAutoSlayer.RunEnded = true;
            return;
        }
        await UiHelper.Click(continueButton);

        NReturnToMainMenuButton mainMenuButton = null;
        int waitCycles = 0;
        await WaitHelper.Until(delegate
        {
            if (!GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree())
                return true;
            mainMenuButton = UiHelper.FindFirst<NReturnToMainMenuButton>(screen);
            waitCycles++;
            if (waitCycles % WatchdogRefreshCycles == 0)
            {
                RlAutoSlayer.CurrentWatchdog?.Reset("Waiting for game over summary animation");
            }
            return mainMenuButton != null && mainMenuButton.Visible && mainMenuButton.IsEnabled;
        }, ct, TimeSpan.FromSeconds(SummaryAnimationTimeoutSeconds),
            "Main menu button did not become enabled");

        if (!GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree())
            return;

        await UiHelper.Click(mainMenuButton);
        await WaitHelper.Until(
            () => !GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree(),
            ct, TimeSpan.FromSeconds(ContinueButtonTimeoutSeconds),
            "Game over screen did not close");
    }
}
