using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CharacterModel), "CreateVisuals")]
internal class CustomCharacterVisuals
{
	[HarmonyPrefix]
	private static bool UseCustomVisuals(CharacterModel __instance, ref NCreatureVisuals? __result)
	{
		if (!(__instance is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		__result = customCharacterModel.CreateCustomVisuals();
		return __result == null;
	}
}
