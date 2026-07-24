using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomOrbSpritePath
{
	[HarmonyPrefix]
	private static bool Custom(OrbModel __instance, ref string __result)
	{
		if (__instance is CustomOrbModel customOrbModel)
		{
			string customSpritePath = customOrbModel.CustomSpritePath;
			if (customSpritePath != null)
			{
				__result = customSpritePath;
				return false;
			}
		}
		return true;
	}
}
