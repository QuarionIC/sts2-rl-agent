using System.Collections.Generic;
using BaseLib.Abstracts;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Gems;

public class RubyGemPower : CustomTemporaryPowerModelWrapper<RubyGem, StrengthPower>
{
	public override LocString Title
	{
		get
		{
			if (!(((CustomTemporaryPowerModel)this).OriginModel is GemModel gemModel))
			{
				return base.Title;
			}
			return gemModel.Title;
		}
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			if (!(((CustomTemporaryPowerModel)this).OriginModel is GemModel gemModel))
			{
				return base.ExtraHoverTips;
			}
			return gemModel.HoverTips;
		}
	}
}
