using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class Nope : SneckoCardModel
{
	public Nope()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
		CardModel card = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)((CardModel e) => (object)e != this), (AbstractModel)(object)this)).FirstOrDefault();
		if (card == null)
		{
			return;
		}
		await CardCmd.Exhaust(ctx, card, false, false);
		if (ModelDb.AllCharacterCardPools.Contains(card.Pool))
		{
			CardModel val2 = CardFactory.GetForCombat(((CardModel)this).Owner, card.Pool.AllCards, 1, ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
			if (val2 != null)
			{
				await CardPileCmd.AddGeneratedCardToCombat(val2, (PileType)2, ((CardModel)this).Owner, (CardPilePosition)1);
			}
		}
	}
}
