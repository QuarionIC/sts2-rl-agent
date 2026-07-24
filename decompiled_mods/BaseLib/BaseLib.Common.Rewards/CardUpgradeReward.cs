using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace BaseLib.Common.Rewards;

public sealed class CardUpgradeReward : CustomReward
{
	[CustomEnum(null)]
	public static RewardType CardUpgrade;

	public required int Amount;

	private static string RewardIcon => ImageHelperExtensions.GetModImagePath("ui/reward_screen/reward_icon_card_upgrade.png");

	protected override string IconPath => RewardIcon;

	public override LocString Description
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Expected O, but got Unknown
			LocString val = new LocString("gameplay_ui", "BASELIB-COMBAT_REWARD_CARD_UPGRADE");
			val.Add("cards", (decimal)Amount);
			return val;
		}
	}

	protected override RewardType RewardType => CardUpgrade;

	public override int RewardsSetIndex => 8;

	public override bool IsPopulated => true;

	public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

	public CardUpgradeReward(Player player)
		: base(player)
	{
	}

	public override SerializableReward ToSerializable()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		return new SerializableReward
		{
			RewardType = CardUpgrade,
			GoldAmount = Amount
		};
	}

	public static CardUpgradeReward CreateFromSerializable(SerializableReward save, Player player)
	{
		return new CardUpgradeReward(player)
		{
			Amount = save.GoldAmount
		};
	}

	public override void Populate()
	{
	}

	public override void MarkContentAsSeen()
	{
	}

	protected override async Task<bool> OnSelect()
	{
		BaseLibMain.Logger.Debug($"Player {((Reward)this).Player} Obtained targeted card upgrade from reward", 1);
		return await RunManager.Instance.RewardSynchronizer.DoCardUpgrade(((Reward)this).Player, Amount);
	}
}
