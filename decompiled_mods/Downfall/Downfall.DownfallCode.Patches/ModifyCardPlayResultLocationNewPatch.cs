using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch]
public static class ModifyCardPlayResultLocationNewPatch
{
	private static readonly Type? CardLocationType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Entities.Cards.CardLocation");

	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(typeof(Hook), "ModifyCardPlayResultLocation", (Type[])null, (Type[])null);
	}

	private static void Postfix(ICombatState combatState, CardModel card, bool isAutoPlay, ResourceInfo resources, ref object __result, ref IEnumerable<AbstractModel> modifiers)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		Traverse obj = Traverse.Create(__result);
		Player value = obj.Field("player").GetValue<Player>();
		PileType value2 = obj.Field("pileType").GetValue<PileType>();
		CardPilePosition value3 = obj.Field("position").GetValue<CardPilePosition>();
		IEnumerable<IModifyCardPlayResultLocation> source = default(IEnumerable<IModifyCardPlayResultLocation>);
		CardLocationCompatiblity cardLocationCompatiblity = HookUtils.Modify<IModifyCardPlayResultLocation, CardLocationCompatiblity>(combatState, new CardLocationCompatiblity(value, value2, value3), (Func<IModifyCardPlayResultLocation, CardLocationCompatiblity, CardLocationCompatiblity>)((IModifyCardPlayResultLocation m, CardLocationCompatiblity loc) => m.ModifyCardPlayResultLocationCompability(card, isAutoPlay, resources, loc)), ref source);
		__result = Activator.CreateInstance(CardLocationType, cardLocationCompatiblity.Player, cardLocationCompatiblity.PileType, cardLocationCompatiblity.Position);
		List<AbstractModel> list = source.OfType<AbstractModel>().ToList();
		if (list.Count > 0)
		{
			modifiers = modifiers.Concat(list).ToList();
		}
	}
}
