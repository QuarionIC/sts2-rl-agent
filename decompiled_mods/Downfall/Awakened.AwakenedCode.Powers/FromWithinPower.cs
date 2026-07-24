using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class FromWithinPower : AwakenedPowerModel
{
	public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && cardPlay.Card.VisualCardPool.IsColorless)
		{
			await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, cardPlay.Card.Owner);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public FromWithinPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
