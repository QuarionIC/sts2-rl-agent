using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Abstract;

public abstract class ConstructedPowerModel(PowerType powerType = (PowerType)1, PowerStackType stackType = (PowerStackType)1) : HookedPowerModel()
{
	private readonly List<AbstractTooltipSource<PowerModel>> _hoverTips = new List<AbstractTooltipSource<PowerModel>>();

	private readonly List<Func<PowerModel, IEnumerable<IHoverTip>>> _multiHoverTips = new List<Func<PowerModel, IEnumerable<IHoverTip>>>();

	private readonly List<DynamicVar> _newDynamicVars = new List<DynamicVar>();

	public override PowerType Type => powerType;

	public override PowerStackType StackType => stackType;

	protected sealed override IEnumerable<DynamicVar> CanonicalVars => _newDynamicVars;

	protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => _hoverTips.Select((AbstractTooltipSource<PowerModel> tip) => tip.Tip((PowerModel)(object)this)).Concat(_multiHoverTips.SelectMany((Func<PowerModel, IEnumerable<IHoverTip>> e) => e((PowerModel)(object)this)));

	public virtual bool ShouldRemoveDueToZero => true;

	protected ConstructedPowerModel WithUpgradedCardTip<T>(Action<T, PowerModel>? modifyTipCard = null) where T : CardModel
	{
		return WithTip(new PowerTooltipSource(delegate(PowerModel power)
		{
			CardModel obj = ((CardModel)ModelDb.Card<T>()).ToMutable();
			obj.UpgradeInternal();
			T val = (T)(object)((obj is T) ? obj : null);
			if (val != null)
			{
				modifyTipCard?.Invoke(val, power);
			}
			return HoverTipFactory.FromCard(obj, false);
		}));
	}

	protected ConstructedPowerModel WithCardTip<T>(Action<T, PowerModel>? modifyTipCard = null) where T : CardModel
	{
		return WithTip(new PowerTooltipSource(delegate(PowerModel power)
		{
			CardModel obj = ((CardModel)ModelDb.Card<T>()).ToMutable();
			T val = (T)(object)((obj is T) ? obj : null);
			if (val != null)
			{
				modifyTipCard?.Invoke(val, power);
			}
			return HoverTipFactory.FromCard(obj, false);
		}));
	}

	protected ConstructedPowerModel WithVars(params DynamicVar[] vars)
	{
		foreach (DynamicVar val in vars)
		{
			_newDynamicVars.Add(val);
			Type type = ((object)val).GetType();
			if (!type.IsGenericType)
			{
				continue;
			}
			Type[] genericArguments = type.GetGenericArguments();
			foreach (Type type2 in genericArguments)
			{
				if (type2.IsAssignableTo(typeof(PowerModel)))
				{
					WithTip(type2);
				}
			}
		}
		return this;
	}

	protected ConstructedPowerModel WithPower<T>(decimal i) where T : PowerModel
	{
		return WithVars((DynamicVar)new PowerVar<T>(i));
	}

	protected ConstructedPowerModel WithVar(string name, decimal baseVal)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_newDynamicVars.Add(new DynamicVar(name, baseVal));
		return this;
	}

	protected ConstructedPowerModel WithBlock(decimal baseVal)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_newDynamicVars.Add((DynamicVar)new BlockVar(baseVal, (ValueProp)4));
		return this;
	}

	protected ConstructedPowerModel WithCards(int baseVal)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		_newDynamicVars.Add((DynamicVar)new CardsVar(baseVal));
		return this;
	}

	public ConstructedPowerModel WithEnergy(int baseVal)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		_newDynamicVars.Add((DynamicVar)new EnergyVar(baseVal));
		WithEnergyTip();
		return this;
	}

	protected ConstructedPowerModel WithDamage(decimal baseVal)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		_newDynamicVars.Add((DynamicVar)new DamageVar(baseVal, (ValueProp)4));
		return this;
	}

	protected ConstructedPowerModel WithTip(AbstractTooltipSource<PowerModel> tipSource)
	{
		_hoverTips.Add(tipSource);
		return this;
	}

	protected ConstructedPowerModel WithTips(Func<PowerModel, IEnumerable<IHoverTip>> multiTipSource)
	{
		_multiHoverTips.Add(multiTipSource);
		return this;
	}

	protected ConstructedPowerModel WithEnergyTip()
	{
		_hoverTips.Add(new PowerTooltipSource((Func<PowerModel, IHoverTip>)HoverTipFactory.ForEnergy));
		return this;
	}

	public ConstructedPowerModel WithTip<T>() where T : AbstractModel
	{
		return WithTip(typeof(T));
	}
}
