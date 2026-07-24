using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class FinalStrike : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public FinalStrike()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			IsStrike = true
		});
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithCalculatedVar("UniqueStrikesPlayed", 0, (Func<CardModel, Creature, decimal>)UniqueStrikesPlayed, 0, 0);
	}

	private static decimal UniqueStrikesPlayed(CardModel card, Creature? creature)
	{
		return (from e in CombatManager.Instance.History.CardPlaysFinished
			select e.CardPlay.Card into e
			where e.Owner == card.Owner && e.Tags.Contains((CardTag)1)
			select e).DistinctBy((CardModel c) => ((AbstractModel)c).Id).Count() + 1;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = (int)UniqueStrikesPlayed((CardModel)(object)this, null);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
