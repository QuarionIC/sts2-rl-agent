using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Powers;

public class RepulsePower : AutomatonPowerModel
{
	public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator != null && creator.Creature == ((PowerModel)this).Owner && (int)card.Type == 4)
		{
			((PowerModel)this).Flash();
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)12, (CardPlay)null, false);
		}
	}

	public RepulsePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
