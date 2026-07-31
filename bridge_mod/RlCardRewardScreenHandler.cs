// RlCardRewardScreenHandler.cs -- RL-agent-driven card reward screen handler.
//
// This handles the NCardRewardSelectionScreen overlay. When the card selector
// (RlCardSelector) is active, this screen may not appear because CardSelectCmd
// bypasses it. But if it does appear (e.g. due to some code path not using
// Selector), this handler sends the options to Python and clicks the chosen card.
//
// Falls back to random selection if Python is disconnected or times out.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;

namespace STS2BridgeMod;

public class RlCardRewardScreenHandler : IScreenHandler, IHandler
{
    private const int AgentTimeoutSeconds = 30;
    private const int HandlerTimeoutSeconds = 30;
    private const int InitialSettleDelayMs = 400;
    private const int CloseTimeoutSeconds = 10;
    private static readonly TimeSpan AgentTimeout = TimeSpan.FromSeconds(AgentTimeoutSeconds);

    public Type ScreenType => typeof(NCardRewardSelectionScreen);
    public TimeSpan Timeout => TimeSpan.FromSeconds(HandlerTimeoutSeconds);

    public async Task HandleAsync(Rng random, CancellationToken ct)
    {
        Logger.Log("[RlCardReward] Card reward screen appeared");
        NCardRewardSelectionScreen screen =
            (NCardRewardSelectionScreen)NOverlayStack.Instance.Peek();
        await Task.Delay(InitialSettleDelayMs, ct);

        List<NCardHolder> holders = UiHelper.FindAll<NCardHolder>(screen);
        if (holders.Count == 0)
        {
            Logger.Log("[RlCardReward] No card holders found");
            return;
        }

        // Build state message
        var cards = new List<Dictionary<string, object>>();
        for (int i = 0; i < holders.Count; i++)
        {
            var cardData = new Dictionary<string, object>
            {
                ["index"] = i,
            };
            var card = holders[i].CardModel;
            if (card != null)
            {
                cardData["id"] = card.Id.Entry;
                cardData["type"] = card.Type.ToString();
                cardData["cost"] = card.EnergyCost.Canonical;
                if (card.IsUpgraded)
                    cardData["upgraded"] = true;
            }
            cards.Add(cardData);
        }

        var stateMsg = RunStateBridgeFields.Apply(new Dictionary<string, object>
        {
            ["type"] = NonCombatBridgeProtocol.CardRewardState,
            ["cards"] = cards,
            // Report whether skipping is ACTUALLY possible, rather than
            // asserting it. Hardcoding true told the agent it could always
            // skip; when it could not, the skip silently failed and the run
            // was lost. CanSkip() probes the same reflection path the skip
            // itself uses, so the answer matches what will happen.
            ["can_skip"] = HasSkipAlternative(),
        });

        NCardHolder chosenHolder = null;

        if (BridgeServer.Instance.IsClientConnected)
        {
            try
            {
                string stateJson = JsonSerializer.Serialize(stateMsg);
                string responseJson = await BridgeServer.Instance.SendStateAndWaitForActionAsync(
                    stateJson,
                    AgentTimeout, ct);

                if (responseJson != null)
                {
                    using var doc = JsonDocument.Parse(responseJson);
                    var root = doc.RootElement;
                    string action = root.GetProperty("action").GetString() ?? "";

                    if (action == NonCombatBridgeProtocol.SkipAction)
                    {
                        Logger.Log("[RlCardReward] Agent chose to skip");
                        if (TrySkipCardReward())
                            return;
                        // NOT EVERY CARD REWARD CAN BE SKIPPED, and until now
                        // the failure was only logged: the screen stayed open,
                        // the drain loop re-handled it, and the run died with
                        // "Screen NCardRewardSelectionScreen not closing after
                        // being handled". Measured live on the all-RL stack,
                        // whose run agent skips card rewards by preference.
                        //
                        // Taking a card always closes the screen, so an
                        // unskippable reward costs a card we did not want
                        // rather than the whole run.
                        Logger.Log("[RlCardReward] Skip is not available on this "
                                   + "screen -- taking a card so it closes");
                        chosenHolder = holders.Count > 0 ? holders[0] : null;
                    }

                    if (action == NonCombatBridgeProtocol.ChooseAction &&
                        root.TryGetProperty("index", out var idxProp))
                    {
                        int idx = idxProp.GetInt32();
                        if (idx >= holders.Count)
                        {
                            Logger.Log("[RlCardReward] Agent chose to skip via out-of-range choose");
                            if (TrySkipCardReward())
                                return;
                            Logger.Log("[RlCardReward] Skip is not available -- "
                                       + "taking a card so the screen closes");
                            chosenHolder = holders.Count > 0 ? holders[0] : null;
                        }
                        if (idx >= 0 && idx < holders.Count)
                        {
                            chosenHolder = holders[idx];
                            Logger.Log($"[RlCardReward] Agent chose card at index {idx}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[RlCardReward] Agent error: {ex.Message}");
            }
        }

        // Fallback to random
        if (chosenHolder == null)
        {
            Logger.Log("[RlCardReward] Falling back to random selection");
            chosenHolder = random.NextItem(holders);
        }

        chosenHolder.EmitSignal(NCardHolder.SignalName.Pressed, chosenHolder);
        await WaitHelper.Until(
            () => !GodotObject.IsInstanceValid(screen) || !screen.IsVisibleInTree(),
            ct, TimeSpan.FromSeconds(CloseTimeoutSeconds),
            "Card reward screen did not close after selection");
        Logger.Log("[RlCardReward] Card reward screen handled");
    }

    /// <summary>
    /// True when the screen actually offers a Skip alternative.
    ///
    /// Reported to the agent as can_skip. It used to be hardcoded true, so
    /// the agent was told it could always skip and kept choosing an action
    /// that could not be carried out.
    /// </summary>
    private static bool HasSkipAlternative()
    {
        return FindSkipButton() != null;
    }

    /// <summary>
    /// The alternative button's label, read from its private _optionName.
    ///
    /// NCardRewardAlternativeButton exposes no public text -- the name is a
    /// private string used to fill a MegaLabel in _Ready
    /// (NCardRewardAlternativeButton.cs:141,180). Matching on it is how Skip
    /// is told apart from REROLL or a relic-granted SACRIFICE, which share
    /// the same container and do very different things.
    /// </summary>
    private static string AlternativeName(NCardRewardAlternativeButton button)
    {
        try
        {
            FieldInfo field = typeof(NCardRewardAlternativeButton).GetField(
                "_optionName", BindingFlags.NonPublic | BindingFlags.Instance);
            return (field?.GetValue(button) as string) ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static NCardRewardAlternativeButton FindSkipButton()
    {
        try
        {
            NOverlayStack stack = NOverlayStack.Instance;
            if (stack == null || stack.ScreenCount <= 0)
                return null;
            NCardRewardSelectionScreen screen =
                stack.Peek() as NCardRewardSelectionScreen;
            if (screen == null)
                return null;
            return UiHelper.FindAll<NCardRewardAlternativeButton>(screen)
                .FirstOrDefault(b => AlternativeName(b)
                    .IndexOf("skip", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch
        {
            return null;
        }
    }


    /// <summary>
    /// Click the screen's own Skip button.
    ///
    /// Skip is not a method on some manager -- it is a
    /// CardRewardAlternative, rendered as an NCardRewardAlternativeButton
    /// beside the cards:
    ///   CardRewardAlternative.cs:62
    ///     new CardRewardAlternative("Skip",
    ///         PostAlternateCardRewardAction.EndSelectionAndDoNotCompleteReward)
    /// Clicking it resolves the screen through OnAlternateRewardSelected.
    ///
    /// The previous implementation searched by reflection for a
    /// RewardScreen.Skip()/Dismiss() that does not exist on this screen, so
    /// EVERY skip failed. The screen stayed open, the drain loop re-handled
    /// it, and the run died with "Screen NCardRewardSelectionScreen not
    /// closing after being handled" -- on rewards that were perfectly
    /// skippable.
    /// </summary>
    private static bool TrySkipCardReward()
    {
        try
        {
            NCardRewardAlternativeButton skip = FindSkipButton();
            if (skip == null)
            {
                Logger.Log("[RlCardReward] This screen offers no Skip alternative");
                return false;
            }
            Logger.Log("[RlCardReward] Clicking the screen's Skip button");
            skip.EmitSignal(NClickableControl.SignalName.Released, skip);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlCardReward] Skip failed: {ex.Message}");
            return false;
        }
    }

    private static Type? FindGameType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type? found = null;
            try
            {
                found = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            }
            catch (ReflectionTypeLoadException ex)
            {
                found = ex.Types.FirstOrDefault(t => t?.Name == typeName);
            }

            if (found != null)
                return found;
        }

        return null;
    }
}
