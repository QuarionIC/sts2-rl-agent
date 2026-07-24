using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class FromChooseACardScreenPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CardSelectCmd).GetNestedTypes(AccessTools.all).First((Type t) => t.Name.Contains("FromChooseACardScreen")), "MoveNext", (Type[])null, (Type[])null);
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Expected O, but got Unknown
		return InstructionPatcher.op_Implicit(new InstructionPatcher(instructions).Match((IMatcher[])(object)new IMatcher[1] { (IMatcher)new InstructionMatcher().opcode(OpCodes.Ldstr).opcode(OpCodes.Ldstr).opcode(OpCodes.Newobj)
			.opcode(OpCodes.Throw) }).ReplaceLastMatch((IEnumerable<CodeInstruction>)new global::_003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[4]
		{
			new CodeInstruction(OpCodes.Nop, (object)null),
			new CodeInstruction(OpCodes.Nop, (object)null),
			new CodeInstruction(OpCodes.Nop, (object)null),
			new CodeInstruction(OpCodes.Nop, (object)null)
		})));
	}
}
