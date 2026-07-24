using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Hooks;

public interface IAfterCardDowngraded
{
	[HarmonyPatch(typeof(CardModel), "DowngradeInternal")]
	private static class DowngradeHook
	{
		[HarmonyPostfix]
		private static void Patch(CardModel __instance)
		{
			ICombatState combatState = __instance.CombatState;
			Player owner = __instance.Owner;
			object obj = ((owner != null) ? owner.RunState : null);
			if (obj == null)
			{
				if (combatState != null)
				{
					obj = combatState.RunState;
				}
				else
				{
					IRunState instance = (IRunState)(object)NullRunState.Instance;
					obj = instance;
				}
			}
			foreach (AbstractModel item in ((IRunState)obj).IterateHookListeners(combatState))
			{
				(item as IAfterCardDowngraded)?.AfterCardDowngraded(__instance);
			}
		}
	}

	void AfterCardDowngraded(CardModel card);
}
