using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Events;
using Hermit.HermitCode.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class ConcentrationPower : HermitPowerModel, IShouldTriggerDeadOn, IAfterDeadOnTrigger
{
	public ConcentrationPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(HermitKeywords.DeadOn);
	}

	public async Task AfterDeadOnTrigger(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, -1m, ((PowerModel)this).Owner, cardPlay.Card, false);
		}
	}

	public bool ShouldTriggerDeadOn(CardModel card)
	{
		return card.Owner.Creature == ((PowerModel)this).Owner;
	}

	public override async Task AfterSideTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (!participants.Contains(((PowerModel)this).Owner))
		{
			Player player = ((PowerModel)this).Owner.Player;
			if (((player != null) ? player.GetRelic<Spyglass>() : null) == null)
			{
				await PowerCmd.Remove((PowerModel)(object)this);
			}
		}
	}
}
