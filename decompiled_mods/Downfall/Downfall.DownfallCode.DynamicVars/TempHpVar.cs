using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Downfall.DownfallCode.DynamicVars;

public class TempHpVar : DynamicVar
{
	public TempHpVar(decimal value)
		: base("TempHP", value)
	{
	}
}
