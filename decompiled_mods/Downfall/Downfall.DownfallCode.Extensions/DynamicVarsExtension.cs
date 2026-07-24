using Downfall.DownfallCode.DynamicVars;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace Downfall.DownfallCode.Extensions;

public static class DynamicVarsExtension
{
	public static EnemyDamageVar EnemyDamage(this DynamicVarSet vard)
	{
		return (EnemyDamageVar)(object)vard._vars["EnemyDamage"];
	}

	public static SelfDamageVar SelfDamage(this DynamicVarSet vard)
	{
		return (SelfDamageVar)(object)vard._vars["SelfDamage"];
	}
}
