using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Cards.Uncommon;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.History;
using SlimeBoss.SlimeBossCode.Interfaces;

namespace SlimeBoss.SlimeBossCode.Powers;

public class GoopPower : SlimeBossPowerModel, IModifyDamageAdditive
{
	private class Data
	{
		public int AmountWhenAttackStarted;

		public AttackCommand? CommandToModify;
	}

	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	public GoopPower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		if (((PowerModel)this).Owner != target || dealer != ((PowerModel)this).Applier || !ValuePropExtensions.IsPoweredAttack(props))
		{
			return 0m;
		}
		Data internalData = ((PowerModel)this).GetInternalData<Data>();
		if ((internalData.CommandToModify == null || cardSource == null || (object)cardSource == internalData.CommandToModify.ModelSource) && (internalData.CommandToModify == null || internalData.CommandToModify.Attacker == dealer))
		{
			return (decimal)((PowerModel)this).Amount * ((cardSource is IDoubleGoopBonus) ? 2m : 1m);
		}
		return 0m;
	}

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override Task BeforeAttack(AttackCommand command)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (command.Attacker != ((PowerModel)this).Applier || !ValuePropExtensions.IsPoweredAttack(command.DamageProps))
		{
			return Task.CompletedTask;
		}
		Data internalData = ((PowerModel)this).GetInternalData<Data>();
		if (internalData.CommandToModify != null || (command.ModelSource != null && !(command.ModelSource is CardModel)))
		{
			return Task.CompletedTask;
		}
		internalData.CommandToModify = command;
		internalData.AmountWhenAttackStarted = ((PowerModel)this).Amount;
		return Task.CompletedTask;
	}

	public override async Task AfterAttack(PlayerChoiceContext ctx, AttackCommand command)
	{
		Creature attacker = command.Attacker;
		if (attacker != null)
		{
			Data internalData = ((PowerModel)this).GetInternalData<Data>();
			if (command != internalData.CommandToModify || command.Results.SelectMany((List<DamageResult> a) => a).All((DamageResult e) => e.Receiver != ((PowerModel)this).Owner))
			{
				internalData.CommandToModify = null;
			}
			else
			{
				await ConsumeGoop(ctx, ((PowerModel)this).Owner, attacker, command);
			}
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (creature == ((PowerModel)this).Owner)
		{
			Data internalData = ((PowerModel)this).GetInternalData<Data>();
			Creature applier = ((PowerModel)this).Applier;
			if (applier != null && internalData.CommandToModify != null)
			{
				await ConsumeGoop(choiceContext, creature, applier, internalData.CommandToModify);
			}
		}
	}

	private async Task ConsumeGoop(PlayerChoiceContext ctx, Creature creature, Creature attacker, AttackCommand command)
	{
		Data internalData = ((PowerModel)this).GetInternalData<Data>();
		int amount = ((PowerModel)this).Amount;
		int originalAmount = -internalData.AmountWhenAttackStarted;
		IEnumerable<IModifyGoopConsume> modifiers;
		int newAmount = SlimeBossHook.ModifyGoopConsume(((PowerModel)this).CombatState, originalAmount, out modifiers, creature, ((PowerModel)this).Applier);
		await SlimeBossHook.AfterModifyingGoopConsume(((PowerModel)this).CombatState, modifiers, creature, ((PowerModel)this).Applier);
		await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, (decimal)newAmount, (Creature)null, (CardModel)null, false);
		if (command.ModelSource is IHasConsumeEffect hasConsumeEffect)
		{
			await hasConsumeEffect.ConsumeEffect(ctx, creature, command, amount);
		}
		internalData.CommandToModify = null;
		ConsumeEntry consumeEntry = new ConsumeEntry(creature, amount, attacker, ((PowerModel)this).CombatState.RoundNumber, attacker.Side, CombatManager.Instance.History, ((PowerModel)this).CombatState.Players);
		CombatManager.Instance.History.Add(((PowerModel)this).CombatState, (CombatHistoryEntry)(object)consumeEntry);
		await SlimeBossHook.AfterConsumeEffect(((PowerModel)this).CombatState, ctx, creature, attacker, amount);
	}
}
