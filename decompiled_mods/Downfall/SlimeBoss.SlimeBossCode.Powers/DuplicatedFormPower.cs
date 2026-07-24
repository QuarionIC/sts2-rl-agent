using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Powers;

public class DuplicatedFormPower : SlimeBossPowerModel, IHasSecondAmount
{
	private int _visualValue;

	private int CardsPlayed => CombatManager.Instance.History.CardPlaysStarted.Count(delegate(CardPlayStartedEntry e)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Invalid comparison between Unknown and I4
		if (((CombatHistoryEntry)e).Actor == ((PowerModel)this).Owner && e.CardPlay.IsFirstInSeries && ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState))
		{
			Creature target = e.CardPlay.Target;
			if (target != null)
			{
				return (int)target.Side == 2;
			}
			return false;
		}
		return false;
	});

	public string GetSecondAmount()
	{
		return $"{Math.Min(_visualValue, ((PowerModel)this).Amount)}";
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		if (card.Owner.Creature == ((PowerModel)this).Owner && target != null && (int)target.Side == 2 && CardsPlayed < ((PowerModel)this).Amount)
		{
			return playCount + 1;
		}
		return playCount;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(((PowerModel)this).Owner))
		{
			return Task.CompletedTask;
		}
		_visualValue = 0;
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
		return Task.CompletedTask;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		((PowerModel)this).Flash();
		_visualValue++;
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
		return Task.CompletedTask;
	}

	public DuplicatedFormPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
