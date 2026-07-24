using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
internal static class AfterModifyingLocationNewPatch
{
	private static readonly Type CardLocationType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.CardLocation");

	private static readonly MethodInfo Vanilla = AccessTools.Method(typeof(AbstractModel), "AfterModifyingCardPlayResultLocation", (Type[])null, (Type[])null);

	private static MethodBase TargetMethod()
	{
		return OnPlayWrapperStateMachine.MoveNext();
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		MethodInfo methodInfo = AccessTools.Method(typeof(AfterModifyingLocationNewPatch), "Bridge", (Type[])null, (Type[])null);
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(list[i], Vanilla))
			{
				CodeInstruction val = new CodeInstruction(OpCodes.Box, (object)CardLocationType);
				val.labels.AddRange(list[i].labels);
				val.blocks.AddRange(list[i].blocks);
				list[i] = new CodeInstruction(OpCodes.Call, (object)methodInfo);
				list.Insert(i, val);
				i++;
				flag = true;
			}
		}
		if (!flag)
		{
			throw new InvalidOperationException("AfterModifyingCardPlayResultLocation call site not found in OnPlayWrapper");
		}
		return list;
	}

	public static Task Bridge(AbstractModel model, CardModel card, object boxedLocation)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		Task task = (Task)Vanilla.Invoke(model, new object[2] { card, boxedLocation });
		if (!(model is IModifyCardPlayResultLocation c))
		{
			return task;
		}
		Traverse val = Traverse.Create(boxedLocation);
		CardLocationCompatiblity l = new CardLocationCompatiblity(val.Field("player").GetValue<Player>(), val.Field("pileType").GetValue<PileType>(), val.Field("position").GetValue<CardPilePosition>());
		return Chain(task, c, card, l);
		static async Task Chain(Task orig, IModifyCardPlayResultLocation modifyCardPlayResultLocation, CardModel cd, CardLocationCompatiblity cardLocation)
		{
			await orig;
			await modifyCardPlayResultLocation.AfterModifyingCardPlayResultLocationCompability(cd, cardLocation);
		}
	}
}
