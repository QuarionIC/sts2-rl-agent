using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Patches;

[HarmonyPatch(typeof(NCreature))]
public static class SlimeDeathPatches
{
	private static readonly HashSet<NCreature> DyingSlimes = new HashSet<NCreature>();

	[HarmonyPrefix]
	[HarmonyPatch("StartDeathAnim")]
	public static void Prefix(NCreature __instance, ref bool shouldRemove)
	{
		if (__instance.Entity.Monster is SlimeModel)
		{
			DyingSlimes.Add(__instance);
			shouldRemove = true;
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				instance.RemoveCreatureNode(__instance);
			}
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch("GetCurrentAnimationTimeRemaining")]
	public static bool Prefix(NCreature __instance, ref float __result)
	{
		if (!DyingSlimes.Contains(__instance))
		{
			return true;
		}
		DyingSlimes.Remove(__instance);
		__result = 0f;
		return false;
	}
}
