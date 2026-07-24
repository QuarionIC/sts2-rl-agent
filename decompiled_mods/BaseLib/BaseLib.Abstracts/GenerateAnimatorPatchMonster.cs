using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(MonsterModel), "GenerateAnimator")]
internal class GenerateAnimatorPatchMonster
{
	[HarmonyPrefix]
	private static bool CustomAnimator(MonsterModel __instance, MegaSprite controller, ref CreatureAnimator? __result)
	{
		if (!(__instance is CustomMonsterModel customMonsterModel))
		{
			return true;
		}
		__result = customMonsterModel.SetupCustomAnimationStates(controller);
		return __result == null;
	}
}
