using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Downfall.DownfallCode.Abstract;

public abstract class HookedPowerModel : CustomPowerModel
{
	private Task ExecuteWithContext(Func<PlayerChoiceContext, Task> action)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		if (!LocalContext.NetId.HasValue)
		{
			return action((PlayerChoiceContext)new ThrowingPlayerChoiceContext());
		}
		if (((PowerModel)this).Owner.IsDead)
		{
			return Task.CompletedTask;
		}
		return action((PlayerChoiceContext)new BlockingPlayerChoiceContext());
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? player)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterCardGeneratedForCombat(ctx, card, player));
	}

	protected virtual Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? player)
	{
		return Task.CompletedTask;
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterApplied(ctx, applier, cardSource));
	}

	protected virtual Task AfterApplied(PlayerChoiceContext ctx, Creature? applier, CardModel? cardSource)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterRemoved(Creature oldOwner)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterRemoved(ctx, oldOwner));
	}

	protected virtual Task AfterRemoved(PlayerChoiceContext ctx, Creature oldOwner)
	{
		return Task.CompletedTask;
	}

	public override Task AfterEnergyReset(Player player)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterEnergyReset(ctx, player));
	}

	protected virtual Task AfterEnergyReset(PlayerChoiceContext ctx, Player player)
	{
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterSideTurnStart(ctx, side, participants, combatState));
	}

	protected virtual Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterBlockGained(ctx, creature, amount, props, cardSource));
	}

	protected virtual Task AfterBlockGained(PlayerChoiceContext ctx, Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterModifyingHpLostAfterOsty()
	{
		return ExecuteWithContext(AfterModifyingHpLostAfterOsty);
	}

	protected virtual Task AfterModifyingHpLostAfterOsty(PlayerChoiceContext ctx)
	{
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => BeforeCardPlayed(ctx, cardPlay));
	}

	protected virtual Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterCardChangedPiles(ctx, card, oldPileType, clonedBy));
	}

	protected virtual Task AfterCardChangedPiles(PlayerChoiceContext card, CardModel oldPileType, PileType clonedBy, AbstractModel? abstractModel)
	{
		return Task.CompletedTask;
	}
}
