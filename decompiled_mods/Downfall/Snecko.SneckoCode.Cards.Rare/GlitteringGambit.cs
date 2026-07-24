using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class GlitteringGambit : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public GlitteringGambit()
		: base(-1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		((ConstructedCardModel)this).WithVar((DynamicVar)new GoldVar(150));
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)4,
			IsUpgraded = true,
			Gold = 150
		});
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)4,
			(CardKeyword)7
		});
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)1);
	}
}
