using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Abstract;

public abstract class ConstructedPotionModel(PotionRarity potionRarity, PotionUsage potionUsage, TargetType targetType) : CustomPotionModel()
{
	private readonly List<AbstractTooltipSource<PotionModel>> _hoverTips = new List<AbstractTooltipSource<PotionModel>>();

	private readonly List<Func<PotionModel, IEnumerable<IHoverTip>>> _multiHoverTips = new List<Func<PotionModel, IEnumerable<IHoverTip>>>();

	private readonly List<DynamicVar> _newDynamicVars = new List<DynamicVar>();

	public override PotionRarity Rarity => potionRarity;

	public override PotionUsage Usage => potionUsage;

	public override TargetType TargetType => targetType;

	protected sealed override IEnumerable<DynamicVar> CanonicalVars => _newDynamicVars;

	public sealed override IEnumerable<IHoverTip> ExtraHoverTips => _hoverTips.Select((AbstractTooltipSource<PotionModel> tip) => tip.Tip((PotionModel)(object)this)).Concat(_multiHoverTips.SelectMany((Func<PotionModel, IEnumerable<IHoverTip>> mt) => mt((PotionModel)(object)this)));

	protected ConstructedPotionModel WithVars(params DynamicVar[] vars)
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

	protected ConstructedPotionModel WithRepeat(int i)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		WithVars((DynamicVar)new RepeatVar(i));
		return this;
	}

	protected ConstructedPotionModel WithDamage(int i)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		WithVars((DynamicVar)new DamageVar((decimal)i, (ValueProp)4));
		return this;
	}

	protected ConstructedPotionModel WithCards(int i)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		WithVars((DynamicVar)new CardsVar(i));
		return this;
	}

	protected ConstructedPotionModel WithBlock(int i)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		WithTip((StaticHoverTip)5);
		WithVars((DynamicVar)new BlockVar((decimal)i, (ValueProp)4));
		return this;
	}

	protected ConstructedPotionModel WithEnergy(int i)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return WithVars((DynamicVar)new EnergyVar(i));
	}

	protected ConstructedPotionModel WithPower<T>(int i, bool showTip = true) where T : PowerModel
	{
		if (showTip)
		{
			this.WithTip<T>();
		}
		_newDynamicVars.Add((DynamicVar)(object)new PowerVar<T>((decimal)i));
		return this;
	}

	protected ConstructedPotionModel WithVar(string name, int baseVal)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		_newDynamicVars.Add(new DynamicVar(name, (decimal)baseVal));
		return this;
	}

	protected ConstructedPotionModel WithTip(AbstractTooltipSource<PotionModel> tipSource)
	{
		_hoverTips.Add(tipSource);
		return this;
	}

	protected ConstructedPotionModel WithTips(Func<PotionModel, IEnumerable<IHoverTip>> multiTipSource)
	{
		_multiHoverTips.Add(multiTipSource);
		return this;
	}

	protected ConstructedPotionModel WithEnergyTip()
	{
		_hoverTips.Add(new PotionTooltipSource((Func<PotionModel, IHoverTip>)HoverTipFactory.ForEnergy));
		return this;
	}

	public ConstructedPotionModel WithTip<T>() where T : AbstractModel
	{
		return WithTip(typeof(T));
	}
}
