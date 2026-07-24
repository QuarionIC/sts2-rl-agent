using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal class CustomResourceUiPatches
{
	[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
	[HarmonyPostfix]
	private static void UpdateCustomCostVisuals(NCard __instance, PileType pileType)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		CardModel model = __instance.Model;
		if (model == null)
		{
			return;
		}
		foreach (ResourceHandler registeredResource in CustomResourcePatches.RegisteredResources)
		{
			registeredResource.GetCost(model)?.UpdateCostVisuals(__instance, pileType);
		}
	}
}
