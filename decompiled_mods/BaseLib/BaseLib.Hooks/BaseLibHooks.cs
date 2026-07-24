using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Hooks;

public static class BaseLibHooks
{
	public static Task AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardedAmount, List<CardModel> seen, List<CardModel> discarded)
	{
		return HookUtils.Dispatch(player.Creature.CombatState, ctx, (IAfterScryed m) => m.AfterScryed(ctx, player, scryAmount, discardedAmount, seen, discarded));
	}

	public static int ModifyScryAmount(Player player, int amount, out IEnumerable<IModifyScryAmount> modifiers)
	{
		return HookUtils.Modify(player.Creature.CombatState, amount, (IModifyScryAmount m, int a) => m.ModifyScryAmount(player, a), out modifiers);
	}

	public static Task AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, IEnumerable<IModifyScryAmount> modifiers, int originalAmount, int modifiedAmount)
	{
		return HookUtils.AfterModifying(player.Creature.CombatState, modifiers, (IModifyScryAmount a) => a.AfterModifyingScryAmount(ctx, player, originalAmount, modifiedAmount));
	}

	public static async Task AfterSpendCustomResource<T>(ICombatState combatState, T resource, AbstractModel? spender, int amount) where T : CustomResource
	{
		await HookUtils.Dispatch(combatState, (IAfterSpendResource<T> m) => m.AfterSpendResource(combatState, resource, spender, amount));
	}

	public static decimal ModifyResourceCostInCombat<T>(ICombatState combatState, T resource, CardModel card, decimal originalCost) where T : CustomResource
	{
		if (originalCost < 0m)
		{
			return originalCost;
		}
		IEnumerable<IModifyResourceCostInCombat<T>> modifiers;
		return HookUtils.Modify(combatState, originalCost, (IModifyResourceCostInCombat<T> modifier, decimal amt) => modifier.ModifyResourceCostInCombat(card, resource, amt), out modifiers);
	}
}
