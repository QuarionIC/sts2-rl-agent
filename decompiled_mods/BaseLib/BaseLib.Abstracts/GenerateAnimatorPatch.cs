using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CharacterModel), "GenerateAnimator")]
internal class GenerateAnimatorPatch
{
	[HarmonyPrefix]
	private static bool CustomAnimator(CharacterModel __instance, MegaSprite controller, ref CreatureAnimator? __result)
	{
		if (!(__instance is CustomCharacterModel customCharacterModel))
		{
			return true;
		}
		__result = customCharacterModel.SetupCustomAnimationStates(controller);
		return __result == null;
	}
}
