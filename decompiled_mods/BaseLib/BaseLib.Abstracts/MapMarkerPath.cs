using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class MapMarkerPath
{
	[HarmonyPrefix]
	private static bool Custom(CharacterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		__result = customCharacterModel.CustomMapMarkerPath;
		return __result == null;
	}
}
