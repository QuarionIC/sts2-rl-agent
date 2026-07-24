using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(NSettingsScreen), "OnSubmenuShown")]
public static class NSettingsScreen_OnSubmenuShown_Patch
{
	public static void Postfix(NSettingsScreen __instance)
	{
		bool flag = ((NSubmenu)__instance)._stack is NMainMenuSubmenuStack;
		NButton nodeOrNull = ((Node)__instance).GetNodeOrNull<NButton>(NodePath.op_Implicit("%BaseLibModConfigButton"));
		if (nodeOrNull != null)
		{
			if (flag)
			{
				((NClickableControl)nodeOrNull).Enable();
			}
			else
			{
				((NClickableControl)nodeOrNull).Disable();
			}
		}
	}
}
