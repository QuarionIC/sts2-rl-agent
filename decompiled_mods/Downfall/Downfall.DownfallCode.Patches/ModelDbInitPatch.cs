using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(ModelDb), "InitIds")]
internal static class ModelDbInitPatch
{
	[HarmonyPostfix]
	private static void Postfix()
	{
		PostInitRegistry.RunAll();
	}
}
