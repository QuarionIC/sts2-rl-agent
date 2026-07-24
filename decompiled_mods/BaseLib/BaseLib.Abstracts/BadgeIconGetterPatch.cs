using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class BadgeIconGetterPatch
{
	[HarmonyPrefix]
	private static bool CustomPath(Badge __instance, ref string? __result)
	{
		__result = CustomBadge.CustomBadgeIconPathDict[__instance];
		BaseLibMain.Logger.Info($"Got custom badge path {__result} for badge {__instance}", 1);
		return __result == null;
	}
}
