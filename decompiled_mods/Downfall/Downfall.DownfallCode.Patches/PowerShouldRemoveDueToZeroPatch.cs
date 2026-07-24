using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(PowerModel), "ShouldRemoveDueToAmount")]
public static class PowerShouldRemoveDueToZeroPatch
{
	[HarmonyPrefix]
	public static bool ShouldRemoveOnZero(PowerModel __instance, ref bool __result)
	{
		if (!(__instance is ConstructedPowerModel { ShouldRemoveDueToZero: false }))
		{
			return true;
		}
		__result = false;
		return false;
	}
}
