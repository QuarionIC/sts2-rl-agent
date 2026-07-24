using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(CardPile), "Get")]
internal class GetCombatPile
{
	[HarmonyPrefix]
	private static bool CheckCustomPile(PileType type, Player player, ref CardPile? __result)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		__result = (CardPile?)(object)CustomPiles.GetCustomPile(player.PlayerCombatState, type);
		return __result == null;
	}
}
