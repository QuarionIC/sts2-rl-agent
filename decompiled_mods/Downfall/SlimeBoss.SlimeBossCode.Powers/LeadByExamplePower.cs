using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class LeadByExamplePower : SlimeBossPowerModel, IHasSecondAmount
{
	private int CardPlayCount => CombatManager.Instance.History.CardPlaysFinished.Count(delegate(CardPlayFinishedEntry e)
	{
		if (((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState))
		{
			Creature target = e.CardPlay.Target;
			if (target != null)
			{
				return target.IsEnemy;
			}
			return false;
		}
		return false;
	});

	public string GetSecondAmount()
	{
		return $"{CardPlayCount}";
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner)
		{
			Creature target = cardPlay.Target;
			if (target != null && target.IsEnemy && CardPlayCount <= ((PowerModel)this).Amount)
			{
				await SlimeBossCmd.Command(ctx, cardPlay.Card.Owner, 1, (ValueProp)4);
				((PowerModel)this).Flash();
				PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
			}
		}
	}

	protected override Task AfterSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(((PowerModel)this).Owner))
		{
			return Task.CompletedTask;
		}
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
		return Task.CompletedTask;
	}

	public LeadByExamplePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
