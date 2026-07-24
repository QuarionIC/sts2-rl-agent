using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Basic;

[Pool(typeof(SneckoCardPool))]
public class DefendSnecko : SneckoCardModel
{
	public DefendSnecko()
		: base(1, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)2 });
		((ConstructedCardModel)this).WithBlock(5, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
