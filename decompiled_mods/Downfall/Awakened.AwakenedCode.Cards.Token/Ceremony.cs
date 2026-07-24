using System.Threading.Tasks;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Ceremony : AwakenedCardModel
{
	public Ceremony()
		: base(0, (CardType)3, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(1, 1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if ((object)card != this)
		{
			return Task.CompletedTask;
		}
		int powerAmount = ((CardModel)this).Owner.Creature.GetPowerAmount<FerventWorshipPower>();
		if (powerAmount == 0)
		{
			return Task.CompletedTask;
		}
		((CardModel)this).EnergyCost.AddThisCombat(powerAmount, false);
		((CardModel)this).BaseReplayCount = ((CardModel)this).BaseReplayCount + powerAmount;
		return Task.CompletedTask;
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (!(power is FerventWorshipPower) || power.Owner != ((CardModel)this).Owner.Creature)
		{
			return Task.CompletedTask;
		}
		int num = (int)amount;
		((CardModel)this).EnergyCost.AddThisCombat(num, false);
		((CardModel)this).BaseReplayCount = ((CardModel)this).BaseReplayCount + num;
		return Task.CompletedTask;
	}
}
