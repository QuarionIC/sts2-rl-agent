using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(CombatRoom), "ToSerializable")]
public static class CombatRoomToSerializableRewardExtPatch
{
	private static void Postfix(ref SerializableRoom __result)
	{
		foreach (KeyValuePair<ulong, List<SerializableReward>> extraReward in __result.ExtraRewards)
		{
			extraReward.Deconstruct(out var key, out var value);
			ulong netId = key;
			List<SerializableReward> list = value;
			for (int i = 0; i < list.Count; i++)
			{
				if (RewardSerializationExt.TryGetExtData(list[i], out RewardExtData data) && data != null)
				{
					string key2 = RewardSerializationExt.MakeKey(netId, i);
					SerializableRoom val = __result;
					if (val.EncounterState == null)
					{
						Dictionary<string, string> dictionary = (val.EncounterState = new Dictionary<string, string>());
					}
					__result.EncounterState[key2] = RewardSerializationExt.ToJson(data);
				}
			}
		}
	}
}
