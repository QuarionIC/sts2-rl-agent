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

public sealed class CardTransformReward : CustomReward
{
	[CustomEnum(null)]
	public static RewardType CardTransform;

	public required bool Upgrade;

	public required int Amount;

	protected override RewardType RewardType => CardTransform;

	public override LocString Description
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			LocString val = new LocString("gameplay_ui", "BASELIB-COMBAT_REWARD_CARD_TRANSFORM");
			val.Add("cards", (decimal)Amount);
			val.Add("Upgrade", Upgrade);
			return val;
		}
	}

	public override bool IsPopulated => true;

	public static string RewardIcon => ImageHelperExtensions.GetModImagePath("ui/reward_screen/reward_icon_card_transform.png");

	protected override string IconPath => RewardIcon;

	public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

	public CardTransformReward(Player player)
		: base(player)
	{
	}

	public override SerializableReward ToSerializable()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Expected O, but got Unknown
		return new SerializableReward
		{
			RewardType = CardTransform,
			GoldAmount = Amount,
			WasGoldStolenBack = Upgrade
		};
	}

	public static CardTransformReward CreateFromSerializable(SerializableReward save, Player player)
	{
		return new CardTransformReward(player)
		{
			Amount = save.GoldAmount,
			Upgrade = save.WasGoldStolenBack
		};
	}

	public override void MarkContentAsSeen()
	{
	}

	public override void Populate()
	{
	}

	protected override async Task<bool> OnSelect()
	{
		BaseLibMain.Logger.Info("Obtained card transformation from reward", 1);
		return await RunManager.Instance.RewardSynchronizer.DoUnsyncedCardTransform(((Reward)this).Player, Amount, upgrade: true);
	}
}
