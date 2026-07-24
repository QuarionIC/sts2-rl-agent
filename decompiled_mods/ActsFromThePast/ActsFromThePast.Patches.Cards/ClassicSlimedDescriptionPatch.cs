using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch]
public class ClassicSlimedDescriptionPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", new Type[3]
		{
			typeof(PileType),
			AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"),
			typeof(Creature)
		}, (Type[])null);
	}

	public static void Postfix(CardModel __instance, ref string __result)
	{
		if (__instance is Slimed && ClassicSlimedTracker.IsClassicSlimed.Get(__instance))
		{
			LocString description = __instance.Description;
			__instance.DynamicVars.AddTo(description);
			string formattedText = description.GetFormattedText();
			__result = __result.Replace(formattedText, "").Trim('\n');
		}
	}
}
