using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class DeathSfxMonster
{
	[HarmonyPrefix]
	private static bool Custom(MonsterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomMonsterModel customMonsterModel))
		{
			return true;
		}
		__result = customMonsterModel.CustomDeathSfx;
		return __result == null;
	}
}
