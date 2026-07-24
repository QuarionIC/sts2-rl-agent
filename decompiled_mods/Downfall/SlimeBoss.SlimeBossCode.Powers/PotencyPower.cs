using BaseLib.Patches.Localization;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Powers;

public class PotencyPower : SlimeBossPowerModel, IAddDumbVariablesToPowerDescription, IModifySecondarySlimeEffects, IModifyDamageAdditive
{
	private int Amount2 => (((PowerModel)this).Amount + 1) / 2;

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		description.Add("Amount2", (decimal)Amount2);
	}

	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		return (((dealer != null) ? dealer.Monster : null) is SlimeModel slimeModel && slimeModel.PetOwner == ((PowerModel)this).Owner) ? ((PowerModel)this).Amount : 0;
	}

	public int ModifySecondarySlimeEffects(int amount, SlimeModel slime)
	{
		if (slime.PetOwner != ((PowerModel)this).Owner)
		{
			return amount;
		}
		return amount + Amount2;
	}

	public PotencyPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
