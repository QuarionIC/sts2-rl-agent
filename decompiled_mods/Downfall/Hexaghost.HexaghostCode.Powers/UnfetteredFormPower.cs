using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class UnfetteredFormPower : HexaghostPowerModel, IModifyGhostflameRepeatAdditive
{
	public int ModifyGhostflameRepeatAdditive(Player owner, GhostflameRepeatType repeatType, GhostflameModel bolsteringGhostflame)
	{
		if (owner.Creature == ((PowerModel)this).Owner)
		{
			return ((PowerModel)this).Amount;
		}
		return 0;
	}

	public UnfetteredFormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
