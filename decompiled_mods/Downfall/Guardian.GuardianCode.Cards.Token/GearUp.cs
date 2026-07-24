using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Guardian.GuardianCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class GearUp : GuardianCardModel
{
	public GearUp()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
		((ConstructedCardModel)(object)this).WithBrace(10, 5);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
