using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(CardCmd), "AutoPlay")]
internal static class CardCmdAutoPlayAnyPlayerPatch
{
	[HarmonyPrefix]
	private static void RandomAnyPlayer(CardModel card, ref Creature? target)
	{
		if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card) || target != null)
		{
			return;
		}
		ICombatState val = card.CombatState ?? card.Owner.Creature.CombatState;
		if (val != null)
		{
			IEnumerable<Creature> enumerable = val.PlayerCreatures.Where((Creature c) => c != null && c.IsAlive && c.IsPlayer);
			target = card.Owner.RunState.Rng.CombatTargets.NextItem<Creature>(enumerable);
		}
	}
}
