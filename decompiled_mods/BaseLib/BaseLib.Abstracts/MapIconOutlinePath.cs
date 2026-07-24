using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class MapIconOutlinePath
{
	[HarmonyPrefix]
	private static bool Custom(AncientEventModel __instance, ref string? __result)
	{
		if (!(__instance is CustomAncientModel customAncientModel))
		{
			return true;
		}
		__result = customAncientModel.CustomMapIconOutlinePath;
		return __result == null;
	}
}
