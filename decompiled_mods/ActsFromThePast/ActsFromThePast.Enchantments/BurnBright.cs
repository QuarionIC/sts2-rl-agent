using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Enchantments;

public sealed class BurnBright : CustomEnchantmentModel
{
	private const int MaxCombats = 5;

	private int _combatsSeen;

	public override bool HasExtraCardText => true;

	public override bool ShowAmount => false;

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DynamicVar("Combats", 5m));

	[SavedProperty]
	public int CombatsSeen
	{
		get
		{
			return _combatsSeen;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_combatsSeen = value;
			((EnchantmentModel)this).DynamicVars["Combats"].BaseValue = 5 - _combatsSeen;
		}
	}

	public override bool CanEnchantCardType(CardType cardType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		return (int)cardType == 1;
	}

	public override decimal EnchantDamageMultiplicative(decimal originalDamage, ValueProp props)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return (!ValuePropExtensions.IsPoweredAttack(props)) ? 1m : 2m;
	}

	public override async Task AfterCombatEnd(CombatRoom _)
	{
		CardPile pile = ((EnchantmentModel)this).Card.Pile;
		if (pile != null && (int)pile.Type == 6)
		{
			CombatsSeen++;
			if (CombatsSeen >= 5 && (int)((EnchantmentModel)this).Card.Pile.Type == 6)
			{
				await CardPileCmd.RemoveFromDeck(((EnchantmentModel)this).Card, true);
			}
		}
	}
}
