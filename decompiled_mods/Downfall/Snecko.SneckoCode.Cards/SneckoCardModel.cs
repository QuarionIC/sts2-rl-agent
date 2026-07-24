using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards;

public abstract class SneckoCardModel : DownfallCardModel<Snecko.SneckoCode.Core.Snecko>
{
	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			if (((CardModel)this).Keywords.Contains(SneckoKeywords.Overflow))
			{
				return SneckoCmd.OverflowActive((CardModel)(object)this);
			}
			return false;
		}
	}

	protected SneckoCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)
	//IL_0004: Unknown result type (might be due to invalid IL or missing references)

}
