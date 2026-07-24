using System;
using System.Collections.Generic;
using Downfall.DownfallCode.Voting;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NMainMenuSubmenuStack), "GetSubmenuType", new Type[] { typeof(Type) })]
internal static class CustomSubmenuPatch
{
	private static readonly Dictionary<Type, NSubmenu> cache = new Dictionary<Type, NSubmenu>();

	[HarmonyPrefix]
	private static bool Prefix(Type type, NMainMenuSubmenuStack __instance, ref NSubmenu __result)
	{
		MainMenuButtonRegistry.Entry entry = MainMenuButtonRegistry.FindBySubmenuType(type);
		if (entry == null || entry.CreateSubmenu == null)
		{
			return true;
		}
		if (!cache.TryGetValue(type, out NSubmenu value) || !GodotObject.IsInstanceValid((GodotObject)(object)value))
		{
			value = entry.CreateSubmenu();
			if (value == null)
			{
				return true;
			}
			((CanvasItem)value).Visible = false;
			GodotTreeExtensions.AddChildSafely((Node)(object)__instance, (Node)(object)value);
			cache[type] = value;
		}
		__result = value;
		return false;
	}
}
