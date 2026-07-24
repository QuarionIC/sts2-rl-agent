using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(PowerModel))]
internal class PowerModelLocPatch
{
	[HarmonyPatch("AddDumbVariablesToDescription")]
	[HarmonyPostfix]
	private static void Postfix(PowerModel __instance, LocString description)
	{
		if (__instance is IAddDumbVariablesToPowerDescription addDumbVariablesToPowerDescription)
		{
			addDumbVariablesToPowerDescription.AddDumbVariablesToPowerDescription(description);
		}
	}
}
