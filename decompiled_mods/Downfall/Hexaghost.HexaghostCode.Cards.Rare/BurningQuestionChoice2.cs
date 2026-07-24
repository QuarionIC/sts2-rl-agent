using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

public class BurningQuestionChoice2 : BurningQuestionChoiceBase
{
	public BurningQuestionChoice2()
	{
		((ConstructedCardModel)this).WithPower<MetallicizePower>(6, 2);
	}

	public static BurningQuestionChoice2 Create(Player owner)
	{
		return owner.Creature.CombatState.CreateCard<BurningQuestionChoice2>(owner);
	}

	public override Task OnSelect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<MetallicizePower>(ctx, (CardModel)(object)this, false);
	}
}
