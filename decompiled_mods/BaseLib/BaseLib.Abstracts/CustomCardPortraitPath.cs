using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCardPortraitPath
{
	[HarmonyPrefix]
	private static bool UseAltTexture(CardModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCardModel customCardModel))
		{
			return true;
		}
		if (customCardModel.CustomPortrait != null)
		{
			__result = ((Resource)customCardModel.CustomPortrait).ResourcePath;
		}
		else
		{
			if (customCardModel.CustomPortraitPath == null)
			{
				return true;
			}
			__result = ((Resource)ResourceLoader.Load<Texture2D>(customCardModel.CustomPortraitPath, (string)null, (CacheMode)1)).ResourcePath;
		}
		return false;
	}
}
