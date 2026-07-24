using System;
using System.Collections.Generic;
using BaseLib;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace Baselib.Patches.Content;

[HarmonyPatch(typeof(Reward))]
internal static class CustomRewardPatches
{
	internal static readonly Dictionary<RewardType, CreateRewardFromSave<CustomReward>> _RewardTypeDeserializers = new Dictionary<RewardType, CreateRewardFromSave<CustomReward>>();

	public static void RegisterCustomReward(RewardType type, CreateRewardFromSave<CustomReward> deserializer)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		if (_RewardTypeDeserializers.ContainsKey(type))
		{
			throw new NotSupportedException($"Registering multiple rewards of the same type ({type}) is not supported");
		}
		BaseLibMain.Logger.Info("Registering RewardType " + CustomEnums.EnumName<RewardType>((int)type), 1);
		_RewardTypeDeserializers.Add(type, deserializer);
	}

	[HarmonyPatch("FromSerializable")]
	[HarmonyPrefix]
	public static bool FromSerializablePrefix(SerializableReward save, Player player, ref Reward __result)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected I4, but got Unknown
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if (_RewardTypeDeserializers.ContainsKey(save.RewardType))
		{
			BaseLibMain.Logger.Info($"Found RewardType {CustomEnums.EnumName<RewardType>((int)save.RewardType)} in registry from mod {_RewardTypeDeserializers[save.RewardType].GetType().Assembly}", 1);
			CreateRewardFromSave<CustomReward> createRewardFromSave = _RewardTypeDeserializers[save.RewardType];
			__result = (Reward)(object)createRewardFromSave(save, player);
			return false;
		}
		BaseLibMain.Logger.Warn($"No CustomReward found for RewardType {save.RewardType}, proceeding to basegame method", 1);
		return true;
	}
}
