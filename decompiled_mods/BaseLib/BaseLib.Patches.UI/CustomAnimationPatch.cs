using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.UI;

[HarmonyPatch]
internal static class CustomAnimationPatch
{
	[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
	[HarmonyPostfix]
	private static void AdjustTime(NCreature __instance, ref float __result)
	{
		Player player = __instance.Entity.Player;
		if (((player != null) ? player.Character : null) is CustomCharacterModel customCharacterModel && CustomAnimation.HasCustomAnimation((Node)(object)__instance))
		{
			__result = Math.Min(customCharacterModel.DeathAnimTime, 5f);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> CustomAnimDie(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		return AsyncMethodCall.Create(generator, instructions, original, AccessTools.Method(typeof(CustomAnimationPatch), "WaitCustomAnim", (Type[])null, (Type[])null), original);
	}

	private static async Task WaitCustomAnim(NCreature __instance, CancellationToken cancelToken)
	{
		if (CustomAnimation.PlayCustomAnimation((Node)(object)__instance, "Dead", "Die", "die"))
		{
			Player player = __instance.Entity.Player;
			if (((player != null) ? player.Character : null) is CustomCharacterModel customCharacterModel)
			{
				await Cmd.Wait(Math.Min(customCharacterModel.DeathAnimTime, 5f), cancelToken, true);
			}
		}
	}

	[HarmonyPatch(typeof(NCreature), "AnimTempRevive")]
	[HarmonyPrefix]
	private static bool UseCustomReviveAnim(NCreature __instance)
	{
		if (__instance.HasSpineAnimation)
		{
			return true;
		}
		return !CustomAnimation.PlayCustomAnimation((Node)(object)__instance, "revive", "Revive");
	}

	[HarmonyPatch(typeof(NCreature), "SetAnimationTrigger")]
	[HarmonyPrefix]
	private static bool SendTriggerToOtherAnimators(NCreature __instance, string trigger)
	{
		if (__instance.HasSpineAnimation)
		{
			return true;
		}
		BaseLibMain.Logger.Debug("SetAnimationTrigger called for " + trigger + " on creature without spine animation", 1);
		string[] array = ((trigger == "Hit") ? new string[4]
		{
			trigger,
			"Hurt",
			trigger.ToLowerInvariant(),
			"hurt"
		} : ((!(trigger == "Dead")) ? new string[2]
		{
			trigger,
			trigger.ToLowerInvariant()
		} : new string[4]
		{
			trigger,
			"Die",
			trigger.ToLowerInvariant(),
			"die"
		}));
		string[] tryAnimNames = array;
		return !CustomAnimation.PlayCustomAnimation((Node)(object)__instance, tryAnimNames);
	}
}
