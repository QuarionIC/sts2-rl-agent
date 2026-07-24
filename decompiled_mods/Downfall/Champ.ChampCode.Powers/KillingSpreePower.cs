using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class KillingSpreePower : ChampPowerModel, IIgnoreChampChargeCap
{
	public KillingSpreePower()
		: base((PowerType)1, (PowerStackType)2)
	{
	}

	public bool IgnoreChargeCap(Player player)
	{
		return player.Creature == ((PowerModel)this).Owner;
	}
}
