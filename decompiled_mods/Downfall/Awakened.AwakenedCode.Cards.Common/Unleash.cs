using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Unleash : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Unleash()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(5, (Func<CardModel, Creature, decimal>)DamageCalc, (ValueProp)8, 0, 1);
	}

	private static decimal DamageCalc(CardModel card, Creature? creature)
	{
		return PileTypeExtensions.GetPile((PileType)2, card.Owner).Cards.Count((CardModel c) => c != card);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay.Target, ((CardModel)this).DynamicVars.CalculatedDamage, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
