using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using SmartFormat.Utilities;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(LocManager), "LoadLocFormatters")]
public static class PluralRulesPatch
{
	[HarmonyPostfix]
	private static void FixChinesePlural()
	{
		object obj = typeof(PluralRules).GetProperty("IsoLangToDelegate", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
		obj?.GetType().GetProperty("Item")?.SetValue(obj, PluralRules.GetPluralRule("en"), new object[1] { "zh" });
	}
}
