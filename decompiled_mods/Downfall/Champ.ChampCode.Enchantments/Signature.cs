using System.Linq;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Enchantments;

public class Signature : DownfallEnchantmentModel<Champ.ChampCode.Core.Champ>
{
	public override bool CanEnchant(CardModel card)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (((EnchantmentModel)this).CanEnchant(card))
		{
			return card.Tags.Contains(ChampTag.Finisher);
		}
		return false;
	}

	protected override void OnEnchant()
	{
		((EnchantmentModel)this).Card.EnergyCost.UpgradeBy(-((EnchantmentModel)this).Card.EnergyCost.GetWithModifiers((CostModifiers)0));
		((EnchantmentModel)this).Card.EnergyCost.FinalizeUpgrade();
	}
}
