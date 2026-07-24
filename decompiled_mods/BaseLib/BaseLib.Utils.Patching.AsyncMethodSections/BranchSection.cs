using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BaseLib.Extensions;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class BranchSection : IAsyncStateSection, IAsyncMethodSection
{
	public required CodeInstruction ResumeInstruction { get; init; }

	public required StateInfo State { get; init; }

	public required IEnumerable<Label> LeaveLabels { get; init; }

	public List<CodeInstruction> Code => State.Code;

	public IEnumerable<StateInfo> AllStates => new _003C_003Ez__ReadOnlySingleElementList<StateInfo>(State);

	public static IAsyncStateSection Read(AsyncMethodContext context, LoadStateSection loadStateSection, BranchingStateSection branchSource, IEnumerator<CodeInstruction> codeEnumerator)
	{
		List<CodeInstruction> list = new List<CodeInstruction>();
		List<int> list2 = new List<int>();
		CodeInstruction val = null;
		do
		{
			CodeInstruction current = codeEnumerator.Current;
			foreach (Label label in current.labels)
			{
				if (branchSource.LabelStates.TryGetValue(label, out var value))
				{
					BaseLibMain.Logger.Debug($"Found state resume point label {label.Id} for state {value}", 1);
					list2.Add(value);
					val = current;
				}
			}
			if (val != null)
			{
				BaseLibMain.Logger.VeryDebug("End of state prep", 1);
				break;
			}
			list.Add(current);
		}
		while (codeEnumerator.MoveNext());
		if (list2.Count == 0 || val == null)
		{
			throw new Exception("Failed to find state branches.");
		}
		if (list2.Count > 1)
		{
			BaseLibMain.Logger.Debug("Section is destination of multiple states; should be additional branching section.", 1);
			BranchingStateSection branchingStateSection = BranchingStateSection.Read(context, loadStateSection, codeEnumerator);
			branchingStateSection.Prep = list;
			return branchingStateSection;
		}
		int num = list2[0];
		List<CodeInstruction> list3 = list.ToList();
		HashSet<Label> hashSet = new HashSet<Label>();
		bool flag = false;
		do
		{
			CodeInstruction current3 = codeEnumerator.Current;
			if ((current3.opcode == OpCodes.Leave || current3.opcode == OpCodes.Leave_S) && current3.operand is Label item)
			{
				hashSet.Add(item);
			}
			if (current3.opcode == OpCodes.Call && current3.operand is MethodInfo { Name: "GetResult" } methodInfo)
			{
				Type? declaringType = methodInfo.DeclaringType;
				if ((object)declaringType != null && declaringType.Name.StartsWith("TaskAwaiter"))
				{
					flag = true;
					list3.Add(current3);
					continue;
				}
			}
			if (flag)
			{
				if (CodeInstructionExtensions.IsStloc(current3, (LocalBuilder)null) || current3.opcode == OpCodes.Nop || current3.opcode == OpCodes.Pop)
				{
					list3.Add(current3);
					codeEnumerator.MoveNext();
				}
				StateInfo stateInfo = new StateInfo(num, list3, context.StateField);
				BaseLibMain.Logger.Debug($"Generated StateInfo for state {num}", 1);
				if (stateInfo.Index >= context.NextStateIndex)
				{
					context.NextStateIndex = stateInfo.Index + 1;
				}
				BranchSection branchSection = new BranchSection
				{
					State = stateInfo,
					ResumeInstruction = val,
					LeaveLabels = hashSet
				};
				if (loadStateSection.AddStateLoading)
				{
					branchSection.State.AddSaveState(context.StateField, loadStateSection.StringDictLocal);
				}
				return branchSection;
			}
			list3.Add(current3);
		}
		while (codeEnumerator.MoveNext());
		throw new Exception($"Failed to find end of state {num}");
	}

	public static BranchSection Create(AsyncMethodContext context, LoadStateSection loadSection, EndingSection endingSection, MethodInfo callMethod, IEnumerable<StateParamInfo> loadFields, Label resumeLabel, AsyncMethodCall.ResultType resultType, string? resultName)
	{
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0282: Expected O, but got Unknown
		//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ba: Expected O, but got Unknown
		//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Expected O, but got Unknown
		//IL_0305: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Expected O, but got Unknown
		//IL_0313: Unknown result type (might be due to invalid IL or missing references)
		//IL_0319: Expected O, but got Unknown
		//IL_0334: Unknown result type (might be due to invalid IL or missing references)
		//IL_033a: Expected O, but got Unknown
		//IL_0342: Unknown result type (might be due to invalid IL or missing references)
		//IL_0348: Expected O, but got Unknown
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_037f: Expected O, but got Unknown
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f3: Expected O, but got Unknown
		//IL_0428: Unknown result type (might be due to invalid IL or missing references)
		//IL_042e: Expected O, but got Unknown
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Expected O, but got Unknown
		//IL_048a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0490: Expected O, but got Unknown
		//IL_04cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d2: Expected O, but got Unknown
		//IL_04fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0514: Expected O, but got Unknown
		//IL_0601: Unknown result type (might be due to invalid IL or missing references)
		//IL_060b: Expected O, but got Unknown
		//IL_0619: Unknown result type (might be due to invalid IL or missing references)
		//IL_0623: Expected O, but got Unknown
		//IL_062b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0644: Expected O, but got Unknown
		//IL_05a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_05c3: Expected O, but got Unknown
		//IL_06aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_06b4: Expected O, but got Unknown
		//IL_0684: Unknown result type (might be due to invalid IL or missing references)
		//IL_068e: Expected O, but got Unknown
		//IL_06e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_06f3: Expected O, but got Unknown
		ILGenerator generator = context.Generator;
		Type type = typeof(TaskAwaiter);
		Type fieldType = context.BuilderField.FieldType;
		bool isValueType = context.StateMachineType.IsValueType;
		Type type2 = typeof(void);
		if (callMethod.ReturnType.IsGenericType)
		{
			type2 = callMethod.ReturnType.GetGenericArguments()[0];
			BaseLibMain.Logger.Debug($"Method to call has return type; making generic awaiter type [{type2}]", 1);
			type = typeof(TaskAwaiter<>).MakeGenericType(type2);
		}
		MethodInfo method = callMethod.ReturnType.GetMethod("GetAwaiter");
		if (method == null)
		{
			throw new Exception($"Failed to get GetAwaiter for type {callMethod.ReturnType}");
		}
		MethodInfo methodInfo = fieldType.GetMethod("AwaitUnsafeOnCompleted");
		if (methodInfo == null)
		{
			throw new Exception($"Failed to get AwaitUnsafeOnCompleted for type {fieldType}");
		}
		if (methodInfo.IsGenericMethodDefinition)
		{
			methodInfo = methodInfo.MakeGenericMethod(type, context.StateMachineType);
		}
		LocalBuilder localBuilder = generator.DeclareLocal(type);
		MethodInfo methodInfo2 = AccessToolsExtensions.PropertyGetter(type, "IsCompleted");
		Label label = generator.DefineLabel();
		List<Label> list = new List<Label>();
		List<CodeInstruction> list2 = new List<CodeInstruction>();
		StateParamInfo stateParamInfo = null;
		foreach (StateParamInfo loadField in loadFields)
		{
			if (resultType == AsyncMethodCall.ResultType.Named && loadField.Parameter.Name == resultName)
			{
				stateParamInfo = loadField;
			}
			loadField.AddLoadInstructions(list2);
		}
		if (stateParamInfo != null && !type2.IsAssignableTo(stateParamInfo.Parameter.ParameterType))
		{
			throw new ArgumentException($"Cannot store method result of type {type2} to parameter {stateParamInfo.Parameter.Name} of type {stateParamInfo.Parameter.ParameterType}");
		}
		list2.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
		{
			new CodeInstruction(OpCodes.Call, (object)callMethod),
			method.CallVirt(),
			CodeInstruction.StoreLocal(localBuilder.LocalIndex),
			CodeInstruction.LoadLocal(localBuilder.LocalIndex, true),
			new CodeInstruction(OpCodes.Call, (object)methodInfo2),
			new CodeInstruction(OpCodes.Brtrue, (object)label)
		}));
		list2.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[13]
		{
			CodeInstruction.LoadArgument(0, false),
			context.NextStateIndex.LoadConstant(),
			new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.StoreStateInDictMethod),
			new CodeInstruction(OpCodes.Dup, (object)null),
			CodeInstruction.LoadLocal(loadSection.StringDictLocal, false),
			new CodeInstruction(OpCodes.Call, (object)AsyncMethodCall.StoreDictionaryForStateMethod),
			new CodeInstruction(OpCodes.Dup, (object)null),
			CodeInstruction.StoreLocal(0),
			context.StateField.Stfld(),
			CodeInstruction.LoadLocal(localBuilder.LocalIndex, false),
			new CodeInstruction(OpCodes.Box, (object)type),
			CodeInstruction.LoadLocal(0, false),
			AsyncMethodCall.StoreAwaiterMethod.Call()
		}));
		Label label2 = generator.DefineLabel();
		list.Add(label2);
		CodeInstructionExtensions.WithLabels(endingSection.AwaitAsyncInstruction, new Label[1] { label2 });
		list2.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
		{
			CodeInstruction.LoadArgument(0, false),
			new CodeInstruction(OpCodes.Ldflda, (object)context.BuilderField),
			CodeInstruction.LoadLocal(localBuilder.LocalIndex, true),
			CodeInstruction.LoadArgument(0, !isValueType),
			methodInfo.Call(),
			new CodeInstruction(OpCodes.Leave, (object)label2)
		}));
		CodeInstruction val = CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Nop, (object)null), new Label[1] { resumeLabel });
		list2.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[5]
		{
			val,
			CodeInstruction.LoadLocal(loadSection.StateKeyLocal, false),
			AsyncMethodCall.GetAwaiterMethod.Call(),
			new CodeInstruction(OpCodes.Unbox_Any, (object)type),
			CodeInstruction.StoreLocal(localBuilder.LocalIndex)
		}));
		list2.AddRange(new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[5]
		{
			CodeInstruction.LoadArgument(0, false),
			(-1).LoadConstant(),
			new CodeInstruction(OpCodes.Dup, (object)null),
			CodeInstruction.StoreLocal(0),
			context.StateField.Stfld()
		}));
		list2.Add(CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Nop, (object)null), new Label[1] { label }));
		list2.Add(CodeInstruction.LoadLocal(localBuilder.LocalIndex, true));
		list2.Add(CodeInstruction.Call(type, "GetResult", (Type[])null, (Type[])null));
		switch (resultType)
		{
		case AsyncMethodCall.ResultType.Return:
		{
			Label label5 = generator.DefineLabel();
			CodeInstructionExtensions.WithLabels(endingSection.FinishAsyncInstruction, new Label[1] { label5 });
			list2.Add((CodeInstruction)((!(type2 == typeof(void))) ? ((object)CodeInstruction.StoreLocal(1)) : ((object)new CodeInstruction(OpCodes.Nop, (object)null))));
			list2.Add(new CodeInstruction(OpCodes.Leave, (object)label5));
			break;
		}
		case AsyncMethodCall.ResultType.ReturnIf:
		{
			Label label3 = generator.DefineLabel();
			Label label4 = generator.DefineLabel();
			CodeInstructionExtensions.WithLabels(endingSection.FinishAsyncInstruction, new Label[1] { label4 });
			list2.Add(new CodeInstruction(OpCodes.Brfalse_S, (object)label3));
			list2.Add(new CodeInstruction(OpCodes.Leave, (object)label4));
			list2.Add(CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Nop, (object)null), new Label[1] { label3 }));
			break;
		}
		case AsyncMethodCall.ResultType.Named:
			if (stateParamInfo != null)
			{
				stateParamInfo.AddStoreInstructions(list2);
			}
			else if (resultName != null)
			{
				if (type2.IsValueType)
				{
					list2.Add(new CodeInstruction(OpCodes.Box, (object)type2));
				}
				list2.Add(CodeInstruction.LoadLocal(loadSection.StringDictLocal, false));
				list2.Add(new CodeInstruction(OpCodes.Ldstr, (object)resultName));
				list2.Add(AsyncMethodCall.StoreNamedMethod.Call());
			}
			break;
		default:
			list2.Add(new CodeInstruction((type2 == typeof(void)) ? OpCodes.Nop : OpCodes.Pop, (object)null));
			break;
		}
		return new BranchSection
		{
			State = new StateInfo(context.NextStateIndex, list2, callMethod),
			ResumeInstruction = val,
			LeaveLabels = list
		};
	}

	public BranchingStateSection BranchTo(AsyncMethodContext context, StateInfo targetState, ILGenerator generator, out Label resumeLabel)
	{
		throw new InvalidOperationException("Non-branching state section cannot perform branching.");
	}
}
