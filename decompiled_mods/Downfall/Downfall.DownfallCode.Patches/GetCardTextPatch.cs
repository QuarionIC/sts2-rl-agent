using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(CardKeywordExtensions), "GetCardText")]
public static class GetCardTextPatch
{
	[ThreadStatic]
	public static CardModel? CurrentCard;

	public static void Postfix(CardKeyword keyword, ref string __result)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		CardModel currentCard = CurrentCard;
		if (currentCard == null)
		{
			return;
		}
		try
		{
			__result = CardKeywordSubRegistry.AppendSubs(__result, keyword, currentCard);
		}
		catch (Exception value)
		{
			DownfallMainFile.Logger.Error($"AppendSubs failed for keyword '{keyword}': {value}", 1);
		}
	}
}
