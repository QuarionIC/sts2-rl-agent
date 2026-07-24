using System;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Relics;

[Obsolete]
public sealed class Horseshoe : HermitRelicModel
{
	public Horseshoe()
		: base((RelicRarity)2, autoAdd: false)
	{
		WithTip<WeakPower>();
		WithTip<FrailPower>();
		WithTip<VulnerablePower>();
	}

	public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount, Creature? applier, out decimal modifiedAmount)
	{
		modifiedAmount = amount;
		if (target != ((RelicModel)this).Owner.Creature)
		{
			return false;
		}
		if (amount <= 0m)
		{
			return false;
		}
		if ((!(canonicalPower is WeakPower) && !(canonicalPower is FrailPower) && !(canonicalPower is VulnerablePower)) || 1 == 0)
		{
			return false;
		}
		modifiedAmount = Math.Max(0m, amount - 1m);
		return true;
	}
}
