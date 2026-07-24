using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(Burn))]
public static class BurnUpgradePatch
{
	public static bool AllowBurnUpgrade;

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static void MaxUpgradeLevel_Postfix(ref int __result)
	{
		__result = (AllowBurnUpgrade ? 1 : 0);
	}

	[HarmonyPatch(typeof(CardModel), "UpgradeInternal")]
	[HarmonyPostfix]
	private static void UpgradeInternal_Postfix(CardModel __instance)
	{
		Burn val = (Burn)(object)((__instance is Burn) ? __instance : null);
		if (val != null && ((CardModel)val).IsUpgraded)
		{
			((DynamicVar)((CardModel)val).DynamicVars.Damage).UpgradeValueBy(2m);
		}
	}
}
