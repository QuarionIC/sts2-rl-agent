using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NMouseCardPlay), "TargetSelection")]
internal class TargetSelectionPatch
{
	[HarmonyPrefix]
	private static bool CustomMouseTargetSelection(NMouseCardPlay __instance, TargetMode targetMode, ref Task __result)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (((NCardPlay)__instance).Card == null || !CustomTargetType.SingleTargeting.ContainsKey(((NCardPlay)__instance).Card.TargetType))
		{
			return true;
		}
		__result = AnyoneTargetSelectionAsync(__instance, targetMode, ((NCardPlay)__instance).Card);
		return false;
	}

	private static async Task AnyoneTargetSelectionAsync(NMouseCardPlay __instance, TargetMode targetMode, CardModel type)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((NCardPlay)__instance).TryShowEvokingOrbs();
		NCard cardNode = ((NCardPlay)__instance).CardNode;
		if (cardNode != null)
		{
			cardNode.CardHighlight.AnimFlash();
		}
		await __instance.SingleCreatureTargeting(targetMode, type.TargetType);
	}
}
