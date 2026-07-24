using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCard), "Reload")]
internal static class NCardReloadPortraitPatch
{
	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		CustomPortraitApplier.Apply(__instance);
	}
}
