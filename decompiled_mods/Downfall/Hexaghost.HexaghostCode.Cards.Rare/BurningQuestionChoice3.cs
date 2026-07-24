using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Rare;

public class BurningQuestionChoice3 : BurningQuestionChoiceBase
{
	public BurningQuestionChoice3()
	{
		((ConstructedCardModel)this).WithPower<RoyaltiesPower>(30, 5);
	}

	public static BurningQuestionChoice3 Create(Player owner)
	{
		return owner.Creature.CombatState.CreateCard<BurningQuestionChoice3>(owner);
	}

	public override Task OnSelect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<RoyaltiesPower>(ctx, (CardModel)(object)this, false);
	}
}
