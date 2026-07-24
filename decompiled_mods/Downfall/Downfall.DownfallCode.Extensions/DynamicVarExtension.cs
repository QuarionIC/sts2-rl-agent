using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Downfall.DownfallCode.Extensions;

public static class DynamicVarExtension
{
	public static decimal Calculate(this DynamicVar var, Creature? target)
	{
		CalculatedVar val = (CalculatedVar)(object)((var is CalculatedVar) ? var : null);
		if (val == null)
		{
			return 0m;
		}
		return val.Calculate(target);
	}
}
