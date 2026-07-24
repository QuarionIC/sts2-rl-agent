using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BaseLib.Config.UI;

[HarmonyPatch(typeof(NDropdownPositioner), "OnVisibilityChange")]
public static class NDropdownPositioner_OnVisibilityChange_Patch
{
	public static bool Prefix(NDropdownPositioner __instance)
	{
		return !(__instance._dropdownNode is NConfigDropdown);
	}
}
