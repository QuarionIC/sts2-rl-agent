using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class RainOfGoop : SlimeBossCardModel
{
	public RainOfGoop()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)4)
	{
		((ConstructedCardModel)this).WithPower<GoopPower>(4, 0);
		((ConstructedCardModel)(object)this).WithRepeat(3, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		decimal repeat = ((DynamicVar)((CardModel)this).DynamicVars.Repeat).BaseValue;
		for (int i = 0; (decimal)i < repeat; i++)
		{
			await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
		}
	}
}
