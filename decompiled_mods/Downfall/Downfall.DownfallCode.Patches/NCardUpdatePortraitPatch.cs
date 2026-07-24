using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class NCardUpdatePortraitPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		CustomPortraitApplier.Apply(__instance);
	}
}
