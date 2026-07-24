using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.SharedEvents;

public sealed class Duplicator : CustomEventModel, IShrineEvent
{
	private const int KneelDamage = 5;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("KneelDamage", 5m) };

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Count((CardModel c) => c.IsUpgraded) >= 2);
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Kneel, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(5m)
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
		CardSelectorPrefs prefs = new CardSelectorPrefs(((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".SELECT_DUPLICATE"), 1);
		CardModel card = (await CardSelectCmd.FromDeckGeneric(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null, (Func<CardModel, int>)null)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(((ICardScope)((EventModel)this).Owner.RunState).CloneCard(card), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
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
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 5m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		List<CardModel> upgraded = ListExtensions.StableShuffle<CardModel>(((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgraded).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(2).ToList();
		foreach (CardModel card in upgraded)
		{
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(((ICardScope)((EventModel)this).Owner.RunState).CloneCard(card), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 1.2f, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	public Duplicator()
		: base(true)
	{
	}
}
