using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class EndingSection : IAsyncMethodSection
{
	public required List<CodeInstruction> Code { get; init; }

	public IEnumerable<StateInfo> AllStates => Array.Empty<StateInfo>();

	public required Type? ReturnType { get; init; }

	public required CodeInstruction AwaitAsyncInstruction { get; init; }

	public required CodeInstruction FinishAsyncInstruction { get; init; }

	public static EndingSection Read(IAsyncStateSection mainSection, IEnumerator<CodeInstruction> codeEnumerator)
	{
		Type type = null;
		CodeInstruction val = null;
		CodeInstruction val2 = null;
		List<Label> list = mainSection.LeaveLabels.ToList();
		List<CodeInstruction> list2 = new List<CodeInstruction>();
		do
		{
			CodeInstruction current = codeEnumerator.Current;
			list2.Add(current);
			if ((current.opcode == OpCodes.Leave || current.opcode == OpCodes.Leave_S) && current.operand is Label item)
			{
				list.Add(item);
			}
			foreach (Label label in current.labels)
			{
				if (!list.Remove(label))
				{
					continue;
				}
				if (current.opcode == OpCodes.Ret)
				{
					if (val2 == null)
					{
						val2 = current;
					}
				}
				else if (val == null)
				{
					val = current;
				}
			}
			if (!(type != null) && !(current.opcode != OpCodes.Call) && current.operand is MethodInfo { Name: "SetResult" } methodInfo)
			{
				Type declaringType = methodInfo.DeclaringType;
				if (!(declaringType == null) && !(declaringType == typeof(AsyncTaskMethodBuilder)) && !(declaringType == typeof(AsyncValueTaskMethodBuilder)) && declaringType.IsConstructedGenericType && (declaringType.GetGenericTypeDefinition() == typeof(AsyncTaskMethodBuilder<>) || declaringType.GetGenericTypeDefinition() == typeof(AsyncValueTaskMethodBuilder<>)))
				{
					type = declaringType.GenericTypeArguments[0];
				}
			}
		}
		while (codeEnumerator.MoveNext());
		if (val == null)
		{
			throw new Exception("Failed to find instruction to jump to when done with async method;\nCODE:\n" + GeneralExtensions.Join<CodeInstruction>((IEnumerable<CodeInstruction>)list2, (Func<CodeInstruction, string>)((CodeInstruction instruction) => ((object)instruction).ToString()), "\n"));
		}
		if (val2 == null)
		{
			throw new Exception("Failed to find instruction to jump to when awaiting;\nCODE:\n" + GeneralExtensions.Join<CodeInstruction>((IEnumerable<CodeInstruction>)list2, (Func<CodeInstruction, string>)((CodeInstruction instruction) => ((object)instruction).ToString()), "\n"));
		}
		return new EndingSection
		{
			Code = list2,
			ReturnType = type,
			FinishAsyncInstruction = val,
			AwaitAsyncInstruction = val2
		};
	}
}
