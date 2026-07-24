using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(MonsterModel), "CreateVisuals")]
internal class CreateVisuals
{
	[HarmonyPrefix]
	private static bool CustomCreateVisuals(MonsterModel __instance, ref NCreatureVisuals? __result)
	{
		if (!(__instance is CustomMonsterModel customMonsterModel))
		{
			return true;
		}
		__result = customMonsterModel.CreateCustomVisuals();
		return __result == null;
	}
}
