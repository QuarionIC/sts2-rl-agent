using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;

namespace SlimeBoss.SlimeBossCode.Slimes;

public class SpikySlimePower : CustomTemporaryPowerModelWrapper<SpikySlime, ThornsPower>
{
	protected override bool UntilEndOfOtherSideTurn => true;
}
