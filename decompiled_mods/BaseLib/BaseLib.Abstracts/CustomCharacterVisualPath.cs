using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomCharacterVisualPath
{
	[HarmonyPrefix]
	private static bool UseCustomScene(CharacterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		__result = customCharacterModel.CustomVisualPath;
		return __result == null;
	}
}
