using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(CardModel), "SpendResources")]
internal static class GenericSpendResourcesPatch
{
	[HarmonyPrefix]
	private static bool HandleResourceSpending(CardModel __instance, ref Task<(int, int)> __result)
	{
		if (__instance.Owner.PlayerCombatState == null)
		{
			return true;
		}
		foreach (CardResource item in CardResourceRegistry.GetAll())
		{
			if (item.ShouldHandleSpending(__instance))
			{
				(int, int) result = item.HandleSpending(__instance);
				__result = Task.FromResult(result);
				return !item.UsesResourceExclusively(__instance);
			}
		}
		return true;
	}
}
