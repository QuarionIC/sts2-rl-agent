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

public sealed class UpgradeShrine : CustomEventModel, IShrineEvent
{
	private const int KneelDamage = 10;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("KneelDamage", 10m) };

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => c.IsUpgradable));
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		List<EventOption> list = new List<EventOption>();
		if (((EventModel)this).Owner.Deck.Cards.Any((CardModel c) => c.IsUpgradable))
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Pray, "INITIAL", Array.Empty<IHoverTip>()));
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.PRAY_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Kneel, "INITIAL_REBALANCED", Array.Empty<IHoverTip>()).ThatDoesDamage(10m));
		}
		else
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task Pray()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForUpgrade(((EventModel)this).Owner, prefs)).FirstOrDefault();
		if (card != null)
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
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
		await CreatureCmd.Damage((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((EventModel)this).Owner.Creature, 10m, (ValueProp)6, (CardModel)null, (CardPlay)null);
		foreach (CardModel card in ListExtensions.StableShuffle<CardModel>(((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => c.IsUpgradable).ToList(), ((EventModel)this).Owner.RunState.Rng.Niche).Take(2))
		{
			CardCmd.Upgrade(card, (CardPreviewStyle)1);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("PRAY"));
	}

	public UpgradeShrine()
		: base(true)
	{
	}
}
