using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Powers;

public class DefensiveStylePower : ChampPowerModel, IModifySkillBonus
{
	public DefensiveStylePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		((ConstructedPowerModel)this).WithTip<CounterPower>();
	}

	public int ModifySkillBonus<TPower>(ChampStanceModel stance, int amount) where TPower : PowerModel
	{
		if (typeof(TPower) != typeof(CounterPower) || stance.Owner.Creature != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return amount + ((PowerModel)this).Amount;
	}
}
