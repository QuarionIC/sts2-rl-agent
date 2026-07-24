using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CardModel), "UpgradeInternal")]
internal static class UpgradeModifiers
{
	[HarmonyPostfix]
	private static void UpgradeModifiersOnCard(CardModel __instance)
	{
		foreach (CardModifier item in CardModifier.Modifiers(__instance))
		{
			item.OnUpgrade();
			item.DynamicVars.RecalculateForUpgradeOrEnchant();
		}
	}
}
