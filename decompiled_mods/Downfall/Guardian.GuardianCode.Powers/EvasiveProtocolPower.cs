using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class EvasiveProtocolPower : GuardianPowerModel, IAfterGuardianModeChange
{
	public async Task AfterGuardianModeChange(PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		if (player.Creature == ((PowerModel)this).Owner && newMode is GuardianDefensiveMode)
		{
			await GuardianCmd.Polish(ctx, ((PowerModel)this).Owner, ((PowerModel)this).Amount, null);
		}
	}

	public EvasiveProtocolPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
