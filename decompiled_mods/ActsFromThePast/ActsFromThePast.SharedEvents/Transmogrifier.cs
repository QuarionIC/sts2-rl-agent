using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.SharedEvents;

public sealed class Transmogrifier : CustomEventModel, IShrineEvent
{
	private const int KneelMaxHpLoss = 10;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("KneelMaxHpLoss", 10m) };

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Kneel, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()).ThatDecreasesMaxHp(10m)
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
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 1);
		foreach (CardModel original in (await CardSelectCmd.FromDeckForTransformation(((EventModel)this).Owner, prefs, (Func<CardModel, CardTransformation>)null)).ToList())
		{
			await CardCmd.TransformToRandom(original, ((EventModel)this).Rng, (CardPreviewStyle)3);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private async Task Kneel()
	{
		await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 10m, false);
		List<CardModel> cards = ListExtensions.StableShuffle<CardModel>(((EventModel)this).Owner.Deck.Cards.ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(2).ToList();
		foreach (CardModel original in cards)
		{
			CardModel transformed = CardFactory.CreateRandomCardForTransform(original, false, ((EventModel)this).Owner.RunState.Rng.Niche);
			CardCmd.Upgrade(transformed, (CardPreviewStyle)1);
			await CardCmd.Transform(original, transformed, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	public Transmogrifier()
		: base(true)
	{
	}
}
