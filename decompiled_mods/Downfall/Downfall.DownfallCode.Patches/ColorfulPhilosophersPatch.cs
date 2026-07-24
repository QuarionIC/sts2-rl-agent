using System.Collections.Generic;
using System.Linq;
using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(ColorfulPhilosophers))]
public static class ColorfulPhilosophersPatch
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	public static void Postfix(ref IEnumerable<CardPoolModel> __result)
	{
		IEnumerable<CardPoolModel> second = from e in ModelDb.AllCharacters.OfType<DownfallCharacterModel>()
			select ((CharacterModel)e).CardPool;
		__result = __result.Concat(second);
	}
}
