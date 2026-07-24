using System;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class TheTower : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public TheTower()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(2, 2, (Func<CardModel, Creature, decimal>)DamageCalc, (ValueProp)8, 1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay.Target, ((CardModel)this).DynamicVars.CalculatedDamage, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	private static decimal DamageCalc(CardModel card, Creature? creature)
	{
		return CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count((CardGeneratedEntry e) => e.Creator != null && e.Card.Owner == card.Owner);
	}
}
