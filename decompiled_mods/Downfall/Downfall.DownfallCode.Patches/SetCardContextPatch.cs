using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class SetCardContextPatch
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

	public static void Prefix(CardModel __instance, out CardModel? __state)
	{
		__state = GetCardTextPatch.CurrentCard;
		GetCardTextPatch.CurrentCard = __instance;
	}

	public static void Finalizer(CardModel? __state)
	{
		GetCardTextPatch.CurrentCard = __state;
	}
}
