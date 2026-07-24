using Downfall.DownfallCode.Interfaces;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
public static class NCreatureAnimationPatch
{
	private static void Postfix(NCreature __instance, string trigger)
	{
		if (__instance.Visuals is IAnimatedVisuals animatedVisuals)
		{
			animatedVisuals.OnAnimationTrigger(trigger);
		}
	}
}
