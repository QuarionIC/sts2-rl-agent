using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class Purifier : CustomEventModel, IShrineEvent
{
	private const decimal HpLossPercent = 0.15m;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("HpLoss", 0m) };

	public override void CalculateVars()
	{
		((EventModel)this).DynamicVars["HpLoss"].BaseValue = (int)Math.Round((decimal)((EventModel)this).Owner.Creature.MaxHp * 0.15m);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Kneel, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()).ThatDecreasesMaxHp(((EventModel)this).DynamicVars["HpLoss"].BaseValue)
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Pray()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	private async Task Kneel()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, ((EventModel)this).DynamicVars["HpLoss"].BaseValue, false);
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 2);
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)(await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).ToList(), true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	public Purifier()
		: base(true)
	{
	}
}
