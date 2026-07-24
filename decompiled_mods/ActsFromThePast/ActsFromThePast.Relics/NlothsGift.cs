using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class NlothsGift : CustomRelicModel
{
	internal static Player? CurrentRewardPlayer;

	internal static NlothsGift? InstanceToFlash;

	internal static bool ShouldFlash;

	internal static float LastRoll;

	internal static float OriginalThreshold;

	public override RelicRarity Rarity => (RelicRarity)6;

	public NlothsGift()
		: base(true)
	{
	}
}
