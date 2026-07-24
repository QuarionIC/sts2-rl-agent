using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class DrawFewerCardsNextTurnPower : HermitPowerModel
{
	public DrawFewerCardsNextTurnPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		if (player != ((PowerModel)this).Owner.Player || ((PowerModel)this).AmountOnTurnStart == 0)
		{
			return count;
		}
		return Math.Max(0m, count - 1m);
	}

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).AmountOnTurnStart != 0)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
