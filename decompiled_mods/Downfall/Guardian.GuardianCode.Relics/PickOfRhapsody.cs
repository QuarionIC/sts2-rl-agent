using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Rewards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class PickOfRhapsody : GuardianRelicModel
{
	public PickOfRhapsody()
		: base((RelicRarity)3)
	{
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)((AbstractRoom)room).RoomType != 2)
		{
			return Task.CompletedTask;
		}
		GemFinderReward gemFinderReward = new GemFinderReward(1, 1, ((RelicModel)this).Owner);
		room.AddExtraReward(((RelicModel)this).Owner, (Reward)(object)gemFinderReward);
		return Task.CompletedTask;
	}
}
