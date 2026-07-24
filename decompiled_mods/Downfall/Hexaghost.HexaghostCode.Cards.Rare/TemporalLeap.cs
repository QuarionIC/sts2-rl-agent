using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class TemporalLeap : HexaghostCardModel
{
	public TemporalLeap()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)5,
			(CardKeyword)(int)HexaghostKeyword.Advance
		});
		((ConstructedCardModel)this).WithBlock(10, 2);
		((ConstructedCardModel)this).WithCards(1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
