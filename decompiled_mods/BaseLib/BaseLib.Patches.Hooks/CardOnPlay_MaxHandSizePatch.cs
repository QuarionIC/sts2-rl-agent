using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch]
internal static class CardOnPlay_MaxHandSizePatch
{
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.AsyncMoveNext((MethodBase)AccessTools.Method(typeof(Scrawl), "OnPlay", new Type[2]
		{
			typeof(PlayerChoiceContext),
			typeof(CardPlay)
		}, (Type[])null));
		yield return AccessTools.AsyncMoveNext((MethodBase)AccessTools.Method(typeof(Dredge), "OnPlay", new Type[2]
		{
			typeof(PlayerChoiceContext),
			typeof(CardPlay)
		}, (Type[])null));
		yield return AccessTools.AsyncMoveNext((MethodBase)AccessTools.Method(typeof(CrashLanding), "OnPlay", new Type[2]
		{
			typeof(PlayerChoiceContext),
			typeof(CardPlay)
		}, (Type[])null));
		yield return AccessTools.AsyncMoveNext((MethodBase)AccessTools.Method(typeof(Pillage), "OnPlay", new Type[2]
		{
			typeof(PlayerChoiceContext),
			typeof(CardPlay)
		}, (Type[])null));
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
	{
		List<CodeInstruction> list = instructions.ToList();
		new InstructionPatcher(list).Match(new InstructionMatcher().ldarg_0().ldfld(null).PredicateMatch((object? op) => op is FieldInfo fieldInfo && typeof(CardModel).IsAssignableFrom(fieldInfo.FieldType))).CopyMatch(out List<CodeInstruction> loadCard);
		foreach (CodeInstruction ins in list)
		{
			if (MaxHandSizePatch.IsDefaultMaxHandSizeConst(ins) || MaxHandSizePatch.IsBetaMaxHandSize(ins))
			{
				foreach (CodeInstruction item in loadCard.Select((CodeInstruction ci) => ci.Clone()))
				{
					yield return item;
				}
				yield return ins;
				yield return new CodeInstruction(OpCodes.Call, (object)AccessTools.Method(typeof(MaxHandSizePatch), "GetMaxHandSizeFromCard", (Type[])null, (Type[])null));
			}
			else
			{
				yield return ins;
			}
		}
	}
}
