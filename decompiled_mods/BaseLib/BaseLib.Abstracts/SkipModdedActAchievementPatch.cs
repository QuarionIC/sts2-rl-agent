using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(AchievementsHelper), "CheckForDefeatedAllEnemiesAchievement")]
public class SkipModdedActAchievementPatch
{
	[HarmonyPrefix]
	public static bool SkipCustomActs(ActModel act)
	{
		return !(act is CustomActModel);
	}
}
