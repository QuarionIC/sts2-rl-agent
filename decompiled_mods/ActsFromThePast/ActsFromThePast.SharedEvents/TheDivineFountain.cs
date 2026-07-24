using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class TheDivineFountain : CustomEventModel, IShrineEvent
{
	private const int MaxHpPerCurse = 3;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new IntVar("MaxHpGain", 0m) };

	public override void CalculateVars()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			int num = ((EventModel)this).Owner.Deck.Cards.Count((CardModel c) => (int)c.Type == 5);
			((EventModel)this).DynamicVars["MaxHpGain"].BaseValue = num * 3;
		}
	}

	public override bool IsAllowed(IRunState runState)
	{
		return ((IPlayerCollection)runState).Players.All((Player p) => PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => (int)c.Type == 5 && c.IsRemovable && !(c is Guilty)));
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Drink, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Bathe, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Drink, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private async Task Drink()
	{
		List<CardModel> curses = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => (int)c.Type == 5 && c.IsRemovable).ToList();
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)curses, true);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DRINK"));
	}

	private async Task Bathe()
	{
		int curseCount = ((EventModel)this).Owner.Deck.Cards.Count((CardModel c) => (int)c.Type == 5);
		int maxHpGain = curseCount * 3;
		if (maxHpGain > 0)
		{
			await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, (decimal)maxHpGain);
		}
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("BATHE"));
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	public TheDivineFountain()
		: base(true)
	{
	}
}
