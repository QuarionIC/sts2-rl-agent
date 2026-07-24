using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public class RelicTooltipSource : AbstractTooltipSource<RelicModel>
{
	public RelicTooltipSource(Func<RelicModel, IHoverTip> tip)
		: base(tip)
	{
	}
}
