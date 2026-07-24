using System;
using System.Globalization;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Cards.Variables;

public class CustomCalculatedBlockVar : CalculatedBlockVar
{
	private static Action<DynamicVar, string>? _nameSetter = ReflectionUtils.GetSetterForProperty<DynamicVar, string>("Name");

	private Func<RelicModel, Creature?, decimal>? _relicCalc;

	private Func<PowerModel, Creature?, decimal>? _powerCalc;

	private Func<DynamicVarSource, Creature?, decimal>? _generalCalc;

	public CustomCalculatedBlockVar(string name, ValueProp props)
		: base(props)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		_nameSetter?.Invoke((DynamicVar)(object)this, name);
	}

	public virtual decimal CalculateCustom(Creature? target)
	{
		return CustomCalculatedVar.CalculateCustomVar((DynamicVar)(object)this, ((CalculatedVar)this).GetBaseVar(), ((CalculatedVar)this).GetExtraVar(), target, (Func<Creature?, decimal>)((CalculatedVar)this).Calculate, _relicCalc, _powerCalc, _generalCalc);
	}

	public CalculatedVar WithMultiplier(Func<RelicModel, Creature?, decimal> multiplierCalc)
	{
		if (_relicCalc != null)
		{
			throw new InvalidOperationException("Tried to set multiplier calc for relic on CustomCalculatedBlockVar " + ((DynamicVar)this).Name + " twice!");
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
			throw new InvalidOperationException("Tried to set multiplier calc for power on CustomCalculatedBlockVar " + ((DynamicVar)this).Name + " twice!");
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
			throw new InvalidOperationException("Tried to set multiplier calc for CustomCalculatedBlockVar " + ((DynamicVar)this).Name + " twice!");
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
		return CalculateCustom(null).ToString(CultureInfo.InvariantCulture);
	}
}
