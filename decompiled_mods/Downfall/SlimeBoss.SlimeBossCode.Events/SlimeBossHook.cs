using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Events;

public static class SlimeBossHook
{
	public static Task AfterConsumeEffect(ICombatState cs, PlayerChoiceContext ctx, Creature creature, Creature attacker, int amount)
	{
		return HookUtils.Dispatch<IAfterConsumeEffect>(cs, (Func<IAfterConsumeEffect, Task>)((IAfterConsumeEffect e) => e.AfterConsumeEffect(ctx, creature, attacker, amount)));
	}

	public static int ModifyGoopConsume(ICombatState cs, int originalAmount, out IEnumerable<IModifyGoopConsume> modifiers, Creature creature, Creature? applier)
	{
		return HookUtils.Modify<IModifyGoopConsume, int>(cs, originalAmount, (Func<IModifyGoopConsume, int, int>)((IModifyGoopConsume e, int a) => e.ModifyGoopConsume(a, creature, applier)), ref modifiers);
	}

	public static Task AfterModifyingGoopConsume(ICombatState cs, IEnumerable<IModifyGoopConsume> modifiers, Creature creature, Creature? applier)
	{
		return HookUtils.AfterModifying<IModifyGoopConsume>(cs, modifiers, (Func<IModifyGoopConsume, Task>)((IModifyGoopConsume e) => e.AfterModifyingGoopConsume(creature, applier)));
	}

	public static int ModifySecondarySlimeEffects(ICombatState cs, int originalAmount, out IEnumerable<IModifySecondarySlimeEffects> modifiers, SlimeModel slime)
	{
		return HookUtils.Modify<IModifySecondarySlimeEffects, int>(cs, originalAmount, (Func<IModifySecondarySlimeEffects, int, int>)((IModifySecondarySlimeEffects e, int a) => e.ModifySecondarySlimeEffects(a, slime)), ref modifiers);
	}

	public static Task AfterSplit(ICombatState cs, Player player, SlimeModel slime)
	{
		return HookUtils.Dispatch<IAfterSplit>(cs, (Func<IAfterSplit, Task>)((IAfterSplit e) => e.AfterSplit(player, slime)));
	}

	public static int ModifyConsumeCount(ICombatState cs, Player player, int amount, CardModel? cardSource, out IEnumerable<IModifyConsumeCount> modifiers)
	{
		return HookUtils.Modify<IModifyConsumeCount, int>(cs, amount, (Func<IModifyConsumeCount, int, int>)((IModifyConsumeCount e, int a) => e.ModifyConsumeCount(player, a, cardSource)), ref modifiers);
	}

	public static Task AfterModifyingConsumeCount(ICombatState cs, IEnumerable<IModifyConsumeCount> modifiers, Player player, CardModel? cardSource)
	{
		return HookUtils.AfterModifying<IModifyConsumeCount>(cs, modifiers, (Func<IModifyConsumeCount, Task>)((IModifyConsumeCount e) => e.AfterModifyingConsumeCount(player, cardSource)));
	}
}
