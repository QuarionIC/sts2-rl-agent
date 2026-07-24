using Downfall.DownfallCode.Interfaces;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
public static class NCreatureDeathAnimationPatch
{
	private static void Postfix(NCreature __instance)
	{
		if (__instance.Visuals is IAnimatedVisuals animatedVisuals)
		{
			animatedVisuals.OnAnimationTrigger("Dead");
		}
	}
}
