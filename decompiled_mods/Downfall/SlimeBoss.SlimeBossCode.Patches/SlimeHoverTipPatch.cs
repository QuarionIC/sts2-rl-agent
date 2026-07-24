using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Patches;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal static class SlimeHoverTipPatch
{
	private static void Postfix(Creature __instance, ref IEnumerable<IHoverTip> __result)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		if (__instance.Monster is SlimeModel slimeModel)
		{
			__result = __result.Append((IHoverTip)(object)slimeModel.SlimeTip);
			__result = __result.Concat(slimeModel.ExtraTips);
		}
	}
}
