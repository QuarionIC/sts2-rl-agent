using System;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
public static class InjectMainMenuModConfigPatch
{
	public static void Prefix(NMainMenu __instance)
	{
		if (!BaseLibConfig.ShowModConfigInMainMenu)
		{
			return;
		}
		try
		{
			InjectMainMenuEntry(__instance);
		}
		catch (Exception)
		{
			ModConfig.ModConfigLogger.Error("BaseLib was unable to add the Mod Configuration entry to the main menu.This is likely either due to a recent game update, or mod incompatibility.");
		}
	}

	public static void Postfix(NMainMenu __instance)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		NButton[] mainMenuButtons = __instance.MainMenuButtons;
		foreach (NButton obj in mainMenuButtons)
		{
			((Control)obj).FocusNeighborLeft = new NodePath(".");
			((Control)obj).FocusNeighborRight = new NodePath(".");
		}
	}

	private static void InjectMainMenuEntry(NMainMenu mainMenu)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Expected O, but got Unknown
		NMainMenuTextButton nodeOrNull = ((Node)mainMenu).GetNodeOrNull<NMainMenuTextButton>(NodePath.op_Implicit("MainMenuTextButtons/SettingsButton"));
		NMainMenuTextButton modConfigButton = (NMainMenuTextButton)((Node)nodeOrNull).Duplicate(15);
		((Node)modConfigButton).Name = StringName.op_Implicit("ModConfigButton");
		((GodotObject)modConfigButton).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			mainMenu._lastHitButton = modConfigButton;
			mainMenu.SubmenuStack.PushSubmenuType<NModConfigSubmenu>();
		}), 0u);
		((Node)nodeOrNull).AddSibling((Node)(object)modConfigButton, false);
		modConfigButton.SetLocalization("BASELIB-MOD_CONFIGURATION");
		((Control)modConfigButton).CustomMinimumSize = new Vector2(300f, ((Control)modConfigButton).CustomMinimumSize.Y);
		NodePath val = new NodePath(".");
		((Control)modConfigButton).FocusNeighborRight = val;
		((Control)modConfigButton).FocusNeighborLeft = val;
	}
}
