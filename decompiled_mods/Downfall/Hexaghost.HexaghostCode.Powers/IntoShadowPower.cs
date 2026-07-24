using System.Threading.Tasks;
using BaseLib.Abstracts;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class IntoShadowPower : HexaghostPowerModel, IWheelMoved, IHasSecondAmount
{
	private class Data
	{
		public int FreeCards;

		public CardModel? Source;
	}

	private int FreeCards
	{
		get
		{
			return ((PowerModel)this).GetInternalData<Data>().FreeCards;
		}
		set
		{
			((PowerModel)this).GetInternalData<Data>().FreeCards = value;
			if (((PowerModel)this).Amount > 1)
			{
				((PowerModel)this).InvokeDisplayAmountChanged();
			}
		}
	}

	private CardModel? CardSource
	{
		get
		{
			return ((PowerModel)this).GetInternalData<Data>().Source;
		}
		set
		{
			((PowerModel)this).GetInternalData<Data>().Source = value;
		}
	}

	public string GetSecondAmount()
	{
		if (((PowerModel)this).Amount <= 1 || FreeCards <= 0)
		{
			return string.Empty;
		}
		return $"{FreeCards}";
	}

	public Task AfterWheelAdvance(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		return Task.CompletedTask;
	}

	public Task AfterWheelRetract(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		if (((PowerModel)this).Owner != player.Creature)
		{
			return Task.CompletedTask;
		}
		FreeCards += ((PowerModel)this).Amount;
		return Task.CompletedTask;
	}

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (ShouldSkip(card))
		{
			return false;
		}
		modifiedCost = default(decimal);
		return true;
	}

	public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (ShouldSkip(card))
		{
			return false;
		}
		modifiedCost = default(decimal);
		return true;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (ShouldSkip(cardPlay.Card) || !cardPlay.IsLastInSeries)
		{
			return Task.CompletedTask;
		}
		FreeCards--;
		CardSource = cardPlay.Card;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (CardSource == cardPlay.Card)
		{
			await CardCmd.Exhaust(ctx, cardPlay.Card, false, false);
		}
	}

	private bool ShouldSkip(CardModel card)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Invalid comparison between Unknown and I4
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Invalid comparison between Unknown and I4
		if (card.Owner.Creature != ((PowerModel)this).Owner)
		{
			return true;
		}
		CardPile pile = card.Pile;
		PileType? val = ((pile != null) ? new PileType?(pile.Type) : ((PileType?)null));
		if ((int)val.GetValueOrDefault() != 2 && (int)val.GetValueOrDefault() != 5)
		{
			return true;
		}
		return FreeCards == 0;
	}

	public IntoShadowPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
