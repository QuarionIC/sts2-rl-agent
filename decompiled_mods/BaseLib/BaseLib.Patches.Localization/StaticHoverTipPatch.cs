using System.Collections.Generic;
using BaseLib.Patches.Content;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(HoverTipFactory), "Static")]
public static class StaticHoverTipPatch
{
	[HarmonyPrefix]
	public static bool StaticHoverTipPrefix(StaticHoverTip tip, DynamicVar[] vars, ref IHoverTip __result)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected I4, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		if (!CustomEnums.GeneratedCustomEnumEntries.TryGetValue(typeof(StaticHoverTip), out Dictionary<int, (string, string)> value))
		{
			return true;
		}
		if (!value.TryGetValue((int)tip, out var value2))
		{
			return true;
		}
		string text = StringHelper.Slugify(value2.Item2);
		string item = value2.Item1;
		LocString val = new LocString("static_hover_tips", item + text + ".title");
		LocString val2 = new LocString("static_hover_tips", item + text + ".description");
		foreach (DynamicVar val3 in vars)
		{
			val.Add(val3);
			val2.Add(val3);
		}
		__result = (IHoverTip)(object)new HoverTip(val, val2, (Texture2D)null);
		return false;
	}
}
