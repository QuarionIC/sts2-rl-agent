using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public class PowerTooltipSource : AbstractTooltipSource<PowerModel>
{
	public PowerTooltipSource(Func<PowerModel, IHoverTip> tip)
		: base(tip)
	{
	}
}
