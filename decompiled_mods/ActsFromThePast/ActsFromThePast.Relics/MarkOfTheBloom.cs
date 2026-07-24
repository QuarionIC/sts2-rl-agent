using BaseLib.Abstracts;
using BaseLib.Hooks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class MarkOfTheBloom : CustomRelicModel, IHealAmountModifier
{
	public override RelicRarity Rarity => (RelicRarity)6;

	public decimal ModifyHealMultiplicative(Creature creature, decimal amount)
	{
		if (creature.Player != ((RelicModel)this).Owner)
		{
			return 1m;
		}
		if (amount > 0m)
		{
			((RelicModel)this).Flash();
		}
		return 0m;
	}

	public MarkOfTheBloom()
		: base(true)
	{
	}
}
