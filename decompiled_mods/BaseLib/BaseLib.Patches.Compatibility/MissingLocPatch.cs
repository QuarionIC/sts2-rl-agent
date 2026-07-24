using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Compatibility;

[HarmonyPatch(typeof(LocTable))]
public class MissingLocPatch
{
	[HarmonyPatch("GetLocString")]
	[HarmonyPrefix]
	public static bool Prefix(LocTable __instance, string key, string ____name, ref LocString __result)
	{
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Expected O, but got Unknown
		if (__instance.HasEntry(key))
		{
			return true;
		}
		BaseLibMain.Logger.Warn($"GetLocString: Key '{key}' not found in table '{____name}'", 1);
		__result = new LocString(____name, key);
		return false;
	}

	[HarmonyPatch("GetRawText")]
	[HarmonyPrefix]
	public static bool Prefix(LocTable __instance, string key, string ____name, ref string __result)
	{
		if (__instance.HasEntry(key))
		{
			return true;
		}
		BaseLibMain.Logger.Warn($"GetRawText: Key '{key}' not found in table '{____name}'", 1);
		__result = ____name + "." + key;
		return false;
	}
}
