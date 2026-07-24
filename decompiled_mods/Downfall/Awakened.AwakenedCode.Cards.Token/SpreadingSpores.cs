using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class SpreadingSpores : AwakenedCardModel
{
	public SpreadingSpores()
		: base(0, (CardType)3, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)2 });
		((ConstructedCardModel)this).WithPower<ThornsPower>(2, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<ThornsPower>(ctx, (CardModel)(object)this, false);
		CardPileAddResult val = await CardPileCmd.AddGeneratedCardToCombat(((CardModel)this).CreateClone(), (PileType)1, ((CardModel)this).Owner, (CardPilePosition)3);
		if (val.success)
		{
			CardCmd.PreviewCardPileAdd(val, 0.1f, (CardPreviewStyle)2);
		}
	}
}
