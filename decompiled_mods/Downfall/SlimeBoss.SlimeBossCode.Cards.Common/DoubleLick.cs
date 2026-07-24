using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class DoubleLick : SlimeBossCardModel
{
	public DoubleLick()
		: base(0, (CardType)2, (CardRarity)2, (TargetType)2)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithPower<GoopPower>(4, 0);
		((ConstructedCardModel)(object)this).WithRepeat(2);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithCards(0, 1);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Lick });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		decimal repeat = ((DynamicVar)((CardModel)this).DynamicVars.Repeat).BaseValue;
		for (int i = 0; (decimal)i < repeat; i++)
		{
			await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
		}
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
