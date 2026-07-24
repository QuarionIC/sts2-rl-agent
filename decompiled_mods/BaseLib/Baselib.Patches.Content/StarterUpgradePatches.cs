using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(TouchOfOrobas), "GetUpgradedStarterRelic")]
internal class StarterUpgradePatches
{
	[HarmonyPrefix]
	private static bool CustomStarterUpgrade(RelicModel starterRelic, ref RelicModel? __result)
	{
		if (starterRelic is CustomRelicModel customRelicModel)
		{
			__result = customRelicModel.GetUpgradeReplacement();
			return __result == null;
		}
		return true;
	}
}
