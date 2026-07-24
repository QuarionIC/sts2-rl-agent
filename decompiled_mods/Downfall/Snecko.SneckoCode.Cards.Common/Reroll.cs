using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class Reroll : SneckoCardModel
{
	public Reroll()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(6, 3);
		((ConstructedCardModel)(object)this).WithMuddle(1m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await SneckoCmd.MuddleHandCards(ctx, (CardModel)(object)this);
	}
}
