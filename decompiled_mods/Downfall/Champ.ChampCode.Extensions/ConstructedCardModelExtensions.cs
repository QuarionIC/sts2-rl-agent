using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Powers;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Extensions;

public static class ConstructedCardModelExtensions
{
	public static ConstructedCardModel WithDefensiveTip(this ConstructedCardModel card)
	{
		return card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(ChampModelDb.ChampStance<ChampDefensiveStance>().HoverTip)));
	}

	public static ConstructedCardModel WithBerserkerTip(this ConstructedCardModel card)
	{
		return card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(ChampModelDb.ChampStance<ChampBerserkerStance>().HoverTip)));
	}

	public static ConstructedCardModel WithUltimateTip(this ConstructedCardModel card)
	{
		return card.WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(ChampModelDb.ChampStance<ChampUltimateStance>().HoverTip)));
	}

	public static ConstructedCardModel WithFinisher(this ConstructedCardModel card)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected I4, but got Unknown
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		card.WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)ChampTag.Finisher });
		card.WithTip(TooltipSource.op_Implicit(ChampTip.Finisher));
		return card;
	}

	public static ConstructedCardModel WithEnterBerserker(this ConstructedCardModel card)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected I4, but got Unknown
		card.WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)ChampTag.EnterBerserker });
		card.WithBerserkerTip();
		return card;
	}

	public static ConstructedCardModel WithEnterDefensive(this ConstructedCardModel card)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Expected I4, but got Unknown
		card.WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)ChampTag.EnterDefensive });
		card.WithDefensiveTip();
		return card;
	}

	public static ConstructedCardModel WithGlory(this ConstructedCardModel card, int baseVal, int upgrade = 0)
	{
		card.WithPower<GloryPower>(baseVal, upgrade);
		card.WithUltimateTip();
		return card;
	}
}
