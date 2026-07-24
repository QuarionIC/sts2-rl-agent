using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(CardModel), "IsValidTarget")]
internal static class CardModelIsValidTargetAnyPlayerPatch
{
	[HarmonyPrefix]
	private static bool CheckValidPlayerTarget(CardModel __instance, Creature? target, ref bool __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)__instance.TargetType != 5)
		{
			return true;
		}
		if (target == null)
		{
			__result = ((IPlayerCollection)__instance.Owner.RunState).Players.Count <= 1;
			return false;
		}
		__result = target != null && target.IsAlive && target.IsPlayer;
		return false;
	}
}
