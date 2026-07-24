using Downfall.DownfallCode.History;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(Creature), "ClearBlock")]
internal static class OnClearBlockPatch
{
	[HarmonyPrefix]
	private static bool SaveUnusedToHistory(Creature __instance)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		ICombatState combatState = __instance.CombatState;
		if (combatState == null)
		{
			return true;
		}
		UnusedBlockEntry unusedBlockEntry = new UnusedBlockEntry(__instance.Block, __instance, combatState.RoundNumber, __instance.Side, CombatManager.Instance.History, combatState.Players);
		CombatManager.Instance.History.Add(combatState, (CombatHistoryEntry)(object)unusedBlockEntry);
		return true;
	}
}
