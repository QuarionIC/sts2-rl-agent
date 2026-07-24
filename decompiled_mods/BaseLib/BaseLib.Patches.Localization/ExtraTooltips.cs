using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch]
public class ExtraTooltips
{
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyTranspiler]
	private static List<CodeInstruction> AddCustomCardTips(IEnumerable<CodeInstruction> instructions)
	{
		return new InstructionPatcher(instructions).Match(new InstructionMatcher().ldarg_0().callvirt(AccessTools.PropertyGetter(typeof(CardModel), "ExtraHoverTips")).call(null)
			.stloc_0()).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
		{
			CodeInstruction.LoadLocal(0, false),
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(ExtraTooltips), "AddTips", (Type[])null, (Type[])null)
		}));
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static IEnumerable<IHoverTip> AddCustomRelicTips(IEnumerable<IHoverTip> __result, RelicModel __instance)
	{
		if (__result is ICollection<IHoverTip> tips)
		{
			AddTipsGeneric(tips, __instance);
		}
		return __result;
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	[HarmonyPostfix]
	private static IEnumerable<IHoverTip> AddCustomPowerTips(IEnumerable<IHoverTip> __result, PowerModel __instance)
	{
		if (__result is ICollection<IHoverTip> tips)
		{
			AddTipsGeneric(tips, __instance);
		}
		return __result;
	}

	public static void AddTips(List<IHoverTip> tips, CardModel card)
	{
		AddTipsGeneric(tips, card);
		foreach (CardModifier modifier in card.GetModifiers())
		{
			modifier.AddTips(tips);
		}
	}

	private static void AddTipsGeneric(ICollection<IHoverTip> tips, DynamicVarSource dynVarSource)
	{
		foreach (DynamicVar value in dynVarSource.DynamicVars.Values)
		{
			IHoverTip val = DynamicVarExtensions.DynamicVarTips[value]?.Invoke(value);
			if (val != null)
			{
				tips.Add(val);
			}
		}
	}
}
