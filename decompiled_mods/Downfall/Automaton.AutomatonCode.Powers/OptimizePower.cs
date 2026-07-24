using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class OptimizePower : AutomatonPowerModel, IModifyStashDraw
{
	public int ModifyStashDraw(int amount, Player player)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return amount + ((PowerModel)this).Amount;
	}

	public OptimizePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
