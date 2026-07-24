using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class MaximumOverdrivePower : GuardianPowerModel, IAfterCardTick
{
	public async Task AfterCardTick(PlayerChoiceContext ctx, CardModel card, Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Apply<MaximumOverdrivePowerPower>(ctx, player.Creature, (decimal)((PowerModel)this).Amount, player.Creature, (CardModel)null, false);
		}
	}

	public MaximumOverdrivePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
