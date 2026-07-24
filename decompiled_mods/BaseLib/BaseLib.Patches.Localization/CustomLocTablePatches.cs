using System.Collections.Generic;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

internal static class CustomLocTablePatches
{
	[HarmonyPatch(typeof(LocManager), "ListLocalizationFiles")]
	internal static class ListLocalizationFilesPatch
	{
		[HarmonyPostfix]
		public static IEnumerable<string> GetCustomTables(IEnumerable<string> __result)
		{
			return CustomLocTableManager.GetCustomLocTables(__result);
		}
	}

	[HarmonyPatch(typeof(LocManager), "LoadTable")]
	internal static class LoadTablePatch
	{
		[HarmonyPrefix]
		public static bool EmptyDictFallback(string path, ref Dictionary<string, string> __result)
		{
			if (FileAccess.FileExists(path))
			{
				return true;
			}
			__result = new Dictionary<string, string>();
			return false;
		}
	}
}
