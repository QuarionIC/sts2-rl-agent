using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class ExoticFormPower : SneckoPowerModel, IHasSecondAmount
{
	private readonly HashSet<CardPoolModel> _uniqueColorsThisTurn = new HashSet<CardPoolModel>();

	public string GetSecondAmount()
	{
		return _uniqueColorsThisTurn.Count.ToString();
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		if (_uniqueColorsThisTurn.Add(cardPlay.Card.Pool))
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
		return Task.CompletedTask;
	}

	public override async Task BeforeFlush(PlayerChoiceContext ctx, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			int count = _uniqueColorsThisTurn.Count;
			if (count > 0)
			{
				await PowerCmd.Apply<StrengthPower>(ctx, ((PowerModel)this).Owner, (decimal)(((PowerModel)this).Amount * count), ((PowerModel)this).Owner, (CardModel)null, false);
			}
			_uniqueColorsThisTurn.Clear();
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
	}

	public ExoticFormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
