// RlCombatHandler.cs -- RL-agent-driven combat handler.
//
// Replaces AutoSlay's CombatRoomHandler. Instead of applying god-mode buffs
// and playing random cards, this handler:
//   1. Waits for combat to start and the play phase
//   2. Serializes the combat state to JSON
//   3. Sends state to the Python RL agent via BridgeServer
//   4. Waits for the agent's response (play card or end turn)
//   5. Executes the action using CardCmd.AutoPlay or PlayerCmd.EndTurn
//   6. Loops until combat ends
//
// If the Python agent is not connected or times out, falls back to random play.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.AutoSlay;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace STS2BridgeMod;

public class RlCombatHandler : IRoomHandler, IHandler
{
    /// How long to wait for one agent decision before giving up and playing a
    /// random card. Must exceed the planner's own budget
    /// (PlannerConfig.time_budget_s, 90s for the whole-combat search in
    /// agent_runner._combat_planner_action) or a long plan is discarded and
    /// the mod plays randomly instead -- silently throwing away the search.
    private static readonly TimeSpan AgentTimeout = TimeSpan.FromSeconds(150);

    /// How often to tell the watchdog we are alive while the agent thinks.
    /// The watchdog measures NO PROGRESS, so without this a long plan looks
    /// identical to a hung game and AutoSlayTimeoutException kills the run.
    private static readonly TimeSpan WatchdogHeartbeat = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Await one agent decision, keeping the watchdog alive while it thinks.
    ///
    /// The AutoSlay watchdog measures NO PROGRESS rather than response
    /// latency, so a long deterministic plan is indistinguishable from a hung
    /// game. Measured live 2026-07-30 at the stock 30s threshold: a 28s plan
    /// tripped it at 39.9s and RlAutoSlayer aborted the run one card into the
    /// first fight.
    ///
    /// Heartbeating is preferable to raising the global timeout, for two
    /// reasons. First, it is the only option that works:
    /// AutoSlayConfig.watchdogTimeout is a static readonly (initonly) field
    /// and .NET 9 rejects FieldInfo.SetValue on it once the type is
    /// initialized ("Cannot set initonly static field"). Second, it is
    /// narrower -- genuine stuck-detection stays intact everywhere else, and
    /// only the interval where we KNOW the agent is working is excused.
    /// AgentTimeout still bounds the wait, so a dead client cannot hang the
    /// run forever.
    /// </summary>
    private static async Task<string?> AwaitAgentDecisionAsync(
        string stateJson, CancellationToken ct)
    {
        Task<string?> pending = BridgeServer.Instance.SendStateAndWaitForActionAsync(
            stateJson, AgentTimeout, ct);
        while (true)
        {
            Task finished = await Task.WhenAny(
                pending, Task.Delay(WatchdogHeartbeat, ct));
            if (finished == pending)
            {
                return await pending;
            }
            RlAutoSlayer.CurrentWatchdog?.Reset("Waiting for agent decision");
        }
    }
    private const int MaxRlHandSlots = 10;

    public RoomType[] HandledTypes => new RoomType[]
    {
        RoomType.Monster, RoomType.Elite, RoomType.Boss
    };

    public TimeSpan Timeout => TimeSpan.FromMinutes(10);

    private static bool IsPlayPhase(Player player) =>
        player.PlayerCombatState?.Phase == PlayerTurnPhase.Play;

    public async Task HandleAsync(Rng random, CancellationToken ct)
    {
        Logger.Log("[RlCombat] Waiting for combat to start");
        await WaitHelper.Until(
            () => CombatManager.Instance.IsInProgress, ct,
            AutoSlayConfig.nodeWaitTimeout, "Combat not started");

        Logger.Log("[RlCombat] Combat started");
        Player player = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState());

        int turnCount = 0;
        while (CombatManager.Instance.IsInProgress && turnCount < 200)
        {
            ct.ThrowIfCancellationRequested();
            turnCount++;

            // Wait for play phase
            await WaitHelper.Until(
                () => IsPlayPhase(player) ||
                      !CombatManager.Instance.IsInProgress,
                ct, TimeSpan.FromSeconds(30), "Play phase not started");

            if (!CombatManager.Instance.IsInProgress)
                break;

            RlAutoSlayer.CurrentWatchdog?.Reset($"Combat turn {turnCount}");
            Logger.Log($"[RlCombat] Turn {turnCount}: awaiting agent decision");

            int cardsPlayed = 0;
            bool turnEnded = false;

            while (!turnEnded && cardsPlayed < 50 && IsPlayPhase(player))
            {
                ct.ThrowIfCancellationRequested();

                if (cardsPlayed > 0 && cardsPlayed % 10 == 0)
                {
                    RlAutoSlayer.CurrentWatchdog?.Reset(
                        $"Combat turn {turnCount}, played {cardsPlayed} cards");
                }

                // Serialize combat state
                string stateJson = SerializeCombatState(player);

                // Send to Python and wait for response
                string responseJson = null;
                bool clientConnected = BridgeServer.Instance.IsClientConnected;
                Logger.Log($"[RlCombat] Client connected: {clientConnected}, sending state ({stateJson.Length} bytes)");
                if (clientConnected)
                {
                    try
                    {
                        Logger.Log("[RlCombat] State sent, waiting for agent response...");
                        responseJson = await AwaitAgentDecisionAsync(stateJson, ct);
                        Logger.Log($"[RlCombat] Agent response: {responseJson ?? "null"}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[RlCombat] Agent communication error: {ex.Message}");
                    }
                }

                // Parse and execute the response, or fall back to random
                if (responseJson != null)
                {
                    turnEnded = await ExecuteAgentAction(
                        responseJson, player, random, ct);
                }
                else
                {
                    Logger.Log("[RlCombat] No agent response, falling back to random");
                    turnEnded = await PlayRandomFallback(player, random, ct);
                }

                if (!turnEnded)
                    cardsPlayed++;

                await Task.Delay(100, ct);
            }

            // If we ran out of cards to play without ending turn, end it
            if (IsPlayPhase(player) && CombatManager.Instance.IsInProgress && !turnEnded)
            {
                PlayerCmd.EndTurn(player, canBackOut: false);
            }
        }

        await WaitHelper.Until(
            () => !CombatManager.Instance.IsInProgress, ct,
            TimeSpan.FromSeconds(30), "Combat did not end");
        Logger.Log("[RlCombat] Combat finished");
    }

    /// <summary>
    /// Execute an action from the Python agent response JSON.
    /// Returns true if turn was ended, false if a card or potion was used.
    /// </summary>
    private async Task<bool> ExecuteAgentAction(
        string json, Player player, Rng random, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            string action = root.GetProperty("action").GetString() ?? "";

            switch (action.ToLowerInvariant())
            {
                case "play":
                {
                    int cardIndex = root.GetProperty("card_index").GetInt32();
                    int targetIndex = root.TryGetProperty("target_index", out var ti)
                        ? ti.GetInt32() : -1;

                    if (cardIndex >= MaxRlHandSlots)
                    {
                        int potionSlot = cardIndex - MaxRlHandSlots;
                        Logger.Log($"[RlCombat] Using potion slot {potionSlot} -> target_index {targetIndex}");
                        await UsePotionAndWaitAsync(player, potionSlot, targetIndex, ct);
                        return false;
                    }

                    CardPile hand = PileType.Hand.GetPile(player);
                    if (cardIndex < 0 || cardIndex >= hand.Cards.Count)
                    {
                        Logger.Log($"[RlCombat] Invalid card_index {cardIndex}, hand size {hand.Cards.Count}");
                        return false;
                    }

                    CardModel card = hand.Cards[cardIndex];

                    UnplayableReason reason;
                    AbstractModel preventer;
                    if (!card.CanPlay(out reason, out preventer))
                    {
                        Logger.Log($"[RlCombat] Card {card.Id.Entry} not playable: {reason}");
                        return false;
                    }

                    Creature target = ResolveTarget(card, targetIndex);
                    if (card.TargetType == TargetType.AnyEnemy && target == null)
                    {
                        Logger.Log($"[RlCombat] Invalid target_index {targetIndex} for {card.Id.Entry}");
                        return false;
                    }
                    Logger.Log($"[RlCombat] Playing card: {card.Id.Entry} -> target_index {targetIndex}");

                    await PlayCardAndWaitAsync(player, card, target, ct);
                    return false;
                }

                case "end_turn":
                {
                    Logger.Log("[RlCombat] Agent chose to end turn");
                    PlayerCmd.EndTurn(player, canBackOut: false);
                    return true;
                }

                case "potion":
                {
                    int slot = root.GetProperty("slot").GetInt32();
                    int targetIndex = root.TryGetProperty("target_index", out var ti)
                        ? ti.GetInt32() : -1;
                    Logger.Log($"[RlCombat] Using potion slot {slot} -> target_index {targetIndex}");
                    await UsePotionAndWaitAsync(player, slot, targetIndex, ct);
                    return false;
                }

                default:
                    Logger.Log($"[RlCombat] Unknown action: {action}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlCombat] Error executing agent action: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Fallback: play a random playable card, then end turn.
    /// Returns true (turn ended).
    /// </summary>
    private async Task<bool> PlayRandomFallback(
        Player player, Rng random, CancellationToken ct)
    {
        CardPile hand = PileType.Hand.GetPile(player);
        UnplayableReason reason;
        AbstractModel preventer;
        List<CardModel> playable = hand.Cards
            .Where(c => c.CanPlay(out reason, out preventer))
            .ToList();

        if (playable.Count > 0)
        {
            CardModel card = random.NextItem(playable);
            Creature target = GetRandomTarget(card, random);
            Logger.Log($"[RlCombat] Random fallback: playing {card.Id.Entry}");
            await PlayCardAndWaitAsync(player, card, target, ct);
            return false;
        }
        else
        {
            Logger.Log("[RlCombat] Random fallback: no playable cards, ending turn");
            PlayerCmd.EndTurn(player, canBackOut: false);
            return true;
        }
    }

    /// <summary>
    /// Resolve a target creature from the target_index.
    /// </summary>
    private Creature? ResolveTarget(CardModel card, int targetIndex)
    {
        if (card.TargetType != TargetType.AnyEnemy)
            return null;

        ICombatState combatState = card.CombatState;
        if (combatState == null)
            return null;

        List<Creature> allEnemies = combatState.Enemies.ToList();
        if (allEnemies.Count == 0)
            return null;

        if (targetIndex >= 0)
        {
            if (targetIndex >= allEnemies.Count)
                return null;
            Creature indexedEnemy = allEnemies[targetIndex];
            return indexedEnemy.IsHittable ? indexedEnemy : null;
        }

        return combatState.HittableEnemies.FirstOrDefault();
    }

    private static Creature? ResolvePotionTarget(Player player, PotionModel? potion, int targetIndex)
    {
        if (potion == null)
            return null;

        string targetType = "Self";
        try
        {
            targetType = potion.TargetType.ToString() ?? "Self";
        }
        catch
        {
            return player.Creature;
        }

        if (targetType == "AnyEnemy")
        {
            ICombatState? combatState = player.Creature?.CombatState;
            if (combatState == null)
                return null;
            List<Creature> allEnemies = combatState.Enemies.ToList();
            if (targetIndex >= 0)
            {
                if (targetIndex >= allEnemies.Count)
                    return null;
                Creature indexedEnemy = allEnemies[targetIndex];
                return indexedEnemy.IsHittable ? indexedEnemy : null;
            }
            return combatState.HittableEnemies.FirstOrDefault();
        }

        if (targetType == "Self" || targetType == "AnyPlayer")
            return player.Creature;

        return null;
    }

    private static Creature? GetRandomTarget(CardModel card, Rng random)
    {
        if (card.TargetType != TargetType.AnyEnemy)
            return null;
        ICombatState combatState = card.CombatState;
        if (combatState == null)
            return null;
        List<Creature> hittable = combatState.HittableEnemies.ToList();
        if (hittable.Count == 0)
            return null;
        return random.NextItem(hittable);
    }

    /// <summary>
    /// A fingerprint of everything a card play can move.
    ///
    /// Deliberately the SAME quantities SerializeCombatState sends: if none of
    /// them has changed for a few consecutive polls, the state Python is about
    /// to receive is the settled one.
    /// </summary>
    private static string StateFingerprint(Player player)
    {
        try
        {
            PlayerCombatState pcs = player.PlayerCombatState;
            Creature creature = player.Creature;
            CombatState cs = CombatManager.Instance?.DebugOnlyGetState();
            var sb = new System.Text.StringBuilder();
            sb.Append(pcs?.Energy ?? -1).Append('/')
              .Append(pcs?.Hand.Cards.Count ?? -1).Append('/')
              .Append(pcs?.DrawPile.Cards.Count ?? -1).Append('/')
              .Append(pcs?.DiscardPile.Cards.Count ?? -1).Append('/')
              .Append(pcs?.ExhaustPile.Cards.Count ?? -1).Append('/')
              .Append(creature?.CurrentHp ?? -1).Append('/')
              .Append(creature?.Block ?? -1).Append('/')
              .Append(creature?.Powers.Count() ?? -1);
            if (cs != null)
            {
                foreach (Creature enemy in cs.Enemies)
                    sb.Append(';').Append(enemy.CurrentHp).Append(',')
                      .Append(enemy.Block).Append(',')
                      .Append(enemy.Powers.Count());
            }
            return sb.ToString();
        }
        catch (Exception)
        {
            // A fingerprint we cannot take must not stall the run; returning a
            // fresh value each time simply means "not settled yet", and the
            // overall timeout still applies.
            return Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Enqueue a card play and wait for it to FULLY RESOLVE.
    ///
    /// The previous version broke out of its wait on the first observable
    /// change -- energy or hand count moving. That is a proxy for "the play
    /// started", not "the play finished". A card's effects (draws, damage,
    /// generated cards, power applications) keep working through the action
    /// queue afterwards, and SerializeCombatState could run mid-resolution.
    ///
    /// Python then received a state that did not yet reflect the action it had
    /// just sent, while its own simulation had resolved it completely -- the
    /// SIM-AHEAD divergence class, measured at 3.92% of live combat actions
    /// (54 of 1376) on 2026-07-31 against a stable ~2% genuine fidelity rate.
    /// Every occurrence also threw away a valid plan and forced a fresh search
    /// (~30s on the eval ladder), so this was expensive as well as misleading.
    ///
    /// Waiting for QUIESCENCE instead: the play must first be observed, then
    /// the state must stop changing for several consecutive polls. Bounded by
    /// the same overall timeout, so a long animation costs time but never the
    /// run.
    /// </summary>
    private static async Task PlayCardAndWaitAsync(
        Player player, CardModel card, Creature? target, CancellationToken ct)
    {
        var playAction = new PlayCardAction(card, target);
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(playAction);
        await WaitForActionToSettleAsync(player, ct);
    }

    //: Polls of an unchanged fingerprint before the state counts as settled.
    //: Three at 50ms is 150ms of quiet -- comfortably longer than the gaps
    //: between chained effects, short enough not to slow a batch materially.
    private const int SettlePolls = 3;
    private const int SettlePollMs = 50;
    private const int ActionSettleTimeoutMs = 4000;

    private static async Task WaitForActionToSettleAsync(
        Player player, CancellationToken ct)
    {
        string before = StateFingerprint(player);
        int waitMs = 0;
        bool observed = false;
        int stablePolls = 0;
        string last = before;

        while (waitMs < ActionSettleTimeoutMs)
        {
            if (!IsPlayPhase(player) || !CombatManager.Instance.IsInProgress)
                return;

            string now = StateFingerprint(player);
            if (!observed)
            {
                // Phase 1: has the action taken effect at all yet?
                if (now != before)
                {
                    observed = true;
                    last = now;
                    stablePolls = 0;
                }
            }
            else if (now == last)
            {
                // Phase 2: has it stopped changing?
                if (++stablePolls >= SettlePolls)
                    return;
            }
            else
            {
                // Still resolving -- effects are chaining.
                last = now;
                stablePolls = 0;
            }

            await Task.Delay(SettlePollMs, ct);
            waitMs += SettlePollMs;
        }

        Logger.Log($"[RlCombat] Action did not settle within {ActionSettleTimeoutMs}ms "
                   + $"(observed={observed}); sending state anyway");
    }

    private static async Task UsePotionAndWaitAsync(
        Player player, int slot, int targetIndex, CancellationToken ct)
    {
        if (slot < 0)
            return;

        dynamic potion = null;
        try
        {
            potion = player.GetPotionAtSlotIndex(slot);
        }
        catch
        {
            return;
        }

        if (potion == null)
            return;

        Creature? target = ResolvePotionTarget(player, potion, targetIndex);
        if (potion.TargetType.ToString() == "AnyEnemy" && target == null)
            return;

        var usePotionAction = new UsePotionAction(
            potion,
            target,
            CombatManager.Instance.IsInProgress
        );
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(usePotionAction);

        // Wait for the SLOT to empty -- that is the potion's own signal that
        // the action was consumed -- and then for the resulting effects to
        // settle. A potion is at least as effect-heavy as a card (damage,
        // draws, powers, generated cards), so serializing on slot-empty alone
        // races exactly the way the card path did.
        int waitMs = 0;
        while (waitMs < ActionSettleTimeoutMs)
        {
            dynamic potionNow = null;
            try
            {
                potionNow = player.GetPotionAtSlotIndex(slot);
            }
            catch
            {
                potionNow = null;
            }

            if (potionNow == null || !IsPlayPhase(player) || !CombatManager.Instance.IsInProgress)
                break;
            await Task.Delay(SettlePollMs, ct);
            waitMs += SettlePollMs;
        }

        await WaitForActionToSettleAsync(player, ct);
    }

    // ----------------------------------------------------------------
    // State serialization
    // ----------------------------------------------------------------

    private string SerializeCombatState(Player player)
    {
        try
        {
            var cm = CombatManager.Instance;
            CombatState combatState = cm.DebugOnlyGetState();
            Creature playerCreature = player.Creature;
            PlayerCombatState pcs = player.PlayerCombatState;

            Logger.Log($"[RlCombat] Serialize: cm={cm != null}, cs={combatState != null}, creature={playerCreature != null}, pcs={pcs != null}");
            if (playerCreature != null)
                Logger.Log($"[RlCombat] Player: HP={playerCreature.CurrentHp}/{playerCreature.MaxHp} Block={playerCreature.Block}");
            if (pcs != null)
                Logger.Log($"[RlCombat] Energy={pcs.Energy}/{pcs.MaxEnergy} Hand={pcs.Hand.Cards.Count} Draw={pcs.DrawPile.Cards.Count}");
            if (combatState != null)
                Logger.Log($"[RlCombat] Enemies={combatState.Enemies.Count()} Round={combatState.RoundNumber}");

            // Player info
            var playerObj = new Dictionary<string, object>
            {
                ["hp"] = playerCreature.CurrentHp,
                ["max_hp"] = playerCreature.MaxHp,
                ["block"] = playerCreature.Block,
                ["energy"] = pcs?.Energy ?? 0,
                ["max_energy"] = pcs?.MaxEnergy ?? 3,
            };

            // Player powers
            var powers = new List<Dictionary<string, object>>();
            foreach (PowerModel power in playerCreature.Powers)
            {
                powers.Add(new Dictionary<string, object>
                {
                    ["id"] = power.Id.Entry,
                    ["amount"] = power.Amount,
                });
            }
            if (powers.Count > 0)
                playerObj["powers"] = powers;

            // Hand cards
            var handCards = new List<Dictionary<string, object>>();
            if (pcs != null)
            {
                foreach (CardModel card in pcs.Hand.Cards)
                {
                    handCards.Add(SerializeCard(card));
                }
            }

            // Enemies
            var enemies = new List<Dictionary<string, object>>();
            if (combatState != null)
            {
                foreach (Creature enemy in combatState.Enemies)
                {
                    enemies.Add(SerializeEnemy(enemy));
                }
            }

            // Run state info
            RunState runState = RunManager.Instance.DebugOnlyGetState();

            List<Dictionary<string, object>> potions = SerializePotions(player);
            var state = new Dictionary<string, object>
            {
                ["type"] = "combat_action",
                ["player"] = playerObj,
                ["hand"] = handCards,
                ["enemies"] = enemies,
                ["potions"] = potions,
                ["available_actions"] = GetAvailableActions(potions),
                ["draw_pile_count"] = pcs?.DrawPile.Cards.Count ?? 0,
                ["discard_pile_count"] = pcs?.DiscardPile.Cards.Count ?? 0,
                ["exhaust_pile_count"] = pcs?.ExhaustPile.Cards.Count ?? 0,
                // Ordered pile CONTENTS + the owned deck. The counts above are
                // not enough for the Python-side deterministic combat planner:
                // it rebuilds the combat in the simulator and searches it, so
                // it needs to know WHICH cards are where, in order. Without
                // these it refuses to plan (see combat_reconstruct.py) and
                // combat falls back to the language model.
                ["draw_pile"] = pcs?.DrawPile.Cards.Select(SerializeCard).ToList()
                    ?? new List<Dictionary<string, object>>(),
                ["discard_pile"] = pcs?.DiscardPile.Cards.Select(SerializeCard).ToList()
                    ?? new List<Dictionary<string, object>>(),
                ["exhaust_pile"] = pcs?.ExhaustPile.Cards.Select(SerializeCard).ToList()
                    ?? new List<Dictionary<string, object>>(),
                ["deck"] = player?.Deck?.Cards?.Select(SerializeCard).ToList()
                    ?? new List<Dictionary<string, object>>(),
                ["round"] = combatState?.RoundNumber ?? 0,
                // THE GAME'S LIVE SHUFFLE RNG.
                //
                // The simulator cannot guess this: the game draws from
                // MegaRandom (xoshiro256**, four 64-bit state words) while
                // sts2_env.core.rng.Rng wraps a .NET System.Random clone.
                // Different generators, so a reconstructed combat has never
                // been able to reshuffle the way the game will.
                //
                // Measured live: 83 of 104 whole-combat plans truncated at
                // the first reshuffle, and 100% of observed plan divergences
                // were "different cards". Sending the four state words lets
                // the simulation continue the SAME stream rather than start a
                // parallel one that merely looks random.
                ["shuffle_rng"] = SerializeShuffleRng(runState),
                ["floor"] = runState?.TotalFloor ?? 0,
                ["act"] = (runState?.CurrentActIndex ?? 0) + 1,
                // PETS (Osty). Osty is a Creature in CombatState.Allies with
                // PetOwner set, and it was never serialized at all -- so the
                // planner searched every Necrobinder fight with no Osty, while
                // the damage pipeline redirects player damage to it and
                // several cards scale off its HP. For a Necrobinder agent that
                // is not a detail; it is most of the character.
                ["pets"] = (combatState?.Allies ?? new List<Creature>())
                    .Where(c => c.IsPet)
                    .Select(SerializePet)
                    .ToList(),
            };

            // Run-level fields (gold, deck_size, relic_count, ...). Keeps the
            // richer combat "potions" list already set above.
            RunStateBridgeFields.Apply(state);

            return JsonSerializer.Serialize(state);
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlCombat] Error serializing combat state: {ex.Message}");
            return "{\"type\":\"combat_action\",\"error\":\"serialization_failed\"}";
        }
    }

    /// <summary>
    /// The Shuffle stream's live state, or null when unavailable.
    /// RunState.Rng is a public RunRngSet; RunRngSet.Shuffle is the stream
    /// the game documents as "how your draw pile gets shuffled, both at the
    /// start of combat and when you run out of cards in it" -- exactly the
    /// event the planner keeps mispredicting.
    /// </summary>
    private static Dictionary<string, object>? SerializeShuffleRng(RunState? runState)
    {
        try
        {
            SerializableRng ser = runState?.Rng?.Shuffle?.ToSerializable();
            if (ser == null)
                return null;
            return new Dictionary<string, object>
            {
                ["counter"] = ser.counter,
                ["state0"] = ser.state0,
                ["state1"] = ser.state1,
                ["state2"] = ser.state2,
                ["state3"] = ser.state3,
            };
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlCombat] Could not read shuffle RNG: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, object> SerializeCard(CardModel card)
    {
        int cost;
        try
        {
            cost = card.EnergyCost.GetWithModifiers(CostModifiers.All);
        }
        catch
        {
            cost = card.EnergyCost.Canonical;
        }

        UnplayableReason reason;
        AbstractModel preventer;
        var result = new Dictionary<string, object>
        {
            ["id"] = card.Id.Entry,
            ["cost"] = cost,
            ["type"] = card.Type.ToString(),
            ["target"] = card.TargetType.ToString(),
            ["playable"] = card.CanPlay(out reason, out preventer),
            // KEYWORDS, including globally-granted ones (Ethereal from Hex,
            // etc). Without these the simulator rebuilds each card from its
            // id alone and loses every instance-applied keyword: an Ethereal
            // card was planned as if it would survive the turn. Measured
            // against real runs, a DEFEND_NECROBINDER carrying a runtime
            // 'ethereal' keyword reconstructed with no keywords at all.
            ["keywords"] = card.Keywords.Select(k => k.ToString()).ToList(),
        };

        if (card.IsUpgraded)
            result["upgraded"] = true;

        return result;
    }

    /// <summary>
    /// Serialize a pet (Osty) so the planner can reconstruct it. Same shape as
    /// SerializeEnemy, minus the intent block -- pets act via the owner's
    /// cards, not via a monster move.
    /// </summary>
    private Dictionary<string, object> SerializePet(Creature pet)
    {
        var data = new Dictionary<string, object>
        {
            ["hp"] = pet.CurrentHp,
            ["max_hp"] = pet.MaxHp,
            ["block"] = pet.Block,
            ["is_alive"] = pet.IsAlive,
        };
        var powers = new List<Dictionary<string, object>>();
        foreach (PowerModel power in pet.Powers)
        {
            powers.Add(new Dictionary<string, object>
            {
                ["id"] = power.Id.Entry,
                ["amount"] = power.Amount,
            });
        }
        if (powers.Count > 0)
            data["powers"] = powers;
        return data;
    }

    private Dictionary<string, object> SerializeEnemy(Creature enemy)
    {
        var data = new Dictionary<string, object>
        {
            ["id"] = enemy.IsMonster ? enemy.Monster!.Id.Entry : "UNKNOWN",
            ["hp"] = enemy.CurrentHp,
            ["max_hp"] = enemy.MaxHp,
            ["block"] = enemy.Block,
            ["is_alive"] = enemy.IsAlive,
        };

        // Powers
        var powers = new List<Dictionary<string, object>>();
        foreach (PowerModel power in enemy.Powers)
        {
            powers.Add(new Dictionary<string, object>
            {
                ["id"] = power.Id.Entry,
                ["amount"] = power.Amount,
            });
        }
        if (powers.Count > 0)
            data["powers"] = powers;

        // Intent
        if (enemy.IsMonster && enemy.Monster != null)
        {
            try
            {
                var nextMove = enemy.Monster.NextMove;

                // Enemy AI state. The Python planner rebuilds this fight in
                // the simulator and searches it; without the move the monster
                // is actually on, the reconstruction rolls a FRESH move and
                // every turn planned past the first is against a different
                // enemy than the one on screen. ai_state is emitted even when
                // the move carries no intents, which is exactly the case
                // (sleep, buff wind-ups) where guessing goes wrong.
                if (nextMove != null)
                {
                    data["ai_state"] = nextMove.Id;
                    try
                    {
                        data["ai_intent_count"] = nextMove.Intents?.Count ?? 0;
                    }
                    catch { }
                }

                if (nextMove?.Intents != null && nextMove.Intents.Count > 0)
                {
                    AbstractIntent firstIntent = nextMove.Intents[0];
                    data["intent"] = firstIntent.IntentType.ToString();
                    data["intent_move_id"] = nextMove.Id;

                    if (firstIntent is AttackIntent attackIntent)
                    {
                        ICombatState cs = enemy.CombatState;
                        if (cs != null)
                        {
                            try
                            {
                                data["intent_damage"] = attackIntent.GetSingleDamage(
                                    cs.PlayerCreatures, enemy);
                                data["intent_hits"] = attackIntent.Repeats > 0
                                    ? attackIntent.Repeats : 1;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch
            {
                data["intent"] = "UNKNOWN";
            }
        }

        return data;
    }

    private static List<Dictionary<string, object>> SerializePotions(Player player)
    {
        var potions = new List<Dictionary<string, object>>();
        try
        {
            int slot = 0;
            foreach (dynamic potion in player.PotionSlots)
            {
                if (potion != null)
                {
                    string targetType = "Self";
                    bool canUse = true;
                    try
                    {
                        targetType = potion.TargetType?.ToString() ?? "Self";
                    }
                    catch { }

                    try
                    {
                        string usage = potion.Usage?.ToString() ?? "";
                        if (string.Equals(usage, "Automatic", StringComparison.OrdinalIgnoreCase))
                            canUse = false;
                    }
                    catch { }

                    potions.Add(new Dictionary<string, object>
                    {
                        ["slot"] = slot,
                        ["id"] = potion.Id.Entry,
                        ["usage"] = potion.Usage.ToString(),
                        ["can_use"] = canUse,
                        ["target"] = targetType,
                        ["requires_target"] = targetType == "AnyEnemy",
                        ["target_type"] = targetType,
                    });
                }
                slot++;
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"[RlCombat] Error serializing potions: {ex.Message}");
        }

        return potions;
    }

    private static List<string> GetAvailableActions(IEnumerable<Dictionary<string, object>> potions)
    {
        var actions = new List<string> { "PLAY", "END_TURN" };
        if (potions.Any(p => p.TryGetValue("can_use", out object? canUse) && canUse is bool b && b))
            actions.Add("POTION");
        return actions;
    }
}
