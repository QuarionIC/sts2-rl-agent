using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch(typeof(CombatManager), "SetupPlayerTurn")]
internal static class CombatManager_SetupPlayerTurn_MaxHandSizePatch
{
	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase original)
	{
		foreach (CodeInstruction ins in instructions)
		{
			if (MaxHandSizePatch.IsDefaultMaxHandSizeConst(ins) || MaxHandSizePatch.IsBetaMaxHandSize(ins))
			{
				yield return new CodeInstruction(OpCodes.Ldarg_1, (object)null);
				yield return ins;
				yield return new CodeInstruction(OpCodes.Call, (object)MaxHandSizePatch.GetMaxHandSizeFromBaseMethod);
			}
			else
			{
				yield return ins;
			}
		}
	}
}
