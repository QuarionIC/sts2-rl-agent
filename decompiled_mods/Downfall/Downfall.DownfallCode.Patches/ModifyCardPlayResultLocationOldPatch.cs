using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class ModifyCardPlayResultLocationOldPatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(Hook), "ModifyCardPlayResultPileTypeAndPosition", (Type[])null, (Type[])null);
	}

	private static void Postfix(ICombatState combatState, CardModel card, bool isAutoPlay, ResourceInfo resources, ref (PileType, CardPilePosition) __result, ref IEnumerable<AbstractModel> modifiers)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		IEnumerable<IModifyCardPlayResultLocation> source = default(IEnumerable<IModifyCardPlayResultLocation>);
		CardLocationCompatiblity cardLocationCompatiblity = HookUtils.Modify<IModifyCardPlayResultLocation, CardLocationCompatiblity>(combatState, new CardLocationCompatiblity(card.Owner, __result.Item1, __result.Item2), (Func<IModifyCardPlayResultLocation, CardLocationCompatiblity, CardLocationCompatiblity>)((IModifyCardPlayResultLocation m, CardLocationCompatiblity loc) => m.ModifyCardPlayResultLocationCompability(card, isAutoPlay, resources, loc)), ref source);
		__result = (cardLocationCompatiblity.PileType, cardLocationCompatiblity.Position);
		List<AbstractModel> list = source.OfType<AbstractModel>().ToList();
		if (list.Count > 0)
		{
			modifiers = modifiers.Concat(list).ToList();
		}
	}
}
