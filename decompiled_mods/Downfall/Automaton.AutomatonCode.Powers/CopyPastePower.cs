using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class CopyPastePower : AutomatonPowerModel
{
	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		if (card.Owner.Creature != ((PowerModel)this).Owner || card.EnergyCost.GetResolved() > 1)
		{
			return playCount;
		}
		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		((PowerModel)this).Flash();
		return PowerCmd.Decrement((PowerModel)(object)this);
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public CopyPastePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
