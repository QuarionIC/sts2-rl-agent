using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public static class HookUtils
{
	public static async Task Dispatch<THook>(ICombatState? combatState, Func<THook, Task> action) where THook : class
	{
		if (combatState == null)
		{
			return;
		}
		foreach (THook item in Hook.IterateCombatHookListeners(combatState).OfType<THook>())
		{
			await action(item);
		}
	}

	public static async Task Dispatch<THook>(ICombatState? combatState, PlayerChoiceContext ctx, Func<THook, Task> action) where THook : class
	{
		if (combatState == null)
		{
			return;
		}
		foreach (THook item in Hook.IterateCombatHookListeners(combatState).OfType<THook>())
		{
			AbstractModel abstractModel = (AbstractModel)(object)((item is AbstractModel) ? item : null);
			if (abstractModel != null)
			{
				ctx.PushModel(abstractModel);
				await action(item);
				ctx.PopModel(abstractModel);
			}
		}
	}

	public static async Task DispatchWithContext<THook>(Player player, Func<THook, PlayerChoiceContext, Task> action) where THook : class
	{
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}
		ulong netId = player.NetId;
		foreach (THook item in Hook.IterateCombatHookListeners(combatState).OfType<THook>())
		{
			AbstractModel abstractModel = (AbstractModel)(object)((item is AbstractModel) ? item : null);
			if (abstractModel != null)
			{
				HookPlayerChoiceContext val = new HookPlayerChoiceContext(abstractModel, netId, combatState, (GameActionType)1);
				Task task = action(item, (PlayerChoiceContext)(object)val);
				await val.AssignTaskAndWaitForPauseOrCompletion(task);
				abstractModel.InvokeExecutionFinished();
			}
		}
	}

	public static TResult Aggregate<THook, TResult>(ICombatState combatState, TResult initial, Func<THook, TResult, TResult> action) where THook : class
	{
		return Hook.IterateCombatHookListeners(combatState).OfType<THook>().Aggregate(initial, (TResult current, THook model) => action(model, current));
	}

	public static bool All<THook>(ICombatState combatState, Func<THook, bool> predicate) where THook : class
	{
		return Hook.IterateCombatHookListeners(combatState).OfType<THook>().All(predicate);
	}

	public static bool All<THook>(ICombatState combatState, Func<THook, bool> predicate, out IEnumerable<THook> nonMatches) where THook : class
	{
		return ((List<THook>)(nonMatches = (from m in Hook.IterateCombatHookListeners(combatState).OfType<THook>()
			where !predicate(m)
			select m).ToList())).Count == 0;
	}

	public static bool Any<THook>(ICombatState combatState, Func<THook, bool> predicate) where THook : class
	{
		return Hook.IterateCombatHookListeners(combatState).OfType<THook>().Any(predicate);
	}

	public static bool Any<THook>(ICombatState combatState, Func<THook, bool> predicate, out IEnumerable<THook> matches) where THook : class
	{
		return ((List<THook>)(matches = Hook.IterateCombatHookListeners(combatState).OfType<THook>().Where(predicate)
			.ToList())).Count > 0;
	}

	public static TValue Modify<THook, TValue>(ICombatState? combatState, TValue originalAmount, Func<THook, TValue, TValue> amountModifier, out IEnumerable<THook> modifiers) where THook : class where TValue : IEquatable<TValue>
	{
		if (combatState == null)
		{
			modifiers = Array.Empty<THook>();
			return originalAmount;
		}
		TValue val = originalAmount;
		List<THook> list = new List<THook>();
		foreach (THook item in Hook.IterateCombatHookListeners(combatState).OfType<THook>())
		{
			TValue val2 = val;
			val = amountModifier(item, val);
			if (!val2.Equals(val))
			{
				list.Add(item);
			}
		}
		modifiers = list;
		return val;
	}

	public static async Task AfterModifying<THook>(ICombatState cs, IEnumerable<THook> modifiers, Func<THook, Task> action) where THook : class
	{
		HashSet<THook> modifierSet = new HashSet<THook>(modifiers);
		foreach (THook iterateHookListener in Hook.IterateCombatHookListeners(cs).OfType<THook>())
		{
			if (modifierSet.Contains(iterateHookListener))
			{
				await action(iterateHookListener);
				AbstractModel val = (AbstractModel)(object)((iterateHookListener is AbstractModel) ? iterateHookListener : null);
				if (val != null)
				{
					val.InvokeExecutionFinished();
				}
			}
		}
	}

	public static TValue ModifyMutable<THook, TValue>(ICombatState combatState, TValue value, Func<THook, TValue, bool> amountModifier, out IEnumerable<THook> modifiers) where THook : class
	{
		List<THook> list = (from model in Hook.IterateCombatHookListeners(combatState).OfType<THook>()
			where amountModifier(model, value)
			select model).ToList();
		modifiers = list;
		return value;
	}
}
