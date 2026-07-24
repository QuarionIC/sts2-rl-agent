using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Ancient;

[Pool(typeof(SlimeBossCardPool))]
public class AncientDarv : SlimeBossCardModel
{
	public AncientDarv()
		: base(1, (CardType)2, (CardRarity)5, (TargetType)1)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)(int)BaseLibKeywords.Purge });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
