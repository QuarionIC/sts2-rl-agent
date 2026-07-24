using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class Belittle : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public Belittle()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)3,
			IsDebuff = true
		});
		((ConstructedCardModel)this).WithCalculatedDamage(0, 8, (Func<CardModel, Creature, decimal>)CalcDamage, (ValueProp)14, 0, 3);
	}

	private static decimal CalcDamage(CardModel card, Creature? creature)
	{
		return (creature != null) ? creature.Powers.Count((PowerModel e) => (int)e.TypeForCurrentAmount == 2) : 0;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
