using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class DoomsArrivalPower : HexaghostPowerModel
{
	public override bool ShouldTakeExtraTurn(Player player)
	{
		return player.Creature == ((PowerModel)this).Owner;
	}

	public override async Task AfterTakingExtraTurn(Player player)
	{
		((PowerModel)this).Flash();
		await PowerCmd.TickDownDuration((PowerModel)(object)this);
	}

	public DoomsArrivalPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
