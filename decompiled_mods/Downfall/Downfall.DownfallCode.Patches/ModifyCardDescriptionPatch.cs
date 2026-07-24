using Downfall.DownfallCode.Interfaces;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public static class ModifyCardDescriptionPatch
{
	private static bool Prefix(CardModel __instance, ref LocString __result)
	{
		if (!(__instance is IModfyCardDescription modfyCardDescription))
		{
			return true;
		}
		__result = modfyCardDescription.ModifyDescription(__result);
		return __result == null;
	}
}
