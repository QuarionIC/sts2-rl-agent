using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch]
internal static class CardPileCmd_Add_MaxHandSizePatch
{
	private static MethodInfo TargetMethod()
	{
		return AccessTools.AsyncMoveNext((MethodBase)(AccessTools.Method(typeof(CardPileCmd), "Add", new Type[6]
		{
			typeof(IEnumerable<CardModel>),
			typeof(CardPile),
			typeof(CardPilePosition),
			typeof(AbstractModel),
			typeof(bool),
			typeof(bool)
		}, (Type[])null) ?? AccessTools.Method(typeof(CardPileCmd), "Add", new Type[5]
		{
			typeof(IEnumerable<CardModel>),
			typeof(CardPile),
			typeof(CardPilePosition),
			typeof(AbstractModel),
			typeof(bool)
		}, (Type[])null)));
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
	{
		List<CodeInstruction> list = instructions.ToList();
		new InstructionPatcher(list).Match(new InstructionMatcher().ldarg_0().ldfld(null).PredicateMatch((object? op) => op is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(Player))).CopyMatch(out List<CodeInstruction> loadPlayer);
		foreach (CodeInstruction ins in list)
		{
			if (MaxHandSizePatch.IsDefaultMaxHandSizeConst(ins) || MaxHandSizePatch.IsBetaMaxHandSize(ins))
			{
				foreach (CodeInstruction item in loadPlayer.Select((CodeInstruction ci) => ci.Clone()))
				{
					yield return item;
				}
				yield return ins;
				yield return new CodeInstruction(OpCodes.Call, (object)MaxHandSizePatch.GetMaxHandSizeFromBaseMethod);
			}
			else
			{
				yield return ins;
			}
		}
	}
}
