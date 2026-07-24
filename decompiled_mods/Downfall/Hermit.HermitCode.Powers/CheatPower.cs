using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class CheatPower : HermitPowerModel, IShouldTriggerDeadOn
{
	public CheatPower()
		: base((PowerType)1, (PowerStackType)2)
	{
	}

	public bool ShouldTriggerDeadOn(CardModel card)
	{
		return card.Owner.Creature == ((PowerModel)this).Owner;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		return PowerCmd.Remove((PowerModel)(object)this);
	}
}
