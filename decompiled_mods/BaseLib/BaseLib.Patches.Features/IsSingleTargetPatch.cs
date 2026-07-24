using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(ActionTargetExtensions), "IsSingleTarget")]
internal class IsSingleTargetPatch
{
	[HarmonyPostfix]
	private static void CustomSingleTargets(TargetType targetType, ref bool __result)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		if (!__result && CustomTargetType.SingleTargeting.ContainsKey(targetType))
		{
			__result = true;
		}
	}
}
