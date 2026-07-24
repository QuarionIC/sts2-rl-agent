using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class VisualsPath
{
	[HarmonyPrefix]
	private static bool CustomVisualsPath(MonsterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomMonsterModel customMonsterModel))
		{
			return true;
		}
		__result = customMonsterModel.CustomVisualPath;
		return __result == null;
	}
}
