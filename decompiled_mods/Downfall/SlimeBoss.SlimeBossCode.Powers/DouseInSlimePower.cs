using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;

namespace SlimeBoss.SlimeBossCode.Powers;

public class DouseInSlimePower : SlimeBossPowerModel, IModifyGoopConsume
{
	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	public int ModifyGoopConsume(int amount, Creature creature, Creature? applier)
	{
		if (((PowerModel)this).Applier != applier || ((PowerModel)this).Owner != creature)
		{
			return amount;
		}
		return 0;
	}

	public Task AfterModifyingGoopConsume(Creature creature, Creature? applier)
	{
		return PowerCmd.Decrement((PowerModel)(object)this);
	}

	public DouseInSlimePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
