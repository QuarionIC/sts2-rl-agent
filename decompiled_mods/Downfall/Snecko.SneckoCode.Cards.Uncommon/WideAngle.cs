using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class WideAngle : SneckoCardModel
{
	public WideAngle()
		: base(3, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)0);
		((ConstructedCardModel)this).WithDamage(18, 6);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
