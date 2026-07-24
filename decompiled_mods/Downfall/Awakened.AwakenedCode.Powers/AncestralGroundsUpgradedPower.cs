using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Powers;

public class AncestralGroundsUpgradedPower : AwakenedPowerModel
{
	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (participants.Contains(((PowerModel)this).Owner) && ((PowerModel)this).Owner.Player != null)
		{
			await PlayerCmd.GainEnergy(3m, ((PowerModel)this).Owner.Player);
			await DownfallCardCmd.GiveCard<Void>(((PowerModel)this).Owner.Player, (PileType)1, (CardPilePosition)2, upgraded: false, 0.2f, (CardPreviewStyle)1, skipAnimation: false, (Action<Void>?)null, (Player?)null);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public AncestralGroundsUpgradedPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
