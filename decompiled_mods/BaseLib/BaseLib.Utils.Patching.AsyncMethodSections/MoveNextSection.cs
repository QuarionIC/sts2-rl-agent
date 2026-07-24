using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class MoveNextSection : IAsyncMethodSection
{
	public required LoadStateSection LoadSection { get; internal init; }

	public required IAsyncStateSection MainSection { get; internal init; }

	public required EndingSection EndingSection { get; internal init; }

	public List<CodeInstruction> Code
	{
		get
		{
			List<CodeInstruction> code = LoadSection.Code;
			List<CodeInstruction> code2 = MainSection.Code;
			List<CodeInstruction> code3 = EndingSection.Code;
			int num = code.Count + code2.Count + code3.Count;
			List<CodeInstruction> list = new List<CodeInstruction>(num);
			CollectionsMarshal.SetCount(list, num);
			Span<CodeInstruction> span = CollectionsMarshal.AsSpan(list);
			int num2 = 0;
			Span<CodeInstruction> span2 = CollectionsMarshal.AsSpan(code);
			span2.CopyTo(span.Slice(num2, span2.Length));
			num2 += span2.Length;
			Span<CodeInstruction> span3 = CollectionsMarshal.AsSpan(code2);
			span3.CopyTo(span.Slice(num2, span3.Length));
			num2 += span3.Length;
			Span<CodeInstruction> span4 = CollectionsMarshal.AsSpan(code3);
			span4.CopyTo(span.Slice(num2, span4.Length));
			num2 += span4.Length;
			return list;
		}
	}

	public IEnumerable<StateInfo> AllStates => MainSection.AllStates;

	public static MoveNextSection Read(AsyncMethodContext context, IEnumerator<CodeInstruction> codeEnumerator)
	{
		LoadStateSection loadStateSection = LoadStateSection.Read(context, codeEnumerator);
		BranchingStateSection mainSection = BranchingStateSection.Read(context, loadStateSection, codeEnumerator);
		EndingSection endingSection = BaseLib.Utils.Patching.AsyncMethodSections.EndingSection.Read(mainSection, codeEnumerator);
		return new MoveNextSection
		{
			LoadSection = loadStateSection,
			MainSection = mainSection,
			EndingSection = endingSection
		};
	}

	public void InsertState(AsyncMethodContext context, bool before, StateInfo targetState, MethodInfo callMethod, List<StateParamInfo> methodCallParams, AsyncMethodCall.ResultType resultType, string? resultName)
	{
		ILGenerator generator = context.Generator;
		MainSection.BranchTo(context, targetState, generator, out var resumeLabel).InsertState(context, LoadSection, EndingSection, before, targetState, callMethod, methodCallParams, resumeLabel, resultType, resultName);
	}
}
