using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Cards.Variables;

public class CustomExtraDamageVar : DynamicVar
{
	public CustomExtraDamageVar(string baseName, decimal damage)
		: base(baseName + "Extra", damage)
	{
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		decimal baseValue = ((DynamicVar)this).BaseValue;
		EnchantmentModel enchantment = card.Enchantment;
		if (enchantment != null)
		{
			baseValue *= enchantment.EnchantDamageMultiplicative(baseValue, (ValueProp)8);
			if (!card.IsEnchantmentPreview)
			{
				((DynamicVar)this).EnchantedValue = baseValue;
			}
		}
		((DynamicVar)this).PreviewValue = baseValue;
	}
}
