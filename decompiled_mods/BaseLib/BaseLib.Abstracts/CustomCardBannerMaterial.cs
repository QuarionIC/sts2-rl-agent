using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardBannerMaterial
{
	[HarmonyPrefix]
	private static bool UseAltMaterial(CardModel __instance, ref Material? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		__result = customCardModel.CustomBannerMaterial;
		return __result == null;
	}
}
