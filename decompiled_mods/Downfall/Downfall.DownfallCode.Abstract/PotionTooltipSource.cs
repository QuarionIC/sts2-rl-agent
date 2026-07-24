using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public class PotionTooltipSource : AbstractTooltipSource<PotionModel>
{
	public PotionTooltipSource(Func<PotionModel, IHoverTip> tip)
		: base(tip)
	{
	}
}
