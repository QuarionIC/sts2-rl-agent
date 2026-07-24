using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardFrame
{
	[HarmonyPrefix]
	private static bool UseAltTexture(CardModel __instance, ref Texture2D? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		__result = customCardModel.CustomFrame;
		if (__result != null)
		{
			return false;
		}
		if (!(__instance.Pool is CustomCardPoolModel customCardPoolModel))
		{
			return true;
		}
		__result = customCardPoolModel.CustomFrame(customCardModel);
		return __result == null;
	}
}
