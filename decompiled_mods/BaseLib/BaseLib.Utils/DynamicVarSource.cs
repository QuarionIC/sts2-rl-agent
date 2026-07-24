using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public sealed class DynamicVarSource
{
	public required DynamicVarSet DynamicVars { get; init; }

	public required Creature? Owner { get; init; }

	public CardModel? Card { get; init; }

	public RelicModel? Relic { get; init; }

	public PowerModel? Power { get; init; }

	public static implicit operator DynamicVarSource(CardModel card)
	{
		return new DynamicVarSource
		{
			DynamicVars = card.DynamicVars,
			Owner = ((card != null && ((AbstractModel)card).IsMutable && card.Owner != null) ? card.Owner.Creature : null),
			Card = card
		};
	}

	public static implicit operator DynamicVarSource(RelicModel relic)
	{
		return new DynamicVarSource
		{
			DynamicVars = relic.DynamicVars,
			Owner = ((relic != null && ((AbstractModel)relic).IsMutable && relic.Owner != null) ? relic.Owner.Creature : null),
			Relic = relic
		};
	}

	public static implicit operator DynamicVarSource(PowerModel power)
	{
		return new DynamicVarSource
		{
			DynamicVars = power.DynamicVars,
			Owner = ((power != null && ((AbstractModel)power).IsMutable && power.Owner != null) ? power.Owner : null),
			Power = power
		};
	}

	public static implicit operator DynamicVarSource(PotionModel potion)
	{
		return new DynamicVarSource
		{
			DynamicVars = potion.DynamicVars,
			Owner = ((potion != null && ((AbstractModel)potion).IsMutable && potion.Owner != null) ? potion.Owner.Creature : null)
		};
	}

	public static implicit operator DynamicVarSource(EnchantmentModel enchant)
	{
		DynamicVarSource obj = new DynamicVarSource
		{
			DynamicVars = enchant.DynamicVars
		};
		object owner;
		if (enchant != null && ((AbstractModel)enchant).IsMutable)
		{
			CardModel card = enchant.Card;
			if (card != null && ((AbstractModel)card).IsMutable && card.Owner != null)
			{
				owner = enchant.Card.Owner.Creature;
				goto IL_004a;
			}
		}
		owner = null;
		goto IL_004a;
		IL_004a:
		obj.Owner = (Creature?)owner;
		obj.Card = enchant.Card;
		return obj;
	}

	public static implicit operator DynamicVarSource(CardModifier modifier)
	{
		DynamicVarSource obj = new DynamicVarSource
		{
			DynamicVars = modifier.DynamicVars
		};
		CardModel? owner = modifier.Owner;
		object owner2;
		if (owner == null || !((AbstractModel)owner).IsMutable)
		{
			owner2 = null;
		}
		else
		{
			CardModel? owner3 = modifier.Owner;
			owner2 = ((owner3 != null) ? owner3.Owner.Creature : null);
		}
		obj.Owner = (Creature?)owner2;
		obj.Card = modifier.Owner;
		return obj;
	}
}
