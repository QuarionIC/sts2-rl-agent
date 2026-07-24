using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(CardModel), "ToMutable")]
public class TagClassicSlimedPatch
{
	public static void Postfix(CardModel __result)
	{
		if (__result is Slimed && ClassicSlimedTracker.CreatingClassicSlimed)
		{
			ClassicSlimedTracker.IsClassicSlimed.Set(__result, true);
		}
	}
}
