using Downfall.DownfallCode.Utils.UI;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NCreature), "UpdateNavigation")]
public static class CreatureNavigationLinkPatch
{
	private static void Postfix(NCreature __instance)
	{
		DownfallControllerNav.ReapplyAnchorLink(__instance.Hitbox);
	}
}
