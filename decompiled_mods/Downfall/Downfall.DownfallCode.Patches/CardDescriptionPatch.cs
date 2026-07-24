using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Localization;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class CardDescriptionPatch
{
	private static string Name(CardModel? card)
	{
		if (card == null)
		{
			return "<null>";
		}
		try
		{
			return ((object)card).ToString() ?? ((object)card).GetType().Name;
		}
		catch
		{
			return ((object)card).GetType().Name;
		}
	}

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(CardModel), "GetDescriptionForPile", new Type[3]
		{
			typeof(PileType),
			AccessTools.Inner(typeof(CardModel), "DescriptionPreviewType"),
			typeof(Creature)
		}, (Type[])null);
	}

	public static void Postfix(CardModel __instance, ref string __result)
	{
		if (!(__instance is DownfallCardModel))
		{
			return;
		}
		try
		{
			string joined = CardDescriptionRegistry.GetJoined(__instance, DescriptionInjectionPoint.TopOfCard);
			string joined2 = CardDescriptionRegistry.GetJoined(__instance, DescriptionInjectionPoint.BottomOfCard);
			if (!string.IsNullOrEmpty(joined))
			{
				__result = joined + "\n" + __result;
			}
			if (!string.IsNullOrEmpty(joined2))
			{
				__result = __result + "\n" + joined2;
			}
		}
		catch (Exception value)
		{
			DownfallMainFile.Logger.Error($"Postfix description failed for '{Name(__instance)}': {value}", 1);
		}
	}

	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Expected O, but got Unknown
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected O, but got Unknown
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Expected O, but got Unknown
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Expected O, but got Unknown
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Expected O, but got Unknown
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Expected O, but got Unknown
		List<CodeInstruction> list = instructions.ToList();
		try
		{
			MethodInfo methodInfo = typeof(string).GetMethods().First((MethodInfo m) => (object)m != null && m.Name == "Join" && m.IsGenericMethod && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(char));
			MethodInfo methodInfo2 = AccessTools.Method(typeof(CardDescriptionPatch), "Inject", (Type[])null, (Type[])null);
			for (int num = 0; num < list.Count; num++)
			{
				if (list[num].opcode == OpCodes.Stloc_S && list[num].operand is LocalBuilder { LocalIndex: 5 })
				{
					list.Insert(num + 1, new CodeInstruction(OpCodes.Call, (object)methodInfo2));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldc_I4, (object)2));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldloc_S, (object)(byte)5));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Call, (object)methodInfo2));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldc_I4, (object)1));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldloc_S, (object)(byte)5));
					list.Insert(num + 1, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
					break;
				}
				if (CodeInstructionExtensions.Calls(list[num], methodInfo))
				{
					list.Insert(num, new CodeInstruction(OpCodes.Call, (object)methodInfo2));
					list.Insert(num, new CodeInstruction(OpCodes.Ldc_I4, (object)3));
					list.Insert(num, new CodeInstruction(OpCodes.Ldloc_S, (object)(byte)5));
					list.Insert(num, new CodeInstruction(OpCodes.Ldarg_0, (object)null));
					break;
				}
			}
			return list;
		}
		catch (Exception value)
		{
			DownfallMainFile.Logger.Error($"Description transpiler failed, returning original IL: {value}", 1);
			return instructions;
		}
	}

	public static void Inject(CardModel card, List<string> source, DescriptionInjectionPoint point)
	{
		if (!(card is DownfallCardModel) || source == null)
		{
			return;
		}
		try
		{
			List<string> list = (from l in CardDescriptionRegistry.GetLines(card, point)
				where !string.IsNullOrEmpty(l)
				select l).ToList();
			if (list.Count != 0)
			{
				if (point == DescriptionInjectionPoint.AboveMainText)
				{
					source.InsertRange(0, list);
				}
				else
				{
					source.AddRange(list);
				}
			}
		}
		catch (Exception value)
		{
			DownfallMainFile.Logger.Error($"Inject({point}) failed for '{Name(card)}': {value}", 1);
		}
	}
}
