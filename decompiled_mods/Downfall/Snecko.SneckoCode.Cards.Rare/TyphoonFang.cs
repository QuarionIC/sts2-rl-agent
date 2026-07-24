using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class TyphoonFang : SneckoCardModel, IHasOverflowEffect
{
	public TyphoonFang()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(12, 4);
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithPower<TyphoonFangPower>(1, 0);
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (!cardPlay.IsAutoPlay)
		{
			(await CommonActions.ApplySelf<TyphoonFangPower>(ctx, (CardModel)(object)this, false))?.SetCard((CardModel)(object)this);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
