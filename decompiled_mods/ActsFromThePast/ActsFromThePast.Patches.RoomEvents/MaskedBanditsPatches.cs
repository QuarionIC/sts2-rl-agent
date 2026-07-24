using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts.TheCity.Events;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.addons.mega_text;

namespace ActsFromThePast.Patches.RoomEvents;

public class MaskedBanditsPatches
{
	[HarmonyPatch(typeof(RewardsSet), "WithRewardsFromRoom")]
	public class RewardsPatch
	{
		public static void Postfix(RewardsSet __result, AbstractRoom room)
		{
			CombatRoom val = (CombatRoom)(object)((room is CombatRoom) ? room : null);
			if (val != null && BanditEncounters.Contains(((object)val.Encounter).GetType()))
			{
				HashSet<Reward> extraRewards = val.ExtraRewards.Values.SelectMany((List<Reward> list) => list).ToHashSet();
				__result.Rewards.RemoveAll((Reward r) => !extraRewards.Contains(r) && r is GoldReward);
			}
		}
	}

	[HarmonyPatch(typeof(NMapScreen), "Close")]
	public class MapClosePatch
	{
		public static void Postfix(NMapScreen __instance)
		{
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Expected O, but got Unknown
			string text = null;
			if (MaskedBandits.WaitingForMapEasterEgg)
			{
				MaskedBandits.WaitingForMapEasterEgg = false;
				text = "ACTSFROMTHEPAST-MASKED_BANDITS.pages.PAID_4.description";
			}
			else if (MaskedBandits.WaitingForBrandishEasterEgg)
			{
				MaskedBandits.WaitingForBrandishEasterEgg = false;
				text = "ACTSFROMTHEPAST-MASKED_BANDITS.pages.BRANDISH_4.description";
			}
			if (text == null)
			{
				return;
			}
			NEventRoom instance = NEventRoom.Instance;
			NEventLayout val = ((instance != null) ? instance.Layout : null);
			if (val != null)
			{
				MegaRichTextLabel nodeOrNull = ((Node)val).GetNodeOrNull<MegaRichTextLabel>(NodePath.op_Implicit("EventDescription"));
				if (nodeOrNull != null)
				{
					LocString val2 = new LocString("events", text);
					nodeOrNull.Text = val2.GetFormattedText();
				}
			}
		}
	}

	private static readonly HashSet<Type> BanditEncounters = new HashSet<Type> { typeof(RedMaskBanditsEvent) };
}
