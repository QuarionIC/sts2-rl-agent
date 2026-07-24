using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithPower<T>(this ConstructedCardModel card, int baseVal, int upgrade, bool showTooltip) where T : PowerModel
	{
		card._constructedDynamicVars.Add((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<PowerVar<T>>(new PowerVar<T>((decimal)baseVal), (decimal)upgrade));
		if (showTooltip)
		{
			card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<T>((int?)DynamicVarSetExtensions.Power<T>(e.DynamicVars).IntValue))));
		}
		return card;
	}

	public static ConstructedCardModel WithPower<T>(this ConstructedCardModel card, int baseVal, bool showTooltip) where T : PowerModel
	{
		return card.WithPower<T>(baseVal, 0, showTooltip);
	}

	public static ConstructedCardModel WithGold(this ConstructedCardModel card, int baseVal, int upgradeVal = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		return card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<GoldVar>(new GoldVar(baseVal), (decimal)upgradeVal));
	}

	public static ConstructedCardModel WithRepeat(this ConstructedCardModel card, int baseVal, int upgradeVal = 0)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Expected O, but got Unknown
		return card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<RepeatVar>(new RepeatVar(baseVal), (decimal)upgradeVal));
	}

	public static ConstructedCardModel WithTempHp(this ConstructedCardModel card, int baseValue, int upgrade = 0)
	{
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<TempHpVar>(new TempHpVar(baseValue), (decimal)upgrade) });
	}

	public static ConstructedCardModel WithHpLoss(this ConstructedCardModel card, int baseVal, int upgrade = 0)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		return card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<HpLossVar>(new HpLossVar((decimal)baseVal), (decimal)upgrade));
	}

	public static ConstructedCardModel WithSelfDamage(this ConstructedCardModel card, int baseVal, int upgrade = 0)
	{
		return card.WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<SelfDamageVar>(new SelfDamageVar(baseVal, (ValueProp)12), (decimal)upgrade));
	}

	public static ConstructedCardModel WithEnemyDamage(this ConstructedCardModel card, int baseValue, int upgrade = 0)
	{
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<EnemyDamageVar>(new EnemyDamageVar(baseValue, (ValueProp)8), (decimal)upgrade) });
	}

	public static ConstructedCardModel WithUpgradedCardTip<T>(this ConstructedCardModel cons, Action<T, CardModel>? modifyTipCard = null) where T : CardModel
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		return cons.WithTip(new TooltipSource((Func<CardModel, IHoverTip>)delegate(CardModel card)
		{
			CardModel obj = ((CardModel)ModelDb.Card<T>()).ToMutable();
			obj.UpgradeInternal();
			T val = (T)(object)((obj is T) ? obj : null);
			if (val != null)
			{
				modifyTipCard?.Invoke(val, card);
			}
			return HoverTipFactory.FromCard(obj, false);
		}));
	}

	public static ConstructedCardModel WithCardTip<T>(this ConstructedCardModel cons, Action<T, CardModel>? modifyTipCard = null) where T : CardModel
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		return cons.WithTip(new TooltipSource((Func<CardModel, IHoverTip>)delegate(CardModel card)
		{
			CardModel obj = ((CardModel)ModelDb.Card<T>()).ToMutable();
			T val = (T)(object)((obj is T) ? obj : null);
			if (val != null)
			{
				modifyTipCard?.Invoke(val, card);
			}
			return HoverTipFactory.FromCard(obj, false);
		}));
	}

	public static ConstructedCardModel WithTip(this ConstructedCardModel card, TooltipSource tooltipSource, UpgradeType upgradeType)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected I4, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		return (ConstructedCardModel)((int)upgradeType switch
		{
			1 => card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel c) => (!c.IsUpgraded) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(tooltipSource.Tip(c))))), 
			2 => card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel c) => c.IsUpgraded ? ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(tooltipSource.Tip(c))) : ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()))), 
			0 => card.WithTip(tooltipSource), 
			_ => throw new ArgumentOutOfRangeException("upgradeType", upgradeType, null), 
		});
	}

	public static ConstructedCardModel WithTip(this ConstructedCardModel card, TooltipSource tooltipSource, int baseVal, int upgrade)
	{
		if (baseVal == 0)
		{
			if (upgrade != 0)
			{
				return card.WithTip(tooltipSource, (UpgradeType)1);
			}
			return card;
		}
		return card.WithTip(tooltipSource, (UpgradeType)((baseVal + upgrade == 0) ? 2 : 0));
	}

	public static ConstructedCardModel WithTip<T>(this ConstructedCardModel card) where T : AbstractModel
	{
		return card.WithTip(TooltipSource.op_Implicit(typeof(T)));
	}

	public static ConstructedCardModel WithArtist<T>(this ConstructedCardModel card) where T : Artist, new()
	{
		return card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel _) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(Artist.Get<T>().HoverTip)));
	}

	public static ConstructedCardModel WithScry(this ConstructedCardModel card, int baseValue, int upgrade = 0)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		return card.WithVars((DynamicVar[])(object)new DynamicVar[1] { (DynamicVar)DynamicVarExtensions.WithUpgrade<ScryVar>(new ScryVar((decimal)baseValue), (decimal)upgrade) });
	}
}
