using System;
using System.Collections.Generic;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(NMerchantDialogue), "ShowForPurchaseAttempt")]
public static class TheBoxPurchaseDialoguePatch
{
	private static readonly LocString _removeLine1 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.purchaseSuccess.theBox.1");

	private static readonly LocString _removeLine2 = new LocString("merchant_room", "ACTSFROMTHEPAST-MERCHANT.talk.purchaseSuccess.theBox.2");

	public static bool Prefix(NMerchantDialogue __instance, PurchaseStatus status)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (!TheBoxTracker.ShowRemovalDialogue || (int)status > 0)
		{
			return true;
		}
		TheBoxTracker.ShowRemovalDialogue = false;
		AccessTools.Method(typeof(NMerchantDialogue), "ShowRandom", (Type[])null, (Type[])null)?.Invoke(__instance, new object[1]
		{
			new List<LocString> { _removeLine1, _removeLine2 }
		});
		return false;
	}
}
