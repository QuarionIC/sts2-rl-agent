using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
internal static class CustomIntentLabelPatch
{
	private static void Postfix(NIntent __instance)
	{
		if (__instance._intent is CustomIntent customIntent)
		{
			__instance._valueLabel.Text = ((AbstractIntent)customIntent).GetIntentLabel(__instance._targets, __instance._owner).GetFormattedText();
		}
	}
}
