using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class ChosenVersePower : AwakenedPowerModel
{
	public CardPlay? CardPlay;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	public ChosenVersePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithBlock(4m);
	}

	public void SetBlock(int block)
	{
		((DynamicVar)((PowerModel)this).DynamicVars.Block).BaseValue = block;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && cardPlay != CardPlay && (int)cardPlay.Card.Type != 1)
		{
			await CardPileCmd.Draw(context, 1m, cardPlay.Card.Owner, false);
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, ((PowerModel)this).DynamicVars.Block, (CardPlay)null, false);
			((PowerModel)this).Flash();
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		await PowerCmd.Remove((PowerModel)(object)this);
	}
}
