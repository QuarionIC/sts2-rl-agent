using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Events;

public static class GuardianHook
{
	public static Task AfterGuardianModeChange(ICombatState cs, PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		return HookUtils.Dispatch<IAfterGuardianModeChange>(cs, (Func<IAfterGuardianModeChange, Task>)((IAfterGuardianModeChange m) => m.AfterGuardianModeChange(ctx, player, oldMode, newMode)));
	}

	public static Task AfterGuardianModeChangeEarly(ICombatState cs, PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		return HookUtils.Dispatch<IAfterGuardianModeChangeEarly>(cs, (Func<IAfterGuardianModeChangeEarly, Task>)((IAfterGuardianModeChangeEarly m) => m.AfterGuardianModeChangeEarly(ctx, player, oldMode, newMode)));
	}

	public static Task BeforeCardEntersStasis(ICombatState cs, PlayerChoiceContext ctx, CardModel card, AbstractModel source)
	{
		return HookUtils.Dispatch<IBeforeCardEntersStasis>(cs, ctx, (Func<IBeforeCardEntersStasis, Task>)((IBeforeCardEntersStasis m) => m.BeforeCardEntersStasis(ctx, card, source)));
	}

	public static Task AfterCardEntersStasis(ICombatState cs, PlayerChoiceContext ctx, CardModel card, AbstractModel source)
	{
		return HookUtils.Dispatch<IAfterCardEntersStasis>(cs, ctx, (Func<IAfterCardEntersStasis, Task>)((IAfterCardEntersStasis m) => m.AfterCardEntersStasis(ctx, card, source)));
	}

	public static decimal ModifyGemEffect(ICombatState cs, GemModel gem, decimal baseValue, CardModel? card)
	{
		return HookUtils.Aggregate<IModifyGemEffect, decimal>(cs, baseValue, (Func<IModifyGemEffect, decimal, decimal>)((IModifyGemEffect m, decimal val) => m.ModifyGemEffect(gem, val, card)));
	}

	public static Task AfterGemPlayed(ICombatState cs, PlayerChoiceContext ctx, GemModel gemModel, CardPlay? cardPlay)
	{
		return HookUtils.Dispatch<IAfterGemPlayed>(cs, ctx, (Func<IAfterGemPlayed, Task>)((IAfterGemPlayed m) => m.AfterGemPlayed(ctx, gemModel, cardPlay)));
	}

	public static Task AfterCardTick(ICombatState cs, PlayerChoiceContext ctx, CardModel card, Player player)
	{
		return HookUtils.Dispatch<IAfterCardTick>(cs, ctx, (Func<IAfterCardTick, Task>)((IAfterCardTick m) => m.AfterCardTick(ctx, card, player)));
	}

	public static decimal ModifyBraceAmount(ICombatState cs, Player player, decimal amount)
	{
		return HookUtils.Aggregate<IModifyBraceAmount, decimal>(cs, amount, (Func<IModifyBraceAmount, decimal, decimal>)((IModifyBraceAmount m, decimal val) => m.ModifyBraceAmount(player, amount)));
	}
}
