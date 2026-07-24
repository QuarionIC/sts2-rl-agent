using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class SnakeCharmersFlute : SneckoRelicModel, IShouldAllowMuddleCost
{
	public SnakeCharmersFlute()
		: base((RelicRarity)7)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Muddle);
	}

	public bool ShouldAllowMuddleCost(CardModel card, int cost)
	{
		if (cost == 3)
		{
			return card.Owner != ((RelicModel)this).Owner;
		}
		return true;
	}
}
