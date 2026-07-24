using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomOrbIconPath
{
	[HarmonyPrefix]
	private static bool Custom(OrbModel __instance, ref string __result)
	{
		if (__instance is CustomOrbModel customOrbModel)
		{
			string customIconPath = customOrbModel.CustomIconPath;
			if (customIconPath != null)
			{
				__result = customIconPath;
				return false;
			}
		}
		return true;
	}
}
