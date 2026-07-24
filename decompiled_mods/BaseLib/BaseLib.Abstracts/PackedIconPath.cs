using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class PackedIconPath
{
	[HarmonyPrefix]
	private static bool Custom(PowerModel __instance, ref string? __result)
	{
		if (!(__instance is ICustomPower customPower))
		{
			return true;
		}
		__result = customPower.CustomPackedIconPath;
		return __result == null;
	}
}
