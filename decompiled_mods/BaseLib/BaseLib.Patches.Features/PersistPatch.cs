using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Cards.Variables;
using BaseLib.Utils.Patching;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Patches.Features;

[HarmonyPatch(typeof(CardModel))]
public static class PersistPatch
{
	private static MethodInfo? TargetMethod = AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeForCardPlay", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileType", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultPileTypeAndPositionForCardPlay", (Type[])null, (Type[])null) ?? AccessTools.DeclaredMethod(typeof(CardModel), "GetResultLocationForCardPlay", (Type[])null, (Type[])null);

	private static IEnumerable<MethodBase> TargetMethods()
	{
		if (TargetMethod != null)
		{
			yield return TargetMethod;
		}
	}

	private static bool Prepare()
	{
		if (TargetMethod != null)
		{
			return true;
		}
		BaseLibMain.Logger.Info("No valid target found, skipping PersistPatch", 1);
		return false;
	}

	[HarmonyTranspiler]
	private static List<CodeInstruction> AltDestination(IEnumerable<CodeInstruction> instructions)
	{
		return new InstructionPatcher(instructions).MatchFromEnd(new InstructionMatcher().ldc_i4_3()).Insert((IEnumerable<CodeInstruction>)new _003C_003Ez__ReadOnlyArray<CodeInstruction>((CodeInstruction[])(object)new CodeInstruction[2]
		{
			CodeInstruction.LoadArgument(0, false),
			CodeInstruction.Call(typeof(PersistPatch), "NormalOrPersist", (Type[])null, (Type[])null)
		}));
	}

	private static PileType NormalOrPersist(PileType dest, CardModel model)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Invalid comparison between Unknown and I4
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if ((int)dest == 3 && model.IsPersist())
		{
			return (PileType)2;
		}
		return dest;
	}

	public static bool IsPersist(this CardModel card)
	{
		DynamicVar val = default(DynamicVar);
		int basePersist = (card.DynamicVars.TryGetValue("Persist", ref val) ? val.IntValue : 0);
		return PersistVar.PersistCount(card, basePersist) > 0;
	}
}
