using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Rewards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;

namespace Guardian.GuardianCode.Relics;

[Pool(typeof(GuardianRelicPool))]
public class SackOfGems : GuardianRelicModel
{
	public override bool HasUponPickupEffect => true;

	public SackOfGems()
		: base((RelicRarity)5)
	{
	}

	public override async Task AfterObtained()
	{
		await RewardsCmd.OfferCustom(((RelicModel)this).Owner, new List<Reward>(1) { (Reward)(object)new GemFinderReward(5, 10, ((RelicModel)this).Owner) });
	}
}
