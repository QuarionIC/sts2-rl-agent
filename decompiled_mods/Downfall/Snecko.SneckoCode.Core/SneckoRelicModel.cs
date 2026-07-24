using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace Snecko.SneckoCode.Core;

public abstract class SneckoRelicModel : DownfallRelicModel<Snecko>
{
	protected SneckoRelicModel(RelicRarity rarity, bool autoAdd = true)
		: base(rarity, autoAdd)
	{
	}//IL_0001: Unknown result type (might be due to invalid IL or missing references)

}
