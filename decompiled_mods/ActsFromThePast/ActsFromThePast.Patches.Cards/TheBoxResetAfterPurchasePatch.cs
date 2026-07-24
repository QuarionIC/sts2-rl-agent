using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(MerchantEntry), "InvokePurchaseCompleted")]
public static class TheBoxResetAfterPurchasePatch
{
	public static void Prefix(MerchantEntry __instance, Player ____player)
	{
		if (__instance is MerchantCardRemovalEntry)
		{
			TheBoxTracker.CardRemovalUsed = true;
		}
		if (TheBoxTracker.SkipNextCompletion)
		{
			TheBoxTracker.SkipNextCompletion = false;
		}
		else if (TheBoxTracker.FreeNextPurchasePlayer == ____player)
		{
			TheBoxTracker.FreeNextPurchasePlayer = null;
		}
	}
}
