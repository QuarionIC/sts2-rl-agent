using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(NCard), "FindOnTable")]
internal class GetNCardPile
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> CheckCustomPiles(IEnumerable<CodeInstruction> instructions)
	{
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		List<Label> labels;
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldloc_1().ret()).Step(-2).GetLabels(out labels)
			.ResetPosition()
			.Match(new InstructionMatcher().stloc_3().ldloc_3())
			.Step(-1)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[7]
			{
				CodeInstruction.LoadLocal(3, false),
				CodeInstruction.Call(typeof(CustomPiles), "IsCustomPile", (Type[])null, (Type[])null),
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.LoadLocal(3, false),
				CodeInstruction.Call(typeof(CustomPiles), "FindOnTable", (Type[])null, (Type[])null),
				CodeInstruction.StoreLocal(1),
				new CodeInstruction(OpCodes.Brtrue_S, (object)labels[0])
			}));
	}
}
