using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Guardian.GuardianCode.DynamicVars;

public class AccelerateVar : DynamicVar
{
	public AccelerateVar(int baseValue)
		: base("Accelerate", (decimal)baseValue)
	{
	}
}
