using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BaseLib.Utils.Patching;

public class CallMatcher(MethodInfo method) : IMatcher
{
	public bool Match(List<string> log, List<CodeInstruction> code, int startIndex, out int matchStart, out int matchEnd)
	{
		log.Add("Starting CallMatcher for " + method.Name);
		matchStart = startIndex;
		matchEnd = matchStart;
		for (int i = startIndex; i < code.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(code[i], method))
			{
				matchStart = i;
				matchEnd = i + 1;
				return true;
			}
		}
		return false;
	}
}
