using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.SharedEvents;

public sealed class WeMeetAgain : CustomEventModel, IShrineEvent
{
	private const int MinGold = 50;

	private const int MaxGold = 150;

	private PotionModel _potionOption;

	private CardModel _cardOption;

	private int _goldAmount;

	public override ActModel[] Acts => Array.Empty<ActModel>();

	bool IShrineEvent.IsOneTimeEvent => true;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[3]
	{
		(DynamicVar)new IntVar("GoldAmount", 0m),
		(DynamicVar)new StringVar("PotionName", ""),
		(DynamicVar)new StringVar("CardName", "")
	};

	public override bool IsAllowed(IRunState runState)
	{
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			return true;
		}
		return ((IPlayerCollection)runState).Players.All(delegate(Player p)
		{
			int num = 0;
			if (p.Potions.Any())
			{
				num++;
			}
			if (p.Gold >= 50)
			{
				num++;
			}
			if (PileTypeExtensions.GetPile((PileType)6, p).Cards.Any((CardModel c) => (int)c.Rarity != 1 && (int)c.Type != 5))
			{
				num++;
			}
			return num >= 2;
		});
	}

	public override void CalculateVars()
	{
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		_potionOption = (((EventModel)this).Owner.Potions.Any() ? ((EventModel)this).Rng.NextItem<PotionModel>(((EventModel)this).Owner.Potions) : null);
		List<CardModel> list = ((EventModel)this).Owner.Deck.Cards.Where((CardModel c) => (int)c.Rarity != 1 && (int)c.Type != 5).ToList();
		_cardOption = (list.Any() ? ((EventModel)this).Rng.NextItem<CardModel>((IEnumerable<CardModel>)list) : null);
		if (((EventModel)this).Owner.Gold < 50)
		{
			_goldAmount = 0;
		}
		else if (((EventModel)this).Owner.Gold > 150)
		{
			_goldAmount = ((EventModel)this).Rng.NextInt(50, 151);
		}
		else
		{
			_goldAmount = ((EventModel)this).Rng.NextInt(50, ((EventModel)this).Owner.Gold + 1);
		}
		((EventModel)this).DynamicVars["GoldAmount"].BaseValue = _goldAmount;
		if (_potionOption != null)
		{
			((StringVar)((EventModel)this).DynamicVars["PotionName"]).StringValue = _potionOption.Title.GetFormattedText();
		}
		if (_cardOption != null)
		{
			((StringVar)((EventModel)this).DynamicVars["CardName"]).StringValue = _cardOption.Title;
		}
	}

	protected override Task BeforeEventStarted(bool isPreFinished)
	{
		((EventModel)this).Owner.CanUseOrRemovePotions = false;
		return Task.CompletedTask;
	}

	protected override void OnEventFinished()
	{
		((EventModel)this).Owner.CanUseOrRemovePotions = true;
	}

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Expected O, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		List<EventOption> list = new List<EventOption>();
		if (_potionOption != null)
		{
			LocString val = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_POTION.title");
			LocString val2 = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_POTION.description");
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)async delegate
			{
				await GivePotion(_potionOption);
			}, val, val2, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_POTION", _potionOption.HoverTips).ThatHasDynamicTitle());
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_POTION_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (_goldAmount > 0)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)GiveGold, "INITIAL", Array.Empty<IHoverTip>()).ThatHasDynamicTitle());
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_GOLD_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (_cardOption != null)
		{
			LocString val3 = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_CARD.title");
			LocString val4 = ((EventModel)this).L10NLookup(((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_CARD.description");
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)async delegate
			{
				await GiveCard(_cardOption);
			}, val3, val4, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_CARD", (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard(_cardOption, false) }).ThatHasDynamicTitle());
		}
		else
		{
			list.Add(new EventOption((EventModel)(object)this, (Func<Task>)null, ((AbstractModel)this).Id.Entry + ".pages.INITIAL.options.GIVE_CARD_LOCKED", Array.Empty<IHoverTip>()));
		}
		if (!ActsFromThePastConfig.RebalancedMode)
		{
			list.Add(((CustomEventModel)this).Option((Func<Task>)Attack, "INITIAL", Array.Empty<IHoverTip>()));
		}
		return list;
	}

	private async Task GivePotion(PotionModel potion)
	{
		await PotionCmd.Discard(potion);
		await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("GIVE_POTION"));
	}

	private async Task GiveGold()
	{
		await PlayerCmd.LoseGold((decimal)_goldAmount, ((EventModel)this).Owner, (GoldLossType)1);
		await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("GIVE_GOLD"));
	}

	private async Task GiveCard(CardModel card)
	{
		await CardPileCmd.RemoveFromDeck((IReadOnlyList<CardModel>)new List<CardModel> { card }, true);
		await RelicCmd.Obtain(RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner).ToMutable(), ((EventModel)this).Owner, -1);
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("GIVE_CARD"));
	}

	private Task Attack()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("ATTACK"));
		return Task.CompletedTask;
	}

	public WeMeetAgain()
		: base(true)
	{
	}
}
