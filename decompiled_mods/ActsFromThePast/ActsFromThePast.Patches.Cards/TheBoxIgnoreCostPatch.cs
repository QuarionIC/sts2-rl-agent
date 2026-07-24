using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(MerchantEntry), "OnTryPurchaseWrapper")]
public static class TheBoxIgnoreCostPatch
{
	public static void Prefix(ref bool ignoreCost, Player ____player)
	{
		if (TheBoxTracker.FreeNextPurchasePlayer == ____player)
		{
			ignoreCost = true;
		}
	}
}
