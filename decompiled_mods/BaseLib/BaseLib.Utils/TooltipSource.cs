using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public class TooltipSource
{
	private readonly Func<CardModel, IHoverTip> _makeTip;

	public TooltipSource(Func<CardModel, IHoverTip> tip)
	{
		_makeTip = tip;
	}

	public IHoverTip Tip(CardModel card)
	{
		return _makeTip(card);
	}

	public static implicit operator TooltipSource(Type t)
	{
		if (t.IsAssignableTo(typeof(PowerModel)))
		{
			return new TooltipSource((CardModel card) => HoverTipFactory.FromPower(ModelDb.GetById<PowerModel>(ModelDb.GetId(t)), (int?)null));
		}
		if (t.IsAssignableTo(typeof(CardModel)))
		{
			return new TooltipSource((CardModel card) => HoverTipFactory.FromCard(ModelDb.GetById<CardModel>(ModelDb.GetId(t)), false));
		}
		if (t.IsAssignableTo(typeof(PotionModel)))
		{
			return new TooltipSource((CardModel card) => HoverTipFactory.FromPotion(ModelDb.GetById<PotionModel>(ModelDb.GetId(t))));
		}
		if (t.IsAssignableTo(typeof(EnchantmentModel)))
		{
			return new TooltipSource((CardModel card) => (IHoverTip)(object)ModelDb.GetById<EnchantmentModel>(ModelDb.GetId(t)).HoverTip);
		}
		throw new Exception($"Unable to generate hovertip from type {t}");
	}

	public static implicit operator TooltipSource(CardKeyword keyword)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new TooltipSource((CardModel card) => HoverTipFactory.FromKeyword(keyword));
	}

	public static implicit operator TooltipSource(StaticHoverTip staticTip)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new TooltipSource((CardModel card) => HoverTipFactory.Static(staticTip, Array.Empty<DynamicVar>()));
	}
}
