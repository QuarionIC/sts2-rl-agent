using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Abstract;

public abstract class ConstructedRelicModel : HookedRelicModel
{
	private readonly List<AbstractTooltipSource<RelicModel>> _hoverTips;

	private readonly List<Func<RelicModel, IEnumerable<IHoverTip>>> _multiHoverTips;

	private readonly List<DynamicVar> _newDynamicVars;

	protected sealed override IEnumerable<DynamicVar> CanonicalVars => _newDynamicVars;

	protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => _hoverTips.Select((AbstractTooltipSource<RelicModel> tip) => tip.Tip((RelicModel)(object)this)).Concat(_multiHoverTips.SelectMany((Func<RelicModel, IEnumerable<IHoverTip>> mt) => mt((RelicModel)(object)this)));

	public override RelicRarity Rarity => _003Crarity_003EP;

	protected ConstructedRelicModel(RelicRarity rarity, bool autoAdd = true)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		_003Crarity_003EP = rarity;
		_hoverTips = new List<AbstractTooltipSource<RelicModel>>();
		_multiHoverTips = new List<Func<RelicModel, IEnumerable<IHoverTip>>>();
		_newDynamicVars = new List<DynamicVar>();
		base._002Ector(autoAdd);
	}

	protected ConstructedRelicModel WithVars(params DynamicVar[] vars)
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

	protected ConstructedRelicModel WithDamage(int i)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		WithVars((DynamicVar)new DamageVar((decimal)i, (ValueProp)4));
		return this;
	}

	protected ConstructedRelicModel WithCards(int i)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		WithVars((DynamicVar)new CardsVar(i));
		return this;
	}

	protected ConstructedRelicModel WithBlock(int i)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		WithTip((StaticHoverTip)5);
		WithVars((DynamicVar)new BlockVar((decimal)i, (ValueProp)4));
		return this;
	}

	protected ConstructedRelicModel WithEnergy(int i)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return WithVars((DynamicVar)new EnergyVar(i));
	}

	protected ConstructedRelicModel WithPower<T>(int i, bool showTooltip = true) where T : PowerModel
	{
		if (showTooltip)
		{
			WithTip<T>();
		}
		_newDynamicVars.Add((DynamicVar)(object)new PowerVar<T>((decimal)i));
		return this;
	}

	protected ConstructedRelicModel WithVar(string name, int baseVal)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		_newDynamicVars.Add(new DynamicVar(name, (decimal)baseVal));
		return this;
	}

	protected ConstructedRelicModel WithTip(AbstractTooltipSource<RelicModel> tipSource)
	{
		_hoverTips.Add(tipSource);
		return this;
	}

	protected ConstructedRelicModel WithTips(Func<RelicModel, IEnumerable<IHoverTip>> multiTipSource)
	{
		_multiHoverTips.Add(multiTipSource);
		return this;
	}

	protected ConstructedRelicModel WithTip<T>() where T : AbstractModel
	{
		return WithTip(typeof(T));
	}

	protected ConstructedRelicModel WithEnergyTip()
	{
		_hoverTips.Add(new RelicTooltipSource((Func<RelicModel, IHoverTip>)HoverTipFactory.ForEnergy));
		return this;
	}

	protected ConstructedRelicModel WithHeal(int baseVal)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		WithVars((DynamicVar)new HealVar((decimal)baseVal));
		return this;
	}

	protected ConstructedRelicModel WithGold(int baseVal)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		WithVars((DynamicVar)new GoldVar(baseVal));
		return this;
	}
}
