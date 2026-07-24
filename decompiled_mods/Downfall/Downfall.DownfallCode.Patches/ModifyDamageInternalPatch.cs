using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Downfall.DownfallCode.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
internal static class ModifyDamageInternalPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(AccessTools.TypeByName("MegaCrit.Sts2.Core.Hooks.Hook") ?? throw new MissingMethodException("Hook not found"), "ModifyDamageInternal", (Type[])null, (Type[])null) ?? throw new MissingMethodException("ModifyDamageInternal not found");
	}

	private static decimal AdditiveBridge(AbstractModel listener, decimal vanillaNum, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (listener is IModifyDamageAdditive modifyDamageAdditive)
		{
			return vanillaNum + modifyDamageAdditive.ModifyDamageAdditiveCompability(target, amount, props, dealer, cardSource, cardPlay);
		}
		return vanillaNum;
	}

	private static decimal MultiplicativeBridge(AbstractModel listener, decimal vanillaNum, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (listener is IModifyDamageMultiplicative modifyDamageMultiplicative)
		{
			return vanillaNum * modifyDamageMultiplicative.ModifyDamageMultiplicativeCompability(target, amount, props, dealer, cardSource, cardPlay);
		}
		return vanillaNum;
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Expected O, but got Unknown
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Expected O, but got Unknown
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Expected O, but got Unknown
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected O, but got Unknown
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d5: Expected O, but got Unknown
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e7: Expected O, but got Unknown
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		bool flag = original.GetParameters().Any((ParameterInfo p) => p.ParameterType == typeof(CardPlay));
		MethodInfo methodInfo = AccessTools.Method(typeof(AbstractModel), "ModifyDamageAdditive", (Type[])null, (Type[])null);
		MethodInfo methodInfo2 = AccessTools.Method(typeof(AbstractModel), "ModifyDamageMultiplicative", (Type[])null, (Type[])null);
		MethodInfo methodInfo3 = AccessTools.Method(typeof(ModifyDamageInternalPatch), "AdditiveBridge", (Type[])null, (Type[])null);
		MethodInfo methodInfo4 = AccessTools.Method(typeof(ModifyDamageInternalPatch), "MultiplicativeBridge", (Type[])null, (Type[])null);
		for (int num = 0; num < list.Count; num++)
		{
			bool flag2 = CodeInstructionExtensions.Calls(list[num], methodInfo);
			bool flag3 = CodeInstructionExtensions.Calls(list[num], methodInfo2);
			if (!flag2 && !flag3)
			{
				continue;
			}
			int num2 = num + 1;
			if (num2 < list.Count && !(list[num2].opcode != OpCodes.Stloc_S))
			{
				object operand = list[num2].operand;
				CodeInstruction val = FindListenerLoadBackwards(list, num);
				if (val != null)
				{
					List<CodeInstruction> list2 = new List<CodeInstruction>
					{
						val.Clone(),
						new CodeInstruction(OpCodes.Ldloc_S, operand),
						new CodeInstruction(OpCodes.Ldarg_2, (object)null),
						new CodeInstruction(OpCodes.Ldloc_0, (object)null),
						new CodeInstruction(OpCodes.Ldarg_S, (object)(byte)5),
						new CodeInstruction(OpCodes.Ldarg_3, (object)null),
						new CodeInstruction(OpCodes.Ldarg_S, (object)(byte)6),
						flag ? new CodeInstruction(OpCodes.Ldarg_S, (object)(byte)7) : new CodeInstruction(OpCodes.Ldnull, (object)null),
						new CodeInstruction(OpCodes.Call, (object)(flag2 ? methodInfo3 : methodInfo4)),
						new CodeInstruction(OpCodes.Stloc_S, operand)
					};
					list.InsertRange(num2 + 1, list2);
					num = num2 + list2.Count;
				}
			}
		}
		return list;
	}

	private static CodeInstruction? FindListenerLoadBackwards(List<CodeInstruction> code, int callIndex)
	{
		int num = callIndex - 1;
		while (num >= 0 && num > callIndex - 10)
		{
			if (code[num].opcode == OpCodes.Ldarg_2)
			{
				return code[num - 1];
			}
			num--;
		}
		return null;
	}
}
