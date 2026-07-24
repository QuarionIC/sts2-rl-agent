using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class CustomCalculatedVar : CalculatedVar
{
	private Func<RelicModel, Creature?, decimal>? _relicCalc;

	private Func<PowerModel, Creature?, decimal>? _powerCalc;

	private Func<DynamicVarSource, Creature?, decimal>? _generalCalc;

	public CustomCalculatedVar(string name)
		: base(name)
	{
	}

	public virtual decimal CalculateCustom(Creature? target)
	{
		return CalculateCustomVar((DynamicVar)(object)this, ((CalculatedVar)this).GetBaseVar(), ((CalculatedVar)this).GetExtraVar(), target, (Func<Creature?, decimal>)base.Calculate, _relicCalc, _powerCalc, _generalCalc);
	}

	public static decimal CalculateCustomVar(DynamicVar dynVar, DynamicVar baseVar, DynamicVar extraVar, Creature? target, Func<Creature?, decimal> cardCalc, Func<RelicModel, Creature?, decimal>? relicCalc, Func<PowerModel, Creature?, decimal>? powerCalc, Func<DynamicVarSource, Creature?, decimal>? generalCalc)
	{
		AbstractModel owner = dynVar._owner;
		if (!(owner is CardModel))
		{
			PowerModel val = (PowerModel)(object)((owner is PowerModel) ? owner : null);
			decimal num;
			if (val == null)
			{
				RelicModel val2 = (RelicModel)(object)((owner is RelicModel) ? owner : null);
				if (val2 == null)
				{
					PotionModel val3 = (PotionModel)(object)((owner is PotionModel) ? owner : null);
					if (val3 == null)
					{
						EnchantmentModel val4 = (EnchantmentModel)(object)((owner is EnchantmentModel) ? owner : null);
						if (val4 == null)
						{
							if (owner is CardModifier cardModifier)
							{
								decimal obj;
								if (CombatManager.Instance.IsInProgress && cardModifier.Owner.Owner.Creature.CombatState != null)
								{
									if (generalCalc == null)
									{
										throw new InvalidOperationException($"{((object)dynVar).GetType().Name} {dynVar.Name} does not have multiplier calc defined for card modifiers in {owner.Id}");
									}
									obj = generalCalc(cardModifier, target);
								}
								else
								{
									obj = 0m;
								}
								num = obj;
								return baseVar.BaseValue + extraVar.BaseValue * num;
							}
							return dynVar.BaseValue;
						}
						decimal obj2;
						if (CombatManager.Instance.IsInProgress && val4.Card.Owner.Creature.CombatState != null)
						{
							if (generalCalc == null)
							{
								throw new InvalidOperationException($"{((object)dynVar).GetType().Name} {dynVar.Name} does not have multiplier calc defined for enchantments in {owner.Id}");
							}
							obj2 = generalCalc(val4, target);
						}
						else
						{
							obj2 = 0m;
						}
						num = obj2;
						return baseVar.BaseValue + extraVar.BaseValue * num;
					}
					if (generalCalc == null)
					{
						throw new InvalidOperationException($"{((object)dynVar).GetType().Name} {dynVar.Name} does not have multiplier calc defined for potions in {owner.Id}");
					}
					num = generalCalc(val3, target);
					return baseVar.BaseValue + extraVar.BaseValue * num;
				}
				if (relicCalc == null)
				{
					throw new InvalidOperationException($"{((object)dynVar).GetType().Name} {dynVar.Name} does not have multiplier calc defined for relics in {owner.Id}");
				}
				num = relicCalc(val2, target);
				return baseVar.BaseValue + extraVar.BaseValue * num;
			}
			if (powerCalc == null)
			{
				throw new InvalidOperationException($"{((object)dynVar).GetType().Name} {dynVar.Name} does not have multiplier calc defined for powers in {owner.Id}");
			}
			num = powerCalc(val, target);
			return baseVar.BaseValue + extraVar.BaseValue * num;
		}
		return cardCalc(target);
	}

	public CalculatedVar WithMultiplier(Func<RelicModel, Creature?, decimal> multiplierCalc)
	{
		if (_relicCalc != null)
		{
			throw new InvalidOperationException("Tried to set multiplier calc for relic on CustomCalculatedVar " + ((DynamicVar)this).Name + " twice!");
		}
		if (multiplierCalc.Target is AbstractModel)
		{
			throw new InvalidOperationException("Multiplier calc must be static!");
		}
		_relicCalc = multiplierCalc;
		return (CalculatedVar)(object)this;
	}

	public CalculatedVar WithMultiplier(Func<PowerModel, Creature?, decimal> multiplierCalc)
	{
		if (_powerCalc != null)
		{
			throw new InvalidOperationException("Tried to set multiplier calc for power on CustomCalculatedVar " + ((DynamicVar)this).Name + " twice!");
		}
		if (multiplierCalc.Target is AbstractModel)
		{
			throw new InvalidOperationException("Multiplier calc must be static!");
		}
		_powerCalc = multiplierCalc;
		return (CalculatedVar)(object)this;
	}

	public CalculatedVar GeneralMultiplier(Func<DynamicVarSource, Creature?, decimal> multiplierCalc)
	{
		if (_generalCalc != null)
		{
			throw new InvalidOperationException("Tried to set multiplier calc for CustomCalculatedVar " + ((DynamicVar)this).Name + " twice!");
		}
		if (multiplierCalc.Target is AbstractModel)
		{
			throw new InvalidOperationException("Multiplier calc must be static!");
		}
		((CalculatedVar)this).WithMultiplier((Func<CardModel, Creature, decimal>)((CardModel card, Creature? c) => multiplierCalc(card, c)));
		_powerCalc = (PowerModel pow, Creature? target) => multiplierCalc(pow, target);
		_relicCalc = (RelicModel pow, Creature? target) => multiplierCalc(pow, target);
		_generalCalc = multiplierCalc;
		return (CalculatedVar)(object)this;
	}

	protected override DynamicVar GetBaseVar()
	{
		return ((DynamicVar)this)._owner.GetDynamicVar(((DynamicVar)this).Name + "Base");
	}

	protected override DynamicVar GetExtraVar()
	{
		return ((DynamicVar)this)._owner.GetDynamicVar(((DynamicVar)this).Name + "Extra");
	}

	protected override decimal GetBaseValueForIConvertible()
	{
		return CalculateCustom(null);
	}

	public override string ToString()
	{
		return CalculateCustom(null).ToString();
	}
}
