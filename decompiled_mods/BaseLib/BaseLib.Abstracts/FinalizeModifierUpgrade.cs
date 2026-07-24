using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CardModel), "FinalizeUpgradeInternal")]
internal static class FinalizeModifierUpgrade
{
	[HarmonyPostfix]
	private static void FinalizeModifiersOnCard(CardModel __instance)
	{
		foreach (CardModifier item in CardModifier.Modifiers(__instance))
		{
			item.DynamicVars.FinalizeUpgrade();
		}
	}
}
