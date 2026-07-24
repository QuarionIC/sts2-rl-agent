using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class IronFang : SneckoCardModel
{
	public IronFang()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)3)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
