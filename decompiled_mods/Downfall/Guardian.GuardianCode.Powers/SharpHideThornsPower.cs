using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Powers;

public class SharpHideThornsPower : CustomTemporaryPowerModelWrapper<SharpHidePower, ThornsPower>
{
	protected override bool UntilEndOfOtherSideTurn => true;
}
