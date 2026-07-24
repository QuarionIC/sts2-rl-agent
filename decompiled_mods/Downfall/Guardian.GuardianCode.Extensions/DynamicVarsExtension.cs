using Guardian.GuardianCode.DynamicVars;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Guardian.GuardianCode.Extensions;

public static class DynamicVarsExtension
{
	public static BraceVar Brace(this DynamicVarSet vard)
	{
		return (BraceVar)(object)vard._vars["Brace"];
	}

	public static AccelerateVar Accelerate(this DynamicVarSet vard)
	{
		return (AccelerateVar)(object)vard._vars["Accelerate"];
	}

	public static PolishVar Polish(this DynamicVarSet vard)
	{
		return (PolishVar)(object)vard._vars["Polish"];
	}

	public static GemVar Gem(this DynamicVarSet vard)
	{
		return (GemVar)(object)vard._vars["Gem"];
	}
}
