// RlMatchAndKeepScreenHandler.cs -- plays ActsFromThePast' Match and Keep.
//
// Match and Keep is a memory minigame: a grid of face-down cards, flip two at
// a time, a matching pair is kept, and you have a limited number of attempts.
// It is an IOverlayScreen (NMatchAndKeepScreen), and the bridge had NO handler
// for it. The drain loop therefore fell through to TryDismissUnknownScreenAsync,
// which cannot resolve a grid of card holders, and the run stalled until the
// watchdog terminated it. Reported live 2026-07-31 as "the live agent fails at
// the match and keep event".
//
// REFLECTION, because the type lives in the ActsFromThePast mod assembly rather
// than the game's. The bridge mod does not reference that assembly, so the type
// is resolved by name at startup and the handler is only registered when the
// mod is actually installed. Everything this handler touches -- the screen
// type, the private _slots list, the CardSlot fields -- is therefore accessed
// dynamically and every step fails soft: an exception here must cost the
// minigame, never the run.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.AutoSlay.Handlers;
using MegaCrit.Sts2.Core.AutoSlay.Helpers;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Random;

namespace STS2BridgeMod;

public class RlMatchAndKeepScreenHandler : IScreenHandler, IHandler
{
    private const string ScreenTypeName = "NMatchAndKeepScreen";
    private const int HandlerTimeoutSeconds = 120;
    private const int FlipSettleDelayMs = 700;
    private const int InitialSettleDelayMs = 600;
    // The screen resolves a pair itself; without a cap a screen that stops
    // responding would spin here instead of at the drain loop.
    private const int MaxFlips = 60;

    private readonly Type _screenType;

    private RlMatchAndKeepScreenHandler(Type screenType)
    {
        _screenType = screenType;
    }

    /// <summary>
    /// The handler, or null when ActsFromThePast is not installed.
    /// </summary>
    public static RlMatchAndKeepScreenHandler? TryCreate()
    {
        Type? type = FindType(ScreenTypeName);
        return type == null ? null : new RlMatchAndKeepScreenHandler(type);
    }

    public Type ScreenType => _screenType;
    public TimeSpan Timeout => TimeSpan.FromSeconds(HandlerTimeoutSeconds);

    private static Type? FindType(string typeName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                Type? found = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == typeName);
                if (found != null)
                    return found;
            }
            catch (ReflectionTypeLoadException ex)
            {
                Type? found = ex.Types.FirstOrDefault(t => t?.Name == typeName);
                if (found != null)
                    return found;
            }
            catch (Exception)
            {
                // A single unloadable assembly must not stop the search.
            }
        }
        return null;
    }

    /// <summary>One grid slot, read out of the screen's private _slots list.</summary>
    private sealed class Slot
    {
        public int Index;
        public NCardHolder? Holder;
        public bool IsFaceUp;
        public bool IsMatched;
        public string CardId = "";
    }

    private static object? ReadMember(object target, string name)
    {
        Type type = target.GetType();
        const BindingFlags flags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        try
        {
            FieldInfo? field = type.GetField(name, flags);
            if (field != null)
                return field.GetValue(target);
            PropertyInfo? property = type.GetProperty(name, flags);
            return property?.GetValue(target);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static List<Slot> ReadSlots(object screen)
    {
        var slots = new List<Slot>();
        if (ReadMember(screen, "_slots") is not IEnumerable raw)
            return slots;

        int index = 0;
        foreach (object? entry in raw)
        {
            if (entry == null)
            {
                index++;
                continue;
            }
            var slot = new Slot
            {
                Index = index,
                Holder = ReadMember(entry, "Holder") as NCardHolder,
                IsFaceUp = ReadMember(entry, "IsFaceUp") as bool? ?? false,
                IsMatched = ReadMember(entry, "IsMatched") as bool? ?? false,
            };
            // The card's identity is what makes this a MEMORY game rather than
            // a coin flip: a slot seen once can be paired deliberately later.
            object? cardNode = ReadMember(entry, "CardNode");
            if (cardNode != null)
            {
                object? model = ReadMember(cardNode, "CardModel");
                if (model != null)
                {
                    object? id = ReadMember(model, "Id");
                    object? entryText = id == null ? null : ReadMember(id, "Entry");
                    slot.CardId = entryText?.ToString() ?? "";
                }
            }
            slots.Add(slot);
            index++;
        }
        return slots;
    }

    public async Task HandleAsync(Rng random, CancellationToken ct)
    {
        Node? screen = NOverlayStack.Instance?.Peek() as Node;
        if (screen == null || !_screenType.IsInstanceOfType(screen))
        {
            Logger.Log("[RlMatchAndKeep] Screen is not a Match and Keep screen");
            return;
        }

        Logger.Log("[RlMatchAndKeep] Match and Keep minigame started");
        await Task.Delay(InitialSettleDelayMs, ct);

        // Slot index -> the card id seen there. Flipping a card reveals it for
        // the rest of the minigame even when the pair misses, so remembering
        // is strictly better than guessing and costs nothing.
        var seen = new Dictionary<int, string>();

        for (int flip = 0; flip < MaxFlips; flip++)
        {
            if (ct.IsCancellationRequested)
                return;
            if (!GodotObject.IsInstanceValid(screen) || !screen.IsInsideTree())
            {
                Logger.Log("[RlMatchAndKeep] Screen closed");
                return;
            }

            List<Slot> slots = ReadSlots(screen);
            if (slots.Count == 0)
            {
                Logger.Log("[RlMatchAndKeep] No slots readable -- leaving the "
                           + "minigame to the drain loop");
                return;
            }

            foreach (Slot slot in slots.Where(s => s.IsFaceUp && s.CardId != ""))
                seen[slot.Index] = slot.CardId;

            List<Slot> available = slots
                .Where(s => !s.IsMatched && !s.IsFaceUp && s.Holder != null)
                .ToList();
            if (available.Count == 0)
            {
                Logger.Log("[RlMatchAndKeep] Nothing left to flip");
                return;
            }

            Slot chosen = ChooseSlot(available, seen, random);
            Logger.Log($"[RlMatchAndKeep] Flipping slot {chosen.Index}"
                       + (seen.ContainsKey(chosen.Index)
                          ? $" (remembered {seen[chosen.Index]})" : ""));
            try
            {
                chosen.Holder!.EmitSignal(NCardHolder.SignalName.Pressed, chosen.Holder);
            }
            catch (Exception ex)
            {
                Logger.Log($"[RlMatchAndKeep] Flip failed: {ex.Message}");
                return;
            }
            await Task.Delay(FlipSettleDelayMs, ct);
        }

        Logger.Log($"[RlMatchAndKeep] Gave up after {MaxFlips} flips; the drain "
                   + "loop will take the screen from here");
    }

    /// <summary>
    /// Prefer a KNOWN match, then an unknown card, then anything legal.
    ///
    /// When one card of a pair is already face up, a slot remembered to hold
    /// its twin completes the pair outright. Failing that, flipping a slot
    /// never seen before buys information; re-flipping a remembered card that
    /// cannot pair buys nothing.
    /// </summary>
    private static Slot ChooseSlot(
        List<Slot> available, Dictionary<int, string> seen, Rng random)
    {
        // A remembered pair we can still flip: play it.
        var byId = new Dictionary<string, List<int>>();
        foreach (KeyValuePair<int, string> entry in seen)
        {
            if (!byId.TryGetValue(entry.Value, out List<int>? indexes))
                byId[entry.Value] = indexes = new List<int>();
            indexes.Add(entry.Key);
        }
        foreach (List<int> group in byId.Values.Where(g => g.Count >= 2))
        {
            Slot? known = available.FirstOrDefault(s => group.Contains(s.Index));
            if (known != null)
                return known;
        }

        // Otherwise buy information: a slot never seen before tells us
        // something, while re-flipping a remembered card that cannot pair
        // tells us nothing.
        List<Slot> unseen = available.Where(s => !seen.ContainsKey(s.Index)).ToList();
        return random.NextItem(unseen.Count > 0 ? unseen : available);
    }
}
