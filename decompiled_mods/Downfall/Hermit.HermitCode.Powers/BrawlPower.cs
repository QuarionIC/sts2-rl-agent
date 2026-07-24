using System.Collections.Generic;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class BrawlPower : HermitPowerModel
{
	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			((PowerModel)this).Flash();
			await PowerCmd.Apply<BruisePower>(choiceContext, (IEnumerable<Creature>)((PowerModel)this).CombatState.HittableEnemies, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public BrawlPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
