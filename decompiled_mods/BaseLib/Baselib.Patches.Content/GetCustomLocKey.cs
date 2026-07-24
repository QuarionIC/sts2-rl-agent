using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(CardKeywordExtensions), "GetLocKeyPrefix")]
internal class GetCustomLocKey
{
	[HarmonyPrefix]
	private static bool UseCustomKeywordMap(CardKeyword keyword, ref string? __result)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected I4, but got Unknown
		if (!CustomKeywords.KeywordIDs.TryGetValue((int)keyword, out var value))
		{
			return true;
		}
		__result = value.Key;
		return false;
	}
}
