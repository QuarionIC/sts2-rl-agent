using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(OrbModel), "CreateSprite")]
internal class CustomOrbCreateSprite
{
	[HarmonyPrefix]
	private static bool Custom(OrbModel __instance, ref Node2D __result)
	{
		if (!(__instance is CustomOrbModel customOrbModel))
		{
			return true;
		}
		Node2D val = customOrbModel.CreateCustomSprite();
		if (val == null)
		{
			return true;
		}
		__result = val;
		return false;
	}
}
