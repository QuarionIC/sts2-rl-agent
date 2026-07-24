using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

public class BurningQuestionChoice1 : BurningQuestionChoiceBase
{
	public BurningQuestionChoice1()
	{
		((ConstructedCardModel)this).WithPower<IntensityPower>(3, 1);
	}

	public static BurningQuestionChoice1 Create(Player owner)
	{
		return owner.Creature.CombatState.CreateCard<BurningQuestionChoice1>(owner);
	}

	public override Task OnSelect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<IntensityPower>(ctx, (CardModel)(object)this, false);
	}
}
