using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ActsFromThePast.Patches.Audio;

public class SfxPatches
{
	[HarmonyPatch(typeof(PowerModel), "AfterApplied")]
	public class SneckoConfusedSfxPatch
	{
		public static void Postfix(PowerModel __instance, Creature? applier)
		{
			if (__instance is ConfusedPower && ((applier != null) ? applier.Monster : null) is Snecko)
			{
				AFTPModAudio.Play("snecko", "confusion_applied");
			}
		}
	}
}
