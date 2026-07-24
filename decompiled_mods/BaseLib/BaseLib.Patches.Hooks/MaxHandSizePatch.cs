using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Hooks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Hooks;

public static class MaxHandSizePatch
{
	private static MethodInfo? MaxCardsInHandProperty = AccessTools.PropertyGetter(typeof(CardPile), "MaxCardsInHand");

	public const int DefaultMaxHandSize = 10;

	internal static readonly MethodInfo GetMaxHandSizeFromBaseMethod = AccessTools.Method(typeof(MaxHandSizePatch), "GetMaxHandSize", new Type[2]
	{
		typeof(Player),
		typeof(int)
	}, (Type[])null);

	internal static bool IsDefaultMaxHandSizeConst(CodeInstruction ins)
	{
		if (!(ins.opcode == OpCodes.Ldc_I4_S) || !(ins.operand is sbyte b) || b != 10)
		{
			if (ins.opcode == OpCodes.Ldc_I4 && ins.operand is int num)
			{
				return num == 10;
			}
			return false;
		}
		return true;
	}

	internal static bool IsBetaMaxHandSize(CodeInstruction ins)
	{
		if (MaxCardsInHandProperty != null)
		{
			return CodeInstructionExtensions.Calls(ins, MaxCardsInHandProperty);
		}
		return false;
	}

	[Obsolete("Prefer to use GetMaxHandSize(player, CardPile.MaxCardsInHand) instead")]
	public static int GetMaxHandSize(Player player)
	{
		return GetMaxHandSize(player, 10);
	}

	public static int GetMaxHandSize(Player player, int baseLimit)
	{
		IRunState instance = (IRunState)(((object)player.RunState) ?? ((object)NullRunState.Instance));
		object obj = BetaMainCompatibility.Creature_.CombatState.Get(player.Creature);
		int num = baseLimit;
		List<IMaxHandSizeModifier> list = new List<IMaxHandSizeModifier>();
		foreach (AbstractModel item in BetaMainCompatibility.RunState.IterateHookListeners.Invoke<IEnumerable<AbstractModel>>(instance, new object[1] { obj }) ?? throw new InvalidOperationException("Failed to invoke IterateHookListeners properly"))
		{
			if (item is IMaxHandSizeModifier maxHandSizeModifier)
			{
				list.Add(maxHandSizeModifier);
				num = maxHandSizeModifier.ModifyMaxHandSize(player, num);
			}
		}
		foreach (IMaxHandSizeModifier item2 in list)
		{
			num = item2.ModifyMaxHandSizeLate(player, num);
		}
		return Math.Max(0, num);
	}

	internal static int GetMaxHandSizeFromCard(CardModel? card, int baseAmount)
	{
		Player val = ((card != null) ? card.Owner : null);
		if (val == null)
		{
			return 10;
		}
		return GetMaxHandSize(val, baseAmount);
	}
}
