using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace SlimeBoss.SlimeBossCode.Core;

public abstract class SlimeBossRelicModel : DownfallRelicModel<SlimeBoss>
{
	protected SlimeBossRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
