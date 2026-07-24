using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Patches.Creatures;

public class AnimationPatches
{
	[HarmonyPatch(typeof(CreatureCmd), "TriggerAnim")]
	public class StaggerAnimationPatch
	{
		[HarmonyPatch(typeof(Hook), "AfterRoomEntered")]
		public class StaggerCleanupPatch
		{
			public static void Postfix()
			{
				StaggerAnimation.Reset();
			}
		}

		[HarmonyPatch(typeof(NCreature), "GetCurrentAnimationTimeRemaining")]
		public static class DeathAnimTimePatch
		{
			private static readonly HashSet<NCreature> _dyingCreatures = new HashSet<NCreature>();

			public static void MarkAsDying(NCreature creature)
			{
				_dyingCreatures.Add(creature);
			}

			public static bool Prefix(NCreature __instance, ref float __result)
			{
				if (__instance.Entity.Monster == null)
				{
					return true;
				}
				if (!_dyingCreatures.Contains(__instance))
				{
					return true;
				}
				_dyingCreatures.Remove(__instance);
				__result = 0f;
				return false;
			}
		}

		[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
		public static class DeathAnimStartPatch
		{
			public static void Prefix(NCreature __instance, ref bool shouldRemove)
			{
				MonsterModel monster = __instance.Entity.Monster;
				if (1 == 0)
				{
				}
				bool flag = monster is AcidSlimeLarge || monster is AcidSlimeMedium || monster is AcidSlimeSmall || monster is Cultist || monster is FungiBeast || monster is GremlinFat || monster is GremlinMad || monster is GremlinShield || monster is GremlinSneaky || monster is GremlinWizard || monster is JawWorm || monster is Looter || monster is LouseGreen || monster is LouseRed || monster is SlaverBlue || monster is SlaverRed || monster is SpikeSlimeLarge || monster is SpikeSlimeMedium || monster is SpikeSlimeSmall || monster is GremlinNob || monster is Lagavulin || monster is Sentry || monster is Guardian || (!(monster is Hexaghost) && (monster is SlimeBoss || monster is Byrd || monster is Centurion || monster is Mugger || monster is Mystic || monster is Chosen || monster is ShelledParasite || monster is SnakePlant || monster is SphericGuardian || monster is Pointy || monster is Romeo || monster is Bear || monster is Taskmaster || monster is BookOfStabbing || monster is GremlinLeader || monster is TorchHead || monster is Collector || monster is Champ || monster is BronzeAutomaton || (!(monster is BronzeOrb) && (monster is Darkling || monster is Exploder || monster is Maw || monster is OrbWalker || monster is Repulsor || monster is Spiker || monster is SpireGrowth || monster is Transient || monster is WrithingMass || monster is GiantHead || monster is Nemesis || monster is Reptomancer || monster is SnakeDagger || monster is AwakenedOne || monster is Donu || monster is Deca))));
				if (1 == 0)
				{
				}
				if (!flag)
				{
					return;
				}
				DeathAnimTimePatch.MarkAsDying(__instance);
				bool flag2 = !shouldRemove;
				bool flag3 = flag2;
				if (flag3)
				{
					MonsterModel monster2 = __instance.Entity.Monster;
					flag = ((monster2 is BookOfStabbing || monster2 is Reptomancer) ? true : false);
					flag3 = flag;
				}
				if (flag3)
				{
					shouldRemove = true;
					NCombatRoom instance = NCombatRoom.Instance;
					if (instance != null)
					{
						instance.RemoveCreatureNode(__instance);
					}
				}
			}
		}

		public static bool Prefix(Creature creature, string triggerName, float waitTime, ref Task __result)
		{
			if (((creature != null) ? creature.Monster : null) is Lagavulin { IsAwake: not false })
			{
				if (1 == 0)
				{
				}
				float num = triggerName switch
				{
					"Hit" => 0.25f, 
					"Attack" => 0.25f, 
					"Debuff" => 0.25f, 
					_ => 0f, 
				};
				if (1 == 0)
				{
				}
				float num2 = num;
				if (num2 > 0f)
				{
					__result = PlayHitWithMix(creature, triggerName, "Idle_2", num2);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is AwakenedOne awakenedOne)
			{
				if (awakenedOne.Respawns >= 1 && triggerName == "Hit")
				{
					__result = PlayHitWithMix(creature, "Hit", "Idle_2", 0.2f);
					return false;
				}
				if (awakenedOne.Respawns == 0)
				{
					if (1 == 0)
					{
					}
					float num = ((triggerName == "Hit") ? 0.3f : ((!(triggerName == "Attack_1")) ? 0f : 0.2f));
					if (1 == 0)
					{
					}
					float num3 = num;
					if (num3 > 0f)
					{
						__result = PlayHitWithMix(creature, triggerName, "Idle_1", num3);
						return false;
					}
				}
			}
			if (((creature != null) ? creature.Monster : null) is ShelledParasite && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is SphericGuardian)
			{
				if (triggerName == "Hit")
				{
					__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
					return false;
				}
				if (triggerName == "Slam")
				{
					__result = PlayAnimWithMixBothWays(creature, "Attack", "Idle", 0.1f);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Chosen)
			{
				if (triggerName == "Hit")
				{
					__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
					return false;
				}
				if (triggerName == "Hex")
				{
					__result = PlayHitWithMix(creature, "Attack", "Idle", 0.2f);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Sentry)
			{
				if (1 == 0)
				{
				}
				(string, float) tuple = triggerName switch
				{
					"Attack" => ("attack", 0.1f), 
					"spaz1" => ("spaz1", 0.1f), 
					"spaz2" => ("spaz2", 0.1f), 
					"spaz3" => ("spaz3", 0.1f), 
					"Hit" => ("hit", 0.1f), 
					_ => (null, 0f), 
				};
				if (1 == 0)
				{
				}
				var (text, num4) = tuple;
				if (text != null)
				{
					__result = PlayAnimWithMixBothWays(creature, text, "idle", 0.1f);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Bear && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Romeo && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Pointy && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is BookOfStabbing && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Centurion && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Mystic && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is GremlinLeader && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is SnakePlant && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Snecko && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is Deca)
			{
				if (1 == 0)
				{
				}
				float num = ((triggerName == "Hit") ? 0.1f : ((!(triggerName == "Attack_2")) ? 0f : 0.1f));
				if (1 == 0)
				{
				}
				float num5 = num;
				if (num5 > 0f)
				{
					__result = PlayHitWithMix(creature, triggerName, "Idle", num5);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Donu)
			{
				if (1 == 0)
				{
				}
				float num = ((triggerName == "Hit") ? 0.1f : ((!(triggerName == "Attack_2")) ? 0f : 0.1f));
				if (1 == 0)
				{
				}
				float num6 = num;
				if (num6 > 0f)
				{
					__result = PlayHitWithMix(creature, triggerName, "Idle", num6);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Nemesis)
			{
				if (triggerName == "Hit")
				{
					__result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
					return false;
				}
				if (triggerName == "Slash")
				{
					__result = PlayAnimWithMixBothWays(creature, "Attack", "Idle", 0.1f);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is Reptomancer)
			{
				if (1 == 0)
				{
				}
				string text2 = triggerName switch
				{
					"Strike" => "Attack", 
					"Summon" => "Sumon", 
					"Hit" => "Hurt", 
					_ => null, 
				};
				if (1 == 0)
				{
				}
				string text3 = text2;
				if (text3 != null)
				{
					__result = PlayAnimWithMixBothWays(creature, text3, "Idle", 0.1f);
					return false;
				}
			}
			if (((creature != null) ? creature.Monster : null) is SpireGrowth && triggerName == "Hurt")
			{
				__result = PlayAnimWithMixBothWays(creature, triggerName, "Idle", 0.2f);
				return false;
			}
			if (((creature != null) ? creature.Monster : null) is TimeEater && triggerName == "Hit")
			{
				__result = PlayHitWithMix(creature, "Hit", "Idle", 0.1f);
				return false;
			}
			if (triggerName != "Hit")
			{
				return true;
			}
			MonsterModel val = ((creature != null) ? creature.Monster : null);
			if (1 == 0)
			{
			}
			bool flag = !(val is AcidSlimeLarge) && !(val is AcidSlimeMedium) && !(val is AcidSlimeSmall) && (val is Cultist || (!(val is FungiBeast) && (val is GremlinFat || val is GremlinMad || val is GremlinShield || val is GremlinSneaky || val is GremlinWizard || val is JawWorm || val is Looter || val is LouseGreen || val is LouseRed || val is SlaverBlue || val is SlaverRed || (!(val is SpikeSlimeLarge) && !(val is SpikeSlimeMedium) && !(val is SpikeSlimeSmall) && (val is GremlinNob || (!(val is Lagavulin) && !(val is Sentry) && (val is Guardian || (!(val is Hexaghost) && !(val is SlimeBoss) && (val is Byrd || val is Chosen || (!(val is Centurion) && (val is Mugger || (!(val is Mystic) && !(val is ShelledParasite) && !(val is SnakePlant) && !(val is SphericGuardian) && !(val is Pointy) && !(val is Romeo) && !(val is Bear) && (val is Taskmaster || (!(val is BookOfStabbing) && !(val is GremlinLeader) && (val is TorchHead || val is Collector || (!(val is Champ) && (val is BronzeAutomaton || val is BronzeOrb || (!(val is Darkling) && (val is Exploder || val is Maw || val is Repulsor || (!(val is Spiker) && !(val is SpireGrowth) && !(val is Transient) && !(val is OrbWalker) && !(val is WrithingMass) && (val is GiantHead || (!(val is Nemesis) && !(val is Reptomancer) && !(val is SnakeDagger) && !(val is AwakenedOne) && !(val is Donu) && !(val is Deca) && false))))))))))))))))))))));
			if (1 == 0)
			{
			}
			if (!flag)
			{
				return true;
			}
			StaggerAnimation.Play(creature);
			__result = Task.CompletedTask;
			return false;
		}

		private static async Task PlayHitWithMix(Creature creature, string hitAnim, string idleAnim, float mixDuration)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
			MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
			if (spineBody == null)
			{
				return;
			}
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation(hitAnim, false, 0);
			MegaTrackEntry queued = animState.AddAnimationTracked(idleAnim, 0f, true, 0);
			try
			{
				if (queued != null)
				{
					queued.SetMixDuration(mixDuration);
				}
			}
			finally
			{
				((IDisposable)queued)?.Dispose();
			}
		}

		private static async Task PlayAnimWithMix(Creature creature, string animName, float mixDuration)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
			MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
			if (spineBody == null)
			{
				return;
			}
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation(animName, false, 0);
			MegaTrackEntry entry = animState.GetCurrent(0);
			try
			{
				if (entry != null)
				{
					entry.SetMixDuration(mixDuration);
				}
			}
			finally
			{
				((IDisposable)entry)?.Dispose();
			}
		}

		private static async Task PlayAnimWithMixBothWays(Creature creature, string animName, string idleAnim, float mixDuration, float exitDelay = 0f)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(creature) : null);
			MegaSprite spineBody = ((creatureNode != null) ? creatureNode.Visuals.SpineBody : null);
			if (spineBody == null)
			{
				return;
			}
			MegaAnimationState animState = spineBody.GetAnimationState();
			animState.SetAnimation(animName, false, 0);
			MegaTrackEntry entry = animState.GetCurrent(0);
			try
			{
				if (entry != null)
				{
					entry.SetMixDuration(mixDuration);
				}
				MegaTrackEntry queued = animState.AddAnimationTracked(idleAnim, exitDelay, true, 0);
				try
				{
					if (queued != null)
					{
						queued.SetMixDuration(mixDuration);
					}
				}
				finally
				{
					((IDisposable)queued)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)entry)?.Dispose();
			}
		}
	}
}
