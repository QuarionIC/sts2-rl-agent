using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.PotionLab;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NPotionLab), "LoadPotions")]
internal static class CustomPotionPoolMarkAsSeenPatch
{
	[HarmonyPrefix]
	public static void MarkAllAsSeen()
	{
		foreach (PotionPoolModel allPotionPool in ModelDb.AllPotionPools)
		{
			if (!(allPotionPool is CustomPotionPoolModel { SeenByDefault: not false }))
			{
				continue;
			}
			foreach (PotionModel allPotion in allPotionPool.AllPotions)
			{
				SaveManager.Instance.MarkPotionAsSeen(allPotion);
			}
		}
	}
}
