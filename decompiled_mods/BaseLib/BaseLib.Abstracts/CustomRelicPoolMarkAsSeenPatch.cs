using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.RelicCollection;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(NRelicCollection), "LoadRelics")]
internal static class CustomRelicPoolMarkAsSeenPatch
{
	[HarmonyPrefix]
	public static void MarkAllAsSeen()
	{
		foreach (RelicPoolModel allRelicPool in ModelDb.AllRelicPools)
		{
			if (!(allRelicPool is CustomRelicPoolModel { SeenByDefault: not false }))
			{
				continue;
			}
			foreach (RelicModel allRelic in allRelicPool.AllRelics)
			{
				SaveManager.Instance.MarkRelicAsSeen(allRelic);
			}
		}
	}
}
