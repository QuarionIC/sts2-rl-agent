using System;
using BaseLib.Extensions;
using Baselib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;

namespace BaseLib.Abstracts;

public abstract class CustomReward : Reward
{
	public override int RewardsSetIndex => 9;

	public abstract CreateRewardFromSave<CustomReward> DeserializeMethod { get; }

	protected CustomReward(Player player)
		: base(player)
	{
	}

	public LocString GetLoc()
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		return new LocString("gameplay_ui", ((object)this).GetType().GetPrefix() + StringHelper.Slugify(((object)this).GetType().Name));
	}

	public virtual void Initialize()
	{
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		if (DeserializeMethod.Target != null)
		{
			throw new ArgumentException($"DeserializeMethod of {((object)this).GetType()} is not static");
		}
		BaseLibMain.Logger.Info($"Registering CustomReward deserializer for {((object)this).GetType()}", 1);
		CustomRewardPatches.RegisterCustomReward(((Reward)this).RewardType, DeserializeMethod);
	}
}
