using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Events;

public class HermitHook
{
	public static bool ShouldTriggerDeadOn(ICombatState cs, CardModel card)
	{
		return HookUtils.Any<IShouldTriggerDeadOn>(cs, (Func<IShouldTriggerDeadOn, bool>)((IShouldTriggerDeadOn e) => e.ShouldTriggerDeadOn(card)));
	}

	public static Task AfterDeadOnTrigger(ICombatState cs, PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		return HookUtils.Dispatch<IAfterDeadOnTrigger>(cs, (Func<IAfterDeadOnTrigger, Task>)((IAfterDeadOnTrigger e) => e.AfterDeadOnTrigger(ctx, card, cardPlay)));
	}

	public static int ModifyDeadOnCount(ICombatState cs, int orignal, CardModel card, out IEnumerable<IModifyDeadOnCount> modifiers)
	{
		return HookUtils.Modify<IModifyDeadOnCount, int>(cs, orignal, (Func<IModifyDeadOnCount, int, int>)((IModifyDeadOnCount e, int amount) => e.ModifyDeadOnCount(amount, card)), ref modifiers);
	}

	public static Task AfterModifyingDeadOnCount(ICombatState cs, PlayerChoiceContext ctx, CardModel card, IEnumerable<IModifyDeadOnCount> modifiers)
	{
		return HookUtils.AfterModifying<IModifyDeadOnCount>(cs, modifiers, (Func<IModifyDeadOnCount, Task>)((IModifyDeadOnCount e) => e.AfterModifyingDeadOnCount(ctx, card)));
	}

	public static bool ShouldPreventBruiseRemoval(ICombatState cs, BruisePower power, out IEnumerable<IShouldPreventBruiseRemoval> preventers)
	{
		return HookUtils.Any<IShouldPreventBruiseRemoval>(cs, (Func<IShouldPreventBruiseRemoval, bool>)((IShouldPreventBruiseRemoval h) => h.ShouldPreventBruiseRemoval(power)), ref preventers);
	}

	public static Task AfterPreventedBruiseRemoval(ICombatState cs, BruisePower power, IEnumerable<IShouldPreventBruiseRemoval> preventers)
	{
		return HookUtils.AfterModifying<IShouldPreventBruiseRemoval>(cs, preventers, (Func<IShouldPreventBruiseRemoval, Task>)((IShouldPreventBruiseRemoval h) => h.AfterPreventedBruiseRemoval(power)));
	}
}
