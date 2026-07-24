using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Extensions.Relics;

public static class StaticHoverTipPowerExtensions
{
	public static AbstractTooltipSource<RelicModel> WithVars(this StaticHoverTip staticTip, params DynamicVar[] vars)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return new AbstractTooltipSource<RelicModel>((RelicModel _) => HoverTipFactory.Static(staticTip, vars));
	}
}
