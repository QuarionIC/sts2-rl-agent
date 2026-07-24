using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Powers;

public sealed class SmokingBarrelPower : HermitPowerModel, IAfterDeadOnTrigger
{
	public async Task AfterDeadOnTrigger(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Apply<VigorPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, cardPlay.Card, false);
		}
	}

	public SmokingBarrelPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
