using System;
using System.Collections.Generic;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithConjure(this ConstructedCardModel card, Func<CardModel, bool>? a = null)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected I4, but got Unknown
		if (a == null)
		{
			card.WithTip(TooltipSource.op_Implicit(AwakenedTip.Conjure));
		}
		else
		{
			card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => (!a(e)) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(AwakenedTip.Conjure, Array.Empty<DynamicVar>())))));
		}
		card.WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)AwakenedTag.Conjure });
		return card;
	}

	public static ConstructedCardModel WithDrained(this ConstructedCardModel card, int baseVal, int upgrade = 0)
	{
		card.WithPower<DrainedPower>(baseVal, upgrade, showTooltip: false);
		card.WithEnergy(baseVal, upgrade);
		return card;
	}
}
