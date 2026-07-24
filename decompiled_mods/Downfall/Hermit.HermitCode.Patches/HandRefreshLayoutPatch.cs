using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Hermit.HermitCode.Patches;

[HarmonyPatch(typeof(NPlayerHand), "RefreshLayout")]
internal static class HandRefreshLayoutPatch
{
	private static void Postfix()
	{
		if (!HandVisualSync.IsSyncing)
		{
			HandVisualSync.Queue();
		}
	}
}
