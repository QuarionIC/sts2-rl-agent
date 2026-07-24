using System.Threading.Tasks;
using Automaton.AutomatonCode.Vfx;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace Automaton.AutomatonCode.Core;

public class AutomatonRunModel : CustomSingletonModel
{
	public AutomatonRunModel()
		: base((HookType)2)
	{
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		NCombatRoom instance = NCombatRoom.Instance;
		if (val == null || instance == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in val.Players)
		{
			if (player.Character is Automaton)
			{
				NSequenceDisplay.SetupFor(instance, player);
			}
		}
		return Task.CompletedTask;
	}
}
