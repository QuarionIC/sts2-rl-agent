using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class MegaLick : SlimeBossCardModel
{
	public MegaLick()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)3)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Lick });
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
		((ConstructedCardModel)this).WithPower<GoopPower>(4, 0);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCards(0, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
