using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Cards;

namespace Hermit.HermitCode.Patches;

[HarmonyPatch(typeof(NCardTransformShineVfx), "UpdateCard")]
internal static class TransformShineUpdateCardPatch
{
	private static void Postfix(CardModel endCard)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		CardPile pile = endCard.Pile;
		if (pile != null && (int)pile.Type == 2)
		{
			HandVisualSync.Queue();
		}
	}
}
