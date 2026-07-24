using Champ.ChampCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Enchantments;

public class Crowned : DownfallEnchantmentModel<Champ.ChampCode.Core.Champ>
{
	public override bool CanEnchant(CardModel card)
	{
		if (((EnchantmentModel)this).CanEnchant(card))
		{
			return !card.EnergyCost.CostsX;
		}
		return false;
	}

	protected override void OnEnchant()
	{
		((EnchantmentModel)this).Card.EnergyCost.UpgradeBy(-((EnchantmentModel)this).Card.EnergyCost.GetWithModifiers((CostModifiers)0));
		((EnchantmentModel)this).Card.EnergyCost.FinalizeUpgrade();
	}
}
