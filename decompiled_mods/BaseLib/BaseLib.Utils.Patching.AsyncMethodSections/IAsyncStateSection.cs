using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal interface IAsyncStateSection : IAsyncMethodSection
{
	IEnumerable<Label> LeaveLabels { get; }

	CodeInstruction ResumeInstruction { get; }

	BranchingStateSection BranchTo(AsyncMethodContext context, StateInfo targetState, ILGenerator generator, out Label resumeLabel);
}
