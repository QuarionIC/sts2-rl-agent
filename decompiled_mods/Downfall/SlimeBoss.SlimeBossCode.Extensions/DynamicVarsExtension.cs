using MegaCrit.Sts2.Core.Localization.DynamicVars;
using SlimeBoss.SlimeBossCode.DynamicVars;

namespace SlimeBoss.SlimeBossCode.Extensions;

public static class DynamicVarsExtension
{
	public static SlimeSecondaryVar Slime(this DynamicVarSet vard)
	{
		return (SlimeSecondaryVar)(object)vard._vars["Slime"];
	}
}
