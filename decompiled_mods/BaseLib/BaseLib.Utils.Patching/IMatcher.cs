using System.Collections.Generic;
using HarmonyLib;

namespace BaseLib.Utils.Patching;

public interface IMatcher
{
	bool Match(List<string> log, List<CodeInstruction> code, int startIndex, out int matchStart, out int matchEnd);
}
