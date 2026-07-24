using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class OverexertPower : SlimeBossPowerModel
{
	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
			if (((PowerModel)this).Amount <= 0)
			{
				await SlimeBossCmd.AbsorbAll(ctx, player);
			}
		}
	}

	public OverexertPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
