using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NTargetManager), "AllowedToTargetCreature")]
internal class AllowedToTargetCreaturePatch
{
	[HarmonyPrefix]
	private static bool CustomTargetingAllowed(NTargetManager __instance, Creature creature, ref bool __result)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		CustomTargetType.SingleTargeting.TryGetValue(__instance._validTargetsType, out Func<Creature, Player, bool> value);
		if (value == null)
		{
			return true;
		}
		Player me = LocalContext.GetMe((IPlayerCollection)(object)RunManager.Instance.State);
		if (me == null)
		{
			return true;
		}
		__result = value(creature, me);
		return false;
	}
}
