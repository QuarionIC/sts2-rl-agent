using System;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class CodeInstructionExtensions
{
	public static bool TryGetIntValue(this CodeInstruction instruction, out int result)
	{
		result = 0;
		switch (instruction.opcode.Value)
		{
		case 22:
		case 23:
		case 24:
		case 25:
		case 26:
		case 27:
		case 28:
		case 29:
		case 30:
			result = instruction.opcode.Value - 22;
			return true;
		case 21:
			result = -1;
			return true;
		case 31:
		case 32:
			result = (int)Convert.ToInt64(instruction.operand);
			return true;
		default:
			return false;
		}
	}
}
