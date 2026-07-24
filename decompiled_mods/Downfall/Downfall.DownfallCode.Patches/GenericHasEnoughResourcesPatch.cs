using System;
using System.Linq;
using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(PlayerCombatState), "HasEnoughResourcesFor")]
internal static class GenericHasEnoughResourcesPatch
{
	[HarmonyPrefix]
	private static bool HandleExclusiveResourceLogic(PlayerCombatState __instance, CardModel card, ref bool __result, ref UnplayableReason reason)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected I4, but got Unknown
		foreach (CardResource item in CardResourceRegistry.GetAll())
		{
			if (item.ShouldHandleResourceCheck(card) && item.UsesResourceExclusively(card))
			{
				(bool, UnplayableReason) tuple = item.CheckResources(card);
				__result = tuple.Item1;
				reason = (UnplayableReason)(int)tuple.Item2;
				return false;
			}
		}
		return true;
	}

	[HarmonyPostfix]
	private static void HandleHybridResourceLogic(PlayerCombatState __instance, CardModel card, ref bool __result, ref UnplayableReason reason)
	{
		if (!__result && ((Enum)reason).HasFlag((Enum)(object)(UnplayableReason)16) && (from resource in CardResourceRegistry.GetAll()
			where resource.ShouldHandleResourceCheck(card) && !resource.UsesResourceExclusively(card)
			select resource.CheckResources(card)).Any(((bool hasResources, UnplayableReason reason) check) => check.hasResources))
		{
			reason = (UnplayableReason)((uint)reason & 0xFFFFFFEFu);
			__result = (int)reason == 0;
		}
	}
}
