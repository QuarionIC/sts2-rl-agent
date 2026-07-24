using System;
using BaseLib.Config.UI;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(NMainMenuSubmenuStack), "GetSubmenuType", new Type[] { typeof(Type) })]
public static class InjectModConfigSubmenuTypePatch
{
	private static readonly SpireField<NMainMenuSubmenuStack, NModConfigSubmenu> SubmenuField = new SpireField<NMainMenuSubmenuStack, NModConfigSubmenu>(CreateSubmenu);

	private static NModConfigSubmenu CreateSubmenu(NMainMenuSubmenuStack stack)
	{
		NModConfigSubmenu nModConfigSubmenu = new NModConfigSubmenu();
		((CanvasItem)nModConfigSubmenu).Visible = false;
		GodotTreeExtensions.AddChildSafely((Node)(object)stack, (Node)(object)nModConfigSubmenu);
		return nModConfigSubmenu;
	}

	public static bool Prefix(NMainMenuSubmenuStack __instance, Type type, ref NSubmenu __result)
	{
		if (type != typeof(NModConfigSubmenu))
		{
			return true;
		}
		__result = (NSubmenu)(object)SubmenuField.Get(__instance);
		return false;
	}
}
