using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

[HarmonyPatch(typeof(CardModel), "UpdateDynamicVarPreview")]
internal static class UpdateModifierPreview
{
	[HarmonyTranspiler]
	private static List<CodeInstruction> UpdateModifierVars(IEnumerable<CodeInstruction> code)
	{
		List<CodeInstruction> match;
		InstructionPatcher instructionPatcher = new InstructionPatcher(code).Match(new InstructionMatcher().ldarg_0().ldarg_1().ldarg_2()
			.ldloc_any()
			.callvirt(typeof(DynamicVar), "UpdateCardPreview")).CopyMatch(out match).MatchEnd()
			.Step(-1);
		List<CodeInstruction> list = new List<CodeInstruction>();
		list.AddRange(match.SkipLast(1));
		list.Add(CodeInstruction.Call(typeof(UpdateModifierPreview), "UpdateModifierVarPreview", (Type[])null, (Type[])null));
		return instructionPatcher.Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyList<CodeInstruction>(list));
	}

	private static void UpdateModifierVarPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		foreach (CardModifier item in CardModifier.Modifiers(card))
		{
			item.UpdateDynamicVarPreview(previewMode, target, runGlobalHooks);
		}
	}
}
