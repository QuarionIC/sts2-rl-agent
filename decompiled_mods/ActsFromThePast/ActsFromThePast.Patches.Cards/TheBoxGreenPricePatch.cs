using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActsFromThePast.Cards;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.addons.mega_text;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch]
public static class TheBoxGreenPricePatch
{
	public static IEnumerable<MethodBase> TargetMethods()
	{
		return from t in AccessTools.GetTypesFromAssembly(typeof(NMerchantSlot).Assembly)
			where t.IsSubclassOf(typeof(NMerchantSlot)) && !t.IsAbstract
			select AccessTools.Method(t, "UpdateVisual", (Type[])null, (Type[])null) into m
			where m != null
			select m;
	}

	public static void Postfix(object __instance, MegaLabel ____costLabel)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		if (TheBoxTracker.FreeNextPurchasePlayer != null && ____costLabel != null)
		{
			NMerchantSlot val = (NMerchantSlot)((__instance is NMerchantSlot) ? __instance : null);
			Player value = Traverse.Create((object)((val != null) ? val.Entry : null)).Field("_player").GetValue<Player>();
			if (value == TheBoxTracker.FreeNextPurchasePlayer)
			{
				((CanvasItem)____costLabel).Modulate = StsColors.green;
			}
		}
	}
}
