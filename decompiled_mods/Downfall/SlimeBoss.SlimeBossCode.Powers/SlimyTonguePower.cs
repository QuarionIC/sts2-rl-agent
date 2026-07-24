using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class SlimyTonguePower : SlimeBossPowerModel
{
	public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return (power is GoopPower && giver == ((PowerModel)this).Owner) ? ((PowerModel)this).Amount : 0;
	}

	public SlimyTonguePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
