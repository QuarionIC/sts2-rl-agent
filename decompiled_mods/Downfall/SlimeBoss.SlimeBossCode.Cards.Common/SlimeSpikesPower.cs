using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

public class SlimeSpikesPower : CustomTemporaryPowerModelWrapper<SlimeSpikes, ThornsPower>
{
	protected override bool UntilEndOfOtherSideTurn => true;
}
