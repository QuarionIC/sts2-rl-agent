using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Powers;

public class ShiftingStrengthDownPower : TemporaryStrengthPower, ICustomPower, ICustomModel
{
	public override AbstractModel OriginModel => (AbstractModel)(object)ModelDb.Power<ShiftingPower>();

	protected override bool IsPositive => false;

	public override LocString Title => ((PowerModel)ModelDb.Power<ShiftingPower>()).Title;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[2]
	{
		HoverTipFactory.FromPower<ShiftingPower>((int?)null),
		HoverTipFactory.FromPower<StrengthPower>((int?)null)
	};
}
