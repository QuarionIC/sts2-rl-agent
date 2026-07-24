using System.Threading.Tasks;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Scheme : AwakenedCardModel
{
	public Scheme()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<SchemePower>(1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<SchemePower>(ctx, (CardModel)(object)this, false);
	}
}
