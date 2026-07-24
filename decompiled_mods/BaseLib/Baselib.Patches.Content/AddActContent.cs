using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Content;

public static class AddActContent
{
	public static void Patch(Harmony harmony)
	{
		StringBuilder stringBuilder = new StringBuilder("Patching act types for custom encounters and events");
		foreach (Type item in ReflectionHelper.GetSubtypes<ActModel>().Chain(ReflectionHelper.GetSubtypesInMods<ActModel>()))
		{
			bool flag = false;
			MethodInfo methodInfo = AccessTools.DeclaredMethod(item, "GenerateAllEncounters", (Type[])null, (Type[])null);
			if (methodInfo != null)
			{
				flag = true;
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(AccessTools.Method(typeof(AddActContent), "AddCustomEncounters", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
			}
			methodInfo = AccessTools.DeclaredPropertyGetter(item, "AllEvents");
			if (methodInfo != null)
			{
				flag = true;
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, HarmonyMethod.op_Implicit(AccessTools.Method(typeof(AddActContent), "AddCustomEvents", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
			}
			if (flag)
			{
				stringBuilder.Append(" | ").Append(item.Name);
			}
		}
		BaseLibMain.Logger.Info(stringBuilder.ToString(), 1);
	}

	private static IEnumerable<EncounterModel> AddCustomEncounters(IEnumerable<EncounterModel> result, ActModel __instance)
	{
		List<EncounterModel> origResult = result.ToList();
		foreach (EncounterModel item in origResult)
		{
			yield return item;
		}
		foreach (CustomEncounterModel encounter in CustomContentDictionary.CustomEncounters)
		{
			if (!origResult.Any((EncounterModel existingEncounter) => ((AbstractModel)existingEncounter).Id.Equals(((AbstractModel)encounter).Id)) && encounter.IsValidForAct(__instance))
			{
				yield return (EncounterModel)(object)encounter;
			}
		}
	}

	private static IEnumerable<EventModel> AddCustomEvents(IEnumerable<EventModel> result, ActModel __instance)
	{
		List<EventModel> origResult = result.ToList();
		foreach (EventModel item in origResult)
		{
			yield return item;
		}
		foreach (CustomEventModel eventModel in CustomContentDictionary.ActCustomEvents)
		{
			if (!origResult.Any((EventModel existingEvent) => ((AbstractModel)existingEvent).Id.Equals(((AbstractModel)eventModel).Id)) && eventModel.Acts.Any((ActModel act) => ((AbstractModel)act).Id.Equals(((AbstractModel)__instance).Id)))
			{
				yield return (EventModel)(object)eventModel;
			}
		}
	}
}
