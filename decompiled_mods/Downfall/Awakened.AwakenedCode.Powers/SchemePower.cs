using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class SchemePower : AwakenedPowerModel
{
	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((PowerModel)this).Owner.Player && !cardPlay.IsAutoPlay && !(cardPlay.Card is Scheme))
		{
			CardModel val = cardPlay.Card.CreateDupeCompat();
			await CardCmd.AutoPlay(ctx, val, cardPlay.Target, (AutoPlayType)1, false, false);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((PowerModel)this).Owner))
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}

	public SchemePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
