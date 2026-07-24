using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Powers;

public class AlchyricalElixirPower : AutomatonPowerModel, IModifyCompiledFunction
{
	public bool ModifyCompiledFunction(FunctionCard function, Player player)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return false;
		}
		((CardModel)function).SetToFreeThisCombat();
		return true;
	}

	public Task AfterModifyCompiledFunction(FunctionCard result, Player player)
	{
		return PowerCmd.Decrement((PowerModel)(object)this);
	}

	public AlchyricalElixirPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
