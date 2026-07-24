using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Powers;

public class InfiniteBeamsPower : AutomatonPowerModel
{
	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Owner.Player != null)
		{
			await DownfallCardCmd.GiveCards<MinorBeam>(((PowerModel)this).Owner.Player, (PileType)2, (decimal)((PowerModel)this).Amount, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<MinorBeam>?)null, (Player?)null);
		}
	}

	public InfiniteBeamsPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
