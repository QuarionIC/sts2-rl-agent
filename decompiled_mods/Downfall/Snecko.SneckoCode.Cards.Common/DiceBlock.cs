using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class DiceBlock : SneckoCardModel, IHasOverflowEffect
{
	public DiceBlock()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithBlock(5, 2);
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
