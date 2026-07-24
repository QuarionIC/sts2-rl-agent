using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class DoomsdayPower : HexaghostPowerModel, IAfterGhostwheelAllIgnited
{
	public async Task AfterGhostwheelAllIgnited(PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.Apply<DoomsArrivalPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public DoomsdayPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
