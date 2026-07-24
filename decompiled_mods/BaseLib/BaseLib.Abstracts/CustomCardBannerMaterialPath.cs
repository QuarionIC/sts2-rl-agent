using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardBannerMaterialPath
{
	[HarmonyPrefix]
	private static bool UseAltMaterial(CardModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		__result = customCardModel.CustomBannerMaterialPath;
		return __result == null;
	}
}
