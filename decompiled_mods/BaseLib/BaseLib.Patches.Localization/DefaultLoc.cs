using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(ModManager), "GetModdedLocTables")]
public static class DefaultLoc
{
	private static readonly string[] LanguagePreference = new string[13]
	{
		"zhs", "jpn", "deu", "kor", "rus", "spa", "esp", "fra", "tur", "ita",
		"pol", "ptb", "tha"
	};

	private static readonly Dictionary<string, string> _defaultLoc = new Dictionary<string, string>();

	public static void Set(string modId, string defaultLoc)
	{
		if (defaultLoc == "eng")
		{
			BaseLibMain.Logger.Warn("Mod " + modId + " sets English as default loc; this does nothing, as game already will use English as default", 1);
			return;
		}
		if (_defaultLoc.Remove(modId, out string value))
		{
			BaseLibMain.Logger.Warn($"Default localization is set multiple times for {modId}; previous value {value}, new value {defaultLoc}", 1);
		}
		_defaultLoc.Add(modId, defaultLoc);
	}

	[HarmonyPostfix]
	private static void LoadDefaultTablesFirst(string language, string file, ref IEnumerable<string> __result)
	{
		List<string> list = new List<string>();
		foreach (Mod loadedMod in ModManager.GetLoadedMods())
		{
			if (loadedMod.manifest?.id == null)
			{
				continue;
			}
			if (!_defaultLoc.TryGetValue(loadedMod.manifest.id, out string value))
			{
				string text = $"res://{loadedMod.manifest.id}/localization/{language}/{file}";
				if (ResourceLoader.Exists(text, ""))
				{
					continue;
				}
				BaseLibMain.Logger.VeryDebug("\"" + text + "\" not found and DefaultLoc not set; looking for existing loc file", 1);
				HashSet<string> hashSet = new HashSet<string>();
				foreach (string item in from str in ResourceLoader.ListDirectory($"res://{loadedMod.manifest.id}/localization/{language}/")
					where str.EndsWith('/')
					select str)
				{
					if (item != null)
					{
						value = item;
						hashSet.Add(item);
					}
				}
				if (hashSet.Count == 0 || hashSet.Contains("eng"))
				{
					continue;
				}
				string[] languagePreference = LanguagePreference;
				foreach (string text2 in languagePreference)
				{
					if (hashSet.Contains(text2))
					{
						value = text2;
						break;
					}
				}
				if (value == null)
				{
					continue;
				}
			}
			if (!(value == language))
			{
				string text3 = $"res://{loadedMod.manifest.id}/localization/{value}/{file}";
				if (ResourceLoader.Exists(text3, ""))
				{
					list.Add(text3);
				}
			}
		}
		BaseLibMain.Logger.Debug($"Found {list.Count} default loc files; [{string.Join(", ", list)}]", 1);
		list.AddRange(__result);
		__result = list;
	}
}
