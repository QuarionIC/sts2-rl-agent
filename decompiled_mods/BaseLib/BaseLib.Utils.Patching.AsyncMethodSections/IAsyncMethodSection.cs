using System.Collections.Generic;
using HarmonyLib;

namespace BaseLib.Utils.Patching.AsyncMethodSections;

internal interface IAsyncMethodSection
{
	List<CodeInstruction> Code { get; }

	IEnumerable<StateInfo> AllStates { get; }
}
