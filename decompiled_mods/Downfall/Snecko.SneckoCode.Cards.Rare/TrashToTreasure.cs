using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class TrashToTreasure : SneckoCardModel
{
	public TrashToTreasure()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(9, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.ExhaustSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)((CardModel e) => (object)e != this), (AbstractModel)(object)this)).FirstOrDefault();
		if (val2 != null)
		{
			int cost = val2.EnergyCost.GetResolved();
			await CardCmd.Exhaust(ctx, val2, false, false);
			await PlayerCmd.GainEnergy((decimal)cost, ((CardModel)this).Owner);
		}
	}
}
