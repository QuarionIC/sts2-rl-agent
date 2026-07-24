using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

internal class CustomBadgesPatch
{
	public static void Patch(Harmony harmony)
	{
		MethodInfo methodInfo = AccessToolsExtensions.Method(typeof(BadgePool), "CreateAll", (Type[])null, (Type[])null);
		if (methodInfo.GetParameters().Length == 3)
		{
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(AccessToolsExtensions.Method(typeof(CustomBadgesPatch), "AddCustomBadgesNew", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
		}
		else
		{
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(AccessToolsExtensions.Method(typeof(CustomBadgesPatch), "AddCustomBadgesOld", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
		}
	}

	private static IReadOnlyCollection<Badge> AddCustomBadgesNew(IReadOnlyCollection<Badge> __result, SerializableRun run, bool won, ulong playerId)
	{
		List<Badge> list = __result.ToList();
		foreach (Type customBadgeType in CustomContentDictionary.CustomBadgeTypes)
		{
			CustomBadge customBadge = (CustomBadge)Activator.CreateInstance(customBadgeType);
			list.Add(customBadge.ToRealBadge(run, won, playerId));
		}
		return list;
	}

	private static IReadOnlyCollection<Badge> AddCustomBadgesOld(IReadOnlyCollection<Badge> __result, SerializableRun run, ulong playerId)
	{
		List<Badge> list = __result.ToList();
		foreach (Type customBadgeType in CustomContentDictionary.CustomBadgeTypes)
		{
			CustomBadge customBadge = (CustomBadge)Activator.CreateInstance(customBadgeType);
			list.Add(customBadge.ToRealBadge(run, won: true, playerId));
		}
		return list;
	}
}
