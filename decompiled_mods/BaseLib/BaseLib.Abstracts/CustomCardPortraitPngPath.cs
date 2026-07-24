using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardPortraitPngPath
{
	[HarmonyPrefix]
	private static bool UseAltTexture(CardModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		if (customCardModel.CustomPortraitPath != null)
		{
			__result = customCardModel.CustomPortraitPath;
			return false;
		}
		return true;
	}
}
