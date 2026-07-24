using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace SlimeBoss.SlimeBossCode.Core;

public abstract class SlimeBossPowerModel : DownfallPowerModel<SlimeBoss>
{
	protected SlimeBossPowerModel(PowerType powerType = (PowerType)1, PowerStackType powerStackType = (PowerStackType)1)
		: base(powerType, powerStackType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)

}
