using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardPortrait
{
	[HarmonyPrefix]
	private static bool UseAltTexture(CardModel __instance, ref Texture2D? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		if (customCardModel.CustomPortrait != null)
		{
			__result = customCardModel.CustomPortrait;
		}
		else
		{
			if (customCardModel.CustomPortraitPath == null)
			{
				return true;
			}
			__result = ResourceLoader.Load<Texture2D>(customCardModel.CustomPortraitPath, (string)null, (CacheMode)1);
		}
		return false;
	}
}
