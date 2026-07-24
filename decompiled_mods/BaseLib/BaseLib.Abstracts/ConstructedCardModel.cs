using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts;

public abstract class ConstructedCardModel : CustomCardModel
{
	protected enum UpgradeType
	{
		None,
		Add,
		Remove
	}

	private readonly List<CardKeyword> _cardKeywords = new List<CardKeyword>();

	protected readonly List<(CardKeyword, UpgradeType)> UpgradeKeywords = new List<(CardKeyword, UpgradeType)>();

	private readonly List<DynamicVar> _constructedDynamicVars = new List<DynamicVar>();

	private readonly List<TooltipSource> _hoverTips = new List<TooltipSource>();

	private readonly List<Func<CardModel, IEnumerable<IHoverTip>>> _multiHoverTips = new List<Func<CardModel, IEnumerable<IHoverTip>>>();

	private readonly HashSet<CardTag> _constructedTags = new HashSet<CardTag>();

	private bool _hasBasegameCalculatedVar;

	internal int? CostUpgrade;

	protected sealed override IEnumerable<DynamicVar> CanonicalVars => _constructedDynamicVars;

	public sealed override IEnumerable<CardKeyword> CanonicalKeywords => _cardKeywords;

	protected sealed override IEnumerable<IHoverTip> ExtraHoverTips => _hoverTips.Select((TooltipSource t) => t.Tip((CardModel)(object)this)).Concat(_multiHoverTips.SelectMany((Func<CardModel, IEnumerable<IHoverTip>> mt) => mt((CardModel)(object)this)));

	protected sealed override HashSet<CardTag> CanonicalTags => _constructedTags;

	protected ConstructedCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true)
		: base(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
	{
	}//IL_0044: Unknown result type (might be due to invalid IL or missing references)
	//IL_0045: Unknown result type (might be due to invalid IL or missing references)
	//IL_0046: Unknown result type (might be due to invalid IL or missing references)


	protected ConstructedCardModel WithVars(params DynamicVar[] vars)
	{
		foreach (DynamicVar val in vars)
		{
			_constructedDynamicVars.Add(val);
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

	protected ConstructedCardModel WithVar(string name, int baseVal, int upgrade = 0)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		_constructedDynamicVars.Add(DynamicVarExtensions.WithUpgrade<DynamicVar>(new DynamicVar(name, (decimal)baseVal), (decimal)upgrade));
		return this;
	}

	protected ConstructedCardModel WithVar(DynamicVar var)
	{
		return WithVars(var);
	}

	protected ConstructedCardModel WithBlock(int baseVal, int upgrade = 0)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<BlockVar>(new BlockVar((decimal)baseVal, (ValueProp)8), (decimal)upgrade));
		return this;
	}

	protected ConstructedCardModel WithDamage(int baseVal, int upgrade = 0)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<DamageVar>(new DamageVar((decimal)baseVal, (ValueProp)8), (decimal)upgrade));
		return this;
	}

	protected ConstructedCardModel WithCards(int baseVal, int upgrade = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		CardsVar item = DynamicVarExtensions.WithUpgrade<CardsVar>(new CardsVar(baseVal), (decimal)upgrade);
		_constructedDynamicVars.Add((DynamicVar)(object)item);
		return this;
	}

	protected ConstructedCardModel WithEnergy(int baseVal, int upgrade = 0)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		EnergyVar item = DynamicVarExtensions.WithUpgrade<EnergyVar>(new EnergyVar(baseVal), (decimal)upgrade);
		_constructedDynamicVars.Add((DynamicVar)(object)item);
		WithEnergyTip();
		return this;
	}

	protected ConstructedCardModel WithHeal(int baseVal, int upgrade = 0)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		HealVar item = DynamicVarExtensions.WithUpgrade<HealVar>(new HealVar((decimal)baseVal), (decimal)upgrade);
		_constructedDynamicVars.Add((DynamicVar)(object)item);
		return this;
	}

	protected ConstructedCardModel WithPower<T>(int baseVal, int upgrade = 0) where T : PowerModel
	{
		_constructedDynamicVars.Add((DynamicVar)(object)new PowerVar<T>((decimal)baseVal).WithUpgrade<PowerVar<T>>((decimal)upgrade));
		_hoverTips.Add(new TooltipSource((CardModel _) => HoverTipFactory.FromPower<T>((int?)null)));
		return this;
	}

	protected ConstructedCardModel WithPower<T>(string name, int baseVal, int upgrade = 0) where T : PowerModel
	{
		_constructedDynamicVars.Add((DynamicVar)(object)new PowerVar<T>(name, (decimal)baseVal).WithUpgrade<PowerVar<T>>((decimal)upgrade));
		_hoverTips.Add(new TooltipSource((CardModel _) => HoverTipFactory.FromPower<T>((int?)null)));
		return this;
	}

	protected ConstructedCardModel WithTags(params CardTag[] tags)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		foreach (CardTag item in tags)
		{
			_constructedTags.Add(item);
		}
		return this;
	}

	protected ConstructedCardModel WithCalculatedVar(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, int upgrade = 0, int bonusUpgrade = 0)
	{
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedVar(name), baseVal, 1, bonus, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedVar(string name, int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, int upgrade = 0, int bonusUpgrade = 0)
	{
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedVar(name), baseVal, multVal, mult, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedBlock(int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		if (_hasBasegameCalculatedVar)
		{
			throw new Exception("CardModel only supports a single normal calculated var; use WithCalculatedBlock while providing a custom name to useCustomCalculatedBlockVar instead");
		}
		_hasBasegameCalculatedVar = true;
		SetupCalculatedVar((CalculatedVar)new CalculatedBlockVar(props), baseVal, 1, bonus, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedBlock(int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		if (_hasBasegameCalculatedVar)
		{
			throw new Exception("CardModel only supports a single normal calculated var; use WithCalculatedBlock while providing a custom name to useCustomCalculatedBlockVar instead");
		}
		_hasBasegameCalculatedVar = true;
		SetupCalculatedVar((CalculatedVar)new CalculatedBlockVar(props), baseVal, multVal, mult, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedBlock(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedBlockVar(name, props), baseVal, 1, bonus, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedBlock(string name, int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedBlockVar(name, props), baseVal, multVal, mult, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedDamage(int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		if (_hasBasegameCalculatedVar)
		{
			throw new Exception("CardModel only supports a single normal calculated var; use WithCalculatedDamage while providing a custom name to useCustomCalculatedBlockVar instead");
		}
		_hasBasegameCalculatedVar = true;
		SetupCalculatedVar((CalculatedVar)new CalculatedDamageVar(props), baseVal, 1, bonus, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedDamage(int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		if (_hasBasegameCalculatedVar)
		{
			throw new Exception("CardModel only supports a single normal calculated var; use WithCalculatedDamage while providing a custom name to useCustomCalculatedDamageVar instead");
		}
		_hasBasegameCalculatedVar = true;
		SetupCalculatedVar((CalculatedVar)new CalculatedDamageVar(props), baseVal, multVal, mult, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedDamage(string name, int baseVal, Func<CardModel, Creature?, decimal> bonus, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedDamageVar(name, props), baseVal, 1, bonus, upgrade, bonusUpgrade);
		return this;
	}

	protected ConstructedCardModel WithCalculatedDamage(string name, int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, ValueProp props = (ValueProp)8, int upgrade = 0, int bonusUpgrade = 0)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (_hasBasegameCalculatedVar)
		{
			throw new Exception("CardModel only supports a single normal calculated var; use WithCalculatedDamage while providing a custom name to useCustomCalculatedDamageVar instead");
		}
		_hasBasegameCalculatedVar = true;
		SetupCalculatedVar((CalculatedVar)(object)new CustomCalculatedDamageVar(name, props), baseVal, multVal, mult, upgrade, bonusUpgrade);
		return this;
	}

	private void SetupCalculatedVar(CalculatedVar var, int baseVal, int multVal, Func<CardModel, Creature?, decimal> mult, int upgrade, int bonusUpgrade)
	{
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Expected O, but got Unknown
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Expected O, but got Unknown
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected O, but got Unknown
		if (!(var is CustomCalculatedVar) && !(var is CustomCalculatedBlockVar))
		{
			if (!(var is CustomCalculatedDamageVar))
			{
				if (var is CalculatedDamageVar)
				{
					_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<CalculationBaseVar>(new CalculationBaseVar((decimal)baseVal), (decimal)upgrade));
					_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<ExtraDamageVar>(new ExtraDamageVar((decimal)multVal), (decimal)bonusUpgrade));
				}
				else
				{
					_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<CalculationBaseVar>(new CalculationBaseVar((decimal)baseVal), (decimal)upgrade));
					_constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<CalculationExtraVar>(new CalculationExtraVar((decimal)multVal), (decimal)bonusUpgrade));
				}
			}
			else
			{
				_constructedDynamicVars.Add(DynamicVarExtensions.WithUpgrade<DynamicVar>(new DynamicVar(((DynamicVar)var).Name + "Base", (decimal)baseVal), (decimal)upgrade));
				_constructedDynamicVars.Add((DynamicVar)(object)new CustomExtraDamageVar(((DynamicVar)var).Name, multVal).WithUpgrade(bonusUpgrade));
			}
		}
		else
		{
			_constructedDynamicVars.Add(DynamicVarExtensions.WithUpgrade<DynamicVar>(new DynamicVar(((DynamicVar)var).Name + "Base", (decimal)baseVal), (decimal)upgrade));
			_constructedDynamicVars.Add(DynamicVarExtensions.WithUpgrade<DynamicVar>(new DynamicVar(((DynamicVar)var).Name + "Extra", (decimal)multVal), (decimal)bonusUpgrade));
		}
		_constructedDynamicVars.Add((DynamicVar)(object)var.WithMultiplier(mult));
	}

	protected ConstructedCardModel WithKeywords(params CardKeyword[] keywords)
	{
		_cardKeywords.AddRange(keywords);
		return this;
	}

	protected ConstructedCardModel WithKeyword(CardKeyword keyword, UpgradeType upgradeType = UpgradeType.None)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (upgradeType != UpgradeType.Add)
		{
			_cardKeywords.Add(keyword);
		}
		if (upgradeType != UpgradeType.None)
		{
			UpgradeKeywords.Add((keyword, upgradeType));
		}
		return this;
	}

	protected ConstructedCardModel WithCostUpgradeBy(int amount)
	{
		CostUpgrade = amount;
		return this;
	}

	protected ConstructedCardModel WithTip(TooltipSource tipSource)
	{
		_hoverTips.Add(tipSource);
		return this;
	}

	protected ConstructedCardModel WithTips(Func<CardModel, IEnumerable<IHoverTip>> multiTipSource)
	{
		_multiHoverTips.Add(multiTipSource);
		return this;
	}

	protected ConstructedCardModel WithEnergyTip()
	{
		_hoverTips.Add(new TooltipSource((Func<CardModel, IHoverTip>)HoverTipFactory.ForEnergy));
		return this;
	}

	protected ConstructedCardModel WithUpgradingCardTip<T>(Action<T, CardModel>? modifyTipCard = null) where T : CardModel
	{
		return WithTip(new TooltipSource(delegate(CardModel card)
		{
			CardModel val = ((CardModel)ModelDb.Card<T>()).ToMutable();
			if (card.IsUpgraded)
			{
				val.UpgradeInternal();
			}
			T val2 = (T)(object)((val is T) ? val : null);
			if (val2 != null)
			{
				modifyTipCard?.Invoke(val2, card);
			}
			return HoverTipFactory.FromCard(val, false);
		}));
	}

	public void ConstructedUpgrade()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		foreach (var upgradeKeyword in UpgradeKeywords)
		{
			switch (upgradeKeyword.Item2)
			{
			case UpgradeType.Add:
				((CardModel)this).AddKeyword(upgradeKeyword.Item1);
				break;
			case UpgradeType.Remove:
				((CardModel)this).RemoveKeyword(upgradeKeyword.Item1);
				break;
			}
		}
		if (CostUpgrade.HasValue)
		{
			((CardModel)this).EnergyCost.UpgradeBy(CostUpgrade.Value);
		}
	}
}
