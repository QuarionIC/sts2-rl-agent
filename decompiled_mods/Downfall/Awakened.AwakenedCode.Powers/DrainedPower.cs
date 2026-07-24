using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class DrainedPower : AwakenedPowerModel
{
	public DrainedPower()
		: base((PowerType)2, (PowerStackType)1)
	{
		WithEnergyTip();
	}

	protected override async Task AfterEnergyReset(PlayerChoiceContext ctx, Player player)
	{
		if (player == ((PowerModel)this).Owner.Player && ((PowerModel)this).Owner.CombatState != null)
		{
			await PlayerCmd.LoseEnergy((decimal)((PowerModel)this).Amount, player);
			await AwakenedHook.OnDrained(((PowerModel)this).Owner.CombatState, ctx, ((PowerModel)this).Owner.Player, ((PowerModel)this).Amount);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
