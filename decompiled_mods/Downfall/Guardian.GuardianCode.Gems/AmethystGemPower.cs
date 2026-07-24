using System.Collections.Generic;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Abstract;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Gems;

public class AmethystGemPower : TemporaryDebuffPowerWrapper<AmethystGem, StrengthPower>
{
	public override LocString Title
	{
		get
		{
			if (!(((CustomTemporaryPowerModel)this).OriginModel is GemModel gemModel))
			{
				return ((CustomTemporaryPowerModelWrapper<AmethystGem, StrengthPower>)(object)this).Title;
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
				return ((CustomTemporaryPowerModelWrapper<AmethystGem, StrengthPower>)(object)this).ExtraHoverTips;
			}
			return gemModel.HoverTips;
		}
	}
}
