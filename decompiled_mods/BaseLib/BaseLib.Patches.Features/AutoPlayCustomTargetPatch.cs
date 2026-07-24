using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(CardCmd), "AutoPlay")]
internal static class AutoPlayCustomTargetPatch
{
	[HarmonyPrefix]
	private static void PickRandomCustomTarget(CardModel card, ref Creature? target)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (target != null || card == null || !CustomTargetType.SingleTargeting.TryGetValue(card.TargetType, out Func<Creature, Player, bool> canTarget) || canTarget == null)
			{
				return;
			}
			Player player = card.Owner;
			Player obj = player;
			object obj2;
			if (obj == null)
			{
				obj2 = null;
			}
			else
			{
				IRunState runState = obj.RunState;
				obj2 = ((runState != null) ? runState.Rng : null);
			}
			if (obj2 == null)
			{
				return;
			}
			ICombatState combatState = card.CombatState;
			if (combatState != null)
			{
				List<Creature> list = combatState.Creatures.Where((Creature creature) => creature.IsAlive && canTarget(creature, player)).ToList();
				if (list.Count != 0)
				{
					target = player.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)list);
				}
			}
		}
		catch (Exception value)
		{
			BaseLibMain.Logger.Warn($"AutoPlay custom-target fallback failed; using vanilla behavior. {value}", 1);
		}
	}
}
