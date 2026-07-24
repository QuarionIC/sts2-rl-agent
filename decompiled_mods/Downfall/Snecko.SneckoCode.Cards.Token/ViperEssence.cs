using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Snecko.SneckoCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class ViperEssence : SneckoCardModel
{
	public ViperEssence()
		: base(0, (CardType)3, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)0);
		((ConstructedCardModel)this).WithPower<StrengthPower>(1, 1);
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
	}
}
