using System;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class RuggedPower : HermitPowerModel
{
	public override decimal ModifyHpLostBeforeOsty(Creature target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (target != ((PowerModel)this).Owner || ((PowerModel)this).Amount <= 0 || amount <= 0m || !ValuePropExtensions.IsPoweredAttack(props))
		{
			return amount;
		}
		return Math.Min(amount, 2m);
	}

	public override Task AfterModifyingHpLostBeforeOsty()
	{
		PowerCmd.Decrement((PowerModel)(object)this);
		return Task.CompletedTask;
	}

	public RuggedPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
