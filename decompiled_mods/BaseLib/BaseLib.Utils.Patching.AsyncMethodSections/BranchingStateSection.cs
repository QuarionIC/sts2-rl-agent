using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Extensions;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal class BranchingStateSection : IAsyncStateSection, IAsyncMethodSection
{
	public readonly List<IAsyncStateSection> Sections = new List<IAsyncStateSection>();

	public required Dictionary<Label, int> LabelStates { get; init; }

	public List<CodeInstruction> Prep { get; set; } = new List<CodeInstruction>();

	public required List<CodeInstruction> Branching { get; set; }

	public CodeInstruction ResumeInstruction => Branching.First();

	public List<CodeInstruction> Code
	{
		get
		{
			List<CodeInstruction> list = new List<CodeInstruction>();
			list.AddRange(Prep);
			list.AddRange(Branching);
			list.AddRange(Sections.SelectMany((IAsyncStateSection section) => section.Code));
			return list;
		}
	}

	public IEnumerable<StateInfo> AllStates => Sections.SelectMany((IAsyncStateSection section) => section.AllStates);

	public IEnumerable<Label> LeaveLabels => Sections.SelectMany((IAsyncStateSection section) => section.LeaveLabels);

	public static BranchingStateSection Read(AsyncMethodContext context, LoadStateSection loadStateSection, IEnumerator<CodeInstruction> codeEnumerator)
	{
		BaseLibMain.Logger.Debug("Starting branching section", 1);
		List<CodeInstruction> list = new List<CodeInstruction>();
		Dictionary<int, Label> dictionary = new Dictionary<int, Label>();
		int? num = null;
		int num2 = 0;
		bool flag = false;
		do
		{
			CodeInstruction current = codeEnumerator.Current;
			if (list.Count == 0 || current.opcode == OpCodes.Ldloc_0)
			{
				list.Add(current);
				continue;
			}
			if (current.opcode == OpCodes.Switch)
			{
				Label[] array = (Label[])current.operand;
				for (int i = 0; i < array.Length; i++)
				{
					dictionary[i + num2] = array[i];
				}
				list.Add(current);
				codeEnumerator.MoveNext();
				break;
			}
			if (current.TryGetIntValue(out var result))
			{
				num = result;
				list.Add(current);
				continue;
			}
			switch (current.opcode.Value)
			{
			case 0:
				if (current.labels.Count > 0)
				{
					flag = true;
				}
				break;
			case 89:
				if (!num.HasValue)
				{
					BaseLibMain.Logger.Warn("Failed to evaluate sub, checkState null", 1);
					break;
				}
				if (num2 != 0)
				{
					BaseLibMain.Logger.Warn("Failed to process sub, stateOffset already set", 1);
					break;
				}
				num2 = num.Value;
				num = null;
				BaseLibMain.Logger.Debug($"Branching section uses sub offset of {num2}", 1);
				break;
			case 44:
			case 57:
				dictionary[0] = (Label)current.operand;
				break;
			case 45:
			case 58:
				BaseLibMain.Logger.Warn("Unexpected Brtrue in jump section of state machine", 1);
				break;
			case 46:
			case 59:
				if (!num.HasValue)
				{
					BaseLibMain.Logger.Warn("Failed to evaluate beq, checkState null", 1);
				}
				else
				{
					dictionary[num.Value] = (Label)current.operand;
				}
				break;
			case 43:
			case 56:
			{
				Label value = (Label)current.operand;
				foreach (KeyValuePair<int, Label> item in dictionary)
				{
					foreach (Label label in current.labels)
					{
						if (item.Value == label)
						{
							dictionary[item.Key] = value;
							break;
						}
					}
				}
				break;
			}
			default:
				BaseLibMain.Logger.Debug("Found end of branching section", 1);
				flag = true;
				break;
			}
			if (flag)
			{
				break;
			}
			list.Add(current);
		}
		while (codeEnumerator.MoveNext());
		Dictionary<Label, int> dictionary2 = new Dictionary<Label, int>();
		foreach (KeyValuePair<int, Label> item2 in dictionary)
		{
			dictionary2[item2.Value] = item2.Key;
		}
		BranchingStateSection branchingStateSection = new BranchingStateSection
		{
			Branching = list,
			LabelStates = dictionary2
		};
		while (dictionary.Count > 0)
		{
			IAsyncStateSection asyncStateSection = BranchSection.Read(context, loadStateSection, branchingStateSection, codeEnumerator);
			foreach (StateInfo allState in asyncStateSection.AllStates)
			{
				dictionary.Remove(allState.Index);
			}
			branchingStateSection.Sections.Add(asyncStateSection);
		}
		return branchingStateSection;
	}

	public BranchingStateSection BranchTo(AsyncMethodContext context, StateInfo targetState, ILGenerator generator, out Label resumeLabel)
	{
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Expected O, but got Unknown
		foreach (IAsyncStateSection section in Sections)
		{
			if (!section.AllStates.Contains(targetState))
			{
				continue;
			}
			resumeLabel = generator.DefineLabel();
			BaseLibMain.Logger.Debug($"Generating branch instruction in branching section using label {resumeLabel.Id}", 1);
			for (int i = 0; i < Branching.Count; i++)
			{
				if (Branching[i].blocks.Count != 0)
				{
					if (i == 0)
					{
						List<CodeInstruction> list = new List<CodeInstruction>();
						list.Add(CodeInstructionExtensions.MoveBlocksFrom(CodeInstructionExtensions.MoveLabelsFrom(CodeInstruction.LoadLocal(0, false), Branching[0]), Branching[0]));
						list.Add(context.NextStateIndex.LoadConstant());
						list.Add(new CodeInstruction(OpCodes.Beq, (object)resumeLabel));
						list.AddRange(Branching);
						Branching = list;
					}
					else
					{
						List<CodeInstruction> list2 = new List<CodeInstruction>();
						list2.AddRange(Branching.Take(i));
						list2.Add(CodeInstructionExtensions.MoveBlocksFrom(CodeInstruction.LoadLocal(0, false), Branching[i]));
						list2.Add(context.NextStateIndex.LoadConstant());
						list2.Add(new CodeInstruction(OpCodes.Beq, (object)resumeLabel));
						list2.AddRange(Branching.Skip(i));
						Branching = list2;
					}
					break;
				}
			}
			if (section is BranchingStateSection branchingStateSection)
			{
				CodeInstructionExtensions.WithLabels(branchingStateSection.ResumeInstruction, new Label[1] { resumeLabel });
				return branchingStateSection.BranchTo(context, targetState, generator, out resumeLabel);
			}
			return this;
		}
		throw new InvalidOperationException("Failed to find target state in branching state section.");
	}

	public void InsertState(AsyncMethodContext context, LoadStateSection loadSection, EndingSection endingSection, bool before, StateInfo targetState, MethodInfo callMethod, List<StateParamInfo> methodCallParams, Label resumeLabel, AsyncMethodCall.ResultType resultType, string? resultName)
	{
		int num = Sections.FindIndex((IAsyncStateSection section) => section is BranchSection branchSection && branchSection.State == targetState);
		if (num < 0)
		{
			throw new InvalidOperationException("Failed to find target state in branching state section.");
		}
		if (!before)
		{
			num++;
		}
		Sections.Insert(num, BranchSection.Create(context, loadSection, endingSection, callMethod, methodCallParams, resumeLabel, resultType, resultName));
	}
}
