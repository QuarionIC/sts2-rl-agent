using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Cards;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch]
public class PurgePatch
{
	[HarmonyPatch(typeof(CardModel))]
	private static class OldPurgePatch
	{
		private static MethodInfo? TargetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeForCardPlay", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileType", (Type[])null, (Type[])null);

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (TargetMethod != null)
			{
				yield return TargetMethod;
			}
		}

		private static bool Prepare()
		{
			if (TargetMethod != null)
			{
				return true;
			}
			BaseLibMain.Logger.Info("No valid target found, skipping old PurgePatch", 1);
			return false;
		}

		[HarmonyPrefix]
		private static bool GoAwayForever(CardModel __instance, ref PileType __result)
		{
			if (ShouldPurge(__instance))
			{
				__result = (PileType)0;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(CardModel))]
	private static class BetaPurgePatch
	{
		private static MethodInfo? TargetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeAndPositionForCardPlay", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultLocationForCardPlay", (Type[])null, (Type[])null);

		private static IEnumerable<MethodBase> TargetMethods()
		{
			if (TargetMethod != null)
			{
				yield return TargetMethod;
			}
		}

		private static bool Prepare()
		{
			if (TargetMethod != null)
			{
				return true;
			}
			BaseLibMain.Logger.Info("No valid target found, skipping beta PurgePatch", 1);
			return false;
		}

		[HarmonyTranspiler]
		private static List<CodeInstruction> GoAwayForever(IEnumerable<CodeInstruction> code)
		{
			return new InstructionPatcher(code).Match(new CallMatcher(AccessToolsExtensions.PropertyGetter(typeof(CardModel), "IsDupe"))).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
			{
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.Call(typeof(BetaPurgePatch), "AlterResult", (Type[])null, (Type[])null)
			}));
		}

		private static bool AlterResult(bool origIsDupe, CardModel card)
		{
			if (!origIsDupe)
			{
				return ShouldPurge(card);
			}
			return true;
		}
	}

	public static bool ShouldPurge(CardModel c)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return c.Keywords.Contains(BaseLibKeywords.Purge);
	}
}
