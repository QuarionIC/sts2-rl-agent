using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(TokenCardPool))]
public class FlareFlickChoice : HexaghostCardModel
{
	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public CardKeyword Keyword { get; private set; } = (CardKeyword)1;

	public override string CustomPortraitPath => ((CustomCardModel)ModelDb.Card<FlareFlick>()).CustomPortraitPath;

	public FlareFlickChoice()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel c) => (!(c is FlareFlickChoice { Keyword: var keyword })) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromKeyword(keyword)))));
	}

	public static FlareFlickChoice Create(CardKeyword flame, Player owner)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		FlareFlickChoice flareFlickChoice = owner.Creature.CombatState.CreateCard<FlareFlickChoice>(owner);
		flareFlickChoice.Keyword = flame;
		return flareFlickChoice;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		description.Add("Keyword", CardKeywordExtensions.GetTitle(Keyword));
	}
}
