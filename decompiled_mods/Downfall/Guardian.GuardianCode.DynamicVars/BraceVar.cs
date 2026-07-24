using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Guardian.GuardianCode.DynamicVars;

public class BraceVar : DynamicVar
{
	public BraceVar(int baseValue)
		: base("Brace", (decimal)baseValue)
	{
	}
}
