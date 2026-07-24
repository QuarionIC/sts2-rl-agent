using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class TimeSifterPower : GuardianPowerModel
{
	public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await GuardianCmd.Accelerate(ctx, player, ((PowerModel)this).Amount);
			((PowerModel)this).Flash();
		}
	}

	public TimeSifterPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
