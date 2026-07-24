using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Powers;

public class RadiantFlamePower : HexaghostPowerModel, IAfterGhostflameIgnited
{
	public Task AfterGhostflameIgnited(PlayerChoiceContext ctx, Player player, GhostflameModel flame, int index)
	{
		if (player.Creature == ((PowerModel)this).Owner)
		{
			return CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)12, (CardPlay)null, false);
		}
		return Task.CompletedTask;
	}

	public RadiantFlamePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
