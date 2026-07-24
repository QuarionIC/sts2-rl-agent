using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch(typeof(NPlayerHand), "StartCardPlay")]
internal static class NPlayerHandStartCardPlayShortcutSafePatch
{
	private static readonly FieldInfo _selectCardShortcutsField = AccessTools.Field(typeof(NPlayerHand), "_selectCardShortcuts");

	private static readonly FieldInfo _draggedHolderIndexField = AccessTools.Field(typeof(NPlayerHand), "_draggedHolderIndex");

	private static readonly MethodInfo _getShortcutOrDefault = AccessTools.Method(typeof(NPlayerHandStartCardPlayShortcutSafePatch), "GetShortcutOrDefault", (Type[])null, (Type[])null);

	private static StringName GetShortcutOrDefault(NPlayerHand hand, int idx)
	{
		StringName[] selectCardShortcuts = hand._selectCardShortcuts;
		if (idx < 0 || idx >= selectCardShortcuts.Length)
		{
			return MegaInput.releaseCard;
		}
		return selectCardShortcuts[idx];
	}

	[HarmonyTranspiler]
	private static List<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
	{
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Expected O, but got Unknown
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldarg_0().opcode(OpCodes.Ldfld).PredicateMatch((object? op) => object.Equals(op, _selectCardShortcutsField))
			.ldarg_0()
			.opcode(OpCodes.Ldfld)
			.PredicateMatch((object? op) => object.Equals(op, _draggedHolderIndexField))
			.opcode(OpCodes.Ldelem_Ref)).ReplaceLastMatch(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[4]
		{
			new CodeInstruction(OpCodes.Ldarg_0, (object)null),
			new CodeInstruction(OpCodes.Ldarg_0, (object)null),
			new CodeInstruction(OpCodes.Ldfld, (object)_draggedHolderIndexField),
			new CodeInstruction(OpCodes.Call, (object)_getShortcutOrDefault)
		}));
	}
}
