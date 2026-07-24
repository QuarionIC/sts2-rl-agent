using System;
using System.Collections.Generic;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace BaseLib.Patches.Content;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
internal class SpecialPileInCombat
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> AddPile(IEnumerable<CodeInstruction> instructions)
	{
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().stfld(AccessTools.Field(typeof(PlayerCombatState), "_piles"))).Step(-1).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(CustomPiles), "AddCustomPiles", (Type[])null, (Type[])null)
		}));
	}
}
