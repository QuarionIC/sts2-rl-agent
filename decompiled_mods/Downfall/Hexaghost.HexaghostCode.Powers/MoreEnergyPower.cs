using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class MoreEnergyPower : HexaghostPowerModel
{
	public override async Task AfterEnergyReset(Player player)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).Flash();
			await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, player);
		}
	}

	public MoreEnergyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
