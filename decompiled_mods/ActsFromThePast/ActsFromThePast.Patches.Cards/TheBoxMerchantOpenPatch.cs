using System;
using System.Collections.Generic;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(NMerchantDialogue), "ShowOnInventoryOpen")]
public static class TheBoxMerchantOpenPatch
{
	private static readonly LocString _boxLine1 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.openInventory.theBox.1");

	private static readonly LocString _boxLine2 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.openInventory.theBox.2");

	public static bool Prefix(NMerchantDialogue __instance)
	{
		if (!TheBoxTracker.PlayerHasBox || TheBoxTracker.CardRemovalUsed)
		{
			return true;
		}
		AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom", (Type[])null, (Type[])null)?.Invoke(__instance, new object[1]
		{
			new List<LocString> { _boxLine1, _boxLine2 }
		});
		return false;
	}
}
