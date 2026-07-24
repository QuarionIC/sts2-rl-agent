using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch]
internal static class CustomResourcePatches
{
	internal static readonly List<ResourceHandler> RegisteredResources = new List<ResourceHandler>();

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static void Setup(PlayerCombatState __instance)
	{
		BaseLibMain.Logger.Debug($"Initializing custom resources ({RegisteredResources.Count}) at start of combat", 1);
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.Prep(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerCombatState), "AfterCombatEnd")]
	[HarmonyPostfix]
	private static void Cleanup(PlayerCombatState __instance)
	{
		BaseLibMain.Logger.Debug($"Cleaning up custom resources ({RegisteredResources.Count}) at end of combat", 1);
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.Cleanup(__instance);
		}
	}

	[HarmonyPatch(typeof(PlayerCombatState), "HasEnoughResourcesFor")]
	[HarmonyPostfix]
	private static void CheckAdditionalCosts(PlayerCombatState __instance, CardModel card, ref bool __result, ref UnplayableReason reason)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Expected I4, but got Unknown
		if (!__result)
		{
			return;
		}
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			UnplayableReason val = registeredResource.ResourceCheck(__instance, card);
			if ((int)val != 0)
			{
				reason = (UnplayableReason)(int)val;
				__result = false;
				break;
			}
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> AddSpendAdditionalCosts(ILGenerator generator, IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		return AsyncMethodCall.Create(generator, instructions, original, AccessTools.Method(typeof(CustomResourcePatches), "SpendAdditionalCosts", (Type[])null, (Type[])null), null, original);
	}

	private static async Task SpendAdditionalCosts(CardModel __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			await registeredResource.Spend(__instance);
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static void RecordAdditionalCosts(CardPlay __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.RecordSpend(__instance);
		}
	}

	[HarmonyPatch(typeof(CardEnergyCost), "AfterCardPlayedCleanup")]
	[HarmonyPostfix]
	private static void CleanupAdditionalCosts(CardEnergyCost __instance)
	{
		CardModel card = __instance._card;
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.AfterCardPlayedCleanup(card);
		}
	}

	[HarmonyPatch(typeof(CardModel), "EndOfTurnCleanup")]
	[HarmonyPostfix]
	private static void CleanupEndOfTurn(CardModel __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.EndOfTurnCleanup(__instance);
		}
	}

	[HarmonyPatch(typeof(CardModel), "SetToFreeThisCombat")]
	[HarmonyPostfix]
	private static void SetToFreeThisCombat(CardModel __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.SetToFreeThisCombat(__instance);
		}
	}

	[HarmonyPatch(typeof(CardModel), "SetToFreeThisTurn")]
	[HarmonyPostfix]
	private static void SetToFreeThisTurn(CardModel __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.SetToFreeThisTurn(__instance);
		}
	}

	[HarmonyPatch(typeof(CardModel), "FinalizeUpgradeInternal")]
	[HarmonyPostfix]
	private static void FinalizeAdditionalResourceUpgrades(CardModel __instance)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.FinalizeUpgrade(__instance);
		}
	}

	[HarmonyPatch(typeof(CardModel), "DowngradeInternal")]
	[HarmonyTranspiler]
	private static List<CodeInstruction> DowngradeAdditionalResourcesTranspiler(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new CallMatcher(AccessToolsExtensions.Method(typeof(CardEnergyCost), "ResetForDowngrade", (Type[])null, (Type[])null))).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(CustomResourcePatches), "DowngradeAdditionalResources", (Type[])null, (Type[])null)
		}));
	}

	private static void DowngradeAdditionalResources(CardModel card)
	{
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			registeredResource.ResetForDowngrade(card);
		}
	}

	[HarmonyPatch(typeof(CardModel), "CostsEnergyOrStars")]
	[HarmonyPostfix]
	private static void OrAnotherResource(CardModel __instance, ref bool __result, bool includeGlobalModifiers)
	{
		if (__result)
		{
			return;
		}
		foreach (ResourceHandler registeredResource in RegisteredResources)
		{
			if (registeredResource.CostsMoreThanZero(__instance, includeGlobalModifiers))
			{
				__result = true;
			}
		}
	}
}
