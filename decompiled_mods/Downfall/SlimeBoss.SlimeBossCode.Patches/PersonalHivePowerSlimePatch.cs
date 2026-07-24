using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Patches;

[HarmonyPatch(typeof(PersonalHivePower), "AfterDamageReceived")]
internal static class PersonalHivePowerSlimePatch
{
	private static bool Prefix(Creature? dealer, ref Task __result)
	{
		if (!(((dealer != null) ? dealer.Monster : null) is SlimeModel))
		{
			return true;
		}
		__result = Task.CompletedTask;
		return false;
	}
}
