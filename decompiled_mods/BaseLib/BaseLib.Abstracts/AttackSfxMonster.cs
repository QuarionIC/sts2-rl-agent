using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class AttackSfxMonster
{
	[HarmonyPrefix]
	private static bool Custom(MonsterModel __instance, ref string? __result)
	{
		if (!(__instance is CustomMonsterModel customMonsterModel))
		{
			return true;
		}
		__result = customMonsterModel.CustomAttackSfx;
		return __result == null;
	}
}
