using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(CardReward), "ToSerializable")]
public static class CardRewardToSerializablePatch
{
	private static readonly Func<CardReward, CardCreationOptions> GetOptions = AccessTools.MethodDelegate<Func<CardReward, CardCreationOptions>>(AccessTools.DeclaredPropertyGetter(typeof(CardReward), "Options"), (object)null, true, (Type[])null);

	private static readonly Func<CardReward, int> GetOptionCount = AccessTools.MethodDelegate<Func<CardReward, int>>(AccessTools.DeclaredPropertyGetter(typeof(CardReward), "OptionCount"), (object)null, true, (Type[])null);

	private static bool Prefix(CardReward __instance, ref SerializableReward __result)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Invalid comparison between Unknown and I4
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected O, but got Unknown
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Expected I4, but got Unknown
		CardCreationOptions val = GetOptions(__instance);
		IEnumerable<CardModel> customCardPool = CardRewardSerializationCompatibility.GetCustomCardPool(val);
		bool flag = (int)val.Flags > 0;
		bool flag2 = val.CardPoolFilter != null;
		bool flag3 = val.CardPools.Count <= 0;
		if (!flag && !flag2 && !flag3)
		{
			return true;
		}
		SerializableReward val2 = new SerializableReward
		{
			RewardType = (RewardType)1
		};
		RewardExtData rewardExtData = null;
		if (flag3)
		{
			if (customCardPool != null)
			{
				rewardExtData = BuildCustomPoolExt(val, customCardPool);
				val2.Source = val.Source;
				val2.RarityOdds = val.RarityOdds;
			}
			else if (!CardRewardSerializationCompatibility.SupportsLegacyCustomCardPool)
			{
				rewardExtData = BuildSpecificCardsExt(val, __instance.Cards);
				val2.Source = val.Source;
				val2.RarityOdds = val.RarityOdds;
			}
		}
		else if (flag2 && val.CardPools.Count > 0)
		{
			rewardExtData = BuildFilterSnapshotExt(val);
			val2.Source = val.Source;
			val2.RarityOdds = val.RarityOdds;
		}
		else
		{
			val2.Source = val.Source;
			val2.RarityOdds = val.RarityOdds;
			val2.CardPoolIds = val.CardPools.Select((CardPoolModel p) => ((AbstractModel)p).Id).ToList();
		}
		val2.OptionCount = GetOptionCount(__instance);
		if (flag)
		{
			if (rewardExtData == null)
			{
				rewardExtData = new RewardExtData();
			}
			rewardExtData.Flags = (int)val.Flags;
		}
		if (rewardExtData != null)
		{
			RewardSerializationExt.SetExtData(val2, rewardExtData);
		}
		__result = val2;
		return false;
	}

	private static RewardExtData BuildSpecificCardsExt(CardCreationOptions options, IEnumerable<CardModel> cards)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected I4, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		return new RewardExtData
		{
			IsCustomPool = true,
			CustomCardIds = cards.Select((CardModel c) => ((object)((AbstractModel)c).Id).ToString()).ToList(),
			Source = (int)options.Source,
			RarityOdds = (int)options.RarityOdds
		};
	}

	private static RewardExtData BuildCustomPoolExt(CardCreationOptions options, IEnumerable<CardModel> customCardPool)
	{
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Expected I4, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Expected I4, but got Unknown
		return new RewardExtData
		{
			IsCustomPool = true,
			CustomCardIds = customCardPool.Select((CardModel c) => ((object)((AbstractModel)c).Id).ToString()).ToList(),
			Source = (int)options.Source,
			RarityOdds = (int)options.RarityOdds
		};
	}

	private static RewardExtData BuildFilterSnapshotExt(CardCreationOptions options)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected I4, but got Unknown
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Expected I4, but got Unknown
		List<CardModel> source = options.CardPools.SelectMany((CardPoolModel p) => p.AllCards).Where(options.CardPoolFilter).ToList();
		return new RewardExtData
		{
			IsCustomPool = true,
			CustomCardIds = source.Select((CardModel c) => ((object)((AbstractModel)c).Id).ToString()).ToList(),
			Source = (int)options.Source,
			RarityOdds = (int)options.RarityOdds
		};
	}
}
