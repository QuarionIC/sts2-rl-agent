using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class GoldenIdol : CustomRelicModel
{
	private const decimal GoldMultiplier = 0.25m;

	internal static readonly Dictionary<GoldReward, (int baseAmount, int bonus)> BoostedRewards = new Dictionary<GoldReward, (int, int)>();

	private static readonly PropertyInfo AmountProperty = AccessTools.Property(typeof(GoldReward), "Amount");

	public override RelicRarity Rarity => (RelicRarity)6;

	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		if (player != ((RelicModel)this).Owner || room == null || !RoomTypeExtensions.IsCombatRoom(room.RoomType))
		{
			return false;
		}
		bool result = false;
		foreach (GoldReward item in rewards.OfType<GoldReward>())
		{
			if (((Reward)item).IsPopulated)
			{
				int amount = item.Amount;
				int num = (int)((decimal)amount * 0.25m);
				AmountProperty.SetValue(item, amount + num);
				BoostedRewards[item] = (amount, num);
				result = true;
			}
		}
		return result;
	}

	public override Task AfterModifyingRewards()
	{
		((RelicModel)this).Flash();
		return Task.CompletedTask;
	}

	public GoldenIdol()
		: base(true)
	{
	}
}
