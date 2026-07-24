using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(ArchaicTooth), "GetTranscendenceTransformedCard")]
internal static class TranscendenceTransformationPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardModel starterCard, CardModel __result)
	{
		TranscendenceHooks.RaiseTransformed(starterCard, __result);
	}
}
