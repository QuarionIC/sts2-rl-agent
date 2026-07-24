using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(Reward), "FromSerializable")]
public static class RewardFromSerializableExtPatch
{
	private static bool Prefix(SerializableReward save, Player player, ref Reward __result)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)save.RewardType != 1 || !RewardSerializationExt.TryGetExtData(save, out RewardExtData data) || data == null)
		{
			return true;
		}
		__result = (Reward)(object)RebuildCardReward(save, data, player);
		return false;
	}

	private static CardReward RebuildCardReward(SerializableReward save, RewardExtData ext, Player player)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Expected O, but got Unknown
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017d: Expected O, but got Unknown
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0115: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		CardCreationFlags val = (CardCreationFlags)ext.Flags;
		if (ext != null && ext.IsCustomPool && ext.CustomCardIds != null)
		{
			CardCreationSource val2 = (CardCreationSource)ext.Source;
			CardRarityOddsType val3 = (CardRarityOddsType)ext.RarityOdds;
			List<CardModel> list = (from id in ext.CustomCardIds
				select ModelDb.GetByIdOrNull<CardModel>(ModelId.Deserialize(id)) into c
				where c != null
				select (c)).ToList();
			if (list.Count > 0)
			{
				if (CardRewardSerializationCompatibility.SupportsLegacyCustomCardPool)
				{
					CardCreationOptions val4 = CardRewardSerializationCompatibility.CreateCustomPoolOptions(list, val2, val3);
					if ((int)val != 0)
					{
						val4.WithFlags(val);
					}
					return new CardReward(val4, save.OptionCount, player, (PlayerChoiceSynchronizer)null);
				}
				CardCreationOptions val5 = new CardCreationOptions((IEnumerable<CardPoolModel>)new _003C_003Ez__ReadOnlySingleElementList<CardPoolModel>(player.Character.CardPool), val2, val3, (Func<CardModel, bool>)null);
				if ((int)val != 0)
				{
					val5.WithFlags(val);
				}
				return new CardReward((IEnumerable<CardModel>)list, val2, player, val5, (PlayerChoiceSynchronizer)null);
			}
			BaseLibMain.Logger.Warn("Reward.FromSerializable: CustomCardPool had no resolvable cards, falling back.", 1);
		}
		CardCreationOptions val6 = new CardCreationOptions((IEnumerable<CardPoolModel>)((IEnumerable<ModelId>)save.CardPoolIds).Select((Func<ModelId, CardPoolModel>)ModelDb.GetById<CardPoolModel>).ToList(), save.Source, save.RarityOdds, (Func<CardModel, bool>)null);
		if ((int)val != 0)
		{
			val6.WithFlags(val);
		}
		return new CardReward(val6, save.OptionCount, player, (PlayerChoiceSynchronizer)null);
	}
}
