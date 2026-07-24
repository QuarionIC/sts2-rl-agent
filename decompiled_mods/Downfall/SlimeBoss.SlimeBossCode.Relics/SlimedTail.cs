using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class SlimedTail : SlimeBossRelicModel, IAfterSplit
{
	public SlimedTail()
		: base((RelicRarity)4)
	{
		WithBlock(3);
	}

	public Task AfterSplit(Player player, SlimeModel slime)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		return MyCommonActions.Block((AbstractModel)(object)this);
	}
}
