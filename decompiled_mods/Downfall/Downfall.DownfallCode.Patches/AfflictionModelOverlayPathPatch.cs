using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class AfflictionModelOverlayPathPatch
{
	private static bool Prefix(AfflictionModel __instance, ref string __result)
	{
		if (!(__instance is CustomAfflictionModel customAfflictionModel))
		{
			return true;
		}
		string customOverlayPath = customAfflictionModel.CustomOverlayPath;
		if (customOverlayPath == null)
		{
			return true;
		}
		__result = customOverlayPath;
		return false;
	}
}
