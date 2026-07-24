using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Automaton.AutomatonCode.Powers;

public class SelfRepairPower : AutomatonPowerModel
{
	public override Task AfterCombatEnd(CombatRoom room)
	{
		return CreatureCmd.Heal(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, true);
	}

	public SelfRepairPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
