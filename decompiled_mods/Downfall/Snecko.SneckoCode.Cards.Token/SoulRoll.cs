using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;

namespace Snecko.SneckoCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class SoulRoll : SneckoCardModel
{
	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<SneckoCardPool>();

	public SoulRoll()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)5,
			(CardKeyword)1
		});
		((ConstructedCardModel)(object)this).WithMuddle(1m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await SneckoCmd.MuddleHandCards(ctx, (CardModel)(object)this);
	}
}
