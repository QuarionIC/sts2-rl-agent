using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class DrainedPower : HermitPowerModel
{
	public DrainedPower()
		: base((PowerType)2, (PowerStackType)1)
	{
		WithEnergyTip();
	}

	protected override async Task AfterEnergyReset(PlayerChoiceContext ctx, Player player)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			await PlayerCmd.LoseEnergy((decimal)((PowerModel)this).Amount, player);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
