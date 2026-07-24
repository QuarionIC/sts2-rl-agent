using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Common;

public class PiercingHidePower : CustomTemporaryPowerModelWrapper<PiercingHide, ThornsPower>
{
	protected override bool UntilEndOfOtherSideTurn => true;
}
