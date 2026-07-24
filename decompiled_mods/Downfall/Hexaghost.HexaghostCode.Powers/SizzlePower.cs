using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class SizzlePower : HexaghostPowerModel
{
	private bool _ignoredFirst;

	public SizzlePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip((CardKeyword)1);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (!_ignoredFirst)
		{
			_ignoredFirst = true;
			return;
		}
		CardModel card = cardPlay.Card;
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			await CardCmd.Exhaust(ctx, card, false, false);
			((PowerModel)this).Flash();
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
