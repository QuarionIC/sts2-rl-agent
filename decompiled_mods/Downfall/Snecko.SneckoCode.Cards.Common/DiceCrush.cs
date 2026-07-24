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
public class DiceCrush : SneckoCardModel, IHasOverflowEffect
{
	public DiceCrush()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithDamage(18, 4);
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
