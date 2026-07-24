using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(QuestCardPool))]
public sealed class TheBox : CustomCardModel
{
	public override int MaxUpgradeLevel => 0;

	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

	public TheBox()
		: base(-1, (CardType)6, (CardRarity)10, (TargetType)0, true, true)
	{
	}
}
