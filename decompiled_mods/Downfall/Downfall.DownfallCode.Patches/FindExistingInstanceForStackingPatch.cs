using System;
using System.Linq;
using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(PowerCmd), "FindExistingInstanceForStacking")]
public static class FindExistingInstanceForStackingPatch
{
	public static bool Prefix(PowerModel basePower, Creature target, Creature? applier, ref PowerModel? __result)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		if (!CustomPowerInstanceType.PowerInstanceTypes.TryGetValue(basePower.InstanceType, out Func<PowerModel, Creature, Creature?, PowerModel, bool> isPowerSame))
		{
			return true;
		}
		__result = target.GetPowerInstances(((AbstractModel)basePower).Id).FirstOrDefault((Func<PowerModel, bool>)((PowerModel p) => isPowerSame(basePower, target, applier, p)));
		return false;
	}
}
