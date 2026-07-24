using System;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions.Cards;

public static class StaticHoverTipCardExtensions
{
	public static TooltipSource WithVars(this StaticHoverTip staticTip, params DynamicVar[] vars)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Expected O, but got Unknown
		return new TooltipSource((Func<CardModel, IHoverTip>)((CardModel _) => HoverTipFactory.Static(staticTip, vars)));
	}
}
