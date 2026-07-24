using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Ghostflames;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(TokenCardPool))]
public class BadOmenChoice : HexaghostCardModel
{
	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public GhostflameModel? GhostflameModel { get; private set; }

	public override string CustomPortraitPath => ((CustomCardModel)ModelDb.Card<BadOmen>()).CustomPortraitPath;

	public BadOmenChoice()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)delegate(CardModel c)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (c is BadOmenChoice badOmenChoice)
			{
				GhostflameModel ghostflameModel = badOmenChoice.GhostflameModel;
				if (ghostflameModel != null)
				{
					return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)(object)ghostflameModel.HoverTip);
				}
			}
			return Array.Empty<IHoverTip>();
		});
	}

	public static BadOmenChoice Create(GhostflameModel flame, Player owner)
	{
		BadOmenChoice badOmenChoice = owner.Creature.CombatState.CreateCard<BadOmenChoice>(owner);
		badOmenChoice.GhostflameModel = flame;
		return badOmenChoice;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		description.Add("Ghostflame", GhostflameModel?.Title ?? HexaghostModelDb.Ghostflame<InfernoGhostflame>().Title);
	}
}
