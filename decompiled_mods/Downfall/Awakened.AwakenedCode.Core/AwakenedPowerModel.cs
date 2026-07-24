using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Awakened.AwakenedCode.Core;

public abstract class AwakenedPowerModel : DownfallPowerModel<Awakened>
{
	protected AwakenedPowerModel(PowerType powerType = (PowerType)1, PowerStackType powerStackType = (PowerStackType)1)
		: base(powerType, powerStackType)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)
	//IL_0002: Unknown result type (might be due to invalid IL or missing references)

}
