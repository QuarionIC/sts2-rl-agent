using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts.TheCity.Events;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Multiplayer.Replay;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Events;

public class ColosseumPatches
{
	[HarmonyPatch(typeof(CombatManager), "StartCombatInternal")]
	public class ReplayWriterFixPatch
	{
		public static void Prefix()
		{
			if (Colosseum.NeedsReplayFix)
			{
				Colosseum.NeedsReplayFix = false;
				CombatReplayWriter combatReplayWriter = RunManager.Instance.CombatReplayWriter;
				if (combatReplayWriter.IsEnabled && !combatReplayWriter.IsRecordingReplay)
				{
					combatReplayWriter.RecordInitialState(RunManager.Instance.ToSave((AbstractRoom)null));
				}
			}
		}
	}

	[HarmonyPatch(typeof(RewardsSet), "WithRewardsFromRoom")]
	public class RewardsPatch
	{
		public static void Postfix(RewardsSet __result, AbstractRoom room)
		{
			CombatRoom val = (CombatRoom)(object)((room is CombatRoom) ? room : null);
			if (val == null || !ColosseumEncounters.Contains(((object)val.Encounter).GetType()))
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

	private static readonly HashSet<Type> ColosseumEncounters = new HashSet<Type> { typeof(ColosseumSecondEncounter) };
}
