using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BaseLib.Config;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
public static class NMainMenu_Ready_Patch
{
	public static void Postfix()
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (ModConfig.ModConfigLogger.PendingUserMessages.Count != 0)
		{
			Callable val = Callable.From((Action)ModConfig.ShowAndClearPendingErrors);
			((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		}
	}
}
