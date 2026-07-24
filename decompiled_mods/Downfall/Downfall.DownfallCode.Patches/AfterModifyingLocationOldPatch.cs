using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
internal static class AfterModifyingLocationOldPatch
{
	private static readonly MethodInfo Vanilla = AccessTools.Method(typeof(AbstractModel), "AfterModifyingCardPlayResultPileOrPosition", (Type[])null, (Type[])null);

	private static MethodBase TargetMethod()
	{
		return OnPlayWrapperStateMachine.MoveNext();
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Expected O, but got Unknown
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		MethodInfo methodInfo = AccessTools.Method(typeof(AfterModifyingLocationOldPatch), "Bridge", (Type[])null, (Type[])null);
		bool flag = false;
		for (int i = 0; i < list.Count; i++)
		{
			if (CodeInstructionExtensions.Calls(list[i], Vanilla))
			{
				list[i] = new CodeInstruction(OpCodes.Call, (object)methodInfo)
				{
					labels = list[i].labels,
					blocks = list[i].blocks
				};
				flag = true;
			}
		}
		if (!flag)
		{
			throw new InvalidOperationException("AfterModifyingCardPlayResultPileOrPosition call site not found in OnPlayWrapper");
		}
		return list;
	}

	public static Task Bridge(AbstractModel model, CardModel card, PileType pileType, CardPilePosition position)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		Task task = (Task)Vanilla.Invoke(model, new object[3] { card, pileType, position });
		if (!(model is IModifyCardPlayResultLocation c))
		{
			return task;
		}
		return Chain(task, c, card, new CardLocationCompatiblity(card.Owner, pileType, position));
		static async Task Chain(Task orig, IModifyCardPlayResultLocation modifyCardPlayResultLocation, CardModel cd, CardLocationCompatiblity l)
		{
			await orig;
			await modifyCardPlayResultLocation.AfterModifyingCardPlayResultLocationCompability(cd, l);
		}
	}
}
