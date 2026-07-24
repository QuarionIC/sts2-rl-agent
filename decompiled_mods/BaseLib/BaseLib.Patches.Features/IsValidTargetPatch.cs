using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(CardModel), "IsValidTarget")]
internal class IsValidTargetPatch
{
	[HarmonyPrefix]
	private static bool CustomValidTargets(CardModel __instance, Creature? target, ref bool __result)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		if (target == null)
		{
			return true;
		}
		CustomTargetType.SingleTargeting.TryGetValue(__instance.TargetType, out Func<Creature, Player, bool> value);
		if (value == null)
		{
			return true;
		}
		__result = value(target, __instance.Owner);
		return false;
	}
}
