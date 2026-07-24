using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public abstract class SlimeCard<T> : SlimeBossCardModel, ISlimeCard, IModfyCardDescription where T : SlimeModel
{
	protected override bool IsPlayable => false;

	public override string Title => ((MonsterModel)SlimeModel).Title.GetFormattedText();

	public SlimeModel SlimeModel => ModelDb.Get<T>();

	protected SlimeCard(bool showInCardLibrary = true, bool autoAdd = true)
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1, showInCardLibrary, autoAdd)
	{
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel _) => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)(object)SlimeModel.SlimeTip)));
	}

	public LocString ModifyDescription(LocString oldLocString)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		LocString val = new LocString("cards", "SLIMEBOSS-SLIME_CARD.description");
		val.Add("Slime", ((MonsterModel)SlimeModel).Title.GetFormattedText());
		return val;
	}
}
