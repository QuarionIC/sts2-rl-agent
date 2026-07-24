using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace SlimeBoss.SlimeBossCode.Core;

public class SlimeBossModel : CustomSingletonModel
{
	public SlimeBossModel()
		: base((HookType)1)
	{
	}

	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		return SlimeBossCmd.CommandAll(ctx, player, (ValueProp)4);
	}

	public override Task BeforeCombatStart()
	{
		SlimeQueue.ResetAllSlots();
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player item in val.Players.Where((Player e) => e.Character is SlimeBoss))
		{
			SlimeQueue.SetSlots(item, 3);
		}
		return Task.CompletedTask;
	}
}
