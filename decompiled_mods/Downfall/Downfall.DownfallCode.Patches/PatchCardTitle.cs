using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class PatchCardTitle
{
	[HarmonyPostfix]
	private static void Postfix(CardModel __instance, ref string __result)
	{
		__result = CardTitleHooks.ApplyModifiers(__instance, __result);
	}
}
