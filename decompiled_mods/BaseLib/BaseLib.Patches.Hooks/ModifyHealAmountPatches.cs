using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Hooks;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Hooks;

[HarmonyPatch(/*Could not decode attribute arguments.*/)]
public static class ModifyHealAmountPatches
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> Patch(IEnumerable<CodeInstruction> code)
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		List<CodeInstruction> match;
		object operand;
		return new InstructionPatcher(code).Match(new InstructionMatcher().ldarg_0().ldfld(null).ldfld(null)
			.PredicateMatch((object? op) => op is FieldInfo fieldInfo && fieldInfo.Name.Contains("creature"))).CopyMatch(out match).Match(new InstructionMatcher().ldfld(null).PredicateMatch((object? op) => op is FieldInfo fieldInfo && fieldInfo.Name.Equals("amount")))
			.Step(-1)
			.GetOperand(out operand)
			.Insert(CodeInstruction.LoadArgument(0, false))
			.Step()
			.Insert((IEnumerable<CodeInstruction>)match)
			.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[4]
			{
				CodeInstruction.Call(typeof(ModifyHealAmountPatches), "ModifyHeal", (Type[])null, (Type[])null),
				new CodeInstruction(OpCodes.Stfld, operand),
				CodeInstruction.LoadArgument(0, false),
				new CodeInstruction(OpCodes.Ldfld, operand)
			}));
	}

	public static decimal ModifyHeal(decimal amount, Creature creature)
	{
		object obj = BetaMainCompatibility.Creature_.CombatState.Get(creature);
		Player player = creature.Player;
		object obj2 = ((player != null) ? player.RunState : null);
		if (obj2 == null)
		{
			if (obj != null)
			{
				obj2 = new CombatStateWrapper(obj).RunState;
			}
			else
			{
				IRunState instance = (IRunState)(object)NullRunState.Instance;
				obj2 = instance;
			}
		}
		ModifyAdditive((IRunState)obj2, obj, creature, ref amount);
		ModifyMultiplicative((IRunState)obj2, obj, creature, ref amount);
		return amount;
	}

	private static void ModifyAdditive(IRunState runState, object? combatState, Creature creature, ref decimal amount)
	{
		decimal num = amount;
		foreach (AbstractModel item in BetaMainCompatibility.RunState.IterateHookListeners.Invoke<IEnumerable<AbstractModel>>(runState, new object[1] { combatState }) ?? Array.Empty<AbstractModel>())
		{
			if (item is IHealAmountModifier healAmountModifier)
			{
				num += healAmountModifier.ModifyHealAdditive(creature, amount);
			}
		}
		amount = num;
	}

	private static void ModifyMultiplicative(IRunState runState, object? combatState, Creature creature, ref decimal __result)
	{
		decimal val = __result;
		foreach (AbstractModel item in BetaMainCompatibility.RunState.IterateHookListeners.Invoke<IEnumerable<AbstractModel>>(runState, new object[1] { combatState }) ?? Array.Empty<AbstractModel>())
		{
			if (item is IHealAmountModifier healAmountModifier)
			{
				val *= healAmountModifier.ModifyHealMultiplicative(creature, __result);
			}
		}
		__result = Math.Max(0m, val);
	}
}
