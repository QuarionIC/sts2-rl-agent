using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public sealed class EternalPower : HermitPowerModel, IHasSecondAmount
{
	private const int MaxReductions = 4;

	public string GetSecondAmount()
	{
		return $"{Math.Max(0, 4 - QualifyingHandDrawsThisTurn())}";
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!participants.Contains(((PowerModel)this).Owner))
		{
			return Task.CompletedTask;
		}
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
		return Task.CompletedTask;
	}

	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (!fromHandDraw || card.Owner.Creature != ((PowerModel)this).Owner || card.Keywords.Contains((CardKeyword)4))
		{
			return Task.CompletedTask;
		}
		if (QualifyingHandDrawsThisTurn() > 4)
		{
			return Task.CompletedTask;
		}
		card.EnergyCost.AddThisTurnOrUntilPlayed(-((PowerModel)this).Amount, true);
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
		return Task.CompletedTask;
	}

	private int QualifyingHandDrawsThisTurn()
	{
		return CombatManager.Instance.History.Entries.OfType<CardDrawnEntry>().Count((CardDrawnEntry e) => ((CombatHistoryEntry)e).HappenedThisTurn(((PowerModel)this).CombatState) && e.FromHandDraw && e.Card.Owner.Creature == ((PowerModel)this).Owner && !e.Card.Keywords.Contains((CardKeyword)4));
	}

	public EternalPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
