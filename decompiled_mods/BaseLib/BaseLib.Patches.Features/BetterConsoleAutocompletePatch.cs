using System;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(AbstractConsoleCmd), "CompleteArgument")]
public static class BetterConsoleAutocompletePatch
{
	[HarmonyPrefix]
	public static void UseContainsMatching(ref Func<string, string, bool>? matchPredicate)
	{
		if (matchPredicate == null)
		{
			matchPredicate = (string candidate, string partial) => candidate.Contains(partial, StringComparison.OrdinalIgnoreCase);
		}
	}

	[HarmonyPostfix]
	public static void SortByRelevance(ref CompletionResult __result, string partialArg)
	{
		if (string.IsNullOrWhiteSpace(partialArg))
		{
			return;
		}
		__result.Candidates = __result.Candidates.OrderBy(delegate(string e)
		{
			if (e.StartsWith(partialArg, StringComparison.OrdinalIgnoreCase))
			{
				return 0;
			}
			return (e.Contains('-') ? e.Split('-', 2)[1] : e).StartsWith(partialArg, StringComparison.OrdinalIgnoreCase) ? 1 : 2;
		}).ThenBy((string e) => e).ToList();
	}
}
