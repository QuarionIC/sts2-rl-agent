using System;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(CardCmd))]
public static class NecronomicurseTransformPatch
{
	[HarmonyPatch("TransformToRandom")]
	[HarmonyPrefix]
	public static bool TransformToRandomPrefix(CardModel original, ref Task<CardPileAddResult> __result, CardPreviewStyle style)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if (!(original is Necronomicurse))
		{
			return true;
		}
		__result = ForceNecronomicurse(original, style);
		return false;
	}

	private static async Task<CardPileAddResult> ForceNecronomicurse(CardModel original, CardPreviewStyle style)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		return (CardPileAddResult)(((_003F?)(await CardCmd.TransformTo<Necronomicurse>(original, style))) ?? new CardPileAddResult
		{
			success = false
		});
	}

	[HarmonyPatch("Transform", new Type[]
	{
		typeof(CardModel),
		typeof(CardModel),
		typeof(CardPreviewStyle)
	})]
	[HarmonyPrefix]
	public static void TransformDirectPrefix(CardModel original, ref CardModel replacement)
	{
		if (original is Necronomicurse && !(replacement is Necronomicurse))
		{
			replacement = (CardModel)(object)original.CardScope.CreateCard<Necronomicurse>(original.Owner);
		}
	}
}
