using Downfall.DownfallCode.Abstract;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace Hermit.HermitCode.Powers;

public class BigBruiserPower : HermitPowerModel
{
	public BigBruiserPower()
		: base((PowerType)1, (PowerStackType)2)
	{
		((ConstructedPowerModel)this).WithTip<BruisePower>();
	}
}
