using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class AncientSeaGlassConsolePatch
{
	private const string SeaGlassKey = "SEA_GLASS";

	private const string Prefix = "SEA_GLASS_";

	private static string? _forcedSeaGlassCharacter;

	[HarmonyPrefix]
	[HarmonyPatch(typeof(AncientConsoleCmd), "Process")]
	private static void RewriteChoice(string[] args)
	{
		_forcedSeaGlassCharacter = null;
		if (args.Length > 1 && args[1].ToUpperInvariant().StartsWith("SEA_GLASS_"))
		{
			_forcedSeaGlassCharacter = args[1].ToUpperInvariant().Substring("SEA_GLASS_".Length);
			args[1] = "SEA_GLASS";
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	private static bool ForceSeaGlassCharacter(Orobas __instance, ref IEnumerable<EventOption> __result)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		if (_forcedSeaGlassCharacter == null)
		{
			return true;
		}
		CharacterModel val = ModelDb.AllCharacters.FirstOrDefault((Func<CharacterModel, bool>)((CharacterModel c) => ((AbstractModel)c).Id.Entry.Equals(_forcedSeaGlassCharacter, StringComparison.OrdinalIgnoreCase)));
		if (val == null)
		{
			return true;
		}
		SeaGlass val2 = (SeaGlass)((RelicModel)ModelDb.Relic<SeaGlass>()).ToMutable();
		val2.CharacterId = ((AbstractModel)val).Id;
		__result = new _003C_003Ez__ReadOnlySingleElementList<EventOption>(((AncientEventModel)__instance).RelicOption((RelicModel)(object)val2, "INITIAL", (string)null));
		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(AncientConsoleCmd), "GetArgumentCompletions")]
	private static bool ExpandCompletions(AncientConsoleCmd __instance, string[] args, ref CompletionResult __result)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		if (args.Length != 2)
		{
			return true;
		}
		EventModel byIdOrNull = ModelDb.GetByIdOrNull<EventModel>(new ModelId(ModelDb.GetCategory(typeof(EventModel)), args[0].ToUpperInvariant()));
		Orobas val = (Orobas)(object)((byIdOrNull is Orobas) ? byIdOrNull : null);
		if (val == null)
		{
			return true;
		}
		List<string> list = new List<string>();
		foreach (string item in ((AncientEventModel)val).AllPossibleOptions.Select((EventOption o) => o.TextKey.Split('.').Last()))
		{
			if (item == "SEA_GLASS")
			{
				if (!list.Any((string n) => n.StartsWith("SEA_GLASS_")))
				{
					list.AddRange(ModelDb.AllCharacters.Select((CharacterModel c) => "SEA_GLASS_" + ((AbstractModel)c).Id.Entry));
				}
			}
			else if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		__result = ((AbstractConsoleCmd)__instance).CompleteArgument((IEnumerable<string>)list, new string[1] { args[0] }, args[1], (CompletionType)2, (Func<string, string, bool>)null);
		return false;
	}
}
