using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActsFromThePast.Acts.TheCity;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Creatures;

public class VisualsPatches
{
	[HarmonyPatch(typeof(NCreatureVisuals), "SetScaleAndHue")]
	public class SetScaleAndHuePatch
	{
		private const string LOG_TAG = "[ActsFromThePast]";

		private static readonly HashSet<string> _moddedCreatureTypes = new HashSet<string>
		{
			"AcidSlimeLarge", "AcidSlimeMedium", "AcidSlimeSmall", "Cultist", "FungiBeast", "GremlinFat", "GremlinMad", "GremlinShield", "GremlinSneaky", "GremlinWizard",
			"JawWorm", "Looter", "LouseGreen", "LouseRed", "SlaverBlue", "SlaverRed", "SpikeSlimeLarge", "SpikeSlimeMedium", "SpikeSlimeSmall", "GremlinNob",
			"Lagavulin", "Sentry", "Guardian", "Hexaghost", "SlimeBoss", "Byrd", "Centurion", "Mugger", "Mystic", "Chosen",
			"ShelledParasite", "SnakePlant", "SphericGuardian", "Pointy", "Romeo", "Bear", "Taskmaster", "BookOfStabbing", "GremlinLeader", "TorchHead",
			"Collector", "Champ", "BronzeAutomaton", "BronzeOrb", "Darkling", "Exploder", "Maw", "OrbWalker", "Repulsor", "Spiker",
			"SpireGrowth", "Transient", "WrithingMass", "GiantHead", "Nemesis", "Reptomancer", "SnakeDagger", "AwakenedOne", "Donu", "Deca"
		};

		public static bool Prefix(NCreatureVisuals __instance, float scale, float hue)
		{
			StringName name = ((Node)__instance).Name;
			if (_moddedCreatureTypes.Contains(StringName.op_Implicit(name)))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(NCombatRoom), "PositionEnemies")]
	public static class CreaturePositionPatch
	{
		public static void Postfix(List<NCreature> creatures)
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			foreach (NCreature creature in creatures)
			{
				MonsterModel monster = creature.Entity.Monster;
				if (1 == 0)
				{
				}
				float num = 0f;
				if (1 == 0)
				{
				}
				float num2 = num;
				if (num2 != 0f)
				{
					((Control)creature).Position = ((Control)creature).Position + new Vector2(0f, num2);
				}
			}
		}
	}

	[HarmonyPatch(typeof(PowerCmd), "Remove", new Type[] { typeof(PowerModel) })]
	public class CurlUpLousePatch
	{
		public static void Prefix(PowerModel? power)
		{
			if (!(power is CurlUpPower))
			{
				return;
			}
			Creature owner = power.Owner;
			if (((owner != null) ? owner.Monster : null) is LouseRed louseRed)
			{
				louseRed.IsOpen = false;
				return;
			}
			Creature owner2 = power.Owner;
			if (((owner2 != null) ? owner2.Monster : null) is LouseGreen louseGreen)
			{
				louseGreen.IsOpen = false;
			}
		}
	}

	[HarmonyPatch(typeof(NCreature), "_Ready")]
	public static class CreatureVisualsLayerPatch
	{
		private static readonly PropertyInfo StateProperty = typeof(RunManager).GetProperty("State", BindingFlags.Instance | BindingFlags.NonPublic);

		private static bool IsCityAct()
		{
			object? obj = StateProperty?.GetValue(RunManager.Instance);
			RunState val = (RunState)((obj is RunState) ? obj : null);
			return ((val != null) ? val.Act : null) is TheCityAct;
		}

		public static void Postfix(NCreature __instance)
		{
			if (IsCityAct())
			{
				NCreatureVisuals val = ((IEnumerable)((Node)__instance).GetChildren(false)).OfType<NCreatureVisuals>().FirstOrDefault();
				if (val != null)
				{
					((CanvasItem)val).ZIndex = -5;
				}
			}
		}
	}

	[HarmonyPatch(typeof(NGameOverScreen), "MoveCreaturesToDifferentLayerAndDisableUi")]
	public static class ResetCreatureZOnGameOverPatch
	{
		public static void Postfix(NGameOverScreen __instance, Control ____creatureContainer)
		{
			foreach (NCreatureVisuals item in ((IEnumerable)((Node)____creatureContainer).GetChildren(false)).OfType<NCreatureVisuals>())
			{
				((CanvasItem)item).ZIndex = 0;
			}
		}
	}
}
