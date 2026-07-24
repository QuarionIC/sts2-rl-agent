using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class ClarionCallPower : AwakenedPowerModel, IOnDrained
{
	public async Task OnDrained(PlayerChoiceContext ctx, Player player, int amount)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, player);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public ClarionCallPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
