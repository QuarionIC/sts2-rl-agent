using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(AncientDialogueSet), "GetValidDialogues")]
internal static class ForceVisitIndexConsolePatch
{
	private static void Prefix(ref int charVisits, ref int totalVisits)
	{
		int? forcedVisitIndex = AncientDebug.ForcedVisitIndex;
		if (forcedVisitIndex.HasValue)
		{
			int valueOrDefault = forcedVisitIndex.GetValueOrDefault();
			charVisits = valueOrDefault;
			totalVisits = Math.Max(totalVisits, 1);
			AncientDebug.ForcedVisitIndex = null;
		}
	}
}
