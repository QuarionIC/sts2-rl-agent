using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Snecko.SneckoCode.Events;

public static class SneckoHook
{
	public static Task AfterCardMuddled(ICombatState cs, PlayerChoiceContext ctx, CardModel card, AbstractModel? source)
	{
		return HookUtils.Dispatch<IAfterCardMuddled>(cs, ctx, (Func<IAfterCardMuddled, Task>)((IAfterCardMuddled m) => m.AfterCardMuddled(ctx, card, source)));
	}

	public static Task AfterOverflowEffect(Player player, CardPlay cardPlay, CardModel card)
	{
		return HookUtils.DispatchWithContext<IAfterOverflowEffect>(player, (Func<IAfterOverflowEffect, PlayerChoiceContext, Task>)((IAfterOverflowEffect m, PlayerChoiceContext ctx) => m.AfterOverflowEffect(ctx, cardPlay, card)));
	}

	public static bool ShouldAllowMuddleCost(ICombatState cs, CardModel card, int cost)
	{
		return HookUtils.All<IShouldAllowMuddleCost>(cs, (Func<IShouldAllowMuddleCost, bool>)((IShouldAllowMuddleCost m) => m.ShouldAllowMuddleCost(card, cost)));
	}
}
