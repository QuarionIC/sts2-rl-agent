using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public class AbstractTooltipSource<T>(Func<T, IHoverTip> tip) where T : AbstractModel
{
	public IHoverTip Tip(T model)
	{
		return _003Ctip_003EP(model);
	}

	public static implicit operator AbstractTooltipSource<T>(Type t)
	{
		if (t.IsAssignableTo(typeof(PowerModel)))
		{
			return new AbstractTooltipSource<T>((T _) => HoverTipFactory.FromPower(ModelDb.GetById<PowerModel>(ModelDb.GetId(t)), (int?)null));
		}
		if (t.IsAssignableTo(typeof(CardModel)))
		{
			return new AbstractTooltipSource<T>((T _) => HoverTipFactory.FromCard(ModelDb.GetById<CardModel>(ModelDb.GetId(t)), false));
		}
		if (t.IsAssignableTo(typeof(PotionModel)))
		{
			return new AbstractTooltipSource<T>((T _) => HoverTipFactory.FromPotion(ModelDb.GetById<PotionModel>(ModelDb.GetId(t))));
		}
		if (t.IsAssignableTo(typeof(EnchantmentModel)))
		{
			return new AbstractTooltipSource<T>((T _) => (IHoverTip)(object)ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(t)).HoverTip);
		}
		throw new Exception($"Unable to generate hovertip from type {t}");
	}

	public static implicit operator AbstractTooltipSource<T>(CardKeyword keyword)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new AbstractTooltipSource<T>((T _) => HoverTipFactory.FromKeyword(keyword));
	}

	public static implicit operator AbstractTooltipSource<T>(StaticHoverTip staticTip)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new AbstractTooltipSource<T>((T _) => HoverTipFactory.Static(staticTip, Array.Empty<DynamicVar>()));
	}
}
