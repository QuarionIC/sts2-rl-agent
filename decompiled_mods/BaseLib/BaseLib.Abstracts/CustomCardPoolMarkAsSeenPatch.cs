using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CardLibrary;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NCardLibraryGrid), "RefreshVisibility")]
internal static class CustomCardPoolMarkAsSeenPatch
{
	[HarmonyPrefix]
	public static void MarkAllAsSeen()
	{
		foreach (CardPoolModel allCardPool in ModelDb.AllCardPools)
		{
			if (!(allCardPool is CustomCardPoolModel { SeenByDefault: not false }))
			{
				continue;
			}
			foreach (CardModel allCard in allCardPool.AllCards)
			{
				SaveManager.Instance.MarkCardAsSeen(allCard);
			}
		}
	}
}
