using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Utils;

[HarmonyPatch(typeof(CardModel), "UpgradeInternal")]
internal class UpgradeInternalPatch
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> InsertVarUpgrade(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new CallMatcher(AccessTools.Method(typeof(CardModel), "OnUpgrade", (Type[])null, (Type[])null))).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(UpgradeInternalPatch), "UpgradeVars", (Type[])null, (Type[])null)
		}));
	}

	private static void UpgradeVars(CardModel card)
	{
		foreach (KeyValuePair<string, DynamicVar> dynamicVar in card.DynamicVars)
		{
			decimal? num = DynamicVarExtensions.DynamicVarUpgrades[dynamicVar.Value];
			if (num.HasValue)
			{
				dynamicVar.Value.UpgradeValueBy(num.Value);
			}
		}
		if (card is ConstructedCardModel constructedCardModel)
		{
			constructedCardModel.ConstructedUpgrade();
		}
	}
}
