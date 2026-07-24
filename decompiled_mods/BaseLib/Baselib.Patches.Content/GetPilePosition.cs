using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace BaseLib.Patches.Content;

[HarmonyPatch(typeof(PileTypeExtensions), "GetTargetPosition")]
internal class GetPilePosition
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> CustomPilePosition(IEnumerable<CodeInstruction> instructions)
	{
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		List<Label> labels;
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldloc_2().ret()).Step(-2).GetLabels(out labels)
			.ResetPosition()
			.Match(new InstructionMatcher().call(AccessTools.PropertyGetter(typeof(Rect2), "Size")).stloc_0().ldarg_0())
			.Step(-1)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[8]
			{
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.Call((Expression<Action>)(() => CustomPiles.IsCustomPile((PileType)0))),
				CodeInstruction.LoadArgument(0, false),
				CodeInstruction.LoadArgument(1, false),
				CodeInstruction.LoadLocal(0, false),
				CodeInstruction.Call((Expression<Action>)(() => CustomPiles.GetPosition((PileType)0, null, default(Vector2)))),
				CodeInstruction.StoreLocal(2),
				new CodeInstruction(OpCodes.Brtrue_S, (object)labels[0])
			}));
	}
}
