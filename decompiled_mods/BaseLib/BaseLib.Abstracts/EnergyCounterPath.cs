using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class EnergyCounterPath
{
	[HarmonyPrefix]
	private static bool Custom(CharacterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		__result = customCharacterModel.CustomEnergyCounterPath ?? (customCharacterModel.CustomEnergyCounter.HasValue ? SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter") : null);
		return __result == null;
	}
}
