using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class InitialPortraitPath
{
	[HarmonyPrefix]
	private static bool CustomInitialPortraitPath(EventModel __instance, ref string? __result)
	{
		if (!(__instance is CustomEventModel customEventModel))
		{
			return true;
		}
		__result = customEventModel.CustomInitialPortraitPath;
		return __result == null;
	}
}
