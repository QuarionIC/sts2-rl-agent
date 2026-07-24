using System.Linq;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Abstract;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NHandCardHolder), "get_ShouldGlowGold")]
internal static class CardModifierGlowGoldPatch
{
	private static void Postfix(NHandCardHolder __instance, ref bool __result)
	{
		if (!__result)
		{
			NCard cardNode = ((NCardHolder)__instance).CardNode;
			CardModel val = ((cardNode != null) ? cardNode.Model : null);
			if (val != null && CardModifier.Modifiers(val).OfType<DownfallCardModifier>().Any((DownfallCardModifier e) => e.ShouldGlowGold))
			{
				__result = true;
			}
		}
	}
}
