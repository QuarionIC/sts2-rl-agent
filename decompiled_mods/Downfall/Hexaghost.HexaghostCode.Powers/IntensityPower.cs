using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class IntensityPower : HexaghostPowerModel, IModifyGhostflameEffectAdditive
{
	public int ModifyGhostflameEffectAdditive(Player owner, GhostflameModel bolsteringGhostflame)
	{
		if (owner.Creature != ((PowerModel)this).Owner)
		{
			return 0;
		}
		return ((PowerModel)this).Amount;
	}

	public IntensityPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
