using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace ActsFromThePast.Patches.LogSuppressors;

[HarmonyPatch]
public static class LogSuppressorPatches
{
	private static readonly HashSet<string> ModdedEnemies = new HashSet<string>
	{
		"AcidSlimeLarge", "AcidSlimeMedium", "AcidSlimeSmall", "Cultist", "FungiBeast", "GremlinFat", "GremlinMad", "GremlinShield", "GremlinSneaky", "GremlinWizard",
		"JawWorm", "Looter", "LouseGreen", "LouseRed", "SlaverBlue", "SlaverRed", "SpikeSlimeLarge", "SpikeSlimeMedium", "SpikeSlimeSmall", "GremlinNob",
		"Lagavulin", "Sentry", "Guardian", "Hexaghost", "SlimeBoss", "Byrd", "Centurion", "Mugger", "Mystic", "Chosen",
		"ShelledParasite", "SnakePlant", "SphericGuardian", "Pointy", "Romeo", "Bear", "Taskmaster", "BookOfStabbing", "GremlinLeader", "TorchHead",
		"Collector", "Champ", "BronzeAutomaton", "BronzeOrb", "Darkling", "Exploder", "Maw", "OrbWalker", "Repulsor", "Spiker",
		"SpireGrowth", "Transient", "WrithingMass", "GiantHead", "Nemesis", "Reptomancer", "SnakeDagger", "AwakenedOne", "Donu", "Deca"
	};

	private static MegaSprite GetSpineController(CreatureAnimator instance)
	{
		return Traverse.Create((object)instance).Field("_spineController").GetValue<MegaSprite>();
	}

	private static bool IsModdedEnemy(MegaSprite spineController)
	{
		GodotObject obj = ((spineController != null) ? ((MegaSpineBinding)spineController).BoundObject : null);
		GodotObject obj2 = ((obj is Node) ? obj : null);
		Node val = ((obj2 != null) ? ((Node)obj2).GetParent() : null);
		string item = ((val != null) ? ((object)val.Name).ToString() : null) ?? "";
		return ModdedEnemies.Contains(item);
	}

	[HarmonyPatch(typeof(CreatureAnimator), "SetNextState")]
	[HarmonyPrefix]
	public static bool SetNextStatePrefix(CreatureAnimator __instance, AnimState state)
	{
		MegaSprite spineController = GetSpineController(__instance);
		if (!IsModdedEnemy(spineController))
		{
			return true;
		}
		Traverse.Create((object)__instance).Field("_currentState").SetValue((object)state);
		if (!spineController.HasAnimation(state.Id))
		{
			return false;
		}
		return true;
	}

	[HarmonyPatch(typeof(CreatureAnimator), "AddNextState")]
	[HarmonyPrefix]
	public static bool AddNextStatePrefix(CreatureAnimator __instance, AnimState state)
	{
		MegaSprite spineController = GetSpineController(__instance);
		if (!IsModdedEnemy(spineController))
		{
			return true;
		}
		if (!spineController.HasAnimation(state.Id))
		{
			return false;
		}
		return true;
	}
}
