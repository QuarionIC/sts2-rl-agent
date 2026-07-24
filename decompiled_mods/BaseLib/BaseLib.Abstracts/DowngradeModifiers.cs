using System;
using System.Collections.Generic;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CardModel), "DowngradeInternal")]
internal static class DowngradeModifiers
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> DowngradeModifiersOnCard(IEnumerable<CodeInstruction> code)
	{
		return new InstructionPatcher(code).Match(new CallMatcher(AccessToolsExtensions.DeclaredMethod(typeof(CardModel), "AfterDowngraded", (Type[])null, (Type[])null))).InsertBeforeMatch(new _003C_003Ez__ReadOnlySingleElementList<CodeInstruction>(CodeInstruction.Call(typeof(DowngradeModifiers), "OnDowngrade", (Type[])null, (Type[])null)));
	}

	private static CardModel OnDowngrade(CardModel card)
	{
		foreach (CardModifier item in CardModifier.Modifiers(card))
		{
			item.OnDowngrade();
			item.DynamicVars.RecalculateForUpgradeOrEnchant();
		}
		return card;
	}
}
