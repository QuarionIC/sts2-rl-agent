using System.Linq;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(MerchantRoom), "EnterInternal")]
public static class TheBoxResetOnShopEnterPatch
{
	public static void Prefix(IRunState runState)
	{
		TheBoxTracker.FreeNextPurchasePlayer = null;
		TheBoxTracker.SkipNextCompletion = false;
		TheBoxTracker.ShowRemovalDialogue = false;
		TheBoxTracker.CardRemovalUsed = false;
		TheBoxTracker.PlayerHasBox = runState != null && ((IPlayerCollection)runState).Players.Any((Player p) => p.Deck.Cards.Any((CardModel c) => c is TheBox));
	}
}
