using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ActsFromThePast.Powers;

public sealed class StasisPower : CustomPowerModel
{
	private CardModel? _stolenCard;

	private Creature? _cardOwner;

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	private CardModel? StolenCard
	{
		get
		{
			return _stolenCard;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_stolenCard = value;
		}
	}

	private Creature? CardOwner
	{
		get
		{
			return _cardOwner;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_cardOwner = value;
		}
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)((StolenCard == null) ? ((Array)Array.Empty<IHoverTip>()) : ((Array)new IHoverTip[1] { HoverTipFactory.FromCard(StolenCard, false) }));

	public async Task Capture(CardModel card, Creature originalOwner)
	{
		StolenCard = card;
		CardOwner = originalOwner;
	}

	public override async Task BeforeDeath(Creature target)
	{
		if (((PowerModel)this).Owner != target || StolenCard == null || CardOwner == null)
		{
			return;
		}
		ICombatState combatState = CardOwner.CombatState;
		if (combatState != null)
		{
			typeof(CardModel).GetProperty("HasBeenRemovedFromState", BindingFlags.Instance | BindingFlags.Public)?.SetValue(StolenCard, false);
			List<CardModel> allCards = typeof(CombatState).GetField("_allCards", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(combatState) as List<CardModel>;
			if (allCards != null && !allCards.Contains(StolenCard))
			{
				allCards.Add(StolenCard);
			}
			await CardPileCmd.Add(StolenCard, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
