using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
public class RelicImageOverridePatch
{
	private static Dictionary<Type, List<(RelicIconData, Func<RelicModel, bool>?)>> _relicImageOverrides = new Dictionary<Type, List<(RelicIconData, Func<RelicModel, bool>)>>();

	public static void AddOverride<TRelicType>(RelicIconData data, Func<RelicModel, bool>? condition = null) where TRelicType : RelicModel
	{
		if (!_relicImageOverrides.TryGetValue(typeof(TRelicType), out List<(RelicIconData, Func<RelicModel, bool>)> value))
		{
			value = new List<(RelicIconData, Func<RelicModel, bool>)>();
			_relicImageOverrides[typeof(TRelicType)] = value;
		}
		value.Add((data, condition));
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static bool PackedIconPath(RelicModel __instance, ref string? __result)
	{
		return TryGetCustomPath(__instance, (RelicIconData y) => y.PackedIconPath, ref __result);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static bool PackedIconOutlinePath(RelicModel __instance, ref string? __result)
	{
		return TryGetCustomPath(__instance, (RelicIconData y) => y.PackedIconOutlinePath, ref __result);
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPrefix]
	private static bool BigIconPath(RelicModel __instance, ref string? __result)
	{
		return TryGetCustomPath(__instance, (RelicIconData y) => y.BigIconPath, ref __result);
	}

	private static bool TryGetCustomPath(RelicModel relic, Func<RelicIconData, string?> selector, ref string? result)
	{
		if (!_relicImageOverrides.TryGetValue(((object)relic).GetType(), out List<(RelicIconData, Func<RelicModel, bool>)> value))
		{
			return true;
		}
		foreach (var item in value)
		{
			if (item.Item2 == null || item.Item2(relic))
			{
				result = selector(item.Item1);
				return result == null;
			}
		}
		return true;
	}
}
