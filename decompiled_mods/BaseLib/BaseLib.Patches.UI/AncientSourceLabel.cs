using System.Threading.Tasks;
using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Patches.UI;

[HarmonyPatch(typeof(NAncientNameBanner), "AnimateVfx")]
public static class AncientSourceLabel
{
	private const string LabelName = "BaseLibModSourceLabel";

	[HarmonyPostfix]
	private static void Postfix(NAncientNameBanner __instance, ref Task __result)
	{
		__result = AddAfterSettled(__result, __instance);
	}

	private static async Task AddAfterSettled(Task original, NAncientNameBanner banner)
	{
		await original;
		if (!GodotObject.IsInstanceValid((GodotObject)(object)banner) || !BaseLibConfig.ShowAncientModSource)
		{
			return;
		}
		AncientEventModel value = Traverse.Create((object)banner).Field("_ancient").GetValue<AncientEventModel>();
		if (value == null)
		{
			return;
		}
		string text = WhatMod.FindModName(((object)value).GetType());
		if (text == null)
		{
			return;
		}
		MegaLabel nodeOrNull = ((Node)banner).GetNodeOrNull<MegaLabel>(NodePath.op_Implicit("%Epithet"));
		if (nodeOrNull != null && ((Node)nodeOrNull).GetNodeOrNull(NodePath.op_Implicit("BaseLibModSourceLabel")) == null)
		{
			MegaLabel val = new MegaLabel
			{
				Name = StringName.op_Implicit("BaseLibModSourceLabel"),
				AnchorRight = 1f,
				AnchorBottom = 1f,
				GrowHorizontal = (GrowDirection)2,
				GrowVertical = (GrowDirection)2,
				OffsetTop = 26f,
				OffsetBottom = 26f,
				HorizontalAlignment = (HorizontalAlignment)0,
				VerticalAlignment = (VerticalAlignment)2,
				MinFontSize = 11,
				MaxFontSize = 14,
				MouseFilter = (MouseFilterEnum)2
			};
			Font themeFont = ((Control)nodeOrNull).GetThemeFont(StringName.op_Implicit("font"), (StringName)null);
			if (themeFont != null)
			{
				((Control)val).AddThemeFontOverride(StringName.op_Implicit("font"), themeFont);
			}
			((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_color"), StsColors.cream);
			((Control)val).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), Colors.Transparent);
			val.SetTextAutoSize(text ?? "");
			((Node)nodeOrNull).AddChild((Node)(object)val, false, (InternalMode)0);
		}
	}
}
