using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Fixes;

internal static class RewardSerializationExt
{
	internal const string KeyPrefix = "__mod_reward_ext_";

	private static readonly ConditionalWeakTable<SerializableReward, RewardExtData> ExtTable = new ConditionalWeakTable<SerializableReward, RewardExtData>();

	internal static void SetExtData(SerializableReward reward, RewardExtData data)
	{
		ExtTable.AddOrUpdate(reward, data);
	}

	internal static bool TryGetExtData(SerializableReward reward, out RewardExtData? data)
	{
		if (ExtTable.TryGetValue(reward, out data))
		{
			return true;
		}
		data = null;
		return false;
	}

	internal static string MakeKey(ulong netId, int index)
	{
		return $"{"__mod_reward_ext_"}{netId}_{index}";
	}

	internal static bool TryParseKey(string key, out ulong netId, out int index)
	{
		netId = 0uL;
		index = 0;
		if (!key.StartsWith("__mod_reward_ext_", StringComparison.Ordinal))
		{
			return false;
		}
		ReadOnlySpan<char> span = key.AsSpan("__mod_reward_ext_".Length);
		int num = span.IndexOf('_');
		if (num < 0)
		{
			return false;
		}
		if (ulong.TryParse(span.Slice(0, num), out netId))
		{
			int num2 = num + 1;
			return int.TryParse(span.Slice(num2, span.Length - num2), out index);
		}
		return false;
	}

	internal static string ToJson(RewardExtData data)
	{
		return JsonSerializer.Serialize(data);
	}

	internal static RewardExtData? FromJson(string json)
	{
		try
		{
			return JsonSerializer.Deserialize<RewardExtData>(json);
		}
		catch (JsonException ex)
		{
			BaseLibMain.Logger.Debug("Reward ext JSON deserialize failed: " + ex.Message, 1);
			return null;
		}
		catch (NotSupportedException ex2)
		{
			BaseLibMain.Logger.Debug("Reward ext JSON deserialize not supported: " + ex2.Message, 1);
			return null;
		}
	}
}
