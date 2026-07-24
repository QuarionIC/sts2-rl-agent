using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Powers;

public class EnergizedPower : DownfallPowerModel
{
	public override string CustomPackedIconPath => EnergyIconHelper.GetPath((AbstractModel)(object)this);

	public override string CustomBigIconPath => EnergyIconHelper.GetPath((AbstractModel)(object)this);

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		if (((PowerModel)this).Owner != player.Creature)
		{
			return amount;
		}
		return amount + (decimal)((PowerModel)this).Amount;
	}

	public EnergizedPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
