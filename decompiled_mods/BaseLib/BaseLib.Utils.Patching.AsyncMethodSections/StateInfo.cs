using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Extensions;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class StateInfo
{
	public int Index { get; }

	public List<CodeInstruction> Code { get; private set; }

	public MethodInfo? StateMethod { get; }

	public StateInfo(int index, List<CodeInstruction> code, FieldInfo stateField)
	{
		Index = index;
		Code = code;
		StateMethod = AnalyzeCode(stateField, Index, Code);
	}

	public StateInfo(int index, List<CodeInstruction> code, MethodInfo stateMethod)
	{
		Index = index;
		Code = code;
		StateMethod = stateMethod;
	}

	private static MethodInfo? AnalyzeCode(FieldInfo stateField, int stateIndex, List<CodeInstruction> code)
	{
		InstructionMatcher instructionMatcher = new InstructionMatcher().stfld(stateField);
		InstructionPatcher instructionPatcher = new InstructionPatcher(code);
		bool[] matched = new bool[1] { true };
		instructionPatcher.Match(delegate
		{
			matched[0] = false;
		}, AsyncMethodCall.StateAwaitMatcher);
		if (!matched[0])
		{
			BaseLibMain.Logger.Info("CODE:\n" + GeneralExtensions.Join<CodeInstruction>((IEnumerable<CodeInstruction>)code, (Func<CodeInstruction, string>)((CodeInstruction instruction) => ((object)instruction).ToString()), "\n"), 1);
			throw new InvalidOperationException($"Failed to find state awaiter for state {stateIndex}");
		}
		instructionPatcher.Step(-2).GetOperand(out object operand).Step(3)
			.Match(instructionMatcher);
		return operand as MethodInfo;
	}

	public void AddSaveState(FieldInfo stateField, int stringDictLocal)
	{
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		Code = new InstructionPatcher(Code).Match(AsyncMethodCall.StateAwaitMatcher).Match(new InstructionMatcher().dup().stloc_0().stfld(stateField)).Step(-3)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[4]
			{
				new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.StoreStateInDictMethod),
				new CodeInstruction(OpCodes.Dup, (object)null),
				CodeInstruction.LoadLocal(stringDictLocal, false),
				new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.StoreDictionaryForStateMethod)
			}));
	}

	public void AddResumeLog()
	{
		Code = new InstructionPatcher(Code).Match(new InstructionMatcher().initobj()).Insert($"Resuming state {Index}".MakeWriteLog());
	}
}
