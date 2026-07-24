using System.Threading.Tasks;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Daggerstorm : AwakenedCardModel
{
	public Daggerstorm()
		: base(2, (CardType)3, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<DaggerstormPower>(4, 2, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DaggerstormPower>(ctx, (CardModel)(object)this, false);
	}
}
