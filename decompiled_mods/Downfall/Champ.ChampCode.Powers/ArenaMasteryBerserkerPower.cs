using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class ArenaMasteryBerserkerPower : ChampPowerModel, IModifyBerserkerFinisherBonus
{
	public int ModifyBerserkerFinisherBonus(ChampStanceModel stanceModel, int baseAmount)
	{
		if (stanceModel.Owner.Creature != ((PowerModel)this).Owner)
		{
			return baseAmount;
		}
		return baseAmount + ((PowerModel)this).Amount;
	}

	public ArenaMasteryBerserkerPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
