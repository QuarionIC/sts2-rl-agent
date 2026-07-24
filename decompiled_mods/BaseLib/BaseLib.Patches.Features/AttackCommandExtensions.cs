using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace BaseLib.Patches.Features;

public static class AttackCommandExtensions
{
	private static readonly FieldInfo AttackCommandCombatState = AccessToolsExtensions.DeclaredField(typeof(AttackCommand), "_combatState");

	public static AttackCommand TargetingFiltered(this AttackCommand cmd, IEnumerable<Creature> targets)
	{
		List<Creature> value = targets.ToList();
		AttackCommandGetPossibleTargetsPatch.CustomTargets.Add(cmd, new StrongBox<IReadOnlyList<Creature>>(value));
		if (cmd.Attacker == null)
		{
			return cmd;
		}
		AttackCommandCombatState.SetValue(cmd, cmd.Attacker.CombatState);
		return cmd;
	}
}
