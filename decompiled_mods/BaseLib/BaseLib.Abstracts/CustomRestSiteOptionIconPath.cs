using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class CustomRestSiteOptionIconPath
{
	[HarmonyPrefix]
	private static bool Custom(RestSiteOption __instance, ref string __result)
	{
		if (__instance is CustomRestSiteOption customRestSiteOption)
		{
			string customIconPath = customRestSiteOption.CustomIconPath;
			if (customIconPath != null)
			{
				__result = customIconPath;
				return false;
			}
		}
		return true;
	}
}
