using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(PileTypeExtensions), "IsCombatPile")]
internal class IsCombatPile
{
	[HarmonyPrefix]
	private static bool CustomIsCombat(PileType pileType, ref bool __result)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		if (CustomPiles.CustomPileProviders.ContainsKey(pileType))
		{
			__result = true;
			return false;
		}
		return true;
	}
}
