using Downfall.DownfallCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(CardKeywordExtensions), "GetCardText")]
internal static class KeywordColorPatch
{
	[HarmonyPostfix]
	private static void Postfix(CardKeyword keyword, ref string __result)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		if (KeywordColorRegistry.TryGetColor(keyword, out string color))
		{
			__result = __result.Replace("[gold]", "[" + color + "]").Replace("[/gold]", "[/" + color + "]");
		}
	}
}
