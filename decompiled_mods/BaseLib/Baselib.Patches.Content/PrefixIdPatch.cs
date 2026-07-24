using System;
using System.Collections.Concurrent;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils.Attributes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(ModelDb), "GetEntry")]
public class PrefixIdPatch
{
	private static readonly ConcurrentDictionary<Type, string> IdCache = new ConcurrentDictionary<Type, string>();

	[HarmonyPostfix]
	private static void AdjustID(ref string __result, Type type)
	{
		if (IdCache.TryGetValue(type, out string value))
		{
			__result = value;
			return;
		}
		CustomIDAttribute customAttribute = type.GetCustomAttribute<CustomIDAttribute>();
		if (customAttribute != null)
		{
			IdCache[type] = customAttribute.ID;
			__result = customAttribute.ID;
		}
		else if (type.IsAssignableTo(typeof(ICustomModel)))
		{
			IdCache[type] = type.GetPrefix() + __result;
			__result = IdCache[type];
		}
		else
		{
			IdCache[type] = __result;
		}
	}
}
