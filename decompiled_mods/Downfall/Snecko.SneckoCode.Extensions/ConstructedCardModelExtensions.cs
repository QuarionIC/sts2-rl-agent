using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.DynamicVars;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithMuddle(this ConstructedCardModel card, decimal val, decimal upgrade = 0m)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		card.WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<MuddleVar>(new MuddleVar(val), upgrade) });
		card.WithKeyword(SneckoKeywords.Muddle, (UpgradeType)0);
		return card;
	}

	public static ConstructedCardModel WithOverflow(this ConstructedCardModel card)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		card.WithKeyword(SneckoKeywords.Overflow, (UpgradeType)0);
		return card;
	}

	public static ConstructedCardModel WithGift(this ConstructedCardModel card, Gift gift)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (!(card is IHasGift { Gift: var gift2 } hasGift))
		{
			return card;
		}
		if (gift2.HasValue)
		{
			throw new InvalidOperationException("Gift already set");
		}
		hasGift.Gift = gift;
		card.WithTip(TooltipSource.op_Implicit(SneckoTip.Gift));
		return card;
	}
}
