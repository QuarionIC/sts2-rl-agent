using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class EventBackgroundScenePath
{
	[HarmonyPrefix]
	private static bool CustomInitialPortraitPath(EventModel __instance, ref string? __result)
	{
		if (!(__instance is CustomEventModel customEventModel))
		{
			return true;
		}
		__result = customEventModel.CustomBackgroundScenePath;
		return __result == null;
	}
}
