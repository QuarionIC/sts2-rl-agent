using System.Collections.Generic;
using Downfall.DownfallCode.Localization;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(ModManager), "GetModdedLocTables")]
internal static class GetModdedLocTablesPatch
{
	private static IEnumerable<string> Postfix(IEnumerable<string> values, string language, string file)
	{
		foreach (string value in values)
		{
			yield return value;
		}
		foreach (string id in BundledSubmodLocRegistry.Ids)
		{
			string text = $"res://{id}/localization/{language}/{file}";
			if (ResourceLoader.Exists(text, ""))
			{
				yield return text;
			}
		}
	}
}
