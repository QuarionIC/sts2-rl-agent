using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class Shed : SneckoCardModel
{
	public Shed()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithBlock(5, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		IReadOnlyList<CardModel> cards = ((CardModel)this).Owner.GetHand();
		await SneckoCmd.Muddle(ctx, (IEnumerable<CardModel>)cards, (AbstractModel?)(object)this, lowerOnly: false);
		int nowNull = cards.Count((CardModel e) => e.EnergyCost.GetResolved() == 0);
		for (int i = 0; i < nowNull; i++)
		{
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		}
	}
}
