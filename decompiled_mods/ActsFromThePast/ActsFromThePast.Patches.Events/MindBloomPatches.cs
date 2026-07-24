using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Acts.TheBeyond.Events;
using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.Events;

public class MindBloomPatches
{
	[HarmonyPatch(typeof(RewardsSet), "WithRewardsFromRoom")]
	public class RewardsPatch
	{
		public static void Postfix(RewardsSet __result, AbstractRoom room)
		{
			CombatRoom val = (CombatRoom)(object)((room is CombatRoom) ? room : null);
			if (val == null || !MindBloomEncounters.Contains(((object)val.Encounter).GetType()))
			{
				return;
			}
			HashSet<Reward> extraRewards = val.ExtraRewards.Values.SelectMany((List<Reward> list) => list).ToHashSet();
			__result.Rewards.RemoveAll(delegate(Reward r)
			{
				bool flag = !extraRewards.Contains(r);
				bool flag2 = flag;
				if (flag2)
				{
					bool flag3 = ((r is GoldReward || r is RelicReward) ? true : false);
					flag2 = flag3;
				}
				return flag2;
			});
		}
	}

	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	public class MindBloomEncounterRoomTypePatch
	{
		public static void Postfix(CustomEncounterModel __instance, ref RoomType __result)
		{
			bool combatActive = MindBloom.CombatActive;
			bool flag = combatActive;
			if (flag)
			{
				bool flag2 = ((__instance is MindBloomGuardian || __instance is MindBloomHexaghost || __instance is MindBloomSlimeBoss) ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				__result = (RoomType)3;
			}
		}
	}

	private static readonly HashSet<Type> MindBloomEncounters = new HashSet<Type>
	{
		typeof(MindBloomGuardian),
		typeof(MindBloomHexaghost),
		typeof(MindBloomSlimeBoss)
	};
}
