using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Powers;

public class SpikerProtocolPower : GuardianPowerModel, IAfterGuardianModeChange
{
	public async Task AfterGuardianModeChange(PlayerChoiceContext ctx, Player player, GuardianModeModel oldMode, GuardianModeModel newMode)
	{
		if (player.Creature == ((PowerModel)this).Owner && newMode is GuardianDefensiveMode)
		{
			await PowerCmd.Apply<ThornsPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public SpikerProtocolPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
