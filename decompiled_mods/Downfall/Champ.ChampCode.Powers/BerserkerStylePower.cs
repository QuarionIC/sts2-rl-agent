using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Powers;

public class BerserkerStylePower : ChampPowerModel, IModifySkillBonus
{
	public BerserkerStylePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<VigorPower>();
	}

	public int ModifySkillBonus<TPower>(ChampStanceModel stance, int amount) where TPower : PowerModel
	{
		if (typeof(TPower) != typeof(VigorPower) || stance.Owner.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return amount + ((PowerModel)this).Amount;
	}
}
