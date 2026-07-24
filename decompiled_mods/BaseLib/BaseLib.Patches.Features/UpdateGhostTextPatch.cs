using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Debug;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(NDevConsole), "UpdateGhostText")]
public static class UpdateGhostTextPatch
{
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> RemovePushError(IEnumerable<CodeInstruction> instructions)
	{
		MethodInfo methodInfo = AccessTools.Method(typeof(GD), "PushError", new Type[1] { typeof(string) }, (Type[])null);
		List<CodeInstruction> list = instructions.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			if (!CodeInstructionExtensions.Calls(list[i], methodInfo))
			{
				continue;
			}
			for (int num = i; num >= 0; num--)
			{
				if (!(list[num].opcode != OpCodes.Brtrue_S) || !(list[num].opcode != OpCodes.Brtrue))
				{
					for (int j = num + 1; j <= i; j++)
					{
						list[j].opcode = OpCodes.Nop;
						list[j].operand = null;
					}
					break;
				}
			}
			break;
		}
		return list;
	}
}
