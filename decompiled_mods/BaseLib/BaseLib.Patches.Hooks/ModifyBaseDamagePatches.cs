using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch]
public class ModifyBaseDamagePatches
{
	[HarmonyPatch(typeof(Hook), "ModifyDamage")]
	private static class ModifyDamageCalc
	{
		[HarmonyPrefix]
		private static void AdjustBaseAdditive(ref decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			damage = ModifyBaseDamageAdditive(damage, props, cardSource, modifyDamageHookType);
		}

		[HarmonyTranspiler]
		private static List<CodeInstruction> AdjustBaseMultiplicative(IEnumerable<CodeInstruction> code, MethodBase original)
		{
			int operand;
			return new InstructionPatcher(code).Match(new InstructionMatcher().ldargIndex(original.ArgIndex("damage")).stloc_any()).Step(-1).GetIndexOperand(out operand)
				.Match(new InstructionMatcher().ldargIndex(original.ArgIndex("target")))
				.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[6]
				{
					CodeInstruction.LoadLocal(operand, false),
					CodeInstruction.LoadArgument(original.ArgIndex("props"), false),
					CodeInstruction.LoadArgument(original.ArgIndex("cardSource"), false),
					CodeInstruction.LoadArgument(original.ArgIndex("modifyDamageHookType"), false),
					CodeInstruction.Call(typeof(ModifyBaseDamagePatches), "ModifyBaseDamageMultiplicative", (Type[])null, (Type[])null),
					CodeInstruction.StoreLocal(operand)
				}));
		}
	}

	public static decimal ModifyBaseDamageAdditive(decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (!((Enum)modifyDamageHookType).HasFlag((Enum)(object)(ModifyDamageHookType)2))
		{
			return damage;
		}
		return ModifyBaseDamageAdditiveInternal(damage, props, cardSource);
	}

	private static decimal ModifyBaseDamageAdditiveInternal(decimal damage, ValueProp props, CardModel? cardSource)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (cardSource != null)
		{
			foreach (CardModifier modifier in cardSource.GetModifiers())
			{
				damage += modifier.ModifyBaseDamageAdditive(damage, props);
			}
		}
		return Math.Max(damage, 0m);
	}

	public static decimal ModifyBaseDamageMultiplicative(decimal damage, ValueProp props, CardModel? cardSource, ModifyDamageHookType modifyDamageHookType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		if (!((Enum)modifyDamageHookType).HasFlag((Enum)(object)(ModifyDamageHookType)4))
		{
			return damage;
		}
		return ModifyBaseDamageMultiplicativeInternal(damage, props, cardSource);
	}

	private static decimal ModifyBaseDamageMultiplicativeInternal(decimal damage, ValueProp props, CardModel? cardSource)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		if (cardSource != null)
		{
			foreach (CardModifier modifier in cardSource.GetModifiers())
			{
				damage *= modifier.ModifyBaseDamageMultiplicative(damage, props);
			}
		}
		return Math.Max(damage, 0m);
	}
}
