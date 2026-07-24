using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class ShadowStrike : HexaghostCardModel
{
	public ShadowStrike()
		: base(0, (CardType)1, (CardRarity)7, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 2);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
