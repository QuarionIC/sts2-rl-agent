using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class RecyclingPower : SlimeBossPowerModel
{
	public override Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		return SlimeBossCmd.Slurp(player, ((PowerModel)this).Amount);
	}

	public RecyclingPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
