using System;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(NSettingsScreen), "_Ready")]
public static class InjectSettingsModConfigPatch
{
	public static void Postfix(NSettingsScreen __instance)
	{
		try
		{
			InjectSettingsMenuEntry(__instance);
		}
		catch (Exception)
		{
			ModConfig.ModConfigLogger.Error("BaseLib was unable to add the Mod Configuration entry to the Settings menu.This is likely either due to a recent game update, or mod incompatibility.");
		}
	}

	private static void InjectSettingsMenuEntry(NSettingsScreen settingsScreen)
	{
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		Control nodeOrNull = ((Node)settingsScreen).GetNodeOrNull<Control>(NodePath.op_Implicit("ScrollContainer/Mask/Clipper/GeneralSettings"));
		ColorRect nodeOrNull2 = ((Node)nodeOrNull).GetNodeOrNull<ColorRect>(NodePath.op_Implicit("VBoxContainer/SendFeedbackDivider"));
		MarginContainer nodeOrNull3 = ((Node)nodeOrNull).GetNodeOrNull<MarginContainer>(NodePath.op_Implicit("VBoxContainer/SendFeedback"));
		MarginContainer nodeOrNull4 = ((Node)nodeOrNull).GetNodeOrNull<MarginContainer>(NodePath.op_Implicit("VBoxContainer/Modding"));
		Node val = ((Node)nodeOrNull2).Duplicate(15);
		MarginContainer val2 = (MarginContainer)((Node)nodeOrNull4).Duplicate(15);
		((Node)val2).UniqueNameInOwner = false;
		((Node)val2).Name = StringName.op_Implicit("BaseLibModConfig");
		((CanvasItem)val2).Visible = true;
		Control nodeOrNull5 = ((Node)val2).GetNodeOrNull<Control>(NodePath.op_Implicit("ModdingButton"));
		((Node)nodeOrNull5).Name = StringName.op_Implicit("BaseLibModConfigButton");
		((Node)nodeOrNull5).UniqueNameInOwner = true;
		((Node)nodeOrNull3).AddSibling(val, false);
		val.AddSibling((Node)(object)val2, false);
		((Node)nodeOrNull5).Owner = (Node)(object)settingsScreen;
		RichTextLabel nodeOrNull6 = ((Node)val2).GetNodeOrNull<RichTextLabel>(NodePath.op_Implicit("Label"));
		LocString ifExists = LocString.GetIfExists("settings_ui", "BASELIB.MOD_CONFIG_SETTINGS_ROW.title");
		nodeOrNull6.Text = ((ifExists != null) ? ifExists.GetFormattedText() : null) ?? "Mod Configuration (BaseLib)";
		Label nodeOrNull7 = ((Node)nodeOrNull5).GetNodeOrNull<Label>(NodePath.op_Implicit("Label"));
		LocString ifExists2 = LocString.GetIfExists("settings_ui", "BASELIB.MOD_CONFIG_SETTINGS_ROW.button");
		nodeOrNull7.Text = ((ifExists2 != null) ? ifExists2.GetFormattedText() : null) ?? "Open Config";
		((GodotObject)nodeOrNull5).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
		{
			NSubmenuStack stack = ((NSubmenu)settingsScreen)._stack;
			NMainMenuSubmenuStack val3 = (NMainMenuSubmenuStack)(object)((stack is NMainMenuSubmenuStack) ? stack : null);
			if (val3 != null)
			{
				val3.PushSubmenuType<NModConfigSubmenu>();
			}
			else
			{
				ModConfig.ModConfigLogger.Error("Unable to open BaseLib's Mod Configuration.", showInGui: false);
			}
		}), 0u);
		Control nodeOrNull8 = ((Node)nodeOrNull3).GetNodeOrNull<Control>(NodePath.op_Implicit("FeedbackButton"));
		Control nodeOrNull9 = ((Node)nodeOrNull4).GetNodeOrNull<Control>(NodePath.op_Implicit("%ModdingButton"));
		Control nodeOrNull10 = ((Node)nodeOrNull).GetNodeOrNull<Control>(NodePath.op_Implicit("VBoxContainer/Credits/CreditsButton"));
		if (nodeOrNull8 != null && nodeOrNull9 != null && nodeOrNull10 != null)
		{
			nodeOrNull10.FocusNeighborTop = ((Node)nodeOrNull10).GetPathTo((Node)(object)nodeOrNull9, false);
			nodeOrNull9.FocusNeighborBottom = ((Node)nodeOrNull9).GetPathTo((Node)(object)nodeOrNull10, false);
			nodeOrNull5.FocusNeighborTop = ((Node)nodeOrNull5).GetPathTo((Node)(object)nodeOrNull8, false);
			nodeOrNull5.FocusNeighborBottom = ((Node)nodeOrNull5).GetPathTo((Node)(object)nodeOrNull9, false);
			nodeOrNull8.FocusNeighborBottom = ((Node)nodeOrNull8).GetPathTo((Node)(object)nodeOrNull5, false);
			nodeOrNull9.FocusNeighborTop = ((Node)nodeOrNull9).GetPathTo((Node)(object)nodeOrNull5, false);
		}
	}
}
