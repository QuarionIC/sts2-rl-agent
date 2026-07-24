using Champ.ChampCode.Core;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Enchantments;

public class Sturdy : DownfallEnchantmentModel<Champ.ChampCode.Core.Champ>
{
	public override bool CanEnchant(CardModel card)
	{
		if (((EnchantmentModel)this).CanEnchant(card))
		{
			return card.GainsBlock;
		}
		return false;
	}

	public override decimal EnchantBlockMultiplicative(decimal originalBlock)
	{
		return 2m;
	}
}
