using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Config;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal static class ModSourceTooltip
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class RelicTips
	{
		[HarmonyPostfix]
		private static void Postfix(RelicModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowRelicModSource ? Fold(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class PotionTips
	{
		[HarmonyPostfix]
		private static void Postfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowPotionModSource ? Fold(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class CardTips
	{
		[HarmonyPostfix]
		private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowCardModSource ? AppendBox(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class PowerTips
	{
		[HarmonyPostfix]
		private static void Postfix(PowerModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowCombatElementModSource ? Fold(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class OrbTips
	{
		[HarmonyPostfix]
		private static void Postfix(OrbModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowCombatElementModSource ? Fold(__result, (AbstractModel)(object)__instance, foldLast: true) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class EnchantmentTips
	{
		[HarmonyPostfix]
		private static void Postfix(EnchantmentModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowCombatElementModSource ? Fold(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static class AfflictionTips
	{
		[HarmonyPostfix]
		private static void Postfix(AfflictionModel __instance, ref IEnumerable<IHoverTip> __result)
		{
			__result = (BaseLibConfig.ShowCombatElementModSource ? Fold(__result, (AbstractModel)(object)__instance) : __result);
		}
	}

	private const string FoldedLineColor = "#8a8a8a";

	private static readonly FieldInfo? DescriptionField = AccessTools.Field(typeof(HoverTip), "<Description>k__BackingField");

	private static LocString TitleLoc()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new LocString("gameplay_ui", "BASELIB-MOD_SOURCE.title");
	}

	private static IEnumerable<IHoverTip> Fold(IEnumerable<IHoverTip> tips, AbstractModel model, bool foldLast = false)
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Expected O, but got Unknown
		string text = WhatMod.FindModName(((object)model).GetType());
		if (text == null)
		{
			return tips;
		}
		List<IHoverTip> list = tips.ToList();
		int num = (foldLast ? (list.Count - 1) : 0);
		if (DescriptionField != null && num >= 0 && list[num] is HoverTip val)
		{
			object obj = val;
			string text2 = $"[color={"#8a8a8a"}]{TitleLoc().GetFormattedText()}: {text}[/color]";
			DescriptionField.SetValue(obj, ((HoverTip)(ref val)).Description + "\n" + text2);
			list[num] = (IHoverTip)obj;
		}
		else
		{
			HoverTip val2 = default(HoverTip);
			((HoverTip)(ref val2))._002Ector(TitleLoc(), text, (Texture2D)null);
			((HoverTip)(ref val2)).Id = "BASELIB-MOD_SOURCE-" + text;
			list.Add((IHoverTip)(object)val2);
		}
		return list;
	}

	private static IEnumerable<IHoverTip> AppendBox(IEnumerable<IHoverTip> tips, AbstractModel model)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		string text = WhatMod.FindModName(((object)model).GetType());
		if (text != null)
		{
			HoverTip val = default(HoverTip);
			((HoverTip)(ref val))._002Ector(TitleLoc(), text, (Texture2D)null);
			((HoverTip)(ref val)).Id = "BASELIB-MOD_SOURCE-" + text;
			return tips.Append((IHoverTip)(object)val);
		}
		return tips;
	}
}
