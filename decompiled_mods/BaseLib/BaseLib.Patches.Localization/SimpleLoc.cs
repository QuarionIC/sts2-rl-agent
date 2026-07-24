using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(LocManager), "LoadTable")]
public static class SimpleLoc
{
	private static readonly HashSet<string> SimpleLocEnabled = new HashSet<string>();

	private static readonly Dictionary<string, string> SpecialVarDictionary = new Dictionary<string, string>
	{
		{ "D", "Damage" },
		{ "CD", "CalculatedDamage" },
		{ "B", "Block" },
		{ "CB", "CalculatedBlock" },
		{ "C", "Cards" },
		{ "E", "Energy" },
		{ "H", "Heal" }
	};

	[GeneratedRegex("(?<=^|[^/])\\*({.+?}|.+?(?=$|[\\s*.,|}]))\\*?")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex GoldHighlightRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__GoldHighlightRegex_1.Instance;

	[GeneratedRegex("(?<=^|[^/])\\$({.+?}|.+?(?=$|[\\s$.,|}]))\\$?")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex BlueHighlightRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__BlueHighlightRegex_2.Instance;

	[GeneratedRegex("({)([^:}.]+)([:}])")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex NormalVariableRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__NormalVariableRegex_3.Instance;

	[GeneratedRegex("!(.*?)!")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex DiffVariableRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__DiffVariableRegex_4.Instance;

	[GeneratedRegex("@(.*?)@")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex InverseVariableRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__InverseVariableRegex_5.Instance;

	[GeneratedRegex("(?<=^|[^/])(?:(?:-(.+?)-)|(?:\\+(.*?[^/])\\+))(?:\\+(.*?[^/])\\+)?")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex UpgradeSwapRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__UpgradeSwapRegex_6.Instance;

	[GeneratedRegex("(.*?{)([^{]+?)((?::[^{]*)?}(?:(?:[^{]*?[^{/])|(?:)))\\(([^()]+?)\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex PluralizeRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__PluralizeRegex_7.Instance;

	[GeneratedRegex("\\[(?:(E\\?)|(E+))\\]")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "9.0.14.6317")]
	private static Regex EnergyIconsRegex => _003CRegexGenerator_g_003EF2510F4315406E63A39BEC1503FA28868C36343733B7483E5396C61FEAF06DCCD__EnergyIconsRegex_8.Instance;

	public static void EnableSimpleLoc(string modId)
	{
		SimpleLocEnabled.Add(modId);
	}

	[HarmonyPostfix]
	private static void ProcessSimpleLoc(string path, Dictionary<string, string>? __result)
	{
		if (__result == null)
		{
			return;
		}
		string[] array = StringExtensions.SimplifyPath(path).Split('/');
		int i;
		for (i = 0; i < array.Length && !(array[i] == "localization"); i++)
		{
		}
		if (i >= array.Length || i == 0)
		{
			return;
		}
		string item = array[i - 1];
		bool flag = SimpleLocEnabled.Contains(item);
		foreach (string item2 in __result.Keys.ToList())
		{
			string text = __result[item2];
			if (flag)
			{
				if (text.StartsWith('#'))
				{
					string text2 = text;
					__result[item2] = text2.Substring(1, text2.Length - 1);
				}
				else
				{
					__result[item2] = Simplify(text);
				}
			}
			else if (text.StartsWith('#'))
			{
				string text2 = text;
				__result[item2] = Simplify(text2.Substring(1, text2.Length - 1));
			}
		}
	}

	public static string TrySimplify(string loc)
	{
		if (loc.StartsWith('#'))
		{
			return Simplify(loc.Substring(1, loc.Length - 1));
		}
		return loc;
	}

	private static string Simplify(string loc)
	{
		if (loc.StartsWith('#'))
		{
			return loc;
		}
		loc = GoldHighlightRegex.Replace(loc, "[gold]$1[/gold]");
		loc = BlueHighlightRegex.Replace(loc, "[blue]$1[/blue]");
		loc = loc.Replace("/*", "*").Replace("/$", "$");
		loc = NormalVariableRegex.Replace(loc, (Match match) => match.Groups[1].Value + SpecialVarDictionary.GetValueOrDefault(match.Groups[2].Value, match.Groups[2].Value) + match.Groups[3].Value);
		loc = DiffVariableRegex.Replace(loc, (Match match) => ReplaceVarName(match, ":diff()"));
		loc = InverseVariableRegex.Replace(loc, (Match match) => ReplaceVarName(match, ":inverseDiff()"));
		loc = EnergyIconsRegex.Replace(loc, MakeEnergyIcons);
		loc = PluralizeRegex.Replace(loc, "$1$2$3{$2:plural:|$4}");
		loc = loc.Replace("/(", "(");
		loc = UpgradeSwapRegex.Replace(loc, MakeUpgradeSwap);
		loc = loc.Replace("/-", "-").Replace("/+", "+");
		BaseLibMain.Logger.VeryDebug("SimplifiedLoc: " + loc, 1);
		return loc;
	}

	private static string MakeEnergyIcons(Match match)
	{
		if (match.Groups[1].Length > 0)
		{
			return "{Energy:energyIcons()}";
		}
		int length = match.Groups[2].Length;
		if (length != 0)
		{
			return $"{{energyPrefix:energyIcons({length})}}";
		}
		return match.Value;
	}

	private static string MakeUpgradeSwap(Match match)
	{
		string value = match.Groups[1].Value;
		string value2 = match.Groups[2].Value + match.Groups[3].Value;
		return $"{{IfUpgraded:show:{value2}|{value}}}";
	}

	private static string ReplaceVarName(Match match, string processor)
	{
		if (match.Groups.Count <= 1)
		{
			return match.Value;
		}
		string value = match.Groups[1].Value;
		return "{" + SpecialVarDictionary.GetValueOrDefault(value, value) + processor + "}";
	}
}
