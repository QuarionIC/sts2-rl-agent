using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NEventLayout), "SetEvent")]
public static class EventSourceLabel
{
	private const string LabelName = "BaseLibModSourceLabel";

	[HarmonyPostfix]
	private static void AddSourceLabel(NEventLayout __instance, EventModel eventModel)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		if (eventModel is AncientEventModel)
		{
			return;
		}
		MegaLabel nodeOrNull = ((Node)__instance).GetNodeOrNull<MegaLabel>(NodePath.op_Implicit("BaseLibModSourceLabel"));
		string text = (BaseLibConfig.ShowEventModSource ? WhatMod.FindModName(((object)eventModel).GetType()) : null);
		if (text == null)
		{
			if (nodeOrNull != null)
			{
				((Node)nodeOrNull).QueueFree();
			}
			return;
		}
		if (nodeOrNull != null)
		{
			nodeOrNull.SetTextAutoSize(text);
			return;
		}
		MegaLabel val = new MegaLabel
		{
			Name = StringName.op_Implicit("BaseLibModSourceLabel"),
			HorizontalAlignment = (HorizontalAlignment)0,
			VerticalAlignment = (VerticalAlignment)0,
			MinFontSize = 24,
			MaxFontSize = 24,
			MouseFilter = (MouseFilterEnum)2
		};
		Color white = Colors.White;
		white.A = 0.7f;
		((CanvasItem)val).Modulate = white;
		MegaLabel label = val;
		FontVariation val2 = ResourceLoader.Load<FontVariation>("res://themes/kreon_regular_shared.tres", (string)null, (CacheMode)1);
		if (val2 != null)
		{
			((Control)label).AddThemeFontOverride(StringName.op_Implicit("font"), (Font)(object)val2);
		}
		((Control)label).AddThemeColorOverride(StringName.op_Implicit("font_color"), StsColors.cream);
		MegaLabel obj = label;
		StringName obj2 = StringName.op_Implicit("font_outline_color");
		white = Colors.Black;
		white.A = 0.55f;
		((Control)obj).AddThemeColorOverride(obj2, white);
		((Control)label).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 6);
		label.SetTextAutoSize(text);
		((Node)__instance).AddChild((Node)(object)label, false, (InternalMode)0);
		Place();
		((Control)label).Resized += Place;
		((Control)__instance).Resized += Place;
		void Place()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)label))
			{
				Rect2 viewportRect = ((CanvasItem)label).GetViewportRect();
				Vector2 size = ((Rect2)(ref viewportRect)).Size;
				((Control)label).GlobalPosition = new Vector2(56f, size.Y - 56f - ((Control)label).Size.Y);
			}
		}
	}
}
