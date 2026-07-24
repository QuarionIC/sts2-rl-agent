using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using ActsFromThePast.Relics;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.SharedEvents;

public sealed class BonfireSpirits : CustomEventModel, IShrineEvent
{
	private const int CommonHeal = 5;

	private const int RareMaxHpGain = 10;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		return (IReadOnlyList<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Continue, "INITIAL", Array.Empty<IHoverTip>()) };
	}

	public override void OnRoomEnter()
	{
		AFTPModAudio.Play("events", "goop");
	}

	private Task Continue()
	{
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("APPROACH"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Offer, "APPROACH", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private async Task Offer()
	{
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromDeckForRemoval(((EventModel)this).Owner, prefs, (Func<CardModel, bool>)null)).FirstOrDefault();
		if (card == null)
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("NOTHING"));
			return;
		}
		CardRarity rarity = card.Rarity;
		bool isCurse = (int)card.Type == 5;
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)new List<CardModel> { card }, true);
		if (isCurse)
		{
			await RelicCmd.Obtain(((RelicModel)ModelDb.Relic<SpiritPoop>()).ToMutable(), ((EventModel)this).Owner, -1);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_CURSE"));
		}
		else if ((int)rarity == 1)
		{
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_BASIC"));
		}
		else if ((int)rarity == 2)
		{
			await CreatureCmd.Heal(((EventModel)this).Owner.Creature, 5m, true);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_COMMON"));
		}
		else if ((int)rarity == 3 || (int)rarity == 10)
		{
			await CreatureCmd.Heal(((EventModel)this).Owner.Creature, (decimal)((EventModel)this).Owner.Creature.MaxHp, true);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_UNCOMMON"));
		}
		else if ((int)rarity == 4 || (int)rarity == 5)
		{
			await CreatureCmd.GainMaxHp(((EventModel)this).Owner.Creature, 10m);
			await CreatureCmd.Heal(((EventModel)this).Owner.Creature, (decimal)((EventModel)this).Owner.Creature.MaxHp, true);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_RARE"));
		}
		else
		{
			await CreatureCmd.Heal(((EventModel)this).Owner.Creature, 5m, true);
			((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("OFFER_COMMON"));
		}
	}

	public BonfireSpirits()
		: base(true)
	{
	}
}
