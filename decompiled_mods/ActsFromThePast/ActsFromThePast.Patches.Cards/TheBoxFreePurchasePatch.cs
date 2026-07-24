using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public static class TheBoxFreePurchasePatch
{
	public static void Postfix(ref int __result, Player ____player)
	{
		if (TheBoxTracker.FreeNextPurchasePlayer == ____player)
		{
			__result = 0;
		}
	}
}
