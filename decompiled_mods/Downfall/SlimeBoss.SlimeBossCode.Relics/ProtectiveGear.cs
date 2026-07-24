using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Events;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class ProtectiveGear : SlimeBossRelicModel, IModifySelfDamage
{
	public ProtectiveGear()
		: base((RelicRarity)5)
	{
		WithVar("TackleReduce", 3);
	}

	public decimal ModifySelfDamage(decimal amount, AbstractModel model)
	{
		if (model.GetCreature() == ((RelicModel)this).Owner.Creature)
		{
			return Math.Max(0m, amount - ((RelicModel)this).DynamicVars["TackleReduce"].BaseValue);
		}
		return amount;
	}

	public Task AfterModifyingSelfDamage(AbstractModel model)
	{
		return Task.CompletedTask;
	}
}
