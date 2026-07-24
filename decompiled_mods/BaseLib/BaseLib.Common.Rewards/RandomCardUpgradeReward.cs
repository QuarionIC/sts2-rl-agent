using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Common.Rewards;

public sealed class RandomCardUpgradeReward : CustomReward
{
	[CustomEnum(null)]
	public static RewardType RandomCardUpgrade;

	private static string RewardIcon => ImageHelperExtensions.GetModImagePath("ui/reward_screen/reward_icon_card_upgrade_random.png");

	protected override string IconPath => RewardIcon;

	public override LocString Description => new LocString("gameplay_ui", "BASELIB-COMBAT_REWARD_RANDOM_CARD_UPGRADE");

	protected override RewardType RewardType => RandomCardUpgrade;

	public override int RewardsSetIndex => 8;

	public override bool IsPopulated => true;

	public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

	public RandomCardUpgradeReward(Player player)
		: base(player)
	{
	}

	public override void Populate()
	{
	}

	public override void MarkContentAsSeen()
	{
	}

	public static RandomCardUpgradeReward CreateFromSerializable(SerializableReward save, Player player)
	{
		return new RandomCardUpgradeReward(player);
	}

	protected override async Task<bool> OnSelect()
	{
		List<CardModel> list = ListExtensions.StableShuffle<CardModel>(PileTypeExtensions.GetPile((PileType)6, ((Reward)this).Player).Cards.Where((CardModel c) => c.IsUpgradable).ToList(), ((Reward)this).Player.RunState.Rng.Niche).Take(1).ToList();
		if (list == null)
		{
			return false;
		}
		foreach (CardModel item in list)
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
		}
		return list.Count > 0;
	}
}
