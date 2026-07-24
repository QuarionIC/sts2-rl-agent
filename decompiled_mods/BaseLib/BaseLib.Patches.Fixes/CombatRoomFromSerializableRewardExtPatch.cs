using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(CombatRoom), "FromSerializable")]
public static class CombatRoomFromSerializableRewardExtPatch
{
	private static void Prefix(SerializableRoom serializableRoom)
	{
		if (serializableRoom.EncounterState != null)
		{
			foreach (var (key, json) in serializableRoom.EncounterState)
			{
				if (RewardSerializationExt.TryParseKey(key, out var netId, out var index) && serializableRoom.ExtraRewards.TryGetValue(netId, out var value) && index >= 0 && index < value.Count)
				{
					RewardExtData rewardExtData = RewardSerializationExt.FromJson(json);
					if (rewardExtData != null)
					{
						RewardSerializationExt.SetExtData(value[index], rewardExtData);
					}
				}
			}
		}
		foreach (KeyValuePair<ulong, List<SerializableReward>> extraReward in serializableRoom.ExtraRewards)
		{
			extraReward.Deconstruct(out var _, out var value2);
			int num = value2.RemoveAll((SerializableReward r) => (int)r.RewardType == 0);
			if (num > 0)
			{
				BaseLibMain.Logger.Warn($"Stripped {num} RewardType.None entry(s) from ExtraRewards " + "(e.g. LinkedRewardSet) - serialization for this type is not supported.", 1);
			}
		}
	}
}
