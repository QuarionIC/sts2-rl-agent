using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(NMouseCardPlay), "TargetSelection")]
internal static class NMouseCardPlayTargetSelectionAnyPlayerPatch
{
	[HarmonyPrefix]
	private static bool AnyPlayerTargeting(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(((NCardPlay)__instance).Card))
		{
			return true;
		}
		__result = RunAnyPlayerTargeting(__instance, targetMode);
		return false;
	}

	private static async Task RunAnyPlayerTargeting(NMouseCardPlay instance, TargetMode targetMode)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		NCard cardNode = ((NCardPlay)instance).CardNode;
		if (cardNode != null)
		{
			((NCardPlay)instance).TryShowEvokingOrbs();
			cardNode.CardHighlight.AnimFlash();
			await instance.SingleCreatureTargeting(targetMode, (TargetType)5);
		}
	}
}
