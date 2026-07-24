using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class BodyCrash : GuardianCardModel
{
	public BodyCrash()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithBlock(5, 3);
		((ConstructedCardModel)this).WithCalculatedDamage(0, (Func<CardModel, Creature, decimal>)Calc, (ValueProp)8, 0, 0);
		((ConstructedCardModel)this).WithCalculatedDamage("VisualBlock", 0, (Func<CardModel, Creature, decimal>)Calc2, (ValueProp)8, 0, 0);
	}

	private static decimal Calc2(CardModel card, Creature? arg2)
	{
		return (decimal)card.Owner.Creature.Block + ((DynamicVar)card.DynamicVars.Block).PreviewValue;
	}

	private static decimal Calc(CardModel card, Creature? arg2)
	{
		return card.Owner.Creature.Block;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
