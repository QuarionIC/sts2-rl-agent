using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Localization;

[HarmonyPatch(typeof(CardModel), "GetDescriptionForPile", new Type[]
{
	typeof(PileType),
	typeof(DescriptionPreviewType),
	typeof(Creature)
})]
public static class DescriptionOverrides
{
	public delegate void CustomizeDescriptionHandler(CardModel card, Creature? target, ref string description);

	public static event CustomizeDescriptionHandler? CustomizeDescription;

	public static event CustomizeDescriptionHandler? CustomizeDescriptionPost;

	[HarmonyTranspiler]
	private static List<CodeInstruction> TranspileGetDescriptionForPile(IEnumerable<CodeInstruction> instructionsIn)
	{
		return new InstructionPatcher(instructionsIn).Match(new InstructionMatcher().ldloc_0().callvirt(typeof(LocString), "GetFormattedText").opcode(OpCodes.Stind_Ref)
			.stloc_s()).Step(-1).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[3]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.LoadArgument(3, false),
			CodeInstruction.Call(typeof(DescriptionOverrides), "InvokeCustomize", (Type[])null, (Type[])null)
		}));
	}

	[HarmonyPostfix]
	internal static void InvokeCustomizePost(CardModel __instance, Creature? target, ref string __result)
	{
		DescriptionOverrides.CustomizeDescriptionPost?.Invoke(__instance, target, ref __result);
	}

	internal static List<string> InvokeCustomize(List<string> descriptionList, CardModel card, Creature? target)
	{
		if (descriptionList.Count == 0)
		{
			return descriptionList;
		}
		string description = descriptionList[0];
		DescriptionOverrides.CustomizeDescription?.Invoke(card, target, ref description);
		descriptionList[0] = description;
		return descriptionList;
	}
}
