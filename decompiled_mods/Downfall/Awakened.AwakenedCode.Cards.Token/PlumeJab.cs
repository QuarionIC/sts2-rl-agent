using System.Threading.Tasks;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class PlumeJab : AwakenedCardModel
{
	public PlumeJab()
		: base(0, (CardType)1, (CardRarity)7, (TargetType)4)
	{
		((ConstructedCardModel)this).WithDamage(2, 1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
		((ConstructedCardModel)(object)this).WithRepeat(2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? player)
	{
		if ((object)card != this)
		{
			return Task.CompletedTask;
		}
		int powerAmount = ((CardModel)this).Owner.Creature.GetPowerAmount<RazorSharpPower>();
		if (powerAmount == 0)
		{
			return Task.CompletedTask;
		}
		((DynamicVar)((CardModel)this).DynamicVars.Repeat).UpgradeValueBy((decimal)powerAmount);
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power is RazorSharpPower && power.Owner == ((CardModel)this).Owner.Creature)
		{
			((DynamicVar)((CardModel)this).DynamicVars.Repeat).UpgradeValueBy(amount);
		}
		return Task.CompletedTask;
	}
}
