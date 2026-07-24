using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class LoadStateSection : IAsyncMethodSection
{
	public required List<CodeInstruction> Code { get; init; }

	public required bool AddStateLoading { get; init; }

	public required int StringDictLocal { get; init; }

	public required int StateKeyLocal { get; init; }

	public IEnumerable<StateInfo> AllStates => Array.Empty<StateInfo>();

	public static LoadStateSection Read(AsyncMethodContext context, IEnumerator<CodeInstruction> codeEnumerator)
	{
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Expected O, but got Unknown
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Expected O, but got Unknown
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Expected O, but got Unknown
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected O, but got Unknown
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected O, but got Unknown
		bool flag = false;
		bool flag2 = false;
		int operand = -1;
		int operand2 = -1;
		List<CodeInstruction> list = new List<CodeInstruction>();
		do
		{
			CodeInstruction current = codeEnumerator.Current;
			if (current.HasBlock((ExceptionBlockType)0))
			{
				break;
			}
			list.Add(current);
			if (CodeInstructionExtensions.LoadsField(current, context.StateField, false))
			{
				flag = true;
			}
			if (CodeInstructionExtensions.Calls(current, AsyncMethodCall.LoadStateFromDictMethod))
			{
				flag2 = true;
			}
		}
		while (codeEnumerator.MoveNext());
		if (!flag)
		{
			throw new ArgumentException($"MoveNext does not load found state field {context.StateField}; failed to set up AsyncMethodCall properly");
		}
		if (!flag2)
		{
			BaseLibMain.Logger.Debug("Setting up external state", 1);
			LocalBuilder localBuilder = context.Generator.DeclareLocal(typeof(int));
			LocalBuilder localBuilder2 = context.Generator.DeclareLocal(typeof(Dictionary<string, object>));
			operand = localBuilder.LocalIndex;
			operand2 = localBuilder2.LocalIndex;
			List<CodeInstruction> list2 = new List<CodeInstruction>();
			list2.Add(CodeInstruction.LoadArgument(0, false));
			list2.Add(CodeInstruction.LoadArgument(0, false));
			list2.Add(new CodeInstruction(OpCodes.Ldfld, (object)context.StateField));
			list2.Add(new CodeInstruction(OpCodes.Dup, (object)null));
			list2.Add(CodeInstruction.StoreLocal(operand));
			list2.Add(new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.LoadStateFromDictMethod));
			list2.Add(new CodeInstruction(OpCodes.Stfld, (object)context.StateField));
			list2.Add(CodeInstruction.LoadLocal(operand, false));
			list2.Add(new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.LoadDictionaryForStateMethod));
			list2.Add(CodeInstruction.StoreLocal(operand2));
			list2.AddRange(list);
			list = list2;
		}
		else
		{
			BaseLibMain.Logger.Debug("Checking for external state loading", 1);
			new InstructionPatcher(list).TryMatch(new InstructionMatcher().ldfld(context.StateField).dup().stloc_any())?.Step(-1).GetIndexOperand(out operand).TryMatch(new InstructionMatcher().call(AsyncMethodCall.LoadDictionaryForStateMethod).stloc_any())?.Step(-1).GetIndexOperand(out operand2);
		}
		if (operand == -1)
		{
			throw new ArgumentException("Failed to find local used to hold temporary state key.");
		}
		if (operand2 == -1)
		{
			throw new ArgumentException("Failed to find local used to hold extra saved values.");
		}
		return new LoadStateSection
		{
			Code = list,
			AddStateLoading = !flag2,
			StateKeyLocal = operand,
			StringDictLocal = operand2
		};
	}
}
